"""
경기 UI PNG 애셋 생성 — 디자인 확정 v1 (경기_UI_명세.md 6장 · 3-1)

사용법
    python Design/ui_assets/render_assets.py            전부 다시 만든다
    python Design/ui_assets/render_assets.py panel_     이름에 panel_ 이 들어간 것만

동작
    애셋마다 SVG를 만들어 헤드리스 Edge로 투명 배경 PNG를 찍는다.
    UI 애셋은 1080×1920 기준 1배로 그리고 2배로 내보낸다 (Unity PPU 200).
    카드 애셋은 카드 원본 1398×1957 좌표 그대로 1배로 내보낸다 (Unity PPU 100).

출력
    Assets/UI Assets/{Card,Match,Currency,Common}/*.png
    Design/ui_assets/manifest.json      크기 · 9-슬라이스 테두리 · 틴트 · 용도
    Design/ui_assets/contact_sheet.png  한 장으로 훑어보는 검수용
"""

import concurrent.futures
import json
import math
import os
import subprocess
import sys
import tempfile
import time
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT_ROOT = ROOT / "Assets" / "UI Assets"
HERE = Path(__file__).resolve().parent
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
WORK = Path(tempfile.gettempdir()) / "bbm_assets"

WHITE = "#FFFFFF"
ASSETS = []


def asset(folder, name, w, h, body, scale=2, border=None, tint=None, mode="Simple", usage="", vb=None):
    """애셋 1개 등록. w·h는 1배 크기(카드는 카드 좌표), border는 내보낸 PNG 픽셀 기준 (좌, 하, 우, 상)"""
    ASSETS.append({
        "folder": folder, "name": name, "w": w, "h": h, "scale": scale,
        "svg": f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="{vb or f"0 0 {w} {h}"}">{body}</svg>',
        "border": border or [0, 0, 0, 0], "tint": tint, "mode": mode, "usage": usage,
    })


def box(w, h, fill=None, ring=0, ring_color=None, radius=0):
    """꽉 찬 사각형 + 안쪽 테두리. 9-슬라이스 패널·버튼·행의 공통 모양"""
    parts = []
    if fill:
        parts.append(f'<rect width="{w}" height="{h}" rx="{radius}" fill="{fill}"/>')
    if ring:
        half = ring / 2
        parts.append(f'<rect x="{half}" y="{half}" width="{w - ring}" height="{h - ring}" '
                     f'rx="{max(0, radius - half)}" fill="none" stroke="{ring_color}" stroke-width="{ring}"/>')
    return "".join(parts)


STAR = "M12 2.5l2.9 6.1 6.6.8-4.9 4.6 1.3 6.6L12 17.3l-5.9 3.3 1.3-6.6-4.9-4.6 6.6-.8z"
BURST = "M12 1l2.6 6.3L21 5l-2.3 6.4L24 14l-6.4 1.3L19 22l-7-3.6L5 22l1.4-6.7L0 14l5.3-2.6L3 5l6.4 2.3z"


# ---------------------------------------------------------------- A. 카드 (카드 좌표 1배)

def card_assets():
    """카드 아이콘·빈 슬롯·테두리 광. 카드 배경·전경·강화 메달·카드 별은 render_card_art.py 가 만든다"""
    asset("Card", "icon_star", 176, 176,
          f'<path d="{STAR}" fill="{WHITE}" stroke="rgba(8,12,20,0.9)" stroke-width="1.1" stroke-linejoin="round"/>',
          scale=1, vb="0 0 24 24", tint="카드 종류색 (#93C1F5 · #FF6FA0 · #F2C14E)",
          usage="등급 수만큼 배치. 카드 88px · 목록 행 23px")

    cx, cy = 349.5, 489.5
    asset("Card", "card_slot_empty", 699, 979,
          '<rect width="699" height="979" fill="rgba(255,255,255,0.04)"/>'
          '<rect x="3" y="3" width="693" height="973" fill="none" stroke="rgba(255,255,255,0.35)" stroke-width="6" stroke-dasharray="28 18"/>'
          f'<path d="M{cx} {cy - 60}V{cy + 60}M{cx - 60} {cy}H{cx + 60}" stroke="rgba(255,255,255,0.35)" stroke-width="12"/>',
          scale=1, usage="빈 카드 슬롯 (라인업 · 조합 재료 칸). 카드 절반 크기")

    # card_fx_glow_{signature,goldenglove} 는 카드 모양(사선 모서리)을 따라야 해서 card_shape.py 가 만든다

    asset("Card", "icon_breakthrough", 128, 128, f'<path d="{BURST}" fill="{WHITE}"/>',
          scale=1, vb="0 0 24 24", tint="#7EE3D8", usage="카드 뒷면 '돌파 완료'")
    asset("Card", "icon_lock", 128, 128,
          '<path d="M8 11V8a4 4 0 0 1 8 0v3" fill="none" stroke="#fff" stroke-width="2.4"/>'
          '<path fill-rule="evenodd" fill="#fff" d="M5 11h14v10H5z M12 14.2a1.9 1.9 0 1 0 0.001 0z"/>',
          scale=1, vb="0 0 24 24", tint="흰색", usage="인벤토리 목록에서 잠금 카드 위 오버레이")


# ---------------------------------------------------------------- B. 경기 UI (1080 기준 1배 → 2배)

def match_assets():
    asset("Match", "out_dot_filled", 24, 24, '<circle cx="12" cy="12" r="12" fill="#FF4D5A"/>', usage="아웃 점")
    asset("Match", "out_dot_empty", 24, 24, '<circle cx="12" cy="12" r="10.5" fill="none" stroke="#3A4B66" stroke-width="3"/>', usage="아웃 점 (빈)")
    asset("Match", "base_diamond_occupied", 40, 40, '<polygon points="20,0.5 39.5,20 20,39.5 0.5,20" fill="#F3F6FA"/>', usage="주자 있는 베이스")
    asset("Match", "base_diamond_empty", 40, 40, '<polygon points="20,2 38,20 20,38 2,20" fill="#0E1522" stroke="#3A4B66" stroke-width="2.6"/>', usage="빈 베이스")
    asset("Match", "base_home", 20, 20, '<path d="M0 0H20V10L10 20L0 10Z" fill="#1C2739"/>', usage="홈 (항상 비활성)")
    asset("Match", "icon_inning", 36, 30, f'<path d="M18 0L36 30H0Z" fill="{WHITE}"/>', tint="흰색 · 구분선 #C9D2DF",
          usage="▲ 초. ▼ 말은 Z축 180° 회전")

    asset("Match", "stamina_bar_frame", 24, 16, box(24, 16, "#0E1522", 2, "#1C2739"), border=[6, 6, 6, 6], mode="Sliced", usage="투수 체력 바 틀")
    asset("Match", "stamina_bar_fill", 16, 16, f'<rect width="16" height="16" fill="{WHITE}"/>', tint="#4FD1A5 >50% · #FFB23F ≤50% · #FF4D5A ≤20%",
          mode="Filled", usage="Image Type Filled · Horizontal")
    asset("Match", "stamina_bar_tick", 3, 16, '<rect width="3" height="16" fill="rgba(243,246,250,0.75)"/>', usage="50% 눈금")

    asset("Match", "log_row_offense", 32, 32, box(32, 32, "rgba(255,162,76,0.07)", radius=4),
          border=[16, 16, 16, 16], mode="Sliced", usage="중계 행 · 공격 (평범·수비는 배경 없음)")
    asset("Match", "log_row_score", 32, 32, box(32, 32, "rgba(242,193,78,0.09)", 2, "rgba(242,193,78,0.35)", radius=4),
          border=[16, 16, 16, 16], mode="Sliced", usage="중계 행 · 득점 동반")
    asset("Match", "log_row_homerun", 128, 40,
          '<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="rgba(242,193,78,0.28)"/>'
          '<stop offset="0.75" stop-color="rgba(242,193,78,0.04)"/><stop offset="1" stop-color="rgba(242,193,78,0.04)"/></linearGradient></defs>'
          + box(128, 40, "url(#g)", 2, "rgba(242,193,78,0.8)", radius=4),
          border=[16, 16, 16, 16], mode="Sliced", usage="중계 행 · 홈런")
    asset("Match", "log_inning_chip", 32, 32, box(32, 32, "rgba(255,255,255,0.2)", 2, "rgba(255,255,255,0.6)", radius=2),
          border=[8, 8, 8, 8], mode="Sliced", tint="공격 팀 강조색", usage="중계 행 앞 이닝 칩")
    asset("Match", "log_inning_header", 16, 2, '<rect width="16" height="2" fill="#1C2739"/>', usage="이닝 구분선 (가로로 늘림)")
    asset("Match", "badge_score", 32, 32, box(32, 32, "#F2C14E", radius=2), border=[6, 6, 6, 6], mode="Sliced", usage="+N점 배지")

    asset("Match", "panel_large", 32, 32, box(32, 32, "#0E1522", 2, "#1C2739"), border=[8, 8, 8, 8], mode="Sliced", usage="화면 단위 패널 · 스코어보드")
    asset("Match", "panel_small", 32, 32, box(32, 32, "#121B2B", 2, "#1C2739"), border=[8, 8, 8, 8], mode="Sliced", usage="칩 · 목록 섹션 · 모달 요약")
    asset("Match", "button_primary", 32, 32, box(32, 32, "#F3F6FA"), border=[4, 4, 4, 4], mode="Sliced", usage="주 버튼 (글자 #0A111D)")
    asset("Match", "button_normal", 32, 32, box(32, 32, None, 3, WHITE), border=[10, 10, 10, 10], mode="Sliced",
          tint="보조 #2A3953 · 일시정지 #F3F6FA · 예약됨 #8EC5FF (+바탕 하늘 10%)", usage="테두리 버튼")
    asset("Match", "button_disabled", 32, 32, box(32, 32, "#121B2B", 2, "#1C2739"), border=[8, 8, 8, 8], mode="Sliced", usage="비활성 버튼 (글자 #465267)")
    asset("Match", "tab_on", 32, 32, box(32, 32, "#18233A") + '<rect y="27" width="32" height="5" fill="#F3F6FA"/>',
          border=[4, 12, 4, 4], mode="Sliced", usage="켜진 탭 (꺼진 탭은 이미지 없음)")

    asset("Match", "row_normal", 32, 32, box(32, 32, "#121B2B", 2, "#1C2739"), border=[8, 8, 8, 8], mode="Sliced", usage="목록 행")
    asset("Match", "row_selected", 32, 32, box(32, 32, "#18233A", 3, "#F3F6FA"), border=[10, 10, 10, 10], mode="Sliced", usage="선택된 행")
    asset("Match", "row_used", 32, 32, box(32, 32, "#0B111C", 2, "#161F2F"), border=[8, 8, 8, 8], mode="Sliced", usage="사용됨·강판 (CanvasGroup 알파 0.5)")
    asset("Match", "row_pitching", 32, 32, box(32, 32, "#0E1E2A", 2, "rgba(79,209,197,0.6)"), border=[8, 8, 8, 8], mode="Sliced", usage="등판 중")
    asset("Match", "row_empty", 56, 56, dashed_border(56, 10, 12, 6, 2, "#2A3953"),
          border=[20, 20, 20, 20], mode="Tiled", usage="빈 슬롯 점선 (Image Type Tiled)")
    asset("Match", "tag_outline", 16, 16, box(16, 16, None, 2, WHITE), border=[6, 6, 6, 6], mode="Sliced",
          tint="AWAY·HOME·포지션 #2A3953 · MY TEAM #F2C14E · 공격 #FFA24C", usage="테두리 태그")
    asset("Match", "scrollbar_track", 16, 6, '<rect width="16" height="6" fill="#1C2739"/>', usage="가로 스크롤바 트랙")
    asset("Match", "scrollbar_handle", 16, 6, '<rect width="16" height="6" fill="#6E7C92"/>', usage="가로 스크롤바 손잡이")

    icons = {
        "icon_pause": f'<rect x="6" y="4.5" width="4" height="15" fill="{WHITE}"/><rect x="14" y="4.5" width="4" height="15" fill="{WHITE}"/>',
        "icon_resume": f'<path d="M7 4.5v15l12.5-7.5z" fill="{WHITE}"/>',
        "icon_back": f'<path d="M15 5l-7 7 7 7" fill="none" stroke="{WHITE}" stroke-width="2.4" stroke-linecap="square"/>',
        "icon_swap": f'<path d="M4 8h14l-3.5-3.5M20 16H6l3.5 3.5" fill="none" stroke="{WHITE}" stroke-width="2.2" stroke-linecap="square"/>',
        "icon_steal": f'<path d="M4 12h14M13 6l6 6-6 6" fill="none" stroke="{WHITE}" stroke-width="2.4" stroke-linecap="square"/>',
        "icon_ball": f'<circle cx="12" cy="12" r="9" fill="none" stroke="{WHITE}" stroke-width="1.8"/>'
                     f'<path d="M7.5 5.2c1.6 1.9 1.6 11.7 0 13.6M16.5 5.2c-1.6 1.9-1.6 11.7 0 13.6" fill="none" stroke="{WHITE}" stroke-width="1.8"/>',
    }
    for name, body in icons.items():
        asset("Match", name, 48, 48, body, vb="0 0 24 24", tint="흰색 (상황별 색)", usage="아이콘")


def dashed_border(size, border, period, dash, stroke, color):
    """Tiled 모드용 점선 테두리. 가운데 구간이 period의 정수배라 반복해도 이음매가 맞는다"""
    s, half, parts = size, stroke / 2, []
    arm = dash
    corners = [(0, 0, 1, 1), (s, 0, -1, 1), (0, s, 1, -1), (s, s, -1, -1)]
    for x, y, dx, dy in corners:
        ex, ey = x + dx * half, y + dy * half
        parts.append(f'<path d="M{ex} {ey + dy * arm}V{ey}H{ex + dx * arm}" fill="none" stroke="{color}" stroke-width="{stroke}"/>')
    start = border
    while start + dash <= s - border:
        for line in (f"M{start} {half}H{start + dash}", f"M{start} {s - half}H{start + dash}",
                     f"M{half} {start}V{start + dash}", f"M{s - half} {start}V{start + dash}"):
            parts.append(f'<path d="{line}" fill="none" stroke="{color}" stroke-width="{stroke}"/>')
        start += period
    return "".join(parts)


# ---------------------------------------------------------------- C. 재화 아이콘 (128 → 256)

def card_icon(top, bottom, edge, symbol, stars=0, star_color="#FFFFFF", gid="c"):
    """카드 모양 아이템 아이콘 공통 틀"""
    body = (f'<defs><linearGradient id="{gid}" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{top}"/>'
            f'<stop offset="1" stop-color="{bottom}"/></linearGradient></defs>'
            f'<rect x="31" y="12" width="70" height="104" fill="rgba(0,0,0,0.35)" transform="translate(4 4)"/>'
            f'<rect x="31" y="12" width="70" height="104" fill="url(#{gid})"/>'
            f'<rect x="36" y="17" width="60" height="94" fill="none" stroke="{edge}" stroke-width="2.5"/>'
            + symbol)
    if stars:
        size, gap = 11, 2
        total = stars * size + (stars - 1) * gap
        x0 = 66 - total / 2
        for i in range(stars):
            x = x0 + i * (size + gap)
            body += f'<path d="{STAR}" fill="{star_color}" transform="translate({x} 92) scale({size / 24})"/>'
    return body


HEX_SMALL = "56,40 76,40 86,57 76,74 56,74 46,57"


def enhance_symbol(fill, stroke):
    return (f'<polygon points="{HEX_SMALL}" fill="{fill}" stroke="{stroke}" stroke-width="3"/>'
            f'<path d="M66 49V65M58 57H74" stroke="{stroke}" stroke-width="4"/>')


def ticket(body_top, body_bottom, stub, accent, gid):
    notch = 9
    path = (f"M14 34H114V{64 - notch}A{notch} {notch} 0 0 0 114 {64 + notch}V94H14V{64 + notch}"
            f"A{notch} {notch} 0 0 0 14 {64 - notch}Z")
    return (f'<defs><linearGradient id="{gid}" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{body_top}"/>'
            f'<stop offset="1" stop-color="{body_bottom}"/></linearGradient>'
            f'<clipPath id="{gid}c"><path d="{path}"/></clipPath></defs>'
            f'<g transform="rotate(-12 64 64)">'
            f'<path d="{path}" fill="rgba(0,0,0,0.35)" transform="translate(4 4)"/>'
            f'<path d="{path}" fill="url(#{gid})"/>'
            f'<rect x="14" y="34" width="30" height="60" fill="{stub}" clip-path="url(#{gid}c)"/>'
            f'<path d="M44 38V90" stroke="rgba(255,255,255,0.55)" stroke-width="2.5" stroke-dasharray="5 5"/>'
            f'<path d="{STAR}" fill="{accent}" transform="translate(60 44) scale(1.75)"/>'
            f'</g>')


def currency_assets():
    gold = ('<defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFE39A"/>'
            '<stop offset="0.5" stop-color="#F2C14E"/><stop offset="1" stop-color="#C9922A"/></linearGradient></defs>'
            '<circle cx="64" cy="68" r="52" fill="#9A6A14"/>'
            '<circle cx="64" cy="62" r="52" fill="url(#g)"/>'
            '<circle cx="64" cy="62" r="40" fill="none" stroke="#FFF0C2" stroke-opacity="0.75" stroke-width="4"/>'
            '<path d="M47 37C57 49 57 75 47 87M81 37C71 49 71 75 81 87" fill="none" stroke="#B07A1C" stroke-width="5" stroke-linecap="round"/>'
            '<path d="M24 50A42 42 0 0 1 50 22" fill="none" stroke="#FFFFFF" stroke-opacity="0.55" stroke-width="5" stroke-linecap="round"/>')
    point = ('<polygon points="64,14 102,38 64,54 26,38" fill="#C9DEFF"/>'
             '<polygon points="26,38 64,54 64,116 32,82" fill="#6FA2F5"/>'
             '<polygon points="102,38 96,82 64,116 64,54" fill="#3E6FC4"/>'
             '<polygon points="64,14 102,38 96,82 64,116 32,82 26,38" fill="none" stroke="#1F3F7A" stroke-opacity="0.35" stroke-width="2"/>'
             '<path d="M98 10L101 17L108 20L101 23L98 30L95 23L88 20L95 17Z" fill="#FFFFFF"/>')
    glove = ('<defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFE39A"/>'
             '<stop offset="0.55" stop-color="#E8B84A"/><stop offset="1" stop-color="#A8741A"/></linearGradient></defs>'
             '<path d="M44 116V100C30 94 18 80 20 64C21 55 30 51 37 55L44 62V34C44 20 57 20 57 34C57 14 70 14 70 30'
             'C70 16 83 16 83 32C83 24 94 24 94 38V76C94 94 84 104 76 108V116Z" fill="url(#g)" stroke="#7A5210" stroke-width="3" stroke-linejoin="round"/>'
             '<path d="M57 34V58M70 30V58M83 32V60" stroke="#7A5210" stroke-width="3" stroke-linecap="round"/>'
             '<path d="M44 62C48 70 54 74 62 74" fill="none" stroke="#7A5210" stroke-width="3" stroke-linecap="round"/>'
             '<rect x="44" y="104" width="32" height="12" fill="#7A5210"/>'
             '<circle cx="70" cy="80" r="15" fill="#F3F6FA" stroke="#7A5210" stroke-width="2.5"/>'
             '<path d="M61 69C66 75 66 85 61 91M79 69C74 75 74 85 79 91" fill="none" stroke="#D9383A" stroke-width="2.5" stroke-linecap="round"/>')
    asset("Currency", "currency_gold", 128, 128, gold, usage="골드")
    asset("Currency", "currency_point", 128, 128, point, usage="포인트")
    asset("Currency", "currency_goldenglove_point", 128, 128, glove, usage="골든글러브 포인트")
    asset("Currency", "currency_normal_ticket", 128, 128, ticket("#3D6FB0", "#2A4F86", "#5B8BD0", "#CFE3FF", "t1"), usage="일반 뽑기권")
    asset("Currency", "currency_signature_ticket", 128, 128, ticket("#C8386C", "#8E1C48", "#E06A94", "#FFE0EB", "t2"), usage="시그니쳐 뽑기권")

    up = '<path d="M50 70L66 54L82 70M50 86L66 70L82 86" fill="none" stroke="#FFFFFF" stroke-width="6" stroke-linecap="square"/>'
    burst = f'<path d="{BURST}" fill="#FFFFFF" transform="translate(42 38) scale(2)"/>'
    asset("Currency", "currency_train_card", 128, 128, card_icon("#34B27A", "#1E7A52", "rgba(255,255,255,0.45)", up, gid="a"), usage="훈련 카드")
    asset("Currency", "currency_breakthrough_card", 128, 128, card_icon("#35B7D6", "#1E7FA3", "rgba(255,255,255,0.45)", burst, gid="b"), usage="훈련돌파 카드")
    for stars in (3, 4, 5):
        asset("Currency", f"currency_enhance_card_star{stars}", 128, 128,
              card_icon("#2A5590", "#16325C", "#93C1F5", enhance_symbol("#1C3A66", "#93C1F5"), stars, "#93C1F5", gid=f"n{stars}"),
              usage=f"강화 전용 카드 ({stars}성 노말)")
    asset("Currency", "currency_enhance_card_signature", 128, 128,
          card_icon("#C8386C", "#7A1240", "#FF8DB5", enhance_symbol("#7A1240", "#FFD0E1"), 5, "#FFD0E1", gid="s"),
          usage="강화 전용 카드 (시그니쳐)")
    asset("Currency", "currency_enhance_card_goldenglove", 128, 128,
          card_icon("#E8B84A", "#9C6A12", "#FFF0C2", enhance_symbol("#6B4608", "#FFF4DA"), 5, "#FFF4DA", gid="g"),
          usage="강화 전용 카드 (골든글러브)")


# ---------------------------------------------------------------- D. 공통

def common_assets():
    alphas = [1.0, 0.82, 0.66, 0.52, 0.4, 0.3, 0.2, 0.12]
    for frame in range(8):
        bars = ""
        for i in range(8):
            a = alphas[(i - frame) % 8]
            bars += f'<rect x="45" y="8" width="6" height="22" fill="#FFFFFF" fill-opacity="{a}" transform="rotate({i * 45} 48 48)"/>'
        asset("Common", f"spinner_{frame:02d}", 96, 96, bars, tint="흰색", usage="로딩 스피너 8프레임 (0.08초 간격)")


# ---------------------------------------------------------------- 렌더링

HTML = ('<!doctype html><html><head><meta charset="utf-8"><style>html,body{{margin:0;padding:0;background:transparent;'
        'overflow:hidden}}svg{{display:block}}</style></head><body>{svg}</body></html>')


def render(item, worker):
    work = WORK / f"w{worker}"
    work.mkdir(parents=True, exist_ok=True)
    html, shot = work / f"{item['name']}.html", work / f"{item['name']}.png"
    html.write_text(HTML.format(svg=item["svg"]), encoding="utf-8")
    if shot.exists():
        shot.unlink()
    # 창이 너무 작으면 Edge가 스크린샷을 만들지 않는다. 넉넉히 열고 왼쪽 위만 잘라 쓴다
    window = f"{max(item['w'], 100)},{max(item['h'], 100)}"
    subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run",
                    f"--user-data-dir={work / 'profile'}", "--default-background-color=00000000",
                    f"--force-device-scale-factor={item['scale']}", f"--window-size={window}",
                    f"--screenshot={shot}", html.as_uri()],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=90)
    for _ in range(100):
        if shot.exists() and shot.stat().st_size > 0:
            break
        time.sleep(0.1)
    width, height = item["w"] * item["scale"], item["h"] * item["scale"]
    with Image.open(shot) as im:
        png = im.convert("RGBA").crop((0, 0, width, height))
    dest = OUT_ROOT / item["folder"] / f"{item['name']}.png"
    dest.parent.mkdir(parents=True, exist_ok=True)
    png.save(dest, optimize=True)
    alpha = png.getchannel("A")
    return item["name"], png.size, alpha.getextrema()


def contact_sheet(items):
    cell, pad, cols = 210, 18, 8
    font = ImageFont.truetype(r"C:\Windows\Fonts\consola.ttf", 15)
    rows = math.ceil(len(items) / cols)
    sheet = Image.new("RGBA", (cols * (cell + pad) + pad, rows * (cell + pad + 26) + pad), (8, 13, 23, 255))
    draw = ImageDraw.Draw(sheet)
    for i, item in enumerate(items):
        x = pad + (i % cols) * (cell + pad)
        y = pad + (i // cols) * (cell + pad + 26)
        draw.rectangle([x, y, x + cell, y + cell], fill=(19, 41, 74, 255))
        with Image.open(OUT_ROOT / item["folder"] / f"{item['name']}.png") as im:
            im = im.convert("RGBA")
            im.thumbnail((cell - 16, cell - 16), Image.LANCZOS)
            sheet.alpha_composite(im, (x + (cell - im.width) // 2, y + (cell - im.height) // 2))
        draw.text((x, y + cell + 4), item["name"][:26], font=font, fill=(169, 180, 197, 255))
    sheet.convert("RGB").save(HERE / "contact_sheet.png")


def main():
    card_assets()
    match_assets()
    currency_assets()
    common_assets()
    pattern = sys.argv[1] if len(sys.argv) > 1 else ""
    todo = [a for a in ASSETS if pattern in a["name"]]
    print(f"애셋 {len(ASSETS)}종 중 {len(todo)}종 렌더링")

    results = {}
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        futures = {pool.submit(render, item, i % 4): item for i, item in enumerate(todo)}
        for future in concurrent.futures.as_completed(futures):
            name, size, alpha = future.result()
            results[name] = size
            flag = "" if alpha[1] > 0 else "  ← 전부 투명!"
            print(f"  {name:36s} {size[0]}×{size[1]}{flag}")

    manifest = [{
        "file": f"Assets/UI Assets/{a['folder']}/{a['name']}.png",
        "size": [a["w"] * a["scale"], a["h"] * a["scale"]],
        "designSize": [a["w"], a["h"]],
        "pixelsPerUnit": 100 * a["scale"],
        "border_LBRT": a["border"],
        "imageType": a["mode"],
        "tint": a["tint"],
        "usage": a["usage"],
    } for a in ASSETS]
    (HERE / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=1), encoding="utf-8")
    contact_sheet(ASSETS)
    print(f"완료 · manifest.json · contact_sheet.png → {HERE}")


if __name__ == "__main__":
    main()
