"""
로비 엠블럼 반짝임 플립북 — 빛 띠가 지나간 끝자리에서 별빛이 한 번 번쩍이고 사라진다 (가산 합성 · 알파만 쓴다)
가는 가로 · 세로 빛줄기가 길게 뻗고, 짧은 대각선 줄기와 작은 심 · 옅은 번짐이 함께 커졌다가 줄기부터 사그라든다

사용법
    python Design/ui_assets/render_lobby_fx.py

출력
    Assets/Resources/UI/Lobby/fx_glint.png   4x4 플립북 (칸 256 · 왼쪽 위부터 오른쪽으로 · 마지막 칸은 빈 칸)
    Design/ui_assets/_fx_glint_preview.png   어두운 바탕 위 미리보기 (커밋하지 않는다)
"""
from pathlib import Path

import numpy as np
from PIL import Image

PROJECT = Path(__file__).resolve().parents[2]
OUT = PROJECT / "Assets" / "Resources" / "UI" / "Lobby"
OUT.mkdir(parents=True, exist_ok=True)
CELL = 256
GRID = 4
FRAMES = GRID * GRID
SUPER = 2


def ease_out(t, power=3):
    return 1 - (1 - t) ** power


def ray(x, y, length, width):
    """원점에서 x 축 양쪽으로 뻗는 가는 빛줄기 — 길이 방향은 지수로 가늘어지고, 폭 방향은 가우시안"""
    along = np.exp(-np.abs(x) / max(length, 1e-3))
    taper = width * (0.35 + 0.65 * along)
    return along * np.exp(-(y / taper) ** 2)


def frame(t, size):
    half = size / 2
    yy, xx = (np.mgrid[0:size, 0:size].astype(np.float32) - half + 0.5) / SUPER
    rise = ease_out(min(t / 0.22, 1.0), 2)
    fade = np.exp(-max(t - 0.22, 0) * 4.2)
    level = rise * fade
    angle = np.radians(8 + 14 * t)
    c, s = np.cos(angle), np.sin(angle)
    u, v = c * xx + s * yy, -s * xx + c * yy
    main_len = 12 + 46 * ease_out(t, 2)
    diag_len = main_len * 0.42
    rays = ray(u, v, main_len, 1.3) + ray(v, u, main_len * 0.86, 1.3)
    du, dv = (u + v) / np.sqrt(2), (u - v) / np.sqrt(2)
    rays += 0.38 * (ray(du, dv, diag_len, 1.0) + ray(dv, du, diag_len, 1.0))
    radius2 = xx ** 2 + yy ** 2
    core = np.exp(-radius2 / (2 * (2.2 + 2.0 * rise) ** 2))
    halo = 0.16 * np.exp(-radius2 / (2 * (12 + 10 * t) ** 2))
    ray_fade = np.exp(-max(t - 0.3, 0) * 2.5)
    out = (rays * ray_fade * 0.9 + core * 1.1 + halo) * level
    edge = np.clip((half / SUPER - np.sqrt(radius2)) / (12 / SUPER * SUPER), 0, 1)
    return np.clip(out * edge, 0, 1)


def downsample(a):
    return a.reshape(CELL, SUPER, CELL, SUPER).mean(axis=(1, 3))


if __name__ == "__main__":
    sheet = np.zeros((CELL * GRID, CELL * GRID), np.float32)
    for index in range(FRAMES - 1):
        t = index / (FRAMES - 2)
        row, column = divmod(index, GRID)
        sheet[row * CELL:(row + 1) * CELL, column * CELL:(column + 1) * CELL] = downsample(frame(t, CELL * SUPER))
    rgba = np.dstack([np.ones_like(sheet)] * 3 + [sheet])
    Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), "RGBA").save(OUT / "fx_glint.png", optimize=True)
    preview = np.clip(0.06 + sheet * 0.94, 0, 1)
    Image.fromarray((preview * 255).astype(np.uint8)).save(Path(__file__).resolve().parent / "_fx_glint_preview.png")
    print("fx_glint.png", sheet.shape)
