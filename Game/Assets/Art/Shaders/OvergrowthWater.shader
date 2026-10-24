// T30: stylized water for ponds and streams. Unlit and opaque (overlapping blobs blend seamlessly): a depth gradient from _ShallowColor
// to _DeepColor, two counter-scrolling ripple layers sampled in WORLD space (so neighbouring blobs line up), soft white glints
// where the ripples peak, a cyan grazing-angle rim (art bible: glow-cyan edge) and fog.
Shader "PlantGame/OvergrowthWater"
{
    Properties
    {
        _ShallowColor ("Shallow (sky blue)", Color) = (0.435, 0.659, 0.863, 1)
        _DeepColor ("Deep (deep blue)", Color) = (0.173, 0.369, 0.561, 1)
        _RimColor ("Rim (glow cyan)", Color) = (0.486, 0.949, 0.902, 1)
        _Ripple ("Ripple noise (alpha)", 2D) = "white" {}
        _Tiling ("World tiling (1/m)", Float) = 0.28
        _Speed ("Scroll speed", Float) = 0.05
        _Depth ("Depth 0 light .. 1 dark", Range(0, 1)) = 0.3
        _Glint ("Glint strength", Range(0, 2)) = 0.55
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+10" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Ripple); SAMPLER(sampler_Ripple);
            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor, _DeepColor, _RimColor; float _Tiling, _Speed; half _Depth, _Glint;
            CBUFFER_END

            struct A { float4 pos : POSITION; float3 nrm : NORMAL; };
            struct V { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; float3 wnrm : TEXCOORD1; float fog : TEXCOORD2; };

            V vert(A i)
            {
                V o; o.pos = TransformObjectToHClip(i.pos.xyz);
                o.wpos = TransformObjectToWorld(i.pos.xyz); o.wnrm = TransformObjectToWorldNormal(i.nrm);
                o.fog = ComputeFogFactor(o.pos.z); return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float VNoise(float2 p)
            {
                float2 c = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(c), Hash(c + float2(1, 0)), f.x), lerp(Hash(c + float2(0, 1)), Hash(c + float2(1, 1)), f.x), f.y);
            }

            half4 frag(V i) : SV_Target
            {
                float2 uv = i.wpos.xz * _Tiling;
                float t = _Time.y * _Speed;
                half a = SAMPLE_TEXTURE2D(_Ripple, sampler_Ripple, uv + float2(t, t * 0.6)).a;
                half b = SAMPLE_TEXTURE2D(_Ripple, sampler_Ripple, uv * 1.7 + float2(-t * 1.3, t * 0.4) + 0.31).a;
                half rip = a * 0.6 + b * 0.4;
                // broad drifting patches: lighter sunlit shallows and darker deep pockets inside one lake
                half p1 = VNoise(i.wpos.xz * 0.22 + float2(t * 0.5, -t * 0.3));
                half p2 = VNoise(i.wpos.xz * 0.55 + float2(-t * 0.7, t * 0.4) + 7.3);
                half patch = saturate(((p1 * 0.65 + p2 * 0.35) - 0.5) * 2.2 + 0.5);
                half depth = saturate(_Depth + (patch - 0.5) * 0.85 + (rip - 0.5) * 0.3);
                half3 col = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth);
                half caustic = smoothstep(0.55, 0.9, a) * smoothstep(0.35, 0.8, b);   // bright net lines, strongest in the shallows
                col += caustic * (1.0 - depth) * 0.16 * half3(0.8, 1.0, 0.95);
                col += smoothstep(0.75, 1.0, a) * 0.05;                            // soft crest lines
                half g = smoothstep(0.62, 0.95, a * b);                      // sparse glints where both layers peak
                col += g * _Glint * half3(0.85, 0.95, 1.0);
                float3 v = normalize(GetWorldSpaceViewDir(i.wpos));
                half fres = pow(1.0 - saturate(dot(normalize(i.wnrm), v)), 3.0);
                col = lerp(col, _RimColor.rgb, fres * 0.55);
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
