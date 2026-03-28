using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Util;

public class VRSL : IDMXSerializer
{
    public enum OutputConfigs
    {
        HorizontalTop,
        VerticalLeft,
        VerticalRight,
        HorizontalBottom,
    }
    
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

    public bool GammaCorrection
    {
        get => _gammaCorrection;
        set {
            _gammaCorrection = value;

            Loader.gpuSerializerManager.FullUpdate();
        }
    }
    public bool RGBGridMode
    {
        get => _rgbGridMode;
        set {
            _rgbGridMode = value;

            Loader.gpuSerializerManager.FullUpdate();
        }
    }
    public OutputConfigs outputConfig
    {
        get => _outputConfig;
        set {
            _outputConfig = value;

            bool vertical = (_outputConfig == OutputConfigs.VerticalLeft) || (_outputConfig == OutputConfigs.VerticalRight);
            _rtUpdateZone.rotation = vertical ? 270f : 0f;

            Loader.gpuSerializerManager.FullUpdate();
        }
    }

    private Vector2 _origin;
    private Vector2 _size;
    private int _dataOffset;
    private int _dataLength;
    private int _universe;
    private int _blockSize;
    private int _rowCount;
    private CustomRenderTextureUpdateZone _rtUpdateZone;

    private bool _gammaCorrection;
    private bool _rgbGridMode;
    private OutputConfigs _outputConfig;
    
    public VRSL(Vector2 ?origin, Vector2 ?size, int ?dataOffset = 0, int ?dataLength = (512 * 3), int ?universe = 0, int ?blockSize = 16, int ?rowCount = 13)
    {   
        Origin = origin ?? Vector2.zero;
        Size = size ?? new Vector2(1920, 208);
        DataOffset = dataOffset ?? 0;
        DataLength = dataLength ?? (512 * 3);
        Universe = universe ?? 0;
        BlockSize = blockSize ?? 16; // 16x16 pixels per channel block
        RowCount = rowCount ?? 13; // 13 blocks per column, linear, VRSL spec

        _gammaCorrection = true;
        _rgbGridMode = false;
        _outputConfig = OutputConfigs.HorizontalTop;

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
        GetPositionData(channel, out int x, out int y, out int universeOffset);

        //if vertical, flip
        switch (outputConfig)
        {
            case OutputConfigs.HorizontalTop:
                x += universeOffset;
                break;
            case OutputConfigs.HorizontalBottom:
                x += universeOffset;
                y += textureHeight - (RowCount * BlockSize); // Shift down for horizontal bottom layout
                break;
            case OutputConfigs.VerticalLeft:
                //swap x and y
                int temp = x;
                x = y;
                y = temp;
                y += universeOffset;
                //flip Y coordinate
                y = textureHeight - y - BlockSize; // Flip Y coordinate for vertical layout
                break;
            case OutputConfigs.VerticalRight:
                //swap x and y
                temp = x;
                x = y;
                y = temp;
                y += universeOffset;
                //flip Y coordinate
                y = textureHeight - y - BlockSize; // Flip Y coordinate for vertical layout
                x += textureWidth - (RowCount * BlockSize); // Shift to the right for vertical right layout
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outputConfig), outputConfig, null);
        }


        //convert the x y to pixel index
        //return 4x4 area
        var color = new Color(
            channelValue / 255f,
            channelValue / 255f,
            channelValue / 255f,
            Util.GetBlockAlpha(channelValue)
        );
        if (GammaCorrection) { color = color.linear; } //lol WTF VRSL, you output in a converted color space instead of native linear???????
        if (RGBGridMode)
        {
            byte value = 0;
            TextureWriter.ColorChannel cchannel;
            switch (GetUniverseWrap(channel))
            {
                case 0:
                    value = ((Color32)color).r;
                    cchannel = TextureWriter.ColorChannel.Red;
                    break;
                case 1:
                    value = ((Color32)color).g;
                    cchannel = TextureWriter.ColorChannel.Green;
                    break;
                case 2:
                    value = ((Color32)color).b;
                    cchannel = TextureWriter.ColorChannel.Blue;
                    break;
                default:
                    return;
            }
            TextureWriter.MixColorBlock(ref pixels, x, y, value, cchannel, BlockSize);
        }
        else
        {
            TextureWriter.MakeColorBlock(ref pixels, x, y, color, BlockSize);
        }
    }

    public void DeserializeChannel(Texture2D tex, ref byte channelValue, int channel, int textureWidth, int textureHeight)
    {
        GetPositionData(channel, out int x, out int y, out int universeOffset);

        //add a half offset to get the center
        x += BlockSize / 2;
        y += BlockSize / 2;

        // Get the color block from the texture
        Color color = TextureReader.GetColor(tex, x + universeOffset, y);
        if (GammaCorrection) { color = color.gamma; } //TODO: test this NEEDS MORE TESTING, seems like this actually should be off by default?????

        // Convert the color block to a channel value
        if (RGBGridMode)
        {
            switch (GetUniverseWrap(channel))
            {
                case 0:
                    channelValue = ((Color32)color).r;
                    break;
                case 1:
                    channelValue = ((Color32)color).g;
                    break;
                case 2:
                    channelValue = ((Color32)color).b;
                    break;
                default:
                    return; // Not a valid RGB channel
            }
        }
        else
        {
            channelValue = ((Color32)color).g;
        }
    }

    private void GetPositionData(int channel, out int x, out int y, out int universeOffset)
    {
        int universe = channel / 512; // Assuming 512 channels per universe
        int channelInUniverse = channel % 512; // Channel within the universe

        //if rgb grid, make every 3 universes appear as the first 3
        if (RGBGridMode)
        {
            universe = universe % 3; // Limit to 3 channels for RGB grid
        }

        x = (channelInUniverse / RowCount) * BlockSize;
        y = (channelInUniverse % RowCount) * BlockSize;

        //stupid universe bullshit in VRSL
        universeOffset = universe * (512 / RowCount * BlockSize) + (universe * BlockSize);
    }
    
    /// <summary>
    /// Returns what current universe set of 3 this channel is in
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    private int GetUniverseWrap(int channel)
    {
        int universe = channel / 512; // Assuming 512 channels per universe
        return universe / 3; // Return the universe set of 3 (0, 1, or 2)
    }

    public void ConstructUserInterface(RectTransform rect)
    {
        AddToggle(rect, "Gamma Correction")
            .WithValue(GammaCorrection)
            .WithCallback((isOn) =>
            {
                GammaCorrection = isOn;
            });

        AddToggle(rect, "RGB Grid Mode")
            .WithValue(RGBGridMode)
            .WithCallback((isOn) =>
            {
                RGBGridMode = isOn;
            });

        var text = AddText(rect, "Output Config: " + outputConfig.ToString());

        //do a button to cycle through the output configs
        AddButton(rect, "Cycle Output Config")
            .WithCallback(() =>
            {
                outputConfig = (OutputConfigs)(((int)outputConfig + 1) % Enum.GetNames(typeof(OutputConfigs)).Length);
                text.text = "Output Config: " + outputConfig.ToString();
            });
    }

    public void DeconstructUserInterface()
    {

    }

    public void UpdateUserInterface()
    {

    }
}
