using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GPUSerializerManager : MonoBehaviour
{
    public CustomRenderTexture gpuSerializerRenderTexture;
    public int universeCount = 32;

    private Texture2D serializerTexture;
    private Texture2D metaTexture;
    private byte[] dataBuffer;

    public Texture2D Generate_sRGB_LUT()
    {
        Texture2D sRGB_LUT = new Texture2D(256, 1, TextureFormat.R8, false, true);

        for (int indx = 0; indx < sRGB_LUT.width; indx ++)
            sRGB_LUT.SetPixel(indx, 0, new Color(indx / 255.0f, 0, 0).linear);

        sRGB_LUT.Apply();
        return sRGB_LUT;
    }
    
    void Start()
    {
        serializerTexture = new Texture2D(512, universeCount, TextureFormat.R8, false, true);
        serializerTexture.filterMode = FilterMode.Point;
        serializerTexture.wrapMode = TextureWrapMode.Clamp;

        metaTexture = new Texture2D(512, universeCount, TextureFormat.R16, false, true);
        metaTexture.filterMode = FilterMode.Point;
        metaTexture.wrapMode = TextureWrapMode.Clamp;

        dataBuffer = new byte[512 * universeCount];

        gpuSerializerRenderTexture.material.SetTexture("_serializerData", serializerTexture);
        gpuSerializerRenderTexture.material.SetTexture("_serializerMeta", metaTexture);
        gpuSerializerRenderTexture.material.SetTexture("_sRGB_LUT", Generate_sRGB_LUT());
    }

    void OnDestroy()
    {
        gpuSerializerRenderTexture.ClearUpdateZones();
    }

    public void UpdateGPUSerializerChannelMeta()
    {
        ushort[] serializerMeta = new ushort[dataBuffer.Length];
        int cumulativeOffset = 0;

        for (int indx = 0; indx < serializerMeta.Length; indx ++)
        {
            //check if between any masked channel sets
            bool renderChannel = !Loader.showconf.invertMask;
            foreach (DMXChannelRange channel in Loader.showconf.maskedChannels)
            {
                if (channel.Contains(indx))
                {
                    renderChannel = Loader.showconf.invertMask;
                    break;
                }
            }

            uint colorMode = 0; // 0: A, 1: B, 2: BG, 3: BGR

            foreach (IDMXSerializer serializer in Loader.showconf.Serializers)
                if (serializer.GetType() == typeof(FuralitySomna))
                {
                    var mergedChannels = ((FuralitySomna)serializer).mergedChannels;
                    int channelOffset = 0;

                    int maxInd = 3;

                    for (int rollbackIndx = 0; rollbackIndx < maxInd; rollbackIndx ++)
                    {
                        if (mergedChannels.ContainsKey(indx + rollbackIndx))
                        {
                            if (mergedChannels[indx + rollbackIndx] == TextureWriter.ColorChannel.Blue && rollbackIndx == (maxInd - 1))
                            {
                                cumulativeOffset += channelOffset;
                                colorMode = (uint)channelOffset + 1;
                                break;
                            }
                            
                            channelOffset++;
                        }
                    }

                    break;
                }

            uint channelMeta = (uint)Math.Abs(cumulativeOffset);

            channelMeta = (channelMeta << 2) | (colorMode & 0x03);
            channelMeta = (channelMeta << 1) | (cumulativeOffset < 0 ? 1u : 0u);
            channelMeta = (channelMeta << 1) | (renderChannel ? 1u : 0u);

            serializerMeta[indx] = Convert.ToUInt16(channelMeta & 0xffff);
        }

        metaTexture.SetPixelData(serializerMeta, 0);
        metaTexture.Apply();
    }
    
    public void UpdateGPUSerializerZones(ref List<IDMXSerializer> dmxSerializers)
    {
        gpuSerializerRenderTexture.SetUpdateZones(dmxSerializers.Select(serializer => serializer.RTUpdateZone).ToArray());

        gpuSerializerRenderTexture.material.SetVectorArray("_SerializerSizes", dmxSerializers.Select(serializer => {
            int serializerBlockSize = Math.Clamp(serializer.BlockSize - 1, 0, 63) & 0x3f;
            int serializerRows = Math.Clamp(serializer.RowCount - 1, 0, 1023) & 0x03ff;
            
            int serializerBlockRow = (serializerRows << 6) | serializerBlockSize;

            int orientationMode = 0;

            switch (serializer.GetType().Name)
            {
                case "VRSL":
                    VRSL vrslSerializer = (VRSL)serializer;

                    orientationMode = vrslSerializer.RGBGridMode ? 1 : 0;
                    orientationMode = (orientationMode << 1) | (vrslSerializer.GammaCorrection ? 1 : 0);
                    break;

                default:
                    break;
            }

            orientationMode = (orientationMode << 2) | 0x0; // Vertical and Horizontal flip

            return new Vector4(serializer.Size.x, serializer.Size.y, serializerBlockRow, orientationMode);
        }).ToArray());

        gpuSerializerRenderTexture.material.SetVectorArray("_SerializerRanges", dmxSerializers.Select(serializer => {
            return new Vector4(serializer.DataOffset, serializer.DataLength, 0, 0);
        }).ToArray());

        gpuSerializerRenderTexture.Initialize(); // Clear canvas when serializer layout changes
        gpuSerializerRenderTexture.Update(); // Just in case we update zones in between data refresh and frame presentation.
    }

    public void RefreshGPUSerializer(ref List<byte> serializerData)
    {
        Array.Copy(serializerData.ToArray(), dataBuffer, Math.Clamp(serializerData.Count, 0, dataBuffer.Length));

        serializerTexture.LoadRawTextureData(dataBuffer);
        serializerTexture.Apply();

        gpuSerializerRenderTexture.Update();
    }

    public void FullUpdate()
    {
        UpdateGPUSerializerZones(ref Loader.showconf.Serializers);
        UpdateGPUSerializerChannelMeta();
    }

    public static int FindPass(object passObject)
    {
        return FindAnyObjectByType<GPUSerializerManager>().gpuSerializerRenderTexture.material.FindPass(passObject.GetType().FullName);
    }
}
