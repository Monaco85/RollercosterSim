// Lichthof um jede Gluehbirne: Billboard im Vertex-Shader, additiv, weich an Geometrie (Szenentiefe).
// Mindestgroesse waechst mit der Entfernung, damit ferne Lichter noch funkeln.
Shader "Achterbahn/BulbHalo"
{
    Properties
    {
        _Intensity ("Staerke", Float) = 1.2
        _MinAngle ("Mindestgroesse je Meter", Float) = 0.0035
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+20" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Halo"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "BulbPattern.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _Intensity; float _MinAngle;
            CBUFFER_END

            struct A { float4 pos : POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; float4 d : TEXCOORD1; float2 sz : TEXCOORD2; };
            struct V { float4 pos : SV_POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; float4 d : TEXCOORD1; float4 sp : TEXCOORD2; float2 fz : TEXCOORD3; };

            V vert(A i)
            {
                V o;
                float3 cv = TransformWorldToView(TransformObjectToWorld(i.pos.xyz));
                float dist = -cv.z;
                float s = max(i.sz.x, dist * _MinAngle);
                cv.xy += i.uv * s;
                cv.z += min(s * 0.7, max(dist - 0.3, 0.0) * 0.5);   // etwas zur Kamera ziehen, damit der Hof nicht in der Wand steckt
                o.pos = mul(UNITY_MATRIX_P, float4(cv, 1.0));
                o.col = i.col; o.uv = i.uv; o.d = i.d;
                o.sp = ComputeScreenPos(o.pos);
                o.fz = float2(ComputeFogFactor(o.pos.z), -cv.z);
                o.col.a = saturate((dist - 1.2) / 3.0);            // direkt vor der Kamera ausblenden
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                float r2 = dot(i.uv, i.uv);
                if (r2 > 1.0) discard;
                float g = exp(-r2 * 6.0) * (1.0 - r2) + exp(-r2 * 45.0) * 0.8;
                float3 col = i.col.rgb; float k;
                BulbPattern(i.d, _Time.y, col, k);
                float2 suv = i.sp.xy / i.sp.w;
                float scene = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float soft = saturate((scene - i.fz.y) / 0.6);
                float3 c = col * (k * g * _Intensity * soft * i.col.a);
                c *= lerp(i.fz.x, 1.0, 0.5) ;
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
