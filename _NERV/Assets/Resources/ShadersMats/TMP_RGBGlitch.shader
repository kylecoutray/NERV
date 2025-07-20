Shader "UI/TMP/RGBGlitch"
{
    Properties
    {
        [PerRendererData]_MainTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,0)
        _OutlineWidth ("Outline Width", Range(0,1)) = 0

        _OffsetR("Red Offset", Vector) = (0,0,0,0)
        _OffsetG("Green Offset", Vector) = (0,0,0,0)
        _OffsetB("Blue Offset", Vector) = (0,0,0,0)

        _GlitchMask("Glitch Mask", 2D) = "white" {} // noise texture
        _GlitchThresh("Glitch Threshold", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex   : POSITION;
                float4 texcoord : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _FaceColor;
            sampler2D _GlitchMask;
            float4 _OffsetR, _OffsetG, _OffsetB;
            float _GlitchThresh;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.pos   = UnityObjectToClipPos(IN.vertex);
                OUT.uv    = IN.texcoord.xy;
                OUT.color = IN.color * _FaceColor;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // sample mask to see if we glitch here
                float mask = tex2D(_GlitchMask, IN.uv * 5).r; 
                bool doGlitch = mask > _GlitchThresh;

                // base UV
                float2 uv = IN.uv;

                // sample each channel with its own offset if glitching
                float2 offR = doGlitch ? _OffsetR.xy : float2(0,0);
                float2 offG = doGlitch ? _OffsetG.xy : float2(0,0);
                float2 offB = doGlitch ? _OffsetB.xy : float2(0,0);

                fixed4 colR = tex2D(_MainTex, uv + offR);
                fixed4 colG = tex2D(_MainTex, uv + offG);
                fixed4 colB = tex2D(_MainTex, uv + offB);

                // reconstruct only the channel bits
                fixed3 rgb;
                rgb.r = colR.r;
                rgb.g = colG.g;
                rgb.b = colB.b;

               // no SDF smoothing, just take the alpha channel
                float alpha = colR.a;
                return fixed4(rgb * IN.color.rgb, alpha * IN.color.a);
            }
            ENDCG
        }
    }
}
