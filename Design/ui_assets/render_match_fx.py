"""
경기 중계 이펙트 플립북 — 득점 · 홈런 · 도루. 빛만 더하는 가산 합성용이라 색은 런타임 정점색 + 셰이더 색으로 입힌다
한 장에 두 겹을 싣는다: R = 흰 심(섬광 · 줄기 심 · 불똥 머리), A = 색 번짐(빛무리 · 충격파 · 줄기 · 불똥 꼬리)

사용법
    python Design/ui_assets/render_match_fx.py

출력 Assets/Resources/UI/MatchFx/ (4x4 = 16장, 왼쪽 위부터 가로로 · 마지막 장은 비어 있음)
    fx_score.png     득점: 짧은 섬광 · 방사 줄기 12 · 찢긴 충격파 · 꼬리 달린 불똥        (한 장 320)
    fx_homerun.png   홈런: 가로 렌즈 줄 · 겹 충격파 · 긴 줄기 · 십자 반짝 · 불똥 소나기  (한 장 448)
    fx_steal.png     도루: 오른쪽으로 쏘아지는 속도선 · 머리 반짝                       (한 장 320)
    Design/ui_assets/_fx_match_preview.png  어두운 바탕 위 확인용 (커밋하지 않음)
"""
import math
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

PROJECT = Path(__file__).resolve().parents[2]
OUT = PROJECT / "Assets" / "Resources" / "UI" / "MatchFx"
OUT.mkdir(parents=True, exist_ok=True)
COLS = ROWS = 4
FRAMES = COLS * ROWS
SS = 2


def ease_out(t, p=3.0):
    return 1 - (1 - t) ** p


def blur(a, s):
    return cv2.GaussianBlur(a, (0, 0), s)


def polar(n):
    y, x = np.mgrid[0:n, 0:n].astype(np.float32)
    dx, dy = (x - n / 2) / (n / 2), (y - n / 2) / (n / 2)
    return np.hypot(dx, dy), np.arctan2(dy, dx), dx, dy


def angular_noise(theta, seed, k_lo=3, k_hi=28, shift=0.0):
    rng = np.random.default_rng(seed)
    out = np.zeros_like(theta)
    for k in range(k_lo, k_hi):
        out += math.pow(k, -0.9) * np.sin(k * theta + rng.uniform(0, 2 * math.pi) + shift * k * 0.15)
    out -= out.min()
    return out / max(out.max(), 1e-6)


def edge_fade(dx, dy):
    return np.clip((0.97 - np.maximum(np.abs(dx), np.abs(dy))) / 0.12, 0, 1)


def streak(canvas, big, start, end, width, gain):
    """start → end 로 가늘어지는 빛줄기 (끝이 뾰족한 쐐기)"""
    d = end - start
    length = max(np.linalg.norm(d), 1e-6)
    d /= length
    p = np.array([-d[1], d[0]])
    pts = [start + p * width * 0.25, end + p * width * 0.5, end + d * width * 1.4, end - p * width * 0.5, start - p * width * 0.25]
    pix = np.array([(big / 2) * (1 + q) for q in pts], np.float32)
    cv2.fillPoly(canvas, [np.round(pix * 16).astype(np.int32)], float(gain), cv2.LINE_AA, shift=4)


class RadialBurst:
    """중심에서 터지는 빛. 득점 · 홈런이 숫자만 바꿔 같이 쓴다"""

    def __init__(self, size, seed, ray_count, spark_count, ray_reach, ring_count, lens, cross):
        rng = np.random.default_rng(seed)
        self.size, self.lens, self.cross, self.ring_count = size, lens, cross, ring_count
        self.rays = [dict(theta=i / ray_count * 2 * math.pi + rng.uniform(-0.18, 0.18), reach=rng.uniform(0.55, 1.0) * ray_reach,
                          width=rng.uniform(0.03, 0.05), birth=rng.uniform(0, 0.05), gain=rng.uniform(0.6, 1.0)) for i in range(ray_count)]
        self.sparks = [dict(theta=rng.uniform(0, 2 * math.pi), speed=rng.uniform(0.35, 0.95), birth=rng.uniform(0, 0.1),
                            size=rng.uniform(1.6, 3.2), gain=rng.uniform(0.55, 1.0), flicker=rng.uniform(0, 6.28)) for _ in range(spark_count)]
        self.r, self.theta, self.dx, self.dy = polar(size)

    def frame(self, t):
        n, big = self.size, self.size * SS
        r, theta = self.r, self.theta
        hot = np.zeros((n, n), np.float32)
        glow = np.zeros((n, n), np.float32)

        flash = math.exp(-t * 9)
        hot += 1.25 * flash * np.exp(-(r / (0.07 + 0.1 * ease_out(t, 2))) ** 2)
        glow += 0.85 * math.exp(-t * 4.5) * np.exp(-(r / (0.18 + 0.3 * ease_out(t, 2))) ** 2)

        for k in range(self.ring_count):
            u = np.clip((t - k * 0.12) / (1 - k * 0.12), 0, 1)
            if u <= 0 or u >= 1:
                continue
            reach = 0.1 + (0.78 + 0.12 * k) * ease_out(u, 3)
            band = 0.018 + 0.05 * u
            wobble = (angular_noise(theta, 21 + k, shift=u * 4) - 0.5) * 0.05
            lead = r - reach - wobble
            ring = np.where(lead > 0, np.exp(-(lead / band) ** 2), np.exp(-(lead / (band * 1.3)) ** 2))
            torn = np.clip((angular_noise(theta, 30 + k, 4, 36, shift=u * 7) - 0.2) * 2.0, 0, 1)
            glow += ring * (0.4 + 0.6 * torn) * (1 - u) ** 1.6 * (0.85 - 0.25 * k)

        ray_hot = np.zeros((big, big), np.float32)
        ray_glow = np.zeros((big, big), np.float32)
        for ray in self.rays:
            u = np.clip((t - ray["birth"]) / (1 - ray["birth"]), 0, 1)
            if u <= 0 or u >= 1:
                continue
            d = np.array([math.cos(ray["theta"]), math.sin(ray["theta"])])
            head = 0.08 + ray["reach"] * ease_out(u, 4)
            tail = 0.05 + ray["reach"] * ease_out(u, 1.6) * 0.75
            gain = ray["gain"] * (1 - u) ** 1.4
            streak(ray_glow, big, d * tail, d * head, ray["width"] * (1 - 0.4 * u), gain)
            streak(ray_hot, big, d * (tail + (head - tail) * 0.35), d * head, ray["width"] * 0.35, gain)
        ray_glow = cv2.resize(ray_glow, (n, n), interpolation=cv2.INTER_AREA)
        ray_hot = cv2.resize(ray_hot, (n, n), interpolation=cv2.INTER_AREA)
        glow += ray_glow + blur(ray_glow, 4) * 0.6
        hot += ray_hot * 0.9

        if self.lens:
            ny = self.dy / 0.022
            span = (0.35 + 0.65 * ease_out(t, 2)) * 1.0
            lens = np.exp(-ny ** 2) * np.clip(1 - np.abs(self.dx) / span, 0, 1) ** 1.5 * math.exp(-t * 3.2)
            hot += lens * 0.9
            glow += blur(lens, 6) * 0.8

        if self.cross:
            u = np.clip((t - 0.05) / 0.6, 0, 1)
            if 0 < u < 1:
                level = math.sin(u * math.pi) ** 1.5
                angle = 0.4 + u * 0.5
                c, s = math.cos(angle), math.sin(angle)
                a, b = c * self.dx + s * self.dy, -s * self.dx + c * self.dy
                arm = 0.16 + 0.42 * level
                spikes = (np.exp(-(b / 0.012) ** 2) * np.clip(1 - np.abs(a) / arm, 0, 1) ** 2
                          + np.exp(-(a / 0.012) ** 2) * np.clip(1 - np.abs(b) / arm, 0, 1) ** 2)
                hot += spikes * level * 0.8
                glow += blur(spikes, 3) * level * 0.5

        spark_hot = np.zeros((big, big), np.float32)
        spark_glow = np.zeros((big, big), np.float32)
        for sp in self.sparks:
            u = np.clip((t - sp["birth"]) / (1 - sp["birth"]), 0, 1)
            if u <= 0 or u >= 1:
                continue

            def at(v):
                rr = 0.06 + sp["speed"] * (1 - math.exp(-4 * v)) / (1 - math.exp(-4))
                return np.array([math.cos(sp["theta"]) * rr, math.sin(sp["theta"]) * rr + 0.12 * v * v])

            a, b = at(max(0.0, u - 0.09)), at(u)
            gain = sp["gain"] * (1 - u) ** 1.2 * (0.7 + 0.3 * math.sin(sp["flicker"] + u * 38))
            pa, pb = tuple(((big / 2) * (1 + a)).astype(int)), tuple(((big / 2) * (1 + b)).astype(int))
            cv2.line(spark_glow, pa, pb, float(gain), max(1, int(sp["size"] * SS)), cv2.LINE_AA)
            cv2.circle(spark_hot, pb, max(1, int(sp["size"] * SS * 0.6)), float(gain), -1, cv2.LINE_AA)
        spark_glow = cv2.resize(spark_glow, (n, n), interpolation=cv2.INTER_AREA)
        spark_hot = cv2.resize(spark_hot, (n, n), interpolation=cv2.INTER_AREA)
        glow += spark_glow + blur(spark_glow, 2.5) * 0.7
        hot += spark_hot

        fade = edge_fade(self.dx, self.dy)
        return np.clip(hot * fade, 0, 1), np.clip(glow * fade, 0, 1)


class SpeedDash:
    """오른쪽으로 쏘아지는 속도선 묶음. 선 머리에 작은 반짝"""

    def __init__(self, size, seed):
        rng = np.random.default_rng(seed)
        self.size = size
        self.lines = [dict(y=rng.uniform(-0.32, 0.32), birth=rng.uniform(0, 0.25), length=rng.uniform(0.6, 1.1),
                           width=rng.uniform(0.014, 0.032), gain=rng.uniform(0.5, 1.0)) for _ in range(12)]
        _, _, self.dx, self.dy = polar(size)

    def frame(self, t):
        n, big = self.size, self.size * SS
        hot = np.zeros((big, big), np.float32)
        glow = np.zeros((big, big), np.float32)
        for ln in self.lines:
            u = np.clip((t - ln["birth"]) / (1 - ln["birth"]), 0, 1)
            if u <= 0 or u >= 1:
                continue
            head = -0.85 + 1.75 * ease_out(u, 2.4)
            tail = head - ln["length"] * (1 - u * 0.6)
            gain = ln["gain"] * min(1.0, u * 8) * (1 - u) ** 1.1
            start, end = np.array([tail, ln["y"]]), np.array([head, ln["y"]])
            streak(glow, big, start, end, ln["width"] * 2.2, gain)
            streak(hot, big, start + (end - start) * 0.55, end, ln["width"] * 0.8, gain)
            cv2.circle(hot, tuple(((big / 2) * (1 + end)).astype(int)), max(1, int(ln["width"] * big * 0.6)), float(gain), -1, cv2.LINE_AA)
        hot = cv2.resize(hot, (n, n), interpolation=cv2.INTER_AREA)
        glow = cv2.resize(glow, (n, n), interpolation=cv2.INTER_AREA)
        glow = glow + blur(glow, 5) * 0.9
        fade = edge_fade(self.dx, self.dy)
        return np.clip(hot * fade, 0, 1), np.clip(glow * fade, 0, 1)


def sheet(effect, name):
    size = effect.size
    out = np.zeros((size * ROWS, size * COLS, 4), np.float32)
    for k in range(FRAMES - 1):
        hot, glow = effect.frame(k / (FRAMES - 2) * 0.97)
        r, c = divmod(k, COLS)
        cell = out[r * size:(r + 1) * size, c * size:(c + 1) * size]
        cell[..., 0], cell[..., 1], cell[..., 2], cell[..., 3] = hot, hot, hot, glow
    Image.fromarray((np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(OUT / name, optimize=True)
    print(name, out.shape[1], out.shape[0])
    return out


def preview(sheets_with_tint):
    tiles = []
    for data, tint in sheets_with_tint:
        size = data.shape[0] // ROWS
        bg = np.full(data.shape[:2] + (3,), 0.07, np.float32)
        bg += data[..., 3:4] * np.array(tint) + data[..., 0:1] * np.array([1.0, 0.97, 0.9])
        tiles.append(cv2.resize(np.clip(bg, 0, 1), (768, 768), interpolation=cv2.INTER_AREA))
    Image.fromarray((np.hstack(tiles) * 255).astype(np.uint8)).save(Path(__file__).resolve().parent / "_fx_match_preview.png")


if __name__ == "__main__":
    score = sheet(RadialBurst(320, 7, 12, 22, 0.8, 1, False, False), "fx_score.png")
    homerun = sheet(RadialBurst(448, 13, 20, 40, 0.95, 2, True, True), "fx_homerun.png")
    steal = sheet(SpeedDash(320, 5), "fx_steal.png")
    preview([(score, (1.0, 0.62, 0.2)), (homerun, (1.0, 0.78, 0.3)), (steal, (0.55, 0.85, 1.0))])
