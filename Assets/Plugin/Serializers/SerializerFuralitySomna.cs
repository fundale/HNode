using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using static TextureWriter;

public class FuralitySomna : IDMXSerializer
{
    public Vector2 Origin { get; }
    public Vector2 Size { get; }
    public int DataOffset { get; }
    public int DataLength { get; }
    public int Universe { get; }
    public int BlockSize { get; }
    public int RowCount { get; }
    public CustomRenderTextureUpdateZone RTUpdateZone { get; }
    public Dictionary<DMXChannel, ColorChannel> mergedChannels = new Dictionary<DMXChannel, ColorChannel>();

    int cumulativeOFfset = 0;
    
    public FuralitySomna(Vector2 ?origin, Vector2 ?size, int ?dataOffset = 0, int ?dataLength = (512 * 4), int ?universe = 0, int ?blockSize = 16, int ?rowCount = 13)
    {
        Origin = origin ?? Vector2.zero;
        Size = size ?? new Vector2(1920, 208); // TODO: Change
        DataOffset = dataOffset ?? 0;
        DataLength = dataLength ?? (512 * 4); // TODO: Change
        Universe = universe ?? 0;
        BlockSize = blockSize ?? 16; // 16x16 pixels per channel block
        RowCount = rowCount ?? 13; // 13 blocks per column, linear, RGB packed, VRSL alike

        CustomRenderTextureUpdateZone updateZone = new CustomRenderTextureUpdateZone();
        
        updateZone.updateZoneCenter = (Size / 2) + Origin;
        updateZone.updateZoneSize = Size;
        updateZone.passIndex = GPUSerializerManager.FindPass(this);
        
        RTUpdateZone = updateZone;
    }
    public void Construct() { }
    public void Deconstruct() { }

    public void InitFrame(ref List<byte> channelValues)
    {
        cumulativeOFfset = 0;
    }
    public void CompleteFrame(ref Color32[] pixels, ref List<byte> channelValues, int textureWidth, int textureHeight) { }
    public void SerializeChannel(ref Color32[] pixels, byte channelValue, int channel, int textureWidth, int textureHeight)
    {
        int x = ((channel - cumulativeOFfset) / RowCount) * BlockSize;
        int y = ((channel - cumulativeOFfset) % RowCount) * BlockSize;

        if (mergedChannels.ContainsKey(channel))
        {
            ColorChannel channelType = mergedChannels[channel];
            TextureWriter.MixColorBlock(ref pixels, x, y, channelValue, channelType, BlockSize);
        }
        else
        {
            var color = new Color32(
                channelValue,
                channelValue,
                channelValue,
                Util.GetBlockAlpha(channelValue)
            );
            TextureWriter.MakeColorBlock(ref pixels, x, y, color, BlockSize);
        }

        if (mergedChannels.ContainsKey(channel))
        {
            //if its blue, dont increment the offset
            if (mergedChannels[channel] == ColorChannel.Blue)
            {
                return;
            }
            cumulativeOFfset++;
        }
    }

    public void DeserializeChannel(Texture2D tex, ref byte channelValue, int channel, int textureWidth, int textureHeight) => throw new NotImplementedException();

    public void ConstructUserInterface(RectTransform rect)
    {

    }

    public void DeconstructUserInterface()
    {

    }

    public void UpdateUserInterface()
    {

    }
}
