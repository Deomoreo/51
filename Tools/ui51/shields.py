"""Disegna gli scudi dei mockup Sospensione e SegnalazioneEsito (svg 74x84) in Assets/UI51/Art/Common:
shield_alert.png (rosso #F08A8D -> #7A151D, "!" bianco) e shield_check.png (oro #FCE29A -> #C4922F, spunta #25160A).
Bordo #0B1626 da 3 con angoli arrotondati. PNG 296x336 = 4 px per unita' (si usa a 74x84)."""
import math
from PIL import Image, ImageDraw


def cubic(p0, c1, c2, p1, n=32):
    return [tuple((1 - t) ** 3 * a + 3 * (1 - t) ** 2 * t * b + 3 * (1 - t) * t * t * c + t ** 3 * e
                  for a, b, c, e in zip(p0, c1, c2, p1)) for t in (i / n for i in range(1, n + 1))]


# M37 3 L69 14 V40 C69 60 55 74 37 81 C19 74 5 60 5 40 V14 Z
outline = [(37, 3), (69, 14), (69, 40)] + cubic((69, 40), (69, 60), (55, 74), (37, 81)) \
    + cubic((37, 81), (19, 74), (5, 60), (5, 40)) + [(5, 14)]

S = 16  # 16 px per unita', poi ridotto a 4
W, H = 74 * S, 84 * S
px = lambda pts: [(x * S, y * S) for x, y in pts]


def stroke(d, pts, width, color, closed):
    r = width * S / 2
    segs = list(zip(pts, pts[1:] + pts[:1])) if closed else list(zip(pts, pts[1:]))
    for a, b in segs:
        steps = max(1, int(math.dist(a, b) / 2))
        for i in range(steps + 1):
            x, y = a[0] + (b[0] - a[0]) * i / steps, a[1] + (b[1] - a[1]) * i / steps
            d.ellipse([x - r, y - r, x + r, y + r], fill=color)


def shield(top, bottom, mark, name):
    grad = Image.new('RGBA', (W, H))
    g = ImageDraw.Draw(grad)
    for y in range(H):  # linearGradient x1=0 y1=0 x2=0 y2=1 sul riquadro del path (y 3..81)
        t = min(1, max(0, (y / S - 3) / 78))
        g.line([(0, y), (W, y)], fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)) + (255,))
    mask = Image.new('L', (W, H), 0)
    ImageDraw.Draw(mask).polygon(px(outline), fill=255)
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    img.paste(grad, (0, 0), mask)
    d = ImageDraw.Draw(img)
    stroke(d, px(outline), 3, (0x0B, 0x16, 0x26, 255), True)
    mark(d)
    img.resize((W // 4, H // 4), Image.LANCZOS).save('Assets/UI51/Art/Common/' + name)


def alert(d):
    stroke(d, px([(37, 24), (37, 48)]), 7, (255, 255, 255, 255), False)  # M37 24 V48, tratto 7 arrotondato
    r = 4.5 * S
    d.ellipse([37 * S - r, 61 * S - r, 37 * S + r, 61 * S + r], fill=(255, 255, 255, 255))


def check(d):
    stroke(d, px([(22, 42), (33, 53), (53, 31)]), 6, (0x25, 0x16, 0x0A, 255), False)  # M22 42 L33 53 L53 31


shield((0xF0, 0x8A, 0x8D), (0x7A, 0x15, 0x1D), alert, 'shield_alert.png')
shield((0xFC, 0xE2, 0x9A), (0xC4, 0x92, 0x2F), check, 'shield_check.png')
