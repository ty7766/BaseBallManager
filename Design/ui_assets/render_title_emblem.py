"""
타이틀 엠블럼을 연출용 층으로 나눈다 — 판 · 공과 궤적 · 베 · 이 · 스 · 볼 · 매니저

사용법
    python Design/ui_assets/render_title_emblem.py

입력 Design/logo_src/dynamic_emblem.png (외부 이미지 생성 결과 원본)
출력 Assets/Resources/UI/Title/
    title_emblem.png        전체 (빛줄기를 엠블럼 모양으로 자르는 틀)
    emblem_base.png         글자 · 공을 뺀 판. 빈자리는 둘레 판 색을 번져 메운다
    emblem_trail.png        공과 궤적
    emblem_glyph_0~3.png    베 · 이 · 스 · 볼 (검은 테두리 포함, 잘라낸 크기)
    emblem_manager.png      매니저
    emblem_layers.json      층마다 엠블럼 안 위치 (0~1, 왼쪽 위 기준) — TitleEmblemView 가 읽는다
"""
import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

PROJECT = Path(__file__).resolve().parents[2]
SRC = PROJECT / "Design" / "logo_src" / "dynamic_emblem.png"
OUT = PROJECT / "Assets" / "Resources" / "UI" / "Title"
WIDTH = 1280
LETTER_ZONE = (180, 170, 900, 345)     # 베이스볼 흰 글자가 있는 칸 (x0, y0, x1, y1)
GLYPH_SPLITS = (430, 590, 705)         # 베 | 이 | 스 | 볼 을 가르는 x (조각 가운데 기준)
DEBUG = PROJECT / "Design" / "ui_assets" / "_emblem_debug.png"  # 층 나눔 확인용 (커밋하지 않음)


def blur(a, s):
    return cv2.GaussianBlur(a, (0, 0), s)


def load():
    im = Image.open(SRC).convert("RGBA")
    l, t, r, b = im.getchannel("A").point(lambda v: 255 if v > 2 else 0).getbbox()
    im = im.crop((max(0, l - 4), max(0, t - 4), min(im.width, r + 4), min(im.height, b + 4)))
    im = im.resize((WIDTH, round(im.height * WIDTH / im.width)), Image.LANCZOS)
    im.save(OUT / "title_emblem.png", optimize=True)
    return np.asarray(im).astype(np.float32) / 255


def distance_from(mask):
    return cv2.distanceTransform((mask == 0).astype(np.uint8), cv2.DIST_L2, 5)


# 채움에서 바깥으로 1px 씩 나가며 순검정 테두리가 끝나는 거리 = 테두리 두께 (판 남색 (11,20,29) 과 구분된다)
def outline_width(fill, rgba, max_r=30):
    dist = distance_from(fill)
    black = rgba[..., :3].max(-1) < 0.04
    opaque = rgba[..., 3] > 0.5
    seen = False
    for d in range(1, max_r):
        ring = (dist > d - 1) & (dist <= d) & opaque
        if ring.sum() < 50:
            continue
        frac = black[ring].mean()
        if frac >= 0.5:
            seen = True
        elif seen:
            return d
    return 3 if not seen else max_r


def fill_holes_small(mask, k):
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (k, k))
    return cv2.morphologyEx(mask, cv2.MORPH_CLOSE, kernel)


def segment(rgba):
    rgb, al = rgba[..., :3], rgba[..., 3]
    mn, mx = rgb.min(-1), rgb.max(-1)
    white = ((mn > 0.62) & (mx - mn < 0.18) & (al > 0.5)).astype(np.uint8)
    green = ((rgb[..., 1] - np.maximum(rgb[..., 0], rgb[..., 2]) > 0.06) & (al > 0.5)).astype(np.uint8)

    n, lab, stats, cent = cv2.connectedComponentsWithStats(white, 8)
    biggest = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    x0, y0, x1, y1 = LETTER_ZONE
    glyphs = [np.zeros_like(white) for _ in range(4)]
    trail = (lab == biggest).astype(np.uint8)
    for i in range(1, n):
        if i == biggest or stats[i, cv2.CC_STAT_AREA] < 30:
            continue
        cx, cy = cent[i]
        if x0 <= cx <= x1 and y0 <= cy <= y1:
            glyphs[int(np.searchsorted(GLYPH_SPLITS, cx))] |= (lab == i).astype(np.uint8)
        elif cy < y0 or (cy < y1 and cx > x1):
            trail |= (lab == i).astype(np.uint8)

    trail = fill_holes_small(trail, 15)
    trail_filled = trail.copy()
    contours, _ = cv2.findContours(trail, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    cv2.drawContours(trail_filled, contours, -1, 1, -1)
    manager = fill_holes_small(green, 5)
    return glyphs, manager, trail_filled


# 층마다 테두리까지 넓힌 영역을 만들고, 겹치는 곳은 가까운 층에 준다 (검은 테두리를 반씩 나눔)
def partition(fills, widths, shape):
    dists = np.stack([distance_from(f) for f in fills])
    reach = np.stack([np.clip(w + 1.0 - d, 0, 1) for d, w in zip(dists, widths)])
    owner = np.argmin(np.where(reach > 0, dists, 1e9), axis=0)
    return [reach[i] * (owner == i) for i in range(len(fills))]


# 가중 평균 번짐: 가까운 곳은 좁게, 먼 곳은 넓게 번진 값을 매끄럽게 섞는다 (경계에 얼룩이 안 생기게)
def spread(value, weight, near=8, far=60):
    w = weight[..., None] if value.ndim == 3 else weight
    den_near, den_far = blur(weight, near), blur(weight, far)
    near_v = blur(value * w, near) / np.maximum(den_near, 1e-4)[..., None] if value.ndim == 3 else blur(value * w, near) / np.maximum(den_near, 1e-4)
    far_v = blur(value * w, far) / np.maximum(den_far, 1e-4)[..., None] if value.ndim == 3 else blur(value * w, far) / np.maximum(den_far, 1e-4)
    k = np.clip(den_near / 0.25, 0, 1)
    k = k[..., None] if value.ndim == 3 else k
    return near_v * k + far_v * (1 - k)


# 빠진 자리를 둘레 판 색으로 메운다. 글자 자리는 원래도 불투명이라 투명도를 그대로 두고,
# 공 · 궤적 자리만 둘레(판이면 불투명, 바깥이면 투명)를 따라 투명도를 새로 정한다
def fill_base(rgba, removed, trail_region):
    rgb, al = rgba[..., :3], rgba[..., 3]
    keep = (removed < 0.01).astype(np.float32)
    plate = keep * (al > 0.9) * (rgb.max(-1) > 0.07) * (rgb.max(-1) < 0.22)
    color = spread(rgb, plate, 10, 60)
    grain = blur(np.random.default_rng(7).normal(0, 1, al.shape).astype(np.float32), 0.8)[..., None] * 0.012
    alpha_fill = np.clip((spread(al, keep, 6, 30) - 0.78) * 14 + 0.5, 0, 1)
    t = np.clip(trail_region, 0, 1)
    new_alpha = al * (1 - t) + alpha_fill * t
    m = np.clip(removed, 0, 1)[..., None]
    out_rgb = rgb * (1 - m) + np.clip(color + grain, 0, 1) * m
    return np.dstack([out_rgb, new_alpha])


def save_layer(rgba, name, layout):
    h, w = rgba.shape[:2]
    ys, xs = np.where(rgba[..., 3] > 0.004)
    x0, y0 = max(0, xs.min() - 2), max(0, ys.min() - 2)
    x1, y1 = min(w, xs.max() + 3), min(h, ys.max() + 3)
    crop = rgba[y0:y1, x0:x1]
    Image.fromarray((np.clip(crop, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(OUT / f"emblem_{name}.png", optimize=True)
    layout.append({"name": name, "x": x0 / w, "y": y0 / h, "w": (x1 - x0) / w, "h": (y1 - y0) / h})
    print(f"emblem_{name}.png", crop.shape[1], crop.shape[0])


def main():
    rgba = load()
    rgb = rgba[..., :3]
    glyphs, manager, trail = segment(rgba)
    glyph_w = outline_width(np.maximum.reduce(glyphs), rgba)
    manager_w = outline_width(manager, rgba)
    trail_w = outline_width(trail, rgba)
    print("outline", glyph_w, manager_w, trail_w)

    regions = partition(glyphs + [manager, trail], [glyph_w] * 4 + [manager_w, trail_w + 4], rgb.shape[:2])
    removed = np.maximum.reduce(regions)
    layout = []
    save_layer(fill_base(rgba, removed, regions[5]), "base", layout)
    save_layer(rgba * np.dstack([np.ones_like(rgb), regions[5][..., None]]), "trail", layout)
    for i in range(4):
        save_layer(rgba * np.dstack([np.ones_like(rgb), regions[i][..., None]]), f"glyph_{i}", layout)
    save_layer(rgba * np.dstack([np.ones_like(rgb), regions[4][..., None]]), "manager", layout)
    (OUT / "emblem_layers.json").write_text(json.dumps({"layers": layout}, indent=1), encoding="utf-8")

    if DEBUG:
        tint = np.array([[1, .3, .3], [.3, 1, .3], [.3, .3, 1], [1, 1, .3], [1, .3, 1], [.3, 1, 1]], np.float32)
        dbg = np.zeros_like(rgb)
        for i, r in enumerate(regions):
            dbg += r[..., None] * tint[i]
        base = fill_base(rgba, removed, regions[5])
        sheet = np.concatenate([np.clip(dbg, 0, 1), base[..., :3] * base[..., 3:] + (1 - base[..., 3:]) * 0.5], 0)
        Image.fromarray((sheet * 255).astype(np.uint8)).save(DEBUG)


main()
