using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using static TextureWriter;

public class FuralitySomna : IDMXSerializer
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
        int x = ((channel - cumulativeOFfset) / _rowCount) * _blockSize;
        int y = ((channel - cumulativeOFfset) % _rowCount) * _blockSize;

        if (mergedChannels.ContainsKey(channel))
        {
            ColorChannel channelType = mergedChannels[channel];
            TextureWriter.MixColorBlock(ref pixels, x, y, channelValue, channelType, _blockSize);
        }
        else
        {
            var color = new Color32(
                channelValue,
                channelValue,
                channelValue,
                Util.GetBlockAlpha(channelValue)
            );
            TextureWriter.MakeColorBlock(ref pixels, x, y, color, _blockSize);
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
