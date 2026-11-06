"""
카드 잠금 자물쇠 — 카드 왼쪽 아래 토글 버튼 그림 (닫힘 · 열림)

사용법
    python Design/ui_assets/render_card_lock_art.py [A|B|C|D|AC]     (기본 = OPTION, 채택안 AC)

출력
    Assets/Resources/UI/Card/lock_closed.png    잠김 200×200 → 2배
    Assets/Resources/UI/Card/lock_open.png      풀림 200×200 → 2배

안
    A 메달 배지   남색 원판 + 은테. 잠김은 금빛 자물쇠, 풀림은 회색 자물쇠에 고리가 들린다
    B 각진 칩     반투명 남색 둥근 네모 + 흰 테. 잠김은 꽉 찬 흰 자물쇠, 풀림은 선만 남은 자물쇠
    C 금속 자물쇠 바탕 없이 그림자만. 잠김은 황동, 풀림은 강철빛으로 고리가 들린다
    D 방패 문장   카드 뒷면 문장처럼 방패. 잠김은 금 방패 속 검은 자물쇠, 풀림은 어두운 방패 속 은빛 열린 자물쇠
    AC (채택)     A 의 메달 원판 위에 C 의 금속 자물쇠 색. 잠김은 황동, 풀림은 강철빛에 고리가 들린다
"""
import subprocess
import sys
import tempfile
import time
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
UI = ROOT / "Assets" / "Resources" / "UI"
WORK = Path(tempfile.gettempdir()) / "bbm_lock_art"
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

OPTION = "AC"
SIZE = 200

BODY = (52, 94, 96, 76)      # 자물쇠 몸통 x, y, 폭, 높이
SHACKLE_LEFT, SHACKLE_RIGHT = 72, 128
SHACKLE_TOP = 70             # 닫힌 고리 꼭대기 (호의 시작 높이)
OPEN_LIFT = 24               # 풀리면 고리가 올라가는 거리


def shackle_path(opened):
    """닫힘: 두 다리가 몸통에 박힌 고리. 열림: 고리가 들리고 오른쪽 다리가 몸통 밖으로 빠진다"""
    top = SHACKLE_TOP - (OPEN_LIFT if opened else 0)
    radius = (SHACKLE_RIGHT - SHACKLE_LEFT) / 2
    right_end = top + 16 if opened else BODY[1] + 4
    return (f'M{SHACKLE_LEFT},{BODY[1] + 4} V{top} A{radius},{radius} 0 0 1 {SHACKLE_RIGHT},{top} '
            f'V{right_end}')


def keyhole(fill):
    x, y = 100, BODY[1] + 32
    return (f'<circle cx="{x}" cy="{y}" r="10" fill="{fill}"/>'
            f'<path d="M{x - 5},{y + 4} L{x + 5},{y + 4} L{x + 6},{y + 24} L{x - 6},{y + 24} Z" fill="{fill}"/>')


def body_rect(fill, stroke="none", stroke_width=0, opacity=1):
    x, y, w, h = BODY
    return (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="14" fill="{fill}" '
            f'stroke="{stroke}" stroke-width="{stroke_width}" opacity="{opacity}"/>')


DEFS = '''<defs>
<linearGradient id="gold" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFE7A0"/><stop offset="0.55" stop-color="#F2C14E"/><stop offset="1" stop-color="#C98A1E"/></linearGradient>
<linearGradient id="steel" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#F3F6FA"/><stop offset="0.5" stop-color="#AEB8C6"/><stop offset="1" stop-color="#7C8796"/></linearGradient>
<linearGradient id="brass" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#FFE9A8"/><stop offset="0.45" stop-color="#E2A93B"/><stop offset="1" stop-color="#9A6516"/></linearGradient>
<linearGradient id="dim" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#C4CCD8"/><stop offset="1" stop-color="#7E8898"/></linearGradient>
<filter id="drop" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur in="SourceAlpha" stdDeviation="5"/><feOffset dy="4"/><feComponentTransfer><feFuncA type="linear" slope="0.6"/></feComponentTransfer><feMerge><feMergeNode/><feMergeNode in="SourceGraphic"/></feMerge></filter>
</defs>'''


def option_a(locked):
    ring = "#D9E0EA" if locked else "#6E7C92"
    plate = (f'<circle cx="100" cy="100" r="90" fill="#0F1622" fill-opacity="0.92" stroke="{ring}" stroke-width="6"/>')
    shackle = f'<path d="{shackle_path(not locked)}" fill="none" stroke="url(#steel)" stroke-width="15" stroke-linecap="round"/>'
    body = body_rect("url(#gold)" if locked else "url(#dim)")
    return DEFS + f'<g filter="url(#drop)">{plate}{shackle}{body}{keyhole("#3A2608" if locked else "#2A2F36")}</g>'


def option_b(locked):
    plate = ('<rect x="14" y="14" width="172" height="172" rx="38" fill="#0A111D" fill-opacity="0.74" '
             f'stroke="#F3F6FA" stroke-opacity="{0.9 if locked else 0.45}" stroke-width="5"/>')
    color = "#F3F6FA"
    shackle = (f'<path d="{shackle_path(not locked)}" fill="none" stroke="{color}" stroke-width="13" '
               f'stroke-linecap="round" opacity="{1 if locked else 0.7}"/>')
    if locked:
        body = body_rect(color) + keyhole("#0A111D")
    else:
        body = body_rect("none", color, 11, 0.7)
    return DEFS + plate + shackle + body


def option_c(locked):
    shackle = (f'<path d="{shackle_path(not locked)}" fill="none" stroke="url(#steel)" stroke-width="17" '
               'stroke-linecap="round"/>')
    body = body_rect("url(#brass)" if locked else "url(#steel)", "#1B1206" if locked else "#2A2F36", 3)
    shine = '<rect x="60" y="100" width="80" height="12" rx="6" fill="#FFFFFF" opacity="0.35"/>'
    return DEFS + f'<g filter="url(#drop)" opacity="{1 if locked else 0.9}">{shackle}{body}{shine}{keyhole("#2A1A04" if locked else "#1F242B")}</g>'


def option_d(locked):
    shield = 'M100,12 L178,40 V98 C178,148 144,178 100,192 C56,178 22,148 22,98 V40 Z'
    if locked:
        plate = f'<path d="{shield}" fill="url(#gold)" stroke="#FFF3C4" stroke-width="4"/>'
        lock_color, hole = "#1A1206", "#F2C14E"
    else:
        plate = f'<path d="{shield}" fill="#0F1622" fill-opacity="0.92" stroke="#AEB8C6" stroke-width="5"/>'
        lock_color, hole = "#C9D1DC", "#0F1622"
    group = (f'<g transform="translate(100 104) scale(0.72) translate(-100 -110)">'
             f'<path d="{shackle_path(not locked)}" fill="none" stroke="{lock_color}" stroke-width="16" stroke-linecap="round"/>'
             f'{body_rect(lock_color)}{keyhole(hole)}</g>')
    return DEFS + f'<g filter="url(#drop)">{plate}{group}</g>'


def option_ac(locked):
    ring = "#D9E0EA" if locked else "#6E7C92"
    plate = (f'<circle cx="100" cy="100" r="90" fill="#0F1622" fill-opacity="0.92" stroke="{ring}" stroke-width="6"/>')
    shackle = (f'<path d="{shackle_path(not locked)}" fill="none" stroke="url(#steel)" stroke-width="15" '
               'stroke-linecap="round"/>')
    body = body_rect("url(#brass)" if locked else "url(#steel)", "#1B1206" if locked else "#2A2F36", 3)
    shine = '<rect x="60" y="100" width="80" height="12" rx="6" fill="#FFFFFF" opacity="0.35"/>'
    return DEFS + f'<g filter="url(#drop)">{plate}{shackle}{body}{shine}{keyhole("#2A1A04" if locked else "#1F242B")}</g>'


OPTIONS = {"A": option_a, "B": option_b, "C": option_c, "D": option_d, "AC": option_ac}


def render(path, body, scale):
    """SVG 한 장을 투명 PNG로 (Edge 헤드리스 스크린샷)"""
    WORK.mkdir(parents=True, exist_ok=True)
    path.parent.mkdir(parents=True, exist_ok=True)
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{SIZE}" height="{SIZE}" viewBox="0 0 {SIZE} {SIZE}">{body}</svg>'
    html, shot = WORK / f"{path.stem}.html", WORK / f"{path.stem}_shot.png"
    html.write_text(f'<html><body style="margin:0;background:transparent">{svg}</body></html>', encoding="utf-8")
    if shot.exists():
        shot.unlink()
    subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run",
                    f"--user-data-dir={WORK / 'profile'}", "--default-background-color=00000000",
                    f"--force-device-scale-factor={scale}", "--window-size=200,200",
                    f"--screenshot={shot}", html.as_uri()],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=120)
    for _ in range(100):
        if shot.exists() and shot.stat().st_size > 0:
            break
        time.sleep(0.1)
    with Image.open(shot) as im:
        png = im.convert("RGBA").crop((0, 0, SIZE * scale, SIZE * scale))
    png.save(path, optimize=True)
    print(f"  {path.name} {png.size[0]}x{png.size[1]}")


def render_option(option, folder, scale=2):
    draw = OPTIONS[option]
    render(folder / "lock_closed.png", draw(True), scale)
    render(folder / "lock_open.png", draw(False), scale)


if __name__ == "__main__":
    render_option(sys.argv[1] if len(sys.argv) > 1 else OPTION, UI / "Card")
