// Rettangolo arrotondato SDF condiviso da UI51/Shape e UI51/Banner (vertici generati da UI51Shape.cs).
// uv0.xy = posizione locale dal centro (unita' canvas, y in alto)
// uv1    = (mezza larghezza, mezza altezza, sfumatura ombra, spessore bordo)
// uv2    = raggi (alto-dx, basso-dx, alto-sx, basso-sx)
// uv3    = (centro forma - centro ombra, 1 se e' il quad dell'ombra)
// tangent = colore bordo, color = riempimento
#ifndef UI51_SHAPE_INCLUDED
#define UI51_SHAPE_INCLUDED

#include "UnityCG.cginc"
#include "UnityUI.cginc"

struct ui51_appdata
{
    float4 vertex : POSITION;
    float4 color : COLOR;
    float4 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    float4 uv2 : TEXCOORD2;
    float4 uv3 : TEXCOORD3;
    float4 tangent : TANGENT;
};

struct ui51_v2f
{
    float4 vertex : SV_POSITION;
    float4 color : COLOR;
    float2 p : TEXCOORD0;
    float4 box : TEXCOORD1;
    float4 radii : TEXCOORD2;
    float4 shadow : TEXCOORD3;
    float4 border : TEXCOORD4;
    float4 world : TEXCOORD5;
};

float4 _ClipRect;
fixed4 _Color;

ui51_v2f ui51_vert(ui51_appdata v)
{
    ui51_v2f o;
    o.world = v.vertex;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.color = v.color * _Color;
    o.p = v.uv0.xy;
    o.box = v.uv1;
    o.radii = v.uv2;
    o.shadow = v.uv3;
    o.border = v.tangent;
    return o;
}

// Inigo Quilez, sdRoundBox con raggio per angolo.
float ui51_sdRoundBox(float2 p, float2 b, float4 r)
{
    r.xy = (p.x > 0.0) ? r.xy : r.zw;
    r.x = (p.y > 0.0) ? r.x : r.y;
    float2 q = abs(p) - b + r.x;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
}

// fill = colore gia' calcolato per questo pixel (non premoltiplicato).
fixed4 ui51_shape(ui51_v2f i, fixed4 fill)
{
    float d = ui51_sdRoundBox(i.p, i.box.xy, i.radii);
    float aa = max(fwidth(d), 1e-4);
    fixed4 c;
    if (i.shadow.z > 0.5)
    {
        // box-shadow CSS: sfumato fuori, e mai visibile sotto la forma (pannelli semitrasparenti).
        c = i.color;
        c.a *= 1.0 - smoothstep(-i.box.z, i.box.z, d);
        c.a *= saturate(0.5 + ui51_sdRoundBox(i.p - i.shadow.xy, i.box.xy, i.radii) / aa);
    }
    else
    {
        c = fill;
        if (i.box.w > 0.0)
        {
            // Bordo CSS: disegnato sopra lo sfondo (background-clip: border-box).
            fixed4 b = i.border;
            b.a *= saturate(0.5 + (d + i.box.w) / aa);
            float a = b.a + c.a * (1.0 - b.a);
            c.rgb = (b.rgb * b.a + c.rgb * c.a * (1.0 - b.a)) / max(a, 1e-4);
            c.a = a;
        }
        c.a *= saturate(0.5 - d / aa);
    }
    #ifdef UNITY_UI_CLIP_RECT
    c.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(c.a - 0.001);
    #endif
    return c;
}

#endif
