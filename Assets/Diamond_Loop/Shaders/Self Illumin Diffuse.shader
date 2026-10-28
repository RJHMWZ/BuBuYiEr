Shader "ClawbearGames/Self Illumin Diffuse" 
{
    Properties 
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.8
        _Glossiness ("Smoothness", Range(0,1)) = 0.75
        _EmissionStrength ("Emission Strength", Range(0,2)) = 1.0
    }
    SubShader 
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma multi_compile_instancing      // ← enables GPU instancing + MPB

        sampler2D _MainTex;
        half _Metallic;
        half _Glossiness;
        half _EmissionStrength;

        // ← _Color moved here instead of plain fixed4
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
        UNITY_INSTANCING_BUFFER_END(Props)

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o) 
        {
            fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);  // ← read from MPB
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 c = tex * color;
            o.Albedo = c.rgb;
            o.Emission = c.rgb * _EmissionStrength;
            o.Alpha = c.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Mobile/Diffuse"
}
