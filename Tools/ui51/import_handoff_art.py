"""Copia la grafica dell'handoff (Design/51_handoff) in Assets/UI51/Art, ridimensionata per il gioco.

Rieseguibile: sovrascrive solo i file in Assets/UI51/Art. Le icone restano sul canvas intero
(i mockup dimensionano l'immagine completa, padding incluso). python Tools/ui51/import_handoff_art.py
"""
import math
import os
from PIL import Image, ImageChops, ImageEnhance, ImageFilter, ImageStat

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "Design", "51_handoff", "51_handoff", "assets")
DST = os.path.join(ROOT, "Assets", "UI51", "Art")


def out(area, name):
    os.makedirs(os.path.join(DST, area), exist_ok=True)
    return os.path.join(DST, area, name)


def fit(img, max_side):
    s = max_side / max(img.size)
    return img if s >= 1 else img.resize((round(img.width * s), round(img.height * s)), Image.LANCZOS)


def copy(name, area, max_side=None, dst_name=None):
    img = Image.open(os.path.join(SRC, name))
    if max_side:
        img = fit(img, max_side)
    img.save(out(area, dst_name or name), optimize=True)


def conic_disc(stops, size=256, start_deg=20, ss=4):
    """Disco pieno con conic-gradient CSS (0deg = alto, senso orario). Bordo antialias via supersampling."""
    n = size * ss
    c = (n - 1) / 2
    rgb = [tuple(int(h[i:i + 2], 16) for i in (1, 3, 5)) for h in stops]
    img = Image.new("RGBA", (n, n))
    px = img.load()
    for y in range(n):
        for x in range(n):
            dx, dy = x - c, y - c
            if dx * dx + dy * dy > (n / 2) ** 2:
                continue
            t = ((math.degrees(math.atan2(dx, -dy)) - start_deg) % 360) / 360 * (len(rgb) - 1)
            i = min(int(t), len(rgb) - 2)
            f = t - i
            px[x, y] = tuple(round(rgb[i][k] + (rgb[i + 1][k] - rgb[i][k]) * f) for k in range(3)) + (255,)
    return img.resize((size, size), Image.LANCZOS)


def main():
    for f in sorted(os.listdir(SRC)):
        if f.startswith("ic_") or f.startswith("medal_") or f in ("ribbon.png", "chest_green.png", "chest_purple.png"):
            copy(f, "Common", 256)
    copy("pugno.png", "Common")  # accuso: 112px a scala 2.2, serve la risoluzione piena
    copy("logo_51.png", "Common", 512)
    copy("Bagliore_morbido.png", "Common", 512)

    for f in ["avatar_1.png"] + ["av_%d.png" % i for i in range(2, 9)]:
        copy(f, "Avatars")
    for f in ("back_giada.png", "back_smeraldo.png", "back_tradizionale.png"):
        copy(f, "Cards")

    # Sfondo Home + versione "interna" (SPEC §2: blur 10px, brightness .42, saturate 1.1).
    # Le varianti .38/.32 si ottengono con il tint dell'Image, non con altri file.
    bg = Image.open(os.path.join(SRC, "home_bg_base.png")).convert("RGB")
    fit(bg, 2048).save(out("Backgrounds", "home_bg_base.png"), optimize=True)
    small = bg.resize((540, round(bg.height * 540 / bg.width)), Image.LANCZOS)
    blurred = small.filter(ImageFilter.GaussianBlur(10))  # mockup: 10px CSS su un bg largo ~530px
    blurred = ImageEnhance.Color(ImageEnhance.Brightness(blurred).enhance(0.42)).enhance(1.1)
    blurred.save(out("Backgrounds", "home_bg_blur.png"), optimize=True)

    # Cornici avatar (Profilo.dc.html, const frames). "classica" e' oro pieno: usa circle.png tinto #F3C969.
    conic_disc(["#FCE29A", "#C4922F", "#FFF1C4", "#8A5A12", "#FCE29A"]).save(out("Shapes", "ring_oro.png"))
    conic_disc(["#27B585", "#F3C969", "#0E6B4F", "#FCE29A", "#27B585"]).save(out("Shapes", "ring_smeraldo.png"))
    conic_disc(["#4F80E8", "#F3C969", "#1B3A7A", "#FCE29A", "#4F80E8"]).save(out("Shapes", "ring_notte.png"))
    conic_disc(["#FFFFFF", "#FFFFFF"]).save(out("Shapes", "circle.png"))
    emoticons()


def emoticons(cols=4, rows=2, cell=448, out_cell=256):
    """Fogli 4x2 (8 fotogrammi) 1774x887 -> 1024x512, fotogrammi 256x256 (bolla 76px, banner 36px).

    Nei fogli le facce non sono centrate nelle celle (fino a ~30 px di deriva): come
    nel vecchio builder delle emoticon animate, ogni fotogramma si allinea al primo (correlazione delle
    maschere alfa) e usa solo i pixel della propria cella, cosi' l'animazione non trema.
    """
    for f in sorted(os.listdir(SRC)):
        if not f.endswith("_sheet_8frames.png"):
            continue
        sheet = Image.open(os.path.join(SRC, f)).convert("RGBA")
        cw, ch = sheet.width / cols, sheet.height / rows
        crops = [sheet.crop((round(i % cols * cw), round(i // cols * ch),
                             round((i % cols + 1) * cw), round((i // cols + 1) * ch))) for i in range(cols * rows)]
        masks = [c.getchannel("A").point(lambda v: 255 if v > 127 else 0) for c in crops]
        bx = masks[0].getbbox()
        anchor = ((bx[0] + bx[2]) // 2, (bx[1] + bx[3]) // 2)
        result = Image.new("RGBA", (cols * out_cell, rows * out_cell), (0, 0, 0, 0))
        for i, crop in enumerate(crops):
            dx, dy = _align(masks[0], masks[i])
            frame = Image.new("RGBA", (cell, cell), (0, 0, 0, 0))
            frame.paste(crop, (cell // 2 - anchor[0] + dx, cell // 2 - anchor[1] + dy))
            result.paste(frame.resize((out_cell, out_cell), Image.LANCZOS), (i % cols * out_cell, i // cols * out_cell))
        result.save(out("Emoticons", f), optimize=True)


def _align(ref, mov, factor=4, coarse=12, fine=4):
    """Spostamento (dx, dy) in pixel che sovrappone la maschera mov a ref."""
    def best(a, b, cx, cy, r):
        top = None
        for dy in range(cy - r, cy + r + 1):
            for dx in range(cx - r, cx + r + 1):
                moved = Image.new("L", a.size, 0)
                moved.paste(b, (dx, dy))
                score = ImageStat.Stat(ImageChops.multiply(a, moved)).sum[0]
                if top is None or score > top[0]:
                    top = (score, dx, dy)
        return top[1], top[2]

    small = (ref.width // factor, ref.height // factor)
    dx, dy = best(ref.resize(small, Image.BOX), mov.resize(small, Image.BOX), 0, 0, coarse)
    return best(ref, mov, dx * factor, dy * factor, fine)

if __name__ == "__main__":
    main()
    for area in sorted(os.listdir(DST)):
        d = os.path.join(DST, area)
        if os.path.isdir(d):
            print(area, len([f for f in os.listdir(d) if f.endswith(".png")]))
