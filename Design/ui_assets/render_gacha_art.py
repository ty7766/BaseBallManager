"""
뽑기 화면 아트 — 카드 뒷면 · 밀어서 뒤집기 화살표 · 카드 모양 빛(뒤 번짐 · 테두리) · 구장 조명 배경

사용법
    python Design/ui_assets/render_gacha_art.py

출력
    Assets/Resources/UI/Card/card_back.png      카드 뒷면 1398×1957 (어두운 회색 + 은색 테두리 + 내야 다이아몬드 문장)
    Assets/Resources/UI/Gacha/swipe_arrow.png   두꺼운 화살표 + 끊어진 꼬리 막대 셋(왼쪽으로 갈수록 가늘다) 330×140 → 2배
    Assets/Resources/UI/Gacha/gacha_bg.png      구장 조명 배경 1080×2340 (위 양쪽 모서리에서 카드 자리로 엇갈리는 빛줄기)
    Assets/Resources/UI/Fx/card_glow.png        카드 뒤에서 번지는 빛 256 · 9-슬라이스 96 (가운데 = 카드, 테두리 = 바깥으로 옅어짐)
    Assets/Resources/UI/Fx/card_rim.png         카드 테두리가 달아오르는 빛 256 · 9-슬라이스 96 (카드 모서리는 바깥에서 64)
"""
import math
import subprocess
import tempfile
import time
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
UI = ROOT / "Assets" / "Resources" / "UI"
WORK = Path(tempfile.gettempdir()) / "bbm_gacha_art"
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

CW, CH = 1398, 1957
DIAMOND_HALF = 400       # 가운데 다이아몬드 반대각선 (시안 330 에서 키움)
PLATE_WIDTH = 190
BASE_SIZE = 62
BG_W, BG_H = 1080, 2340
STAGE_Y = 1330           # 빛줄기가 엇갈리는 높이 = 결과 카드 가운데 (1080×2278 기준)
LAMP_Y = 145             # 조명 높이. 제목(32~92)을 피해 재화 줄(100~190) 뒤에 숨긴다
FX_SIZE = 256
FX_BORDER = 96           # 9-슬라이스 테두리. Unity 메타의 spriteBorder 와 같다
RIM_EDGE = 64            # card_rim 에서 카드 모서리가 놓이는 자리 (바깥에서부터)
FX_CORNER = 18


def f(v):
    return f"{v:.1f}"


def card_back():
    """어두운 회색 바탕(배경 #1B1B1B 보다 한 단 밝다) + 가운데서 옅어지는 내야 격자 + 큰 다이아몬드 두 줄 ·
    은색 홈플레이트 · 1·2·3루 베이스 + 은색 테두리 두 줄"""
    cx, cy, k = CW / 2, CH / 2, DIAMOND_HALF
    out = [f'''<defs>
<linearGradient id="bg" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#3A3E46"/><stop offset="0.55" stop-color="#2A2D33"/><stop offset="1" stop-color="#202227"/></linearGradient>
<linearGradient id="rim" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#E9EEF4"/><stop offset="0.5" stop-color="#8E96A2"/><stop offset="1" stop-color="#D3D9E1"/></linearGradient>
<radialGradient id="hl" cx="0.3" cy="0.18" r="0.9"><stop offset="0" stop-color="#fff" stop-opacity="0.10"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient>
<pattern id="lat" width="120" height="120" patternUnits="userSpaceOnUse" patternTransform="rotate(45)"><rect width="120" height="120" fill="none" stroke="#fff" stroke-width="3"/></pattern>
<radialGradient id="fade" cx="0.5" cy="0.5" r="0.62"><stop offset="0" stop-color="#fff" stop-opacity="0.16"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient>
<mask id="m"><rect width="{CW}" height="{CH}" fill="url(#fade)"/></mask>
</defs>
<rect width="{CW}" height="{CH}" rx="46" fill="url(#bg)"/>
<rect width="{CW}" height="{CH}" rx="46" fill="url(#hl)"/>
<rect width="{CW}" height="{CH}" fill="url(#lat)" mask="url(#m)"/>''']
    out.append(f'<rect x="14" y="14" width="{CW - 28}" height="{CH - 28}" rx="38" fill="none" stroke="url(#rim)" stroke-width="10"/>'
               f'<rect x="56" y="56" width="{CW - 112}" height="{CH - 112}" rx="20" fill="none" stroke="#FFFFFF" stroke-opacity="0.22" stroke-width="4"/>')
    for half, opacity in ((k, 0.9), (k + 34, 0.35)):
        out.append(f'<polygon points="{f(cx)},{f(cy - half)} {f(cx + half)},{f(cy)} {f(cx)},{f(cy + half)} {f(cx - half)},{f(cy)}" '
                   f'fill="none" stroke="url(#rim)" stroke-opacity="{opacity}" stroke-width="9"/>')
    w = PLATE_WIDTH
    out.append(f'<polygon points="{f(cx - w / 2)},{f(cy - w * 0.4)} {f(cx + w / 2)},{f(cy - w * 0.4)} {f(cx + w / 2)},{f(cy + w * 0.07)} '
               f'{f(cx)},{f(cy + w * 0.53)} {f(cx - w / 2)},{f(cy + w * 0.07)}" fill="url(#rim)"/>')
    half_base = BASE_SIZE / 2
    for bx, by in ((cx + k, cy), (cx, cy - k), (cx - k, cy)):
        out.append(f'<rect x="{f(bx - half_base)}" y="{f(by - half_base)}" width="{BASE_SIZE}" height="{BASE_SIZE}" '
                   f'transform="rotate(45 {f(bx)} {f(by)})" fill="url(#rim)"/>')
    return "".join(out)


def swipe_arrow():
    """두꺼운 화살표(몸통 64) + 몸통 앞에 끊어진 막대 셋. 막대는 왼쪽으로 갈수록 가늘어져 꼬리가 흩어지는 느낌을 낸다"""
    top, bottom = 38, 102
    parts = []
    for x, w in ((0, 14), (26, 24), (62, 36)):
        parts.append(f'M{x},{top} L{x + w},{top} L{x + w},{bottom} L{x},{bottom} Z')
    parts.append(f'M110,{top} L218,{top} L218,8 L330,70 L218,132 L218,{bottom} L110,{bottom} Z')
    body = " ".join(parts)
    return ('<defs><filter id="b" x="-10%" y="-30%" width="120%" height="160%"><feGaussianBlur stdDeviation="4"/></filter></defs>'
            f'<path d="{body}" fill="#000" fill-opacity="0.4" filter="url(#b)" transform="translate(3 5)"/>'
            f'<path d="{body}" fill="#fff" fill-opacity="0.95" stroke-linejoin="round"/>')


def rounded_rect_distance(size, inset, corner):
    """정사각형 그림 안 둥근 사각형(가장자리에서 inset 만큼 안쪽)까지의 부호 있는 거리. 바깥 +, 안쪽 -"""
    ys, xs = np.mgrid[0:size, 0:size].astype(np.float32) + 0.5
    half = size / 2 - inset - corner
    qx = np.abs(xs - size / 2) - half
    qy = np.abs(ys - size / 2) - half
    outside = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0))
    inside = np.minimum(np.maximum(qx, qy), 0)
    return outside + inside - corner


def save_alpha(path, alpha):
    path.parent.mkdir(parents=True, exist_ok=True)
    rgba = np.zeros((alpha.shape[0], alpha.shape[1], 4), np.uint8)
    rgba[..., :3] = 255
    rgba[..., 3] = np.clip(alpha * 255 + 0.5, 0, 255).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(path, optimize=True)
    print(f"  {path.relative_to(UI)} {alpha.shape[1]}x{alpha.shape[0]}")


def card_glow():
    """카드 뒤 빛 번짐. 카드 모서리(테두리 안쪽 끝)에서 가장 밝고 바깥 테두리로 갈수록 0. 가운데는 카드에 가려져 꽉 채운다"""
    d = rounded_rect_distance(FX_SIZE, FX_BORDER, FX_CORNER)
    t = np.clip(d / FX_BORDER, 0, 1)
    return np.where(d <= 0, 1.0, (1 - t) ** 2.2)


def card_rim():
    """카드 테두리 빛. 카드 모서리(바깥에서 RIM_EDGE)를 중심으로 안팎으로 번지는 띠. 가운데 칸은 비운다"""
    d = rounded_rect_distance(FX_SIZE, RIM_EDGE, FX_CORNER)
    band = np.exp(-(d / 10.0) ** 2) * 0.95 + np.exp(-(d / 26.0) ** 2) * 0.45
    return np.clip(band, 0, 1)


def beam(corner_x, direction, target_y, length, spread, opacity):
    """모서리 조명(corner_x, LAMP_Y)에서 화면 가운데(target_y)를 지나 퍼지는 쐐기 모양 빛줄기"""
    ax, ay = corner_x, LAMP_Y
    vx, vy = BG_W / 2 - ax, target_y - ay
    norm = math.hypot(vx, vy)
    px, py = -vy / norm * direction, vx / norm * direction
    ex, ey = ax + vx * length, ay + vy * length
    return (f'<polygon points="{f(ax)},{f(ay)} {f(ex + px * spread)},{f(ey + py * spread)} {f(ex - px * spread)},{f(ey - py * spread)}" '
            f'fill="url(#ray)" filter="url(#b)" opacity="{opacity}"/>')


def gacha_bg():
    """구장 조명 — 화면 위 양쪽 모서리 조명에서 카드 자리로 엇갈리는 빛줄기, 교차점의 옅은 빛, 아래쪽 잔디 결"""
    out = [f'''<defs>
<linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#151925"/><stop offset="0.72" stop-color="#121418"/><stop offset="1" stop-color="#132219"/></linearGradient>
<linearGradient id="ray" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#fff" stop-opacity="0.17"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></linearGradient>
<radialGradient id="lamp" cx="0.5" cy="0.5" r="0.5"><stop offset="0" stop-color="#fff" stop-opacity="0.55"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient>
<radialGradient id="cross" cx="0.5" cy="0.5" r="0.5"><stop offset="0" stop-color="#DDE6FF" stop-opacity="0.10"/><stop offset="1" stop-color="#DDE6FF" stop-opacity="0"/></radialGradient>
<filter id="b" x="-20%" y="-20%" width="140%" height="140%"><feGaussianBlur stdDeviation="28"/></filter>
</defs>
<rect width="{BG_W}" height="{BG_H}" fill="url(#g)"/>''']
    for i in range(14):
        y = BG_H - 560 + i * 40
        out.append(f'<rect y="{y}" width="{BG_W}" height="20" fill="#3E7A4F" fill-opacity="{0.012 + i * 0.003:.3f}"/>')
    for x, d in ((0, 1), (BG_W, -1)):
        for target_y, length, spread, opacity in ((STAGE_Y, 1.4, 260, 1.0), (STAGE_Y + 170, 1.25, 170, 0.6)):
            out.append(beam(x, d, target_y, length, spread, opacity))
        out.append(f'<circle cx="{x}" cy="{LAMP_Y}" r="170" fill="url(#lamp)"/>')
        for row in range(2):
            for col in range(3):
                lx = x + d * (34 + col * 34)
                out.append(f'<circle cx="{lx}" cy="{LAMP_Y - 17 + row * 34}" r="10" fill="#fff" fill-opacity="0.5"/>')
    out.append(f'<ellipse cx="{BG_W / 2}" cy="{STAGE_Y}" rx="520" ry="380" fill="url(#cross)"/>')
    return "".join(out)


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
    render(UI / "Gacha" / "swipe_arrow.png", swipe_arrow(), 330, 140, 2)
    render(UI / "Gacha" / "gacha_bg.png", gacha_bg(), BG_W, BG_H)
    save_alpha(UI / "Fx" / "card_glow.png", card_glow())
    save_alpha(UI / "Fx" / "card_rim.png", card_rim())
