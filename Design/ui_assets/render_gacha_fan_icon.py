"""
로비 뽑기 아이콘 — 부채처럼 펼친 카드 세 장. 다른 로비 아이콘(Phosphor Fill)과 같은 256 격자 · 같은 틈 굵기로 맞춘다.
겹친 카드 사이에 틈을 파야 해서 SVG path 만 뽑는 render_lobby_icons.py 대신 PIL 로 8배 크게 그려 줄인다

사용법
    python Design/ui_assets/render_gacha_fan_icon.py

출력 (256x256 · 흰색 · Image 색으로 틴트)
    Assets/Resources/UI/Lobby/Icons/icon_gacha.png
"""
import math
from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parents[2] / "Assets/Resources/UI/Lobby/Icons/icon_gacha.png"
GRID = 256
SUPER = 8
MARGIN = 22
CARD_W, CARD_H, CARD_R = 112, 158, 16
BACK_SCALE = 0.9
GAP = 14
FAN_DEGREES = 25
PIVOT_BELOW = 30
STAR_OUTER, STAR_INNER = 28, 11.5


def rounded_rect(cx, cy, w, h, r, steps=12):
    pts = []
    corners = [(cx + w / 2 - r, cy - h / 2 + r, -90), (cx + w / 2 - r, cy + h / 2 - r, 0),
               (cx - w / 2 + r, cy + h / 2 - r, 90), (cx - w / 2 + r, cy - h / 2 + r, 180)]
    for ox, oy, start in corners:
        for i in range(steps + 1):
            a = math.radians(start + 90 * i / steps)
            pts.append((ox + r * math.cos(a), oy + r * math.sin(a)))
    return pts


def rotate(pts, degrees, px, py):
    a = math.radians(degrees)
    c, s = math.cos(a), math.sin(a)
    return [(px + (x - px) * c - (y - py) * s, py + (x - px) * s + (y - py) * c) for x, y in pts]


def star(cx, cy, outer, inner):
    pts = []
    for i in range(10):
        r = outer if i % 2 == 0 else inner
        a = math.radians(-90 + 36 * i)
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def build_shapes():
    back_w, back_h = CARD_W * BACK_SCALE, CARD_H * BACK_SCALE
    back_cy = CARD_H / 2 - back_h / 2 - GAP * 1.5
    pivot = (0.0, back_cy + back_h / 2 + PIVOT_BELOW)
    back = [rotate(rounded_rect(0, back_cy, back_w, back_h, CARD_R), d, *pivot) for d in (-FAN_DEGREES, FAN_DEGREES)]
    front = rounded_rect(0, 0, CARD_W, CARD_H, CARD_R)
    front_gap = rounded_rect(0, 0, CARD_W + GAP * 2, CARD_H + GAP * 2, CARD_R + GAP)
    mark = star(0, -4, STAR_OUTER, STAR_INNER)
    return back, front, front_gap, mark


def fit(groups):
    xs = [x for g in groups for poly in g for x, _ in poly]
    ys = [y for g in groups for poly in g for _, y in poly]
    w, h = max(xs) - min(xs), max(ys) - min(ys)
    scale = (GRID - MARGIN * 2) / max(w, h)
    ox = GRID / 2 - (min(xs) + w / 2) * scale
    oy = GRID / 2 - (min(ys) + h / 2) * scale
    return lambda poly: [((x * scale + ox) * SUPER, (y * scale + oy) * SUPER) for x, y in poly]


def main():
    back, front, front_gap, mark = build_shapes()
    place = fit([back, [front]])
    mask = Image.new("L", (GRID * SUPER, GRID * SUPER), 0)
    draw = ImageDraw.Draw(mask)
    for poly in back:
        draw.polygon(place(poly), fill=255)
    draw.polygon(place(front_gap), fill=0)
    draw.polygon(place(front), fill=255)
    draw.polygon(place(mark), fill=0)
    mask = mask.resize((GRID, GRID), Image.LANCZOS)
    icon = Image.new("RGBA", (GRID, GRID), (255, 255, 255, 0))
    icon.putalpha(mask)
    icon.save(OUT, optimize=True)
    print(f"  {OUT.name} {icon.size[0]}x{icon.size[1]}")


if __name__ == "__main__":
    main()
