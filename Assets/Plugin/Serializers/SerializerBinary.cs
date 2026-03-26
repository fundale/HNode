using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Binary : IDMXSerializer
{
    public Vector2 Origin { get; }
    public Vector2 Size { get; }
    public int DataOffset { get; }
    public int DataLength { get; }
    public int Universe { get; }
    public int BlockSize { get; }
    public int RowCount { get; }
    public CustomRenderTextureUpdateZone RTUpdateZone { get; }
    
    public Binary(Vector2 ?origin, Vector2 ?size, int ?dataOffset = 0, int ?dataLength = (512 * 1), int ?universe = 0, int ?blockSize = 4, int ?rowCount = 52)
    {
        Origin = origin ?? Vector2.zero;
        Size = size ?? new Vector2(1920, 208);
        DataOffset = dataOffset ?? 0;
        DataLength = dataLength ?? (512 * 1); // TODO: Change
        Universe = universe ?? 0;
        BlockSize = blockSize ?? 4; // 4x4 pixels per channel block
        RowCount = rowCount ?? 52; // 52 blocks per column, binary

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

        for (int i = 0; i < bits.Length; i++)
        {
            GetPositionData(channel, i, out int x, out int y);
            if (x >= textureWidth || y >= textureHeight)
            {
                continue; // Skip if the calculated pixel is out of bounds
            }
            //convert the x y to pixel index
            //return 4x4 area
            var color = new Color32(
                (byte)(bits[i] ? 255 : 0),
                (byte)(bits[i] ? 255 : 0),
                (byte)(bits[i] ? 255 : 0),
                Util.GetBlockAlpha(channelValue)
            );
            TextureWriter.MakeColorBlock(ref pixels, x, y, color, BlockSize);
        }
    }

    public void DeserializeChannel(Texture2D tex, ref byte channelValue, int channel, int textureWidth, int textureHeight)
    {
        var bits = new BitArray(8);
        for (int i = 0; i < bits.Length; i++)
        {
            GetPositionData(channel, i, out int x, out int y);
            //add on a offset
            x += 1;
            y += 1;
            if (x >= textureWidth || y >= textureHeight)
            {
                continue; // Skip if the calculated pixel is out of bounds
            }
            // Read the 4x4 area and combine it into a single byte
            bits[i] = TextureReader.GetColor(tex, x, y).r > 0.5f;
        }
        // Convert the BitArray back to a byte
        channelValue = ConvertToByte(bits);
    }

    private void GetPositionData(int channel, int i, out int x, out int y)
    {
        int newChannel = (channel * 8) + i;
        x = (newChannel / RowCount) * BlockSize;
        y = (newChannel % RowCount) * BlockSize;
    }

    byte ConvertToByte(BitArray bits)
    {
        if (bits.Count != 8)
        {
            throw new ArgumentException("bits");
        }
        byte[] bytes = new byte[1];
        bits.CopyTo(bytes, 0);
        return bytes[0];
    }

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
