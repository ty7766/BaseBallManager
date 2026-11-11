"""
사진 수집 목록 만들기 — 카드 CSV 에서 필요한 사진 키를 모으고, input/ 에 이미 있는 사진은 빼서 사진_수집_목록.md 를 다시 쓴다

사용법
    python PhotoPipeline/make_collection_list.py

규칙 (CardMasterData.BuildPhotoKey 와 같다)
    노말 · 시그니쳐  →  이름_팀          (같은 사진을 함께 쓴다)
    골든글러브       →  이름_팀_골글
"""
import csv
import json
import os
from collections import defaultdict
from datetime import date

ROOT = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(ROOT)
CSV_FILES = [os.path.join(PROJECT, "Assets", "Resources", "Data", name) for name in ("HitterCards.csv", "PitcherCards.csv")]
INPUT_DIR = os.path.join(ROOT, "input")
REPORT = os.path.join(ROOT, "output", "_report.json")
ADJUST = os.path.join(ROOT, "photo_adjust.json")
OUT = os.path.join(ROOT, "사진_수집_목록.md")
EXTS = (".jpg", ".jpeg", ".png", ".webp", ".bmp")
TEAMS = ["기아", "삼성", "LG", "한화", "두산", "SSG", "KT", "NC", "키움", "롯데"]
GOLDEN_GLOVE = "GG"
GOLDEN_GLOVE_SUFFIX = "_골글"
SEVERE = 2

HEADER = """# 선수 사진 수집 목록

생성 {today} · CSV 기준 자동 생성 (`python PhotoPipeline/make_collection_list.py`) · **이미 넣은 사진은 목록에서 뺐다**

## 넣는 곳 · 파일명

```
PhotoPipeline/input/김도영_기아.jpg
```

- **아래 줄이 곧 파일명이다.** 그대로 복사해 쓰면 된다 (확장자만 붙이면 끝)
- 확장자는 **`.jpg` 로 통일**한다 (`.jpeg` `.png` `.webp` `.bmp` 도 받지만 섞지 않는 편이 낫다)
- ⚠️ **`.avif` 는 안 된다.** 저장할 때 AVIF 로 떨어지면 다른 출처에서 받는다
- 원본은 **뉴스 경기 사진** (배트 든 장면 · 투구 장면 등). 정면 프로필 사진 아님
- **CSV 는 건드리지 않는다.** 파일명만 맞으면 게임이 집는다
- 다 넣은 뒤 `python PhotoPipeline/build_photos.py --install` 한 번이면 누끼부터 게임 폴더 복사까지 끝
- 사진을 넣었으면 이 목록도 다시 만든다: `python PhotoPipeline/make_collection_list.py`

## 고르는 기준 (카드는 머리~허벅지로 잘린다)

| 기준 | 이유 |
|---|---|
| **긴 변 1200px 이상** | 기사 본문에 실린 축소본(600×400 안팎)은 카드 상세에서 2~3배로 키워져 흐려진다. 사진을 눌러 **원본 보기**로 연 뒤 저장한다 |
| **허벅지까지 나온 사진** | 허리에서 잘린 사진은 카드 아래쪽이 비어 몸이 공중에 뜬다 |
| 얼굴이 보이는 사진 | 얼굴로 크기·위치를 맞춘다. 뒷모습·헬멧에 가린 얼굴은 실패한다 |
| 워터마크·자막 없는 것 | 누끼에 같이 남는다 (OSEN 로고가 남은 사례 있음) |
| 다른 선수와 안 겹친 것 | 겹친 몸은 한 덩어리로 같이 따진다 |

`output/review.html` 이 카드에 얹은 모습과 경고(저해상도 · 원본이 일찍 끝남 · 다른 얼굴)를 보여 준다. 경고가 뜬 사진만 다시 구하면 된다.
"""


def load_cards():
    """(사진 키, 팀, 골글 여부) — 이름 없는 예약 줄은 건너뛴다"""
    cards = []
    for path in CSV_FILES:
        with open(path, encoding="utf-8-sig", newline="") as f:
            for row in csv.DictReader(f):
                name, team, card_type = (row.get("name") or "").strip(), (row.get("team") or "").strip(), (row.get("cardType") or "").strip()
                if not name or not team:
                    continue
                golden = card_type == GOLDEN_GLOVE
                key = f"{name}_{team}" + (GOLDEN_GLOVE_SUFFIX if golden else "")
                cards.append((key, team, golden))
    return cards


def existing_keys():
    keys = set()
    for name in os.listdir(INPUT_DIR):
        stem, ext = os.path.splitext(name)
        if ext.lower() in EXTS:
            keys.add(stem)
    return keys


def recollect_keys():
    """(실패 — 게임에 안 들어감, 반드시 확인 — 들어갔지만 흐리거나 잘림) 키 → 이유"""
    if not os.path.exists(REPORT):
        return {}, {}
    with open(REPORT, encoding="utf-8") as f:
        report = json.load(f)
    failed, severe = {}, {}
    for key, entry in report.items():
        if "error" in entry:
            failed[key] = entry["error"]
        elif entry.get("severity", 0) >= SEVERE:
            severe[key] = " / ".join(entry.get("flags", []))
    return failed, severe


def review_recollect():
    """검수에서 다시 구하라고 한 사진 (photo_adjust.json 의 recollect)"""
    if not os.path.exists(ADJUST):
        return {}
    with open(ADJUST, encoding="utf-8") as f:
        data = json.load(f)
    return {k: v["recollect"] for k, v in data.items() if isinstance(v, dict) and v.get("recollect")}


def team_sections(keys_by_team, title, note):
    lines = [f"## {title}", "", note, ""]
    for team in TEAMS + sorted(set(keys_by_team) - set(TEAMS)):
        keys = sorted(keys_by_team.get(team, []))
        if not keys:
            continue
        lines += [f"### {team} — {len(keys)}장", ""] + [f"- {key}" for key in keys] + [""]
    return lines


def main():
    cards = load_cards()
    have = existing_keys()
    needed = {False: defaultdict(set), True: defaultdict(set)}
    for key, team, golden in cards:
        needed[golden][team].add(key)

    total = {g: sum(len(v) for v in needed[g].values()) for g in (False, True)}
    missing = {g: {team: {k for k in keys if k not in have} for team, keys in needed[g].items()} for g in (False, True)}
    left = {g: sum(len(v) for v in missing[g].values()) for g in (False, True)}
    all_needed = set().union(*(keys for g in (False, True) for keys in needed[g].values()))
    unused = sorted(have - all_needed)
    failed, severe = recollect_keys()
    failed = {k: why for k, why in failed.items() if k in all_needed}
    severe = {k: why for k, why in severe.items() if k in all_needed}

    lines = [HEADER.format(today=date.today().isoformat()), "## 합계", "",
             "| 구분 | 필요 | 있음 | **남음** |", "|---|---|---|---|"]
    for golden, label in ((False, "노말 · 시그 공용"), (True, "골든글러브 전용")):
        lines.append(f"| {label} | {total[golden]} | {total[golden] - left[golden]} | **{left[golden]}** |")
    lines.append(f"| **합계** | {sum(total.values())} | {sum(total.values()) - sum(left.values())} | **{sum(left.values())}** |")
    lines += ["", "---", ""]

    picked = review_recollect()
    if picked:
        lines += ["## 0-0. 다시 구해야 할 사진 — 검수에서 걸림", "",
                  "작성자 검수에서 바꾸기로 한 사진. 같은 이름으로 덮어쓴 뒤 photo_adjust.json 의 recollect 를 지운다.", ""]
        lines += [f"- {key} — {why}" for key, why in sorted(picked.items())] + [""]
    if failed:
        lines += ["## 0-1. 다시 구해야 할 사진 — 실패 (게임에 안 들어감)", "",
                  "파일은 있지만 처리에 실패했다. 다른 사진으로 같은 이름에 덮어쓴다.", ""]
        lines += [f"- {key} — {why}" for key, why in sorted(failed.items())] + [""]
    if severe:
        lines += ["## 0-2. 다시 구하면 좋은 사진 — 게임에는 들어갔지만 흐리거나 잘림", "",
                  "카드에 나오기는 한다. 더 큰 원본 · 머리 위 여백이 있는 사진으로 같은 이름에 덮어쓰면 좋아진다.", ""]
        lines += [f"- {key} — {why}" for key, why in sorted(severe.items())] + [""]
    if unused:
        lines += ["## ⚠️ 목록에 없는 파일 (이름 확인)", "",
                  "input/ 에 있지만 CSV 의 어떤 카드와도 이름이 맞지 않는다. 오타 · 팀 표기를 확인한다.", ""]
        lines += [f"- {key}" for key in unused] + [""]

    lines += team_sections(missing[False], "1. 노말 · 시그니쳐 공용", "노말과 시그니쳐가 같은 사진을 쓴다.")
    lines += team_sections(missing[True], "2. 골든글러브 전용",
                           "골든글러브 카드는 수상 사진을 따로 쓴다. 파일명 끝에 `_골글` 이 붙는다.")

    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines).rstrip() + "\n")

    print(f"필요 {sum(total.values())} · 있음 {sum(total.values()) - sum(left.values())} · 남음 {sum(left.values())}"
          f" · 실패 {len(failed)} · 흐림/잘림 {len(severe)} · 이름 안 맞는 파일 {len(unused)}")
    print(OUT)


if __name__ == "__main__":
    main()
