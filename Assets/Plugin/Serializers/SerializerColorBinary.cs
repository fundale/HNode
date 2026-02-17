using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorBinary : IDMXSerializer
{
    public Vector2 Origin { get; }
    public Vector2 Size { get; }
    public int DataOffset { get; }
    public int DataLength { get; }
    public int Universe { get; }
    public int BlockSize { get; }
    public CustomRenderTextureUpdateZone RTUpdateZone { get; }
    const int blocksPerCol = 52; // channels per column
    
    public ColorBinary(Vector2 ?origin, Vector2 ?size, int ?dataOffset = 0, int ?dataLength = (512 * 1), int ?universe = 0, int ?blockSize = 4)
    {
        Origin = origin ?? Vector2.zero;
        Size = size ?? new Vector2(1920, 208);
        DataOffset = dataOffset ?? 0;
        DataLength = dataLength ?? (512 * 1); // TODO: Change
        Universe = universe ?? 0;
        BlockSize = blockSize ?? 4; // 4x4 pixels per channel block

        CustomRenderTextureUpdateZone updateZone = new CustomRenderTextureUpdateZone();
        
        updateZone.updateZoneCenter = (Size / 2) + Origin;
        updateZone.updateZoneSize = Size;
        updateZone.passIndex = GPUSerializerManager.FindPass(this);
        
        RTUpdateZone = updateZone;
    }
    public void Construct() { }
    public void Deconstruct() { }
    public void InitFrame(ref List<byte> channelValues) { }
    public void CompleteFrame(ref Color32[] pixels, ref List<byte> channelValues, int textureWidth, int textureHeight) { }

    public void SerializeChannel(ref Color32[] pixels, byte channelValue, int channel, int textureWidth, int textureHeight)
    {
        //split the value into 8 bits
        var bits = new BitArray(new byte[] { channelValue });
        List<bool> bitsList = new List<bool>();
        for (int i = 0; i < bits.Length; i++)
        {
            bitsList.Add(bits[i]);
        }
        bitsList.Add(false); // Add a dummy bit to make it 9 bits, needed for easy interlacing

        for (int i = 0; i < bitsList.Count; i += 3)
        {
            int newChannel = (channel * 3) + i / 3; //3 because we interlace with color
            int x = (newChannel / blocksPerCol) * BlockSize;
            int y = (newChannel % blocksPerCol) * BlockSize;
            if (x >= textureWidth || y >= textureHeight)
            {
                continue; // Skip if the calculated pixel is out of bounds
            }
            //convert the x y to pixel index
            //return 4x4 area
            var color = new Color32(
                (byte)(bitsList[i] ? 255 : 0),
                (byte)(bitsList[i + 1] ? 255 : 0),
                (byte)(bitsList[i + 2] ? 255 : 0),
                Util.GetBlockAlpha(channelValue)
            );
            TextureWriter.MakeColorBlock(ref pixels, x, y, color, BlockSize);
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
