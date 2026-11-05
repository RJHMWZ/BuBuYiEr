Shader "BuBuYiEr/Candy Pixels"
{
    Properties
    {
        _Color ("Candy Color", Color) = (1, .85, .8, 1)
        _Ink ("Chocolate Ink", Color) = (.322, .165, .039, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            UNITY_INSTANCING_BUFFER_START(Candy)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_INSTANCING_BUFFER_END(Candy)
            fixed4 _Ink;
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float light = saturate(dot(UnityObjectToWorldNormal(v.normal), normalize(float3(-.35, .85, -.4))));
                float shade = light > .65 ? 1 : light > .2 ? .96 : .88;
                fixed4 candy = UNITY_ACCESS_INSTANCED_PROP(Candy, _Color);
                o.color = fixed4(lerp(_Ink.rgb, candy.rgb, shade), 1);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target { return i.color; }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
