// Fiamma UI che si muove: onde orizzontali che salgono e crescono verso la punta (la base resta
// ferma sul braciere), allungamento e luminosita' che tremolano. Struttura di UI/Default (maschere,
// RectMask2D). Il seme viene dalla posizione: due fiamme con lo stesso materiale non vanno in sincrono.
// _UIV2Still = 1 (Grafica ridotta, da UIV2AmbientFloat) = posa ferma dello sprite.
Shader "UIV2/FlameWobble"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BaseY ("Base (uv.y)", Range(0,1)) = 0.05
        _TipY ("Tip (uv.y)", Range(0,1)) = 0.95
        _Sway ("Sway (uv)", Range(0,0.1)) = 0.035
        _Speed ("Speed", Float) = 1

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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; float4 worldPosition : TEXCOORD1; float seed : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };

            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd;
            float4 _ClipRect;
            float _BaseY, _TipY, _Sway, _Speed, _UIV2Still;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                float3 pivot = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                o.seed = frac(pivot.x * 0.0137 + pivot.y * 0.0071) * 6.2832;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float motion = 1 - saturate(_UIV2Still);
                float t = _Time.y * _Speed + i.seed;
                float2 uv = i.texcoord;
                float h = saturate((uv.y - _BaseY) / (_TipY - _BaseY));
                float w = h * h * motion;

                // Allungamento dal braciere: la punta sale e scende, la base non si sposta.
                float stretch = 1 + 0.07 * motion * (sin(t * 9.1) * 0.6 + sin(t * 14.3 + 1.7) * 0.4);
                uv.y = _BaseY + (uv.y - _BaseY) / stretch;
                // Lingue che salgono: onde piu' fitte e veloci in alto.
                uv.x += _Sway * w * (sin(uv.y * 11 - t * 7.3) + 0.55 * sin(uv.y * 23 - t * 12.1 + 2.1));

                fixed4 color = (tex2D(_MainTex, uv) + _TextureSampleAdd) * i.color;
                color.rgb *= 1 + 0.12 * motion * sin(t * 17.7 + 0.8) * sin(t * 5.3);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
