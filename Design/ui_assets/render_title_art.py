"""
타이틀 키 아트 층 — 거친 숯색 바탕 · 마른 붓질 · 잉크 튐 · 큰 윤곽 글자 · 로고 (선수 사진은 넣지 않는다)

사용법
    python Design/ui_assets/render_title_art.py

출력 Assets/Resources/UI/Title/ (1080x1920 기준 · TitleKeyArtView 가 층별로 깔고 DOTween 으로 연출한다)
    title_bg.png           바탕 (불투명)
    title_brush_red.png    붉은 붓질 · 잉크 튐 (WipeReveal 로 사선으로 쓸며 나타남)
    title_brush_black.png  검은 붓질
    title_bigtext.png      뒤 큰 윤곽 글자 MANAGER
선수는 실행 중에 Resources/Photos 에서 불러 얹는다 — 실제 선수 사진은 커밋하지 않는다
엠블럼 층은 render_title_emblem.py, 충격 이펙트 플립북은 render_impact_fx.py 가 만든다
"""
import math
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

PROJECT = Path(__file__).resolve().parents[2]
OUT = PROJECT / "Assets" / "Resources" / "UI" / "Title"
OUT.mkdir(parents=True, exist_ok=True)
W, H = 1080, 1920
RNG = np.random.default_rng(26)
Y, X = np.mgrid[0:H, 0:W].astype(np.float32)
U, V = X / W, Y / H
RED = np.array([0.80, 0.06, 0.12], np.float32)


def blur(a, s):
    return cv2.GaussianBlur(a, (0, 0), s)


def field(shape, cells_y, cells_x, seed, octaves=4):
    rng = np.random.default_rng(seed)
    out = np.zeros(shape, np.float32)
    amp, norm = 1.0, 0.0
    for o in range(octaves):
        g = rng.random((max(2, cells_y * 2 ** o), max(2, cells_x * 2 ** o))).astype(np.float32)
        out += cv2.resize(g, (shape[1], shape[0]), interpolation=cv2.INTER_CUBIC) * amp
        norm += amp
        amp *= 0.5
    return out / norm


def screen(a, b):
    return 1 - (1 - a) * (1 - b)


def over(dst_rgb, src_rgb, alpha):
    a = alpha[..., None]
    return dst_rgb * (1 - a) + src_rgb * a


# ── 마른 붓질 하나: 길이 방향으로 늘어진 결 + 끝으로 갈수록 물감이 마름 ─────────
def dry_brush(p0, p1, width, seed, dryness=0.55, start_heavy=True):
    p0, p1 = np.array(p0, np.float32), np.array(p1, np.float32)
    length = int(np.linalg.norm(p1 - p0))
    w = int(width)
    along = np.linspace(0, 1, length, dtype=np.float32)[None, :]
    across = np.linspace(-1, 1, w, dtype=np.float32)[:, None]
    streak = field((w, length), max(2, w // 6), max(2, length // 140), seed, 4)
    hair = cv2.resize(np.random.default_rng(seed + 1).random((w, max(2, length // 40))).astype(np.float32), (length, w), interpolation=cv2.INTER_LINEAR)
    edge_wobble = (field((1, length), 1, max(2, length // 90), seed + 2, 3) - 0.5) * 0.5
    half = 1.0 - 0.55 * along ** 1.6 + edge_wobble
    inside = np.clip((half - np.abs(across)) * w * 0.25, 0, 1)
    thr = dryness * (0.25 + 0.9 * along ** 1.3) + 0.25 * np.abs(across) ** 2
    if start_heavy:
        thr -= 0.35 * np.exp(-along / 0.08)
    a = np.clip((0.55 * streak + 0.45 * hair - thr) * 6.0, 0, 1) * inside
    a *= np.clip(along / 0.02, 0, 1)
    d = (p1 - p0) / max(length, 1)
    n = np.array([-d[1], d[0]], np.float32)
    origin = p0 - n * (w / 2)
    M = np.array([[d[0], n[0], origin[0]], [d[1], n[1], origin[1]]], np.float32)
    return cv2.warpAffine(a, M, (W, H), flags=cv2.INTER_LINEAR, borderValue=0)


def splatter(center, direction_deg, spread, count, seed, max_r=14):
    rng = np.random.default_rng(seed)
    a = np.zeros((H, W), np.float32)
    ang = math.radians(direction_deg)
    for _ in range(count):
        t = rng.normal(0, spread)
        s = rng.normal(0, spread * 0.25)
        x = center[0] + t * math.cos(ang) - s * math.sin(ang)
        y = center[1] + t * math.sin(ang) + s * math.cos(ang)
        r = min(max_r, 0.8 + rng.pareto(2.2) * 1.6)
        if rng.random() < 0.18:
            cv2.ellipse(a, (int(x), int(y)), (int(r * 2.6), max(1, int(r * 0.7))), direction_deg, 0, 360, 1.0, -1, cv2.LINE_AA)
        else:
            cv2.circle(a, (int(x), int(y)), max(1, int(r)), 1.0, -1, cv2.LINE_AA)
    return a


def scratches(count, seed, angle_deg=-62, spread_deg=12):
    rng = np.random.default_rng(seed)
    a = np.zeros((H, W), np.float32)
    for _ in range(count):
        x, y = rng.uniform(0, W), rng.uniform(0, H)
        ang = math.radians(angle_deg + rng.normal(0, spread_deg))
        L = rng.uniform(20, 160)
        cv2.line(a, (int(x), int(y)), (int(x + L * math.cos(ang)), int(y + L * math.sin(ang))), float(rng.uniform(0.3, 1)), 1, cv2.LINE_AA)
    return a


# ── 1. 바탕: 숯색 + 콘크리트 결 + 왼쪽 위에서 번지는 흰 연기 ──────────────────
grit = field((H, W), 9, 5, 1, 6)
speck = (np.random.default_rng(2).random((H, W)) > 0.996).astype(np.float32)
bg = np.full((H, W, 3), 0.075, np.float32) * (0.75 + 0.5 * grit)[..., None]
bg += blur(speck, 0.7)[..., None] * 0.18
smoke = field((H, W), 4, 3, 3, 5)
smoke = np.clip((smoke - 0.35) * 1.8, 0, 1) * np.exp(-(((U - 0.15) / 0.55) ** 2 + ((V - 0.12) / 0.38) ** 2))
bg = screen(bg, (smoke * 0.55)[..., None] * np.array([0.92, 0.92, 0.94]))
bg = screen(bg, (np.exp(-(((U - 0.05) / 0.35) ** 2 + ((V - 0.02) / 0.22) ** 2)) * 0.35)[..., None])
bg = screen(bg, (blur(scratches(260, 4, -62, 25), 0.4) * 0.16)[..., None])

# ── 2. 붉은 · 검은 붓질 (오른쪽 위에서 왼쪽 아래로 쓸어내림) ────────────────
red_alpha = np.zeros((H, W), np.float32)
strokes = [((1180, 120), (520, 980), 230, 10), ((1150, 420), (640, 1150), 150, 11), ((900, -40), (420, 620), 120, 12),
           ((1220, 760), (760, 1420), 190, 13), ((260, 1050), (-60, 1500), 160, 14)]
for p0, p1, w, s in strokes:
    red_alpha = np.maximum(red_alpha, dry_brush(p0, p1, w, s))
black_alpha = np.zeros((H, W), np.float32)
for p0, p1, w, s in (((1120, 300), (700, 900), 70, 21), ((980, 60), (760, 400), 45, 22), ((1200, 980), (900, 1380), 60, 23)):
    black_alpha = np.maximum(black_alpha, dry_brush(p0, p1, w, s, 0.6))
red_alpha = np.maximum(red_alpha, splatter((820, 520), -55, 210, 420, 30) * 0.95)
red_alpha = np.maximum(red_alpha, splatter((300, 1250), -50, 160, 160, 31, 9) * 0.9)
red_tex = RED * (0.75 + 0.35 * field((H, W), 30, 18, 40, 3))[..., None]
art = over(bg, red_tex, red_alpha * 0.92)
art = over(art, np.full(3, 0.03, np.float32), black_alpha * 0.85)
art = screen(art, (blur(scratches(140, 5, -55, 20), 0.3) * 0.12)[..., None])

# ── 3. 뒤 큰 글자 (윤곽선, 아주 흐리게) ────────────────────────────────────
font_card = ImageFont.truetype(str(PROJECT / "Assets/Resources/UI/Fonts/Card/esamanru Bold.ttf"), 160)
big = Image.new("L", (W, H), 0)
d = ImageDraw.Draw(big)
d.text((W * 0.5, H * 0.17), "MANAGER", font=font_card, anchor="mm", fill=0, stroke_width=3, stroke_fill=255)




def save_rgb(rgb, name):
    grain = blur(np.random.default_rng(91).normal(0, 1, (H, W)).astype(np.float32), 0.6)[..., None] * (1.6 / 255)
    Image.fromarray((np.clip(rgb + grain, 0, 1) * 255 + 0.5).astype(np.uint8)).save(OUT / name, optimize=True)


def save_rgba(rgb, alpha, name, crop=False):
    out = np.dstack([np.clip(np.broadcast_to(rgb, (H, W, 3)), 0, 1), np.clip(alpha, 0, 1)])
    im = Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGBA")
    if crop:
        im = im.crop(im.getchannel("A").getbbox())
    im.save(OUT / name, optimize=True)
    print(name, im.size)


# 바탕: 붓질 없이 결 · 연기 · 긁힘 + 아주 약한 비네팅 (붓질 · 글자는 층으로 따로)
bg_final = bg * (0.94 + 0.08 * field((H, W), 60, 34, 90, 2))[..., None]
bg_final *= (1 - 0.35 * np.clip(((U - 0.5) / 0.6) ** 2 + ((V - 0.45) / 0.75) ** 2, 0, 1))[..., None]
save_rgb(bg_final, "title_bg.png")
print("title_bg.png", (W, H))
save_rgba(red_tex, red_alpha * 0.92, "title_brush_red.png")
save_rgba(np.full(3, 0.03, np.float32), black_alpha * 0.85, "title_brush_black.png")
save_rgba(np.ones(3, np.float32), np.asarray(big, np.float32) / 255 * 0.16, "title_bigtext.png")
