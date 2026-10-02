"""Disegna Assets/UI51/Art/Common/tutorial_finger.png dal path SVG del dito nel mockup TutorialPartita
(svg.finger, viewBox 40x46, riempimento #F5E9D0, tratto #6B4418 da 2, ombra 0 4px 6px rgba(0,0,0,.6)).
PNG 240x264 = (40+20)x(46+20) unita' a 4 px: 10 unita' di margine per l'ombra."""
import math
from PIL import Image, ImageDraw, ImageFilter


def arc(p0, r, large, sweep, p1, n=24):
    (x1, y1), (x2, y2) = p0, p1
    dx, dy = (x1 - x2) / 2, (y1 - y2) / 2
    lam = (dx * dx + dy * dy) / (r * r)
    if lam > 1: r *= math.sqrt(lam)
    sign = -1 if large == sweep else 1
    co = sign * math.sqrt(max(0, (r ** 4 - r * r * (dx * dx + dy * dy)) / (r * r * (dx * dx + dy * dy))))
    cx, cy = co * dy + (x1 + x2) / 2, -co * dx + (y1 + y2) / 2
    a1, a2 = math.atan2(y1 - cy, x1 - cx), math.atan2(y2 - cy, x2 - cx)
    d = a2 - a1
    if sweep and d < 0: d += 2 * math.pi
    if not sweep and d > 0: d -= 2 * math.pi
    return [(cx + r * math.cos(a1 + d * i / n), cy + r * math.sin(a1 + d * i / n)) for i in range(1, n + 1)]


def cubic(p0, c1, c2, p1, n=24):
    return [tuple((1 - t) ** 3 * a + 3 * (1 - t) ** 2 * t * b + 3 * (1 - t) * t * t * c + t ** 3 * e
                  for a, b, c, e in zip(p0, c1, c2, p1)) for t in (i / n for i in range(1, n + 1))]


# M14 4 a4 4 0 0 1 8 0 v14 l2 -1 a4 4 0 0 1 5 2 l1 1 a4 4 0 0 1 5 3 v10 c0 6 -5 11 -11 11 h-4
# c-4 0 -7 -2 -9 -5 l-7 -10 a4 4 0 0 1 6 -5 l4 4 z
pts = [(14, 4)]
rel = lambda dx, dy: (pts[-1][0] + dx, pts[-1][1] + dy)
pts += arc(pts[-1], 4, 0, 1, rel(8, 0)); pts.append(rel(0, 14)); pts.append(rel(2, -1))
pts += arc(pts[-1], 4, 0, 1, rel(5, 2)); pts.append(rel(1, 1))
pts += arc(pts[-1], 4, 0, 1, rel(5, 3)); pts.append(rel(0, 10))
p = pts[-1]; pts += cubic(p, (p[0], p[1] + 6), (p[0] - 5, p[1] + 11), (p[0] - 11, p[1] + 11)); pts.append(rel(-4, 0))
p = pts[-1]; pts += cubic(p, (p[0] - 4, p[1]), (p[0] - 7, p[1] - 2), (p[0] - 9, p[1] - 5)); pts.append(rel(-7, -10))
pts += arc(pts[-1], 4, 0, 1, rel(6, -5)); pts.append(rel(4, 4))

S, M = 16, 10  # 16 px per unita' (poi ridotto a 4), margine 10 unita'
W, H = (40 + 2 * M) * S, (46 + 2 * M) * S
to_px = lambda q, oy=0: [((x + M) * S, (y + M + oy) * S) for x, y in q]

shadow = Image.new('L', (W, H), 0)
ImageDraw.Draw(shadow).polygon(to_px(pts, 4), fill=int(255 * 0.6))
shadow = shadow.filter(ImageFilter.GaussianBlur(3 * S))  # blur CSS 6 = sigma 3
img = Image.composite(Image.new('RGBA', (W, H), (0, 0, 0, 255)), Image.new('RGBA', (W, H), (0, 0, 0, 0)), shadow)

d = ImageDraw.Draw(img)
poly = to_px(pts)
d.polygon(poly, fill=(0xF5, 0xE9, 0xD0, 255))
stroke, r = (0x6B, 0x44, 0x18, 255), S  # tratto 2 centrato sul bordo: dischi di raggio 1 unita' lungo il path
for a, b in zip(poly, poly[1:] + poly[:1]):
    steps = max(1, int(math.dist(a, b)))
    for i in range(steps + 1):
        x, y = a[0] + (b[0] - a[0]) * i / steps, a[1] + (b[1] - a[1]) * i / steps
        d.ellipse([x - r, y - r, x + r, y + r], fill=stroke)
img.resize((W // 4, H // 4), Image.LANCZOS).save('Assets/UI51/Art/Common/tutorial_finger.png')
