"""
라인업 화면 아트 — 야수 배치 뒤 야구장 선 · 투수 배치 뒤 야구공 실밥 (흰 선, 바탕은 화면 배경색 그대로)

사용법
    python Design/ui_assets/render_lineup_art.py

좌표는 LineUpScreen 의 수비 배치 칸(1016×1070, 왼쪽 위 원점)과 같다. 2배로 내보낸다.
홈플레이트는 C·DH 사이, 2루는 CF 아래, 1·3루 베이스는 카드 사이 틈에 오도록 내야 크기를 맞췄다.
배치 좌표(HitterSlotPoints)를 바꾸면 여기 HOME · DIAMOND_SIDE 도 같이 맞춘다

투수 탭은 3행 묶음(1016×887) 가운데를 기준으로 1016×1300 에 큰 공 테두리와 실밥 두 줄을 그린다.
공은 화면 폭보다 커서 좌우가 잘리고, 실밥은 카드 사이 틈으로 보인다

출력
    Assets/Resources/UI/Lineup/lineup_field.png      야수 탭
    Assets/Resources/UI/Lineup/pitcher_field.png     투수 탭
"""
import math
import subprocess
import tempfile
import time
from pathlib import Path

from PIL import Image

W, H = 1016, 1070
SCALE = 2
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "Resources" / "UI" / "Lineup"
WORK = Path(tempfile.gettempdir()) / "bbm_lineup_art"
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

LINE = 'stroke="#FFFFFF" stroke-opacity="0.24" stroke-width="5" fill="none" stroke-linecap="round"'
THIN = 'stroke="#FFFFFF" stroke-opacity="0.24" stroke-width="3" fill="none" stroke-linecap="round"'
MARK = 'fill="#FFFFFF" fill-opacity="0.5"'

CX = W / 2
HOME = (CX, 1000)
DIAMOND_SIDE = 293 * math.sqrt(2)        # 2루가 CF 아래(414), 1·3루가 카드 사이 틈(x 801 · 215)에 온다
FENCE_RADIUS = 990                       # 외야 펜스. 꼭대기가 CF 이름표 바로 위
BASE_PATH_GAP = 6                        # 루를 잇는 줄은 두 줄 — 가운데 선에서 양쪽으로 이만큼

PITCHER_W, PITCHER_H = 1016, 1300
BALL_RADIUS = 600                        # 공 테두리. 묶음보다 커서 좌우가 화면 밖으로 나간다
SEAM_STITCHES = 15                       # 실밥 한 줄의 바늘땀 수
INFIELD_RADIUS = DIAMOND_SIDE * 95 / 90  # 내야 잔디 경계 (마운드 중심, 실제 구장 비율)


def f(v):
    return f"{v:.1f}"


def base_points():
    """홈 · 1루 · 2루 · 3루 · 마운드"""
    half = DIAMOND_SIDE / math.sqrt(2)
    hx, hy = HOME
    return (hx, hy), (hx + half, hy - half), (hx, hy - 2 * half), (hx - half, hy - half), (hx, hy - half)


def arc(cx, cy, r, x_limit_y):
    """(cx, cy) 중심 반지름 r 의 위쪽 호. 양 끝은 x_limit_y(x) 가 주는 높이에서 끊는다"""
    pts = []
    for i in range(181):
        a = math.radians(180 + i)
        x, y = cx + math.cos(a) * r, cy + math.sin(a) * r
        if y <= x_limit_y(x):
            pts.append(f"{f(x)},{f(y)}")
    return f'<polyline points="{" ".join(pts)}" {LINE}/>'


def diamond(mound, offset):
    """마운드 중심의 다이아몬드. offset 만큼 바깥(+)·안쪽(-)으로 평행하게 옮긴 선"""
    half = DIAMOND_SIDE / math.sqrt(2) + offset * math.sqrt(2)
    mx, my = mound
    pts = ((mx, my + half), (mx + half, my), (mx, my - half), (mx - half, my))
    return f'<polygon points="{" ".join(f"{f(x)},{f(y)}" for x, y in pts)}" {LINE} stroke-width="4"/>'


def field():
    home, first, second, third, mound = base_points()
    hx, hy = home

    def foul_line_y(x):
        return hy - abs(x - hx)

    parts = [
        # 파울 라인 — 홈에서 45도로 양옆 끝까지
        f'<line x1="{f(hx)}" y1="{f(hy)}" x2="0" y2="{f(hy - hx)}" {LINE}/>',
        f'<line x1="{f(hx)}" y1="{f(hy)}" x2="{W}" y2="{f(hy - (W - hx))}" {LINE}/>',
        # 외야 펜스 · 내야 잔디 경계
        arc(hx, hy, FENCE_RADIUS, lambda x: H),
        arc(mound[0], mound[1], INFIELD_RADIUS, foul_line_y),
        # 내야 다이아몬드 (루를 잇는 줄 두 줄)
        diamond(mound, -BASE_PATH_GAP),
        diamond(mound, BASE_PATH_GAP),
        # 마운드 · 투수판
        f'<circle cx="{f(mound[0])}" cy="{f(mound[1])}" r="38" {LINE}/>',
        f'<rect x="{f(mound[0] - 14)}" y="{f(mound[1] - 4)}" width="28" height="8" {MARK}/>',
        # 타석 두 칸
        f'<rect x="{f(hx - 62)}" y="{f(hy - 34)}" width="34" height="62" {LINE} stroke-width="4"/>',
        f'<rect x="{f(hx + 28)}" y="{f(hy - 34)}" width="34" height="62" {LINE} stroke-width="4"/>',
        # 홈플레이트 (오각형)
        f'<polygon points="{f(hx - 13)},{f(hy - 13)} {f(hx + 13)},{f(hy - 13)} {f(hx + 13)},{f(hy + 2)} '
        f'{f(hx)},{f(hy + 14)} {f(hx - 13)},{f(hy + 2)}" {MARK}/>',
    ]
    for bx, by in (first, second, third):
        parts.append(f'<rect x="{f(bx - 13)}" y="{f(by - 13)}" width="26" height="26" '
                     f'transform="rotate(45 {f(bx)} {f(by)})" {MARK}/>')
    return "".join(parts)


def cubic(p0, p1, p2, p3, t):
    """3차 베지어 위의 점"""
    u = 1 - t
    return tuple(u ** 3 * a + 3 * u * u * t * b + 3 * u * t * t * c + t ** 3 * d for a, b, c, d in zip(p0, p1, p2, p3))


def pitcher_field():
    """큰 야구공 — 테두리 원 + 양쪽으로 휜 실밥 두 줄 (V자 바늘땀). 카드 아트의 실밥 무늬와 짝"""
    cx, cy, r = PITCHER_W / 2, PITCHER_H / 2, BALL_RADIUS
    parts = [f'<circle cx="{f(cx)}" cy="{f(cy)}" r="{r}" {LINE}/>']
    for side in (-1, 1):
        ends_x, bend_x = cx + side * r * 0.62, cx + side * r * 0.25
        p0, p1, p2, p3 = (ends_x, cy - r * 0.78), (bend_x, cy - r * 0.25), (bend_x, cy + r * 0.25), (ends_x, cy + r * 0.78)
        parts.append(f'<path d="M{f(p0[0])},{f(p0[1])} C{f(p1[0])},{f(p1[1])} {f(p2[0])},{f(p2[1])} '
                     f'{f(p3[0])},{f(p3[1])}" {LINE}/>')
        for i in range(1, SEAM_STITCHES + 1):
            px, py = cubic(p0, p1, p2, p3, i / (SEAM_STITCHES + 1))
            for s in (-1, 1):
                parts.append(f'<line x1="{f(px + s * 6)}" y1="{f(py)}" x2="{f(px + s * 24)}" y2="{f(py - 12)}" {THIN}/>')
    return "".join(parts)


def render(name, body, width=W, height=H):
    """SVG 한 장을 투명 PNG로. 창은 넉넉히 열고 왼쪽 위만 잘라 쓴다"""
    WORK.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">{body}</svg>'
    html, shot = WORK / f"{name}.html", WORK / f"{name}_shot.png"
    html.write_text(f'<html><body style="margin:0;background:transparent">{svg}</body></html>', encoding="utf-8")
    if shot.exists():
        shot.unlink()
    subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run",
                    f"--user-data-dir={WORK / 'profile'}", "--default-background-color=00000000",
                    f"--force-device-scale-factor={SCALE}", f"--window-size={width},{height}",
                    f"--screenshot={shot}", html.as_uri()],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=120)
    for _ in range(100):
        if shot.exists() and shot.stat().st_size > 0:
            break
        time.sleep(0.1)
    with Image.open(shot) as im:
        png = im.convert("RGBA").crop((0, 0, width * SCALE, height * SCALE))
    png.save(OUT / f"{name}.png", optimize=True)
    print(f"  {name} {png.size[0]}x{png.size[1]}")


if __name__ == "__main__":
    render("lineup_field", field())
    render("pitcher_field", pitcher_field(), PITCHER_W, PITCHER_H)
