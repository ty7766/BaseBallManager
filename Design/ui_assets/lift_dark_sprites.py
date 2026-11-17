"""
검정에 가깝게 칠해 둔 UI 판 스프라이트를 남회색 쪽으로 들어 올린다 (화면 바탕에 검정을 쓰지 않는다 · 세션 68).
render_assets.py · render_gacha_art.py 가 처음 그린 그림 위에 한 번 더 덮어쓰는 후처리다 — 다시 그리면 이것도 다시 돌린다

사용법
    python Design/ui_assets/lift_dark_sprites.py

바꾸는 것 (Assets/Resources/UI/)
    판(어두운 남색 · 숯색)         rgb × LIFT_GAIN + LIFT_OFFSET — 흰 테두리 · 밝은 선은 그대로
    Match/button_normal_fill.png  가운데 회색 채움(0.45)을 0.8 로 올린다. 색은 실행 중에 BgRow 를 곱한다
"""
from pathlib import Path

import numpy as np
from PIL import Image

UI = Path(__file__).resolve().parents[2] / "Assets" / "Resources" / "UI"
DARK_PLATES = ["Gacha/tab_track.png", "Match/button_disabled.png", "Match/cut_panel.png", "Match/log_inning_header.png",
               "Match/row_normal.png", "Match/row_pitching.png", "Match/row_used.png", "Match/stamina_bar_frame.png"]
LIFT_GAIN = 1.45
LIFT_OFFSET = np.array([10, 13, 20], np.float32)
FILL_FROM, FILL_TO = 115, 205


def lift_plate(path):
    rgba = np.asarray(Image.open(path).convert("RGBA")).astype(np.float32)
    rgb = rgba[..., :3]
    dark = rgb.max(axis=2, keepdims=True) < 120
    rgba[..., :3] = np.where(dark, np.clip(rgb * LIFT_GAIN + LIFT_OFFSET, 0, 255), rgb)
    Image.fromarray((rgba + 0.5).astype(np.uint8), "RGBA").save(path, optimize=True)


def lift_button_fill(path):
    rgba = np.asarray(Image.open(path).convert("RGBA")).astype(np.float32)
    rgb = rgba[..., :3]
    scale = (255 - FILL_TO) / (255 - FILL_FROM)
    rgba[..., :3] = np.clip(255 - (255 - rgb) * scale, 0, 255)
    Image.fromarray((rgba + 0.5).astype(np.uint8), "RGBA").save(path, optimize=True)


if __name__ == "__main__":
    for name in DARK_PLATES:
        lift_plate(UI / name)
        print(name)
    lift_button_fill(UI / "Match" / "button_normal_fill.png")
    print("Match/button_normal_fill.png")
