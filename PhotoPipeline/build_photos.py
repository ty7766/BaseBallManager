"""
선수 사진 일괄 처리 — 배경 제거 + 얼굴 기준 정렬 + 512x640 저장

사용법
    pip install "rembg[cli]" opencv-python pillow
    python build_photos.py

    PhotoPipeline/input/   원본 사진을 넣는다. 파일명이 곧 photo 키가 된다 (예: gujawook.jpg)
    PhotoPipeline/cutout/  배경 제거 중간 결과 (자동 생성)
    PhotoPipeline/output/  최종 결과. Assets/Resources/Photos/ 로 복사하면 된다

경기_UI_명세.md 3-2 의 규격을 그대로 따른다.
    512 x 640 / 눈높이 세로 32% / 머리 위 여백 10% / 얼굴 중심 가로 50%
"""

import os
import shutil
import sys

import cv2
import numpy as np
from PIL import Image

# --- 납품 규격 (경기_UI_명세.md 3-2) ---
OUT_W, OUT_H = 512, 640
EYE_LINE_RATIO = 0.32      # 눈높이가 오는 세로 위치
HEAD_MARGIN_RATIO = 0.10   # 머리 위 최소 여백
FACE_HEIGHT_RATIO = 0.34   # 출력 높이 대비 얼굴 박스 높이 목표

ROOT = os.path.dirname(os.path.abspath(__file__))
INPUT_DIR = os.path.join(ROOT, "input")
CUTOUT_DIR = os.path.join(ROOT, "cutout")
OUTPUT_DIR = os.path.join(ROOT, "output")

EXTS = (".jpg", ".jpeg", ".png", ".webp", ".bmp")


def remove_backgrounds():
    """input 의 모든 사진에서 배경을 제거해 cutout 에 PNG 로 저장한다."""
    from rembg import new_session, remove

    os.makedirs(CUTOUT_DIR, exist_ok=True)
    session = new_session("u2net_human_seg")

    names = [f for f in sorted(os.listdir(INPUT_DIR)) if f.lower().endswith(EXTS)]
    if not names:
        sys.exit(f"[중단] {INPUT_DIR} 에 사진이 없습니다")

    for i, name in enumerate(names, 1):
        stem = os.path.splitext(name)[0]
        dst = os.path.join(CUTOUT_DIR, stem + ".png")
        if os.path.exists(dst):
            continue

        with open(os.path.join(INPUT_DIR, name), "rb") as f:
            data = remove(f.read(), session=session)
        with open(dst, "wb") as f:
            f.write(data)

        print(f"  누끼 {i}/{len(names)}  {stem}")

    return len(names)


def find_face(rgba):
    """얼굴 박스 (x, y, w, h). 못 찾으면 None."""
    rgb = rgba[:, :, :3]
    alpha = rgba[:, :, 3]

    # 투명 영역을 흰색으로 덮어야 검출기가 배경을 얼굴로 오인하지 않는다
    white = np.full_like(rgb, 255)
    mask = (alpha > 8)[:, :, None]
    composited = np.where(mask, rgb, white).astype(np.uint8)

    gray = cv2.cvtColor(composited, cv2.COLOR_BGR2GRAY)
    cascade = cv2.CascadeClassifier(
        cv2.data.haarcascades + "haarcascade_frontalface_default.xml")
    faces = cascade.detectMultiScale(gray, scaleFactor=1.1, minNeighbors=5, minSize=(40, 40))

    if len(faces) == 0:
        return None

    return max(faces, key=lambda f: f[2] * f[3])


def content_box(alpha):
    """알파가 있는 영역의 바운딩 박스. 얼굴을 못 찾았을 때의 대체 기준."""
    ys, xs = np.where(alpha > 8)
    if len(xs) == 0:
        return None
    return xs.min(), ys.min(), xs.max() - xs.min(), ys.max() - ys.min()


def align(path):
    """얼굴(없으면 알파 박스) 기준으로 스케일·위치를 맞춘 512x640 RGBA 를 만든다."""
    rgba = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    if rgba is None:
        return None, "읽기 실패"
    if rgba.shape[2] == 3:
        rgba = cv2.cvtColor(rgba, cv2.COLOR_BGR2BGRA)

    face = find_face(rgba)

    if face is not None:
        fx, fy, fw, fh = face
        scale = (OUT_H * FACE_HEIGHT_RATIO) / fh
        # 눈은 얼굴 박스 위에서 약 40% 지점에 있다
        anchor_x = fx + fw / 2.0
        anchor_y = fy + fh * 0.40
        target_y = OUT_H * EYE_LINE_RATIO
        note = "얼굴 기준"
    else:
        box = content_box(rgba[:, :, 3])
        if box is None:
            return None, "내용 없음"
        bx, by, bw, bh = box
        scale = (OUT_H * (1.0 - HEAD_MARGIN_RATIO)) / bh
        anchor_x = bx + bw / 2.0
        anchor_y = by
        target_y = OUT_H * HEAD_MARGIN_RATIO
        note = "얼굴 미검출 - 알파 기준"

    resized = cv2.resize(rgba, None, fx=scale, fy=scale, interpolation=cv2.INTER_AREA)
    ax, ay = anchor_x * scale, anchor_y * scale

    canvas = np.zeros((OUT_H, OUT_W, 4), dtype=np.uint8)
    ox = int(round(OUT_W / 2.0 - ax))
    oy = int(round(target_y - ay))

    sx0, sy0 = max(0, -ox), max(0, -oy)
    dx0, dy0 = max(0, ox), max(0, oy)
    w = min(resized.shape[1] - sx0, OUT_W - dx0)
    h = min(resized.shape[0] - sy0, OUT_H - dy0)
    if w <= 0 or h <= 0:
        return None, "정렬 실패"

    canvas[dy0:dy0 + h, dx0:dx0 + w] = resized[sy0:sy0 + h, sx0:sx0 + w]
    return canvas, note


def main():
    if not os.path.isdir(INPUT_DIR):
        os.makedirs(INPUT_DIR, exist_ok=True)
        sys.exit(f"[중단] {INPUT_DIR} 를 만들었습니다. 원본 사진을 넣고 다시 실행하세요")

    print("1) 배경 제거")
    total = remove_backgrounds()

    print("\n2) 얼굴 기준 정렬")
    if os.path.isdir(OUTPUT_DIR):
        shutil.rmtree(OUTPUT_DIR)
    os.makedirs(OUTPUT_DIR)

    failed, no_face = [], []
    names = sorted(f for f in os.listdir(CUTOUT_DIR) if f.lower().endswith(".png"))

    for i, name in enumerate(names, 1):
        canvas, note = align(os.path.join(CUTOUT_DIR, name))
        if canvas is None:
            failed.append(f"{name} ({note})")
            continue
        if "미검출" in note:
            no_face.append(name)

        rgb = cv2.cvtColor(canvas, cv2.COLOR_BGRA2RGBA)
        Image.fromarray(rgb).save(os.path.join(OUTPUT_DIR, name), optimize=True)
        print(f"  정렬 {i}/{len(names)}  {name}  [{note}]")

    print(f"\n완료 — 원본 {total} / 출력 {len(names) - len(failed)}")
    if no_face:
        print(f"\n얼굴 미검출 {len(no_face)}장 (알파 기준으로 맞췄으니 눈으로 확인 필요)")
        for n in no_face:
            print("   ", n)
    if failed:
        print(f"\n실패 {len(failed)}장")
        for n in failed:
            print("   ", n)

    print(f"\n출력 위치: {OUTPUT_DIR}")
    print("Assets/Resources/Photos/ 로 복사하고, CSV 의 photo 열에 파일 이름(확장자 제외)을 적으세요")


if __name__ == "__main__":
    main()
