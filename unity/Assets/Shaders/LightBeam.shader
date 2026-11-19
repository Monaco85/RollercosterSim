// Sichtbarer Lichtkegel (Himmelsstrahler, Scheinwerfer): additiver Kegel, Mitte heller als der Rand,
// blendet zum Ende und an Geometrie weich aus. uv.y = 0 an der Lampe, 1 am Kegelende.
Shader "Achterbahn/LightBeam"
{
    Properties
    {
        _Color ("Farbe", Color) = (1, 0.9, 0.7, 1)
        _Intensity ("Staerke", Float) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Beam"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _Color; float _Intensity;
            CBUFFER_END

            struct A { float4 pos : POSITION; float3 nor : NORMAL; float2 uv : TEXCOORD0; float4 col : COLOR; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 nw : TEXCOORD1; float3 vw : TEXCOORD2; float4 sp : TEXCOORD3; float z : TEXCOORD4; float4 col : COLOR; };

            V vert(A i)
            {
                V o; VertexPositionInputs p = GetVertexPositionInputs(i.pos.xyz);
                o.pos = p.positionCS; o.uv = i.uv; o.col = i.col;
                o.nw = TransformObjectToWorldNormal(i.nor); o.vw = GetWorldSpaceViewDir(p.positionWS);
                o.sp = ComputeScreenPos(o.pos); o.z = -p.positionVS.z;
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                float along = saturate(i.uv.y);
                float fall = pow(1.0 - along, 1.6) * smoothstep(0.0, 0.03, along);
                float edge = abs(dot(normalize(i.nw), normalize(i.vw)));
                edge = edge * edge;
                float2 suv = i.sp.xy / i.sp.w;
                float scene = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float soft = saturate((scene - i.z) / 2.0) * saturate((i.z - 0.5) / 3.0);
                float3 c = _Color.rgb * i.col.rgb * (_Intensity * fall * edge * soft);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
