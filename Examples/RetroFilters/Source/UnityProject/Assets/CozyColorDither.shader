Shader "CozyIsland/Color Dither"
{
    Properties
    {
        _Strength ("Strength", Range(0, 1)) = 0.57
        _ColorSteps ("Color Steps", Range(8, 64)) = 8
        _GrainSize ("Grain Size (Pixels)", Range(1, 4)) = 2
        _Chroma ("Color Flecks", Range(0, 1)) = 0.52
        _RenderLines ("Pixel Height (0 = Off)", Float) = 0
        _PosterizeBands ("Shade Bands (0 = Off)", Float) = 0
        _Outline ("Outline Strength", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Color Dither"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Strength;
                float _ColorSteps;
                float _GrainSize;
                float _Chroma;
                float _RenderLines;
                float _PosterizeBands;
                float _Outline;
            CBUFFER_END

            float Grain(float2 pixel)
            {
                return frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
            }

            float EyeDepth(float2 uv)
            {
                float raw = SampleSceneDepth(saturate(uv));
                if (unity_OrthoParams.w < 0.5) return LinearEyeDepth(raw, _ZBufferParams);
                #if UNITY_REVERSED_Z
                    raw = 1.0 - raw;
                #endif
                return lerp(_ProjectionParams.y, _ProjectionParams.z, raw);
            }

            float3 Dither(float3 source, float2 position)
            {
                float3 displayColor = saturate(source);
                #ifndef UNITY_COLORSPACE_GAMMA
                    displayColor = LinearToSRGB(displayColor);
                #endif
                float2 pixel = floor(position / round(_GrainSize));
                float commonNoise = Grain(pixel);
                float3 threshold = lerp(commonNoise.xxx, float3(Grain(pixel + float2(19, 7)),
                    Grain(pixel + float2(5, 23)), Grain(pixel + float2(31, 11))), 0.4);
                float steps = round(_ColorSteps);
                float3 quantized = floor(displayColor * steps + threshold) / steps;
                float lightness = dot(displayColor, float3(0.2126, 0.7152, 0.0722));
                float fleck = Grain(pixel + float2(41, 29));
                float3 warm = float3(0.105, -0.135, -0.05);
                float3 cool = float3(-0.08, 0.0, 0.035);
                float3 tint = warm * (step(fleck, 0.14) - 0.14)
                    + cool * (step(0.9, fleck) - 0.1);
                quantized = saturate(quantized + tint * _Chroma);
                float weight = _Strength * smoothstep(0.025, 0.18, lightness)
                    * (1.0 - smoothstep(0.86, 1.0, lightness));
                #ifndef UNITY_COLORSPACE_GAMMA
                    quantized = SRGBToLinear(quantized);
                #endif

                float3 inRange = step(0.0, source) * step(source, 1.0);
                return lerp(source, quantized, weight * inRange);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 resolution = GetScaledScreenParams().xy;
                float2 cell = 1.0 / resolution;
                if (_RenderLines > 0)
                {
                    float height = min(_RenderLines, resolution.y);
                    float2 grid = round(float2(height * resolution.x / resolution.y, height));
                    cell = 1.0 / grid;
                    uv = (floor(uv * grid) + 0.5) * cell;
                }

                float4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
                float3 color = source.rgb;

                if (_PosterizeBands > 0)
                {
                    float3 shade = saturate(color);
                    #ifndef UNITY_COLORSPACE_GAMMA
                        shade = LinearToSRGB(shade);
                    #endif
                    float lightness = dot(shade, float3(0.2126, 0.7152, 0.0722));
                    float intervals = _PosterizeBands - 1;
                    float band = round(lightness * intervals) / intervals;
                    shade = saturate(shade * band / max(lightness, 0.001));
                    #ifndef UNITY_COLORSPACE_GAMMA
                        shade = SRGBToLinear(shade);
                    #endif
                    color = shade;
                }

                if (_Outline > 0)
                {
                    float depth = EyeDepth(uv);
                    float2 dx = float2(cell.x, 0);
                    float2 dy = float2(0, cell.y);
                    float neighbor = max(max(EyeDepth(uv + dx), EyeDepth(uv - dx)),
                        max(EyeDepth(uv + dy), EyeDepth(uv - dy)));
                    float edge = smoothstep(0.012, 0.055, (neighbor - depth) / max(depth, 1.0));
                    color *= 1.0 - edge * _Outline * 0.85;
                }

                if (_Strength > 0) color = Dither(color, input.positionCS.xy);
                return float4(color, source.a);
            }
            ENDHLSL
        }
    }
}
