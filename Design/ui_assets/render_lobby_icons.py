"""
로비 메뉴 아이콘 — Phosphor Icons (MIT, icons_src/phosphor/LICENSE.txt) Fill 굵기의 윤곽만 뽑아 흰 글리프로 굽는다.
원본 SVG 에서 path 의 d 값만 꺼내 새 SVG 로 다시 쓰므로 원본의 다른 요소는 그려지지 않는다

사용법
    python Design/ui_assets/render_lobby_icons.py

출력 (128 → 2배 256x256 · 흰색 · Image 색으로 틴트)
    Assets/Resources/UI/Lobby/Icons/icon_<메뉴>.png
"""
import re
from pathlib import Path

from render_button_art import render

SOURCE = Path(__file__).resolve().parent / "icons_src" / "phosphor"
SIZE = 128
PHOSPHOR_VIEWBOX = 256

ICONS = {
    "league": "baseball",
    "lineup": "clipboard-text",
    "gacha": "cards",
    "inventory": "archive",
    "enhance": "arrow-fat-lines-up",
    "train": "barbell",
    "combine": "flask",
    "dismantle": "recycle",
    "goldenglove": "trophy",
    "save": "floppy-disk",
    "quickstart": "lightning",
}


def glyph_body(icon_name):
    text = (SOURCE / f"{icon_name}-fill.svg").read_text(encoding="utf-8")
    paths = re.findall(r'<path[^>]*\sd="([^"]+)"', text)
    if not paths:
        raise ValueError(f"{icon_name}: path 가 없습니다")
    scale = SIZE / PHOSPHOR_VIEWBOX
    shapes = "".join(f'<path d="{d}" fill="#FFFFFF"/>' for d in paths if re.fullmatch(r"[0-9MmLlHhVvCcSsQqTtAaZz,.\s\-]+", d))
    return f'<g transform="scale({scale})">{shapes}</g>'


if __name__ == "__main__":
    for key, icon in ICONS.items():
        render(f"icon_{key}", glyph_body(icon), "Lobby/Icons", SIZE, SIZE)
