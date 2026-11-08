"""
조합 배선 안을 흐르는 빛 물줄기 — 가로로 이어 붙여 반복(Repeat)되는 띠 텍스처

사용법
    python Design/ui_assets/render_combine_art.py

출력
    Assets/Resources/UI/Fx/flow_stream.png   256×32. 흰색(Image 색으로 틴트) · 가산 합성(AdditiveLight)용
        가로: 길이 · 밝기가 다른 덩어리 셋이 옅은 바탕 흐름 위에 놓여 있다 (양 끝이 이어져 끊김 없이 반복)
        세로: 가운데 가는 심 + 바깥으로 번지는 빛
"""
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "Resources" / "UI" / "Fx" / "flow_stream.png"

WIDTH, HEIGHT = 256, 32
LINEAR_ALPHA_GAMMA = 2.2  # 선형 색 공간에서 옅은 알파가 밝게 뜨므로 미리 제곱해 둔다
BASE_FLOW = 0.28          # 덩어리 사이에도 흐름이 끊기지 않게 까는 바탕
# (가운데 위치 0~1, 길이(폭 대비), 밝기)
BLOBS = [(0.12, 0.10, 1.0), (0.46, 0.16, 0.75), (0.78, 0.07, 0.9)]


def periodic_distance(x, center):
    d = np.abs(x - center)
    return np.minimum(d, 1.0 - d)


def along():
    x = np.arange(WIDTH) / WIDTH
    level = np.full(WIDTH, BASE_FLOW)
    for center, length, strength in BLOBS:
        level = np.maximum(level, BASE_FLOW + (strength - BASE_FLOW) * np.exp(-(periodic_distance(x, center) / length) ** 2))
    return level


def across():
    y = (np.arange(HEIGHT) + 0.5) / HEIGHT - 0.5
    core = np.exp(-(y / 0.09) ** 2)
    glow = np.exp(-(y / 0.26) ** 2) * 0.55
    return np.clip(core + glow, 0, 1)


def main():
    alpha = np.outer(across(), along())
    alpha = np.clip(alpha, 0, 1) ** LINEAR_ALPHA_GAMMA
    rgba = np.zeros((HEIGHT, WIDTH, 4), dtype=np.uint8)
    rgba[..., :3] = 255
    rgba[..., 3] = (alpha * 255).round().astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(OUT, optimize=True)
    print(f"  {OUT.name} {WIDTH}x{HEIGHT}")


if __name__ == "__main__":
    main()
