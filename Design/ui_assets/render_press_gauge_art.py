"""
길게 누르기 원형 게이지 — 카드를 꾹 누르면 가운데에 차오르는 고리

사용법
    python Design/ui_assets/render_press_gauge_art.py

출력
    Assets/Resources/UI/Common/press_ring.png   흰 고리 128 (Image Filled · Radial360 으로 채운다)
    Assets/Resources/UI/Common/press_disc.png   고리 뒤 어두운 원판 128 (카드 그림 위에서도 고리가 보이게)
"""
from pathlib import Path

import numpy as np
from PIL import Image

UI = Path(__file__).resolve().parents[2] / "Assets" / "Resources" / "UI"

SIZE = 128
RING_OUTER = 0.92        # 반지름 비율
RING_INNER = 0.70


def radius_map():
    ys, xs = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32) + 0.5
    return np.hypot(xs - SIZE / 2, ys - SIZE / 2) / (SIZE / 2)


def press_ring():
    """가장자리 1픽셀을 부드럽게 깎은 고리"""
    r = radius_map() * (SIZE / 2)
    outer, inner = RING_OUTER * SIZE / 2, RING_INNER * SIZE / 2
    return np.clip(0.5 + outer - r, 0, 1) * np.clip(0.5 + r - inner, 0, 1)


def save_alpha(path, alpha):
    """흰색 + 알파 PNG"""
    rgba = np.zeros((alpha.shape[0], alpha.shape[1], 4), np.uint8)
    rgba[..., :3] = 255
    rgba[..., 3] = np.clip(alpha * 255 + 0.5, 0, 255).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(path, optimize=True)
    print(f"  {path.name} {alpha.shape[1]}x{alpha.shape[0]}")


def press_disc():
    """꽉 찬 원판. 가장자리만 살짝 흐리다"""
    r = radius_map()
    return np.clip((1.0 - r) / 0.06, 0, 1)


if __name__ == "__main__":
    save_alpha(UI / "Common" / "press_ring.png", press_ring())
    save_alpha(UI / "Common" / "press_disc.png", press_disc())
