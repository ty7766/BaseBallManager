"""
카드 아트 생성 — 노멀 배경(등급별 3종) · 사진 앞 전경 · 골든글러브 장식 · 강화 메달 · 카드 별 (경기_UI_명세.md 6-6)

사용법
    python Design/ui_assets/render_card_art.py            전부 다시 만든다
    python Design/ui_assets/render_card_art.py badge_     이름에 badge_ 가 들어간 것만

동작
    SVG를 만들어 헤드리스 Edge로 PNG를 찍는다. 카드 원본 1398×1957 좌표 1배로 내보낸다.
    붓 터치는 고정 시드라 몇 번을 돌려도 같은 그림이 나온다

출력
    Assets/Resources/UI/Card/
      card_base_normal_{3,4,5}.png      노멀 배경 (불투명). 구도는 같고 색만 등급별
      card_fg_{normal_3..5,signature,goldenglove}.png   사진 앞 하단 물감 띠 (이름 받침)
      card_ov_goldenglove.png           골글 베이스 위 월계수 · 가장자리 그늘 (사진 뒤)
      badge_enhance_{bronze,silver,gold,holo}.png      강화 육각 메달 (숫자는 TMP)
      card_star_{back,fill}.png         카드 별. back = 흰 테두리 + 그림자, fill = 등급색 틴트
"""
import math
import random
import subprocess
import sys
import tempfile
import time
from pathlib import Path

from PIL import Image

W, H = 1398, 1957
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "Resources" / "UI" / "Card"
WORK = Path(tempfile.gettempdir()) / "bbm_card_art"
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

# 팔레트 — deep·mid·hi 바탕, a1·a2 강조 붓, ink 가장 진한 그늘. Unity CardStyle 과 값을 맞춘다
PALETTES = {
    3: dict(deep="#061235", mid="#0F3488", hi="#2A6FE0", a1="#46D9FF", a2="#FF9440", ink="#030921"),
    4: dict(deep="#03241D", mid="#0A5946", hi="#16A07A", a1="#B6EC5C", a2="#2FE3D2", ink="#011410"),
    5: dict(deep="#14083D", mid="#381893", hi="#6A3CE6", a1="#38E8E0", a2="#FF3F9C", ink="#0A0322"),
    "signature": dict(deep="#4A0826", mid="#8E1E4E", hi="#E05A8C", a1="#FFD3E3", a2="#FF5C93", ink="#2E0418"),
    "goldenglove": dict(deep="#3A2606", mid="#7A5414", hi="#C8962E", a1="#FFE9A8", a2="#F2C14E", ink="#221503"),
}


def f(v):
    return f"{v:.1f}"


def brush(rnd, cx, cy, length, width, angle, color, opacity, taper=0.45, bristles=70, streak=None, tail=0.35):
    """마른 붓 한 획. 몸통 + 잔털(끝이 갈라짐) + 결. 로컬 좌표에서 그리고 돌린다"""
    parts = []
    n = 48
    top, bot = [], []
    for i in range(n + 1):
        t = i / n
        x = -length / 2 + t * length
        prof = min(1.0, t / 0.06) * (1 - taper * t ** 1.6)
        top.append((x, -width / 2 * prof + rnd.uniform(-1, 1) * width * 0.025))
        bot.append((x, width / 2 * prof + rnd.uniform(-1, 1) * width * 0.025))
    body_len = length * (1 - tail * 0.6)
    pts = [p for p in top if p[0] < -length / 2 + body_len] + \
          [p for p in reversed(bot) if p[0] < -length / 2 + body_len]
    parts.append(f'<polygon points="{" ".join(f"{f(x)},{f(y)}" for x, y in pts)}" fill="{color}" fill-opacity="{opacity}"/>')
    for _ in range(bristles):
        y = rnd.uniform(-0.55, 0.55) * width
        prof_y = abs(y) / (width / 2)
        x0 = -length / 2 + rnd.uniform(0, 0.08) * length + prof_y * length * 0.03
        x1 = length / 2 - rnd.uniform(0, tail) * length
        sw = rnd.uniform(2, 16)
        dash = ""
        if rnd.random() < 0.6:
            a, b = rnd.uniform(40, 260), rnd.uniform(6, 60)
            dash = f' stroke-dasharray="{f(a)} {f(b)}" stroke-dashoffset="{f(rnd.uniform(0, 200))}"'
        squeeze = 1 - taper * 0.6 * ((x1 + length / 2) / length) ** 1.6
        parts.append(f'<line x1="{f(x0)}" y1="{f(y)}" x2="{f(x1)}" y2="{f(y * squeeze)}" stroke="{color}" '
                     f'stroke-opacity="{opacity * rnd.uniform(0.45, 1):.2f}" stroke-width="{f(sw)}" stroke-linecap="round"{dash}/>')
    if streak:
        for _ in range(int(bristles * 0.5)):
            y = rnd.uniform(-0.45, 0.45) * width
            x0 = -length / 2 + rnd.uniform(0.02, 0.3) * length
            x1 = x0 + rnd.uniform(0.2, 0.6) * length
            parts.append(f'<line x1="{f(x0)}" y1="{f(y)}" x2="{f(x1)}" y2="{f(y)}" stroke="{streak}" '
                         f'stroke-opacity="{rnd.uniform(0.05, 0.16):.2f}" stroke-width="{f(rnd.uniform(1.5, 6))}"/>')
    return f'<g transform="translate({f(cx)} {f(cy)}) rotate({angle})" filter="url(#rough)">{"".join(parts)}</g>'


def splatter(rnd, cx, cy, spread, count, color, opacity, rmax=9):
    """붓 끝에 튄 물감 방울. 가운데서 멀수록 작다"""
    out = []
    for _ in range(count):
        a = rnd.uniform(0, math.tau)
        d = abs(rnd.gauss(0, spread))
        r = rnd.uniform(1.5, rmax) * (1 - min(d / (spread * 3), 0.8))
        out.append(f'<circle cx="{f(cx + math.cos(a) * d)}" cy="{f(cy + math.sin(a) * d)}" r="{f(r)}" '
                   f'fill="{color}" fill-opacity="{opacity * rnd.uniform(0.4, 1):.2f}"/>')
    return "".join(out)


def halftone(ox, oy, radius, step, rmax, color, opacity, angle=45):
    """(ox, oy)에서 멀어질수록 점이 작아지는 망점"""
    out = []
    ca, sa = math.cos(math.radians(angle)), math.sin(math.radians(angle))
    span = int(radius / step) + 2
    for i in range(-span, span + 1):
        for j in range(-span, span + 1):
            x = ox + (i * ca - j * sa) * step
            y = oy + (i * sa + j * ca) * step
            if not (-20 <= x <= W + 20 and -20 <= y <= H + 20):
                continue
            d = math.hypot(x - ox, y - oy) / radius
            if d >= 1:
                continue
            r = rmax * (1 - d) ** 1.3
            if r < 0.8:
                continue
            out.append(f'<circle cx="{f(x)}" cy="{f(y)}" r="{f(r)}"/>')
    return f'<g fill="{color}" fill-opacity="{opacity}">{"".join(out)}</g>'


def stitches(cx, cy, radius, a0, a1, color, opacity, step=46):
    """야구공 실밥 — 호를 따라 V자 바늘땀"""
    out = []
    count = int(math.radians(abs(a1 - a0)) * radius / step)
    for k in range(count + 1):
        a = math.radians(a0 + (a1 - a0) * k / count)
        px, py = cx + math.cos(a) * radius, cy + math.sin(a) * radius
        nx, ny = math.cos(a), math.sin(a)
        tx, ty = -ny, nx
        for side in (1, -1):
            x0, y0 = px + nx * 10 * side, py + ny * 10 * side
            x1, y1 = px + nx * 34 * side + tx * 16, py + ny * 34 * side + ty * 16
            out.append(f'<line x1="{f(x0)}" y1="{f(y0)}" x2="{f(x1)}" y2="{f(y1)}"/>')
    return (f'<g stroke="{color}" stroke-opacity="{opacity}" stroke-width="7" stroke-linecap="round">'
            f'{"".join(out)}</g>')


def defs(p):
    """배경·전경이 같이 쓰는 필터 · 그라데이션 · 무늬 (거친 붓 가장자리, 입자, 다이아몬드 격자, 핀스트라이프)"""
    return f'''<defs>
<filter id="rough" x="-20%" y="-40%" width="140%" height="180%">
  <feTurbulence type="fractalNoise" baseFrequency="0.03 0.07" numOctaves="2" seed="4" result="n"/>
  <feDisplacementMap in="SourceGraphic" in2="n" scale="18" xChannelSelector="R" yChannelSelector="G"/>
</filter>
<filter id="grain" x="0" y="0" width="100%" height="100%">
  <feTurbulence type="fractalNoise" baseFrequency="0.85" numOctaves="2" seed="9"/>
  <feColorMatrix type="matrix" values="0 0 0 0 1  0 0 0 0 1  0 0 0 0 1  0 0 0 0.9 -0.42"/>
</filter>
<linearGradient id="base" x1="1" y1="0" x2="0.15" y2="1">
  <stop offset="0" stop-color="{p['hi']}"/><stop offset="0.45" stop-color="{p['mid']}"/><stop offset="1" stop-color="{p['deep']}"/>
</linearGradient>
<radialGradient id="fadeBR" cx="1" cy="0.78" r="0.62"><stop offset="0" stop-color="#fff" stop-opacity="1"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient>
<mask id="latticeMask"><rect width="{W}" height="{H}" fill="url(#fadeBR)"/></mask>
<pattern id="diamond" width="132" height="132" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">
  <rect width="132" height="132" fill="none" stroke="{p['a1']}" stroke-width="4"/>
</pattern>
<pattern id="pin" width="30" height="30" patternUnits="userSpaceOnUse" patternTransform="rotate(28)">
  <rect width="9" height="30" fill="{p['ink']}"/>
</pattern>
<clipPath id="band"><polygon points="430,-40 1460,-40 1460,430 120,1150"/></clipPath>
<linearGradient id="vig" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0" stop-color="{p['ink']}" stop-opacity="0.55"/><stop offset="0.16" stop-color="{p['ink']}" stop-opacity="0"/>
  <stop offset="0.7" stop-color="{p['ink']}" stop-opacity="0"/><stop offset="1" stop-color="{p['ink']}" stop-opacity="0.5"/>
</linearGradient>
</defs>'''


def background(grade):
    """노멀 배경. 대각선 붓 4획 · 핀스트라이프 띠 · 내야 격자 · 망점 · 실밥 호 · 얇은 테두리"""
    p = PALETTES[grade]
    rnd = random.Random(26)
    g = [defs(p), f'<rect width="{W}" height="{H}" fill="url(#base)"/>']
    g.append(brush(rnd, 760, 1430, 2400, 560, -29, p["hi"], 0.55, bristles=90, streak="#ffffff"))
    g.append(f'<g mask="url(#latticeMask)" opacity="0.38"><rect width="{W}" height="{H}" fill="url(#diamond)"/></g>')
    g.append(f'<g clip-path="url(#band)" opacity="0.42"><rect x="-200" y="-200" width="{W + 400}" height="{H + 400}" fill="url(#pin)"/></g>')
    g.append(brush(rnd, 1000, 360, 1700, 380, -30, p["a1"], 0.95, bristles=110, streak="#ffffff"))
    g.append(brush(rnd, 250, 980, 1350, 300, -33, p["a2"], 0.92, bristles=90, streak=p["deep"]))
    g.append(brush(rnd, 1150, 1180, 1300, 170, -30, p["ink"], 0.55, bristles=60))
    g.append(splatter(rnd, 1180, 120, 120, 70, p["a1"], 0.8))
    g.append(splatter(rnd, -20, 1260, 140, 60, p["a2"], 0.8))
    g.append(halftone(-40, H + 40, 760, 30, 13, p["a1"], 0.55))
    g.append(halftone(W + 30, -30, 520, 26, 9, "#ffffff", 0.22))
    g.append(stitches(1650, 2050, 1480, 196, 252, "#ffffff", 0.22))
    g.append(f'<rect width="{W}" height="{H}" fill="url(#vig)"/>')
    g.append(f'<rect width="{W}" height="{H}" filter="url(#grain)" opacity="0.5"/>')
    g.append(f'<rect x="30" y="30" width="{W - 60}" height="{H - 60}" fill="none" stroke="#ffffff" stroke-opacity="0.32" stroke-width="5"/>')
    return "".join(g)


def foreground(key):
    """사진 앞 하단 물감 띠. 다리 앞을 지나가 허벅지에서 끊긴 사진을 가리고 이름 받침이 된다"""
    p = PALETTES[key]
    rnd = random.Random(73)
    g = [defs(p)]
    g.append(f'<linearGradient id="bot" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{p["ink"]}" stop-opacity="0"/>'
             f'<stop offset="1" stop-color="{p["ink"]}" stop-opacity="0.78"/></linearGradient>')
    g.append(f'<rect y="1380" width="{W}" height="{H - 1380}" fill="url(#bot)"/>')
    g.append(brush(rnd, 640, 1890, 2300, 470, -8, p["ink"], 0.93, taper=0.3, bristles=120, streak=p["mid"]))
    g.append(brush(rnd, 820, 1645, 1900, 40, -8, p["a1"], 0.95, taper=0.5, bristles=26))
    g.append(brush(rnd, 1100, 1690, 900, 22, -8, p["a2"], 0.9, taper=0.6, bristles=14))
    g.append(halftone(W + 40, H - 300, 420, 26, 10, p["a2"], 0.7))
    return "".join(g)


def bezier(p, t):
    """3차 베지어 위의 점과 접선"""
    (x0, y0), (x1, y1), (x2, y2), (x3, y3) = p
    u = 1 - t
    x = u ** 3 * x0 + 3 * u * u * t * x1 + 3 * u * t * t * x2 + t ** 3 * x3
    y = u ** 3 * y0 + 3 * u * u * t * y1 + 3 * u * t * t * y2 + t ** 3 * y3
    dx = 3 * u * u * (x1 - x0) + 6 * u * t * (x2 - x1) + 3 * t * t * (x3 - x2)
    dy = 3 * u * u * (y1 - y0) + 6 * u * t * (y2 - y1) + 3 * t * t * (y3 - y2)
    n = math.hypot(dx, dy)
    return x, y, dx / n, dy / n


def laurel_branch(ctrl, side):
    """월계수 가지 하나. side 는 바깥쪽 법선 방향 (-1 왼쪽 가지, +1 오른쪽 가지)"""
    stem = " ".join(f"{f(x)},{f(y)}" for x, y, _, _ in (bezier(ctrl, i / 60) for i in range(61)))
    out = [f'<polyline points="{stem}" fill="none" stroke-width="16" stroke-linecap="round"/>']
    count = 15
    for i in range(count):
        t = 0.06 + 0.9 * i / (count - 1)
        x, y, tx, ty = bezier(ctrl, t)
        length = 190 * (1 - 0.45 * t)
        for k, flip in enumerate((1, -1)):
            nx, ny = -ty * side * flip, tx * side * flip
            ang = math.radians(32)
            dx = tx * math.cos(ang) + nx * math.sin(ang)
            dy = ty * math.cos(ang) + ny * math.sin(ang)
            sx, sy = x + tx * k * length * 0.28, y + ty * k * length * 0.28
            ex, ey = sx + dx * length, sy + dy * length
            px, py = -dy * length * 0.24, dx * length * 0.24
            mx, my = (sx + ex) / 2, (sy + ey) / 2
            out.append(f'<path d="M{f(sx)},{f(sy)} Q{f(mx + px)},{f(my + py)} {f(ex)},{f(ey)} '
                       f'Q{f(mx - px)},{f(my - py)} {f(sx)},{f(sy)}Z"/>')
    tip_x, tip_y, tx, ty = bezier(ctrl, 1.0)
    out.append(f'<ellipse cx="{f(tip_x + tx * 60)}" cy="{f(tip_y + ty * 60)}" rx="34" ry="70" '
               f'transform="rotate({math.degrees(math.atan2(ty, tx)) - 90:.1f} {f(tip_x + tx * 60)} {f(tip_y + ty * 60)})"/>')
    return "".join(out)


def gg_overlay():
    """골든글러브 베이스 위에 얹는 장식. 선수가 커지며 가운데 글러브 문양이 가려져 양옆이 비는 것을 채운다"""
    left = ((600, 1840), (20, 1560), (-10, 840), (360, 440))
    right = tuple((W - x, y) for x, y in left)
    wreath = laurel_branch(left, -1) + laurel_branch(right, 1)
    return f'''<defs>
<linearGradient id="leaf" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFE7A0"/><stop offset="0.5" stop-color="#D9A53A"/><stop offset="1" stop-color="#8E5E14"/></linearGradient>
<filter id="soft"><feGaussianBlur stdDeviation="3"/></filter>
<linearGradient id="burn" x1="0" y1="0" x2="1" y2="0">
  <stop offset="0" stop-color="#2A1803" stop-opacity="0.62"/><stop offset="0.22" stop-color="#2A1803" stop-opacity="0"/>
  <stop offset="0.78" stop-color="#2A1803" stop-opacity="0"/><stop offset="1" stop-color="#2A1803" stop-opacity="0.62"/>
</linearGradient>
<linearGradient id="spec" x1="0" y1="0" x2="1" y2="1">
  <stop offset="0.18" stop-color="#FFF8DC" stop-opacity="0"/><stop offset="0.26" stop-color="#FFF8DC" stop-opacity="0.32"/>
  <stop offset="0.30" stop-color="#FFF8DC" stop-opacity="0"/><stop offset="0.62" stop-color="#FFF8DC" stop-opacity="0"/>
  <stop offset="0.68" stop-color="#FFF8DC" stop-opacity="0.22"/><stop offset="0.71" stop-color="#FFF8DC" stop-opacity="0"/>
</linearGradient>
</defs>
<rect width="{W}" height="{H}" fill="url(#burn)"/>
<g fill="#3A2404" stroke="#3A2404" opacity="0.6" transform="translate(7 9)" filter="url(#soft)">{wreath}</g>
<g fill="#FFF4C8" stroke="#FFF4C8" opacity="0.6" transform="translate(-3 -4)">{wreath}</g>
<g fill="url(#leaf)" fill-opacity="0.62" stroke="#B8862A" stroke-opacity="0.8">{wreath}</g>
<rect width="{W}" height="{H}" fill="url(#spec)"/>'''


STAR = "M12 2.5l2.9 6.1 6.6.8-4.9 4.6 1.3 6.6L12 17.3l-5.9 3.3 1.3-6.6-4.9-4.6 6.6-.8z"
STAR_SIZE = 132      # 별 한 칸. 별은 가운데 106, 남는 자리는 그림자
BADGE_W, BADGE_H = 240, 208

# 강화 메달 금속 — 위 밝음 → 가운데 어두운 수평선 → 아래 반사. 단계 1~3 동 · 4~6 은 · 7~9 금 · 10 홀로
METALS = {
    "bronze": (("0", "#FFE0BC"), ("0.42", "#D9925A"), ("0.5", "#86441C"), ("0.8", "#C27A44"), ("1", "#F3BC88")),
    "silver": (("0", "#FFFFFF"), ("0.42", "#DCE2EA"), ("0.5", "#7C8796"), ("0.8", "#B9C3CF"), ("1", "#F1F4F8")),
    "gold": (("0", "#FFF7D2"), ("0.42", "#F4C552"), ("0.5", "#946010"), ("0.8", "#D9A53A"), ("1", "#FFE79C")),
}
HOLO = (("0", "#8CF6FF"), ("0.3", "#C3A0FF"), ("0.55", "#FF9BDD"), ("0.8", "#FFE08C"), ("1", "#8CF6FF"))


def hexagon(scale, top_half=False):
    """가로로 누운 정육각형 (240×208). 가운데를 축으로 줄이면 사방 같은 폭으로 안쪽에 들어간다"""
    pts = [(60, 0), (180, 0), (240, 104), (180, 208), (60, 208), (0, 104)]
    if top_half:
        pts = [(0, 104), (60, 0), (180, 0), (240, 104)]
    return " ".join(f"{120 + (x - 120) * scale:.1f},{104 + (y - 104) * scale:.1f}" for x, y in pts)


def enhance_badge(name):
    """진한 테두리 · 금속면 · 위쪽 광택 · 안쪽 선. 숫자는 Unity에서 각인색으로 얹는다"""
    stops = HOLO if name == "holo" else METALS[name]
    direction = 'x2="1" y2="1"' if name == "holo" else 'x2="0" y2="1"'
    grad = "".join(f'<stop offset="{o}" stop-color="{c}"/>' for o, c in stops)
    return (f'<defs><linearGradient id="m" x1="0" y1="0" {direction}>{grad}</linearGradient></defs>'
            f'<polygon points="{hexagon(1.0)}" fill="#07060B" fill-opacity="0.9"/>'
            f'<polygon points="{hexagon(0.915)}" fill="url(#m)"/>'
            f'<polygon points="{hexagon(0.915, True)}" fill="#fff" fill-opacity="0.16"/>'
            f'<polygon points="{hexagon(0.80)}" fill="none" stroke="#000" stroke-opacity="0.18" stroke-width="3" transform="translate(0 3)"/>'
            f'<polygon points="{hexagon(0.80)}" fill="none" stroke="#fff" stroke-opacity="0.55" stroke-width="3"/>')


def star(back):
    """back = 흰 별을 조금 크게 + 아래로 번진 그림자, fill = 흰 별 (등급색으로 틴트)"""
    k = 106 / 24
    place = f"translate({(STAR_SIZE - 106) / 2:.1f} {(STAR_SIZE - 106) / 2 - 6:.1f}) scale({k:.4f})"
    if not back:
        return f'<path d="{STAR}" fill="#fff" transform="{place}"/>'
    return (f'<defs><filter id="s" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="5"/></filter></defs>'
            f'<path d="{STAR}" fill="#000" fill-opacity="0.55" filter="url(#s)" transform="translate(0 6) {place}"/>'
            f'<path d="{STAR}" fill="#fff" stroke="#fff" stroke-width="1.6" stroke-linejoin="round" transform="{place}"/>')


def render(name, body, size=(W, H), opaque=False):
    """SVG 한 장을 PNG로. 창은 넉넉히 열고 왼쪽 위만 잘라 쓴다"""
    width, height = size
    WORK.mkdir(parents=True, exist_ok=True)
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">{body}</svg>'
    html = WORK / f"{name}.html"
    shot = WORK / f"{name}_shot.png"
    html.write_text(f'<html><body style="margin:0;background:transparent">{svg}</body></html>', encoding="utf-8")
    if shot.exists():
        shot.unlink()
    subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run",
                    f"--user-data-dir={WORK / ('profile_' + name)}", "--default-background-color=00000000",
                    "--force-device-scale-factor=1", f"--window-size={max(width, 200)},{max(height, 200)}",
                    f"--screenshot={shot}", html.as_uri()],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=120)
    for _ in range(100):
        if shot.exists() and shot.stat().st_size > 0:
            break
        time.sleep(0.1)
    with Image.open(shot) as im:
        png = im.convert("RGBA").crop((0, 0, width, height))
    if opaque:
        png = png.convert("RGB")
    png.save(OUT / f"{name}.png", optimize=True)
    print(f"  {name:28s} {png.size[0]}×{png.size[1]}")


def jobs():
    """(파일명, SVG 본문, 크기, 불투명) 목록"""
    out = []
    for grade in (3, 4, 5):
        out.append((f"card_base_normal_{grade}", background(grade), (W, H), True))
        out.append((f"card_fg_normal_{grade}", foreground(grade), (W, H), False))
    for kind in ("signature", "goldenglove"):
        out.append((f"card_fg_{kind}", foreground(kind), (W, H), False))
    out.append(("card_ov_goldenglove", gg_overlay(), (W, H), False))
    for metal in ("bronze", "silver", "gold", "holo"):
        out.append((f"badge_enhance_{metal}", enhance_badge(metal), (BADGE_W, BADGE_H), False))
    out.append(("card_star_back", star(True), (STAR_SIZE, STAR_SIZE), False))
    out.append(("card_star_fill", star(False), (STAR_SIZE, STAR_SIZE), False))
    return out


def main():
    pattern = sys.argv[1] if len(sys.argv) > 1 else ""
    todo = [job for job in jobs() if pattern in job[0]]
    print(f"카드 아트 {len(todo)}장 → {OUT}")
    for name, body, size, opaque in todo:
        render(name, body, size, opaque)


if __name__ == "__main__":
    main()
