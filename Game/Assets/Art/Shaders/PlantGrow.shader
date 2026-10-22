// T01: shared plant-growth shader (URP). Used by material M_PlantGrow.
//
// Contract (AGENTS.md 5.3): vertex colour R = growth_t (0 at the base -> 1 at the tip).
//   * Geometry with growth_t >  _Growth is clipped away (hidden).
//   * Within _TaperLength of the growth front the surface is pulled inward along its normals,
//     so the growing tip scales in as a tapered point instead of a hard cut.
//   * A thin glowing band marks the front while growing.
//   * Two-tone gradient (_BaseColor -> _TipColor) driven by growth_t, soft toon lighting, rim light.
// Hand-written HLSL instead of Shader Graph so it is reviewable as text and diffs cleanly.
// Per-instance growth: set _Growth with a MaterialPropertyBlock (see GrowSweep.cs) or on a material instance.
Shader "PlantGame/PlantGrow"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color (leaf green)", Color) = (0.365, 0.706, 0.420, 1)
        _TipColor ("Tip Color (cloud white)", Color) = (0.957, 0.973, 0.965, 1)
        _ShadeColor ("Shade Color (moss)", Color) = (0.184, 0.490, 0.310, 1)
        _RimColor ("Rim Color (glow cyan)", Color) = (0.486, 0.949, 0.902, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.6
        _GradientStart ("Gradient Start (growth_t)", Range(0, 1)) = 0.35
        _GradientEnd ("Gradient End (growth_t)", Range(0, 1)) = 1.0
        _Growth ("Growth", Range(0, 1)) = 1
        _TaperLength ("Taper Length (growth_t)", Range(0.001, 0.5)) = 0.08
        _TaperAmount ("Taper Amount (m)", Range(0, 1)) = 0.15
        _FrontGlowWidth ("Front Glow Width (growth_t)", Range(0, 0.1)) = 0.02
        _FrontGlowStrength ("Front Glow Strength", Range(0, 4)) = 1.5
        _ShadeSoftness ("Toon Softness", Range(0.01, 0.5)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _TipColor;
            float4 _ShadeColor;
            float4 _RimColor;
            float _RimPower;
            float _RimStrength;
            float _GradientStart;
            float _GradientEnd;
            float _Growth;
            float _TaperLength;
            float _TaperAmount;
            float _FrontGlowWidth;
            float _FrontGlowStrength;
            float _ShadeSoftness;
        CBUFFER_END

        // Tapered tip: inset along the normal near the growth front. t = 1 well behind the front, 0 at the front.
        float3 GrowOffsetOS(float3 positionOS, float3 normalOS, float growthT)
        {
            float t = saturate((_Growth - growthT) / _TaperLength);
            // The taper fades out over the last 8% of growth so a finished plant keeps its full-width tip.
            float fade = 1.0 - smoothstep(0.92, 1.0, _Growth);
            return positionOS - normalOS * (_TaperAmount * (1.0 - t) * fade);
        }

        // Returns > 0 when the fragment must be discarded.
        float GrowClip(float growthT)
        {
            if (_Growth <= 0.0001) return 1.0;
            if (_Growth >= 0.9999) return 0.0;
            return growthT > _Growth ? 1.0 : 0.0;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  growthT    : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float growthT = IN.color.r;
                float3 posOS = GrowOffsetOS(IN.positionOS.xyz, IN.normalOS, growthT);
                VertexPositionInputs vp = GetVertexPositionInputs(posOS);
                VertexNormalInputs vn = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS = vn.normalWS;
                OUT.growthT = growthT;
                OUT.fogFactor = ComputeFogFactor(vp.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                clip(-GrowClip(IN.growthT));

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // Soft toon ramp: half-Lambert squeezed through a smoothstep.
                float ndl = dot(N, mainLight.direction) * 0.5 + 0.5;
                float ramp = smoothstep(0.5 - _ShadeSoftness, 0.5 + _ShadeSoftness, ndl) * mainLight.shadowAttenuation;

                float h = smoothstep(_GradientStart, max(_GradientEnd, _GradientStart + 0.001), IN.growthT);
                float3 albedo = lerp(_BaseColor.rgb, _TipColor.rgb, h);

                float3 lit = lerp(albedo * _ShadeColor.rgb * 1.2, albedo, ramp) * mainLight.color;
                float3 ambient = SampleSH(N) * albedo;
                float3 color = lit * 0.9 + ambient * 0.6;

                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower) * _RimStrength;
                color += _RimColor.rgb * rim;

                // Glowing growth front (only while growing).
                float inProgress = step(0.0001, _Growth) * step(_Growth, 0.9999);
                float front = saturate(1.0 - (_Growth - IN.growthT) / max(_FrontGlowWidth, 0.0001));
                color += _RimColor.rgb * front * _FrontGlowStrength * inProgress;

                color = MixFog(color, IN.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float growthT : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float growthT = IN.color.r;
                float3 posOS = GrowOffsetOS(IN.positionOS.xyz, IN.normalOS, growthT);
                float3 positionWS = TransformObjectToWorld(posOS);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                OUT.positionCS = positionCS;
                OUT.growthT = growthT;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                clip(-GrowClip(IN.growthT));
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float growthT : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float growthT = IN.color.r;
                float3 posOS = GrowOffsetOS(IN.positionOS.xyz, IN.normalOS, growthT);
                OUT.positionCS = TransformObjectToHClip(posOS);
                OUT.growthT = growthT;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                clip(-GrowClip(IN.growthT));
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
