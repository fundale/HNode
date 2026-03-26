#ifndef GPU_SERIALIZER_COMMON
#define GPU_SERIALIZER_COMMON

#define UNIVERSE_SIZE 512
#define BYTE_BITS 8
#define MAX_SERIALIZERS 32

Texture2D<float> _serializerData;
Texture2D<float> _serializerMeta;
Texture2D<float> _sRGB_LUT;

#define LinearToSRGB(lin) (_sRGB_LUT[uint2((lin), 0)])

uniform float4 _SerializerSizes[MAX_SERIALIZERS]; // Width, Height, Block Size + Row Count, Orientation + Mode
uniform float2 _SerializerRanges[MAX_SERIALIZERS]; // Offset, Length, null, null

#define SerializerSize(i) uint4(_SerializerSizes[i.primitiveID])
#define SerializerRange(i) uint2(_SerializerRanges[i.primitiveID])
#define SerializerPixel(i) floor(SerializerSize(i).xy * i.localTexcoord.xy)

// YES THIS NEEDS THE DOUBLE ((channel)) PARENTHESIS, OR THE COMPILER F'S UP
#define SerializerData(channel, universe) _serializerData[uint2((channel), (universe))]
#define SerializerDataLinear(channel) SerializerData((channel) % UNIVERSE_SIZE, (channel) / UNIVERSE_SIZE)

#define SerializerMode(zoneData) (zoneData.w >> 2)
#define SerializerOrientation(zoneData) (zoneData.w & 0x03)

#define SERIALIZER_COMMON_HEADER \
    uint4 serializerSize = SerializerSize(i); \
    SerializerUVFlip(i.localTexcoord.xy, SerializerOrientation(serializerSize)); \
    float4 serializerColor = 0.0; \
    uint2 serializerPixel = SerializerPixel(i); \
    uint2 serializerRange = SerializerRange(i).xy; \
    uint serializerBlockSize = ((serializerSize.z & 0x3f) + 1); \
    uint serializerRowSize = (((serializerSize.z >> 6) & 0x03ff) + 1);

#define CHANNEL_COMMON_META(channel) \
    uint channelMeta = ChannelGetMeta(channel); \
    bool renderChannel = (channelMeta) & 0x01; \
    int perChannelOffset = ((channelMeta) >> 4) * (((channelMeta >> 1) & 0x01) ? -1 : 1);

#define ChannelPixelGrid(pixel, rows) (pixel.x * rows + pixel.y)
#define ChannelGetByte(channel) (uint(SerializerDataLinear((channel)) * 0xff))
#define ChannelGetBit(channel, bit) ((ChannelGetByte(channel) >> bit) & 0x01)

#define ChannelGetMeta(channel) (uint(_serializerMeta[uint2((channel) % UNIVERSE_SIZE, (channel) / UNIVERSE_SIZE)] * 0xffff))

inline void SerializerUVFlip(inout float2 uv, uint orient)
{
    if (!(orient & 0x01))
        uv.y = 1.0 - uv.y;

    if ((orient >> 1) & 0x01)
        uv.x = 1.0 - uv.x;
}

// CRC4Bit adapted from "public static byte Crc4(params byte[] data)" from "SerializerBinaryStageFlight.cs"
void CRC4Bit(inout uint crc, uint bytev)
{
    uint polynomial = 0x03;

    [unroll] // This for loop HAS to be [unroll]'d otherwise it will cause a GPU timeout due to the "if (top)" check
    for (int bit = 7; bit >= 0; --bit)
    {
        uint inBit = (bytev >> bit) & 1u;
        bool top = (crc & 0x8u) != 0u;
        crc = ((crc << 1) | inBit) & 0xFu;
        if (top) crc ^= polynomial;
    }
}

inline uint SpiralRingIndex(uint2 size, uint2 pixel)
{
    size /= 2;
    int2 centeredPixel = pixel - size;

    centeredPixel.x += centeredPixel.x > -1;
    
    uint2 distFromCenter = size - abs(centeredPixel);
    return min(distFromCenter.x, distFromCenter.y);
}

inline uint SpiralRingLength(uint2 size, uint ring)
{
    return ((size.x + size.y) * 2 * ring) - (4 * ring * ring);
}

uint SpiralRingFromGrid(uint2 size, uint blockSize, uint2 pixel)
{
    size /= blockSize;

    uint ringIndex = SpiralRingIndex(size, pixel);

    uint2 ringPos = pixel - ringIndex;
    uint2 ringSize = size - (ringIndex * 2) - 1;

    uint ringOffset = SpiralRingLength(size, ringIndex);

    if (ringPos.y == 0) // top
    {
        return ringPos.x + ringOffset;
    } else if (ringPos.x == ringSize.x) // right
    {
        return (ringPos.y + ringOffset) + ringSize.x;
    } else if (ringPos.y == ringSize.y) // bottom
    {
        return ((ringSize.x - (ringPos.x)) + ringOffset) + (ringSize.x + ringSize.y);
    } else if (ringPos.x == 0) // left
    {
        return ((ringSize.y - (ringPos.y)) + ringOffset) + ((ringSize.x * 2 + ringSize.y));
    }

    return 0;
}

uint ByteToTernary(uint byte, uint index)
{
    for (int indx = 0; indx < index; indx++)
        byte /= 3;
    
    return byte % 3;
}

#endif