"""
선수 사진 일괄 처리 — 배경 제거 + 얼굴 기준 정렬 + 768x960 저장

사용법
    python build_photos.py                       input/ 전체 (이미 만든 것은 건너뜀)
    python build_photos.py --force               전부 다시 만든다
    python build_photos.py --only gujawook_24    지정한 키만 다시 만든다 (여러 개 가능)

폴더
    input/    원본 사진. 파일명(확장자 제외)이 곧 CSV photo 키다 (예: gujawook_24.jpg)
    output/   768x960 PNG + review.html(검수 시트) + report.csv
    models/   얼굴 검출 모델(YuNet 0.2MB). 없으면 첫 실행 때 자동으로 받는다

필요한 것
    pip install "rembg[cpu]" opencv-python pillow pymatting
    배경 제거 모델(BiRefNet-portrait 973MB)은 rembg가 첫 실행 때 ~/.u2net 에 받는다

규격 (경기_UI_명세.md 3-2)
    768 x 960 · 눈높이 세로 32% · 머리(모자·헬멧 포함) 위 여백 최소 10% · 얼굴 중심 가로 50%
"""

import argparse
import csv
import html
import json
import os
import re
import sys
import time
import urllib.parse
import urllib.request

import cv2
import numpy as np
from PIL import Image, ImageOps
from pymatting import estimate_foreground_ml
from rembg import new_session, remove

# --- 납품 규격 (경기_UI_명세.md 3-2) ---
OUT_W, OUT_H = 768, 960
EYE_LINE = 0.32          # 눈 중심이 오는 세로 위치
HEAD_MARGIN = 0.10       # 머리 위 최소 여백
FACE_RATIO = 0.225       # 얼굴 박스 높이 / 출력 높이. 맨머리는 여백 약 14%, 모자·헬멧은 10% 근처에 온다
MAX_SHRINK = 0.80        # 여백을 지키려고 줄일 수 있는 한계. 더 필요하면 줄이지 않고 경고만 한다

# --- 검수 경고 기준 ---
UPSCALE_WARN = 1.3       # 원본을 이 배율 넘게 키우면 흐려지기 시작한다
UPSCALE_BAD = 2.0
BOTTOM_CUT_OK = 0.90     # 원본 하단 끝이 이 높이보다 아래면 카드 하단 바에 가려져 괜찮다

ROOT = os.path.dirname(os.path.abspath(__file__))
MODEL_DIR = os.path.join(ROOT, "models")
YUNET_FILE = "face_detection_yunet_2023mar.onnx"
YUNET_URL = "https://github.com/opencv/opencv_zoo/raw/main/models/face_detection_yunet/" + YUNET_FILE
REPORT_FILE = "_report.json"

REFINE_SIZE = 320        # 2차 검출 때 얼굴 조각 크기. 얼굴 박스가 약 145px이 된다
POINT_X = [0, 4, 6, 8, 10, 12]   # YuNet 결과에서 x 좌표 열 (박스 · 눈 · 눈 · 코 · 입꼬리 · 입꼬리)
POINT_Y = [1, 5, 7, 9, 11, 13]

EXTS = (".jpg", ".jpeg", ".png", ".webp", ".bmp")
KEY_RULE = re.compile(r"^[a-z0-9_]+$")

SEVERE, CAUTION = 2, 1   # 검수 등급: 2 = 반드시 확인 / 1 = 확인 권장


class FaceDetector:
    """YuNet 얼굴 검출. Haar와 달리 눈·코·입 좌표를 줘서 눈높이를 추정 없이 바로 맞춘다"""

    def __init__(self):
        path = os.path.join(MODEL_DIR, YUNET_FILE)
        if not os.path.exists(path):
            os.makedirs(MODEL_DIR, exist_ok=True)
            print(f"얼굴 검출 모델을 받는 중 ({YUNET_FILE}, 0.2MB)")
            urllib.request.urlretrieve(YUNET_URL, path)
        self._net = cv2.FaceDetectorYN.create(path, "", (320, 320), 0.6, 0.3, 5000)

    def detect(self, rgb):
        """원본 좌표계 얼굴 배열 (N x 15). 큰 사진은 줄여서 찾고 좌표만 되돌린다"""
        bgr = cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR)
        longest = max(bgr.shape[:2])
        tried = set()
        # 보통 크기 → 얼굴이 아주 큰 클로즈업 → 얼굴이 아주 작은 원경 순서로 시도
        for max_side in (1280, 640, 2560):
            k = min(1.0, max_side / longest)
            if k in tried:
                continue
            tried.add(k)
            img = bgr if k == 1.0 else cv2.resize(bgr, None, fx=k, fy=k, interpolation=cv2.INTER_AREA)
            self._net.setInputSize((img.shape[1], img.shape[0]))
            _, faces = self._net.detect(img)
            if faces is not None and len(faces) > 0:
                faces = faces.copy()
                faces[:, :14] /= k
                return faces
        return None

    def refine(self, rgb, face):
        """얼굴 주변을 일정한 크기로 잘라 한 번 더 찾는다.
        원본 해상도와 상관없이 얼굴 박스 높이가 1% 안쪽으로 일정해진다 (15° 기운 얼굴 포함 실측)"""
        x, y, w, h = face[:4]
        cx, cy = x + w / 2, y + h / 2
        k = REFINE_SIZE / (max(w, h) * 2.2)
        matrix = np.float32([[k, 0, REFINE_SIZE / 2 - cx * k], [0, k, REFINE_SIZE / 2 - cy * k]])
        interp = cv2.INTER_AREA if k < 1 else cv2.INTER_CUBIC
        patch = cv2.warpAffine(rgb, matrix, (REFINE_SIZE, REFINE_SIZE), flags=interp,
                               borderMode=cv2.BORDER_REPLICATE)
        self._net.setInputSize((REFINE_SIZE, REFINE_SIZE))
        _, found = self._net.detect(cv2.cvtColor(patch, cv2.COLOR_RGB2BGR))
        if found is None or len(found) == 0:
            return face

        # 조각 한가운데 있는 얼굴이 방금 고른 그 얼굴이다
        centers = found[:, 0:2] + found[:, 2:4] / 2
        best = found[int(np.argmin(np.linalg.norm(centers - REFINE_SIZE / 2, axis=1)))].copy()
        back = cv2.invertAffineTransform(matrix)
        points = np.stack([best[POINT_X], best[POINT_Y], np.ones(len(POINT_X))])
        best[POINT_X], best[POINT_Y] = back @ points
        best[2:4] /= k
        return best


class Segmenter:
    """rembg 배경 제거. 알파만 받고 합성은 직접 한다"""

    def __init__(self, model):
        self._session = new_session(model)

    def alpha(self, rgb):
        mask = remove(Image.fromarray(rgb), session=self._session, only_mask=True)
        return np.asarray(mask, dtype=np.uint8)


def load_rgb(path):
    """EXIF 회전을 반영한 RGB 배열"""
    with Image.open(path) as img:
        return np.asarray(ImageOps.exif_transpose(img).convert("RGB"))


def pick_face(faces, detector, rgb):
    """가장 크고 확실한 얼굴을 주인공으로 고르고, 다시 찾아 정밀한 눈·입 좌표를 얻는다"""
    areas = faces[:, 2] * faces[:, 3]
    main = int(np.argmax(areas * faces[:, 14]))
    f = detector.refine(rgb, faces[main])
    return {
        "eye": (f[4:6] + f[6:8]) / 2,
        "size": float(f[3]),
        "width": float(f[2]),
        "others": int(np.sum(areas >= areas[main] * 0.25)) - 1,
    }


def output_window(eye, scale):
    """출력 768x960이 원본에서 차지하는 영역 (x0, y0, w, h)"""
    w, h = OUT_W / scale, OUT_H / scale
    return eye[0] - w * 0.5, eye[1] - h * EYE_LINE, w, h


def segment_region(rgb, segmenter, eye, scale):
    """출력 영역 주변만 잘라 배경을 지운다. 사진 전체를 넣을 때보다 머리카락 경계가 훨씬 세밀하다"""
    img_h, img_w = rgb.shape[:2]
    x0, y0, w, h = output_window(eye, scale * MAX_SHRINK)   # 최대로 줄였을 때의 창까지 덮는다
    pad = 0.08
    rx0, ry0 = max(0, int(x0 - w * pad)), max(0, int(y0 - h * pad))
    rx1 = min(img_w, int(np.ceil(x0 + w * (1 + pad))))
    ry1 = min(img_h, int(np.ceil(y0 + h * (1 + pad))))
    region = np.ascontiguousarray(rgb[ry0:ry1, rx0:rx1])
    return rx0, ry0, region, segmenter.alpha(region)


def keep_subject(alpha, eye, face_width):
    """얼굴에 이어진 덩어리만 남긴다. 뒤쪽 관중·다른 선수 조각을 지운다"""
    solid = (alpha > 127).astype(np.uint8)
    count, labels, stats, _ = cv2.connectedComponentsWithStats(solid, connectivity=8)
    if count <= 2:
        return alpha

    x = int(np.clip(eye[0], 0, alpha.shape[1] - 1))
    y = int(np.clip(eye[1], 0, alpha.shape[0] - 1))
    main = int(labels[y, x])
    if main == 0:
        r = max(2, int(face_width / 2))
        ids = labels[max(0, y - r):y + r, max(0, x - r):x + r]
        ids = ids[ids > 0]
        if ids.size == 0:
            return alpha
        main = int(np.bincount(ids).argmax())

    keep = labels == main
    # 손에서 살짝 떨어져 잡힌 배트 끝처럼 가까운 큰 조각은 다시 붙인다
    reach = max(3, alpha.shape[0] // 60)
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * reach + 1, 2 * reach + 1))
    near = cv2.dilate(keep.astype(np.uint8), kernel) > 0
    for i in np.flatnonzero(stats[:, cv2.CC_STAT_AREA] >= stats[main, cv2.CC_STAT_AREA] * 0.02):
        if i not in (0, main) and np.any(near & (labels == i)):
            keep |= labels == i

    # 반투명 가장자리가 깎이지 않게 조금 넓힌 뒤 부드럽게 곱한다
    soft = cv2.dilate(keep.astype(np.uint8) * 255, np.ones((5, 5), np.uint8))
    soft = cv2.GaussianBlur(soft, (0, 0), 2)
    return (alpha.astype(np.uint16) * soft // 255).astype(np.uint8)


def head_top(alpha, eye, face_width):
    """눈에서 위로 올라가다 처음 배경을 만나는 높이. 머리 위로 든 배트는 떨어져 있어 세지 않는다"""
    ex, ey = int(round(eye[0])), int(round(eye[1]))
    if ey <= 0:
        return 0
    half = max(2, int(face_width * 0.35))
    cols = alpha[:ey, max(0, ex - half):ex + half + 1][::-1] > 127
    runs = np.where(cols.all(axis=0), cols.shape[0], np.argmin(cols, axis=0))
    return ey - int(runs.max())


def warp(img, x0, y0, scale, border):
    """(x0, y0)부터 scale 배율로 768x960을 뽑는다. 줄일 때는 면적 평균으로 먼저 줄여 계단 현상을 막는다"""
    if scale < 1.0:
        small = cv2.resize(img, None, fx=scale, fy=scale, interpolation=cv2.INTER_AREA)
        sx, sy = small.shape[1] / img.shape[1], small.shape[0] / img.shape[0]
        matrix = np.float32([[1, 0, -x0 * sx], [0, 1, -y0 * sy]])
        return cv2.warpAffine(small, matrix, (OUT_W, OUT_H), flags=cv2.INTER_LINEAR,
                              borderMode=border, borderValue=0)
    matrix = np.float32([[scale, 0, -x0 * scale], [0, scale, -y0 * scale]])
    return cv2.warpAffine(img, matrix, (OUT_W, OUT_H), flags=cv2.INTER_CUBIC,
                          borderMode=border, borderValue=0)


def decontaminate(rgb, alpha):
    """반투명 가장자리에 섞인 원래 배경색을 걷어낸다. 어두운 카드 위에 밝은 테두리(헤일로)가 안 남는다"""
    fg = estimate_foreground_ml(rgb / 255.0, alpha / 255.0)
    return np.clip(fg * 255.0 + 0.5, 0, 255).astype(np.uint8)


def edge_cuts(alpha, region_origin, image_size, window):
    """출력 창이 원본 밖으로 나가는데 그 경계에 선수가 걸려 있으면 몸이 칼로 자른 듯 끊긴다"""
    rx0, ry0 = region_origin
    img_w, img_h = image_size
    x0, y0, w, h = window
    rh, rw = alpha.shape
    rows = slice(max(0, int(y0)), min(rh, int(y0 + h)))
    cols = slice(max(0, int(x0)), min(rw, int(x0 + w)))

    def touches(line):
        return line.size > 0 and np.count_nonzero(line > 127) > line.size * 0.02

    cuts = {}
    if x0 < 0 and rx0 == 0 and touches(alpha[rows, 0]):
        cuts["좌"] = None
    if x0 + w > rw and rx0 + rw == img_w and touches(alpha[rows, rw - 1]):
        cuts["우"] = None
    if y0 < 0 and ry0 == 0 and touches(alpha[0, cols]):
        cuts["상"] = None
    if y0 + h > rh and ry0 + rh == img_h and touches(alpha[rh - 1, cols]):
        cuts["하"] = (rh - y0) / h   # 출력에서 원본이 끝나는 높이 (0~1)
    return cuts


def review_flags(key, face, scale, margin, cuts, bottom_fill):
    """검수 시트에 띄울 경고 목록 [(등급, 문구)]"""
    flags = []
    if scale > UPSCALE_BAD:
        flags.append((SEVERE, f"저해상도 x{scale:.1f}"))
    elif scale > UPSCALE_WARN:
        flags.append((CAUTION, f"확대 x{scale:.1f}"))
    if margin < 0.03:
        flags.append((SEVERE, f"머리 잘림 위험 (여백 {margin:.0%})"))
    elif margin < HEAD_MARGIN - 0.005:
        flags.append((CAUTION, f"머리 위 여백 {margin:.0%}"))
    if "상" in cuts:
        flags.append((SEVERE, "원본 위쪽에서 머리가 잘림"))
    for side in ("좌", "우"):
        if side in cuts:
            flags.append((CAUTION, f"원본 {side}측 끝에서 몸이 잘림"))
    if "하" in cuts and cuts["하"] < BOTTOM_CUT_OK:
        flags.append((CAUTION, f"원본이 {cuts['하']:.0%} 높이에서 끝남"))
    if "하" not in cuts and bottom_fill < 0.15:
        flags.append((CAUTION, "하단이 비어 있음 (몸이 바닥까지 안 닿음)"))
    if face["others"] > 0:
        flags.append((CAUTION, f"다른 얼굴 {face['others']}개 감지"))
    if not KEY_RULE.match(key):
        flags.append((CAUTION, "파일명 규칙 위반 (영문 소문자·숫자·_)"))
    return flags


def process(path, key, detector, segmenter):
    """사진 1장 → (768x960 RGBA 또는 None, 검수 기록)"""
    rgb = load_rgb(path)
    img_h, img_w = rgb.shape[:2]
    entry = {"source": [img_w, img_h]}

    faces = detector.detect(rgb)
    if faces is None:
        entry["error"] = "얼굴을 찾지 못함 (뒷모습·가려진 얼굴은 다른 사진으로 교체)"
        return None, entry
    face = pick_face(faces, detector, rgb)
    scale = FACE_RATIO * OUT_H / face["size"]
    entry["face_px"] = round(face["size"], 1)

    rx0, ry0, region, alpha = segment_region(rgb, segmenter, face["eye"], scale)
    eye = face["eye"] - (rx0, ry0)
    alpha = keep_subject(alpha, eye, face["width"])

    # 모자·헬멧이 여백선을 넘으면 얼굴 위치는 그대로 두고 배율만 줄인다
    top = head_top(alpha, eye, face["width"])
    room = (EYE_LINE - HEAD_MARGIN) * OUT_H
    head_span = (eye[1] - top) * scale
    if head_span > room:
        scale *= max(MAX_SHRINK, room / head_span)

    window = output_window(eye, scale)
    x0, y0 = window[0], window[1]
    rgb_out = warp(region, x0, y0, scale, cv2.BORDER_REPLICATE)
    alpha_out = warp(alpha, x0, y0, scale, cv2.BORDER_CONSTANT)
    rgb_out = decontaminate(rgb_out, alpha_out)

    margin = (top - y0) * scale / OUT_H
    cuts = edge_cuts(alpha, (rx0, ry0), (img_w, img_h), window)
    bottom_fill = float(np.mean(alpha_out[-1] > 127))
    flags = review_flags(key, face, scale, margin, cuts, bottom_fill)

    entry.update({
        "scale": round(float(scale), 3),
        "margin": round(float(margin), 3),
        "flags": [text for _, text in flags],
        "severity": max((level for level, _ in flags), default=0),
    })
    return np.dstack([rgb_out, alpha_out]), entry


def load_report(out_dir):
    path = os.path.join(out_dir, REPORT_FILE)
    if not os.path.exists(path):
        return {}
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def save_report(out_dir, report):
    with open(os.path.join(out_dir, REPORT_FILE), "w", encoding="utf-8") as f:
        json.dump(report, f, ensure_ascii=False, indent=1)

    # 엑셀에서 한글이 안 깨지게 BOM을 붙인다
    with open(os.path.join(out_dir, "report.csv"), "w", encoding="utf-8-sig", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["photo", "상태", "확대배율", "머리여백", "원본크기", "경고"])
        for key in sorted(report):
            e = report[key]
            state = "실패" if "error" in e else ("확인" if e["severity"] else "정상")
            writer.writerow([key, state, e.get("scale", ""), e.get("margin", ""),
                             "x".join(map(str, e["source"])) if "source" in e else "",
                             e.get("error") or " / ".join(e.get("flags", []))])


REVIEW_STYLE = """
:root{--bg:#0b1a2e;--fg:#eef3fa;--dim:#8fa0b8;--line:#1e3b60;--warn:#ffb23f;--bad:#ff5b4a}
*{box-sizing:border-box}body{margin:0;background:#07111f;color:var(--fg);font:14px/1.45 system-ui,"Malgun Gothic",sans-serif}
header{position:sticky;top:0;z-index:2;background:#07111fee;padding:14px 20px;border-bottom:1px solid var(--line)}
h1{margin:0 0 4px;font-size:18px}h1 small{color:var(--dim);font-weight:400;margin-left:8px}
.sum b{margin-right:10px}.sum .w{color:var(--warn)}.sum .x{color:var(--bad)}
.ctl{margin-top:8px;display:flex;gap:6px;flex-wrap:wrap;align-items:center;color:var(--dim)}
.ctl button{background:#11233d;color:var(--fg);border:1px solid var(--line);border-radius:6px;padding:3px 10px;cursor:pointer}
.ctl button.on{border-color:#e8b54a;color:#e8b54a}
main{display:grid;grid-template-columns:repeat(auto-fill,minmax(180px,1fr));gap:14px;padding:18px 20px}
figure{margin:0;background:#0b1a2e;border:1px solid var(--line);border-radius:10px;overflow:hidden}
figure.s1{border-color:var(--warn)}figure.s2,figure.fail{border-color:var(--bad)}
.ph{position:relative;aspect-ratio:4/5;background:#13294a}
.ph img{position:absolute;inset:0;width:100%;height:100%;object-fit:contain}
.bg-check .ph{background:repeating-conic-gradient(#8a8a8a 0 25%,#b5b5b5 0 50%) 0 0/16px 16px}
.bg-white .ph{background:#fff}
.guide .ph::before,.guide .ph::after{content:"";position:absolute;left:0;right:0;pointer-events:none}
.guide .ph::before{top:10%;border-top:1px dashed #ff5b4acc}
.guide .ph::after{top:32%;border-top:1px solid #4fd1c5cc}
.guide .ph i{position:absolute;top:0;bottom:0;left:50%;border-left:1px dotted #4fd1c599}
figcaption{padding:8px 10px}figcaption b{display:block;font-size:13px;word-break:break-all}
figcaption span{color:var(--dim);font-size:12px}
figcaption em{display:block;font-style:normal;font-size:12px;color:var(--warn)}
figure.s2 figcaption em,figure.fail figcaption em{color:var(--bad)}
.only-bad figure.s0{display:none}
"""

REVIEW_SCRIPT = """
const main=document.querySelector('main');
document.querySelectorAll('[data-bg]').forEach(b=>b.onclick=()=>{
  document.querySelectorAll('[data-bg]').forEach(x=>x.classList.toggle('on',x===b));
  main.classList.remove('bg-check','bg-white');if(b.dataset.bg!=='navy')main.classList.add('bg-'+b.dataset.bg);});
document.getElementById('guide').onchange=e=>main.classList.toggle('guide',e.target.checked);
document.getElementById('onlybad').onchange=e=>main.classList.toggle('only-bad',e.target.checked);
"""


def write_review(out_dir, in_dir, report, sources):
    """output/review.html — 전 사진을 가이드선(여백 10% · 눈높이 32% · 중앙)과 함께 한 화면에서 훑는다"""
    def order(key):
        e = report[key]
        return (-3 if "error" in e else -e["severity"], key)

    counts = {"ok": 0, "warn": 0, "bad": 0, "fail": 0}
    cards = []
    for key in sorted(report, key=order):
        e = report[key]
        if "error" in e:
            counts["fail"] += 1
            rel = os.path.relpath(os.path.join(in_dir, sources.get(key, "")), out_dir).replace(os.sep, "/")
            img, cls, meta, note = urllib.parse.quote(rel), "fail", "실패 · 원본 표시", e["error"]
        else:
            level = e["severity"]
            counts[("ok", "warn", "bad")[level]] += 1
            img, cls = urllib.parse.quote(key + ".png"), f"s{level}"
            meta = f"x{e['scale']:.2f} · 머리 여백 {e['margin']:.0%}"
            note = " / ".join(e["flags"])
        cards.append(
            f'<figure class="{cls}"><div class="ph"><img loading="lazy" src="{img}" alt=""><i></i></div>'
            f'<figcaption><b>{html.escape(key)}</b><span>{html.escape(meta)}</span>'
            f'<em>{html.escape(note)}</em></figcaption></figure>')

    page = (
        '<!doctype html><html lang="ko"><head><meta charset="utf-8">'
        '<meta name="viewport" content="width=device-width,initial-scale=1">'
        f'<title>선수 사진 검수</title><style>{REVIEW_STYLE}</style></head><body>'
        '<header><h1>선수 사진 검수<small>768×960 · 빨간 점선 = 머리 여백 10% · 청록선 = 눈높이 32%</small></h1>'
        f'<div class="sum"><b>전체 {len(report)}</b><b>정상 {counts["ok"]}</b>'
        f'<b class="w">확인 권장 {counts["warn"]}</b><b class="x">반드시 확인 {counts["bad"]}</b>'
        f'<b class="x">실패 {counts["fail"]}</b></div>'
        '<div class="ctl">배경 <button data-bg="navy" class="on">남색</button>'
        '<button data-bg="check">체크</button><button data-bg="white">흰색</button>'
        '<label><input type="checkbox" id="guide" checked> 가이드선</label>'
        '<label><input type="checkbox" id="onlybad"> 문제만 보기</label></div></header>'
        f'<main class="guide">{"".join(cards)}</main><script>{REVIEW_SCRIPT}</script></body></html>')
    with open(os.path.join(out_dir, "review.html"), "w", encoding="utf-8") as f:
        f.write(page)
    return counts


def parse_args():
    parser = argparse.ArgumentParser(description="선수 사진 배경 제거 + 얼굴 정렬 (768x960)")
    parser.add_argument("--force", action="store_true", help="이미 만든 사진도 다시 만든다")
    parser.add_argument("--only", nargs="+", metavar="KEY", help="지정한 photo 키만 다시 만든다")
    parser.add_argument("--model", default="birefnet-portrait", help="rembg 모델 (기본 birefnet-portrait)")
    parser.add_argument("--input", default=os.path.join(ROOT, "input"))
    parser.add_argument("--output", default=os.path.join(ROOT, "output"))
    return parser.parse_args()


def main():
    sys.stdout.reconfigure(errors="replace")
    args = parse_args()
    if not os.path.isdir(args.input):
        os.makedirs(args.input, exist_ok=True)
        sys.exit(f"[중단] {args.input} 를 만들었습니다. 원본 사진을 넣고 다시 실행하세요")
    os.makedirs(args.output, exist_ok=True)

    sources = {}
    for name in sorted(os.listdir(args.input)):
        if not name.lower().endswith(EXTS):
            continue
        key = os.path.splitext(name)[0]
        if key in sources:
            print(f"[건너뜀] 키 중복: {sources[key]} / {name}")
            continue
        sources[key] = name
    if not sources:
        sys.exit(f"[중단] {args.input} 에 사진이 없습니다")

    report = {k: v for k, v in load_report(args.output).items() if k in sources}
    todo = []
    for key, name in sources.items():
        src = os.path.join(args.input, name)
        dst = os.path.join(args.output, key + ".png")
        if args.only and key not in args.only:
            continue
        fresh = (os.path.exists(dst) and os.path.getmtime(dst) >= os.path.getmtime(src)
                 and key in report and "error" not in report[key])
        if args.force or args.only or not fresh:
            todo.append(key)

    print(f"원본 {len(sources)}장 · 이번에 처리 {len(todo)}장 · 모델 {args.model}")
    if todo:
        detector = FaceDetector()
        segmenter = Segmenter(args.model)
        started = time.time()
        for i, key in enumerate(todo, 1):
            t = time.time()
            try:
                image, entry = process(os.path.join(args.input, sources[key]), key, detector, segmenter)
            except Exception as error:   # 한 장이 깨져도 나머지는 계속 돈다
                image, entry = None, {"error": f"처리 오류: {error}"}
            if image is not None:
                Image.fromarray(image, "RGBA").save(os.path.join(args.output, key + ".png"))
            report[key] = entry

            spent = time.time() - t
            eta = (time.time() - started) / i * (len(todo) - i)
            state = entry.get("error") or (" / ".join(entry["flags"]) or "정상")
            print(f"  {i}/{len(todo)}  {key}  {spent:.1f}s  남은 시간 {eta / 60:.0f}분  [{state}]")
            if i % 20 == 0:
                save_report(args.output, report)

    save_report(args.output, report)
    counts = write_review(args.output, args.input, report, sources)
    print(f"\n정상 {counts['ok']} · 확인 권장 {counts['warn']} · 반드시 확인 {counts['bad']} · 실패 {counts['fail']}")
    print(f"검수 시트: {os.path.join(args.output, 'review.html')}")
    print("확인이 끝난 PNG를 Assets/Resources/Photos/ 로 복사하고, CSV photo 열에 키를 적으세요")


if __name__ == "__main__":
    main()
