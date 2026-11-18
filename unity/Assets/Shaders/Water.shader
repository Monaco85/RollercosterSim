// Wasser fuer den Dino-See (URP): drei bewegte Wellen-Normalkarten, Tiefenfarbe, Fresnel-Spiegelung
// (Reflection Probe / Himmel), Sonnenglanz mit Schatten, Uferschaum ueber die Szenentiefe, Nebel.
Shader "Achterbahn/Water"
{
    Properties
    {
        _ShallowColor ("Flach", Color) = (0.16, 0.42, 0.40, 1)
        _DeepColor ("Tief", Color) = (0.02, 0.10, 0.10, 1)
        _FoamColor ("Schaum", Color) = (0.85, 0.9, 0.88, 1)
        _NormalTex ("Wellen", 2D) = "bump" {}
        _NormalScale ("Wellenstaerke", Float) = 0.7
        _Tiling ("Kachelung", Float) = 0.045
        _Speed ("Tempo", Float) = 0.02
        _DepthFade ("Tiefenverlauf (m)", Float) = 3.5
        _FoamDist ("Schaumbreite (m)", Float) = 0.7
        _Smoothness ("Glaette", Range(0,1)) = 0.93
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "WaterForward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_NormalTex); SAMPLER(sampler_NormalTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor, _DeepColor, _FoamColor, _NormalTex_ST;
                float _NormalScale, _Tiling, _Speed, _DepthFade, _FoamDist, _Smoothness;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
                float  fog        : TEXCOORD2;
            };

            Varyings vert (Attributes i)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(i.positionOS.xyz);
                o.positionWS = ws;
                o.positionCS = TransformWorldToHClip(ws);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float3 Wave(float2 uv) { return UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, uv), _NormalScale); }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float2 uv = i.positionWS.xz * _Tiling;
                float3 n1 = Wave(uv + float2(t, t * 0.6));
                float3 n2 = Wave(uv * 1.9 + float2(-t * 0.9, t * 0.4));
                float3 n3 = Wave(uv * 0.21 + float2(t * 0.25, -t * 0.18));
                float2 slope = n1.xy + n2.xy * 0.7 + n3.xy * 1.4;
                float3 nw = normalize(float3(slope.x, 1.0, slope.y));

                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
                float2 suv = i.screenPos.xy / i.screenPos.w;
                float sceneEye = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float depth = max(0.0, sceneEye - i.screenPos.w);
                float fade = 1.0 - exp(-depth / _DepthFade);

                Light L = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float sun = saturate(L.direction.y) * L.shadowAttenuation;
                half3 body = lerp(_ShallowColor.rgb, _DeepColor.rgb, fade) * (0.35 + 0.65 * L.color * sun);

                float fres = 0.03 + 0.97 * pow(1.0 - saturate(dot(nw, V)), 5.0);
                float3 R = reflect(-V, nw);
                half3 refl = GlossyEnvironmentReflection(R, 1.0 - _Smoothness, 1.0);

                float3 H = normalize(L.direction + V);
                float spec = pow(saturate(dot(nw, H)), 700.0) * 9.0 * L.shadowAttenuation;

                half3 col = lerp(body, refl, saturate(fres)) + spec * L.color;
                float foam = saturate(1.0 - depth / _FoamDist) * saturate(0.55 + n1.x * 2.0 + n2.y);
                col = lerp(col, _FoamColor.rgb * (0.5 + 0.5 * sun), foam * 0.7);
                float alpha = saturate(0.35 + fade * 0.75 + fres * 0.4 + foam);
                col = MixFog(col, i.fog);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
