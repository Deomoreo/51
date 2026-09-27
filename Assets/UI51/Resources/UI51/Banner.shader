// Banner del giocatore (SPEC §7): gradiente fino a 6 stop, scorrimento (aurora), puntini luminosi (stellato),
// riflesso diagonale (oro). Stessa forma/bordo/ombre di UI51/Shape: va usato su un UI51Shape con riempimento bianco
// (il colore dei vertici fa da tinta). Parametri scritti da UI51Banners.CreateMaterial. _UI51Still = 1 ferma tutto
// (grafica ridotta). Tempi e curve dai @keyframes dei mockup (bnAurora, bnStars, bnSheen).
Shader "UI51/Banner"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _C0 ("Color 0", Color) = (1,1,1,1)
        _C1 ("Color 1", Color) = (1,1,1,1)
        _C2 ("Color 2", Color) = (1,1,1,1)
        _C3 ("Color 3", Color) = (1,1,1,1)
        _C4 ("Color 4", Color) = (1,1,1,1)
        _C5 ("Color 5", Color) = (1,1,1,1)
        _Stops0 ("Stops 0-3", Vector) = (0,1,1,1)
        _Stops1 ("Stop 4, stop 5, count, angle (CSS deg)", Vector) = (1,1,2,180)
        _Anim ("Background size, scroll period (s)", Vector) = (1,0,0,0)
        _Sheen ("Sheen period (s), alpha, width, skew (deg)", Vector) = (3,0,0.45,20)
        _SheenColor ("Sheen color", Color) = (1,0.9255,0.6667,1)
        _Stars ("Star count, drift period (s)", Vector) = (0,5,0,0)
        _DotA0 ("Dot 0 x, y, radius, tile", Vector) = (0,0,0,1)
        _DotA1 ("Dot 1", Vector) = (0,0,0,1)
        _DotA2 ("Dot 2", Vector) = (0,0,0,1)
        _DotA3 ("Dot 3", Vector) = (0,0,0,1)
        _DotA4 ("Dot 4", Vector) = (0,0,0,1)
        _DotB0 ("Dot 0 drift x, drift y, gold", Vector) = (0,0,0,0)
        _DotB1 ("Dot 1", Vector) = (0,0,0,0)
        _DotB2 ("Dot 2", Vector) = (0,0,0,0)
        _DotB3 ("Dot 3", Vector) = (0,0,0,0)
        _DotB4 ("Dot 4", Vector) = (0,0,0,0)
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
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex ui51_vert
            #pragma fragment frag
            #pragma target 2.5
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "Assets/UI51/Shaders/UI51Shape.cginc"

            fixed4 _C0, _C1, _C2, _C3, _C4, _C5, _SheenColor;
            float4 _Stops0, _Stops1, _Anim, _Sheen, _Stars;
            float4 _DotA0, _DotA1, _DotA2, _DotA3, _DotA4, _DotB0, _DotB1, _DotB2, _DotB3, _DotB4;
            float _UI51Still;

            // cubic-bezier CSS (x -> y) con Newton; ease = (.25,.1,.25,1), ease-in-out = (.42,0,.58,1).
            float bezierComp(float u, float a, float b) { float v = 1.0 - u; return 3.0 * v * v * u * a + 3.0 * v * u * u * b + u * u * u; }
            float cssEase(float x, float4 k)
            {
                float u = x;
                for (int n = 0; n < 5; n++)
                {
                    float v = 1.0 - u;
                    float dx = 3.0 * v * v * k.x + 6.0 * v * u * (k.z - k.x) + 3.0 * u * u * (1.0 - k.z);
                    u = saturate(u - (bezierComp(u, k.x, k.z) - x) / max(dx, 1e-3));
                }
                return bezierComp(u, k.y, k.w);
            }
            #define EASE float4(0.25, 0.1, 0.25, 1.0)
            #define EASE_IN_OUT float4(0.42, 0.0, 0.58, 1.0)

            fixed4 stop(fixed4 c, fixed4 next, float t, float from, float to, float k)
            {
                float f = saturate((t - from) / max(to - from, 1e-4));
                return lerp(c, next, f * step(k + 0.5, _Stops1.z));
            }
            fixed4 gradient(float t)
            {
                fixed4 c = _C0;
                c = stop(c, _C1, t, _Stops0.x, _Stops0.y, 1.0);
                c = stop(c, _C2, t, _Stops0.y, _Stops0.z, 2.0);
                c = stop(c, _C3, t, _Stops0.z, _Stops0.w, 3.0);
                c = stop(c, _C4, t, _Stops0.w, _Stops1.x, 4.0);
                c = stop(c, _C5, t, _Stops1.x, _Stops1.y, 5.0);
                return c;
            }
            // Puntino di radial-gradient(r r at x% y%, colore 50%, transparent) su uno sfondo ripetuto di lato tile*100%.
            fixed3 star(fixed3 c, float2 pd, float2 size, float e, float4 a, float4 b, float k)
            {
                float2 tile = size * a.w;
                float2 d = pd - (float2(a.x, a.y) * tile + b.xy * e);
                d -= tile * round(d / tile);
                float alpha = saturate(2.0 * (1.0 - length(d) / max(a.z, 1e-3))) * step(k + 0.5, _Stars.x);
                return lerp(c, lerp(fixed3(1, 1, 1), fixed3(0.988, 0.886, 0.604), b.z), alpha);
            }

            fixed4 frag(ui51_v2f i) : SV_Target
            {
                float2 size = i.box.xy * 2.0;
                float2 pd = float2(i.p.x + i.box.x, i.box.y - i.p.y); // dall'alto a sinistra, y in basso (come il CSS)
                float time = _UI51Still > 0.5 ? 0.0 : _Time.y;

                // Gradiente lineare CSS su un'immagine grande bgSize volte, spostata da background-position (bnAurora).
                float s = max(_Anim.x, 1.0);
                float pos = 0.0;
                if (_Anim.y > 0.0)
                {
                    float ph = frac(time / _Anim.y) * 2.0;
                    pos = ph < 1.0 ? cssEase(ph, EASE) : 1.0 - cssEase(ph - 1.0, EASE);
                }
                float ang = radians(_Stops1.w);
                float2 dir = float2(sin(ang), cos(ang));
                float2 q = float2(i.p.x + (s - 1.0) * size.x * (pos - 0.5), i.p.y);
                float len = s * (abs(size.x * dir.x) + abs(size.y * dir.y));
                fixed4 col = gradient(dot(q, dir) / max(len, 1e-3) + 0.5);

                // Stellato: bnStars 5 s ease-in-out alternate.
                if (_Stars.x > 0.5)
                {
                    float ph = frac(time / (2.0 * max(_Stars.y, 1e-3))) * 2.0;
                    float e = cssEase(ph < 1.0 ? ph : 2.0 - ph, EASE_IN_OUT);
                    col.rgb = star(col.rgb, pd, size, e, _DotA0, _DotB0, 0.0);
                    col.rgb = star(col.rgb, pd, size, e, _DotA1, _DotB1, 1.0);
                    col.rgb = star(col.rgb, pd, size, e, _DotA2, _DotB2, 2.0);
                    col.rgb = star(col.rgb, pd, size, e, _DotA3, _DotB3, 3.0);
                    col.rgb = star(col.rgb, pd, size, e, _DotA4, _DotB4, 4.0);
                }

                // Oro: ::after largo _Sheen.z, skewX(-_Sheen.w), left da -70% a 140% nel primo 60% del periodo (bnSheen).
                if (_Sheen.y > 0.0 && _UI51Still < 0.5)
                {
                    float k = saturate(frac(time / max(_Sheen.x, 1e-3)) / 0.6);
                    float left = lerp(-0.7, 1.4, cssEase(k, EASE_IN_OUT)) * size.x;
                    float x = pd.x - left + tan(radians(_Sheen.w)) * (pd.y - size.y * 0.5);
                    float u = x / max(_Sheen.z * size.x, 1e-3);
                    float a = _Sheen.y * saturate(1.0 - abs(2.0 * u - 1.0));
                    col.rgb = lerp(col.rgb, _SheenColor.rgb, a);
                }

                col.rgb *= i.color.rgb;
                col.a *= i.p.z * i.color.a;
                return ui51_shape(i, col);
            }
            ENDCG
        }
    }
}
