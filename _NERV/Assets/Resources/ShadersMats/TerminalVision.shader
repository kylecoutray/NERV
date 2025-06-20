Shader "Hidden/TerminalVision"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
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

            float luminance(float3 color) {
                return dot(color, float3(0.3, 0.59, 0.11));
            }

            fixed4 frag(v2f i) : SV_Target {
                float3 col = tex2D(_MainTex, i.uv).rgb;

                // Get 4 neighboring pixels
                float3 right = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, 0)).rgb;
                float3 down  = tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y)).rgb;

                // Edge detection using difference with neighbors
                float diffX = distance(col, right);
                float diffY = distance(col, down);
                float edgeStrength = diffX + diffY;

                // Only show edges above a small threshold
                float edge = step(0.1, edgeStrength); // Try 0.2, tweak lower or higher

                float3 green = float3(0.15, 1.0, 0.1);
                return float4(edge * green, 1.0);
            }

            ENDCG
        }
    }
}
