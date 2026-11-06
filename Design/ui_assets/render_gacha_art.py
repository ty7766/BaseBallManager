"""
뽑기 화면 아트 — 카드 뒷면 · 밀어서 뒤집기 화살표 · 뒤집을 때 새는 빛 · 공개 빛 번짐 · 구장 조명 배경

사용법
    python Design/ui_assets/render_gacha_art.py

출력
    Assets/Resources/UI/Card/card_back.png      카드 뒷면 1398×1957 (어두운 회색 + 은색 테두리 + 내야 다이아몬드 문장)
    Assets/Resources/UI/Gacha/swipe_arrow.png   두꺼운 화살표(몸통 76) + 끊어진 꼬리 막대 셋(왼쪽으로 갈수록 가늘다) 330×164 → 2배
    Assets/Resources/UI/Gacha/gacha_bg.png      구장 조명 배경 1080×2340 (위 양쪽 모서리에서 카드 자리로 엇갈리는 빛줄기)
    Assets/Resources/UI/Fx/card_spill.png       들린 가장자리에서 카드 자리로 새는 빛 띠 256×512. 오른쪽 변 = 들린 가장자리, 카드 높이 전체에 고르게 퍼지고
                                                위아래는 카드 모서리 너머로 살짝 번진 뒤 옅어진다 (높이 = 카드 × SPILL_HEIGHT_RATIO)
    Assets/Resources/UI/Fx/card_sheen.png       카드가 기울 때 표면에 비치는 반사광 280×392. 카드 모양 안에서 왼쪽(들린 쪽)이 밝다
    Assets/Resources/UI/Fx/card_bloom.png       앞면이 드러날 때 카드에서 번지는 빛. 카드 실루엣을 흐린 것, 카드 둘레에 카드 폭 절반씩 여백
    Assets/Resources/UI/Fx/card_mask.png        카드 모양(모서리 둥근 흰 판). 공개 순간 앞면이 번쩍이는 덮개
    Assets/Resources/UI/Fx/card_rimlight.png    카드 밑에서 가장자리로 새는 윤곽 빛. 들린 왼쪽이 밝고 경첩 쪽(오른쪽)은 거의 꺼진다
    Assets/Resources/UI/Fx/light_mote.png       틈에서 떠오르는 빛 알갱이 64
    (빛 그림은 모두 가산 합성(BaseBallManager/UI/AdditiveLight)으로 그린다)
    Assets/Resources/UI/Gacha/tab_track.png     탭 바탕 알약 200×96 → 2배 · 9-슬라이스 96
    Assets/Resources/UI/Gacha/tab_pill.png      고른 탭 흰 알약 200×80 → 2배 · 9-슬라이스 80
    Assets/Resources/UI/Gacha/ticket_once_blue.png · ticket_ten_blue.png    일반 뽑기 1회(어두운 바탕 + 파란 테) · 10회(파란 채움 + 빛 띠) 500×130 → 2배
    Assets/Resources/UI/Gacha/ticket_once_pink.png · ticket_ten_pink.png    시그니쳐 뽑기 — 같은 모양에 분홍
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
SPILL_HEIGHT_RATIO = 1.3 # 새는 빛 그림 높이 ÷ 카드 높이. Unity 의 SpillHeightRatio 와 같다
BLOOM_CARD_W = 180       # 번짐 그림에서 카드 폭(픽셀)
BLOOM_MARGIN = 0.5       # 카드 둘레 여백 (카드 폭 비율). Unity 의 BloomMarginRatio 와 같다
MASK_W = 280
TICKET_W, TICKET_H = 500, 130
TICKET_STUB = 150        # 절취선 왼쪽 그림 칸. Unity 의 TicketStubWidth 와 같다
TICKET_NOTCH = 20
TICKET_COLORS = {        # (테두리 · 채움 위 · 채움 아래) — 일반 = 파랑, 시그니쳐 = 분홍
    "blue": ("#5FA2FF", "#5FA2FF", "#2554C7"),
    "pink": ("#FF8DB8", "#FF8DB8", "#C2386E"),
}
RIM_MARGIN = 0.3         # 윤곽 빛 그림의 카드 둘레 여백 (카드 폭 비율). Unity 의 RimMarginRatio 와 같다
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
    """들린 가장자리(오른쪽 변)에서 카드 자리로 새는 빛 띠. 가장자리 바로 앞이 가장 밝고 멀어질수록 옅어진다.
    세로로는 카드 높이 전체가 고르게 밝고, 카드 위아래 모서리 너머로 조금 번진 뒤 꺼진다 (문틈 아래로 새는 빛처럼)"""
    ys, xs = np.mgrid[0:SPILL_H, 0:SPILL_W].astype(np.float32) + 0.5
    u = (SPILL_W - xs) / SPILL_W
    v = np.abs(ys - SPILL_H / 2) / (SPILL_H / 2)
    card_edge = 1 / SPILL_HEIGHT_RATIO
    along = 0.55 * np.exp(-u / 0.1) + 0.45 * np.exp(-u / 0.42)
    across = 1 - np.clip((v - card_edge * 0.82) / (1 - card_edge * 0.82), 0, 1)
    across = across * across * (3 - 2 * across)
    fade_far = 1 - np.clip((u - 0.8) / 0.2, 0, 1) ** 2
    return np.clip(along * across * fade_far, 0, 1) ** (0.75 * LINEAR_ALPHA_GAMMA)


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


def card_rimlight():
    """카드 밑에서 가장자리로 새는 빛. 카드 둘레에 바짝 붙은 좁은 번짐 + 조금 넓은 번짐을 섞고,
    들린 왼쪽 변에서 가장 밝고 경첩인 오른쪽으로 갈수록 꺼진다 (카드 안쪽은 카드에 가려 보이지 않는다)"""
    card_w = BLOOM_CARD_W
    card_h = round(card_w * CH / CW)
    margin = round(card_w * RIM_MARGIN)
    width, height = card_w + margin * 2, card_h + margin * 2
    silhouette = rounded_rect_mask(width, height, card_w, card_h, card_w * CARD_CORNER)
    glow = 0.65 * gaussian_blur(silhouette, card_w * 0.03) + 0.35 * gaussian_blur(silhouette, card_w * 0.09)
    glow /= glow[height // 2, width // 2]
    across = (np.arange(width, dtype=np.float32) - margin) / card_w
    ramp = 0.08 + 0.92 * (1 - np.clip(across / 0.85, 0, 1)) ** 1.6
    fade = rounded_rect_mask(width, height, width - 4, height - 4, margin)
    return (np.clip(glow, 0, 1) * ramp[None, :] * gaussian_blur(fade, 3)) ** LINEAR_ALPHA_GAMMA


def light_mote():
    """빛 알갱이. 밝은 심 + 부드러운 번짐"""
    size = 64
    ys, xs = np.mgrid[0:size, 0:size].astype(np.float32) + 0.5
    r = np.hypot(xs - size / 2, ys - size / 2) / (size / 2)
    dot = 0.7 * np.exp(-(r / 0.16) ** 2) + 0.3 * np.exp(-(r / 0.45) ** 2)
    dot *= np.clip((1 - r) / 0.12, 0, 1)
    return (dot / dot.max()) ** (LINEAR_ALPHA_GAMMA * 0.6)


def card_sheen():
    """카드가 기울 때 표면에 비치는 반사광. 카드 모양 안에서 들린 쪽(왼쪽)이 밝고 반대쪽으로 옅어진다"""
    height = round(MASK_W * CH / CW)
    shape = rounded_rect_mask(MASK_W, height, MASK_W, height, MASK_W * CARD_CORNER)
    across = np.arange(MASK_W, dtype=np.float32) / MASK_W
    ramp = 0.06 + 0.94 * np.exp(-across / 0.28)
    return shape * ramp[None, :] ** LINEAR_ALPHA_GAMMA


def card_mask():
    """카드 모양 흰 판. 모서리는 카드 그림과 같은 반지름"""
    height = round(MASK_W * CH / CW)
    return rounded_rect_mask(MASK_W, height, MASK_W, height, MASK_W * CARD_CORNER)


def tab_track():
    """탭 두 개를 담는 어두운 알약"""
    return '<rect x="1.5" y="1.5" width="197" height="93" rx="46.5" fill="#191A1C" fill-opacity="0.92" stroke="#3C4044" stroke-width="3"/>'


def tab_pill():
    """고른 탭에 깔리는 흰 알약"""
    return '<rect width="200" height="80" rx="40" fill="#F3F6FA"/>'


def mini_card(x, y, w, stroke, rotate=0):
    h = w * 1.4
    d = w * 0.28
    cx, cy = x + w / 2, y + h / 2
    return (f'<g transform="rotate({rotate} {f(cx)} {f(cy)})"><rect x="{f(x)}" y="{f(y)}" width="{f(w)}" height="{f(h)}" '
            f'rx="{f(w * 0.12)}" fill="#2A2D33" stroke="{stroke}" stroke-width="2.5"/>'
            f'<polygon points="{f(cx)},{f(cy - d)} {f(cx + d)},{f(cy)} {f(cx)},{f(cy + d)} {f(cx - d)},{f(cy)}" '
            f'fill="none" stroke="{stroke}" stroke-width="2"/></g>')


def ticket(color, ten):
    """뽑기권 모양: 양옆 가운데 반원 홈 + 절취선. 왼쪽 칸에 카드 뒷면 아이콘 (1회 1장 · 10회 3장 부채꼴).
    1회는 어두운 바탕 + 색 테, 10회는 색 채움 + 비스듬한 빛 띠"""
    rim, top, bottom = TICKET_COLORS[color]
    w, h, n, r = TICKET_W - 3, TICKET_H - 3, TICKET_NOTCH, 18
    x0, y0 = 1.5, 1.5
    path = (f'M{x0 + r},{y0} H{x0 + w - r} Q{x0 + w},{y0} {x0 + w},{y0 + r} V{y0 + h / 2 - n} '
            f'A{n},{n} 0 0 0 {x0 + w},{y0 + h / 2 + n} V{y0 + h - r} Q{x0 + w},{y0 + h} {x0 + w - r},{y0 + h} '
            f'H{x0 + r} Q{x0},{y0 + h} {x0},{y0 + h - r} V{y0 + h / 2 + n} A{n},{n} 0 0 0 {x0},{y0 + h / 2 - n} '
            f'V{y0 + r} Q{x0},{y0} {x0 + r},{y0} Z')
    defs = (f'<defs><linearGradient id="fill" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="{top}"/>'
            f'<stop offset="1" stop-color="{bottom}"/></linearGradient>'
            '<linearGradient id="shine" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#fff" stop-opacity="0"/>'
            '<stop offset="0.5" stop-color="#fff" stop-opacity="0.32"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></linearGradient>'
            f'<clipPath id="ticket"><path d="{path}"/></clipPath></defs>')
    if ten:
        out = [defs, f'<path d="{path}" fill="url(#fill)" stroke="{rim}" stroke-width="3"/>',
               f'<g clip-path="url(#ticket)"><rect x="300" y="-40" width="80" height="{TICKET_H + 80}" fill="url(#shine)" '
               f'transform="rotate(20 340 {TICKET_H / 2})"/></g>',
               f'<line x1="{TICKET_STUB}" y1="14" x2="{TICKET_STUB}" y2="{TICKET_H - 14}" stroke="#FFFFFF" '
               'stroke-width="3" stroke-dasharray="8 8" opacity="0.55"/>',
               mini_card(34, 30, 44, "#FFFFFF", -12), mini_card(54, 26, 44, "#FFFFFF"), mini_card(74, 30, 44, "#FFFFFF", 12)]
    else:
        out = [defs, f'<path d="{path}" fill="#191A1C" fill-opacity="0.94" stroke="{rim}" stroke-width="4"/>',
               f'<line x1="{TICKET_STUB}" y1="14" x2="{TICKET_STUB}" y2="{TICKET_H - 14}" stroke="{rim}" '
               'stroke-width="3" stroke-dasharray="8 8" opacity="0.6"/>',
               mini_card(50, 30, 50, rim)]
    return "".join(out)


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
    render(UI / "Gacha" / "tab_track.png", tab_track(), 200, 96, 2)
    render(UI / "Gacha" / "tab_pill.png", tab_pill(), 200, 80, 2)
    for color in TICKET_COLORS:
        render(UI / "Gacha" / f"ticket_once_{color}.png", ticket(color, False), TICKET_W, TICKET_H, 2)
        render(UI / "Gacha" / f"ticket_ten_{color}.png", ticket(color, True), TICKET_W, TICKET_H, 2)
    save_alpha(UI / "Fx" / "card_spill.png", card_spill())
    save_alpha(UI / "Fx" / "card_bloom.png", card_bloom())
    save_alpha(UI / "Fx" / "card_mask.png", card_mask())
    save_alpha(UI / "Fx" / "card_sheen.png", card_sheen())
    save_alpha(UI / "Fx" / "card_rimlight.png", card_rimlight())
    save_alpha(UI / "Fx" / "light_mote.png", light_mote())
