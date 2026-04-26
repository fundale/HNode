using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spiral : IDMXSerializer
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
        get;
        set;
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
    private CustomRenderTextureUpdateZone _rtUpdateZone;

    int x = 0;
    int y = 0;
    int state = 0;
    List<Vector2Int> visited = new List<Vector2Int>();

    public Spiral(Vector2 ?origin, Vector2 ?size, int ?dataOffset = 0, int ?dataLength = (512 * 32), int ?universe = 0, int ?blockSize = 8)
    {
        Origin = origin ?? Vector2.zero;
        Size = size ?? new Vector2(1920, 1080);
        DataOffset = dataOffset ?? 0;
        DataLength = dataLength ?? (512 * 32); // TODO: Change
        Universe = universe ?? 0;
        BlockSize = blockSize ?? 8; // 8x8 pixels per channel block
        RowCount = 0; // Unsupported due to layout, linear

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
        x = 0;
        y = 0;
        visited.Clear();
        state = 0;
    }
    public void CompleteFrame(ref Color32[] pixels, ref List<byte> channelValues, int textureWidth, int textureHeight) { }

    public void SerializeChannel(ref Color32[] pixels, byte channelValue, int channel, int textureWidth, int textureHeight)
    {
        int scaledWidth = textureWidth / _blockSize;
        int scaledHeight = textureHeight / _blockSize;


        //multiply up by block size
        int xfinal = x * _blockSize;
        int yfinal = y * _blockSize;

        //convert the x y to pixel index
        //return 4x4 area
        var color = new Color32(
            channelValue,
            channelValue,
            channelValue,
            Util.GetBlockAlpha(channelValue)
        );
        TextureWriter.MakeColorBlock(ref pixels, xfinal, yfinal, color, _blockSize);

        int nextX = x;
        int nextY = y;
        CalculateNextMove(ref nextX, ref nextY);

        //self collision check
        if (visited.Contains(new Vector2Int(nextX, nextY)) ||
            nextX < 0 || nextY < 0 ||
            nextX >= scaledWidth || nextY >= scaledHeight)
        {
            //we've hit a wall, change direction
            state++;
            if (state > 3) state = 0;
            CalculateNextMove(ref nextX, ref nextY);
        }

        visited.Add(new Vector2Int(x, y));

        x = nextX;
        y = nextY;
    }

    private void CalculateNextMove(ref int nextX, ref int nextY)
    {
        nextX = x;
        nextY = y;
        //do thing based on state
        switch (state)
        {
            case 0:
                nextX = x + 1;
                break;
            case 1:
                nextY = y + 1;
                break;
            case 2:
                nextX = x - 1;
                break;
            case 3:
                nextY = y - 1;
                break;
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
