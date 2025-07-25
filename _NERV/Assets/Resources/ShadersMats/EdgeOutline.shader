Shader "Hidden/EdgeOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 1, 1, 1) // Cyan by default
        _EdgeThreshold ("Edge Threshold", Range(0.1, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _OutlineColor;
            float _EdgeThreshold;

            float luminance(float3 color) {
                return dot(color, float3(0.299, 0.587, 0.114));
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 texel = 1.0 / _ScreenParams.xy;

                float3 baseColor = tex2D(_MainTex, i.uv).rgb;
                float lumCenter = dot(baseColor, float3(0.299, 0.587, 0.114));

                float lumRight = dot(tex2D(_MainTex, i.uv + float2(texel.x, 0)).rgb, float3(0.299, 0.587, 0.114));
                float lumLeft  = dot(tex2D(_MainTex, i.uv - float2(texel.x, 0)).rgb, float3(0.299, 0.587, 0.114));
                float lumUp    = dot(tex2D(_MainTex, i.uv + float2(0, texel.y)).rgb, float3(0.299, 0.587, 0.114));
                float lumDown  = dot(tex2D(_MainTex, i.uv - float2(0, texel.y)).rgb, float3(0.299, 0.587, 0.114));

                float hEdge = abs(lumRight - lumLeft);
                float vEdge = abs(lumUp - lumDown);

                float edge = step(_EdgeThreshold, sqrt(hEdge * hEdge + vEdge * vEdge));

                // Combine original color with the outline
                float3 finalColor = baseColor + edge * _OutlineColor.rgb;

                return fixed4(finalColor, 1.0);
            }

            ENDCG
        }
    }
}
