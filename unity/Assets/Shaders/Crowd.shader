// Menschenmenge: Skinning auf der Grafikkarte aus einer Animations-Textur (Knochenmatrizen je Bild).
// Ein Material pro Person (SRP-Batcher-tauglich: Bild und Palette als Materialwerte), volle URP-Beleuchtung inkl. Forward+.
Shader "Achterbahn/Crowd"
{
    Properties
    {
        _AnimTex ("Animation", 2D) = "black" {}
        _Slots ("Knochen", Float) = 1
        _Smoothness ("Glaette", Float) = 0.25
        _Frame ("Bild A, Bild B, Mischung", Vector) = (0, 0, 0, 0)
        _C0 ("Farbe 0", Color) = (0.5, 0.5, 0.5, 1)
        _C1 ("Farbe 1", Color) = (0.5, 0.5, 0.5, 1)
        _C2 ("Farbe 2", Color) = (0.5, 0.5, 0.5, 1)
        _C3 ("Farbe 3", Color) = (0.5, 0.5, 0.5, 1)
        _C4 ("Farbe 4", Color) = (0.5, 0.5, 0.5, 1)
        _C5 ("Farbe 5", Color) = (0.5, 0.5, 0.5, 1)
        _C6 ("Farbe 6", Color) = (0.5, 0.5, 0.5, 1)
        _C7 ("Farbe 7", Color) = (0.5, 0.5, 0.5, 1)
        _C8 ("Farbe 8", Color) = (0.5, 0.5, 0.5, 1)
        _C9 ("Farbe 9", Color) = (0.5, 0.5, 0.5, 1)
        _C10 ("Farbe 10", Color) = (0.5, 0.5, 0.5, 1)
        _C11 ("Farbe 11", Color) = (0.5, 0.5, 0.5, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_AnimTex);
        CBUFFER_START(UnityPerMaterial)
        float4 _AnimTex_TexelSize; float _Slots; float _Smoothness; float4 _Frame;
        float4 _C0, _C1, _C2, _C3, _C4, _C5, _C6, _C7, _C8, _C9, _C10, _C11;
        CBUFFER_END
        float4 Palette(int i) { float4 p[12] = { _C0, _C1, _C2, _C3, _C4, _C5, _C6, _C7, _C8, _C9, _C10, _C11 }; return p[clamp(i, 0, 11)]; }

        struct CrowdIn { float4 pos : POSITION; float3 nor : NORMAL; float4 idx : TEXCOORD0; float4 w : TEXCOORD1; float2 pal : TEXCOORD2; };

        float4 AnimTexel(uint i) { uint wdt = (uint)_AnimTex_TexelSize.z; return LOAD_TEXTURE2D_LOD(_AnimTex, uint2(i % wdt, i / wdt), 0); }
        float3x4 Bone(float frame, uint slot)
        {
            uint b = ((uint)(frame + 0.5) * (uint)(_Slots + 0.5) + slot) * 3;
            return float3x4(AnimTexel(b), AnimTexel(b + 1), AnimTexel(b + 2));
        }
        void Skin(CrowdIn i, out float3 p, out float3 n)
        {
            float3x4 ma = (float3x4)0, mb = (float3x4)0;
            [unroll] for (int k = 0; k < 4; k++)
            {
                uint s = (uint)(i.idx[k] + 0.5); float wk = i.w[k];
                ma += Bone(_Frame.x, s) * wk; mb += Bone(_Frame.y, s) * wk;
            }
            float3x4 m = lerp(ma, mb, _Frame.z);
            p = mul(m, float4(i.pos.xyz, 1.0));
            n = normalize(mul((float3x3)m, i.nor));
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct V { float4 pos : SV_POSITION; float3 ws : TEXCOORD0; float3 nw : TEXCOORD1; float pal : TEXCOORD2; float fog : TEXCOORD3; };

            V vert(CrowdIn i)
            {
                V o; float3 p, n; Skin(i, p, n);
                o.ws = TransformObjectToWorld(p); o.nw = TransformObjectToWorldNormal(n);
                o.pos = TransformWorldToHClip(o.ws); o.pal = i.pal.x; o.fog = ComputeFogFactor(o.pos.z);
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                InputData d = (InputData)0;
                d.positionWS = i.ws; d.positionCS = i.pos; d.normalWS = normalize(i.nw);
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.ws);
                d.shadowCoord = TransformWorldToShadowCoord(i.ws);
                d.fogCoord = i.fog; d.bakedGI = SampleSH(d.normalWS);
                d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.pos); d.shadowMask = half4(1, 1, 1, 1);
                SurfaceData s = (SurfaceData)0;
                s.albedo = Palette((int)(i.pal + 0.5)).rgb; s.alpha = 1; s.smoothness = _Smoothness; s.occlusion = 1; s.normalTS = half3(0, 0, 1); s.specular = 0; s.metallic = 0;
                half4 c = UniversalFragmentPBR(d, s);
                c.rgb = MixFog(c.rgb, d.fogCoord);
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection; float3 _LightPosition;
            float4 vert(CrowdIn i) : SV_POSITION
            {
                float3 p, n; Skin(i, p, n);
                float3 ws = TransformObjectToWorld(p), nw = TransformObjectToWorldNormal(n);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 ld = normalize(_LightPosition - ws);
                #else
                float3 ld = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(ws, nw, ld));
                #if UNITY_REVERSED_Z
                cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return cs;
            }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 vert(CrowdIn i) : SV_POSITION { float3 p, n; Skin(i, p, n); return TransformObjectToHClip(p); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct V { float4 pos : SV_POSITION; float3 nw : TEXCOORD0; };
            V vert(CrowdIn i) { V o; float3 p, n; Skin(i, p, n); o.pos = TransformObjectToHClip(p); o.nw = TransformObjectToWorldNormal(n); return o; }
            half4 frag(V i) : SV_Target { return half4(normalize(i.nw), 0); }
            ENDHLSL
        }
    }
}
