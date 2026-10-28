Shader "ClawbearGames/Lit Surface"
{
    Properties
    {
        _GlowIntensity("Glow Intensity", Range(1, 5)) = 1
        _Smoothness("Smoothness", Range(0, 1)) = 0
        _Metallic("Metallic", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+0" "IsEmissive" = "true" }
        Cull Back

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard keepalpha addshadow fullforwardshadows exclude_path:deferred
        #pragma multi_compile_instancing    // ← added for batching

        // _GlowIntensity, _Metallic, _Smoothness moved into instancing buffer
        // so they can be set per-object via MPB if needed
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(half, _GlowIntensity)
            UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
            UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
        UNITY_INSTANCING_BUFFER_END(Props)

        struct Input
        {
            float4 vertexColor : COLOR;     // color comes from vertex paint, not MPB
        };

        void surf(Input i, inout SurfaceOutputStandard o)
        {
            half glowIntensity = UNITY_ACCESS_INSTANCED_PROP(Props, _GlowIntensity);
            float metallic = UNITY_ACCESS_INSTANCED_PROP(Props, _Metallic);
            float smoothness = UNITY_ACCESS_INSTANCED_PROP(Props, _Smoothness);

            o.Albedo = i.vertexColor.rgb;
            o.Emission = (i.vertexColor * glowIntensity).rgb;
            o.Metallic = metallic;
            o.Smoothness = smoothness;
            o.Alpha = 1;
        }

        ENDCG
    }
    FallBack "Mobile/Diffuse"
}
