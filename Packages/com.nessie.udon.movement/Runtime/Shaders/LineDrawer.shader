Shader "Hidden/Nessie/Debug/Line Drawer"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
    }
    
    CGINCLUDE

    #include "UnityCG.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        fixed4 color : COLOR;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        fixed4 color : COLOR;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    sampler2D _MainTex;
    fixed4 _TintColor;

    float4 _MainTex_ST;

    v2f vert (appdata v)
    {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.color = v.color;
        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
        return o;
    }

    ENDCG

    CGINCLUDE
    ENDCG

    Category
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }
        
        Blend One OneMinusSrcAlpha
        ColorMask RGB
        
        Cull Off
        Lighting Off
        ZWrite Off
    
        SubShader
        {
            Pass
            {
                ZTest LEqual
                
                CGPROGRAM
                
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                #pragma multi_compile_particles
    
                fixed4 frag (v2f i) : SV_Target
                {
                    return i.color * tex2D(_MainTex, i.texcoord) * i.color.a;
                }
                
                ENDCG
            }
            
            Pass
            {
                ZTest Greater
                
                CGPROGRAM
                
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                #pragma multi_compile_particles
    
                fixed4 frag (v2f i) : SV_Target
                {
                    return i.color * tex2D(_MainTex, i.texcoord) * i.color.a * 0.1;
                }
                
                ENDCG
            }
        }
    }
}