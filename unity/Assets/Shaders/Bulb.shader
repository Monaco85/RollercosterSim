// Gluehbirne: selbstleuchtend (HDR, fuer Bloom), Muster aus BulbPattern.hlsl
Shader "Achterbahn/Bulb"
{
    Properties
    {
        _Intensity ("Staerke", Float) = 5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Bulb"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BulbPattern.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _Intensity;
            CBUFFER_END

            struct A { float4 pos : POSITION; float3 nor : NORMAL; float4 col : COLOR; float4 d : TEXCOORD1; };
            struct V { float4 pos : SV_POSITION; float4 col : COLOR; float4 d : TEXCOORD0; float3 nw : TEXCOORD1; float3 vw : TEXCOORD2; float fog : TEXCOORD3; };

            V vert(A i)
            {
                V o; VertexPositionInputs p = GetVertexPositionInputs(i.pos.xyz);
                o.pos = p.positionCS; o.col = i.col; o.d = i.d;
                o.nw = TransformObjectToWorldNormal(i.nor); o.vw = GetWorldSpaceViewDir(p.positionWS);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                float3 col = i.col.rgb; float k;
                BulbPattern(i.d, _Time.y, col, k);
                float ndv = saturate(dot(normalize(i.nw), normalize(i.vw)));
                float3 c = col * (k * _Intensity * (0.5 + 0.5 * ndv) + 0.05);
                c = MixFog(c, lerp(i.fog, 1.0, 0.55));   // Lichter leuchten durch den Dunst
                return half4(c, 1);
            }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 vert(float4 pos : POSITION) : SV_POSITION { return TransformObjectToHClip(pos.xyz); }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 pos : POSITION; float3 nor : NORMAL; };
            struct V { float4 pos : SV_POSITION; float3 n : TEXCOORD0; };
            V vert(A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.n = TransformObjectToWorldNormal(i.nor); return o; }
            half4 frag(V i) : SV_Target { return half4(normalize(i.n), 0); }
            ENDHLSL
        }
    }
}
