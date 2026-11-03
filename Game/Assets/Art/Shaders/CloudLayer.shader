// T16: soft cloud layer for the cloud sea and the drifting cloud cards. Unlit, transparent, scrolls a tileable fluffy noise texture,
// fades out radially (no hard edge) and tints with the fog colour so distant layers melt into the haze.
Shader "PlantGame/CloudLayer"
{
    Properties
    {
        _MainTex ("Cloud noise (alpha)", 2D) = "white" {}
        _Color ("Tint", Color) = (0.957, 0.973, 0.965, 1)
        _Shade ("Underside shade", Color) = (0.749, 0.851, 0.933, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.8
        _Scroll ("Scroll (xy)", Vector) = (0.01, 0.004, 0, 0)
        _Tiling ("Tiling", Float) = 6
        _Coverage ("Coverage", Range(0, 1)) = 0.45
        _Radius ("Radial fade: inner/outer (uv)", Vector) = (0.25, 0.5, 0, 0)
        _FogAmount ("Fog tint amount", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color, _Shade; half _Opacity; float4 _Scroll; float _Tiling; half _Coverage; float4 _Radius; half _FogAmount;
            CBUFFER_END

            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };

            V vert(A i)
            {
                V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv;
                o.fog = ComputeFogFactor(o.pos.z);
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                float2 uv = i.uv * _Tiling + _Scroll.xy * _Time.y;
                half n = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                half n2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv * 2.3 + 0.37 - _Scroll.xy * _Time.y * 0.6).a;
                half dens = saturate((n * 0.7 + n2 * 0.3 - (1.0 - _Coverage)) / max(0.001, _Coverage));
                float r = distance(i.uv, float2(0.5, 0.5));
                half fade = 1.0 - smoothstep(_Radius.x, _Radius.y, r);
                half3 col = lerp(_Shade.rgb, _Color.rgb, saturate(dens * 1.4));
                col = lerp(col, MixFog(col, i.fog), _FogAmount);
                return half4(col, dens * fade * _Opacity);
            }
            ENDHLSL
        }
    }
}
