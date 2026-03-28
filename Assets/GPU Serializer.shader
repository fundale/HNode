Shader "HNode/GPU Serializer"
{
    Properties
    {
        // Someday, over the *rainbow*
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Lighting Off
        Blend One Zero

        Pass
        {
            Name "Clear"
            HLSLPROGRAM

            #include "Assets/UnityCustomRenderTexture.cginc"

            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag

            float4 frag(v2f_customrendertexture i) : COLOR
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Binary"
            HLSLPROGRAM

            #include "GPUSerializerCommon.cginc"
            #include "Assets/UnityCustomRenderTexture.cginc"

            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag

            float4 frag(v2f_customrendertexture i) : COLOR
            {
                SERIALIZER_COMMON_HEADER;

                uint vBlocks = serializerRowSize; // 52

                serializerPixel /= serializerBlockSize; // 4

                uint channel = ChannelPixelGrid(serializerPixel, vBlocks) + serializerRange.x;

                uint channelBit = channel % BYTE_BITS;
                channel /= BYTE_BITS;

                CHANNEL_COMMON_META(channel);

                if (serializerPixel.y < vBlocks && channel < (serializerRange.x + serializerRange.y) && renderChannel)
                {
                    serializerColor = ChannelGetBit(channel, channelBit);
                    serializerColor.a = 1.0;
                } else discard;
                    
                return serializerColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "BinaryStageFlight"
            HLSLPROGRAM

            #include "GPUSerializerCommon.cginc"
            #include "Assets/UnityCustomRenderTexture.cginc"

            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag

            float4 frag(v2f_customrendertexture i) : COLOR
            {
                SERIALIZER_COMMON_HEADER;

                uint vBlocks = serializerRowSize; // 52

                serializerPixel /= serializerBlockSize; // 4

                int univRow = serializerPixel.y / vBlocks;

                serializerPixel.y %= vBlocks;

                uint channel = ChannelPixelGrid(serializerPixel, vBlocks) + serializerRange.x;

                channel -= 4 * serializerPixel.x;

                uint channelBit = channel % BYTE_BITS;
                channel /= BYTE_BITS;

                channel += univRow * 6 * (serializerSize.x / serializerBlockSize); // Multi Row

                CHANNEL_COMMON_META(channel);

                if (serializerPixel.y < vBlocks - 4) // Binary data
                {
                    if (channel < (serializerRange.x + serializerRange.y) && renderChannel)
                    {
                        serializerColor = ChannelGetBit(channel, 7 - channelBit);
                        serializerColor.a = 1.0;
                    }
                } else
                if (serializerPixel.y < vBlocks) // CRC generator
                {
                    uint crcBar = (channel / 6) - 1;
                    bool rollForward = (((channel - 6) % 1536) != 0);

                    if (crcBar < ((serializerRange.x + serializerRange.y) / 6) + rollForward) // Roll forward to add extra slack for the end CRC 4 bits
                    {
                        uint crcBitIndex = (serializerPixel.y - 48);
                        uint crcv = 0;

                        uint crcOffset = serializerPixel.x * 6;
                        crcOffset += univRow * 6 * (serializerSize.x / serializerBlockSize); // Muli Row

                        for (int indxCrc = 0; indxCrc < 6; indxCrc ++)
                            CRC4Bit(crcv, ChannelGetByte(indxCrc + crcOffset)); // TODO: Account for channel masking

                        serializerColor = (crcv >> (3 - crcBitIndex)) & 1;
                        serializerColor.a = 1.0;
                    }
                } else discard;
                    
                return serializerColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ColorBinary"
            HLSLPROGRAM

            #include "GPUSerializerCommon.cginc"
            #include "Assets/UnityCustomRenderTexture.cginc"

            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag

            float4 frag(v2f_customrendertexture i) : COLOR
            {
                SERIALIZER_COMMON_HEADER;

                uint vBlocks = serializerRowSize; // 52

                serializerPixel /= serializerBlockSize; // 4

                uint channel = ChannelPixelGrid(serializerPixel, vBlocks) + serializerRange.x;

                uint channelBit = channel % 3;
                channel /= 3;

                CHANNEL_COMMON_META(channel);

                if (serializerPixel.y < vBlocks && channel < (serializerRange.x + serializerRange.y) && (ChannelGetMeta(channel) & 1))
                {
                    uint value = ChannelGetByte(channel) >> (channelBit * 3);

                    serializerColor.r = value & 1;
                    serializerColor.g = (value >> 1) & 1;
                    serializerColor.b = (value >> 2) & 1;

                    serializerColor.a = 1.0;
                } else discard;
                    
                return serializerColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "FuralitySomna"
            HLSLPROGRAM

            #include "GPUSerializerCommon.cginc"
            #include "Assets/UnityCustomRenderTexture.cginc"

            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag

            float4 frag(v2f_customrendertexture i) : COLOR
            {
                SERIALIZER_COMMON_HEADER;

                uint vBlocks = serializerRowSize; // 13

                serializerPixel /= serializerBlockSize; // 16
                uint channel = ChannelPixelGrid(serializerPixel, vBlocks) + serializerRange.x;

                // uint universe = channel / (UNIVERSE_SIZE + BYTE_BITS);
                // bool validBlock = (channel % (UNIVERSE_SIZE + BYTE_BITS)) < UNIVERSE_SIZE;
                // channel -= universe * 8;

                CHANNEL_COMMON_META(channel);
                channel += perChannelOffset;

                if (serializerPixel.y < vBlocks && channel < (serializerRange.x + serializerRange.y) && renderChannel)
                {
                    int colorMode = (channelMeta >> 2) & 0x03;

                    if (colorMode > 0)
                        serializerColor.b = SerializerDataLinear(channel);

                    if (colorMode > 1)
                        serializerColor.g = SerializerDataLinear(channel - 1);

                    if (colorMode > 2)
                        serializerColor.r = SerializerDataLinear(channel - 2);

                    if (colorMode == 0)
                        serializerColor = SerializerDataLinear(channel);
                    
                    serializerColor.a = 1.0;
                } else discard;
                    
                return serializerColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Spiral"
            HLSLPROGRAM

            #include "GPUSerializerCommon.cginc"
            #include "Assets/UnityCustomRenderTexture.cginc"

            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag

            float4 frag(v2f_customrendertexture i) : COLOR
            {
                SERIALIZER_COMMON_HEADER;

                serializerPixel /= serializerBlockSize; // 8

                uint channel = SpiralRingFromGrid(serializerSize, serializerBlockSize, serializerPixel) + serializerRange.x;

                CHANNEL_COMMON_META(channel);

                if (channel < (serializerRange.x + serializerRange.y) && renderChannel)
                {
                    serializerColor = SerializerDataLinear(channel);
                    serializerColor.a = 1.0;
                } else discard;

                return serializerColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Ternary"
            HLSLPROGRAM

            #include "GPUSerializerCommon.cginc"
            #include "Assets/UnityCustomRenderTexture.cginc"

            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag

            float4 frag(v2f_customrendertexture i) : COLOR
            {
                SERIALIZER_COMMON_HEADER;

                uint vBlocks = serializerRowSize; // 8 * 6 = 48

                serializerPixel /= serializerBlockSize; // 4

                uint channel = ChannelPixelGrid(serializerPixel, vBlocks) + serializerRange.x;

                uint channelBit = 5 - (channel % 6);
                channel /= 6;
                
                CHANNEL_COMMON_META(channel);

                if (serializerPixel.y < vBlocks && channel < (serializerRange.x + serializerRange.y) && renderChannel)
                {
                    uint value = ChannelGetByte(channel);
                    
                    serializerColor = ByteToTernary(value, channelBit) / 2.0;

                    serializerColor.a = 1.0;
                } else discard;
                    
                return serializerColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "VRSL"
            HLSLPROGRAM

            #include "GPUSerializerCommon.cginc"
            #include "Assets/UnityCustomRenderTexture.cginc"

            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag

            float4 frag(v2f_customrendertexture i) : COLOR
            {
                SERIALIZER_COMMON_HEADER;

                uint vBlocks = serializerRowSize; // 13

                serializerPixel /= serializerBlockSize; // 16
                uint channel = ChannelPixelGrid(serializerPixel, vBlocks) + serializerRange.x;

                // VRSL Gaps
                uint universe = channel / (UNIVERSE_SIZE + BYTE_BITS);
                bool validBlock = (channel % (UNIVERSE_SIZE + BYTE_BITS)) < UNIVERSE_SIZE;
                channel -= universe * 8;

                CHANNEL_COMMON_META(channel);

                if (serializerPixel.y < vBlocks && channel < (serializerRange.x + serializerRange.y) && validBlock && renderChannel)
                {
                    uint vrslFlags = SerializerMode(serializerSize) & 0x03;

                    serializerColor = ChannelGetByte(channel);

                    if ((vrslFlags >> 1) & 0x01) // 9 universe mode RGB packing
                    {
                        serializerColor.g = ChannelGetByte(channel + (512 * 3));
                        serializerColor.b = ChannelGetByte(channel + (512 * 3 * 2));
                    }

                    if (vrslFlags & 0x01) // Linear to sRGB legacy support
                    {
                        serializerColor.r = LinearToSRGB(serializerColor.r);
                        serializerColor.g = LinearToSRGB(serializerColor.g);
                        serializerColor.b = LinearToSRGB(serializerColor.b);
                    } else serializerColor /= 255.0;
                    
                    serializerColor.a = 1.0;
                } else discard;
                    
                return serializerColor;
            }
            ENDHLSL
        }
    }
}
