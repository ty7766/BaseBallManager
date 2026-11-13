"""
구단 엠블럼 — 작성자가 모은 원본(SVG · PNG)을 같은 규격의 투명 PNG 로 굽는다 (팀 선택 버튼 배경용)

사용법
    python Design/ui_assets/render_team_emblems.py

입력  Design/emblems_src/<영문 구단명>.svg | .png   (실제 구단 엠블럼 — 공개 저장소라 커밋하지 않는다)
출력  Assets/Resources/UI/Emblems/emblem_<팀>.png   (투명 여백을 잘라내고 긴 변 512 이하, 커밋하지 않는다)

SVG 는 Unity 기본 가져오기가 스프라이트가 아니고 그라데이션 · 클립 지원도 들쭉날쭉해서 Edge 로 미리 굽는다.
"""
import subprocess
import tempfile
import time
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Design" / "emblems_src"
OUT = ROOT / "Assets" / "Resources" / "UI" / "Emblems"
WORK = Path(tempfile.gettempdir()) / "bbm_team_emblems"
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

MAX_SIDE = 512
RENDER_BOX = 1024        # SVG 를 이 상자에 맞춰 그린 뒤 줄인다 (줄이면서 가장자리가 부드러워진다)
ALPHA_TRIM = 8           # 이 알파 이하 가장자리는 여백으로 본다

# 원본 파일명 → 게임 팀명 (UiPalette.TeamOrder 와 같은 표기)
TEAMS = {
    "KiaTigers": "기아",
    "SamsungLions": "삼성",
    "LgTwins": "LG",
    "HanhwaEagles": "한화",
    "DoosanBears": "두산",
    "SsgLanders": "SSG",
    "KtWiz": "KT",
    "NcDinos": "NC",
    "KiwoomHeroes": "키움",
    "LotteGiants": "롯데",
}

# 배경이 지워지지 않은 원본 — 가장자리에서 이어진 배경만 지운다
#   white       흰 바탕 (LG)
#   translucent 반투명 바둑판 무늬가 그림에 박힌 바탕 (SSG)
BACKDROP = {
    "LgTwins": "white",
    "SsgLanders": "translucent",
}
BACKDROP_LIGHT = 232     # 이보다 밝고
BACKDROP_GRAY = 14       # 채널 차이가 이보다 작으면 바탕색 후보
BACKDROP_ALPHA = 235     # translucent: 이보다 투명하면 바탕 후보
EDGE_BAND = 2            # 바탕과 맞닿은 이 두께 안에서 흰 번짐을 알파로 바꾼다


def render_svg(path):
    """SVG 를 RENDER_BOX 안에 꽉 차게 그린 투명 RGBA (Edge 헤드리스 스크린샷)"""
    WORK.mkdir(parents=True, exist_ok=True)
    html, shot = WORK / f"{path.stem}.html", WORK / f"{path.stem}.png"
    html.write_text(
        '<html><body style="margin:0;background:transparent;overflow:hidden">'
        f'<img src="{path.as_uri()}" style="width:{RENDER_BOX}px;height:{RENDER_BOX}px;object-fit:contain">'
        '</body></html>', encoding="utf-8")
    if shot.exists():
        shot.unlink()
    subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run",
                    f"--user-data-dir={WORK / 'profile'}", "--default-background-color=00000000",
                    "--force-device-scale-factor=1", f"--window-size={RENDER_BOX},{RENDER_BOX}",
                    f"--screenshot={shot}", html.as_uri()],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=120)
    for _ in range(100):
        if shot.exists() and shot.stat().st_size > 0:
            break
        time.sleep(0.1)
    with Image.open(shot) as im:
        return im.convert("RGBA").crop((0, 0, RENDER_BOX, RENDER_BOX))


def remove_backdrop(image, kind):
    """가장자리에서 이어진 바탕만 투명하게 한다. 엠블럼 안쪽에 갇힌 흰 글자 · 테는 남는다"""
    rgba = np.asarray(image, dtype=np.uint8).copy()
    rgb = rgba[..., :3].astype(np.int16)
    light = (rgb.min(axis=2) >= BACKDROP_LIGHT) & ((rgb.max(axis=2) - rgb.min(axis=2)) <= BACKDROP_GRAY)
    candidate = light if kind == "white" else (light | (rgba[..., 3] < BACKDROP_ALPHA))
    count, labels = cv2.connectedComponents(candidate.astype(np.uint8), connectivity=4)
    border = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    backdrop = np.isin(labels, border[border > 0])

    #바탕과 맞닿은 띠: 흰색에 섞여 밝아진 가장자리를 흰색 빼기로 되돌린다
    kernel = np.ones((2 * EDGE_BAND + 1, 2 * EDGE_BAND + 1), np.uint8)
    band = (cv2.dilate(backdrop.astype(np.uint8), kernel) > 0) & ~backdrop
    color = rgba[..., :3].astype(np.float32) / 255.0
    keep = np.clip((1.0 - color).max(axis=2), 1e-3, 1.0)
    unmixed = np.clip((color - (1.0 - keep[..., None])) / keep[..., None], 0.0, 1.0)
    alpha = rgba[..., 3].astype(np.float32) / 255.0
    rgba[band, :3] = (unmixed[band] * 255).round().astype(np.uint8)
    rgba[band, 3] = (alpha[band] * keep[band] * 255).round().astype(np.uint8)
    rgba[backdrop, 3] = 0
    return Image.fromarray(rgba, "RGBA")


def trim_and_fit(image):
    """투명 여백을 잘라내고 긴 변을 MAX_SIDE 이하로 줄인다 (작은 원본은 키우지 않는다)"""
    box = image.getchannel("A").point(lambda a: 255 if a > ALPHA_TRIM else 0).getbbox()
    if box:
        image = image.crop(box)
    scale = MAX_SIDE / max(image.size)
    if scale < 1:
        image = image.resize((round(image.width * scale), round(image.height * scale)), Image.LANCZOS)
    return image


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    sources = {p.stem: p for p in SRC.iterdir() if p.suffix.lower() in (".svg", ".png")}
    missing = [name for name in TEAMS if name not in sources]
    if missing:
        print(f"원본 없음: {', '.join(missing)}")
    for name, team in TEAMS.items():
        path = sources.get(name)
        if path is None:
            continue
        if path.suffix.lower() == ".svg":
            image = render_svg(path)
        else:
            with Image.open(path) as im:
                image = im.convert("RGBA")
        if name in BACKDROP:
            image = remove_backdrop(image, BACKDROP[name])
        image = trim_and_fit(image)
        target = OUT / f"emblem_{team}.png"
        image.save(target, optimize=True)
        print(f"  {path.name:20s} → {target.name} {image.width}x{image.height}")


if __name__ == "__main__":
    main()
