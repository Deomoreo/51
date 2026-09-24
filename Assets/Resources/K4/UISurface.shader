Shader "Project51/UI/Surface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Mode ("Sweep, silhouette, background, foil", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float2 local:TEXCOORD1; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float2 local:TEXCOORD1; float4 world:TEXCOORD2; };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd;
            float4 _ClipRect;
            float _Mode;
            v2f vert(appdata v)
            {
                v2f o; o.world=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex);
                o.color=v.color*_Color; o.uv=v.uv; o.local=v.local; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=(tex2D(_MainTex,i.uv)+_TextureSampleAdd)*i.color;
                float t=_Time.y;
                if (_Mode < 0.5)
                {
                    float phase=frac(t*0.16)*3.5-1.0;
                    float band=1-smoothstep(0.015,0.13,abs(i.local.x+i.local.y*0.28-phase));
                    c.rgb+=fixed3(1,0.91,0.68)*band*0.23;
                }
                else if (_Mode < 1.5) c.rgb=i.color.rgb;
                else if (_Mode < 2.5)
                {
                    float wave=sin(i.local.x*5+i.local.y*3+t*0.16)*0.5+0.5;
                    float light=pow(saturate(1-length((i.local-float2(0.5+sin(t*0.09)*0.12,0.56))*float2(1.2,0.9))),2);
                    c.rgb+=fixed3(0.012,0.06,0.07)*light*(0.55+wave*0.45);
                }
                else
                {
                    float3 rainbow=0.5+0.5*sin((i.local.x*5+i.local.y*3+t*0.6)+float3(0,2.094,4.188));
                    c.rgb+=rainbow*0.13;
                }
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-0.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
