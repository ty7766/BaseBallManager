"""
선수 사진 자동 수집 — 뉴스 경기 사진 검색 → 후보 받기 → 자동으로 고르기 → input/{photo 키}.jpg

사용법
    python fetch_photos.py                     전체 (이어하기 — 이미 고른 선수는 건너뜀)
    python fetch_photos.py --limit 10          앞에서 10명만 (시험용)
    python fetch_photos.py --only 양의지 최정    지정한 선수만 다시 고른다 (이름 또는 photo 키)
    python fetch_photos.py review              검수 화면 — 브라우저에서 다른 후보를 눌러 바꾼다
    python fetch_photos.py keys                CSV photo 열의 빈 칸만 채운다 (엑셀에서 CSV를 다시 내보낸 뒤)
    python fetch_photos.py clean               검수가 끝난 뒤 안 고른 후보 원본을 지운다 (썸네일은 남김)

흐름
    1. CSV(HitterCards·PitcherCards)에서 인물×연도 목록을 만들고 photo 열 빈 칸에 키를 적는다
    2. 네이버·카카오 이미지 검색 API로 "{그해 팀명} {이름}" · "{연도} {이름}"을 찾아 그해 기사 사진만 남긴다
       2000년 이전은 기사 날짜가 촬영 연도가 아니므로 "{이름} 현역 시절"로 찾고 날짜는 보지 않는다
    3. 상위 후보를 받아 얼굴 검사 — 크기(확대 2배 이하) · 선명도 · 머리 잘림 · 다른 얼굴 · 몸이 프레임을 채우는지
    4. 같은 선수의 후보 얼굴끼리 비교(SFace) — 가장 많이 나오는 얼굴이 그 선수다. 다른 사람은 버린다
    5. 점수 1등을 input/{키}.jpg 로 둔다 → 검수 화면에서 확인 → build_photos.py 로 누끼

폴더
    keys.json   검색 API 키. 첫 실행 때 빈 양식을 만든다 (커밋 안 됨)
    fetch/      후보 원본 · 썸네일 · 검색 캐시 · 선수×연도별 기록 (커밋 안 됨)
    input/      고른 사진. 직접 넣어 둔 사진은 덮어쓰지 않는다
    models/     얼굴 인식 모델(SFace 37MB). 없으면 첫 실행 때 자동으로 받는다
"""

import argparse
import csv
import hashlib
import html
import io
import json
import math
import os
import re
import shutil
import sys
import threading
import time
import urllib.parse
import urllib.request
import webbrowser
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

import cv2
import numpy as np
import requests
from PIL import Image, ImageOps

from build_photos import (BOTTOM_CUT_OK, EXTS, FACE_RATIO, KEY_RULE, MODEL_DIR, OUT_H, UPSCALE_BAD,
                          FaceDetector, output_window)

ROOT = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(ROOT)
CSV_FILES = ("HitterCards.csv", "PitcherCards.csv")
KEYS_FILE = os.path.join(ROOT, "keys.json")

SFACE_FILE = "face_recognition_sface_2021dec.onnx"
SFACE_URL = "https://github.com/opencv/opencv_zoo/raw/main/models/face_recognition_sface/" + SFACE_FILE

NAVER_URL = "https://openapi.naver.com/v1/search/image"
KAKAO_URL = "https://dapi.kakao.com/v2/search/image"
USER_AGENT = "Mozilla/5.0 (compatible; BBM-PhotoFetcher/1.0; personal use)"
API_INTERVAL = 0.12      # 검색 API 호출 간격(초)

# --- 검색 ---
PER_TARGET = 10          # 선수×연도마다 받아서 검사할 후보 수
DATED_FROM = 2000        # 이 해부터는 기사 날짜 = 촬영 연도로 보고 다른 해 사진을 거른다
MIN_LONG, MIN_SHORT = 900, 500   # 검색 결과에 적힌 원본 크기 하한
MAX_BYTES = 25 * 1024 * 1024
THUMB_H = 400

# --- 얼굴 (build_photos 규격에서 나온 값) ---
FACE_GOOD = FACE_RATIO * OUT_H          # 216px — 원본 얼굴이 이 이상이면 키우지 않는다
FACE_MIN = FACE_GOOD / UPSCALE_BAD      # 108px — 이보다 작으면 2배 넘게 키워 흐려진다
FACE_REJECT = FACE_GOOD / 2.5           # 86px — 이보다 작으면 버린다
HEAD_ROOM = 1.05         # 눈 위로 원본이 얼굴 박스 높이의 이만큼 남아야 모자·헬멧 끝까지 들어온다 (맨머리 약 0.8)
HEAD_CUT = 0.65          # 이보다 적으면 이마부터 잘린 사진이라 버린다
SIDE_OK = 0.10           # 출력 창이 좌우로 원본 밖에 나가도 되는 비율
SHARP_REJECT = 30        # 얼굴 선명도(라플라시안 분산)가 이보다 낮으면 흔들린 사진이라 버린다
FACE_SURE = 0.85         # 검출 확신이 이보다 낮으면 로고·패치를 얼굴로 잡았을 수 있다

# --- 신원 (SFace 코사인 유사도) ---
ID_PAIR = 0.363          # 두 얼굴이 같은 사람 (OpenCV 권장값)
ID_KEEP = 0.40           # 선수 중심 얼굴과 이 이상 닮으면 그 선수
ID_REJECT = 0.28         # 이보다 낮으면 다른 사람으로 보고 버린다
DUP_BITS = 6             # 차이 해시가 이 비트 이하로 다르면 같은 사진

# 사진 기록의 경고 중 검수 화면 '확인 필요'로 올릴 것. 나머지(확대·하단 끝남)는 build_photos 검수가 다시 짚는다
SERIOUS = ("저해상도", "다른 얼굴", "신원 애매")

GAME_WORDS = ("홈런", "안타", "적시타", "결승타", "타격", "투구", "역투", "호투", "수비", "호수비", "주루", "도루",
              "슬라이딩", "세리머니", "환호", "포효", "끝내기", "득점", "타점", "삼진", "등판", "마운드", "타석")
OFF_FIELD_WORDS = ("시상식", "기자회견", "입단식", "계약", "사인회", "팬미팅", "결혼", "웨딩", "화보", "광고", "출국",
                   "입국", "귀국", "프로필", "취임", "방송", "예능", "골프", "미디어데이", "행사", "봉사", "기부")
BLOG_HOSTS = ("blogfiles", "postfiles", "cafefiles", "blogthumb", "cafeptthumb", "mblogthumb", "blogpfthumb",
              "post-phinf", "storep-phinf", "tistory", "dthumb")

TEAM_KEY = {"삼성": "samsung", "기아": "kia", "키움": "kiwoom", "LG": "lg", "두산": "doosan",
            "롯데": "lotte", "한화": "hanwha", "SSG": "ssg", "NC": "nc", "KT": "kt"}

# 게임 구단 → 그해 실제 구단명 (뉴스 검색어). [(그 이름을 쓴 마지막 해, 이름)]
TEAM_HISTORY = {
    "기아": [(2001, "해태 타이거즈"), (9999, "KIA")],
    "키움": [(1984, "삼미 슈퍼스타즈"), (1987, "청보 핀토스"), (1995, "태평양 돌핀스"), (2007, "현대 유니콘스"),
             (2008, "우리 히어로즈"), (2009, "히어로즈"), (2018, "넥센"), (9999, "키움")],
    "LG": [(1989, "MBC 청룡"), (9999, "LG")],
    "두산": [(1998, "OB 베어스"), (9999, "두산")],
    "한화": [(1993, "빙그레 이글스"), (9999, "한화")],
    "SSG": [(1999, "쌍방울 레이더스"), (2020, "SK"), (9999, "SSG")],
}

# --- 로마자 (성은 관용 표기, 이름은 국어의 로마자 표기법을 음절 단위로) ---
CHO = ("g", "kk", "n", "d", "tt", "r", "m", "b", "pp", "s", "ss", "", "j", "jj", "ch", "k", "t", "p", "h")
JUNG = ("a", "ae", "ya", "yae", "eo", "e", "yeo", "ye", "o", "wa", "wae", "oe", "yo", "u", "wo", "we", "wi",
        "yu", "eu", "ui", "i")
JONG = ("", "k", "k", "k", "n", "n", "n", "t", "l", "k", "m", "l", "l", "l", "p", "l", "m", "p", "p", "t", "t",
        "ng", "t", "t", "k", "t", "p", "t")
SURNAMES = {
    "김": "kim", "이": "lee", "박": "park", "최": "choi", "정": "jung", "강": "kang", "조": "cho", "윤": "yoon",
    "장": "jang", "임": "lim", "한": "han", "오": "oh", "서": "seo", "신": "shin", "권": "kwon", "황": "hwang",
    "안": "ahn", "송": "song", "류": "ryu", "유": "yoo", "전": "jeon", "홍": "hong", "고": "ko", "문": "moon",
    "양": "yang", "손": "son", "배": "bae", "백": "baek", "허": "heo", "남": "nam", "심": "shim", "노": "noh",
    "하": "ha", "곽": "kwak", "성": "sung", "차": "cha", "주": "joo", "우": "woo", "구": "koo", "민": "min",
    "진": "jin", "나": "na", "지": "ji", "엄": "eom", "채": "chae", "원": "won", "천": "cheon", "방": "bang",
    "공": "kong", "현": "hyun", "함": "ham", "변": "byun", "염": "yeom", "여": "yeo", "추": "choo", "도": "do",
    "소": "so", "석": "seok", "선": "sun", "설": "seol", "마": "ma", "길": "gil", "연": "yeon", "위": "wi",
    "표": "pyo", "명": "myung", "기": "ki", "반": "ban", "왕": "wang", "금": "keum", "옥": "ok", "육": "yook",
    "인": "in", "맹": "maeng", "제": "je", "모": "mo", "탁": "tak", "국": "kook", "어": "eo", "은": "eun",
    "편": "pyeon", "용": "yong", "예": "ye", "경": "kyung", "봉": "bong", "사": "sa", "부": "boo", "가": "ka",
    "복": "bok", "태": "tae", "목": "mok", "형": "hyung", "피": "pi", "두": "doo", "감": "kam", "음": "eum",
    "빈": "bin", "동": "dong", "온": "on", "호": "ho", "범": "beom", "좌": "jwa", "팽": "paeng", "승": "seung",
    "상": "sang", "시": "si", "갈": "gal", "단": "dan", "당": "dang", "계": "kye", "라": "ra", "견": "kyeon",
}
COMPOUND_SURNAMES = {"남궁": "namgoong", "황보": "hwangbo", "제갈": "jegal", "선우": "sunwoo", "독고": "dokgo",
                     "사공": "sagong", "서문": "seomun"}

FORMAT_EXT = {"JPEG": "jpg", "MPO": "jpg", "PNG": "png", "WEBP": "webp", "GIF": "gif", "BMP": "bmp"}
MIME = {".jpg": "image/jpeg", ".png": "image/png", ".webp": "image/webp", ".gif": "image/gif", ".bmp": "image/bmp"}
STATUS_LABEL = {"picked": "고름", "none": "후보 없음"}

NO_KEYS_HELP = """[중단] 검색 API 키가 없습니다. PhotoPipeline/keys.json 에 하나 이상 채우세요 (둘 다 넣으면 후보가 늘어납니다)
  네이버: developers.naver.com → Application → 애플리케이션 등록
          사용 API '검색' · 환경 'WEB 설정'(http://localhost) → Client ID / Client Secret
  카카오: developers.kakao.com → 내 애플리케이션 → 애플리케이션 추가 → 앱 키의 'REST API 키'"""


class QuotaExceeded(Exception):
    pass


class SearchError(Exception):
    pass


@dataclass
class Target:
    """사진 한 장 = 인물 × 연도. 같은 연도의 노말·시그·골글 카드가 이 한 장을 같이 쓴다"""
    key: str
    name: str            # CSV 이름 그대로 (동명이인 표시 A·B 포함) — 같은 사람 묶음 기준
    team: str
    year: int
    order: int
    homonym: bool        # 같은 이름의 다른 선수가 있다

    @property
    def search_name(self):
        return re.sub(r"[A-Za-z0-9]+$", "", self.name)

    @property
    def dated(self):
        return self.year >= DATED_FROM


class Context:
    """폴더 경로와 모델을 한데 들고 다닌다"""

    def __init__(self, args):
        self.data_dir = args.data
        self.input_dir = args.input
        self.work_dir = args.work
        self.per = args.per
        self.lock = threading.Lock()
        self.detector = None
        self.identity = None

    def path(self, *parts):
        return os.path.join(self.work_dir, *parts)


class FaceIdentity:
    """SFace 얼굴 특징(128차원). 같은 사람이면 코사인 유사도가 0.363 이상 나온다"""

    def __init__(self):
        path = os.path.join(MODEL_DIR, SFACE_FILE)
        if not os.path.exists(path):
            os.makedirs(MODEL_DIR, exist_ok=True)
            print(f"얼굴 인식 모델을 받는 중 ({SFACE_FILE}, 37MB)")
            urllib.request.urlretrieve(SFACE_URL, path + ".part")
            os.replace(path + ".part", path)
        self._net = cv2.FaceRecognizerSF.create(path, "")

    def feature(self, bgr, face):
        aligned = self._net.alignCrop(bgr, face)
        return unit(self._net.feature(aligned).flatten()).astype(np.float32)


class ImageSearch:
    """네이버·카카오 이미지 검색 API. 같은 검색어·페이지는 fetch/cache 에서 꺼내 할당량을 아낀다"""

    def __init__(self, keys, cache_dir):
        self._naver = None
        if keys["naver_client_id"] and keys["naver_client_secret"]:
            self._naver = {"X-Naver-Client-Id": keys["naver_client_id"],
                           "X-Naver-Client-Secret": keys["naver_client_secret"]}
        self._kakao = {"Authorization": "KakaoAK " + keys["kakao_rest_api_key"]} if keys["kakao_rest_api_key"] else None
        self._cache_dir = cache_dir
        self._session = requests.Session()
        self._last_call = 0.0
        self.calls = 0

    @property
    def sources(self):
        return [name for name, on in (("네이버", self._naver), ("카카오", self._kakao)) if on]

    def search(self, query, pages):
        """검색어 하나 → 두 곳의 결과를 이어 붙인 목록"""
        results = []
        for page in range(pages):
            if self._naver:
                results += self._cached("naver", query, page)
            if self._kakao:
                results += self._cached("kakao", query, page)
        return results

    def _cached(self, source, query, page):
        digest = hashlib.sha1(f"{query}|{page}".encode("utf-8")).hexdigest()[:20]
        path = os.path.join(self._cache_dir, source, digest + ".json")
        if os.path.exists(path):
            with open(path, encoding="utf-8") as f:
                return json.load(f)["items"]
        items = self._naver_page(query, page) if source == "naver" else self._kakao_page(query, page)
        if items is None:   # 일시적인 실패는 캐시하지 않는다
            return []
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8") as f:
            json.dump({"query": query, "page": page, "items": items}, f, ensure_ascii=False)
        return items

    def _get(self, url, params, headers):
        """API 호출 1번. 키 오류는 SearchError, 할당량 소진은 QuotaExceeded, 일시적인 실패는 None"""
        source = "네이버" if "naver" in url else "카카오"
        for attempt in range(4):
            wait = API_INTERVAL - (time.time() - self._last_call)
            if wait > 0:
                time.sleep(wait)
            self._last_call = time.time()
            try:
                response = self._session.get(url, params=params, headers=headers, timeout=10)
            except requests.RequestException:
                time.sleep(2 * (attempt + 1))
                continue
            self.calls += 1
            if response.status_code == 200:
                return response.json()
            if response.status_code in (401, 403):
                raise SearchError(f"{source} 키 인증 실패 (HTTP {response.status_code}) — keys.json 을 확인하세요")
            if response.status_code == 429:
                if attempt == 3:
                    raise QuotaExceeded(f"{source} 검색 할당량을 다 썼습니다")
                time.sleep(3 * (attempt + 1))
                continue
            if response.status_code >= 500:
                time.sleep(2 * (attempt + 1))
                continue
            raise SearchError(f"{source} 검색 실패 (HTTP {response.status_code}): {response.text[:200]}")
        return None

    def _naver_page(self, query, page):
        params = {"query": query, "display": 100, "start": 1 + page * 100, "sort": "sim", "filter": "large"}
        data = self._get(NAVER_URL, params, self._naver)
        if data is None:
            return None
        items = []
        for item in data.get("items", []):
            link = item.get("link") or ""
            items.append({"src": "naver", "url": link, "title": clean_title(item.get("title", "")),
                          "w": as_int(item.get("sizewidth")), "h": as_int(item.get("sizeheight")),
                          "date": date_from_url(link), "page": "", "site": urllib.parse.urlparse(link).netloc,
                          "news": "imgnews" in link, "blog": False})
        return items

    def _kakao_page(self, query, page):
        params = {"query": query, "sort": "accuracy", "page": page + 1, "size": 80}
        data = self._get(KAKAO_URL, params, self._kakao)
        if data is None:
            return None
        items = []
        for doc in data.get("documents", []):
            link, page_url = doc.get("image_url") or "", doc.get("doc_url") or ""
            stamp = (doc.get("datetime") or "")[:10]
            date = stamp if re.fullmatch(r"\d{4}-\d{2}-\d{2}", stamp) else date_from_url(page_url)
            items.append({"src": "kakao", "url": link, "title": "",
                          "w": as_int(doc.get("width")), "h": as_int(doc.get("height")),
                          "date": date, "page": page_url, "site": doc.get("display_sitename") or "",
                          "news": doc.get("collection") == "news",
                          "blog": doc.get("collection") in ("blog", "cafe")})
        return items


# ---------------------------------------------------------------- 키 · CSV

def romanize(hangul):
    """한글 → 로마자 (음절 단위, 소리 바뀜 없음. 'ㄹㄹ'만 ll)"""
    out, last_l = [], False
    for ch in hangul:
        code = ord(ch) - 0xAC00
        if not 0 <= code < 11172:
            if ch.isascii() and ch.isalnum():
                out.append(ch.lower())
            last_l = False
            continue
        cho, jung, jong = code // 588, code % 588 // 28, code % 28
        out.append(("l" if cho == 5 and last_l else CHO[cho]) + JUNG[jung] + JONG[jong])
        last_l = JONG[jong] == "l"
    return "".join(out)


def romanize_name(hangul):
    """'박동원' → 'parkdongwon'. 두세 글자 이름만 성을 관용 표기로 (네 글자 이상은 외국인 선수)"""
    if len(hangul) >= 3 and hangul[:2] in COMPOUND_SURNAMES:
        return COMPOUND_SURNAMES[hangul[:2]] + romanize(hangul[2:])
    if 2 <= len(hangul) <= 3 and hangul[0] in SURNAMES:
        return SURNAMES[hangul[0]] + romanize(hangul[1:])
    return romanize(hangul)


def name_key(name):
    """'이승현A' → 'leeseunghyeon_a'"""
    hangul, mark = re.match(r"^(.*?)([A-Za-z0-9]*)$", name).groups()
    key = re.sub(r"[^a-z0-9]", "", romanize_name(hangul))
    return f"{key}_{mark.lower()}" if mark else key


def team_key(team):
    return TEAM_KEY.get(team) or re.sub(r"[^a-z0-9]", "", romanize(team)) or "team"


def team_in(team, year):
    """게임 구단명 → 그해 실제 구단명"""
    for last, name in TEAM_HISTORY.get(team, ()):
        if year <= last:
            return name
    return team


def full_year(yy):
    number = int(yy)
    return 1900 + number if number >= 50 else 2000 + number


def read_csv(path):
    """CSV를 원문 그대로 읽는다 → (BOM 여부, 줄바꿈, 끝 줄바꿈 여부, 줄 목록)"""
    with open(path, "rb") as f:
        text = f.read().decode("utf-8")
    bom = text.startswith("﻿")
    text = text[1:] if bom else text
    newline = "\r\n" if "\r\n" in text else "\n"
    lines = text.split(newline)
    tail = lines[-1] == ""
    if tail:
        lines.pop()
    return bom, newline, tail, lines


def card_rows(data_dir):
    """두 CSV의 선수 행(이름이 있는 행) → [열 이름: 값]"""
    rows = []
    for file in CSV_FILES:
        _, _, _, lines = read_csv(os.path.join(data_dir, file))
        header = [h.strip() for h in lines[0].split(",")]
        for line in lines[1:]:
            row = dict(zip(header, (c.strip() for c in line.split(","))))
            if not row.get("name"):
                continue
            if not row.get("team") or not row.get("year", "").isdigit():
                print(f"[건너뜀] {file} cardId {row.get('cardId')}: 팀·연도가 비어 있음")
                continue
            rows.append(row)
    return rows


def person_of(row):
    return row["name"], row["team"], row["year"].zfill(2)


def build_targets(rows):
    """인물×연도 목록과 photo 키. CSV에 이미 적어 둔 키는 그대로 쓴다"""
    persons = {}
    for row in rows:
        persons.setdefault(person_of(row), []).append(row)
    teams_of = {}
    names_of = {}
    for name, team, yy in persons:
        teams_of.setdefault((name, yy), set()).add(team)
        names_of.setdefault(re.sub(r"[A-Za-z0-9]+$", "", name), set()).add(name)

    keys, owner = {}, {}
    for person, members in persons.items():
        name, team, yy = person
        key = next((r["photo"] for r in members if r.get("photo")), "")
        if not key:
            base = name_key(name)
            if len(teams_of[(name, yy)]) > 1:   # 같은 해 같은 이름이 두 팀에 → 팀을 붙인다
                base += "_" + team_key(team)
            key = f"{base}_{yy}"
            number = 2
            while key in owner:
                key = f"{base}_{team_key(team)}{'' if number == 2 else number}_{yy}"
                number += 1
        if key in owner:
            print(f"[경고] photo 키 중복: {key} — {owner[key]} / {person}")
        owner[key] = person
        keys[person] = key

    targets = []
    for order, person in enumerate(persons):
        name, team, yy = person
        homonym = len(teams_of[(name, yy)]) > 1 or len(names_of[re.sub(r"[A-Za-z0-9]+$", "", name)]) > 1
        targets.append(Target(keys[person], name, team, full_year(yy), order, homonym))
    return targets, keys


def write_keys(data_dir, keys):
    """photo 열이 없으면 맨 뒤에 붙이고 빈 칸에만 키를 적는다. 직접 적은 키는 건드리지 않는다"""
    filled = 0
    for file in CSV_FILES:
        path = os.path.join(data_dir, file)
        bom, newline, tail, lines = read_csv(path)
        header = [h.strip() for h in lines[0].split(",")]
        changed = False
        if "photo" not in header:
            header.append("photo")
            lines[0] += ",photo"
            changed = True
        index = {h: i for i, h in enumerate(header)}
        for i in range(1, len(lines)):
            if not lines[i].strip():
                continue
            cols = lines[i].split(",")
            if len(cols) < len(header):
                cols += [""] * (len(header) - len(cols))
                changed = True
            row = {h: cols[j].strip() for h, j in index.items()}
            if row["name"] and not row["photo"] and row["team"] and row["year"].isdigit():
                cols[index["photo"]] = keys[person_of(row)]
                filled += 1
                changed = True
            lines[i] = ",".join(cols)
        if changed:
            text = ("﻿" if bom else "") + newline.join(lines) + (newline if tail else "")
            with open(path, "wb") as f:
                f.write(text.encode("utf-8"))
    return filled


# ---------------------------------------------------------------- 검색 · 받기

def clean_title(text):
    return html.unescape(re.sub(r"<[^>]+>", "", text or "")).strip()


def as_int(value):
    try:
        return int(value)
    except (TypeError, ValueError):
        return 0


DATE_IN_URL = (
    re.compile(r"/((?:19|20)\d\d)/(0[1-9]|1[0-2])/(0[1-9]|[12]\d|3[01])/"),           # .../2019/08/15/...
    re.compile(r"/((?:19|20)\d\d)(0[1-9]|1[0-2])/(0[1-9]|[12]\d|3[01])/"),            # .../201908/15/...
    re.compile(r"(?<!\d)((?:19|20)\d\d)(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])(?:\d{4,9})?(?!\d)"),  # 20190815(1930..)
    re.compile(r"/((?:19|20)\d\d)(0[1-9]|1[0-2])/"),                                   # .../photo/202405/...
)


def date_from_url(url):
    """주소에 박힌 날짜 → 'YYYY-MM-DD' (월까지만 있으면 'YYYY-MM'). 뉴스 사진 주소는 대개 기사 날짜를 담는다"""
    for pattern in DATE_IN_URL:
        m = pattern.search(url or "")
        if m and 1990 <= int(m.group(1)) <= time.localtime().tm_year:
            return "-".join(m.groups())
    return None


def search_plan(t, mode):
    """[(검색어, 페이지 수)]. dated = 그해 기사만 · loose = 날짜 무시 · archive = 2000년 이전 선수"""
    team, name = team_in(t.team, t.year), t.search_name
    if mode == "dated":
        return [(f"{team} {name}", 2), (f"{t.year} {name}", 1)]
    if mode == "loose":
        return [(f"{team} {name}", 2), (name, 1)]
    return [(f"{team} {name}", 1), (f"{name} 현역 시절", 1)]


def prior(c, t, year):
    """받기 전 점수 — 그해 기사 · 제목에 이름 · 뉴스 · 경기 장면 · 큰 원본"""
    score = 0.0
    if year == t.year:
        score += 2.0
    if t.search_name in c["title"]:
        score += 1.5
    if c["news"]:
        score += 1.0
    if any(w in c["title"] for w in GAME_WORDS):
        score += 0.5
    if any(w in c["title"] for w in OFF_FIELD_WORDS):
        score -= 2.0
    if c["h"]:
        score += min(1.5, max(0.0, math.log2(c["h"] / 800)))
    return score


def search_pool(t, search, mode, tried):
    """검색 결과 → 받아 볼 후보 (사전 점수순). 블로그·카페·작은 사진·다른 해 기사는 뺀다"""
    pool, seen = [], set(tried)
    for query, pages in search_plan(t, mode):
        for c in search.search(query, pages):
            url = c["url"]
            if not url.startswith("http") or url in seen:
                continue
            seen.add(url)
            if c["blog"] or any(h in url for h in BLOG_HOSTS):
                continue
            if c["w"] and c["h"] and (max(c["w"], c["h"]) < MIN_LONG or min(c["w"], c["h"]) < MIN_SHORT):
                continue
            year = int(c["date"][:4]) if c["date"] else None
            if mode == "dated" and year is not None and year != t.year:
                continue
            pool.append(dict(c, prior=round(prior(c, t, year), 2)))
    pool.sort(key=lambda c: c["prior"], reverse=True)
    return pool


_local = threading.local()


def download(url):
    """이미지 원본 바이트. 실패하면 None (다른 후보가 있으니 다시 시도하지 않는다)"""
    session = getattr(_local, "session", None)
    if session is None:
        session = _local.session = requests.Session()
        session.headers["User-Agent"] = USER_AGENT
    try:
        with session.get(url, timeout=(5, 15), stream=True) as response:
            if response.status_code != 200:
                return None
            data = response.raw.read(MAX_BYTES + 1, decode_content=True)
    except Exception:   # 연결·읽기 오류는 종류와 상관없이 그 후보만 버린다
        return None
    return data if 1000 < len(data) <= MAX_BYTES else None


def decode(data):
    """바이트 → (EXIF 회전을 반영한 RGB 배열, 확장자). 이미지가 아니면 (None, None)"""
    try:
        with Image.open(io.BytesIO(data)) as img:
            ext = FORMAT_EXT.get(img.format)
            if ext is None:
                return None, None
            return np.asarray(ImageOps.exif_transpose(img).convert("RGB")), ext
    except Exception:   # 깨진 파일·지원하지 않는 형식
        return None, None


def dhash(rgb):
    """차이 해시 64비트. 다른 기사에 다른 크기로 실린 같은 사진을 찾는다"""
    gray = cv2.cvtColor(cv2.resize(rgb, (9, 8), interpolation=cv2.INTER_AREA), cv2.COLOR_RGB2GRAY)
    bits = (gray[:, 1:] > gray[:, :-1]).flatten()
    return f"{int(''.join('1' if b else '0' for b in bits), 2):016x}"


def hamming(a, b):
    return bin(int(a, 16) ^ int(b, 16)).count("1")


def save_thumb(rgb, path):
    h, w = rgb.shape[:2]
    k = THUMB_H / h
    small = cv2.resize(rgb, (max(1, round(w * k)), THUMB_H), interpolation=cv2.INTER_AREA) if k < 1 else rgb
    Image.fromarray(small).save(path, "JPEG", quality=82)


def file_sha1(path):
    with open(path, "rb") as f:
        return hashlib.sha1(f.read()).hexdigest()


# ---------------------------------------------------------------- 얼굴 검사

def unit(v):
    return v / (np.linalg.norm(v) + 1e-9)


def clamp(value, low=0.0, high=1.0):
    return max(low, min(high, value))


def sharpness(rgb, face):
    """얼굴 박스를 높이 128px로 맞춘 뒤 라플라시안 분산. 흔들리거나 초점이 나간 사진일수록 낮다"""
    x, y, w, h = (float(v) for v in face[:4])
    x0, y0 = max(0, int(x)), max(0, int(y))
    x1, y1 = min(rgb.shape[1], int(x + w)), min(rgb.shape[0], int(y + h))
    if x1 - x0 < 8 or y1 - y0 < 8:
        return 0.0
    gray = cv2.cvtColor(np.ascontiguousarray(rgb[y0:y1, x0:x1]), cv2.COLOR_RGB2GRAY)
    k = 128.0 / gray.shape[0]
    gray = cv2.resize(gray, None, fx=k, fy=k, interpolation=cv2.INTER_AREA if k < 1 else cv2.INTER_CUBIC)
    return float(cv2.Laplacian(gray, cv2.CV_64F).var())


def analyze(rgb, detector, identity):
    """후보 1장 → (검사 기록, 얼굴 특징). build_photos와 같은 얼굴·같은 출력 창으로 판단한다"""
    img_h, img_w = rgb.shape[:2]
    rec = {"w": img_w, "h": img_h}
    faces = detector.detect(rgb)
    if faces is None:
        rec["reject"] = "얼굴 없음"
        return rec, None

    areas = faces[:, 2] * faces[:, 3]
    main = int(np.argmax(areas * faces[:, 14]))
    face = detector.refine(rgb, faces[main])
    size = float(face[3])
    eye = (face[4:6] + face[6:8]) / 2
    x0, y0, w, h = output_window(eye, FACE_GOOD / size)
    rec.update({
        "face": int(round(size)),
        "box": [round(float(v), 4) for v in (face[0] / img_w, face[1] / img_h, face[2] / img_w, size / img_h)],
        "conf": round(float(faces[main, 14]), 2),
        "others": int(np.sum(areas >= areas[main] * 0.25)) - 1,
        "room": round(float(eye[1] / size), 2),                        # 눈 위로 남은 원본 (얼굴 박스 높이 단위)
        "fill": round(float(min(1.0, (img_h - y0) / h)), 3),          # 출력 창 세로 중 원본이 닿는 높이
        "side": round(float(max(0.0, -x0, x0 + w - img_w) / w), 3),   # 출력 창이 좌우로 원본 밖에 나간 비율
        "sharp": round(sharpness(rgb, face), 1),
    })
    if size < FACE_REJECT:
        rec["reject"] = f"얼굴 작음 {size:.0f}px"
    elif rec["room"] < HEAD_CUT:
        rec["reject"] = "머리 잘림"
    elif rec["sharp"] < SHARP_REJECT:
        rec["reject"] = "흔들림"
    feature = identity.feature(cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR), face) if identity else None
    return rec, feature


def gather(ctx, t, pool, first_id, tried):
    """후보 원본을 받아 검사한다 → (기록 목록, {id: 얼굴 특징}). 얼굴이 없는 사진은 파일을 남기지 않는다"""
    jobs = pool[:ctx.per]
    with ThreadPoolExecutor(8) as executor:
        blobs = list(executor.map(lambda c: download(c["url"]), jobs))
    os.makedirs(ctx.path("cands", t.key), exist_ok=True)
    os.makedirs(ctx.path("thumbs", t.key), exist_ok=True)

    records, feats, number = [], {}, first_id
    for c, data in zip(jobs, blobs):
        tried.add(c["url"])
        rgb, ext = decode(data) if data else (None, None)
        if rgb is None:
            continue
        cid = f"{number:02d}"
        number += 1
        rec = {"id": cid, "ext": ext, "hash": dhash(rgb)}
        rec.update({k: c[k] for k in ("src", "url", "page", "title", "date", "site", "prior")})
        info, feature = analyze(rgb, ctx.detector, ctx.identity)
        rec.update(info)
        records.append(rec)
        if "face" not in rec:
            continue
        with open(ctx.path("cands", t.key, f"{cid}.{ext}"), "wb") as f:
            f.write(data)
        save_thumb(rgb, ctx.path("thumbs", t.key, cid + ".jpg"))
        if feature is not None:
            feats[cid] = feature
    return records, feats


def mark_duplicates(records):
    """같은 사진(다른 기사·다른 크기)은 얼굴이 큰 쪽만 남긴다"""
    kept = []
    for rec in sorted((r for r in records if "face" in r and not r.get("reject")), key=lambda r: -r["face"]):
        if any(hamming(rec["hash"], k["hash"]) <= DUP_BITS for k in kept):
            rec["reject"] = "중복"
        else:
            kept.append(rec)


def usable(records):
    return [r for r in records if "face" in r and not r.get("reject")]


# ---------------------------------------------------------------- 신원 · 점수

def dominant_face(features):
    """얼굴 특징 행렬 → (가장 많이 닮은 무리의 중심, 소속 여부). 닮은 쌍이 하나도 없으면 (None, None)"""
    sims = features @ features.T
    np.fill_diagonal(sims, -1.0)
    close = sims >= ID_PAIR
    votes = close.sum(1)
    if votes.max() == 0:
        return None, None
    strength = np.where(close, sims, 0).sum(1)
    anchor = int(np.lexsort((-strength, -votes))[0])
    members = close[anchor].copy()
    members[anchor] = True
    center = unit(features[members].mean(0))
    for _ in range(2):
        again = features @ center >= ID_KEEP
        if again.sum() < 2:
            break
        members, center = again, unit(features[again].mean(0))
    return center, members


def identity_center(vectors):
    """같은 선수 후보 얼굴들 → (중심, 그 얼굴 수, 두 번째로 많이 나온 얼굴 수)"""
    if len(vectors) < 2:
        return None, 0, 0
    features = np.stack(vectors).astype(np.float32)
    center, members = dominant_face(features)
    if center is None:
        return None, 0, 0
    rival, rest = 0, features[~members]
    if len(rest) >= 2:
        _, rival_members = dominant_face(rest)
        rival = int(rival_members.sum()) if rival_members is not None else 0
    return center, int(members.sum()), rival


def rate(c, confident):
    """검사 기록 → 점수·메모. 신원이 확실한 선수면 다른 사람 얼굴을 버린다"""
    if "face" not in c:
        return
    notes, score = [], 0.0
    face = c["face"]
    score += 3.0 * clamp((face - FACE_MIN) / (FACE_GOOD - FACE_MIN))
    if face < FACE_MIN:
        notes.append(f"저해상도 x{FACE_GOOD / face:.1f}")
    score += 2.0 * clamp((math.log10(max(c["sharp"], 1.0)) - 1.5) / 1.0)
    if c["room"] < HEAD_ROOM:
        score -= 3.0 * (HEAD_ROOM - c["room"])
        notes.append("머리 위 여유 부족")
    if c["conf"] < FACE_SURE:
        score -= 1.0
        notes.append("얼굴 확신 낮음")
    if c["fill"] < BOTTOM_CUT_OK:
        score -= 3.0 * (BOTTOM_CUT_OK - c["fill"])
        notes.append(f"하단 {c['fill']:.0%}에서 끝남")
    if c["side"] > SIDE_OK:
        score -= 4.0 * (c["side"] - SIDE_OK)
        notes.append("좌우 잘림")
    if c["others"] > 0:
        score -= 2.0
        notes.append(f"다른 얼굴 {c['others']}")
    score += 0.6 * c.get("prior", 0.0)

    ident = c.get("ident")
    if confident and ident is not None:
        if ident < ID_REJECT:
            c.setdefault("reject", "다른 사람")
        elif ident < ID_KEEP:
            score -= 1.5
            notes.append("신원 애매")
        else:
            score += 2.0 * clamp((ident - ID_KEEP) / 0.3)
    c["score"], c["flags"] = round(score, 2), notes


def choose(cands, used):
    """점수 1등. 같은 선수의 다른 연도에 이미 쓴 사진은 건너뛴다"""
    for c in sorted(usable(cands), key=lambda c: -c.get("score", -99)):
        if all(hamming(c["hash"], h) > DUP_BITS for h in used):
            return c
    return None


def picked(st):
    return next((c for c in st["cands"] if c["id"] == st.get("pick")), None) if st else None


# ---------------------------------------------------------------- 기록 · input

def load_state(ctx, key):
    path = ctx.path("state", key + ".json")
    if not os.path.exists(path):
        return None
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def save_state(ctx, st):
    os.makedirs(ctx.path("state"), exist_ok=True)
    path = ctx.path("state", st["key"] + ".json")
    with open(path + ".tmp", "w", encoding="utf-8") as f:
        json.dump(st, f, ensure_ascii=False, indent=1)
    os.replace(path + ".tmp", path)


def all_states(ctx):
    folder = ctx.path("state")
    if not os.path.isdir(folder):
        return []
    return [st for st in (load_state(ctx, n[:-5]) for n in os.listdir(folder) if n.endswith(".json")) if st]


def save_feats(ctx, key, feats):
    os.makedirs(ctx.path("feats"), exist_ok=True)
    ids = sorted(feats)
    matrix = np.stack([feats[i] for i in ids]) if ids else np.zeros((0, 128), np.float32)
    np.savez(ctx.path("feats", key + ".npz"), ids=np.array(ids, dtype=str), feats=matrix)


def load_feats(ctx, key):
    path = ctx.path("feats", key + ".npz")
    if not os.path.exists(path):
        return {}
    with np.load(path) as z:
        return {str(i): f for i, f in zip(z["ids"], z["feats"])}


def input_files(ctx):
    """input/ 의 키 → 파일 이름 목록"""
    found = {}
    if os.path.isdir(ctx.input_dir):
        for name in os.listdir(ctx.input_dir):
            if name.lower().endswith(EXTS):
                found.setdefault(os.path.splitext(name)[0], []).append(name)
    return found


def user_owned(ctx, key, st, files):
    """input/ 에 이 키의 사진이 있는데 이 스크립트가 둔 것이 아니면 직접 넣은 사진이다"""
    names = files.get(key, [])
    if not names:
        return False
    if st and st.get("placed_sha") and names == [key + ".jpg"]:
        return file_sha1(os.path.join(ctx.input_dir, key + ".jpg")) != st["placed_sha"]
    return True


def original_file(ctx, key, cid):
    folder = ctx.path("cands", key)
    if os.path.isdir(folder):
        for name in os.listdir(folder):
            if os.path.splitext(name)[0] == cid:
                return os.path.join(folder, name)
    return None


def place(ctx, key, cand):
    """후보 원본 → input/{키}.jpg (JPEG는 다시 압축하지 않고 복사). 원본을 지웠으면 다시 받는다"""
    src = ctx.path("cands", key, f"{cand['id']}.{cand['ext']}")
    if not os.path.exists(src):
        data = download(cand["url"])
        if data is None:
            return None
        os.makedirs(os.path.dirname(src), exist_ok=True)
        with open(src, "wb") as f:
            f.write(data)
    os.makedirs(ctx.input_dir, exist_ok=True)
    dst = os.path.join(ctx.input_dir, key + ".jpg")
    if cand["ext"] == "jpg":
        shutil.copyfile(src, dst)
    else:
        with Image.open(src) as img:
            ImageOps.exif_transpose(img).convert("RGB").save(dst, "JPEG", quality=95)
    return file_sha1(dst)


def unplace(ctx, key, st):
    """이 스크립트가 둔 input 사진만 지운다"""
    path = os.path.join(ctx.input_dir, key + ".jpg")
    if st and st.get("placed_sha") and os.path.exists(path) and file_sha1(path) == st["placed_sha"]:
        os.remove(path)
    if st:
        st["placed_sha"] = None


def target_flags(t, st, confident, mixed):
    """검수 화면 '확인 필요' 사유"""
    flags = []
    best = picked(st)
    if best is None:
        flags.append("후보 없음")
    else:
        flags += [n for n in best.get("flags", []) if n.startswith(SERIOUS)]
        if not best.get("date") or int(best["date"][:4]) != t.year:
            flags.append("연도 미확인")
    if not confident:
        flags.append("신원 미확인")
    if mixed:
        flags.append("두 사람 섞임")
    if t.homonym:
        flags.append("동명이인")
    return flags


# ---------------------------------------------------------------- 수집

def run_group(ctx, search, group, pending):
    """선수 한 명의 여러 연도를 한꺼번에 처리한다. 신원은 모든 연도의 얼굴을 모아야 정확하다"""
    states = {t.key: load_state(ctx, t.key) for t in group}
    feats = {t.key: load_feats(ctx, t.key) for t in group}
    for t in pending:
        old = states[t.key]
        tried = set()
        mode = "dated" if t.dated else "archive"
        records, found = gather(ctx, t, search_pool(t, search, mode, tried), 0, tried)
        if len(usable(records)) < 2:
            more, extra = gather(ctx, t, search_pool(t, search, "loose", tried), len(records), tried)
            records += more
            found.update(extra)
        mark_duplicates(records)
        states[t.key] = {"key": t.key, "name": t.name, "team": t.team, "year": t.year, "order": t.order,
                         "status": "none", "pick": None, "manual": False, "flags": [],
                         "placed_sha": old.get("placed_sha") if old else None,
                         "cands": records, "tried": sorted(tried)}
        feats[t.key] = found
        save_feats(ctx, t.key, found)

    pairs, vectors = [], []
    for t in group:
        st = states[t.key]
        if st is None:
            continue
        live = {c["id"] for c in st["cands"] if "face" in c and c.get("reject") != "중복"}
        for cid, f in feats[t.key].items():
            if cid in live:
                pairs.append((t.key, cid))
                vectors.append(f)
    center, members, rival = identity_center(vectors)
    confident = center is not None and (members >= 3 or (members >= 2 and len(vectors) <= 4))
    mixed = confident and rival >= 3 and rival >= members * 0.6
    sims = {p: float(v @ center) for p, v in zip(pairs, vectors)} if center is not None else {}

    used = [c["hash"] for c in (picked(states[t.key]) for t in group if t not in pending) if c]
    results = []
    for t in pending:
        st = states[t.key]
        for c in st["cands"]:
            c["ident"] = round(sims[(t.key, c["id"])], 3) if (t.key, c["id"]) in sims else None
            rate(c, confident)
        best = choose(st["cands"], used)
        sha = place(ctx, t.key, best) if best else None
        if sha:
            st.update(status="picked", pick=best["id"], placed_sha=sha)
            used.append(best["hash"])
        else:
            unplace(ctx, t.key, st)
        st["flags"] = target_flags(t, st, confident, mixed)
        save_state(ctx, st)
        label = "없음" if st["status"] == "none" else ("확인" if st["flags"] else "고름")
        results.append(f"{t.year % 100:02d} {label}")
    return results


def load_api_keys():
    keys = {"naver_client_id": "", "naver_client_secret": "", "kakao_rest_api_key": ""}
    if os.path.exists(KEYS_FILE):
        with open(KEYS_FILE, encoding="utf-8") as f:
            keys.update({k: str(v).strip() for k, v in json.load(f).items() if k in keys})
    else:
        with open(KEYS_FILE, "w", encoding="utf-8") as f:
            json.dump(keys, f, indent=2)
    for field, env in (("naver_client_id", "NAVER_CLIENT_ID"), ("naver_client_secret", "NAVER_CLIENT_SECRET"),
                       ("kakao_rest_api_key", "KAKAO_REST_API_KEY")):
        if os.environ.get(env):
            keys[field] = os.environ[env].strip()
    return keys


def write_report(ctx, targets):
    """fetch/report.csv — 고른 사진의 출처 기록 (엑셀에서 열리게 BOM)"""
    files = input_files(ctx)
    counts = {"고름": 0, "확인 필요": 0, "후보 없음": 0, "직접 넣은 사진": 0, "미처리": 0}
    os.makedirs(ctx.work_dir, exist_ok=True)
    with open(ctx.path("report.csv"), "w", encoding="utf-8-sig", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["photo", "이름", "팀", "연도", "상태", "확인 사유", "사진 주소", "기사", "제목"])
        for t in targets:
            st = load_state(ctx, t.key)
            if user_owned(ctx, t.key, st, files):
                state, flags = "직접 넣은 사진", []
            elif st is None:
                state, flags = "미처리", []
            else:
                state, flags = STATUS_LABEL[st["status"]], st.get("flags", [])
            counts["확인 필요" if state == "고름" and flags else state] += 1
            best = picked(st) if state == "고름" else None
            writer.writerow([t.key, t.name, t.team, t.year, state + (" (직접)" if st and st.get("manual") else ""),
                             " / ".join(flags), best["url"] if best else "", best.get("page", "") if best else "",
                             best.get("title", "") if best else ""])
    return counts


def cmd_fetch(ctx, args):
    search = ImageSearch(load_api_keys(), ctx.path("cache"))
    if not search.sources:
        sys.exit(NO_KEYS_HELP)

    targets, keys = build_targets(card_rows(ctx.data_dir))
    filled = write_keys(ctx.data_dir, keys)
    print(f"인물×연도 {len(targets)}명" + (f" · CSV photo 열에 키 {filled}개를 적었습니다" if filled else ""))

    wanted = set(args.only or [])
    files = input_files(ctx)
    groups = {}
    for t in targets:
        groups.setdefault(t.name, []).append(t)
    work = []
    for name, group in groups.items():
        pending = []
        for t in group:
            if wanted and not wanted & {t.name, t.search_name, t.key}:
                continue
            st = load_state(ctx, t.key)
            if user_owned(ctx, t.key, st, files):
                continue
            if args.force or wanted or st is None:
                pending.append(t)
        if pending:
            work.append((name, group, pending))
    if args.limit:
        work = work[:args.limit]

    print(f"이번에 처리: 선수 {len(work)}명 · 사진 {sum(len(p) for _, _, p in work)}장 · 검색 {'+'.join(search.sources)}")
    if work:
        ctx.detector = FaceDetector()
        ctx.identity = FaceIdentity()
        started = time.time()
        for i, (name, group, pending) in enumerate(work, 1):
            began = time.time()
            try:
                results = run_group(ctx, search, group, pending)
            except QuotaExceeded as error:
                print(f"\n[멈춤] {error}. 내일 다시 실행하면 이어서 합니다")
                break
            except SearchError as error:
                print(f"\n[중단] {error}")
                break
            eta = (time.time() - started) / i * (len(work) - i)
            print(f"  {i}/{len(work)}  {name}  {' · '.join(results)}   "
                  f"{time.time() - began:.0f}s  남은 시간 {eta / 60:.0f}분")

    counts = write_report(ctx, targets)
    print("\n" + " · ".join(f"{k} {v}" for k, v in counts.items()) + f"   (검색 API {search.calls}회)")
    print(f"출처 기록: {ctx.path('report.csv')}")
    print("다음: python fetch_photos.py review  →  python build_photos.py")


def cmd_keys(ctx, args):
    targets, keys = build_targets(card_rows(ctx.data_dir))
    filled = write_keys(ctx.data_dir, keys)
    print(f"인물×연도 {len(targets)}명 · photo 열에 새로 적은 칸 {filled}개")


def cmd_clean(ctx, args):
    freed = 0
    for st in all_states(ctx):
        for c in st["cands"]:
            if c["id"] == st.get("pick"):
                continue
            path = original_file(ctx, st["key"], c["id"])
            if path:
                freed += os.path.getsize(path)
                os.remove(path)
    print(f"안 고른 후보 원본 {freed / 1e6:.0f}MB를 지웠습니다. 썸네일은 남아 있어 검수 화면에서 다시 고를 수 있습니다")


# ---------------------------------------------------------------- 검수 화면

REVIEW_STYLE = """
:root{--bg:#07111f;--panel:#0b1a2e;--fg:#eef3fa;--dim:#8fa0b8;--line:#1e3b60;--gold:#e8b54a;--warn:#ffb23f;--bad:#ff5b4a}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--fg);font:14px/1.45 system-ui,"Malgun Gothic",sans-serif}
header{position:sticky;top:0;z-index:5;background:#07111fee;padding:12px 20px;border-bottom:1px solid var(--line);
display:flex;gap:14px;align-items:center;flex-wrap:wrap}
h1{margin:0;font-size:18px}.sum{color:var(--dim)}.sum b{color:var(--fg);margin:0 8px 0 3px}.sum .w{color:var(--warn)}.sum .x{color:var(--bad)}
header select,header input[type=search]{color:var(--fg);background:#11233d;border:1px solid var(--line);border-radius:6px;padding:4px 8px}
.tip{color:var(--dim);font-size:12px}
main{padding:4px 20px 80px}
section{display:grid;grid-template-columns:190px 1fr;gap:14px;padding:12px 0;border-bottom:1px solid var(--line)}
.info b{font-size:16px}.info span{color:var(--dim);margin-left:6px}
.info code{display:block;color:var(--dim);font-size:12px;margin:2px 0 6px;word-break:break-all}
.info em{display:block;font-style:normal;font-size:12px;color:var(--warn)}
section.none .info b{color:var(--bad)}section.done .info b::after{content:" · 직접 고름";color:var(--gold);font-size:12px}
.info button{margin-top:8px;background:#11233d;color:var(--fg);border:1px solid var(--line);border-radius:6px;padding:3px 10px;cursor:pointer}
.cands{display:flex;gap:8px;overflow-x:auto;padding-bottom:6px;align-items:flex-start}
figure{margin:0;flex:none;min-width:150px;border:3px solid transparent;border-radius:8px;overflow:hidden;cursor:pointer;
background:var(--panel);text-align:center}
figure.on{border-color:var(--gold)}figure.x{opacity:.4}figure:hover{border-color:#4f7fb8}figure.on:hover{border-color:var(--gold)}
.ph{position:relative;display:inline-block;vertical-align:top}.ph img{height:210px;display:block}
.ph i{position:absolute;border:1.5px solid #4fd1c5;pointer-events:none}
figcaption{contain:inline-size;text-align:left;font-size:11px;color:var(--dim);padding:3px 6px}
figcaption span{color:var(--warn)}figcaption a{color:#7fb0ff;margin-right:6px}
.hide{display:none!important}
"""

REVIEW_SCRIPT = """
async function post(url, body){
  const r = await fetch(url, {method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(body)});
  return r.json();
}
document.addEventListener('click', async e => {
  if (e.target.closest('a')) return;
  const row = e.target.closest('section');
  const fig = e.target.closest('figure.c');
  if (fig && row) {
    fig.style.opacity = .5;
    const r = await post('/api/pick', {key: row.dataset.key, id: fig.dataset.id});
    fig.style.opacity = '';
    if (!r.ok) { alert(r.msg); return; }
    row.querySelectorAll('figure.c').forEach(f => f.classList.toggle('on', f === fig));
    row.classList.remove('none'); row.classList.add('done');
  } else if (row && e.target.closest('button.none')) {
    const r = await post('/api/none', {key: row.dataset.key});
    if (!r.ok) { alert(r.msg); return; }
    row.querySelectorAll('figure.c').forEach(f => f.classList.remove('on'));
    row.classList.add('none', 'done');
  }
});
function filter(){
  const only = document.getElementById('only').checked;
  const team = document.getElementById('team').value;
  const text = document.getElementById('find').value.trim().toLowerCase();
  document.querySelectorAll('section').forEach(s => s.classList.toggle('hide',
    (only && s.dataset.flag !== '1') || (team && s.dataset.team !== team) ||
    (text && !s.dataset.name.includes(text) && !s.dataset.key.includes(text))));
}
['only', 'team', 'find'].forEach(id => document.getElementById(id).addEventListener('input', filter));
"""


def review_section(ctx, st, files):
    key, pick = st["key"], st.get("pick")
    figures = []
    for c in sorted(st["cands"], key=lambda c: (c["id"] != pick, bool(c.get("reject")), -c.get("score", -99))):
        if "face" not in c or not os.path.exists(ctx.path("thumbs", key, c["id"] + ".jpg")):
            continue
        cls = "c on" if c["id"] == pick else ("c x" if c.get("reject") else "c")
        box = c.get("box")
        mark = f'<i style="left:{box[0]:.1%};top:{box[1]:.1%};width:{box[2]:.1%};height:{box[3]:.1%}"></i>' if box else ""
        bits = [c["date"][2:] if c.get("date") else "날짜 ?", f'얼굴 {c["face"]}px']
        if c.get("ident") is not None:
            bits.append(f'신원 {c["ident"]:.2f}')
        note = c.get("reject") or " · ".join(c.get("flags", []))
        links = f'<a href="/full/{key}/{c["id"]}" target="_blank">원본</a>'
        if c.get("page"):
            links += f'<a href="{html.escape(c["page"])}" target="_blank" rel="noreferrer">기사</a>'
        figures.append(
            f'<figure class="{cls}" data-id="{c["id"]}" title="{html.escape(c.get("title") or c.get("site") or "")}">'
            f'<div class="ph"><img loading="lazy" src="/thumb/{key}/{c["id"]}" alt="">{mark}</div>'
            f'<figcaption>{" · ".join(bits)}<br>{links}<span>{html.escape(note)}</span></figcaption></figure>')

    flags = list(st.get("flags", []))
    if user_owned(ctx, key, st, files):
        flags.insert(0, "input에 직접 넣은 사진이 있어 바꾸지 않음")
    status = " ".join(s for s in ("none" if st["status"] == "none" else "", "done" if st.get("manual") else "") if s)
    return (f'<section class="{status}" data-key="{key}" data-team="{html.escape(st["team"])}" '
            f'data-name="{html.escape(st["name"].lower())}" data-flag="{1 if flags else 0}">'
            f'<div class="info"><b>{html.escape(st["name"])}</b><span>{html.escape(st["team"])} · {st["year"]}</span>'
            f'<code>{key}</code><em>{html.escape(" · ".join(flags))}</em>'
            f'<button class="none">맞는 사진 없음</button></div>'
            f'<div class="cands">{"".join(figures) or "<p>받은 후보가 없습니다</p>"}</div></section>')


def review_page(ctx):
    files = input_files(ctx)
    states = all_states(ctx)
    states.sort(key=lambda st: (0 if st["status"] == "none" else 1 if st.get("flags") else 2, st.get("order", 0)))
    teams = "".join(f"<option>{html.escape(t)}</option>" for t in sorted({st["team"] for st in states}))
    none = sum(st["status"] == "none" for st in states)
    check = sum(st["status"] != "none" and bool(st.get("flags")) for st in states)
    return ('<!doctype html><html lang="ko"><head><meta charset="utf-8">'
            '<meta name="viewport" content="width=device-width,initial-scale=1">'
            f'<title>사진 후보 검수</title><style>{REVIEW_STYLE}</style></head><body><header>'
            '<h1>사진 후보 검수</h1>'
            f'<div class="sum">전체<b>{len(states)}</b>확인 필요<b class="w">{check}</b>후보 없음<b class="x">{none}</b></div>'
            '<label><input type="checkbox" id="only"> 확인 필요만</label>'
            f'<select id="team"><option value="">전체 팀</option>{teams}</select>'
            '<input type="search" id="find" placeholder="이름 · 키">'
            '<span class="tip">사진을 누르면 그 사진으로 바뀝니다 · 금색 = 고른 사진 · 청록 상자 = 잡은 얼굴</span>'
            f'</header><main>{"".join(review_section(ctx, st, files) for st in states)}</main>'
            f'<script>{REVIEW_SCRIPT}</script></body></html>')


def apply_pick(ctx, key, cid):
    with ctx.lock:
        st = load_state(ctx, key)
        cand = next((c for c in st["cands"] if c["id"] == cid), None) if st else None
        if cand is None:
            return False, "후보를 찾지 못했습니다"
        if user_owned(ctx, key, st, input_files(ctx)):
            return False, "input에 직접 넣은 사진이 있어 바꾸지 않습니다"
        sha = place(ctx, key, cand)
        if sha is None:
            return False, "원본을 다시 받지 못했습니다 (기사에서 지워졌을 수 있음)"
        st.update(status="picked", pick=cid, manual=True, placed_sha=sha)
        save_state(ctx, st)
    return True, ""


def apply_none(ctx, key):
    with ctx.lock:
        st = load_state(ctx, key)
        if st is None:
            return False, "기록이 없습니다"
        unplace(ctx, key, st)
        st.update(status="none", pick=None, manual=True)
        save_state(ctx, st)
    return True, ""


def review_handler(ctx):
    class Handler(BaseHTTPRequestHandler):
        def log_message(self, *args):
            pass

        def _send(self, code, body, kind):
            self.send_response(code)
            self.send_header("Content-Type", kind)
            self.send_header("Content-Length", str(len(body)))
            self.send_header("Cache-Control", "no-store")
            self.end_headers()
            self.wfile.write(body)

        def do_GET(self):
            path = urllib.parse.urlparse(self.path).path
            if path == "/":
                return self._send(200, review_page(ctx).encode("utf-8"), "text/html; charset=utf-8")
            m = re.fullmatch(r"/(thumb|full)/([a-z0-9_]+)/(\d{2,3})", path)
            if m:
                kind, key, cid = m.groups()
                file = original_file(ctx, key, cid) if kind == "full" else ctx.path("thumbs", key, cid + ".jpg")
                if file and os.path.exists(file):
                    with open(file, "rb") as f:
                        data = f.read()
                    return self._send(200, data, MIME.get(os.path.splitext(file)[1], "application/octet-stream"))
            self._send(404, b"not found", "text/plain")

        def do_POST(self):
            path = urllib.parse.urlparse(self.path).path
            try:
                body = json.loads(self.rfile.read(int(self.headers.get("Content-Length", 0))) or b"{}")
            except ValueError:
                body = {}
            key, cid = str(body.get("key", "")), str(body.get("id", ""))
            if not KEY_RULE.match(key):
                ok, msg = False, "잘못된 키"
            elif path == "/api/pick":
                ok, msg = apply_pick(ctx, key, cid)
            elif path == "/api/none":
                ok, msg = apply_none(ctx, key)
            else:
                ok, msg = False, "없는 주소"
            self._send(200, json.dumps({"ok": ok, "msg": msg}, ensure_ascii=False).encode("utf-8"),
                       "application/json; charset=utf-8")

    return Handler


def cmd_review(ctx, args):
    if not all_states(ctx):
        sys.exit("[중단] 아직 수집한 기록이 없습니다. 먼저 python fetch_photos.py 를 실행하세요")
    server = ThreadingHTTPServer(("127.0.0.1", args.port), review_handler(ctx))
    url = f"http://127.0.0.1:{args.port}/"
    print(f"검수 화면: {url}   (끝내려면 Ctrl+C)")
    if not args.no_browser:
        webbrowser.open(url)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\n검수를 마쳤습니다. 다음: python build_photos.py")
    finally:
        server.server_close()


def parse_args():
    parser = argparse.ArgumentParser(description="선수 사진 자동 수집 (뉴스 경기 사진 → input/{photo 키}.jpg)")
    parser.add_argument("command", nargs="?", default="fetch", choices=("fetch", "review", "keys", "clean"))
    parser.add_argument("--only", nargs="+", metavar="이름|키", help="지정한 선수만 다시 고른다")
    parser.add_argument("--limit", type=int, default=0, help="앞에서 N명만 (시험용)")
    parser.add_argument("--force", action="store_true", help="이미 고른 선수도 전부 다시 고른다")
    parser.add_argument("--per", type=int, default=PER_TARGET, help=f"선수×연도당 받아 볼 후보 수 (기본 {PER_TARGET})")
    parser.add_argument("--port", type=int, default=8770)
    parser.add_argument("--no-browser", action="store_true", help="검수 화면을 열 때 브라우저를 띄우지 않는다")
    parser.add_argument("--data", default=os.path.join(PROJECT, "Assets", "Resources", "Data"))
    parser.add_argument("--input", default=os.path.join(ROOT, "input"))
    parser.add_argument("--work", default=os.path.join(ROOT, "fetch"))
    return parser.parse_args()


def main():
    sys.stdout.reconfigure(errors="replace")
    args = parse_args()
    ctx = Context(args)
    {"fetch": cmd_fetch, "review": cmd_review, "keys": cmd_keys, "clean": cmd_clean}[args.command](ctx, args)


if __name__ == "__main__":
    main()
