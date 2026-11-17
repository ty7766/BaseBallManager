"""
뽑기 로비 배너 · 뽑기 연출 화면 바탕 그림. 색은 실행 중에 뽑기 종류 · 등급 색을 곱해 입힌다 (흰 결 + 알파)

사용법
    python Design/ui_assets/render_gacha_stage_art.py

출력 Assets/Resources/UI/Gacha/
    gacha_reveal_bg.png      연출 바탕 1080x2340 (불투명) — 차분한 남색. 빛살 · 격자 없이 가운데만 은은하게 밝다 (연출은 카드 이펙트가 맡는다)
    gacha_stage_haze.png     연출 무대 빛 안개 512x512 — 카드 묶음 뒤에 깔고 뒤집힌 카드 등급 색으로 잠깐 차오른다
    gacha_banner_art.png     배너 무늬 1016x1800 — 위에 붙여 배너 전체를 덮는다 (배너가 이보다 길면 아래는 그늘이 덮는다). 위쪽 무대 조명 · 카드 뒤 빛무리 · 카드가 선 무대 끝선과 바닥 빛줄 · 아래 망점
    gacha_banner_rays.png    배너 빛살 1024x1024 — 카드 뒤에서 천천히 돈다. 가장자리로 갈수록 0 으로 사라져 끊긴 자리가 없다
    gacha_banner_shade.png   배너 아래 그늘 8x256 — 아래로 갈수록 짙어지는 알파 (글자 받침)
    Design/ui_assets/_gacha_stage_preview.png  배너 · 바탕 확인용 (커밋하지 않음)
"""
import math
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

PROJECT = Path(__file__).resolve().parents[2]
OUT = PROJECT / "Assets" / "Resources" / "UI" / "Gacha"
OUT.mkdir(parents=True, exist_ok=True)

#배너 그림 크기와 대표 카드 자리 (GachaBannerView 와 짝). 가운데 카드 중심이 빛살 소실점이다
BANNER_W, BANNER_H = 1016, 1800
CENTER_CARD_TOP, CENTER_CARD_W = 130, 580
CARD_ASPECT = 1957 / 1398
VANISH = (BANNER_W / 2, CENTER_CARD_TOP + CENTER_CARD_W * CARD_ASPECT / 2)
STAGE_EDGE_Y = CENTER_CARD_TOP + CENTER_CARD_W * CARD_ASPECT + 40


def blur(a, s):
    return cv2.GaussianBlur(a, (0, 0), s)


def noise(shape, cells, seed, octaves=4):
    rng = np.random.default_rng(seed)
    out = np.zeros(shape, np.float32)
    amp, norm = 1.0, 0.0
    for o in range(octaves):
        g = rng.random((max(2, cells[0] * 2 ** o), max(2, cells[1] * 2 ** o))).astype(np.float32)
        out += cv2.resize(g, (shape[1], shape[0]), interpolation=cv2.INTER_CUBIC) * amp
        norm += amp
        amp *= 0.5
    return out / norm


def rays(theta, count, seed, sharpness, jitter=0.35):
    rng = np.random.default_rng(seed)
    out = np.zeros_like(theta)
    for _ in range(count):
        center = rng.uniform(-math.pi, math.pi)
        width = rng.uniform(0.02, 0.09)
        gain = rng.uniform(0.35, 1.0)
        d = np.angle(np.exp(1j * (theta - center)))
        out = np.maximum(out, gain * np.exp(-(d / width) ** 2) ** sharpness)
    return out * (1 - jitter + jitter * noise(theta.shape, (6, 6), seed + 1, 3))


def save_rgba(rgb, alpha, name):
    rgba = np.dstack([np.clip(rgb, 0, 1), np.clip(alpha, 0, 1)])
    Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), "RGBA").save(OUT / name, optimize=True)
    print(name, rgba.shape[1], "x", rgba.shape[0])
    return rgba


def reveal_background():
    w, h = 1080, 2340
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    top = np.array([0.1, 0.125, 0.19])
    bottom = np.array([0.115, 0.14, 0.215])
    t = (y / h)[..., None]
    rgb = top * (1 - t) + bottom * t

    r = np.hypot((x - w * 0.5) / w, (y - h * 0.47) / h * 1.6)
    rgb += np.array([0.045, 0.06, 0.1]) * np.exp(-(r / 0.42) ** 2)[..., None]

    weave = np.sin((x + y) * 0.9) * np.sin((x - y) * 0.9)
    rgb += np.array([0.004, 0.005, 0.008]) * weave[..., None]

    grain = blur(np.random.default_rng(9).normal(0, 1, (h, w)).astype(np.float32), 0.7)[..., None] * (1.6 / 255)
    vignette = 1 - 0.32 * np.clip(((x - w / 2) / (w * 0.62)) ** 2 + ((y - h * 0.5) / (h * 0.6)) ** 2, 0, 1)
    rgb = np.clip(rgb * vignette[..., None] + grain, 0, 1)
    Image.fromarray((rgb * 255 + 0.5).astype(np.uint8)).save(OUT / "gacha_reveal_bg.png", optimize=True)
    print("gacha_reveal_bg.png", w, "x", h)
    return rgb


def stage_haze():
    n = 512
    y, x = np.mgrid[0:n, 0:n].astype(np.float32)
    dx, dy = (x - n / 2) / (n / 2), (y - n / 2) / (n / 2)
    body = np.exp(-((dx / 0.62) ** 2 + (dy / 0.46) ** 2) * 1.6)
    cloud = 0.65 + 0.35 * noise((n, n), (3, 3), 77, 4)
    edge = np.clip((1 - np.hypot(dx, dy)) / 0.25, 0, 1)
    alpha = blur(body * cloud, 6) * edge
    return save_rgba(np.ones((n, n, 3), np.float32), alpha / alpha.max() * 0.9, "gacha_stage_haze.png")


def banner_art():
    w, h = BANNER_W, BANNER_H
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    vx, vy = VANISH

    core = np.exp(-(((x - vx) / (w * 0.42)) ** 2 + ((y - vy) / (h * 0.2)) ** 2))

    spots = np.zeros((h, w), np.float32)
    for sx in (w * 0.08, w * 0.92):
        aim = math.atan2(vy - 0, vx - sx)
        ang = np.arctan2(y - 0, x - sx)
        spread = np.angle(np.exp(1j * (ang - aim)))
        dist = np.hypot(x - sx, y)
        spots += np.exp(-(spread / 0.16) ** 2) * np.exp(-dist / (h * 0.55)) * np.clip(dist / 60, 0, 1)

    edge_y = STAGE_EDGE_Y
    across = np.exp(-((x - vx) / (w * 0.42)) ** 2)
    edge_line = np.exp(-((y - edge_y) / 5) ** 2) * across
    edge_glow = np.exp(-((y - edge_y) / 70) ** 2) * np.exp(-((x - vx) / (w * 0.5)) ** 2)
    pool = np.exp(-(((x - vx) / (w * 0.38)) ** 2 + ((y - edge_y - 30) / 75) ** 2))

    below = y > edge_y
    depth = np.maximum(y - vy, 1)
    slope = (x - vx) / depth
    lane_gap = 0.38
    lane_dist = np.abs(slope / lane_gap - np.round(slope / lane_gap)) * lane_gap * depth
    lanes = np.exp(-(lane_dist / 2.2) ** 2) * below
    floor_fade = np.clip((y - edge_y) / 30, 0, 1) * np.exp(-(y - edge_y) / (h * 0.5))
    lanes *= floor_fade * np.exp(-((x - vx) / (w * 0.75)) ** 2)

    rng = np.random.default_rng(5)
    motes = np.zeros((h, w), np.float32)
    for _ in range(16):
        mx = rng.uniform(w * 0.12, w * 0.88)
        top = edge_y - rng.uniform(180, 520)
        span = edge_y - top
        column = np.exp(-((x - mx) / 1.6) ** 2) * np.clip((y - top) / span, 0, 1) ** 2 * (y < edge_y)
        motes = np.maximum(motes, column * rng.uniform(0.35, 1.0))

    spacing = 46.0
    across_d = x * 0.8 - y * 0.6
    along = x * 0.6 + y * 0.8
    lane = np.floor(across_d / spacing).astype(np.int32)
    lane_ids = np.arange(lane.min(), lane.max() + 1)
    srng = np.random.default_rng(43)
    starts = dict(zip(lane_ids, srng.uniform(-200, 1800, len(lane_ids))))
    lengths = dict(zip(lane_ids, srng.uniform(80, 420, len(lane_ids))))
    gains = dict(zip(lane_ids, srng.uniform(0, 1, len(lane_ids)) ** 2))
    start = np.vectorize(starts.get)(lane)
    length = np.vectorize(lengths.get)(lane)
    gain = np.vectorize(gains.get)(lane)
    offset = across_d - (lane + 0.5) * spacing
    inside = np.clip((along - start) / 30, 0, 1) * np.clip((start + length - along) / length, 0, 1)
    streaks = np.exp(-(offset / 1.6) ** 2) * inside * gain

    cell = 18.0
    gx, gy = (x % cell) - cell / 2, (y % cell) - cell / 2
    corner = np.clip((y - edge_y) / (h - edge_y), 0, 1) * (0.35 + 0.65 * np.abs(x - vx) / (w * 0.5))
    halftone = np.clip(cell * 0.42 * corner - np.hypot(gx, gy), 0, 1) * 0.2

    alpha = (core * 0.5 + spots * 0.3 + edge_line * 0.85 + edge_glow * 0.35 + pool * 0.35 + lanes * 0.45
             + motes * 0.3 + streaks * 0.3 + halftone)
    return save_rgba(np.ones((h, w, 3), np.float32), np.clip(alpha, 0, 1), "gacha_banner_art.png")


def banner_rays():
    n = 1024
    y, x = np.mgrid[0:n, 0:n].astype(np.float32)
    dx, dy = (x - n / 2) / (n / 2), (y - n / 2) / (n / 2)
    r = np.hypot(dx, dy)
    theta = np.arctan2(dy, dx)
    beam = rays(theta, 22, 21, 1.5, 0.25)
    reach = np.clip(1 - r, 0, 1) ** 1.6 * np.clip(r / 0.1, 0, 1)
    alpha = blur(beam * reach, 2.0)
    return save_rgba(np.ones((n, n, 3), np.float32), alpha * 0.9, "gacha_banner_rays.png")


def banner_shade():
    h = 256
    t = np.linspace(0, 1, h, dtype=np.float32)
    alpha = np.clip((t - 0.15) / 0.85, 0, 1) ** 1.6
    alpha = np.repeat(alpha[:, None], 8, axis=1)
    save_rgba(np.ones((h, 8, 3), np.float32), alpha, "gacha_banner_shade.png")


def preview(background, art, ray):
    """배너: 패널색 위에 무늬(알파 0.7) + 빛살(알파 0.3)을 일반 뽑기 색으로 얹는다. 바탕은 줄여서 옆에 붙인다"""
    accent = np.array([0x3D, 0x8B, 0xFF], np.float32) / 255
    panel = np.array([0x26, 0x31, 0x47], np.float32) / 255
    h, w = BANNER_H, BANNER_W
    canvas = np.ones((h, w, 3), np.float32) * panel
    size = 2800
    big = cv2.resize(ray[..., 3], (size, size))
    cx, cy = int(VANISH[0]), int(VANISH[1])
    x0, y0 = size // 2 - cx, size // 2 - cy
    ray_a = big[y0:y0 + h, x0:x0 + w] * 0.3
    canvas = canvas * (1 - ray_a[..., None]) + accent * ray_a[..., None]
    art_a = art[..., 3:4] * 0.7
    canvas = canvas * (1 - art_a) + accent * art_a
    bg = cv2.resize(background, (int(1080 * h / 2340), h))
    sheet = np.hstack([canvas, np.ones((h, 24, 3), np.float32) * 0.5, bg])
    Image.fromarray((np.clip(sheet, 0, 1) * 255).astype(np.uint8)).save(
        Path(__file__).resolve().parent / "_gacha_stage_preview.png")


if __name__ == "__main__":
    bg = reveal_background()
    stage_haze()
    art = banner_art()
    ray = banner_rays()
    banner_shade()
    preview(bg, art, ray)
