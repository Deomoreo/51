// Rettangolo arrotondato SDF condiviso da UI51/Shape e UI51/Banner (vertici generati da UI51Shape.cs).
// uv0.xy = posizione locale dal centro (unita' canvas, y in alto); uv0.zw non usati
// uv1    = (mezza larghezza, mezza altezza, sfumatura ombra | spessore bordo, alfa riempimento)
// uv2    = raggi (alto-dx, basso-dx, alto-sx, basso-sx)
// uv3    = ombra: (centro forma - centro ombra, 1, 0) · forma: (RGB bordo 24 bit, alfa bordo, 0, 0)
// Niente normal/tangent: il canvas li ruota e scala insieme all'oggetto.
// color.rgb = riempimento, color.a = alfa ereditato (colore Graphic, CanvasGroup, CanvasRenderer),
// che la UI moltiplica solo nel colore dei vertici: qui lo si applica anche a bordo e ombra.
#ifndef UI51_SHAPE_INCLUDED
#define UI51_SHAPE_INCLUDED
#include "UnityCG.cginc"
#include "UnityUI.cginc"
struct ui51_appdata { float4 vertex : POSITION; float4 color : COLOR; float4 uv0 : TEXCOORD0; float4 uv1 : TEXCOORD1; float4 uv2 : TEXCOORD2; float4 uv3 : TEXCOORD3; };
struct ui51_v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float4 p : TEXCOORD0; float4 box : TEXCOORD1; float4 radii : TEXCOORD2; float4 extra : TEXCOORD3; fixed4 border : TEXCOORD4; float4 world : TEXCOORD5; };
float4 _ClipRect;
fixed4 _Color;
ui51_v2f ui51_vert(ui51_appdata v)
{
    ui51_v2f o;
    o.world = v.vertex;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.color = v.color * _Color;
    o.p = float4(v.uv0.xy, v.uv1.w, 0.0);
    o.box = float4(v.uv1.xyz, v.uv3.z);
    o.radii = v.uv2;
    o.extra = float4(v.uv3.xy, 0.0, 0.0);
    // RGB del bordo impacchettato in un intero a 24 bit (esatto in float32), decodificato nel vertex shader.
    float rgb = v.uv3.x;
    float r = floor(rgb / 65536.0);
    float g = floor((rgb - r * 65536.0) / 256.0);
    float b = rgb - r * 65536.0 - g * 256.0;
    o.border = v.uv3.z > 0.5 ? fixed4(0, 0, 0, 0) : fixed4(float3(r, g, b) / 255.0, v.uv3.y);
    return o;
}
// Riempimento dal colore dei vertici (Shape; nel quad dell'ombra e' il colore dell'ombra).
fixed4 ui51_vertexFill(ui51_v2f i) { return fixed4(i.color.rgb, i.p.z * i.color.a); }
float ui51_sdRoundBox(float2 p, float2 b, float4 r)
{
    r.xy = (p.x > 0.0) ? r.xy : r.zw;
    r.x = (p.y > 0.0) ? r.x : r.y;
    float2 q = abs(p) - b + r.x;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
}
fixed4 ui51_shape(ui51_v2f i, fixed4 fill)
{
    float d = ui51_sdRoundBox(i.p.xy, i.box.xy, i.radii);
    float aa = max(fwidth(d), 1e-4);
    fixed4 c;
    if (i.box.w > 0.5)
    {
        // Ombra: bordo sfumato (smoothstep ~ gaussiana sigma = blur/2) e ritagliata sotto la forma, come box-shadow.
        c = ui51_vertexFill(i);
        c.a *= 1.0 - smoothstep(-i.box.z, i.box.z, d);
        c.a *= saturate(0.5 + ui51_sdRoundBox(i.p.xy - i.extra.xy, i.box.xy, i.radii) / aa);
    }
    else
    {
        c = fill;
        if (i.box.z > 0.0)
        {
            fixed4 b = i.border;
            b.a *= i.color.a * saturate(0.5 + (d + i.box.z) / aa);
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
