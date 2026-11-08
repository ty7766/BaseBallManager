"""
카드 모양 한 곳 정의 — 왼쪽 위 · 오른쪽 아래는 직각, 오른쪽 위 · 왼쪽 아래만 사선으로 살짝 깎는다 (둥글리지 않는다)

사용법
    python Design/ui_assets/card_shape.py      카드 전체를 덮는 그림(베이스 · 전경 · 골글 장식)에 모양을 입힌다
                                               (알파를 모양과 겹치는 만큼만 남긴다 — 여러 번 돌려도 결과가 같다)
                                               + 목록 칸 덮개용 9-슬라이스 card_fill · card_frame 을 만든다
                                               + 시그 · 골글 카드 바깥 빛 테 card_fx_glow_{signature,goldenglove} 를 만든다

다른 스크립트가 가져다 쓴다
    chamfer_alpha(...)   numpy 알파 (뽑기 빛 · 마스크)
    chamfer_path(...)    SVG 경로 (카드 뒷면)
    Unity 셰이더 CardUiCommon.cginc 의 CARD_CHAMFER 도 같은 비율이다 (CHAMFER / CARD_W)
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image

CARD_W, CARD_H = 1398, 1957
CHAMFER = 72                       # 카드 원본 픽셀. 바꾸면 셰이더 CARD_CHAMFER · card_fill / card_frame 도 다시 만든다
CHAMFER_RATIO = CHAMFER / CARD_W

#목록 칸(CardCellView, 폭 328) 위에 덮는 9-슬라이스. PPU 200 이라 2px = 1단위
CELL_WIDTH = 328
SLICE_SIZE = 96
SLICE_SCALE = 2
FRAME_OFFSET = 5                   # 선택 테두리는 칸 바깥으로 5 단위
FRAME_STROKE = 5

#시그 · 골글 카드 바깥 빛 테 (9-슬라이스). 카드보다 사방 GLOW_PAD 크게 늘려 카드 뒤에 깐다 (CardView 의 Glow 칸)
GLOW_SIZE = 900
GLOW_PAD = 180
GLOW_BORDER = 280                  # 사선(72)이 늘어나는 가운데 칸에 걸치지 않게 GLOW_PAD + CHAMFER 보다 크게
GLOW_RING = 4
GLOW_BLUR = 45
GLOW_COLORS = {"signature": ("#FF5C93", "#FF6FA0"), "goldenglove": ("#F2C14E", "#F7D57A")}

ROOT = Path(__file__).resolve().parents[2]
CARD_DIR = ROOT / "Assets" / "Resources" / "UI" / "Card"
FULL_CARD_ART = [
    "card_base_normal_3", "card_base_normal_4", "card_base_normal_5", "card_base_signature", "card_base_goldenglove",
    "card_fg_normal_3", "card_fg_normal_4", "card_fg_normal_5", "card_fg_signature", "card_fg_goldenglove",
    "card_ov_goldenglove",
]


def chamfer_alpha(width, height, rect_w=None, rect_h=None, chamfer=None):
    """width×height 그림 한가운데 rect_w×rect_h 카드 모양(1 = 안쪽). 가장자리는 1픽셀 부드럽게"""
    rect_w = width if rect_w is None else rect_w
    rect_h = height if rect_h is None else rect_h
    chamfer = rect_w * CHAMFER_RATIO if chamfer is None else chamfer
    ys, xs = np.mgrid[0:height, 0:width].astype(np.float32) + 0.5
    left, top = (width - rect_w) / 2, (height - rect_h) / 2
    x, y = xs - left, ys - top
    edges = np.minimum(np.minimum(x, rect_w - x), np.minimum(y, rect_h - y))
    top_right = ((rect_w - x) + y - chamfer) / math.sqrt(2)
    bottom_left = (x + (rect_h - y) - chamfer) / math.sqrt(2)
    distance = np.minimum(edges, np.minimum(top_right, bottom_left))
    return np.clip(distance + 0.5, 0, 1)


def chamfer_path(x, y, w, h, chamfer):
    """SVG 경로. 왼쪽 위에서 시계 방향 — 오른쪽 위 · 왼쪽 아래만 사선"""
    c = chamfer
    return (f"M{x:.1f},{y:.1f} H{x + w - c:.1f} L{x + w:.1f},{y + c:.1f} V{y + h:.1f} "
            f"H{x + c:.1f} L{x:.1f},{y + h - c:.1f} Z")


def apply(path):
    """카드 전체를 덮는 그림의 알파를 카드 모양으로 깎는다"""
    with Image.open(path) as im:
        rgba = np.array(im.convert("RGBA"))
    height, width = rgba.shape[:2]
    mask = chamfer_alpha(width, height)
    rgba[..., 3] = np.minimum(rgba[..., 3], np.round(mask * 255)).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(path, optimize=True)
    print(f"  {Path(path).name} {width}x{height}")


def save_white(path, alpha):
    rgba = np.zeros((alpha.shape[0], alpha.shape[1], 4), np.uint8)
    rgba[..., :3] = 255
    rgba[..., 3] = np.clip(alpha * 255 + 0.5, 0, 255).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(path, optimize=True)
    print(f"  {Path(path).name} {alpha.shape[1]}x{alpha.shape[0]}")


def slice_sprites():
    """card_fill = 칸과 같은 모양의 흰 판(어둡게 덮기) · card_frame = 칸 바깥 5 단위에 두른 테 (선택 표시)"""
    cell_chamfer = CELL_WIDTH * CHAMFER_RATIO * SLICE_SCALE
    save_white(CARD_DIR / "card_fill.png", chamfer_alpha(SLICE_SIZE, SLICE_SIZE, chamfer=cell_chamfer))

    offset = FRAME_OFFSET * SLICE_SCALE
    stroke = FRAME_STROKE * SLICE_SCALE
    outer = chamfer_alpha(SLICE_SIZE, SLICE_SIZE, chamfer=cell_chamfer + offset * (2 - math.sqrt(2)))
    inner_size = SLICE_SIZE - stroke * 2
    inner = chamfer_alpha(SLICE_SIZE, SLICE_SIZE, inner_size, inner_size,
                          chamfer=cell_chamfer + (offset - stroke) * (2 - math.sqrt(2)))
    save_white(CARD_DIR / "card_frame.png", outer * (1 - inner))


def glow_sprite(key):
    """카드 자리(가운데 구멍)는 비우고, 둘레에 흐린 빛 + 카드 가장자리를 따라 얇은 테. 구멍과 테도 카드 모양"""
    from render_gacha_art import gaussian_blur

    glow_hex, ring_hex = GLOW_COLORS[key]
    hole_size = GLOW_SIZE - GLOW_PAD * 2
    hole = chamfer_alpha(GLOW_SIZE, GLOW_SIZE, hole_size, hole_size, chamfer=CHAMFER)
    ring_outer = chamfer_alpha(GLOW_SIZE, GLOW_SIZE, hole_size + GLOW_RING * 2, hole_size + GLOW_RING * 2,
                               chamfer=CHAMFER + GLOW_RING * (2 - math.sqrt(2)))
    glow = gaussian_blur(hole, GLOW_BLUR) * 0.5 * (1 - hole)
    ring = ring_outer * (1 - hole) * 0.58

    glow_rgb = np.array([int(glow_hex[i:i + 2], 16) for i in (1, 3, 5)], np.float32)
    ring_rgb = np.array([int(ring_hex[i:i + 2], 16) for i in (1, 3, 5)], np.float32)
    alpha = ring + glow * (1 - ring)
    rgb = (ring_rgb * ring[..., None] + glow_rgb * (glow * (1 - ring))[..., None]) / np.maximum(alpha, 1e-6)[..., None]

    rgba = np.zeros((GLOW_SIZE, GLOW_SIZE, 4), np.uint8)
    rgba[..., :3] = np.clip(rgb, 0, 255).astype(np.uint8)
    rgba[..., 3] = np.clip(alpha * 255 + 0.5, 0, 255).astype(np.uint8)
    path = CARD_DIR / f"card_fx_glow_{key}.png"
    Image.fromarray(rgba, "RGBA").save(path, optimize=True)
    print(f"  {path.name} {GLOW_SIZE}x{GLOW_SIZE} (9-슬라이스 {GLOW_BORDER})")


if __name__ == "__main__":
    names = sys.argv[1:] or FULL_CARD_ART
    for name in names:
        apply(CARD_DIR / f"{name}.png")
    if not sys.argv[1:]:
        slice_sprites()
        for glow_key in GLOW_COLORS:
            glow_sprite(glow_key)
