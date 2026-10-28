Shader "ClawbearGames/Rim Light Transparent"
{
    Properties 
    {
        _InnerColor ("Inner Color", Color) = (0.8, 0.95, 1.0, 0.6)
        _RimColor ("Rim Color", Color) = (0.4, 0.8, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.1, 8.0)) = 4.0
        _Alpha ("Alpha", Range(0.1, 1.0)) = 0.7
        _FresnelStrength ("Fresnel Strength", Range(0.0, 2.0)) = 1.0
    }

    SubShader 
    {
        Tags { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
            "IgnoreProjector" = "True"
        }
        
        Cull Back
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        CGPROGRAM
        #pragma surface surf Lambert alpha:fade
        #pragma multi_compile_instancing
        
        struct Input 
        {
            float3 viewDir;
        };

        // ✅ Instancing buffer
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _InnerColor)
            UNITY_DEFINE_INSTANCED_PROP(float4, _RimColor)
            UNITY_DEFINE_INSTANCED_PROP(float, _RimPower)
            UNITY_DEFINE_INSTANCED_PROP(float, _Alpha)
            UNITY_DEFINE_INSTANCED_PROP(float, _FresnelStrength)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutput o) 
        {
            float3 viewDir = normalize(IN.viewDir);

            // Fresnel
            half fresnel = 1.0 - saturate(dot(viewDir, o.Normal));

            // Access instanced properties
            float4 innerColor = UNITY_ACCESS_INSTANCED_PROP(Props, _InnerColor);
            float4 rimColor = UNITY_ACCESS_INSTANCED_PROP(Props, _RimColor);
            float rimPower = UNITY_ACCESS_INSTANCED_PROP(Props, _RimPower);
            float alphaBase = UNITY_ACCESS_INSTANCED_PROP(Props, _Alpha);
            float fresnelStrength = UNITY_ACCESS_INSTANCED_PROP(Props, _FresnelStrength);

            // Base
            o.Albedo = innerColor.rgb;

            // Rim
            half rim = pow(fresnel, rimPower);
            o.Emission = rimColor.rgb * rim * 2.0;

            // Alpha
            float alphaFresnel = lerp(alphaBase * 0.3, alphaBase, pow(fresnel, 0.5) * fresnelStrength);
            o.Alpha = saturate(alphaFresnel + rim * 0.3);
        }
        ENDCG
    } 

    Fallback "Mobile/Diffuse"
}