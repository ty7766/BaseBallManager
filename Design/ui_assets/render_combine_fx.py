"""
카드 조합 이펙트 플립북 — 재료가 빛 조각으로 흩어져 오르고, 결과 자리에서 번개 갈래 · 각진 파편이 곧게 뻗는다.
중력 없이 곧은 궤적만 쓰고, 원 · 사각 같은 기본 도형 조각 대신 길쭉한 쐐기 · 깨진 유리 조각 · 갈라지는 번개로 그린다.
가산 합성용이라 render_match_fx.py 와 같은 두 겹을 싣는다: R = 흰 심, A = 색 번짐

사용법
    python Design/ui_assets/render_combine_fx.py

출력 Assets/Resources/UI/MatchFx/ (4x4 = 16장, 왼쪽 위부터 가로로 · 마지막 장은 비어 있음)
    fx_combine_absorb.png   재료: 세로 빛기둥 + 위로 솟는 쐐기 조각            (한 장 320)
    fx_combine_forge.png    결과: 비대칭 섬광 가시 + 갈라지는 번개 + 깨진 파편  (한 장 448)
    Design/ui_assets/_fx_combine_preview.png  어두운 바탕 위 확인용 (커밋하지 않음)
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


def grid(n):
    y, x = np.mgrid[0:n, 0:n].astype(np.float32)
    return (x - n / 2) / (n / 2), (y - n / 2) / (n / 2)


def edge_fade(dx, dy):
    return np.clip((0.97 - np.maximum(np.abs(dx), np.abs(dy))) / 0.12, 0, 1)


def to_px(big, pts):
    return np.round(np.array([(big / 2) * (1 + np.asarray(q)) for q in pts], np.float32) * 16).astype(np.int32)


def poly(canvas, big, pts, gain):
    cv2.fillPoly(canvas, [to_px(big, pts)], float(gain), cv2.LINE_AA, shift=4)


def wedge(canvas, big, start, end, width, gain):
    """start 는 넓고 end 로 갈수록 뾰족해지는 쐐기"""
    d = end - start
    length = max(np.linalg.norm(d), 1e-6)
    d = d / length
    p = np.array([-d[1], d[0]])
    poly(canvas, big, [start + p * width * 0.5, end, start - p * width * 0.5, start - d * width * 0.6], gain)


def shard_points(center, angle, length, width, skew):
    """깨진 유리 조각 — 길쭉한 비대칭 사각형. 한쪽 끝이 날카롭다"""
    c, s = math.cos(angle), math.sin(angle)
    local = [(length * 0.55, 0.0), (-length * 0.12, width * 0.5), (-length * 0.45, width * skew), (-length * 0.2, -width * 0.5)]
    return [center + np.array([x * c - y * s, x * s + y * c]) for x, y in local]


def lightning(rng, start, end, roughness, depth):
    """중점을 옆으로 밀어 갈라지는 번개 꺾은선"""
    pts = [np.asarray(start, np.float32), np.asarray(end, np.float32)]
    offset = roughness
    for _ in range(depth):
        nxt = [pts[0]]
        for a, b in zip(pts[:-1], pts[1:]):
            d = b - a
            normal = np.array([-d[1], d[0]])
            mid = (a + b) / 2 + normal * rng.uniform(-offset, offset)
            nxt += [mid, b]
        pts = nxt
        offset *= 0.55
    return pts


def draw_bolt(hot, glow, big, pts, width, gain):
    px = [tuple(((big / 2) * (1 + q)).astype(int)) for q in pts]
    for a, b in zip(px[:-1], px[1:]):
        cv2.line(glow, a, b, float(gain), max(1, int(width * 2.6)), cv2.LINE_AA)
        cv2.line(hot, a, b, float(gain), max(1, int(width)), cv2.LINE_AA)


class Absorb:
    """재료 카드가 빛으로 풀려 위로 빨려 오른다. 세로로 긴 빛기둥이 짧게 섰다가 꺼진다"""

    def __init__(self, size, seed):
        rng = np.random.default_rng(seed)
        self.size = size
        self.dx, self.dy = grid(size)
        self.shards = [dict(x=rng.uniform(-0.42, 0.42), y=rng.uniform(-0.2, 0.55), drift=rng.uniform(-0.12, 0.12),
                            rise=rng.uniform(0.55, 1.15), birth=rng.uniform(0, 0.3), length=rng.uniform(0.1, 0.2),
                            width=rng.uniform(0.03, 0.06), spin=rng.uniform(-2.2, 2.2), angle=rng.uniform(0, math.pi),
                            skew=rng.uniform(-0.6, 0.6), gain=rng.uniform(0.55, 1.0)) for _ in range(26)]

    def frame(self, t):
        n, big = self.size, self.size * SS
        dx, dy = self.dx, self.dy
        hot = np.zeros((n, n), np.float32)
        glow = np.zeros((n, n), np.float32)

        pillar = math.sin(min(1.0, t / 0.55) * math.pi) ** 1.4
        across = np.abs(dx) / (0.05 + 0.1 * (1 - t))
        reach = np.clip((0.55 - dy) / 1.4, 0, 1) * np.clip((dy + 0.95) / 0.3, 0, 1)
        beam = np.exp(-across ** 2) * reach
        hot += beam * pillar * 0.75
        glow += blur(beam, 6) * pillar * 0.9

        shard_hot = np.zeros((big, big), np.float32)
        shard_glow = np.zeros((big, big), np.float32)
        for sh in self.shards:
            u = np.clip((t - sh["birth"]) / (1 - sh["birth"]), 0, 1)
            if u <= 0 or u >= 1:
                continue
            travel = ease_out(u, 1.8)
            center = np.array([sh["x"] * (1 - 0.7 * travel) + sh["drift"] * travel, sh["y"] - sh["rise"] * travel])
            angle = sh["angle"] + sh["spin"] * u
            gain = sh["gain"] * min(1.0, u * 6) * (1 - u) ** 1.3
            pts = shard_points(center, angle, sh["length"] * (1 - 0.5 * u), sh["width"], sh["skew"])
            poly(shard_glow, big, pts, gain)
            poly(shard_hot, big, shard_points(center, angle, sh["length"] * 0.5, sh["width"] * 0.4, sh["skew"]), gain)
            tail = center + np.array([0.0, 0.12 + 0.18 * travel])
            wedge(shard_glow, big, tail, center, sh["width"] * 0.7, gain * 0.55)
        shard_glow = cv2.resize(shard_glow, (n, n), interpolation=cv2.INTER_AREA)
        shard_hot = cv2.resize(shard_hot, (n, n), interpolation=cv2.INTER_AREA)
        glow += shard_glow + blur(shard_glow, 3) * 0.7
        hot += shard_hot

        fade = edge_fade(dx, dy)
        return np.clip(hot * fade, 0, 1), np.clip(glow * fade, 0, 1)


class Forge:
    """새 카드가 맺히는 순간. 길이가 제각각인 섬광 가시가 비틀어 돌고, 번개 갈래가 깜빡이며 뻗고, 깨진 파편이 곧게 날아간다"""

    def __init__(self, size, seed):
        rng = np.random.default_rng(seed)
        self.size, self.seed = size, seed
        self.dx, self.dy = grid(size)
        self.spikes = [dict(theta=i / 7 * 2 * math.pi + rng.uniform(-0.25, 0.25), reach=rng.uniform(0.35, 0.95),
                            width=rng.uniform(0.025, 0.055)) for i in range(7)]
        self.bolts = [dict(theta=rng.uniform(0, 2 * math.pi), reach=rng.uniform(0.55, 0.9), birth=rng.uniform(0, 0.25),
                           life=rng.uniform(0.25, 0.45)) for _ in range(7)]
        self.shards = [dict(theta=rng.uniform(0, 2 * math.pi), speed=rng.uniform(0.45, 0.95), birth=rng.uniform(0.02, 0.15),
                            length=rng.uniform(0.06, 0.13), width=rng.uniform(0.02, 0.045), spin=rng.uniform(-3, 3),
                            skew=rng.uniform(-0.6, 0.6), gain=rng.uniform(0.6, 1.0)) for _ in range(22)]

    def frame(self, t, index):
        n, big = self.size, self.size * SS
        dx, dy = self.dx, self.dy
        hot = np.zeros((n, n), np.float32)
        glow = np.zeros((n, n), np.float32)

        core = math.exp(-t * 8)
        squash = np.sqrt((dx / 0.5) ** 2 + (dy / 0.16) ** 2) + np.sqrt((dx / 0.16) ** 2 + (dy / 0.5) ** 2)
        hot += core * np.exp(-(squash / (0.25 + 0.5 * ease_out(t, 2))) ** 2) * 1.2
        glow += math.exp(-t * 4) * np.exp(-(squash / (0.6 + 1.2 * ease_out(t, 2))) ** 2) * 0.8

        spike_hot = np.zeros((big, big), np.float32)
        spike_glow = np.zeros((big, big), np.float32)
        level = math.sin(min(1.0, t / 0.7) * math.pi) ** 1.2
        twist = 0.35 * ease_out(t, 2)
        for sp in self.spikes:
            d = np.array([math.cos(sp["theta"] + twist), math.sin(sp["theta"] + twist)])
            tip = d * (0.06 + sp["reach"] * (0.4 + 0.6 * ease_out(t, 3)))
            wedge(spike_glow, big, d * 0.0, tip, sp["width"] * 2.2, level)
            wedge(spike_hot, big, d * 0.0, tip * 0.8, sp["width"] * 0.7, level)
        spike_glow = cv2.resize(spike_glow, (n, n), interpolation=cv2.INTER_AREA)
        spike_hot = cv2.resize(spike_hot, (n, n), interpolation=cv2.INTER_AREA)
        glow += blur(spike_glow, 3) * 0.9
        hot += spike_hot * 0.9

        bolt_hot = np.zeros((big, big), np.float32)
        bolt_glow = np.zeros((big, big), np.float32)
        for k, bolt in enumerate(self.bolts):
            u = (t - bolt["birth"]) / bolt["life"]
            if u <= 0 or u >= 1:
                continue
            rng = np.random.default_rng(self.seed * 100 + k * 17 + index)
            d = np.array([math.cos(bolt["theta"]), math.sin(bolt["theta"])])
            end = d * bolt["reach"] * (0.5 + 0.5 * ease_out(u, 2))
            pts = lightning(rng, d * 0.05, end, 0.22, 4)
            gain = (1 - u) ** 0.8 * (0.75 + 0.25 * rng.uniform())
            draw_bolt(bolt_hot, bolt_glow, big, pts, SS * 1.6, gain)
            if len(pts) > 6:
                fork_from = pts[len(pts) // 2]
                fork_dir = (end - fork_from) * 0.5 + np.array([-d[1], d[0]]) * rng.uniform(-0.25, 0.25)
                draw_bolt(bolt_hot, bolt_glow, big, lightning(rng, fork_from, fork_from + fork_dir, 0.2, 3), SS * 1.1, gain * 0.7)
        bolt_glow = cv2.resize(bolt_glow, (n, n), interpolation=cv2.INTER_AREA)
        bolt_hot = cv2.resize(bolt_hot, (n, n), interpolation=cv2.INTER_AREA)
        glow += bolt_glow + blur(bolt_glow, 3) * 0.8
        hot += bolt_hot

        shard_hot = np.zeros((big, big), np.float32)
        shard_glow = np.zeros((big, big), np.float32)
        for sh in self.shards:
            u = np.clip((t - sh["birth"]) / (1 - sh["birth"]), 0, 1)
            if u <= 0 or u >= 1:
                continue
            d = np.array([math.cos(sh["theta"]), math.sin(sh["theta"])])
            center = d * (0.08 + sh["speed"] * ease_out(u, 2.6))
            angle = sh["theta"] + sh["spin"] * u
            gain = sh["gain"] * (1 - u) ** 1.2
            poly(shard_glow, big, shard_points(center, angle, sh["length"], sh["width"], sh["skew"]), gain)
            poly(shard_hot, big, shard_points(center, angle, sh["length"] * 0.45, sh["width"] * 0.35, sh["skew"]), gain)
            wedge(shard_glow, big, center - d * (0.1 + 0.15 * (1 - u)), center, sh["width"] * 0.8, gain * 0.5)
        shard_glow = cv2.resize(shard_glow, (n, n), interpolation=cv2.INTER_AREA)
        shard_hot = cv2.resize(shard_hot, (n, n), interpolation=cv2.INTER_AREA)
        glow += shard_glow + blur(shard_glow, 2.5) * 0.7
        hot += shard_hot

        fade = edge_fade(dx, dy)
        return np.clip(hot * fade, 0, 1), np.clip(glow * fade, 0, 1)


def sheet(render, size, name):
    out = np.zeros((size * ROWS, size * COLS, 4), np.float32)
    for i in range(FRAMES - 1):
        t = i / (FRAMES - 2)
        hot, glow = render(t, i)
        r, c = divmod(i, COLS)
        cell = out[r * size:(r + 1) * size, c * size:(c + 1) * size]
        cell[..., 0], cell[..., 1], cell[..., 2], cell[..., 3] = hot, hot, hot, glow
    Image.fromarray((np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(OUT / name, optimize=True)
    print(name, out.shape[1], "x", out.shape[0])
    return out


def preview(items):
    tiles = []
    for data, size, tint in items:
        frames = []
        for i in (1, 3, 5, 8, 11):
            r, c = divmod(i, COLS)
            cell = data[r * size:(r + 1) * size, c * size:(c + 1) * size]
            light = cell[..., 3:4] * np.array(tint) + cell[..., 0:1]
            base = np.array([0.09, 0.11, 0.16])
            frames.append(cv2.resize(np.clip(base + light, 0, 1), (200, 200)))
        tiles.append(np.hstack(frames))
    Image.fromarray((np.vstack(tiles) * 255).astype(np.uint8)).save(Path(__file__).resolve().parent / "_fx_combine_preview.png")


if __name__ == "__main__":
    absorb = Absorb(320, 7)
    forge = Forge(448, 11)
    a = sheet(lambda t, i: absorb.frame(t), 320, "fx_combine_absorb.png")
    f = sheet(forge.frame, 448, "fx_combine_forge.png")
    preview([(a, 320, (0.45, 0.75, 1.0)), (f, 448, (0.45, 0.75, 1.0)), (f, 448, (1.0, 0.72, 0.25))])
