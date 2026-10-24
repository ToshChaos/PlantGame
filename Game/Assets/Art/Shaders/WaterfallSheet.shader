// Sheet of falling water for waterfalls: streaks that run down (scrolled in time), white foam at the lip, soft sides, fading out far below.
// uv.x across the sheet (0..1), uv.y = 0 at the lip .. 1 at the bottom. Transparent, unlit, two-sided.
Shader "PlantGame/WaterfallSheet"
{
    Properties
    {
        _Light ("Light streaks", Color) = (0.86, 0.95, 1, 1)
        _Deep ("Deep streaks", Color) = (0.435, 0.659, 0.863, 1)
        _Speed ("Fall speed (m/s)", Float) = 2.6
        _Length ("Sheet length (m)", Float) = 14
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Light, _Deep; float _Speed, _Length;
            CBUFFER_END

            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };

            V vert(A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv; o.fog = ComputeFogFactor(o.pos.z); return o; }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float VNoise(float2 p)
            {
                float2 c = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(c), Hash(c + float2(1, 0)), f.x), lerp(Hash(c + float2(0, 1)), Hash(c + float2(1, 1)), f.x), f.y);
            }

            half4 frag(V i) : SV_Target
            {
                float v = i.uv.y * _Length, t = _Time.y;
                float s1 = VNoise(float2(i.uv.x * 16.0, v * 0.3 - t * _Speed));
                float s2 = VNoise(float2(i.uv.x * 33.0 + 5.0, v * 0.55 - t * _Speed * 1.4));
                float streak = s1 * 0.6 + s2 * 0.4;
                half3 col = lerp(_Deep.rgb, _Light.rgb, streak);
                float foam = saturate(1.0 - v * 0.8) * (0.6 + 0.4 * VNoise(float2(i.uv.x * 22.0, t * 2.0)));   // churning white where the water leaves the rim
                col = lerp(col, half3(1, 1, 1), saturate(foam + smoothstep(0.72, 1.0, streak) * 0.4));
                float sides = smoothstep(0.0, 0.14, i.uv.x) * smoothstep(1.0, 0.86, i.uv.x);
                half a = sides * saturate(1.0 - i.uv.y * 0.95) * (0.5 + 0.45 * streak + foam * 0.3);
                col = MixFog(col, i.fog);
                return half4(col, saturate(a));
            }
            ENDHLSL
        }
    }
}
