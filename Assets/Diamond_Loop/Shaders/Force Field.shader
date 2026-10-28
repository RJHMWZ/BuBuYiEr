Shader "ClawbearGames/Force Field"
{
    Properties
    {
		_PatternTex ("Pattern Tex", 2D) = "white" {}
		[HDR]_EmissionColor ("Emission Color", Color) = (0.5,0.5,0.5,0)
        _FresnelPower("Fresnel Power", Float) = 1
        _ScrollSpeed("Scroll Speed", Float) = 1
        _AlphaStrength("Alpha Strength", Range(0.0, 10.0)) = 1
    }

    SubShader
    {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

		Cull Off

        LOD 200

        CGPROGRAM

        #pragma surface surf ForceField noshadow noambient novertexlights nolightmap nodynlightmap nodirlightmap nofog nometa noforwardadd noshadowmask alpha:fade
        #pragma target 3.0

		half4 _EmissionColor;

		half _FresnelPower;

		half _ScrollSpeed;

        sampler2D _PatternTex;

		half _AlphaStrength;

		uniform sampler2D _CameraDepthTexture;

		#include "UnityCG.cginc"
		#include "UnityPBSLighting.cginc"

        struct Input
        {
            float2 uv_PatternTex;
			float3 worldNormal;
			float3 worldPos;
			float3 viewDir;
			float4 screenPos;
			INTERNAL_DATA
        };

		inline half4 LightingForceField(SurfaceOutputStandard s, float3 viewDir, UnityGI gi)
		{
			s.Normal = normalize(s.Normal);

			half oneMinusReflectivity;
			half3 specColor;
			s.Albedo = DiffuseAndSpecularFromMetallic(s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

			half outputAlpha;
			s.Albedo = PreMultiplyAlpha(s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

			half4 c = UNITY_BRDF_PBS(s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
			c.a = outputAlpha;
			return c;
		}

		inline void LightingForceField_GI(
			SurfaceOutputStandard s,
			UnityGIInput data,
			inout UnityGI gi)
		{
#if defined(UNITY_PASS_DEFERRED) && UNITY_ENABLE_REFLECTION_BUFFERS
			gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal);
#else
			Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(s.Smoothness, data.worldViewDir, s.Normal, lerp(unity_ColorSpaceDielectricSpec.rgb, s.Albedo, s.Metallic));
			gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal, g);
#endif
		}

		inline half ComputeDepthDelta(Input IN)
		{
			float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
			float screenDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV));
			float depthDelta = abs(screenDepth - IN.screenPos.w);
			depthDelta = 1 - depthDelta;

			return smoothstep(0, 1, depthDelta);
		}

		inline half ComputeFresnel(Input IN)
		{
			half rim = dot(IN.viewDir, IN.worldNormal);
			rim = lerp(1 - saturate(rim), 0, rim < 0);
			return pow(rim, _FresnelPower);
		}

		inline half ComputePattern(Input IN)
		{
			float2 uv = IN.uv_PatternTex + _Time.y * _ScrollSpeed;
			return tex2D(_PatternTex, uv).r;
		}

		half ForceFieldAlpha(Input IN, inout SurfaceOutputStandard o)
		{
			half depthDelta = ComputeDepthDelta(IN);
			half fresnel = ComputeFresnel(IN);
			half pattern = ComputePattern(IN);

			return (depthDelta + fresnel) * pattern * _AlphaStrength;
		}

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			half alpha = ForceFieldAlpha(IN, o);

			o.Albedo = half3(0, 0, 0);
			o.Occlusion = 1;
			o.Emission = _EmissionColor.rgb;
            o.Alpha = alpha;
        }
        ENDCG
    }
	FallBack "Mobile/Diffuse"
}
