"""
충격 이펙트 플립북 — 엠블럼 글자가 박힐 때 터지는 빛 · 충격선 · 불똥 / 흙먼지

사용법
    python Design/ui_assets/render_impact_fx.py

출력 Assets/Resources/UI/Title/ (4x4 = 16장, 왼쪽 위부터 가로로 · 마지막 장은 비어 있음)
    fx_impact_burst.png   가산(빛): 중심 섬광 · 방사형 충격선 · 찢긴 충격파 · 꼬리 달린 불똥   (한 장 512)
    fx_impact_dust.png    알파(먼지): 옆으로 퍼지며 흩어지는 흙먼지 덩이                  (한 장 384)
    fx_impact_preview.png 두 장을 검은 바탕에 겹친 확인용 (커밋하지 않음)
"""
import math
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

PROJECT = Path(__file__).resolve().parents[2]
OUT = PROJECT / "Assets" / "Resources" / "UI" / "Title"
COLS = ROWS = 4
FRAMES = COLS * ROWS
BURST, DUST, SS = 512, 384, 2


def blur(a, s):
    return cv2.GaussianBlur(a, (0, 0), s)


def ease_out(t, p=3.0):
    return 1 - (1 - t) ** p


def field(shape, cells_y, cells_x, seed, octaves=5):
    rng = np.random.default_rng(seed)
    out = np.zeros(shape, np.float32)
    amp, norm = 1.0, 0.0
    for o in range(octaves):
        g = rng.random((max(2, cells_y * 2 ** o), max(2, cells_x * 2 ** o))).astype(np.float32)
        out += cv2.resize(g, (shape[1], shape[0]), interpolation=cv2.INTER_CUBIC) * amp
        norm += amp
        amp *= 0.5
    return out / norm


def polar(n):
    y, x = np.mgrid[0:n, 0:n].astype(np.float32)
    dx, dy = (x - n / 2) / (n / 2), (y - n / 2) / (n / 2)
    return np.hypot(dx, dy), np.arctan2(dy, dx), dx, dy


# 둘레를 따라 매끄럽게 이어지는 잡음 (사인 겹침이라 한 바퀴 돌아도 끊기지 않는다)
def angular_noise(theta, seed, k_lo=3, k_hi=28, shift=0.0):
    rng = np.random.default_rng(seed)
    out = np.zeros_like(theta)
    for k in range(k_lo, k_hi):
        out += math.pow(k, -0.9) * np.sin(k * theta + rng.uniform(0, 2 * math.pi) + shift * k * 0.15)
    out -= out.min()
    return out / max(out.max(), 1e-6)


def edge_fade(dx, dy):
    return np.clip((0.98 - np.maximum(np.abs(dx), np.abs(dy))) / 0.1, 0, 1)


# ── 가산 섬광 한 장 ─────────────────────────────────────────────────────────────
class Burst:
    def __init__(self, seed=11):
        rng = np.random.default_rng(seed)
        self.lines = [dict(theta=rng.uniform(0, 2 * math.pi), length=rng.uniform(0.25, 0.65),
                           width=rng.uniform(0.035, 0.055) if i < 10 else rng.uniform(0.005, 0.028),
                           speed=rng.uniform(0.7, 1.15), birth=rng.uniform(0, 0.06), gain=rng.uniform(0.5, 1.0)) for i in range(84)]
        self.sparks = [dict(theta=rng.uniform(0, 2 * math.pi), speed=rng.uniform(0.4, 1.0), birth=rng.uniform(0, 0.08),
                            size=rng.uniform(2.0, 4.0), gain=rng.uniform(0.6, 1.0), flicker=rng.uniform(0, 6.28)) for _ in range(54)]
        self.r, self.theta, self.dx, self.dy = polar(BURST)
        self.rays = 0.12 + 0.88 * angular_noise(self.theta, 24, 40, 240) ** 4

    def frame(self, t):
        n, big = BURST, BURST * SS
        r, theta = self.r, self.theta
        energy = np.zeros((n, n, 3), np.float32)

        rc = 0.16 + 0.42 * ease_out(t, 2)
        core = 1.3 * math.exp(-t * 7) * np.exp(-(r / rc) ** 2 * 2.5)
        hot = np.exp(-(r / rc) ** 2 * 5)[..., None]
        energy += core[..., None] * (hot * np.array([1, 1, 1]) + (1 - hot) * np.array([1.0, 0.82, 0.55]))

        lines = np.zeros((big, big), np.uint8)
        for ln in self.lines:
            u = np.clip((t - ln["birth"]) / (1 - ln["birth"]), 0, 1)
            if u <= 0 or u >= 1:
                continue
            head = 0.06 + ln["speed"] * ease_out(u, 5) * 0.92
            tail = max(0.03, head - ln["length"] * (1 - u) ** 0.8)
            gain = ln["gain"] * (1 - u) ** 1.2
            d = np.array([math.cos(ln["theta"]), math.sin(ln["theta"])])
            p = np.array([-d[1], d[0]])
            half = ln["width"] * (1 - 0.5 * u) / 2
            pts = [d * tail, d * head + p * half, d * (head + half * 1.5), d * head - p * half]
            pix = np.array([(big / 2) * (1 + q) for q in pts], np.float32)
            cv2.fillPoly(lines, [np.round(pix * 16).astype(np.int32)], int(255 * gain), cv2.LINE_AA, shift=4)
        lines = cv2.resize(lines, (n, n), interpolation=cv2.INTER_AREA).astype(np.float32) / 255
        lines = lines + blur(lines, 5) * 0.45
        energy += lines[..., None] * np.array([1.0, 0.96, 0.88])

        reach = 0.08 + 1.05 * ease_out(t, 3)
        band = 0.10 + 0.35 * t
        wobble = (angular_noise(theta, 21, shift=t * 4) - 0.5) * 0.08
        lead = r - reach - wobble
        envelope = np.where(lead > 0, np.exp(-(lead / (band * 0.3)) ** 2), np.exp(-(lead / (band * 1.8)) ** 2))
        torn = np.clip((angular_noise(theta, 22, 5, 40, shift=t * 9) - 0.25) * 2.2, 0, 1)
        rays = envelope * self.rays * (0.35 + 0.65 * torn) * 1.05 * (1 - t) ** 1.8
        energy += rays[..., None] * np.array([1.0, 0.93, 0.8])

        sparks = np.zeros((big, big, 3), np.float32)
        for sp in self.sparks:
            u = np.clip((t - sp["birth"]) / (1 - sp["birth"]), 0, 1)
            if u <= 0 or u >= 1:
                continue
            def at(v):
                rr = 0.05 + sp["speed"] * (1 - math.exp(-4 * v)) / (1 - math.exp(-4))
                return np.array([math.cos(sp["theta"]) * rr, math.sin(sp["theta"]) * rr + 0.14 * v * v])
            a, b = at(max(0.0, u - 0.07)), at(u)
            gain = sp["gain"] * (1 - u) ** 1.3 * (0.75 + 0.25 * math.sin(sp["flicker"] + u * 40))
            col = np.array([1.0, 0.95, 0.8]) * (1 - u) + np.array([1.0, 0.5, 0.18]) * u
            pa, pb = ((big / 2) * (1 + a)).astype(int), ((big / 2) * (1 + b)).astype(int)
            cv2.line(sparks, tuple(pa), tuple(pb), tuple(float(c * gain) for c in col), max(1, int(sp["size"] * SS)), cv2.LINE_AA)
        sparks = cv2.resize(sparks, (n, n), interpolation=cv2.INTER_AREA)
        energy += sparks + blur(sparks, 3) * 0.6

        energy *= edge_fade(self.dx, self.dy)[..., None]
        alpha = np.clip(energy.max(-1), 0, 1)
        rgb = np.clip(energy / np.maximum(alpha, 1e-4)[..., None], 0, 1)
        return np.dstack([rgb, alpha])


# ── 알파 흙먼지 한 장 ───────────────────────────────────────────────────────────
class Dust:
    def __init__(self, seed=31):
        rng = np.random.default_rng(seed)
        self.puffs = []
        for i in range(18):
            side = -1 if i % 2 else 1
            ang = math.radians(rng.uniform(-28, 38)) if i < 15 else math.radians(rng.uniform(60, 120))
            d = np.array([side * math.cos(ang), -math.sin(ang) * 0.55]) if i < 15 else np.array([math.cos(ang) * 0.4, -math.sin(ang) * 0.5])
            self.puffs.append(dict(dir=d, reach=rng.uniform(0.45, 0.9), size=rng.uniform(0.10, 0.2), birth=rng.uniform(0, 0.08),
                                   seed=int(rng.integers(1000)), shade=rng.uniform(0.85, 1.05)))
        self.noise = [field((DUST, DUST), 6, 6, 40 + k, 5) for k in range(3)]
        self.y, self.x = np.mgrid[0:DUST, 0:DUST].astype(np.float32)
        _, _, self.dx, self.dy = polar(DUST)

    def frame(self, t):
        n = DUST
        keep = np.ones((n, n), np.float32)
        lit = np.zeros((n, n), np.float32)
        for k, pf in enumerate(self.puffs):
            u = np.clip((t - pf["birth"]) / (1 - pf["birth"]), 0, 1)
            if u <= 0 or u >= 1:
                continue
            c = (pf["dir"] * (0.06 + pf["reach"] * ease_out(u, 2.6)) + 1) * n / 2
            rad = pf["size"] * (0.55 + 1.3 * ease_out(u, 2)) * n / 2
            noise = self.noise[k % 3]
            warp_x = cv2.remap(noise, self.x + u * 40, self.y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)
            dist = np.hypot(self.x - c[0] + (warp_x - 0.5) * rad * 0.9, (self.y - c[1]) * 1.25 + (noise - 0.5) * rad * 0.9) / rad
            body = np.exp(-dist ** 2 * 1.6)
            grain = np.clip((cv2.remap(noise, self.x * 1.7 + pf["seed"] % 200, self.y * 1.7, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT) - 0.3) * 2.6, 0, 1)
            a = np.clip(body * (0.35 + 0.9 * grain), 0, 1) * 0.8 * (1 - u) ** 1.4 * min(1.0, u * 12)
            lit = lit * (1 - a) + a * pf["shade"] * (0.78 + 0.22 * np.clip(1 - body, 0, 1))
            keep *= 1 - a
        alpha = (1 - keep) * edge_fade(self.dx, self.dy)
        tone = lit / np.maximum(1 - keep, 1e-4)
        rgb = np.clip(tone[..., None] * np.array([0.95, 0.92, 0.87]), 0, 1)
        return np.dstack([rgb, alpha])


def sheet(make, size, name):
    out = np.zeros((size * ROWS, size * COLS, 4), np.float32)
    for k in range(FRAMES):
        t = k / (FRAMES - 1)
        f = make(t) if k < FRAMES - 1 else np.zeros((size, size, 4), np.float32)
        r, c = divmod(k, COLS)
        out[r * size:(r + 1) * size, c * size:(c + 1) * size] = f
    Image.fromarray((np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(OUT / name, optimize=True)
    print(name, out.shape[1], out.shape[0])
    return out


def preview(burst, dust):
    d = cv2.resize(dust, (BURST * COLS, BURST * ROWS), interpolation=cv2.INTER_LINEAR)
    bg = np.full(burst.shape[:2] + (3,), 0.07, np.float32)
    bg = bg * (1 - d[..., 3:]) + d[..., :3] * d[..., 3:]
    bg = np.clip(bg + burst[..., :3] * burst[..., 3:], 0, 1)
    Image.fromarray((bg * 255).astype(np.uint8)).resize((1024, 1024)).save(PROJECT / "Design" / "ui_assets" / "_fx_impact_preview.png")


preview(sheet(Burst().frame, BURST, "fx_impact_burst.png"), sheet(Dust().frame, DUST, "fx_impact_dust.png"))
