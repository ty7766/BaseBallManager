"""
뽑기 연출 이펙트 — 소환 섬광 · 카드 뒤집기 폭발 · 시그니처 / 골글 눈부심 빛살 · 반짝임.
가산 합성용이라 render_match_fx.py 와 같은 두 겹을 싣는다: R = 흰 심, A = 색 번짐 (UI/Materials/AdditiveFlare).
반짝임(glint)만 흰 결 + 알파 스프라이트다 (UI/Materials/AdditiveLight)

사용법
    python Design/ui_assets/render_gacha_reveal_fx.py

출력
    Assets/Resources/UI/Gacha/fx_gacha_summon.png   4x4 플립북 (한 장 512). 빛줄기가 가운데로 빨려 들어 모였다가 터진다 — 터지는 순간 카드를 나눠 준다
    Assets/Resources/UI/Gacha/fx_reveal_burst.png   4x4 플립북 (한 장 448). 카드 둘레가 번쩍이고, 카드 가장자리에서 가시 · 깨진 파편이 곧게 뻗는다
                                                     (카드 폭 = 한 장 폭 / BURST_SCALE — GachaRevealCard 와 짝)
    모든 빛은 그림 가장자리 전에 둥글게 0 으로 사라진다 — 크게 키워도 네모 테두리가 드러나지 않는다
    Assets/Resources/UI/Gacha/fx_reveal_glare.png   1024 한 장. 길이가 제각각인 빛살 + 가로로 긴 섬광. 시그 · 골글 카드 뒤에서 돈다
    Assets/Resources/UI/Fx/glint.png                256 한 장. 네 갈래 반짝임 (십자 + 짧은 대각 + 작은 심)
    glare · glint 는 작게 줄여 그리므로 밉맵을 켜서 가져온다 (끄면 가는 줄기가 깨져 사라진다)
    Design/ui_assets/_gacha_fx_preview.png          어두운 바탕 위 확인용 (커밋하지 않음)
"""
import math
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

from render_combine_fx import blur, ease_out, grid, poly, shard_points, wedge

PROJECT = Path(__file__).resolve().parents[2]
GACHA = PROJECT / "Assets" / "Resources" / "UI" / "Gacha"
FX = PROJECT / "Assets" / "Resources" / "UI" / "Fx"
COLS = ROWS = 4
FRAMES = COLS * ROWS
SS = 2
#폭발 플립북 한 장 폭 ÷ 카드 폭 (GachaRevealCard.BurstScale 과 같아야 카드 테가 카드에 맞는다)
BURST_SCALE = 2.6
CARD_ASPECT = 1957 / 1398


def ease_in(t, p=2.0):
    return t ** p


def round_fade(r):
    """그림 가장자리에 닿기 전에 둥글게 0 으로 — 네모난 그림 경계가 빛으로 드러나지 않게 한다"""
    return np.clip((0.97 - r) / 0.3, 0, 1) ** 1.5


class Summon:
    """빛줄기가 사방에서 가운데로 빨려 들며 심이 차오르다(앞 절반) 한 번에 터져 가시 · 파편이 뻗는다(뒤 절반)"""

    def __init__(self, size, seed):
        rng = np.random.default_rng(seed)
        self.size = size
        self.dx, self.dy = grid(size)
        self.r = np.hypot(self.dx, self.dy)
        self.streaks = [dict(theta=rng.uniform(0, 2 * math.pi), start=rng.uniform(0.7, 0.98), length=rng.uniform(0.1, 0.26),
                             width=rng.uniform(0.012, 0.03), birth=rng.uniform(0, 0.45), gain=rng.uniform(0.5, 1.0))
                        for _ in range(38)]
        self.spikes = [dict(theta=i / 14 * 2 * math.pi + rng.uniform(-0.18, 0.18), reach=rng.uniform(0.45, 0.95),
                            width=rng.uniform(0.02, 0.045)) for i in range(14)]
        self.shards = [dict(theta=rng.uniform(0, 2 * math.pi), speed=rng.uniform(0.5, 0.95), length=rng.uniform(0.05, 0.1),
                            width=rng.uniform(0.018, 0.035), spin=rng.uniform(-3, 3), skew=rng.uniform(-0.6, 0.6),
                            gain=rng.uniform(0.6, 1.0)) for _ in range(20)]

    def frame(self, t, index):
        n, big = self.size, self.size * SS
        dx, dy, r = self.dx, self.dy, self.r
        hot = np.zeros((n, n), np.float32)
        glow = np.zeros((n, n), np.float32)
        gather = min(1.0, t / 0.5)
        burst = max(0.0, (t - 0.5) / 0.5)

        if t < 0.5:
            hot += np.exp(-(r / (0.02 + 0.05 * gather)) ** 2) * (0.25 + 0.95 * gather ** 2)
            glow += np.exp(-(r / (0.08 + 0.16 * gather)) ** 2) * 0.85 * gather
            line_hot = np.zeros((big, big), np.float32)
            line_glow = np.zeros((big, big), np.float32)
            for st in self.streaks:
                u = (gather - st["birth"]) / (1 - st["birth"])
                if u <= 0 or u >= 1:
                    continue
                d = np.array([math.cos(st["theta"]), math.sin(st["theta"])])
                head = d * max(0.06, st["start"] * (1 - ease_in(u, 1.7)))
                tail = head + d * st["length"] * (1 - 0.5 * u)
                gain = st["gain"] * min(1.0, u * 4) * (0.45 + 0.55 * u)
                wedge(line_glow, big, head, tail, st["width"] * 2.2, gain)
                wedge(line_hot, big, head, tail, st["width"] * 0.7, gain)
            glow += blur(cv2.resize(line_glow, (n, n), interpolation=cv2.INTER_AREA), 2.5) * 0.9
            hot += cv2.resize(line_hot, (n, n), interpolation=cv2.INTER_AREA) * 0.8
        else:
            flash = math.exp(-burst * 5)
            hot += np.exp(-(r / (0.12 + 0.45 * ease_out(burst, 2))) ** 2) * flash * 1.3
            glow += np.exp(-(r / (0.25 + 0.4 * ease_out(burst, 2))) ** 2) * math.exp(-burst * 3) * 0.9
            hot += np.exp(-(dy / 0.012) ** 2) * np.exp(-np.abs(dx) / (0.25 + 0.55 * burst)) * math.exp(-burst * 3)
            glow += np.exp(-(dy / 0.05) ** 2) * np.exp(-np.abs(dx) / (0.3 + 0.6 * burst)) * math.exp(-burst * 3) * 0.6

            spike_hot = np.zeros((big, big), np.float32)
            spike_glow = np.zeros((big, big), np.float32)
            level = (1 - burst) ** 1.3
            for sp in self.spikes:
                d = np.array([math.cos(sp["theta"]), math.sin(sp["theta"])])
                tip = d * sp["reach"] * (0.25 + 0.75 * ease_out(burst, 3))
                wedge(spike_glow, big, d * 0.0, tip, sp["width"] * 2.2, level)
                wedge(spike_hot, big, d * 0.0, tip * 0.85, sp["width"] * 0.6, level)
            for sh in self.shards:
                d = np.array([math.cos(sh["theta"]), math.sin(sh["theta"])])
                center = d * (0.06 + sh["speed"] * ease_out(burst, 2.6))
                angle = sh["theta"] + sh["spin"] * burst
                gain = sh["gain"] * (1 - burst) ** 1.2
                poly(spike_glow, big, shard_points(center, angle, sh["length"], sh["width"], sh["skew"]), gain)
                poly(spike_hot, big, shard_points(center, angle, sh["length"] * 0.45, sh["width"] * 0.35, sh["skew"]), gain)
            glow += blur(cv2.resize(spike_glow, (n, n), interpolation=cv2.INTER_AREA), 3) * 0.9
            hot += cv2.resize(spike_hot, (n, n), interpolation=cv2.INTER_AREA) * 0.9

        fade = round_fade(r)
        return np.clip(hot * fade, 0, 1), np.clip(glow * fade, 0, 1)


class RevealBurst:
    """카드 뒤에서 터진다. 카드 둘레가 번쩍이며 번지고, 카드 가장자리에서 가시 · 깨진 파편이 곧게 뻗는다 (중력 없음)"""

    def __init__(self, size, seed):
        rng = np.random.default_rng(seed)
        self.size = size
        self.dx, self.dy = grid(size)
        self.half_w = 1 / BURST_SCALE
        self.half_h = self.half_w * CARD_ASPECT
        self.spikes = [dict(theta=i / 22 * 2 * math.pi + rng.uniform(-0.12, 0.12), reach=rng.uniform(0.18, 0.55),
                            width=rng.uniform(0.014, 0.032)) for i in range(22)]
        self.shards = [dict(theta=rng.uniform(0, 2 * math.pi), speed=rng.uniform(0.2, 0.45), birth=rng.uniform(0, 0.12),
                            length=rng.uniform(0.04, 0.08), width=rng.uniform(0.014, 0.028), spin=rng.uniform(-3, 3),
                            skew=rng.uniform(-0.6, 0.6), gain=rng.uniform(0.6, 1.0)) for _ in range(18)]

    def edge_point(self, d):
        """카드 가운데에서 d 방향으로 나가 카드 테두리에 닿는 점"""
        scale = 1 / max(abs(d[0]) / self.half_w, abs(d[1]) / self.half_h)
        return d * scale

    def frame(self, t, index):
        n, big = self.size, self.size * SS
        dx, dy = self.dx, self.dy
        hot = np.zeros((n, n), np.float32)
        glow = np.zeros((n, n), np.float32)

        r = np.hypot(dx, dy)
        box = np.maximum(np.abs(dx) / self.half_w, np.abs(dy) / self.half_h)
        outside = np.maximum(box - 1, 0) * self.half_w
        hot += np.exp(-(outside / (0.012 + 0.03 * t)) ** 2) * math.exp(-t * 6) * (box > 0.97)
        glow += np.exp(-(outside / (0.05 + 0.14 * t)) ** 2) * math.exp(-t * 3) * 0.9
        glow += np.exp(-(r / (0.45 + 0.2 * t)) ** 2) * math.exp(-t * 3.5) * 0.45

        spike_hot = np.zeros((big, big), np.float32)
        spike_glow = np.zeros((big, big), np.float32)
        reach_t = ease_out(min(1.0, t / 0.6), 3)
        spike_level = (1 - t) ** 1.1
        for sp in self.spikes:
            d = np.array([math.cos(sp["theta"]), math.sin(sp["theta"])])
            base = self.edge_point(d) * 0.92
            tip = base + d * sp["reach"] * (0.2 + 0.8 * reach_t)
            wedge(spike_glow, big, base, tip, sp["width"] * 2.4, spike_level)
            wedge(spike_hot, big, base, base + (tip - base) * 0.85, sp["width"] * 0.6, spike_level)
        for sh in self.shards:
            u = np.clip((t - sh["birth"]) / (1 - sh["birth"]), 0, 1)
            if u <= 0 or u >= 1:
                continue
            d = np.array([math.cos(sh["theta"]), math.sin(sh["theta"])])
            center = self.edge_point(d) + d * sh["speed"] * ease_out(u, 2.4)
            angle = sh["theta"] + sh["spin"] * u
            gain = sh["gain"] * (1 - u) ** 1.2
            poly(spike_glow, big, shard_points(center, angle, sh["length"], sh["width"], sh["skew"]), gain)
            poly(spike_hot, big, shard_points(center, angle, sh["length"] * 0.45, sh["width"] * 0.35, sh["skew"]), gain)
        glow += blur(cv2.resize(spike_glow, (n, n), interpolation=cv2.INTER_AREA), 2.5) * 0.9
        hot += cv2.resize(spike_hot, (n, n), interpolation=cv2.INTER_AREA) * 0.9

        fade = round_fade(r)
        return np.clip(hot * fade, 0, 1), np.clip(glow * fade, 0, 1)


def glare(size, seed):
    """가로로 긴 섬광 두 줄기 + 짧은 세로 줄기 + 길이 · 굵기가 제각각인 가는 빛살 + 작은 심"""
    rng = np.random.default_rng(seed)
    dx, dy = grid(size)
    r = np.hypot(dx, dy)
    theta = np.arctan2(dy, dx)
    hot = np.zeros((size, size), np.float32)
    glow = np.zeros((size, size), np.float32)

    span = np.clip(1 - np.abs(dx), 0, 1)
    hot += np.exp(-(dy / 0.011) ** 2) * span ** 2.0
    glow += np.exp(-(dy / 0.05) ** 2) * span ** 1.3 * 0.85
    tall = np.clip(1 - np.abs(dy) / 0.75, 0, 1)
    hot += np.exp(-(dx / 0.01) ** 2) * tall ** 2.2 * 0.85
    glow += np.exp(-(dx / 0.045) ** 2) * tall ** 1.5 * 0.6

    for _ in range(22):
        center = rng.uniform(-math.pi, math.pi)
        length = rng.uniform(0.35, 0.9)
        width = rng.uniform(0.009, 0.02)
        gain = rng.uniform(0.5, 1.0)
        across = r * np.abs(np.sin(np.angle(np.exp(1j * (theta - center)))))
        ahead = np.cos(theta - center) > 0
        along = np.clip(1 - r / length, 0, 1) * ahead
        hot += np.exp(-(across / width) ** 2) * along ** 1.6 * gain
        glow += np.exp(-(across / (width * 4)) ** 2) * along ** 1.2 * gain * 0.55

    hot += np.exp(-(r / 0.06) ** 2) * 1.2
    glow += np.exp(-(r / 0.24) ** 2) * 0.8
    fade = np.clip((1 - r) / 0.08, 0, 1)
    return np.clip(hot * fade, 0, 1), np.clip(blur(glow, 1.5) * fade, 0, 1)


def glint(size):
    """네 갈래 반짝임 — 가는 가로 · 세로 갈래가 길고, 대각 갈래는 짧고 옅다"""
    dx, dy = grid(size)
    r = np.hypot(dx, dy)
    cross = (np.exp(-(dy / 0.04) ** 2) * np.clip(1 - np.abs(dx), 0, 1) ** 3
             + np.exp(-(dx / 0.04) ** 2) * np.clip(1 - np.abs(dy), 0, 1) ** 3)
    u, v = (dx + dy) / math.sqrt(2), (dx - dy) / math.sqrt(2)
    diagonal = (np.exp(-(v / 0.03) ** 2) * np.clip(1 - np.abs(u) / 0.45, 0, 1) ** 2.5
                + np.exp(-(u / 0.03) ** 2) * np.clip(1 - np.abs(v) / 0.45, 0, 1) ** 2.5) * 0.55
    core = np.exp(-(r / 0.1) ** 2)
    halo = np.exp(-(r / 0.24) ** 2) * 0.3
    alpha = np.clip(cross + diagonal + core + halo, 0, 1) * np.clip((1 - r) / 0.05, 0, 1)
    rgba = np.zeros((size, size, 4), np.float32)
    rgba[..., :3] = 1
    rgba[..., 3] = alpha
    Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), "RGBA").save(FX / "glint.png", optimize=True)
    print("glint.png", size, "x", size)
    return alpha


def sheet(render, size, path):
    out = np.zeros((size * ROWS, size * COLS, 4), np.float32)
    for i in range(FRAMES - 1):
        t = i / (FRAMES - 2)
        hot, glw = render(t, i)
        r, c = divmod(i, COLS)
        cell = out[r * size:(r + 1) * size, c * size:(c + 1) * size]
        cell[..., 0], cell[..., 1], cell[..., 2], cell[..., 3] = hot, hot, hot, glw
    Image.fromarray((np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(path, optimize=True)
    print(path.name, out.shape[1], "x", out.shape[0])
    return out


def save_single(hot, glw, path):
    rgba = np.dstack([hot, hot, hot, glw])
    Image.fromarray((np.clip(rgba, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(path, optimize=True)
    print(path.name, hot.shape[1], "x", hot.shape[0])


def preview(summon, burst, glare_pair, glint_alpha):
    base = np.array([0.1, 0.125, 0.19])
    pink = np.array([1.0, 0.44, 0.63])
    blue = np.array([0.24, 0.55, 1.0])

    def lit(cell, tint):
        return np.clip(base + cell[..., 3:4] * tint + cell[..., 0:1], 0, 1)

    rows = []
    for data, size, tint in ((summon, 512, blue), (burst, 448, pink)):
        frames = []
        for i in (1, 4, 7, 9, 11, 13):
            r, c = divmod(i, COLS)
            frames.append(cv2.resize(lit(data[r * size:(r + 1) * size, c * size:(c + 1) * size], tint), (220, 220)))
        rows.append(np.hstack(frames))
    hot, glw = glare_pair
    glare_img = cv2.resize(np.clip(base + glw[..., None] * pink + hot[..., None], 0, 1), (660, 660))
    glint_img = cv2.resize(np.clip(base + glint_alpha[..., None], 0, 1), (660, 660))
    rows.append(np.hstack([glare_img, glint_img]))
    Image.fromarray((np.vstack(rows) * 255).astype(np.uint8)).save(Path(__file__).resolve().parent / "_gacha_fx_preview.png")


if __name__ == "__main__":
    summon = Summon(512, 3)
    burst = RevealBurst(448, 17)
    s = sheet(summon.frame, 512, GACHA / "fx_gacha_summon.png")
    b = sheet(burst.frame, 448, GACHA / "fx_reveal_burst.png")
    g = glare(1024, 29)
    save_single(*g, GACHA / "fx_reveal_glare.png")
    gl = glint(256)
    preview(s, b, g, gl)
