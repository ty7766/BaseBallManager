"""
공용 화면 배경 — 타이틀 바탕을 어둡게 누르고, 오른쪽 위 · 왼쪽 아래 모서리만 붉은 · 검은 마른 붓질이 스친다.
가운데는 UI 가 올라가므로 조용히 비워 둔다

사용법
    python Design/ui_assets/render_title_art.py   (title_bg.png 가 먼저 있어야 한다)
    python Design/ui_assets/render_app_backdrop.py

출력
    Assets/Resources/UI/Backdrop/app_backdrop.png   1080x1920 불투명 · AppBackdrop 이 캔버스 맨 뒤에 깔고 넘치는 쪽을 자른다
"""
import math
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

PROJECT = Path(__file__).resolve().parents[2]
SOURCE = PROJECT / "Assets" / "Resources" / "UI" / "Title" / "title_bg.png"
OUT = PROJECT / "Assets" / "Resources" / "UI" / "Backdrop"
OUT.mkdir(parents=True, exist_ok=True)
W, H = 1080, 1920
Y, X = np.mgrid[0:H, 0:W].astype(np.float32)
U, V = X / W, Y / H
RED = np.array([0.78, 0.06, 0.12], np.float32)
BASE_DARKEN = 0.72


def blur(a, s):
    return cv2.GaussianBlur(a, (0, 0), s)


def field(shape, cy, cx, seed, octaves=5):
    rng = np.random.default_rng(seed)
    out = np.zeros(shape, np.float32)
    amp, norm = 1.0, 0.0
    for o in range(octaves):
        g = rng.random((max(2, cy * 2 ** o), max(2, cx * 2 ** o))).astype(np.float32)
        out += cv2.resize(g, (shape[1], shape[0]), interpolation=cv2.INTER_CUBIC) * amp
        norm += amp
        amp *= 0.5
    return out / norm


def screen(a, b):
    return 1 - (1 - a) * (1 - b)


def over(dst, src, alpha):
    a = alpha[..., None]
    return dst * (1 - a) + src * a


def grain(rgb, amount=1.8, seed=91):
    g = blur(np.random.default_rng(seed).normal(0, 1, (H, W)).astype(np.float32), 0.6)[..., None] * (amount / 255)
    return np.clip(rgb + g, 0, 1)


def vignette(rgb, strength):
    return rgb * (1 - strength * np.clip(((U - 0.5) / 0.62) ** 2 + ((V - 0.5) / 0.78) ** 2, 0, 1))[..., None]


# ── 마른 붓질 하나: 길이 방향으로 늘어진 결 + 끝으로 갈수록 물감이 마름 (타이틀 붓질과 같은 결) ──
def dry_brush(p0, p1, width, seed, dryness=0.55):
    p0, p1 = np.array(p0, np.float32), np.array(p1, np.float32)
    length = int(np.linalg.norm(p1 - p0))
    w = int(width)
    along = np.linspace(0, 1, length, dtype=np.float32)[None, :]
    across = np.linspace(-1, 1, w, dtype=np.float32)[:, None]
    streak = field((w, length), max(2, w // 6), max(2, length // 140), seed, 4)
    hair = cv2.resize(np.random.default_rng(seed + 1).random((w, max(2, length // 40))).astype(np.float32), (length, w), interpolation=cv2.INTER_LINEAR)
    wobble = (field((1, length), 1, max(2, length // 90), seed + 2, 3) - 0.5) * 0.5
    inside = np.clip((1.0 - 0.55 * along ** 1.6 + wobble - np.abs(across)) * w * 0.25, 0, 1)
    thr = dryness * (0.25 + 0.9 * along ** 1.3) + 0.25 * np.abs(across) ** 2 - 0.35 * np.exp(-along / 0.08)
    a = np.clip((0.55 * streak + 0.45 * hair - thr) * 6.0, 0, 1) * inside * np.clip(along / 0.02, 0, 1)
    d = (p1 - p0) / max(length, 1)
    n = np.array([-d[1], d[0]], np.float32)
    origin = p0 - n * (w / 2)
    M = np.array([[d[0], n[0], origin[0]], [d[1], n[1], origin[1]]], np.float32)
    return cv2.warpAffine(a, M, (W, H), flags=cv2.INTER_LINEAR, borderValue=0)


def scratches(count, seed, angle=-62, spread=20):
    rng = np.random.default_rng(seed)
    a = np.zeros((H, W), np.float32)
    for _ in range(count):
        x, y = rng.uniform(0, W), rng.uniform(0, H)
        ang = math.radians(angle + rng.normal(0, spread))
        length = rng.uniform(20, 160)
        cv2.line(a, (int(x), int(y)), (int(x + length * math.cos(ang)), int(y + length * math.sin(ang))), float(rng.uniform(0.3, 1)), 1, cv2.LINE_AA)
    return a


def render():
    bg = np.asarray(Image.open(SOURCE).convert("RGB")).astype(np.float32) / 255 * BASE_DARKEN
    red = np.maximum(dry_brush((1260, -60), (700, 330), 190, 51, 0.5), dry_brush((1180, 160), (840, 420), 80, 52, 0.6) * 0.9)
    red = np.maximum(red, dry_brush((-120, 1790), (330, 1520), 150, 53, 0.55) * 0.85)
    black = np.maximum(dry_brush((1200, 60), (860, 300), 50, 54, 0.6), dry_brush((-80, 1700), (220, 1540), 40, 55, 0.6))
    tex = RED * (0.72 + 0.35 * field((H, W), 30, 18, 56, 3))[..., None]
    out = over(bg, tex, red * 0.85)
    out = over(out, np.full(3, 0.02, np.float32), black * 0.8)
    out = screen(out, (blur(scratches(160, 57), 0.4) * 0.08)[..., None])
    return grain(vignette(out, 0.4))


if __name__ == "__main__":
    rgb = render()
    Image.fromarray((np.clip(rgb, 0, 1) * 255 + 0.5).astype(np.uint8)).save(OUT / "app_backdrop.png", optimize=True)
    print("app_backdrop.png", (W, H))
