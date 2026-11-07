"""
공용 버튼 모양 — 왼쪽 위 · 오른쪽 아래는 각지고, 왼쪽 아래 · 오른쪽 위는 둥근 사각형 (9-슬라이스) · 공용 조작 아이콘

사용법
    python Design/ui_assets/render_button_art.py

출력 (48×48 → 2배 96×96 · PPU 200 · 9-슬라이스 테두리 36px)
    Assets/Resources/UI/Match/button_primary.png    주 버튼 흰 채움 (글자 #0A111D)
    Assets/Resources/UI/Match/button_normal.png     보조 버튼 흰 테두리 (색은 Image 색으로 틴트)
    Assets/Resources/UI/Match/button_disabled.png   비활성 버튼
    Assets/Resources/UI/Match/tab_on.png            켜진 탭 (어두운 채움 + 아래 흰 줄)
    Assets/Resources/UI/Common/icon_filter.png      깔때기 필터 아이콘 48 → 2배
    Assets/Resources/UI/Common/checkbox_off.png     빈 체크박스 (흰 테 — Image 색으로 틴트) 48 → 2배
    Assets/Resources/UI/Common/checkbox_on.png      체크된 체크박스 (흰 채움 + 남색 체크) 48 → 2배
    Assets/Resources/UI/Common/scroll_handle.png    스크롤바 손잡이 · 홈 (둥근 막대, 9-슬라이스 10px) 12×24 → 2배
    Assets/Resources/UI/Common/check_circle.png     선택 표시 — 채움 없이 굵은 흰 선 원 + 체크 (강화 재료) 96 → 2배
    Assets/Resources/UI/Common/icon_minus.png       빼기 막대 (Image 색으로 틴트) 48 → 2배
"""
import subprocess
import tempfile
import time
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
UI = ROOT / "Assets" / "Resources" / "UI"
WORK = Path(tempfile.gettempdir()) / "bbm_button_art"
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

SIZE = 48
SCALE = 2
RADIUS = 16              # 둥근 모서리 반지름 (화면 단위)
BORDER_PX = 36           # 9-슬라이스 테두리 (내보낸 PNG 픽셀). 둥근 모서리를 다 담는다 = 화면 18


def corner_path(inset, radius):
    """왼쪽 위 · 오른쪽 아래는 직각, 오른쪽 위 · 왼쪽 아래만 둥근 사각형 (inset 만큼 안쪽)"""
    a, b, r = inset, SIZE - inset, max(0.0, radius - inset)
    return (f'M{a},{a} H{b - r} A{r},{r} 0 0 1 {b},{a + r} V{b} H{a + r} '
            f'A{r},{r} 0 0 1 {a},{b - r} Z')


def filled(color):
    return f'<path d="{corner_path(0, RADIUS)}" fill="{color}"/>'


def ringed(fill, ring, color):
    out = filled(fill) if fill else ""
    return out + (f'<path d="{corner_path(ring / 2, RADIUS)}" fill="none" stroke="{color}" '
                  f'stroke-width="{ring}"/>')


def tab_on():
    clip = f'<defs><clipPath id="c"><path d="{corner_path(0, RADIUS)}"/></clipPath></defs>'
    return clip + filled("#18233A") + f'<rect y="{SIZE - 5}" width="{SIZE}" height="5" fill="#F3F6FA" clip-path="url(#c)"/>'


def icon_filter():
    """깔때기: 위가 넓은 사다리꼴 + 아래로 좁은 목"""
    return ('<path d="M6,9 H42 L28,25 V38 L20,42 V25 Z" fill="#FFFFFF" stroke="#FFFFFF" stroke-width="3" '
            'stroke-linejoin="round"/>')


def checkbox(checked):
    """체크박스 칸. 공용 버튼과 같은 모서리 (반지름 10)"""
    def box(inset):
        a, b, r = inset, 48 - inset, max(0.0, 10 - inset)
        return (f'M{a},{a} H{b - r} A{r},{r} 0 0 1 {b},{a + r} V{b} H{a + r} '
                f'A{r},{r} 0 0 1 {a},{b - r} Z')
    if checked:
        return (f'<path d="{box(0)}" fill="#F3F6FA"/>'
                '<path d="M12,25 L21,34 L37,15" fill="none" stroke="#0A111D" stroke-width="5.5" '
                'stroke-linecap="round" stroke-linejoin="round"/>')
    return f'<path d="{box(2)}" fill="none" stroke="#FFFFFF" stroke-width="4"/>'


def scroll_handle():
    return '<rect width="12" height="24" rx="6" fill="#FFFFFF"/>'


def check_circle():
    """어둡게 덮인 카드 위 선택 표시 — 채움 없이 굵은 흰 선 원 + 체크"""
    return ('<circle cx="48" cy="48" r="41" fill="none" stroke="#FFFFFF" stroke-width="8"/>'
            '<path d="M28,49 L42,63 L68,34" fill="none" stroke="#FFFFFF" stroke-width="10" '
            'stroke-linecap="round" stroke-linejoin="round"/>')


def icon_minus():
    return '<rect x="8" y="20" width="32" height="8" rx="4" fill="#FFFFFF"/>'


ICONS = {
    "icon_filter": (icon_filter(), 48, 48),
    "checkbox_off": (checkbox(False), 48, 48),
    "checkbox_on": (checkbox(True), 48, 48),
    "scroll_handle": (scroll_handle(), 12, 24),
    "check_circle": (check_circle(), 96, 96),
    "icon_minus": (icon_minus(), 48, 48),
}

SPRITES = {
    "button_primary": filled("#F3F6FA"),
    "button_normal": ringed(None, 3, "#FFFFFF"),
    "button_disabled": ringed("#121B2B", 2, "#1C2739"),
    "tab_on": tab_on(),
}


def render(name, body, folder="Match", width=SIZE, height=SIZE):
    """SVG 한 장을 투명 PNG로 (Edge 헤드리스 스크린샷)"""
    WORK.mkdir(parents=True, exist_ok=True)
    path = UI / folder / f"{name}.png"
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">{body}</svg>'
    html, shot = WORK / f"{name}.html", WORK / f"{name}_shot.png"
    html.write_text(f'<html><body style="margin:0;background:transparent">{svg}</body></html>', encoding="utf-8")
    if shot.exists():
        shot.unlink()
    subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run",
                    f"--user-data-dir={WORK / 'profile'}", "--default-background-color=00000000",
                    f"--force-device-scale-factor={SCALE}", "--window-size=200,200",
                    f"--screenshot={shot}", html.as_uri()],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=120)
    for _ in range(100):
        if shot.exists() and shot.stat().st_size > 0:
            break
        time.sleep(0.1)
    with Image.open(shot) as im:
        png = im.convert("RGBA").crop((0, 0, width * SCALE, height * SCALE))
    png.save(path, optimize=True)
    print(f"  {path.name} {png.size[0]}x{png.size[1]}")


if __name__ == "__main__":
    import sys
    only = set(sys.argv[1:])
    for sprite, svg_body in SPRITES.items():
        if not only or sprite in only:
            render(sprite, svg_body)
    for icon, (svg_body, icon_width, icon_height) in ICONS.items():
        if not only or icon in only:
            render(icon, svg_body, "Common", icon_width, icon_height)
