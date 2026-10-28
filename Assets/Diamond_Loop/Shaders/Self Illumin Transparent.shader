Shader "ClawbearGames/Self Illumin Transparent" 
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
        // NOTE: if objects show through themselves (self-transparency artifact),
        // you can re-add it but batching will go back up for those objects

        CGPROGRAM
        #pragma surface surf Lambert alpha:fade
        #pragma multi_compile_instancing        // ← added

        sampler2D _MainTex;

        // ← _Color moved from plain fixed4 into instancing buffer
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
            o.Emission = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Mobile/Diffuse"
}
