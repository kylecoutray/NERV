Shader "Custom/RetroGlitch"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _NoiseTex ("Noise (R)", 2D) = "white" {}
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.5
        _GlitchIntensity ("Glitch Intensity", Range(0,1)) = 0.3
        _RGBSplitAmount ("RGB Split", Range(0,0.1)) = 0.02
        _Speed ("Glitch Speed", Range(0,10)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex, _NoiseTex;
            float _ScanlineIntensity, _GlitchIntensity, _RGBSplitAmount, _Speed;
            float4 _MainTex_TexelSize;

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                // Time-based noise
                float n = tex2D(_NoiseTex, uv * 5 + _Time.y * _Speed).r;

                // Horizontal slice glitch
                if (n < _GlitchIntensity)
                {
                    float ySlice = floor(uv.y * 10) / 10;
                    uv.x += (rand(float2(ySlice, _Time.y)) - 0.5) * 0.2 * _GlitchIntensity;
                }

                // RGB split
                float2 rUV = uv + float2(_RGBSplitAmount, 0);
                float2 bUV = uv - float2(_RGBSplitAmount, 0);

                fixed4 col;
                col.r = tex2D(_MainTex, rUV).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, bUV).b;
                col.a = 1;

                // Scanlines
                float scan = sin(uv.y * _MainTex_TexelSize.y * 150.0) * 0.5 + 0.5;
                col.rgb *= lerp(1, scan, _ScanlineIntensity);

                return col;
            }
            ENDCG
        }
    }
}
