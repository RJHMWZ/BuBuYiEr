Shader "ClawbearGames/Self Illumin Diffuse Scroll" 
{
    Properties 
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _YScrollSpeed ("Y Scroll Speed", Float) = 1.0
        _XScrollSpeed ("X Scroll Speed", Float) = 1.0
    }
    SubShader 
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            ZWrite On
            ColorMask 0
        }

        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _MainTex;
        fixed4 _Color;
        float _YScrollSpeed;
        float _XScrollSpeed;

        struct Input {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o) 
        {
            // Scroll UVs based on time
            float2 scrolledUV = IN.uv_MainTex + float2(_YScrollSpeed * _Time.y, _XScrollSpeed * _Time.y);
            fixed4 tex = tex2D(_MainTex, scrolledUV);
            fixed4 c = tex * _Color;

            o.Albedo = c.rgb;
            o.Emission = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Mobile/Diffuse"
}
