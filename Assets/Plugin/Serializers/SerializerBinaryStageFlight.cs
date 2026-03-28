using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class BinaryStageFlight : IDMXSerializer
{
    public Vector2 Origin {
        get => _origin;
        set => _origin = value;
    }
    public Vector2 Size {
        get => _size;
        set => _size = value;
    }
    public int DataOffset {
        get => _dataOffset;
        set => _dataOffset = value;
    }
    public int DataLength {
        get => _dataLength;
        set => _dataLength = value;
    }
    public int Universe {
        get => _universe;
        set => _universe = value;
    }
    public int BlockSize {
        get => _blockSize;
        set => _blockSize = value;
    }
    public int RowCount {
        get => _rowCount;
        set => _rowCount = value;
    }
    public CustomRenderTextureUpdateZone RTUpdateZone {
        get => _rtUpdateZone;
        set => _rtUpdateZone = value;
    }

    private Vector2 _origin;
    private Vector2 _size;
    private int _dataOffset;
    private int _dataLength;
    private int _universe;
    private int _blockSize;
    private int _rowCount;
    private CustomRenderTextureUpdateZone _rtUpdateZone;

    const int channelsPerCol = 6;
    const int blocksPerCol = channelsPerCol * 8; // channels per column
    const int CRCBits = 4;
    
    public BinaryStageFlight(Vector2 ?origin, Vector2 ?size, int ?dataOffset = 0, int ?dataLength = (512 * 28), int ?universe = 0, int ?blockSize = 4, int ?rowCount = 52)
    {
        Origin = origin ?? Vector2.zero;
        Size = size ?? new Vector2(1920, 1080);
        DataOffset = dataOffset ?? 0;
        DataLength = dataLength ?? (512 * 28);
        Universe = universe ?? 0;
        BlockSize = blockSize ?? 4; // 4x4 pixels per channel block
        RowCount = rowCount ?? 8 * channelsPerCol + CRCBits; // 52 blocks per column, binary + CRC (8 bits * 6 channels + 4 CRC bits)

        CustomRenderTextureUpdateZone updateZone = new CustomRenderTextureUpdateZone();
        
        updateZone.updateZoneCenter = (Size / 2) + Origin;
        updateZone.updateZoneSize = Size;
        updateZone.passIndex = GPUSerializerManager.FindPass(this);
        
        RTUpdateZone = updateZone;
    }
    public void Construct() { }
    public void Deconstruct() { }
    public void InitFrame(ref List<byte> channelValues) { }
    public void CompleteFrame(ref Color32[] pixels, ref List<byte> channelValues, int textureWidth, int textureHeight)
    {
        //figure out the lowest pixel it wouldve drawn before
        int startY = blocksPerCol * BlockSize;

        //expand channelValues to a multiple of channelsPerCol
        int rounded = (int)(Math.Ceiling(channelValues.Count / (double)channelsPerCol) * channelsPerCol);
        channelValues.EnsureCapacity(rounded);

        //write out all the CRC information
        //the CRC is per collumn of data, so figure out the CRC for every channelsPerCol
        for (int i = 0; i < channelValues.Count; i += channelsPerCol)
        {
            byte[] values = channelValues.GetRange(i, channelsPerCol).ToArray();
            var crc = Crc4(values);

            //calculate the x
            int x = (i / channelsPerCol) * BlockSize;
            //draw the 4 bits
            var bits = new BitArray(new byte[] { crc });
            for (int j = 0; j < /* bits.Length */ CRCBits; j++)
            {
                int y = startY + j * BlockSize;
                CalculateWrapping(x, y, out int xd, out int yd, textureWidth);
                //convert the x y to pixel index
                //return 4x4 area
                var color = new Color32(
                    (byte)(bits[7 - j] ? 255 : 0),
                    (byte)(bits[7 - j] ? 255 : 0),
                    (byte)(bits[7 - j] ? 255 : 0),
                    /* (byte)(bits[j] ? 255 : 0),
                    (byte)(bits[j] ? 255 : 0),
                    (byte)(bits[j] ? 255 : 0), */
                    Util.GetBlockAlpha(255) // Alpha should be forced on always
                );
                TextureWriter.MakeColorBlock(ref pixels, xd, yd, color, BlockSize);
            }
        }
    }

    public void SerializeChannel(ref Color32[] pixels, byte channelValue, int channel, int textureWidth, int textureHeight)
    {
        //split the value into 8 bits
        var bits = new BitArray(new byte[] { channelValue });

        //sane endianness
        for (int i = 0; i < bits.Length; i++)
        {
            GetPositionData(channel, i, textureWidth, out int x, out int y);
            /* if (x >= textureWidth || y >= textureHeight)
            {
                continue; // Skip if the calculated pixel is out of bounds
            } */
            //convert the x y to pixel index
            //return 4x4 area
            byte val = (byte)(bits[i] ? 255 : 0);
            var color = new Color32(
                val,
                val,
                val,
                Util.GetBlockAlpha(channelValue)
            );
            TextureWriter.MakeColorBlock(ref pixels, x, y, color, BlockSize);
        }
    }

    public void DeserializeChannel(Texture2D tex, ref byte channelValue, int channel, int textureWidth, int textureHeight)
    {
        //TODO: CRC Check for transcoding

        var bits = new BitArray(8);
        for (int i = 0; i < bits.Length; i++)
        {
            GetPositionData(channel, i, textureWidth, out int x, out int y);
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

    private void GetPositionData(int channel, int i, int textureWidth, out int x, out int y)
    {
        //int newChannel = (channel * 8) + i;
        //encode backwards, endiannes flip
        int newChannel = (channel * 8) + (7 - i);
        x = (newChannel / blocksPerCol) * BlockSize;
        y = (newChannel % blocksPerCol) * BlockSize;
        CalculateWrapping(x, y, out x, out y, textureWidth);
    }

    private void CalculateWrapping(int x, int y, out int adjx, out int adjy, int textureWidth)
    {
        int wrap = x / textureWidth;
        adjx = x % textureWidth;
        adjy = y + (wrap * (blocksPerCol + CRCBits) * BlockSize); // +4 is for the CRC bits
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

    public static byte Crc4(params byte[] data)
    {
        uint crc = 0u;
        uint polynomial = 0x03;

        foreach (uint v in data)
        {
            for (int bit = 7; bit >= 0; --bit)
            {
                uint inBit = (v >> bit) & 1u;
                bool top = (crc & 0x8u) != 0u;
                crc = ((crc << 1) | inBit) & 0xFu;
                if (top) crc ^= polynomial;
            }
        }
        return (byte)(crc << CRCBits); // put crc on the left and pad 0s
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
