Shader "BuBuYiEr/Candy Ink"
{
    Properties
    {
        _Color ("Candy Color", Color) = (1, .85, .8, 1)
        _Ink ("Chocolate Ink", Color) = (.322, .165, .039, 1)
        _Outline ("Ink Width", Range(0, .03)) = .007
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ChocolateOutline"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Outline;
            fixed4 _Ink;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            float4 vert(appdata v) : SV_POSITION
            {
                v.vertex.xyz += normalize(v.normal) * _Outline;
                return UnityObjectToClipPos(v.vertex);
            }
            fixed4 frag() : SV_Target { return _Ink; }
            ENDCG
        }
        Pass
        {
            Name "SoftCandy"
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            fixed4 _Ink;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 vertex : SV_POSITION; float shade : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float light = saturate(dot(UnityObjectToWorldNormal(v.normal), normalize(float3(-.35, .85, -.4))));
                o.shade = light > .65 ? 1 : light > .2 ? .94 : .84;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(lerp(_Ink.rgb, _Color.rgb, i.shade), 1);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
