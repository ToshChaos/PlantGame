// T16: procedural gradient skybox in the art-bible palette. Horizon cloud white -> sky blue -> deep blue zenith, a sun disc with a
// soft glow around the main light direction, and a faint haze band at the horizon that matches the fog colour.
Shader "PlantGame/OvergrowthSky"
{
    Properties
    {
        _HorizonColor ("Horizon (pale blue)", Color) = (0.70, 0.86, 0.97, 1)
        _MidColor ("Mid (bright blue)", Color) = (0.42, 0.69, 0.95, 1)
        _SkyColor ("Sky (saturated blue)", Color) = (0.18, 0.50, 0.91, 1)
        _ZenithColor ("Zenith (deep blue)", Color) = (0.08, 0.28, 0.70, 1)
        _GroundColor ("Below horizon (mist blue)", Color) = (0.749, 0.851, 0.933, 1)
        _DeepColor ("Far below (deep abyss blue)", Color) = (0.07, 0.14, 0.27, 1)
        _SunColor ("Sun", Color) = (1, 0.96, 0.82, 1)
        _SunSize ("Sun size", Range(0.001, 0.2)) = 0.035
        _SunGlow ("Sun glow", Range(0, 2)) = 0.6
        _Exponent ("Gradient exponent", Range(0.2, 3)) = 0.75
    }
    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half4 _HorizonColor, _MidColor, _SkyColor, _ZenithColor, _GroundColor, _DeepColor, _SunColor;
            half _SunSize, _SunGlow, _Exponent;

            struct A { float4 pos : POSITION; };
            struct V { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            V vert(A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.dir = i.pos.xyz; return o; }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float VNoise(float2 p)
            {
                float2 c = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(c), Hash(c + float2(1, 0)), f.x), lerp(Hash(c + float2(0, 1)), Hash(c + float2(1, 1)), f.x), f.y);
            }
            float Fbm(float2 p) { return VNoise(p) * 0.5 + VNoise(p * 2.03 + 3.1) * 0.25 + VNoise(p * 4.1 + 7.7) * 0.125 + VNoise(p * 8.3 + 1.9) * 0.0625; }

            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                half3 col;
                if (h >= 0)
                {
                    float t = pow(saturate(h), _Exponent);
                    col = lerp(_HorizonColor.rgb, _MidColor.rgb, saturate(t * 3.0));
                    col = lerp(col, _SkyColor.rgb, saturate((t - 0.2) * 2.5));
                    col = lerp(col, _ZenithColor.rgb, saturate((t - 0.55) * 2.2));
                }
                else
                {
                    col = lerp(_HorizonColor.rgb, _GroundColor.rgb, saturate(-h * 6.0));
                    col = lerp(col, _DeepColor.rgb, saturate((-h - 0.12) * 1.8));   // looking down: the sea of cloud gives way to dark depth
                }
                if (h > 0.02)
                {   // puffy clouds on a dome projected to the sky: lit white tops, soft blue-grey undersides, fading into the horizon haze
                    float2 cp = d.xz / (h + 0.45) * 2.2 + float2(_Time.y * 0.006, _Time.y * 0.002);
                    half dens = smoothstep(0.56, 0.80, Fbm(cp));
                    half top = smoothstep(0.56, 0.80, Fbm(cp + float2(0.05, 0.08)));   // offset sample: where the density rises toward the light
                    half3 cc = lerp(half3(0.72, 0.80, 0.92), half3(1.0, 1.0, 1.0), saturate(1.0 - (dens - top) * 3.0));
                    col = lerp(col, cc, dens * smoothstep(0.05, 0.3, h) * 0.95);
                }
                float3 sun = normalize(_MainLightPosition.xyz);
                float c = dot(d, sun);
                float disc = smoothstep(1.0 - _SunSize * 0.02, 1.0 - _SunSize * 0.012, c);
                float glow = pow(saturate(c), 24.0) * _SunGlow + pow(saturate(c), 6.0) * 0.12;
                col += _SunColor.rgb * (disc * 2.0 + glow);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
