Shader "Hidden/GlitchVision"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            // Pseudo-random hash based on UV + time
            float rand(float2 co) {
                return frac(sin(dot(co.xy, float2(12.9898,78.233))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target {
                float t = _Time.y;

                // SOFTER BLOCKY OFFSET — finer resolution blocks
                float2 blockUV = floor(i.uv * 150.0) / 150.0;

                // JITTER (still strong, but more detail preserved)
                float jitterX = (rand(blockUV * 50 + t * 6.0) - 0.5) * 0.03;
                float jitterY = (rand(blockUV * 70 - t * 4.0) - 0.5) * 0.03;

                float2 uvR = blockUV + float2(jitterX, jitterY);
                float2 uvG = blockUV + float2(-jitterX * 0.5, jitterY * 0.5);
                float2 uvB = blockUV;

                // BROKEN IMAGE BURST (lower chance)
                if (rand(blockUV * t * 10.0) > 0.995)
                    discard;

                // RGB split
                float r = tex2D(_MainTex, uvR).r;
                float g = tex2D(_MainTex, uvG).g;
                float b = tex2D(_MainTex, uvB).b;
                float3 color = float3(r, g, b);

                // SCANLINES — keep subtle
                float scan = sin(i.uv.y * 200.0 + t * 60.0);
                color *= 0.85 + 0.15 * scan;

                //  SATURATION (dynamic)
                float lum = dot(color, float3(0.3, 0.59, 0.11));
                float satBoost = lerp(2.5, 6.0, rand(float2(t * 3.1, t * 9.6)));
                color = lerp(float3(lum, lum, lum), color, satBoost);

                // WHITE FLASHES (rare and quick)
                if (rand(i.uv * 800 + t * 8.0) > 0.995)
                    color = float3(1.0, 1.0, 1.0);

                return float4(saturate(color), 1.0);
            }

            ENDCG
        }
    }
}
