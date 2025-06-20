Shader "Hidden/LiquidScanner"
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

            float3 RGBToHSV(float3 c) {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HSVToRGB(float3 c) {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            fixed4 frag(v2f i) : SV_Target {
                float t = _Time.y;

                // 🌊 WAVE DISTORTION (liquid feel)
                float2 distortedUV = i.uv;
                distortedUV.x += sin(i.uv.y * 30.0 + t * 4.0) * 0.01;
                distortedUV.y += cos(i.uv.x * 40.0 + t * 6.0) * 0.01;

                float3 color = tex2D(_MainTex, distortedUV).rgb;

                // HUE SHIFT
                float3 hsv = RGBToHSV(color);
                hsv.x = frac(hsv.x + t * 0.5); // shift hue slowly
                color = HSVToRGB(hsv);

                // GRID SCANNER LINES
                float gridH = frac(i.uv.y * 60.0 + t * 1.5) < 0.01 ? 0.2 : 0.0;
                float gridV = frac(i.uv.x * 60.0 + t * 1.2) < 0.01 ? 0.2 : 0.0;
                color += gridH + gridV;

                return float4(saturate(color), 1.0);
            }
            ENDCG
        }
    }
}
