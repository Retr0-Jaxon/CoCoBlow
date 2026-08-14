Shader "CoCoBlow/PSX Fullscreen"
{
    Properties
    {
        _InternalResolution ("Internal Resolution", Vector) = (320, 180, 0, 0)
        _ColorBits ("Color Bits Per Channel", Range(2, 8)) = 5
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.35
        _ChromaShift ("Chroma Shift (Pixels)", Range(0, 3)) = 1
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.08
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.16
        _JitterStrength ("Frame Jitter (Pixels)", Range(0, 2)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "PSXFullscreen"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _InternalResolution;
                float _ColorBits;
                float _DitherStrength;
                float _ChromaShift;
                float _ScanlineStrength;
                float _VignetteStrength;
                float _JitterStrength;
            CBUFFER_END

            float Bayer4x4(float2 pixel)
            {
                float x = fmod(pixel.x, 4.0);
                float y = fmod(pixel.y, 4.0);

                if (y < 1.0)
                {
                    return x < 1.0 ? 0.0 : x < 2.0 ? 8.0 : x < 3.0 ? 2.0 : 10.0;
                }

                if (y < 2.0)
                {
                    return x < 1.0 ? 12.0 : x < 2.0 ? 4.0 : x < 3.0 ? 14.0 : 6.0;
                }

                if (y < 3.0)
                {
                    return x < 1.0 ? 3.0 : x < 2.0 ? 11.0 : x < 3.0 ? 1.0 : 9.0;
                }

                return x < 1.0 ? 15.0 : x < 2.0 ? 7.0 : x < 3.0 ? 13.0 : 5.0;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float2 resolution = max(_InternalResolution.xy, 1.0);
                float frame = floor(_Time.y * 30.0);
                float2 frameJitter = float2(frac(sin(frame * 12.9898) * 43758.5453), frac(sin(frame * 78.233) * 43758.5453));
                frameJitter = (frameJitter - 0.5) * (_JitterStrength / resolution);

                float2 pixelUv = (floor((input.texcoord + frameJitter) * resolution) + 0.5) / resolution;
                float2 chromaOffset = float2(_ChromaShift / resolution.x, 0.0);

                float red = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pixelUv + chromaOffset).r;
                float green = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pixelUv).g;
                float blue = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pixelUv - chromaOffset).b;
                float3 color = saturate(float3(red, green, blue));

                float levels = exp2(round(_ColorBits)) - 1.0;
                float dither = (Bayer4x4(floor(pixelUv * resolution)) / 15.0 - 0.5) * (_DitherStrength / levels);
                color = floor(saturate(color + dither) * levels + 0.5) / levels;

                float scanline = 1.0 - (fmod(floor(pixelUv.y * resolution.y), 2.0) * _ScanlineStrength);
                float2 centeredUv = input.texcoord * 2.0 - 1.0;
                float vignette = 1.0 - saturate(dot(centeredUv, centeredUv) * _VignetteStrength);
                return float4(color * scanline * vignette, 1.0);
            }
            ENDHLSL
        }
    }
}
