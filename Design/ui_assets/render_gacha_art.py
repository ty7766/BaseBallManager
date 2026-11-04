"""
뽑기 화면 아트 — 카드 뒷면 · 밀어서 뒤집기 화살표 · 공개 이펙트(빛 번짐 · 고리)

사용법
    python Design/ui_assets/render_gacha_art.py

출력
    Assets/Resources/UI/Card/card_back.png      카드 뒷면 1398×1957 (어두운 회색 + 은색 테두리 + 빨간 실밥)
    Assets/Resources/UI/Gacha/swipe_arrow.png   꼬리(왼쪽)로 갈수록 옅어지는 화살표 330×60 → 2배
    Assets/Resources/UI/Fx/glow_soft.png        흰 원형 빛 번짐 256 (등급색으로 틴트)
    Assets/Resources/UI/Fx/ring.png             흰 고리 256 (공개 순간 퍼지는 충격파)
"""
import subprocess
import tempfile
import time
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
UI = ROOT / "Assets" / "Resources" / "UI"
WORK = Path(tempfile.gettempdir()) / "bbm_gacha_art"
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

CW, CH = 1398, 1957
SEAM_RADIUS = 980        # 카드 전체를 공 한가운데로 보고 그린 실밥의 공 반지름
SEAM_STITCHES = 21


def f(v):
    return f"{v:.1f}"


def cubic(p0, p1, p2, p3, t):
    """3차 베지어 위의 점"""
    u = 1 - t
    return tuple(u ** 3 * a + 3 * u * u * t * b + 3 * u * t * t * c + t ** 3 * d for a, b, c, d in zip(p0, p1, p2, p3))


def card_back():
    """어두운 회색 바탕(배경 #1B1B1B 보다 한 단 밝다) + 왼쪽 위 은은한 빛 + 큰 실밥 두 줄(바늘땀만 빨강) + 은색 테두리 두 줄"""
    cx, cy, r = CW / 2, CH / 2, SEAM_RADIUS
    out = [f'''<defs>
<linearGradient id="bg" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#3A3E46"/><stop offset="0.55" stop-color="#2A2D33"/><stop offset="1" stop-color="#202227"/></linearGradient>
<linearGradient id="rim" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#E9EEF4"/><stop offset="0.5" stop-color="#8E96A2"/><stop offset="1" stop-color="#D3D9E1"/></linearGradient>
<radialGradient id="hl" cx="0.3" cy="0.18" r="0.9"><stop offset="0" stop-color="#fff" stop-opacity="0.10"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient>
</defs>
<rect width="{CW}" height="{CH}" rx="46" fill="url(#bg)"/>
<rect width="{CW}" height="{CH}" rx="46" fill="url(#hl)"/>''']
    for side in (-1, 1):
        ends_x, bend_x = cx + side * r * 0.62, cx + side * r * 0.22
        p0, p1, p2, p3 = (ends_x, cy - r * 0.8), (bend_x, cy - r * 0.25), (bend_x, cy + r * 0.25), (ends_x, cy + r * 0.8)
        out.append(f'<path d="M{f(p0[0])},{f(p0[1])} C{f(p1[0])},{f(p1[1])} {f(p2[0])},{f(p2[1])} {f(p3[0])},{f(p3[1])}" '
                   f'fill="none" stroke="#FFFFFF" stroke-opacity="0.28" stroke-width="7"/>')
        for i in range(1, SEAM_STITCHES + 1):
            px, py = cubic(p0, p1, p2, p3, i / (SEAM_STITCHES + 1))
            for s in (-1, 1):
                out.append(f'<line x1="{f(px + s * 10)}" y1="{f(py)}" x2="{f(px + s * 44)}" y2="{f(py - 22)}" '
                           f'stroke="#D9503F" stroke-opacity="0.85" stroke-width="9" stroke-linecap="round"/>')
    out.append(f'<rect x="14" y="14" width="{CW - 28}" height="{CH - 28}" rx="38" fill="none" stroke="url(#rim)" stroke-width="10"/>'
               f'<rect x="56" y="56" width="{CW - 112}" height="{CH - 112}" rx="20" fill="none" stroke="#FFFFFF" stroke-opacity="0.22" stroke-width="4"/>')
    return "".join(out)


def swipe_arrow():
    """가는 몸통이 왼쪽 끝에서 투명 → 오른쪽 화살촉으로 갈수록 진해진다"""
    return ('<defs><linearGradient id="a" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#fff" stop-opacity="0"/>'
            '<stop offset="1" stop-color="#fff" stop-opacity="1"/></linearGradient></defs>'
            '<rect x="0" y="26" width="300" height="8" rx="4" fill="url(#a)"/>'
            '<path d="M286,8 L318,30 L286,52" fill="none" stroke="#fff" stroke-width="8" stroke-linecap="round" stroke-linejoin="round"/>')


def glow_soft():
    return ('<defs><radialGradient id="g" cx="0.5" cy="0.5" r="0.5"><stop offset="0" stop-color="#fff" stop-opacity="1"/>'
            '<stop offset="0.45" stop-color="#fff" stop-opacity="0.45"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient></defs>'
            '<rect width="256" height="256" fill="url(#g)"/>')


def ring():
    return ('<defs><filter id="b"><feGaussianBlur stdDeviation="3"/></filter></defs>'
            '<circle cx="128" cy="128" r="112" fill="none" stroke="#fff" stroke-width="10" filter="url(#b)"/>'
            '<circle cx="128" cy="128" r="112" fill="none" stroke="#fff" stroke-width="4"/>')


def render(path, body, width, height, scale=1):
    """SVG 한 장을 투명 PNG로. 창은 넉넉히 열고 왼쪽 위만 잘라 쓴다"""
    WORK.mkdir(parents=True, exist_ok=True)
    path.parent.mkdir(parents=True, exist_ok=True)
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">{body}</svg>'
    html, shot = WORK / f"{path.stem}.html", WORK / f"{path.stem}_shot.png"
    html.write_text(f'<html><body style="margin:0;background:transparent">{svg}</body></html>', encoding="utf-8")
    if shot.exists():
        shot.unlink()
    subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run",
                    f"--user-data-dir={WORK / 'profile'}", "--default-background-color=00000000",
                    f"--force-device-scale-factor={scale}", f"--window-size={max(width, 200)},{max(height, 200)}",
                    f"--screenshot={shot}", html.as_uri()],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=120)
    for _ in range(100):
        if shot.exists() and shot.stat().st_size > 0:
            break
        time.sleep(0.1)
    with Image.open(shot) as im:
        png = im.convert("RGBA").crop((0, 0, width * scale, height * scale))
    png.save(path, optimize=True)
    print(f"  {path.relative_to(UI)} {png.size[0]}x{png.size[1]}")


if __name__ == "__main__":
    render(UI / "Card" / "card_back.png", card_back(), CW, CH)
    render(UI / "Gacha" / "swipe_arrow.png", swipe_arrow(), 330, 60, 2)
    render(UI / "Fx" / "glow_soft.png", glow_soft(), 256, 256)
    render(UI / "Fx" / "ring.png", ring(), 256, 256)
