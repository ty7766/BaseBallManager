"""
뽑기 화면 아트 — 카드 뒷면 · 밀어서 뒤집기 화살표 · 뒤집을 때 새는 빛 · 공개 빛 번짐 · 구장 조명 배경

사용법
    python Design/ui_assets/render_gacha_art.py

출력
    Assets/Resources/UI/Card/card_back.png      카드 뒷면 1398×1957 (어두운 회색 + 은색 테두리 + 내야 다이아몬드 문장)
    Assets/Resources/UI/Gacha/swipe_arrow.png   두꺼운 화살표(몸통 76) + 끊어진 꼬리 막대 셋(왼쪽으로 갈수록 가늘다) 330×164 → 2배
    Assets/Resources/UI/Gacha/gacha_bg.png      구장 조명 배경 1080×2340 (위 양쪽 모서리에서 카드 자리로 엇갈리는 빛줄기)
    Assets/Resources/UI/Fx/card_spill.png       들린 카드 틈에서 바닥으로 새는 빛 256×512. 오른쪽 변 = 카드 가장자리, 왼쪽으로 타원으로 옅어진다
    Assets/Resources/UI/Fx/card_bloom.png       앞면이 드러날 때 카드에서 번지는 빛. 카드 실루엣을 흐린 것, 카드 둘레에 카드 폭 절반씩 여백
    Assets/Resources/UI/Fx/card_mask.png        카드 모양(모서리 둥근 흰 판). 공개 순간 앞면이 번쩍이는 덮개
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
CARD_CORNER = 46 / CW     # 카드 모서리 반지름 (카드 폭 비율)
SPILL_W, SPILL_H = 256, 512
SPILL_SLIT = 1 / 1.5     # 새는 빛 그림 높이 중 틈(광원을 늘어놓는 줄)이 차지하는 비율
SPILL_REACH = 0.9        # 그림 폭이 가장 넓게 펴졌을 때의 길이 (카드 폭 단위). 둥근 정도를 이 비율로 맞춘다
SPILL_HALF_HEIGHT = 1.05 # 그림 높이의 절반 (같은 단위). 세로 ÷ 가로 = 2.1 / 0.9 는 Unity 의 SpillAspect 와 같다
BLOOM_CARD_W = 180       # 번짐 그림에서 카드 폭(픽셀)
BLOOM_MARGIN = 0.5       # 카드 둘레 여백 (카드 폭 비율). Unity 의 BloomMarginRatio 와 같다
MASK_W = 280
LINEAR_ALPHA_GAMMA = 2.2  # Unity 는 선형 색 공간에서 섞어 옅은 알파가 밝게 뜬다. 빛 알파를 미리 제곱해 감마 공간에서 본 번짐과 맞춘다


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
    """두꺼운 화살표(몸통 76) + 몸통 앞에 끊어진 막대 셋. 막대는 왼쪽으로 갈수록 가늘어져 꼬리가 흩어지는 느낌을 낸다"""
    top, bottom = 42, 118
    parts = []
    for x, w in ((0, 14), (26, 24), (62, 36)):
        parts.append(f'M{x},{top} L{x + w},{top} L{x + w},{bottom} L{x},{bottom} Z')
    parts.append(f'M110,{top} L214,{top} L214,10 L330,80 L214,150 L214,{bottom} L110,{bottom} Z')
    body = " ".join(parts)
    return ('<defs><filter id="b" x="-10%" y="-30%" width="120%" height="160%"><feGaussianBlur stdDeviation="4"/></filter></defs>'
            f'<path d="{body}" fill="#000" fill-opacity="0.4" filter="url(#b)" transform="translate(3 5)"/>'
            f'<path d="{body}" fill="#fff" fill-opacity="0.95" stroke-linejoin="round"/>')


def save_alpha(path, alpha):
    path.parent.mkdir(parents=True, exist_ok=True)
    rgba = np.zeros((alpha.shape[0], alpha.shape[1], 4), np.uint8)
    rgba[..., :3] = 255
    rgba[..., 3] = np.clip(alpha * 255 + 0.5, 0, 255).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(path, optimize=True)
    print(f"  {path.relative_to(UI)} {alpha.shape[1]}x{alpha.shape[0]}")


def rounded_rect_mask(width, height, rect_w, rect_h, corner):
    """width×height 그림 한가운데 rect_w×rect_h 둥근 사각형. 가장자리 1픽셀을 부드럽게 깎는다"""
    ys, xs = np.mgrid[0:height, 0:width].astype(np.float32) + 0.5
    qx = np.abs(xs - width / 2) - (rect_w / 2 - corner)
    qy = np.abs(ys - height / 2) - (rect_h / 2 - corner)
    outside = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0))
    inside = np.minimum(np.maximum(qx, qy), 0)
    return np.clip(0.5 - (outside + inside - corner), 0, 1)


def gaussian_blur(image, sigma):
    radius = int(sigma * 3.2)
    taps = np.arange(-radius, radius + 1, dtype=np.float32)
    kernel = np.exp(-0.5 * (taps / sigma) ** 2)
    kernel /= kernel.sum()
    padded = np.pad(image, radius)
    rows = np.apply_along_axis(lambda v: np.convolve(v, kernel, "valid"), 1, padded)
    return np.apply_along_axis(lambda v: np.convolve(v, kernel, "valid"), 0, rows)


def card_spill():
    """들린 가장자리(오른쪽 변 = 카드 높이만큼의 틈)에서 바닥으로 새는 빛. 틈을 따라 점광원을 늘어놓고 더한다.
    틈 가운데가 가장 밝고 위아래 끝으로 갈수록 약해 빛 웅덩이가 네모가 아닌 타원으로 번진다.
    광원마다 가까이 진한 빛 + 멀리 옅은 빛을 섞고, 옆(바깥)으로 더 멀리 뻗게 한다"""
    ys, xs = np.mgrid[0:SPILL_H, 0:SPILL_W].astype(np.float32) + 0.5
    dx = (SPILL_W - xs) / SPILL_W * SPILL_REACH
    dy = (ys - SPILL_H / 2) / (SPILL_H / 2) * SPILL_HALF_HEIGHT
    gap_half = SPILL_HALF_HEIGHT * SPILL_SLIT
    light = np.zeros_like(dx)
    for t in np.linspace(-1, 1, 81):
        weight = (1 - t * t) ** 0.7
        distance = np.maximum(np.hypot(dx, dy - t * gap_half), 1e-4)
        sideways = dx / distance
        light += weight * (0.5 * np.exp(-distance / 0.08) + 0.5 * np.exp(-distance / 0.3)) * (0.3 + 0.7 * sideways)
    light /= light.max()
    fade_far = 1 - np.clip((dx / SPILL_REACH - 0.75) / 0.25, 0, 1) ** 2
    fade_edge = 1 - np.clip((np.abs(dy) / SPILL_HALF_HEIGHT - 0.85) / 0.15, 0, 1) ** 2
    return np.clip(light * fade_far * fade_edge, 0, 1) ** (0.8 * LINEAR_ALPHA_GAMMA)


def card_bloom():
    """카드 실루엣을 좁게 · 넓게 두 번 흐려 섞는다. 가까이는 진하고 멀리까지 옅게 남아 네모 테두리가 생기지 않는다"""
    card_w = BLOOM_CARD_W
    card_h = round(card_w * CH / CW)
    margin = round(card_w * BLOOM_MARGIN)
    width, height = card_w + margin * 2, card_h + margin * 2
    silhouette = rounded_rect_mask(width, height, card_w, card_h, card_w * CARD_CORNER)
    near = gaussian_blur(silhouette, card_w * 0.06)
    far = gaussian_blur(silhouette, card_w * 0.15)
    bloom = 0.6 * near + 0.4 * far
    bloom /= bloom[height // 2, width // 2]
    fade = rounded_rect_mask(width, height, width - 4, height - 4, margin)
    return (np.clip(bloom, 0, 1) * gaussian_blur(fade, 3)) ** LINEAR_ALPHA_GAMMA


def card_mask():
    """카드 모양 흰 판. 모서리는 카드 그림과 같은 반지름"""
    height = round(MASK_W * CH / CW)
    return rounded_rect_mask(MASK_W, height, MASK_W, height, MASK_W * CARD_CORNER)


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
    render(UI / "Gacha" / "swipe_arrow.png", swipe_arrow(), 330, 164, 2)
    render(UI / "Gacha" / "gacha_bg.png", gacha_bg(), BG_W, BG_H)
    save_alpha(UI / "Fx" / "card_spill.png", card_spill())
    save_alpha(UI / "Fx" / "card_bloom.png", card_bloom())
    save_alpha(UI / "Fx" / "card_mask.png", card_mask())
