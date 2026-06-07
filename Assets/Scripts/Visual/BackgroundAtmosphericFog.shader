Shader "Custom/CreamyAtmosphericFog"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}

        _FogColor ("Fog Color", Color) = (0.75, 0.85, 0.85, 1)

        _ScrollSpeed1 ("Scroll Speed 1", Float) = 0.01
        _ScrollSpeed2 ("Scroll Speed 2", Float) = 0.005

        _NoiseScale1 ("Noise Scale 1", Float) = 1.0
        _NoiseScale2 ("Noise Scale 2", Float) = 0.5

        _Softness ("Softness", Range(0.1, 3)) = 1.5
        _Opacity ("Opacity", Range(0,1)) = 0.35

        _VerticalDensity ("Vertical Density", Range(0,3)) = 1.5
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
            float _ScrollSpeed1;
            float _ScrollSpeed2;
            float _NoiseScale1;
            float _NoiseScale2;
            float _Softness;
            float _Opacity;
            float _VerticalDensity;

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
                o.uv = v.uv;
                return o;
            }

            float SampleLoopingNoise(float2 uv, float scale, float speed)
            {
                uv *= scale;

                // Proper looping
                uv = frac(uv);

                uv.x += _Time.y * speed;
                uv.x = frac(uv.x);

                return tex2D(_MainTex, uv).r;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float noise1 = SampleLoopingNoise(
                    i.uv,
                    _NoiseScale1,
                    _ScrollSpeed1
                );

                float noise2 = SampleLoopingNoise(
                    i.uv + float2(0.37, 0.12),
                    _NoiseScale2,
                    -_ScrollSpeed2
                );

                float noise3 = SampleLoopingNoise(
                    i.uv + float2(0.61, 0.43),
                    _NoiseScale2 * 0.75,
                    _ScrollSpeed2 * 0.5
                );

                float combined =
                    noise1 * 0.5 +
                    noise2 * 0.3 +
                    noise3 * 0.2;

                // soften transitions
                combined = smoothstep(
                    0.2,
                    _Softness,
                    combined
                );

                // more fog lower in screen
                float verticalDensity =
                    pow(1.0 - i.uv.y, _VerticalDensity);

                float alpha =
                    combined *
                    verticalDensity *
                    _Opacity;

                return float4(
                    _FogColor.rgb,
                    alpha
                );
            }

            ENDCG
        }
    }
}