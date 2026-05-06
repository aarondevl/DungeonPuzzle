Shader "DungeonPuzzle/VisionCone"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _EdgeFade ("Edge Fade", Range(0,1)) = 0.35
        _ScrollSpeed ("Scroll Speed", Float) = 0.6
        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.45
        _PulseSpeed ("Pulse Speed", Float) = 2.5
        _PulseAmount ("Pulse Amount", Range(0,1)) = 0.15
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct A { float4 pos:POSITION; float4 col:COLOR; };
            struct V { float4 pos:SV_POSITION; float4 col:COLOR; float2 local:TEXCOORD0; };

            float4 _Color;
            float _EdgeFade;
            float _ScrollSpeed;
            float _NoiseStrength;
            float _PulseSpeed;
            float _PulseAmount;

            // hash-based pseudo-noise
            float hash(float2 p){ return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p){
                float2 i = floor(p); float2 f = frac(p);
                float a = hash(i);
                float b = hash(i + float2(1,0));
                float c = hash(i + float2(0,1));
                float d = hash(i + float2(1,1));
                float2 u = f*f*(3.0-2.0*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            V vert(A i)
            {
                V o;
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.col = i.col;
                o.local = i.pos.xy;
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                float r = length(i.local);
                float radial = saturate(1.0 - r * _EdgeFade);

                float t = _Time.y * _ScrollSpeed;
                float n = noise(i.local * 4.0 + float2(t, t*0.5));
                float scan = lerp(1.0 - _NoiseStrength, 1.0, n);

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                half4 c = _Color;
                c.a *= i.col.a * radial * scan * pulse;
                c.rgb *= scan * pulse;
                return c;
            }
            ENDHLSL
        }
    }
}
