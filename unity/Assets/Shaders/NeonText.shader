// Neon-Schrift fuer TextMesh: Schriftatlas als Maske, Farbe in HDR (leuchtet mit Bloom), gelegentliches Flackern.
Shader "Achterbahn/NeonText"
{
    Properties
    {
        _MainTex ("Schrift", 2D) = "white" {}
        _Color ("Farbe", Color) = (1, 0.3, 0.6, 1)
        _Intensity ("Staerke", Float) = 4
        _Seed ("Zufall", Float) = 0
        _Mode ("Muster (0 ruhig, 1 flackern, 2 blinken)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BulbPattern.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST; float4 _Color; float _Intensity; float _Seed; float _Mode;
            CBUFFER_END

            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; float4 col : COLOR; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 col : COLOR; float fog : TEXCOORD1; };

            V vert(A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv; o.col = i.col; o.fog = ComputeFogFactor(o.pos.z); return o; }

            half4 frag(V i) : SV_Target
            {
                float a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                float t = _Time.y;
                float k = 1.0;
                if (_Mode > 0.5 && _Mode < 1.5)
                {
                    // meist ruhig, ab und zu ein kurzes Flackern wie eine alte Neonroehre
                    float slot = floor(t * 0.4 + _Seed * 7.0);
                    float bad = step(0.78, BulbHash(slot * 3.1 + _Seed));
                    float fl = step(0.45, BulbHash(floor(t * 22.0) + _Seed * 13.0));
                    k = lerp(1.0, 0.15 + 0.85 * fl, bad);
                }
                else if (_Mode > 1.5) k = frac(t * 0.8 + _Seed) < 0.7 ? 1.0 : 0.1;
                float3 c = _Color.rgb * i.col.rgb * _Intensity * k;
                c = MixFog(c, lerp(i.fog, 1.0, 0.6));
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
