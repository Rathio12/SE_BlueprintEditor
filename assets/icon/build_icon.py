"""Generates the SE Blueprint Inspector icon (SVG + PNG + multi-size ICO) from one geometry.

Usage:  python assets/icon/build_icon.py
Needs:  Pillow (pip install pillow)
Writes: assets/icon/app-icon.svg, app-icon-256.png, app.ico
"""
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
N = 1024  # design canvas

BG_TOP, BG_BOTTOM = (11, 30, 58), (18, 61, 107)
GRID = (120, 180, 255, 26)
TOP, LEFT, RIGHT, EDGE = "#5EC8FF", "#2C8FD6", "#1B6AA8", "#E6F4FF"
ACCENT = "#FFA726"

C = (512, 470)
TOPV, UR, LR, BOT, LL, UL = (512, 240), (711, 355), (711, 585), (512, 700), (313, 585), (313, 355)
FACES = [([TOPV, UR, C, UL], TOP), ([UL, C, BOT, LL], LEFT), ([C, UR, LR, BOT], RIGHT)]
EDGES = [(TOPV, UR), (UR, LR), (LR, BOT), (BOT, LL), (LL, UL), (UL, TOPV), (C, UL), (C, UR), (C, BOT)]
LENS_C, LENS_R, LENS_W = (735, 735), 120, 44
HANDLE = ((822, 822), (930, 930))
RADIUS = 220


def svg() -> str:
    pts = lambda ps: " ".join(f"{x},{y}" for x, y in ps)
    grid = "".join(
        f'<path d="M{i} 0V{N}M0 {i}H{N}"/>' for i in range(64, N, 64)
    )
    faces = "".join(f'<polygon points="{pts(p)}" fill="{c}"/>' for p, c in FACES)
    edges = "".join(f'<line x1="{a[0]}" y1="{a[1]}" x2="{b[0]}" y2="{b[1]}"/>' for a, b in EDGES)
    return f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {N} {N}" width="{N}" height="{N}">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="rgb{BG_TOP}"/><stop offset="1" stop-color="rgb{BG_BOTTOM}"/>
    </linearGradient>
    <clipPath id="r"><rect width="{N}" height="{N}" rx="{RADIUS}"/></clipPath>
  </defs>
  <g clip-path="url(#r)">
    <rect width="{N}" height="{N}" fill="url(#bg)"/>
    <g stroke="rgb(120,180,255)" stroke-opacity="0.1" stroke-width="4">{grid}</g>
  </g>
  {faces}
  <g stroke="{EDGE}" stroke-width="12" stroke-linecap="round">{edges}</g>
  <circle cx="{LENS_C[0]}" cy="{LENS_C[1]}" r="{LENS_R}" fill="{ACCENT}" fill-opacity="0.15" stroke="{ACCENT}" stroke-width="{LENS_W}"/>
  <line x1="{HANDLE[0][0]}" y1="{HANDLE[0][1]}" x2="{HANDLE[1][0]}" y2="{HANDLE[1][1]}" stroke="{ACCENT}" stroke-width="64" stroke-linecap="round"/>
</svg>
"""


def png(size: int = 1024) -> Image.Image:
    s = 2  # supersample
    W = N * s
    sc = lambda p: (p[0] * s, p[1] * s)
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))

    bg = Image.new("RGBA", (W, W))
    d = ImageDraw.Draw(bg)
    for y in range(W):
        t = y / (W - 1)
        d.line([(0, y), (W, y)], fill=tuple(int(a + (b - a) * t) for a, b in zip(BG_TOP, BG_BOTTOM)) + (255,))
    grid = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    g = ImageDraw.Draw(grid)
    for i in range(64, N, 64):
        g.line([(i * s, 0), (i * s, W)], fill=GRID, width=4 * s)
        g.line([(0, i * s), (W, i * s)], fill=GRID, width=4 * s)
    bg = Image.alpha_composite(bg, grid)
    mask = Image.new("L", (W, W), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, W - 1, W - 1], radius=RADIUS * s, fill=255)
    img.paste(bg, (0, 0), mask)

    d = ImageDraw.Draw(img)
    for poly, color in FACES:
        d.polygon([sc(p) for p in poly], fill=color)
    for a, b in EDGES:
        d.line([sc(a), sc(b)], fill=EDGE, width=12 * s)
        for p in (a, b):
            r = 6 * s
            d.ellipse([p[0] * s - r, p[1] * s - r, p[0] * s + r, p[1] * s + r], fill=EDGE)

    lens = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    l = ImageDraw.Draw(lens)
    cx, cy, r = LENS_C[0] * s, LENS_C[1] * s, LENS_R * s
    l.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(255, 167, 38, 38))
    hw = LENS_W * s // 2
    l.ellipse([cx - r - hw, cy - r - hw, cx + r + hw, cy + r + hw], outline=ACCENT, width=LENS_W * s)
    l.line([sc(HANDLE[0]), sc(HANDLE[1])], fill=ACCENT, width=64 * s)
    for p in HANDLE:
        rr = 32 * s
        l.ellipse([p[0] * s - rr, p[1] * s - rr, p[0] * s + rr, p[1] * s + rr], fill=ACCENT)
    img = Image.alpha_composite(img, lens)
    return img.resize((size, size), Image.LANCZOS)


def main() -> None:
    (HERE / "app-icon.svg").write_text(svg(), encoding="utf-8")
    big = png(1024)
    big.resize((256, 256), Image.LANCZOS).save(HERE / "app-icon-256.png")
    big.save(HERE / "app.ico", sizes=[(s, s) for s in (16, 24, 32, 48, 64, 128, 256)])
    print("wrote", ", ".join(p.name for p in HERE.iterdir() if p.suffix in {".svg", ".png", ".ico"}))


if __name__ == "__main__":
    main()
