// Assets/Shaders/SuperhotTVEffect.shader

Shader "Custom/SuperhotTVEffect"
{
    Properties
    {
        _MainTex             ("Base (RGB)", 2D)    = "white" {}
        _ScanlineCount       ("Scanline Count", Range(10,256)) = 120
        _ScanlineIntensity   ("Scanline Intensity", Range(0,0.1)) = 0.03
        _ScanlineColor       ("Scanline Color", Color)           = (1,0,0,1)
        _ScanlineScrollSpeed ("Scanline Scroll Speed", Range(-2,2)) = 0.5

        _FlickerSpeed        ("Flicker Speed", Range(0.5,5))     = 2.0
        _FlickerAmount       ("Flicker Amount", Range(0,0.1))    = 0.02

        _GlitchChance        ("Glitch Chance", Range(0,1))       = 0.02
        _GlitchStrength      ("Glitch Strength", Range(0,0.2))    = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float    _ScanlineCount, _ScanlineIntensity;
            float4   _ScanlineColor;
            float    _ScanlineScrollSpeed;
            float    _FlickerSpeed, _FlickerAmount;
            float    _GlitchChance, _GlitchStrength;

            // simple hash for noise
            float hash12(float2 p)
            {
                float h = dot(p, float2(127.1, 311.7));
                return frac(sin(h) * 43758.5453);
            }

            float4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y;

                // 1) occasional horizontal glitch bar
                float glitch = step(1.0 - _GlitchChance,
                    hash12(float2(0, uv.y + floor(time * 10))));
                float offset = (hash12(uv + time) - 0.5)
                               * _GlitchStrength * glitch;
                uv.x += offset;

                // 2) sample
                float4 col = tex2D(_MainTex, uv);

                // 3) vertically scrolling scanlines
                float scroll = time * _ScanlineScrollSpeed;
                float v = uv.y + scroll;
                float scan = sin(v * _ScanlineCount * UNITY_PI)
                             * _ScanlineIntensity;
                col.rgb -= _ScanlineColor.rgb * scan;

                // 4) slight flicker
                float flick = 1.0 + sin(time * _FlickerSpeed) * _FlickerAmount;
                col.rgb *= flick;

                return col;
            }
            ENDCG
        }
    }
}
