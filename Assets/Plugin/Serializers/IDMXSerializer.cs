using System;
using System.Collections.Generic;
using UnityEngine;
using static Util;

[TagMapped]
public interface IDMXSerializer : IUserInterface<IDMXGenerator>, IConstructable
{
    /// <summary>
    /// The beginning of the Serializer on the Canvas. (top left)
    /// </summary>
    public Vector2 Origin { get; }
    /// <summary>
    /// Size of the Serializer.
    /// </summary>
    public Vector2 Size { get; }
    /// <summary>
    /// Beginning of the Serializer data in a Universe.
    /// </summary>
    public int DataOffset { get; }
    /// <summary>
    /// Size of Serilaizer data in the Universe.
    /// </summary>
    public int DataLength { get; }
    /// <summary>
    /// The Universe to start the Serializer on.
    /// </summary>
    public int Universe { get; }
    /// <summary>
    /// The block size of each data byte value or bit.
    /// </summary>
    public int BlockSize { get; }
    /// <summary>
    /// How many rows before wrapping the data.
    /// </summary>
    public int RowCount { get; }
    /// <summary>
    /// The GPU Serializer update info.
    /// </summary>
    public CustomRenderTextureUpdateZone RTUpdateZone { get; }

    /// <summary>
    /// Serializes a channel from a raw byte representation, to a output video stream.
    /// </summary>
    /// <param name="pixels"></param>
    /// <param name="channelValue"></param>
    /// <param name="channel"></param>
    /// <param name="textureWidth"></param>
    /// <param name="textureHeight"></param>
    void SerializeChannel(ref Color32[] pixels, byte channelValue, int channel, int textureWidth, int textureHeight);

    /// <summary>
    /// Deserializes a channel from a input video stream, to a raw byte representation.
    /// </summary>
    /// <param name="tex"></param>
    /// <param name="channelValue"></param>
    /// <param name="channel"></param>
    /// <param name="textureWidth"></param>
    /// <param name="textureHeight"></param>
    void DeserializeChannel(Texture2D tex, ref byte channelValue, int channel, int textureWidth, int textureHeight);

    /// <summary>
    /// Called at the start of each frame to reset any state.
    /// </summary>
    void InitFrame(ref List<byte> channelValues);

    /// <summary>
    /// Called after all channels have been serialized for the current frame.
    /// Can be used to for example generate a CRC block area, or operate on multiple channels at once.
    /// </summary>
    /// <param name="pixels"></param>
    /// <param name="channelValues"></param>
    void CompleteFrame(ref Color32[] pixels, ref List<byte> channelValues, int textureWidth, int textureHeight);
}
