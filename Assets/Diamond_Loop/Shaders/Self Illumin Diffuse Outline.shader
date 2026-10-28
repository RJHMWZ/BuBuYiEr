Shader "ClawbearGames/Self Illumin Diffuse Outline" 
{
    Properties 
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0,2)) = 0
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2f
    {
        float4 pos : SV_POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
        UNITY_DEFINE_INSTANCED_PROP(float4, _OutlineColor)
        UNITY_DEFINE_INSTANCED_PROP(float, _OutlineWidth)
    UNITY_INSTANCING_BUFFER_END(Props)

    v2f vert(appdata v)
    {
        UNITY_SETUP_INSTANCE_ID(v);
        v2f o;
        UNITY_TRANSFER_INSTANCE_ID(v, o);

        float outlineWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _OutlineWidth);
        float3 offset = normalize(v.normal) * outlineWidth;
        v.vertex.xyz += offset;
        o.pos = UnityObjectToClipPos(v.vertex);
        return o;
    }

    half4 frag(v2f i) : SV_Target
    {
        UNITY_SETUP_INSTANCE_ID(i);
        return UNITY_ACCESS_INSTANCED_PROP(Props, _OutlineColor);
    }

    ENDCG

    SubShader 
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        // ================= OUTLINE PASS =================
        Pass
        {
            ZWrite Off
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDCG
        }

        // z-prepass removed — was breaking GPU instancing batching

        // ================= SURFACE PASS =================
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows
        #pragma multi_compile_instancing

        sampler2D _MainTex;

        struct Input 
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutput o) 
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
            fixed4 c = tex * color;
            o.Albedo = c.rgb;
            o.Emission = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }

    FallBack "Mobile/Diffuse"
}
