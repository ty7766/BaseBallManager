"""
뽑기 로비 배너 · 뽑기 연출 화면 그림. 색은 실행 중에 뽑기 종류 · 최고 등급 색을 곱해 입힌다 (흰 결 + 알파)

사용법
    python Design/ui_assets/render_gacha_stage_art.py

출력 Assets/Resources/UI/Gacha/
    gacha_reveal_bg.png      연출 바탕 1080x2340 (불투명) — 짙은 남색, 가운데 아래 소실점으로 모이는 경기장 조명선 · 바닥 격자
    gacha_reveal_rays.png    연출 빛살 1024x1024 — 굵기 · 길이가 제각각인 빛살. 천천히 돌며 등급 색을 곱한다
    gacha_banner_art.png     배너 무늬 1016x1320 — 위쪽 소실점에서 퍼지는 빛살 + 사선 속도선 + 망점. 뽑기 종류 색을 곱한다
    gacha_banner_shade.png   배너 아래 그늘 8x256 — 아래로 갈수록 짙어지는 알파 (글자 받침)
"""
import math
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

PROJECT = Path(__file__).resolve().parents[2]
OUT = PROJECT / "Assets" / "Resources" / "UI" / "Gacha"
OUT.mkdir(parents=True, exist_ok=True)


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


def reveal_background():
    w, h = 1080, 2340
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    vx, vy = w * 0.5, h * 0.47
    top = np.array([0.055, 0.075, 0.14])
    bottom = np.array([0.09, 0.11, 0.2])
    t = (y / h)[..., None]
    rgb = top * (1 - t) + bottom * t

    r = np.hypot((x - vx) / w, (y - vy) / h)
    rgb += np.array([0.08, 0.1, 0.2]) * np.exp(-(r / 0.32) ** 2)[..., None]

    theta = np.arctan2(y - vy, x - vx)
    beams = rays(theta, 22, 3, 2.0) * np.clip(r / 0.12, 0, 1) * np.exp(-r / 0.9)
    rgb += np.array([0.14, 0.18, 0.32]) * beams[..., None] * 0.6

    floor = np.clip((y - vy) / (h - vy), 0, 1)
    depth = 1 / np.maximum(floor, 0.02)
    lanes = np.abs(((x - vx) / np.maximum(y - vy, 1) * 6) % 1 - 0.5)
    rows = np.abs((depth * 0.6) % 1 - 0.5)
    grid_line = (np.exp(-(lanes / 0.03) ** 2) + np.exp(-(rows / (0.05 * floor + 0.01)) ** 2)) * floor ** 1.2
    rgb += np.array([0.1, 0.14, 0.26]) * grid_line[..., None] * 0.55

    grain = blur(np.random.default_rng(9).normal(0, 1, (h, w)).astype(np.float32), 0.7)[..., None] * (1.6 / 255)
    vignette = 1 - 0.45 * np.clip(((x - w / 2) / (w * 0.62)) ** 2 + ((y - h * 0.5) / (h * 0.62)) ** 2, 0, 1)
    rgb = np.clip(rgb * vignette[..., None] + grain, 0, 1)
    Image.fromarray((rgb * 255 + 0.5).astype(np.uint8)).save(OUT / "gacha_reveal_bg.png", optimize=True)
    print("gacha_reveal_bg.png", w, "x", h)


def reveal_rays():
    n = 1024
    y, x = np.mgrid[0:n, 0:n].astype(np.float32)
    dx, dy = (x - n / 2) / (n / 2), (y - n / 2) / (n / 2)
    r = np.hypot(dx, dy)
    theta = np.arctan2(dy, dx)
    beam = rays(theta, 18, 21, 1.5, 0.25)
    reach = np.clip(1 - r, 0, 1) ** 1.3 * np.clip(r / 0.08, 0, 1)
    alpha = blur(beam * reach, 2.0)
    save_rgba(np.ones((n, n, 3), np.float32), alpha * 0.9, "gacha_reveal_rays.png")


def banner_art():
    w, h = 1016, 1320
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    vx, vy = w * 0.5, h * 0.36
    r = np.hypot((x - vx) / w, (y - vy) / h)
    theta = np.arctan2(y - vy, x - vx)

    fan = rays(theta, 26, 41, 1.6) * np.clip(r / 0.05, 0, 1) * np.exp(-r / 0.75)
    core = np.exp(-(r / 0.2) ** 2)

    spacing = 46.0
    across = x * 0.8 - y * 0.6
    along = x * 0.6 + y * 0.8
    lane = np.floor(across / spacing).astype(np.int32)
    rng = np.random.default_rng(43)
    lane_ids = np.arange(lane.min(), lane.max() + 1)
    starts = dict(zip(lane_ids, rng.uniform(-200, 1400, len(lane_ids))))
    lengths = dict(zip(lane_ids, rng.uniform(80, 420, len(lane_ids))))
    gains = dict(zip(lane_ids, rng.uniform(0, 1, len(lane_ids)) ** 2))
    start = np.vectorize(starts.get)(lane)
    length = np.vectorize(lengths.get)(lane)
    gain = np.vectorize(gains.get)(lane)
    offset = across - (lane + 0.5) * spacing
    inside = np.clip((along - start) / 30, 0, 1) * np.clip((start + length - along) / length, 0, 1)
    streaks = np.exp(-(offset / 1.6) ** 2) * inside * gain * np.clip(r / 0.25, 0, 1)

    cell = 18.0
    gx, gy = (x % cell) - cell / 2, (y % cell) - cell / 2
    dot_size = cell * 0.45 * np.clip(1 - r * 1.4, 0, 1)
    halftone = np.clip(dot_size - np.hypot(gx, gy), 0, 1) * 0.22

    alpha = np.clip(fan * 0.75 + core * 0.45 + streaks * 0.5 + halftone, 0, 1)
    save_rgba(np.ones((h, w, 3), np.float32), alpha, "gacha_banner_art.png")


def banner_shade():
    h = 256
    t = np.linspace(0, 1, h, dtype=np.float32)
    alpha = np.clip((t - 0.15) / 0.85, 0, 1) ** 1.6
    alpha = np.repeat(alpha[:, None], 8, axis=1)
    save_rgba(np.ones((h, 8, 3), np.float32), alpha, "gacha_banner_shade.png")


if __name__ == "__main__":
    reveal_background()
    reveal_rays()
    banner_art()
    banner_shade()
