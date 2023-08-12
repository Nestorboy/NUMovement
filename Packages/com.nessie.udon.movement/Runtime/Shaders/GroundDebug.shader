Shader "Hidden/Nessie/Debug/Ground"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Opacity ("Opacity", Range(0, 1)) = 0.25
        
        [Header(Ground Check)]
        _GroundAngle ("Ground Angle (Degrees)", Range(0, 90)) = 50
        _InvalidColor ("Invalid Color", Color) = (1,0,0,1)
        _GroundColor ("Ground Color", Color) = (1,1,0,1)
        _WalkableColor ("Walkable Color", Color) = (0,1,0,1)
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        //ColorMask RGB

        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                nointerpolation float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldDir(v.normal);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed _Opacity;
            fixed _GroundAngle;
            fixed4 _InvalidColor, _GroundColor, _WalkableColor;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                float angle = degrees(acos(i.normal.y));
                bool ground = angle < 90;
                bool walkable = ground && angle < _GroundAngle;
                float3 surfaceColor = ground ? walkable ? _WalkableColor : _GroundColor : _InvalidColor;
                
                return float4(col * surfaceColor, _Opacity);
            }
            ENDCG
        }
    }
}
