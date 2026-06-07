Shader "Custom/SquigglyFog"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0.6,0.6,0.7,1)

        _Speed ("Speed", Float) = 0.2
        _Amplitude ("Amplitude", Float) = 0.08
        _Frequency ("Frequency", Float) = 5

        _Opacity ("Opacity", Range(0,1)) = 0.5

        _EdgeFade ("Edge Fade", Range(0.01,0.5)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _FogColor;
            float _Speed;
            float _Amplitude;
            float _Frequency;
            float _Opacity;
            float _EdgeFade;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float FogBand(float2 uv, float offset)
            {
                float wave =
                    sin(uv.x * _Frequency + _Time.y * _Speed + offset) * _Amplitude +
                    sin(uv.x * (_Frequency * 0.5) + _Time.y * (_Speed * 0.7)) * (_Amplitude * 0.5);

                float center = 0.5 + wave + offset * 0.08;

                float band =
                    smoothstep(center - 0.08, center, uv.y) -
                    smoothstep(center, center + 0.08, uv.y);

                return band;
            }

            float EdgeMask(float2 uv)
            {
                float left = smoothstep(0, _EdgeFade, uv.x);
                float right = smoothstep(0, _EdgeFade, 1 - uv.x);

                float bottom = smoothstep(0, _EdgeFade, uv.y);
                float top = smoothstep(0, _EdgeFade, 1 - uv.y);

                return left * right * bottom * top;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float fog = 0;

                fog += FogBand(i.uv, 0.0);
                fog += FogBand(i.uv, 1.3);
                fog += FogBand(i.uv, 2.6);
                fog += FogBand(i.uv, 3.9);

                fog = saturate(fog);

                // texture support
                float4 tex = tex2D(_MainTex, i.uv);

                // soft edge fade
                float edgeMask = EdgeMask(i.uv);

                float finalAlpha = fog * _Opacity * tex.a * edgeMask;

                return float4(_FogColor.rgb * tex.rgb, finalAlpha);
            }

            ENDCG
        }
    }
}