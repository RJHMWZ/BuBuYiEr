Shader "ClawbearGames/Mobile Diffuse Transparent" 
{
    Properties 
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader 
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        LOD 200

        // z-prepass removed — was breaking GPU instancing batching

        CGPROGRAM
        #pragma surface surf Lambert alpha:fade
        #pragma multi_compile_instancing

        sampler2D _MainTex;

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
        UNITY_INSTANCING_BUFFER_END(Props)

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutput o) 
        {
            fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 c = tex * color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Mobile/Diffuse"
}
