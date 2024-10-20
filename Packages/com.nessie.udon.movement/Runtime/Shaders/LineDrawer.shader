﻿Shader "Hidden/Nessie/Debug/Line Drawer"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    struct Attributes
    {
        float4 vertex : POSITION;
        fixed4 color : COLOR;
        float2 texcoord : TEXCOORD0;

        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 vertex : SV_POSITION;
        fixed4 color : COLOR;
        float2 texcoord : TEXCOORD0;

        UNITY_VERTEX_OUTPUT_STEREO
    };

    sampler2D _MainTex;
    float4 _MainTex_ST;

    fixed4 _TintColor;

    Varyings Vert(Attributes v)
    {
        Varyings o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        o.vertex = UnityObjectToClipPos(v.vertex);
        o.color = v.color;
        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

        return o;
    }

    fixed4 PostFrag(fixed4 color);

    fixed4 Frag(Varyings i) : SV_Target
    {
        fixed4 color = i.color * tex2D(_MainTex, i.texcoord) * i.color.a;
        return PostFrag(color);
    }

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
                Name "In Front"

                ZTest LEqual

                CGPROGRAM

                #pragma target 2.0

                #pragma multi_compile_particles

                #pragma vertex Vert
                #pragma fragment Frag

                fixed4 PostFrag(fixed4 color) : SV_Target
                {
                    return color;
                }

                ENDCG
            }

            Pass
            {
                Name "Behind"

                ZTest Greater

                CGPROGRAM

                #pragma target 2.0

                #pragma multi_compile_particles

                #pragma vertex Vert
                #pragma fragment Frag

                fixed4 PostFrag(fixed4 color) : SV_Target
                {
                    return color * 0.1;
                }

                ENDCG
            }
        }
    }
}