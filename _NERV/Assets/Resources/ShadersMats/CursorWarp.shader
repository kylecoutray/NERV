Shader "Hidden/CursorWarp"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cursor ("Cursor Position", Vector) = (0.5, 0.5, 0, 0)
        _Cursor ("Cursor Position", Vector) = (0.5, 0.5, 0, 0)
        _PulseCenter ("Pulse Center", Vector) = (0.5, 0.5, 0, 0)
        _PulseStartTime ("Pulse Time", Float) = 0

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
            float4 _Cursor;
            float4 _MainTex_TexelSize;
            float4 _PulseCenter;
            float _PulseStartTime;


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

            float2 cursorUV = _Cursor.xy;
            float2 delta = i.uv - cursorUV;
            float dist = length(delta);

            //  Constant Cursor Ripple Distortion
            float wave = sin(dist * 60.0 - t * 10.0) * 0.005;
            float pinch = exp(-dist * 20.0) * 0.05;
            float2 warpedUV = i.uv + normalize(delta) * (wave + pinch);

            //  PULSE: time since click
            float2 pulseCenter = _PulseCenter.xy;
            float pulseTime = t - _PulseStartTime;
            float2 deltaPulse = i.uv - pulseCenter;
            float pulseDist = length(deltaPulse);
            float ring = exp(-pow((pulseDist - pulseTime * 0.5) * 25.0, 2.0)); // Gaussian ring shape
            float2 pulseWarp = normalize(deltaPulse) * ring * 0.1;

            warpedUV += pulseWarp;

            float3 color = tex2D(_MainTex, warpedUV).rgb;

            //  Saturation spike near cursor
            float3 hsv = RGBToHSV(color);
            float saturationBoost = 1.5 + exp(-dist * 10.0) * 4.0 + ring * 5.0;
            hsv.y *= saturationBoost;
            color = HSVToRGB(hsv);

            return float4(saturate(color), 1.0);
        }

            ENDCG
        }
    }
}
