// T01: shared plant-growth shader (URP). Used by material M_PlantGrow.
//
// Contract (AGENTS.md 5.3): vertex colour R = growth_t (0 at the base -> 1 at the tip), G = part id, B = sway weight (0 at a leaf's or
// petal's root -> 1 at its tip, 0 on everything that must stay on its colliders), A = flower opening (1 = not a flower; below 1 = the
// growth_t of the flower's centre, which UV0.xy + UV1.x hold in object space: bridges only).
//   * Geometry with growth_t >  _Growth is clipped away (hidden).
//   * Within _TaperLength of the growth front the surface is pulled inward along its normals,
//     so the growing tip scales in as a tapered point instead of a hard cut.
//   * A thin glowing band marks the front while growing.
//   * Two-tone gradient (_BaseColor -> _TipColor) driven by growth_t, soft toon lighting, rim light.
//   * With part ids on (_PartColors): leaves and petals sway a few centimetres at the tip, scaled by the sway weight (_Sway; decks, vines,
//     steps, caps and bowls have weight 0, so the look stays on its colliders), and the glowing flower centres pulse (_GlowPulse).
//   * Flowers appear whole as a closed bud when the growth front reaches their centre and spring open over _BloomWindow of growth.
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
        _FlowerColor ("Flower Color (yellow flowers: part id ~0.8)", Color) = (0.949, 0.784, 0.294, 1)
        _CapColor ("Cap Color (mushroom red, caps: part id ~0.92)", Color) = (0.839, 0.271, 0.239, 1)
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
        _DetailStrength ("T49 surface detail strength (bark, veins, mottling)", Range(0, 2)) = 1
        _DetailScale ("T49 surface detail scale (per metre)", Range(1, 4)) = 1.8
        [NoScaleOffset] _BarkTex ("T67 bark / fibre texture (stalks, vines, stems)", 2D) = "white" {}
        [NoScaleOffset] _LeafTex ("T67 leaf texture (veins, blades, petals)", 2D) = "white" {}
        [NoScaleOffset] _CapTex ("T67 cap texture (mushroom caps, bowls)", 2D) = "white" {}
        _TexStrength ("T67 texture strength", Range(0, 1)) = 0.85
        _TexScale ("T67 texture tiling (per metre)", Range(0.2, 6)) = 1.4
        [Toggle] _PartColors ("Use part id (vertex colour G) for the palette", Float) = 0
        _Sway ("Leaf / flower sway (m, needs part ids)", Range(0, 0.1)) = 0.022
        _GlowPulse ("Flower-centre glow pulse (needs part ids)", Range(0, 2)) = 0.7
        _BloomWindow ("Flower opening (growth after the front reaches a flower)", Range(0.01, 0.3)) = 0.15
        [HideInInspector] _Bend ("Stalk bend: tip offset x, z (object space, m), stalk height (z); set per plant by PlantInstance", Vector) = (0, 0, 0, 0)
        [HideInInspector] _Squash ("Cap squash: object-space point (xyz), depth in m (w); set per plant by PlantInstance", Vector) = (0, 0, 0, 0)
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
            float4 _CapColor;
            float4 _FlowerColor;
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
            float _PartColors;
            float _DetailStrength;
            float _DetailScale;
            float _TexStrength;
            float _TexScale;
            float _Sway;
            float _GlowPulse;
            float _BloomWindow;
            float4 _Squash;
            float4 _Bend;
        CBUFFER_END

        TEXTURE2D(_BarkTex); SAMPLER(sampler_BarkTex);
        TEXTURE2D(_LeafTex); SAMPLER(sampler_LeafTex);
        TEXTURE2D(_CapTex); SAMPLER(sampler_CapTex);

        // T67: triplanar sample of a tileable map (the bakes carry no UVs), weighted by the surface normal
        float TriTex(TEXTURE2D_PARAM(tex, smp), float3 posOS, float3 n)
        {
            float3 w = pow(abs(n), 4.0); w /= (w.x + w.y + w.z + 1e-4);
            float3 p = posOS * _TexScale;
            return SAMPLE_TEXTURE2D(tex, smp, p.zy).r * w.x + SAMPLE_TEXTURE2D(tex, smp, p.xz).r * w.y + SAMPLE_TEXTURE2D(tex, smp, p.xy).r * w.z;
        }


        // T49: cheap procedural surface detail from the object-space position (the bakes carry no UVs): fibre / bark grain, cracks and veins, cap mottling.
        float Hash13(float3 p) { p = frac(p * 0.1031); p += dot(p, p.zyx + 31.32); return frac((p.x + p.y) * p.z); }
        float VNoise(float3 p)
        {
            float3 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
            float a = lerp(lerp(Hash13(i), Hash13(i + float3(1, 0, 0)), f.x), lerp(Hash13(i + float3(0, 1, 0)), Hash13(i + float3(1, 1, 0)), f.x), f.y);
            float b = lerp(lerp(Hash13(i + float3(0, 0, 1)), Hash13(i + float3(1, 0, 1)), f.x), lerp(Hash13(i + float3(0, 1, 1)), Hash13(i + float3(1, 1, 1)), f.x), f.y);
            return lerp(a, b, f.z);
        }
        // returns a brightness multiplier around 1
        float SurfaceDetail(float3 posOS, float part, float partColors, float3 normalWS)
        {
            float3 p = posOS * _DetailScale;
            float fine = VNoise(p * 9.0), mid = VNoise(p * 3.5 + 11.0);
            float grain = 0.72 + 0.5 * (fine * 0.6 + mid * 0.4);                         // fibres / mottling
            float ridge = 1.0 - abs(2.0 * VNoise(p * 5.0 + 3.0) - 1.0); ridge = pow(saturate(ridge), 6.0);   // thin dark cracks / leaf veins
            float cracks = 1.0 - 0.32 * ridge;
            float d = grain * cracks;
            if (partColors > 0.5 && part > 0.88 && part < 0.97)                          // mushroom caps: darker toward the rim of the blotches, paler underside
            {
                float under = saturate(-normalWS.y * 1.6);
                d = lerp(0.85 + 0.3 * mid, 1.35, under);
            }
            if (partColors > 0.5)
            {
                float t;
                if (part > 0.88 && part < 0.97) t = TriTex(TEXTURE2D_ARGS(_CapTex, sampler_CapTex), posOS, normalWS);
                else if (part > 0.5 && part < 0.72) t = TriTex(TEXTURE2D_ARGS(_LeafTex, sampler_LeafTex), posOS, normalWS);
                else t = TriTex(TEXTURE2D_ARGS(_BarkTex, sampler_BarkTex), posOS, normalWS);
                d *= lerp(1.0, t * 1.15, _TexStrength);
            }
            return lerp(1.0, d, saturate(_DetailStrength));
        }

        // Leaves and petals drift in a light breeze, bending from their root: the bake's sway weight (vertex colour B) is 0 where a leaf
        // meets its stalk and 1 at its tip, and 0 on decks, steps, vines, caps and bowls. The phase changes slowly across the plant, so
        // neighbours follow a moment later. Shared by all three passes (depth and shadows must match the lit pass).
        float3 SwayOS(float3 positionOS, float weight)
        {
            float amount = _Sway * 1.8 * weight * step(0.5, _PartColors);
            float ph = dot(positionOS, float3(1.7, 0.9, 1.3));
            float t = _Time.y;
            return float3(sin(t * 1.31 + ph), 0.6 * sin(t * 1.73 + ph * 1.3), cos(t * 1.07 + ph * 0.8)) * amount;
        }

        // A mushroom cap (part ~.92) and the white spots on it (part > .97) dip round the point the player landed on and spring back:
        // PlantInstance animates the depth. Only the cap that was hit moves (the point's height picks it); its collider stays where it is.
        float3 SquashOS(float3 positionOS, float part)
        {
            float cap = part > 0.88 ? 1.0 : 0.0;
            float near = smoothstep(1.6, 0.0, distance(positionOS.xz, _Squash.xz)) * step(abs(positionOS.y - _Squash.y), 0.6);
            return float3(0.0, -_Squash.w * cap * near * step(0.5, _PartColors), 0.0);
        }

        // A grown beanstalk leans with the breeze from its base: the offset grows with the square of the height, so the root stays put and
        // the tip moves most (PlantInstance animates _Bend.xy and eases it to zero while the player climbs; colliders never move).
        float3 BendOS(float3 positionOS)
        {
            if (_Bend.z <= 0.0) return float3(0.0, 0.0, 0.0);
            float h = saturate(positionOS.y / _Bend.z);
            return float3(_Bend.x, 0.0, _Bend.y) * (h * h);
        }

        // How far a flower has opened (0 bud, 1 open): it starts when the growth front reaches its centre (a = vertex colour A). A flower
        // near the tip gets a shorter window, so every flower is fully open at growth 1.
        float FlowerOpen(float a)
        {
            float w = max(min(_BloomWindow, 1.0 - a), 0.004);
            return saturate((_Growth - a) / w);
        }

        // A flower opening round its centre c (object space, Y up): closed, the petals are pulled in to the stem, folded upward and wrapped
        // round each other; they unfurl and spring slightly past open before they settle. Open (o = 1) leaves the vertex where it is.
        float3 BloomOS(float3 p, float3 c, float o)
        {
            const float k = 1.4;                                  // overshoot
            float u = o - 1.0;
            float e = 1.0 + (k + 1.0) * u * u * u + k * u * u;    // ease out back: 0 -> 1
            float3 d = p - c;
            float r = length(d.xz);
            float tw = (1.0 - e) * 1.1;
            float sn = sin(tw), cs = cos(tw);
            float2 xz = float2(d.x * cs - d.z * sn, d.x * sn + d.z * cs) * lerp(0.3, 1.0, e);
            float y = d.y * lerp(0.6, 1.0, e) + (1.0 - e) * r * 0.85;
            return c + float3(xz.x, y, xz.y) * lerp(0.5, 1.0, e);
        }

        // Everything the vertex stage does to a plant vertex, shared by the three passes. A flower (A < 1) is revealed by its centre's
        // growth_t, so it appears whole as a bud, and skips the taper.
        void PlantVertex(float3 positionOS, float3 normalOS, float4 col, float2 uv0, float2 uv1, out float3 posOS, out float growthT)
        {
            float flower = col.a < 0.995 ? 1.0 : 0.0;
            growthT = flower > 0.5 ? col.a : col.r;
            float3 p = positionOS;
            if (flower > 0.5) p = BloomOS(p, float3(uv0.x, uv0.y, uv1.x), FlowerOpen(col.a));
            // Tapered tip: inset along the normal near the growth front (t = 1 well behind the front, 0 at the front). The taper fades
            // out over the last 8% of growth so a finished plant keeps its full-width tip.
            float t = saturate((_Growth - growthT) / _TaperLength);
            float fade = 1.0 - smoothstep(0.92, 1.0, _Growth);
            p -= normalOS * (_TaperAmount * (1.0 - t) * fade * (1.0 - flower));
            posOS = p + SwayOS(positionOS, col.b) + SquashOS(positionOS, col.g) + BendOS(positionOS);
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
                float2 uv0        : TEXCOORD0;   // flower centre x, y (object space)
                float2 uv1        : TEXCOORD1;   // flower centre z
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  growthT    : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                float  part       : TEXCOORD4;
                float3 positionOS : TEXCOORD5;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float growthT; float3 posOS;
                PlantVertex(IN.positionOS.xyz, IN.normalOS, IN.color, IN.uv0, IN.uv1, posOS, growthT);
                VertexPositionInputs vp = GetVertexPositionInputs(posOS);
                VertexNormalInputs vn = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS = vn.normalWS;
                OUT.growthT = growthT;
                OUT.part = IN.color.g;
                OUT.positionOS = IN.positionOS.xyz;
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
                if (_PartColors > 0.5)
                {
                    // part id from the bakes: 0 deck, .34 vines, .67 leaves, ~.8 yellow flowers, ~.92 mushroom caps (platform), 1 blossoms / white
                    float3 deck = _ShadeColor.rgb * 1.15;
                    float3 vine = _BaseColor.rgb;
                    float3 leaf = lerp(_BaseColor.rgb, _TipColor.rgb, 0.4);
                    float3 blossom = _TipColor.rgb;
                    float3 mist = float3(0.749, 0.851, 0.933);   // T40: flower inner petals, mist blue
                    float3 glow = _RimColor.rgb * 1.15;           // T40: glowing flower centre
                    albedo = IN.part > 0.9975 ? blossom : IN.part > 0.99 ? glow : IN.part > 0.98 ? blossom : IN.part > 0.97 ? mist : IN.part > 0.88 ? _CapColor.rgb : IN.part > 0.72 ? _FlowerColor.rgb : IN.part > 0.5 ? leaf : IN.part > 0.17 ? vine : deck;
                }

                albedo *= SurfaceDetail(IN.positionOS, IN.part, _PartColors, N);

                float3 lit = lerp(albedo * _ShadeColor.rgb * 1.2, albedo, ramp) * mainLight.color;
                float3 ambient = SampleSH(N) * albedo;
                float3 color = lit * 0.9 + ambient * 0.6;

                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower) * _RimStrength;
                color += _RimColor.rgb * rim;

                // The glowing flower centres (part .99 - .9975) pulse softly, each on its own beat.
                if (_PartColors > 0.5 && IN.part > 0.99 && IN.part <= 0.9975)
                    color += _RimColor.rgb * (_GlowPulse * (0.5 + 0.5 * sin(_Time.y * 2.1 + dot(IN.positionOS, float3(0.9, 0.7, 1.1)))));

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

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; float2 uv0 : TEXCOORD0; float2 uv1 : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float growthT : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float growthT; float3 posOS;
                PlantVertex(IN.positionOS.xyz, IN.normalOS, IN.color, IN.uv0, IN.uv1, posOS, growthT);
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

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; float2 uv0 : TEXCOORD0; float2 uv1 : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float growthT : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float growthT; float3 posOS;
                PlantVertex(IN.positionOS.xyz, IN.normalOS, IN.color, IN.uv0, IN.uv1, posOS, growthT);
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
