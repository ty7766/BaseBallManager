# PROGRESS.md — 베이스볼 매니저 진행상황

> **읽는 순서**: 1장(현재 위치) → 2장(다음 할 일) → 필요 시 3~6장(규칙·결정·수치)
> 7장은 과거 이력 요약이므로 맥락이 필요할 때만 본다.
> 세션 1~43의 상세 기록은 git 히스토리(`PROGRESS.md` 이전 버전)에 남아 있다.

---

## 1. 현재 위치

| 로드맵 | 상태 |
|---|---|
| 1 데이터 레이어 / 2 인벤토리 / 3 뽑기 / 4 강화 / 5 훈련·돌파 | ✅ |
| 6 경기 시뮬레이션 엔진 / 7 인터럽트·수동 교체 | ✅ |
| 8 리그 시스템 (일정·순위표·진행·포스트시즌·저장) | ✅ |
| 10 골든글러브 제작 / 재화 / 분해 / 카드 조합 | ✅ |
| **9 경기 UI** | ✅ **구현 완료** (세션 52) |
| **UI 전체 (로비 · 수급 · 육성 · 가을야구)** | ✅ **구현 완료** (세션 53) — 화면 19장. 2-7 참고 |
| **11 튜토리얼** | ⏭️ 미착수 (최종 작업). 새 게임 → 뽑기권 지급 → 안내 토스트까지는 흐름에 들어가 있다 |

**현재 트랙: 전체 코드 검수·리팩토링** (세션 41 착수, 브랜치 `refactoring/Code-Structure-Refactoring`)

작성 순서(git 커밋 기준)대로 `.cs` 한 개씩 검수한다.
**세션 46부터 Claude가 검수 → 수정 적용 → 커밋까지 직접 수행한다.** (세션 45까지는 diff 제시만 하고 작성자가 반영)
시그니처 변경으로 깨지는 호출부도 **미루지 않고 그 자리에서 함께 고친다.**

| # | 파일 | 상태 |
|---|---|---|
| 1~6 | `CardType` · `CardGrade` · `CardMasterData` · `HitterMasterData` · `PitcherMasterData` · `CardCSVLoader` | ✅ |
| 7~10 | `CardInstance` · `CardDataManager` · `PlayerTypeFilter` · `CardFilter` | ✅ |
| 11 | `Inventory/InventoryManager` | ✅ (List → Dictionary 전환) |
| 12~14 | `GachaType` · `GachaResult` · `GachaManager` | ✅ |
| 15 | `Player/PlayerDataManager` | ✅ |
| 16 | `Enhance/EnhanceManager` | ✅ |
| 17~18 | `Train/TrainManager` · `Train/BreakthroughManager` | ✅ |
| 19~22 | `Line Up/` 4개 (포지션 enum·파서 · `LineUpManager` · `LineUpAutoFill`) | ✅ |
| 23~37 | `Simulation/` 15개 | ✅ |
| 38~43 | `ProbabilityModels/` 6개 (+ `RunnerLookup` 신규) | ✅ |
| 44~46 | `Builders/` 2개 · `Cards/CardStatsCalculator` | ✅ |
| 47~51 | `Record/` 5개 | ✅ |
| 52~72 | `League/` 21개 | ✅ (구조 수정 + 주석 규칙) |
| 73~77 | `Player/` · `Save/` 5개 | ✅ |
| 78~82 | `Currency/` · `Dismantle` · `Combine` · `GoldenGlove` · `Core` | ✅ (싱글톤 전환 + 주석 규칙) |

**전 파일 정밀 검수 완료 (1·2회차).**

**세션 48: 3회차 전수 검수 — 결함 9종을 하니스로 실측 확인한 뒤 전부 수정.** 검증 50건 전건 통과.
남은 미해결은 아래 2-1의 🔴 `StatBaseline` 드리프트 하나뿐이다 (코드가 아니라 데이터 문제).

---

## 2. 다음 할 일

### 2-0-A. 🔜 다음 세션 시작점 (세션 54 종료 시점)

**브랜치 `feature/game-ui` · 워킹 트리 깨끗 · 커밋 5개 (develop 기준)**
`dc10ad6` 세로 고정 → `e2a3adc` 디자인 v1 → `d27070b` 경기 UI → `5f5d7ec` 실제 로고 방침 →
`04fa7f0` UI 전체 19화면 → `ce5f874` 연출

#### ① 가장 먼저 — Unity ▶ 로 눈으로 확인 (작성자)

Claude 세션에는 Unity MCP가 없어 **화면을 본 검증만 빠져 있다.** 로직·컴파일·데이터 경로는 PASS 101/0.

1. Unity를 연다 → `Assets/Resources/UI` · `Assets/TextMesh Pro` · `Assets/Resources/UI/Fx` 임포트가 돌아간다
2. `Assets/Scenes/SampleScene` 에서 ▶ — 루트 `UI` 오브젝트(`GameUiBootstrap`)가 캔버스를 만든다
3. 정상 콘솔 로그: `'맑은 고딕' 폰트로 UI 글자를 그립니다` / `'UI/Team/logo_기아' 스프라이트를 찾지 못했습니다`(로고 미납품)
4. 순회: 타이틀 → 새 게임 → 팀 선택 → 로비 → **빠른 시작** → 리그 입장 → 경기 전 → 경기 중(관전) → 경기 후 → 로비
5. 어긋난 좌표·과한 연출을 적어서 알려주면 수치만 고친다 (연출 세기는 `UiParticleSettings.Get` 프리셋)

#### ② 🔒 작성자 작업 3가지 + 보유분 1가지

| 넣을 곳 | 없을 때 |
|---|---|
| `Assets/Resources/UI/Team/logo_<팀명>.png` 10종 | 팀 컬러 + 이니셜로 대체 |
| `Assets/Resources/Data/*.csv` 시그·골글 추가 | 시그 뽑기 빈 결과 · 골글 제작 실패 유지 |
| `Assets/Resources/Photos/<photoKey>.png` 768×960 | 카드 상세의 사진 자리만 빔 |
| `Assets/Resources/UI/Card/card_base_normal` · `_signature` · `_goldenglove` (보유분) | 팀색 반투명 패널 |

#### ③ 그다음 코드 작업 후보 (우선순위 순)

1. **화면 좌표 보정** — ①의 피드백 반영. 가장 먼저 올 작업
2. **A3 `StatBaseline` 재측정** — CSV 시그·골글이 들어오면 `Tools/BaseBallManager/StatBaseline 드리프트 측정` 실행 후 상수 조정 (2-1)
3. **로드맵 11 튜토리얼** — 새 게임 직후 안내 흐름. 현재는 토스트 1줄뿐
4. 가을야구 사다리 대진표 그래픽 · 인벤토리 정렬/일괄 분해 (요청서 2장의 디자인 요청 목록)

#### ④ 검증 하니스 복구

스크래치패드는 세션마다 새로 만들어지므로 하니스를 복사해 와야 한다. 3-3 참고.
`UnityShim.cs` + `UnityUiShim.cs` + `harness.csproj` + `apicheck/` + `Program.cs~Program3.cs` 구성이고,
`dotnet build` → `dotnet run -c Release` 로 PASS 101 / FAIL 0 이 나와야 한다.

---

### 2-0-B. 🔬 Unity MCP 실행 QA (세션 55)

**Unity MCP가 붙었다.** 이제 Claude가 ▶ 실행 · 콘솔 · 스크린샷 · 메뉴 실행까지 직접 한다.
연결 조건은 **Unity Editor 가 떠 있는 상태**에서 Claude Code 를 시작하는 것뿐이다
(`~/.claude.json` 의 `UnityMCP: http://127.0.0.1:8080/mcp`). Editor 가 꺼져 있으면 8080 이 안 열려 툴이 안 붙는다.

#### 확인한 것 (실제 재생 화면)

| 항목 | 결과 |
|---|---|
| 전체 흐름 | 타이틀 → 팀 선택 → 로비 → 빠른 시작 → 리그 → 경기 전 → 경기 중 → 경기 후 → 리그 **완주** |
| 이어하기 | 팀 · 카드 100장 · 라인업 · 리그 진행도(1/9) **전부 복원** |
| 경기 중 | 스코어보드 · 타자/투수 칩 · 체력바 · 중계 로그 색 4단계 · `+N점` 배지 · 도루 로그 · **파티클 연출** 동작 |
| 경기 후 | FINAL/LOSE · 라인스코어(R 금색) · 타자/투수 · AWAY/HOME 탭 · 이름 열 고정 |
| 순위표 | 1경기 반영 정확 (키움 1승 · 기아 0승1패 9위 · 게임차 1.0) |
| 카드 | **베이스 3종 임포트 후 첫 렌더 성공** — OVR · 포지션 · 별 · 이름 띠가 6-6 좌표대로 |
| 한글 | `Malgun Gothic` 런타임 TMP 폰트로 정상 출력 |

#### 고친 것 — 7건

1회차(재생): 로비 '빠른 시작' 라벨 폭 초과 · 순위표 헤더 '선수'→'팀' · 엠블럼 이니셜이 팀명 전체라 중복 ·
`MY TEAM` 태그가 팀명과 겹침 · 체력바 수치가 투수 칩과 겹침
2회차(미리보기): 빈 그리드 안내 문구가 처음에 안 뜸 · 골글 제작 비용 한 줄이라 화면 밖으로 잘림

#### 🔴 QA 파이프라인 규칙 (재발견 방지)

- **`refresh_unity(compile)` 직후 바로 ▶ 금지.** 도메인 리로드가 Awake 뒤에 겹쳐 싱글톤 static 을 날린다.
  매니저 `Instance` 가 전부 null 로 보이면 게임 버그가 아니라 이것이다. 정지 후 다시 재생하면 복구된다
- **`execute_code` 로 `onClick.Invoke()` 하면 `PlayerLoop called recursively` 가 쌓인다.**
  누적되면 재생이 멈추고 캡처가 흰 화면으로 나온다. 클릭 뒤에는 1~2초 쉬고 캡처하며, 흰 화면이면 상태부터 확인한다
- **버튼 이름은 화면끼리 겹친다** (`ConfirmButton` · `StartButton` 등). 반드시 `activeInHierarchy` 로 거른다
- **`GameObject.Find` 는 비활성 오브젝트를 못 찾는다.** 꺼둔 캔버스를 다시 켜려면 `GetRootGameObjects()` 를 쓴다
- **Editor 비포커스면 Update 가 안 돈다** → 트윈이 시작값에서 멈춘다 (alpha 0 · scale 0.82).
  캡처 전에 `Application.runInBackground = true` 를 켠다. 게임 버그가 아니다
- **에디트 모드 Game 뷰는 렌더되지 않고, Scene 뷰는 Overlay 캔버스 내용을 캡처에 담지 않는다.**
  `UiPreviewBuilder` 미리보기는 **재생 중에** 띄워야 보인다 (게임 캔버스는 꺼둔다)
- TMP 컴포넌트는 `get_components` 로 덤프하지 않는다 (textInfo 가 거대하다). 스크린샷으로 본다

#### Game 뷰 해상도

현재 376×794 라 1080 설계 대비 1/3 로 축소된다. 얇은 한글 획이 뭉개져 **가독성 문제처럼 보이지만 뷰 크기 탓**이다.
레이아웃 판정은 비율로 하고, 글자 굵기는 실기기/큰 Game 뷰에서 다시 본다.

---

### 2-0. 남은 일 한눈에 (세션 48 기준)

**코드 결함은 전부 닫혔다.** 남은 것은 데이터 입력 · 인스펙터 수치 · 미착수 기능뿐이다.

#### A. 데이터 — 다른 일의 선행 조건

| # | 항목 | 담당 | 선행 | 상태 |
|---|---|---|---|---|
| A1 | **시그니쳐·골든글러브 마스터 CSV 입력** | 🔒 작성자 | — | 🔧 작성 중 |
| A2 | `StartingSignatureTable._entries` 10팀 지정 | 🔒 작성자 | A1 | ⏭️ |
| A3 | `StatBaseline` 재측정 후 조정 판단 | Claude (하니스) | A1 | ⏭️ |
| ~~A4~~ | ~~포스트시즌 보상 5칸~~ | 🔒 작성자 | — | ✅ 입력 완료 (세션 48) |
| ~~A5~~ | ~~`Minor1~Legend3` `_statBonus`~~ | 🔒 작성자 | — | ✅ 10→30 단조 증가로 입력 (세션 48) |
| ~~A6~~ | ~~골글 제작 비용 코드 기본값 정렬~~ | Claude | — | ✅ 씬 값과 일치 (세션 48) |

> **A1 규모 가이드** (세션 48 역산): 5성 평균으로 들어온다고 가정하면
> **타자 약 95~100장 / 투수 약 50~55장**에서 현재 `StatBaseline` 상수에 자연 수렴한다.
> 이 범위면 A3는 "확인만 하고 끝"이 되고, 벗어나면 상수를 실측값으로 맞춘다.
> A1은 시그 뽑기 빈 결과 2.94%와 골글 제작 항상 실패도 함께 해소한다.

#### B. 인스펙터 확인 — ✅ 세션 48에서 스크립트 검증으로 전부 닫음

`.meta` 6개 · 매니저 15종 참조 · 개명으로 풀린 필드 8종의 실제 적용값까지 확인했다. 2-3 상단 참고.

#### C. 코드 — 착수 대기

| # | 항목 | 비고 |
|---|---|---|
| ~~C1~~ | ~~로드맵 **9번 경기 UI**~~ | ✅ 세션 52 구현 완료 (2-6) |
| ~~C2~~ | ~~`LiveGameController` 씬 배치 + `SetInterruptHandler` 연결~~ | ✅ `GameUiBootstrap`이 런타임에 만들어 주입 |
| C3 | 무승부 상대전적 표시 판단 | 리그 순위표에 무승부 열을 넣었으므로 상대전적에도 넣을지만 남았다 |
| C4 | 로드맵 **11번 튜토리얼** | 최종 |

#### D. 재량 — 안 해도 굴러가는 것

2-5 목록. 다시 꺼내지 않으며 필요해지는 시점에 판단한다.

#### E. 도구 — ✅ 완료 (세션 48)

`Tools/BaseBallManager/StatBaseline 드리프트 측정` — `Assets/Editor/StatBaselineValidator.cs`.
상수 vs 현재 CSV 평균 · 확률 영향 실측 · 상수 도달에 필요한 5성급 장수를 콘솔에 찍는다.
**확률은 튜닝 상수를 복제하지 않고 실제 `BatterOutcomeCalculator`를 20만 타석 돌려 잰다** —
현재 카드풀 카드와 "편차가 정확히 0인 가상 카드"를 같은 조건으로 붙여 차이만 보여준다.
허용 오차 ±0.5점을 넘으면 `LogWarning`, 아니면 `Log`.

---


### 2-1. 🔴 `StatBaseline`이 현재 카드풀과 어긋남 (= 2-0의 A3)

**밸런스 노브가 아니라 정규화 기준점이다.** `BaseStrikeOutProb` 같은 튜닝 상수는 플레이하며 느낌으로 잡는 값이지만,
`StatBaseline`은 "내 카드풀의 평균이 몇이냐"는 **측정하면 답이 나오는 사실값**이다.
이게 어긋나 있으면 노브가 거짓말을 한다 — `BaseHitProb 0.303`(타율 .270 목표)이 실제로는 .256을 낸다.
**기준점을 먼저 맞춘 뒤 노브를 튜닝해야** 돌린 만큼 결과가 움직인다. 순서를 뒤집으면 두 번 일한다.

`StatBaseline`은 "카드풀 평균"이어야 편차 정규화(`GetEdge`)가 의도대로 동작한다.
지금은 상수가 실제 평균보다 높게 잡혀 있어, **평균 타자 대 평균 투수 대결조차 기준 확률에서 벗어난다.**

| 상수 | 값 | 현재 CSV 실제 평균 | 차이 |
|---|---|---|---|
| `HitterPower` | 67 | 63.65 | **-3.35** |
| `HitterContact` | 61 | 58.36 | **-2.64** |
| `HitterRun` | 58 | 56.34 | -1.66 |
| `HitterDefense` | 66 | 64.94 | -1.06 |
| `PitcherStuff` | 68 | 66.08 | -1.92 |
| `PitcherControl` | 66 | 64.69 | -1.31 |
| `PitcherPower` | 72 | 70.24 | -1.76 |

**영향** — 평균 타자 대 평균 투수에서 공식이 내놓는 값 (세션 48 계산):
삼진율 +2.3% / 볼넷율 -4.1% / **홈런율 -8.6%** / BABIP -1.2% / **2루타 비중 -6.3%** / 3루타 비중 -8.9% / 추가 진루 -1.6%.
타자 쪽 드리프트(-3.35)가 투수 쪽(-1.76)보다 커서 상쇄되지 않고 공격 지표가 한 방향으로 눌린다.
1000경기 실측: 타율 .256(기준 .270) / 장타율 .363(.401) / 경기당 득점 7.37(8.53).
타자 쪽 드리프트가 투수 쪽보다 커서 공격 지표가 일관되게 낮다.

**지금 상수를 낮추면 안 되는 이유**: 현재 CSV는 타자 159 / 투수 166장으로
**시그니쳐·골든글러브가 아직 미입력**(2-3의 5번)이다. 이들은 5성 고정이라 스탯이 높아,
입력되면 풀 평균이 다시 올라간다. **시그·골글 CSV 입력 후 재측정하는 것이 맞다.**

**자동 계산으로 바꾸지 않는다.** 런타임에 카드풀에서 평균을 구하게 만들면 편해 보이지만,
**카드 한 장을 추가할 때마다 게임 전체 확률이 흔들려** 플레이테스트에서 잡은 감이 CSV 수정 한 번에 무효화된다.
고정 상수 + 마일스톤마다 수동 재동기화가 맞는 설계다.

측정 방법은 3-3의 검증 하니스(`StatBaseline이 카드풀 평균과 일치` 항목)가 수치까지 찍어준다.

### 2-2. 미처리 호출부 대기표

검수로 public 시그니처가 바뀌어 아직 안 고친 곳. **그 파일 검수 차례에 함께 처리한다.**

**현재 비어 있다.** 세션 46부터 파급 호출부를 즉시 고치므로 이 표는 원칙적으로 쌓이지 않는다.

세션 46에서 바뀐 public 시그니처(호출부 모두 동반 수정 완료):
`CurrencyManager.Restore` `void`→`bool` / `StealCalculator.IsSuccess`에서 `GameState` 매개변수 제거 /
`TrainManager.GetMaxTrainLevel()`→`MaxTrainLevel` 프로퍼티 / `Hittersubstitution`→`HitterSubstitution`

### 2-3. Unity 에디터 작업 (코드 아님)

> 세션 48에서 **인스펙터 확인 8건(옛 1·2·4·8·9·10·11·12)을 스크립트로 검증해 닫았다.**
> `.meta` 6개 전부 존재 / 매니저 15종 스크립트 참조 불일치 0건 /
> 개명으로 풀린 필드는 전부 코드 기본값이 기획서 확정치와 같아 실제 값이 맞다
> (천장 50 · 시그 확률 0.15 · 훈련 분배 2 · 돌파 비용 50/20/10/5/3 · 강화카드 분배 60/30).
> 씬에 남은 옛 이름(`_gradeSigProbabilitySig` · `_breakthroughCardCost_*`)은 무시되는 잔재이고,
> Unity가 씬을 한 번 저장하면 정리된다.

| # | 항목 | 비고 |
|---|---|---|
| 5 | 시그니쳐 · 골든글러브 마스터 CSV 입력 | 🔒 작성자. cardId **append-only** — 타자 `160~` / 투수 `50167~` |
| 6 | `StartingSignatureTable._entries` 채우기 | **5번이 끝나야 가능.** 현재 비어 있어 전 팀이 시작 카드 없이 출발 |
| 13 | 🔒 시그니쳐·골글 CSV 입력 후 `StatBaseline` 재측정 | 2-1 참고. 하니스가 수치를 찍어준다 |

### 2-4. 코드 쪽 대기

**로드맵 9번 착수 — `경기_UI_명세.md` 작성 완료 (세션 50).** 디자인은 Claude Desktop에서 진행한다.
아래는 명세대로 화면을 채우려면 **UI 코드보다 먼저** 뚫어야 하는 것들이다 (하니스로 확인한 실제 공백).

| # | 없는 것 | 필요한 화면 | 해결 방향 |
|---|---|---|---|
| U1 | `HitterSnapshot`·`PitcherSnapshot`에 **년도** | 전 화면의 선수 표기 | 스냅샷에 `Year` 추가. AI는 `AiRosterBuilder`, 플레이어는 `SimulationContextBuilder`가 마스터에서 채운다 |
| U2 | 스냅샷에 **OVR** | 전 화면 | 4스탯 평균이라 UI가 계산해도 되지만, 내림 규칙이 `CardStatsCalculator`에 있으므로 스냅샷에 담는 편이 안전 |
| U3 | 스냅샷에 **포지션** | 라인업 행의 포지션 표기 | U1과 같은 경로 |
| U4 | `SimulationContext`에 **팀명** | 경기 전·중 스코어보드 | 시뮬 코어는 팀을 몰라도 되므로 UI가 `LeagueGame`에서 받는 쪽이 맞다. 결정 필요 |
| U5 | **팀 로고 애셋 10종** | 경기 전 대진 | **실제 구단 로고 사용 확정** (세션 52, 개인 소장용 · 배포 안 함). 사진과 같이 커밋 제외 — 명세 3-3 |
| U6 | **교체 모드(자동/수동) 설정** | 경기 중 일시정지 버튼 노출 | `PlayerSaveData`에 필드 추가 + `LeagueManager.SetInterruptHandler` 연동 |
| U7 | **타석 진행 코루틴 드라이버** | 경기 중 자동 흐름 | `GameSession.StepAtBat()`을 일정 간격으로 돌리는 MonoBehaviour. `LiveGameController`도 아직 씬에 없다 |

**U1~U4 · U6 · U7 완료 (세션 50).** 남은 것은 U5(팀 로고 애셋 10종, 작성자)뿐이다.
- U1~U3 `HitterSnapshot`·`PitcherSnapshot`에 `Year`·`Position` 추가, `OVR`은 최종 4스탯 평균 프로퍼티
- U4 `LeagueManager.NextPlayerGame` — 팀명은 `LeagueGame`에서 받는다. 시뮬 코어는 팀을 계속 모른다
- U6 `SubstitutionMode`(Auto/Manual) 신설. `PlayerDataManager` 보유 + 세이브 왕복 +
  `LeagueManager.ActiveInterruptHandler`가 자동 모드에서 창구를 끊는다
- U7 `GamePlaybackDriver` — `StepAtBat()`을 일정 간격으로 돌리고 로그를 이벤트로 흘린다.
  일시정지는 예약이라 타석 경계에서만 실제로 멈춘다 (`IsPauseRequested` → `IsPaused`)
- 🔴 **선수 사진은 사용한다** (인물 × 연도 단위, 수천 장 — 세션 51 확정). 규격·파이프라인·매핑은 `경기_UI_명세.md` 3-2
  - `CardMasterData.PhotoKey` + CSV `photo` **선택 열** 신설. 열이 없거나 비어 있으면 사진 없음으로 돌아간다
  - 노말·시그·골글이 같은 인물이면 같은 사진을 쓰므로 **cardId가 아니라 인물 단위**로 매핑한다
  - 🔴 **저장소가 GitHub 공개**라 사진은 커밋하지 않는다. `.gitignore`로 `Assets/Resources/Photos/` 제외
  - **실제 선수 사진·실제 구단 로고를 쓴다** (세션 52 확정 — 개인 소장용 · 배포 안 함). 로고도 같은 규칙으로 커밋 제외
  - `PhotoPipeline/build_photos.py` — 경기·미디어 사진용으로 재작성 (세션 51). YuNet 눈·입 검출 + 2차 정밀 검출 → BiRefNet-portrait 누끼(출력 영역만) →
    관중 조각 제거 · 배경색 번짐 제거 → **768×960**. 이어하기 + `output/review.html` 검수 시트. CPU 장당 약 8초
  - 카드 베이스는 **1398×1957 3종**(노말·시그·골글). **노말 3·4·5성은 같은 베이스에 별 개수만 다르다**
  - 저장소는 공개 유지. 사진만 로컬 보관 (작성자 확정)

- U1~U3은 한 번에 처리한다. 스냅샷은 `readonly struct`라 필드 추가가 곧 생성자 변경이고, 호출부는 `AiRosterBuilder`·`SimulationContextBuilder` 두 곳뿐이다
- **U4는 판단이 필요하다.** 4-1의 "`Simulation/`은 `Cards/`를 모른다" 원칙과 "팀명을 컨텍스트에 넣는 편의" 중 선택


| 항목 | 상태 |
|---|---|
| 로드맵 9번 경기 UI | ✅ 세션 52 구현 완료 (2-6) |
| `LiveGameController` 씬 배치 + `LeagueManager.SetInterruptHandler` 호출 | ✅ `GameUiBootstrap` → `AppFlowController.StartLeague`에서 주입 |
| 무승부 상대전적 표시 (`TeamRecord._drawsAgainst`) | ⏸️ 순위표에는 무 열을 넣었다. 경기 전 화면 상대전적은 승·패만 표시 중 |
| 로드맵 11번 튜토리얼 | ⏭️ 최종 |

---

### 2-6. 경기 UI 구현 (세션 52 완료)

**플레이 모드에서 타이틀 → 팀 선택 → 리그 → 경기 관전 → 박스스코어 → 리그가 한 바퀴 돈다.**
씬 `SampleScene`에 루트 오브젝트 `UI`(`GameUiBootstrap`)를 배치해 두었으므로 ▶를 누르면 바로 시작된다.

| 계층 | 파일 | 역할 |
|---|---|---|
| 조립 | `UI/GameUiBootstrap.cs` | 캔버스(1080×1920 · Match=Width) · EventSystem(**새 입력 시스템**) · 드라이버 · 교체 창구 생성 |
| 흐름 | `UI/Flow/AppFlowController.cs` | 타이틀 → 팀 선택 → 리그 → 경기 4화면 전환, 드라이버 이벤트 구독 |
| 흐름 | `UI/Flow/RosterBootstrapper.cs` | 새 게임 직후 튜토리얼 보상 + 자동 10연차 + `LineUpAutoFill` |
| 코어 | `UI/Core/` 9개 | `UiPalette`(6-2 색 토큰) · `UiBuilder`(좌표 1:1 배치) · `UiSprites` · `UiFontProvider` · `UiScreen` · `UiRouter` · `PlayLogTextProvider` · `PlayerLabelFormat` · `LeagueTierNames` |
| 위젯 | `UI/Widgets/` 10개 | 라인업 행(96) · 타석 칩(120) · 스코어보드(264) · 베이스 · 체력 바 · 중계 로그 · 라인스코어 · 기록 표 · 팀 심볼 · 행 상태 |
| 화면 | `UI/Screens/` 7개 | 타이틀 · 팀 선택 · 리그 · 경기 전 · 경기 중 · 일시정지 · 경기 후 |

**설계 결정**

- **UI를 프리팹이 아니라 코드로 조립한다.** 명세 6장이 1080×1920 좌표를 그대로 주므로
  `UiBuilder.Place(x, y, w, h)`(왼쪽 위 원점)로 시안 값을 1:1로 옮긴다. 씬 병합 충돌도 없다
- **애셋은 `Assets/Resources/UI/`로 옮겼다.** 코드가 런타임에 `Resources.Load<Sprite>`로 집어야 하기 때문이다.
  `manifest.json`의 PPU·9-슬라이스 테두리는 `.meta`에 직접 써 넣어 Unity를 켜지 않고 반영했다
- **글꼴은 OS 폰트에서 런타임에 TMP 자산을 만든다** (`TMP_FontAsset.CreateFontAsset(familyName, ...)`).
  한글 글리프를 가진 TMP 폰트 자산이 프로젝트에 없어서다. Pretendard → 맑은 고딕 순으로 찾는다.
  `TMP Essential Resources`는 패키지에서 추출해 `Assets/TextMesh Pro/`에 넣었다
- **입력은 새 입력 시스템.** `ProjectSettings.activeInputHandler: 1`이라 `StandaloneInputModule`은 동작하지 않는다.
  `InputSystemUIInputModule`을 붙여야 버튼이 눌린다 (붙이면 기본 액션을 스스로 할당한다)
- **스프라이트가 없으면 단색 사각형으로 떨어진다.** 팀 로고 10종이 아직 없어도 화면이 깨지지 않는다
  (팀 컬러 판 + 이니셜로 대체. `Resources/UI/Team/logo_<팀명>`을 넣으면 자동으로 바뀐다)

**관전을 위해 손본 기존 코드**

| 파일 | 변경 | 이유 |
|---|---|---|
| `LeagueRunner` | `BuildNextPlayerGameContext` · `BeginPlayerGame` · `CommitPlayerGame` 신설, 하루 확정 로직을 `ApplyDayScores`로 공용화 | 기존 `SimulateNextGame`은 내 경기까지 한 번에 돌려버려 관전할 틈이 없다. 내 경기만 세션으로 열고, 끝난 뒤 같은 날 AI 4경기를 돌려 확정한다 |
| `LeagueManager` | 위 3종 노출 + `CurrentTier` | UI 진입점 |
| `GameSession` | `Context` 프로퍼티 | 라인업·벤치·투수진을 그려야 한다 |
| `GamePlaybackDriver` | `SetSecondsPerAtBat` | 런타임 생성이라 인스펙터 값을 못 쓴다 |
| 🔴 `LeagueManager.ActiveInterruptHandler` 제거 | 교체 모드 판정을 `LiveGameController`로 이전 | **버그**: 리그 시작 시점의 모드가 `LeagueRunner`에 박혀, 리그 중에 수동으로 바꿔도 일시정지가 먹지 않았다. 모드는 매 타석 확인하는 것이 맞다 |

**검증** — 하니스 PASS 57 / FAIL 0 (기존 35 + 포스트시즌 15 유지, 관전 경로 22 신설).
관전 22건은 실제 CSV로 새 게임 → 자동 뽑기 → 라인업 자동편성 → 일정 → 타석 단위 전진 → 하루 확정 →
순위표·박스스코어까지 돌려 확인했다. 컴파일은 `net9.0` + `netstandard2.1`(C# 9) 양쪽 통과.

**남은 것**

| 항목 | 담당 |
|---|---|
| 팀 로고 10종 (`Resources/UI/Team/logo_<팀명>.png`) | 🔒 작성자. **실제 구단 로고를 로컬에 넣는다** (명세 3-3 · 커밋 제외) |
| ~~카드 베이스 3종 · 인벤토리/뽑기/강화 화면~~ | ✅ 세션 53에서 전 화면 구현 (2-7). 카드 베이스 PNG만 넣으면 카드가 완성된다 |
| ~~포스트시즌 화면~~ | ✅ 세션 53 `PostSeasonScreen` |
| 실기기 폰트 | OS 폰트 의존이라 모바일 빌드 전에 Pretendard TTF를 프로젝트에 넣어야 한다 |

---

### 2-7. UI 전체 구현 (세션 53 완료)

**앱 실행 → 타이틀 → 팀 선택 → 로비 → (뽑기 · 라인업 · 육성) → 리그 → 경기 관전 → 가을야구까지 전부 화면이 있다.**
화면 19장 · 위젯 16종 · 코어 12종. 전부 `UiBuilder` 좌표 조립 방식이고 프리팹은 쓰지 않는다.

| 분류 | 화면 | 데이터 출처 |
|---|---|---|
| 시작 | `TitleScreen` · `TeamSelectScreen` | `GameFlowManager` |
| 허브 | `LobbyScreen` (메뉴 11칸 + 재화 줄) | `PlayerDataManager` · `CurrencyManager` · `LineUpManager` |
| 리그 | `LeagueScreen` · `PostSeasonScreen` | `LeagueManager` · `PostSeasonRunner` |
| 경기 | `PreGameScreen` · `LiveGameScreen` · `PauseSubstitutionScreen` · `PostGameScreen` | 2-6 |
| 수급 | `GachaScreen` · `InventoryScreen` | `GachaManager` · `InventoryManager.GetFiltered` |
| 편성 | `LineUpScreen` (야수 9 · 벤치 5 · 투수 11) | `LineUpManager` · `LineUpAutoFill` |
| 육성 | `EnhanceScreen` · `TrainScreen` · `CombineScreen` · `DismantleScreen` · `GoldenGloveCraftScreen` | 각 매니저 |
| 오버레이 | `CardDetailScreen` · `CardPickerScreen` | `CardDisplayInfo` |

**공용 구조 (중복을 없앤 곳)**

- `CardDisplayInfo` — instanceId 하나로 마스터 + 인스턴스 + 최종 스탯을 모은 **읽기 전용 구조체**.
  모든 카드 UI가 이것만 본다. 화면이 `CardStatsCalculator`를 직접 부르지 않는다
- `CardPickerScreen` — 강화 · 훈련 · 조합 · 분해 · 라인업이 **같은 선택 모달**을 쓴다.
  고를 수 있는 카드 판정만 `Func<CardDisplayInfo, bool>`로 주입한다
- `CardActionScreen` — 육성·경제 화면 5장의 공통 틀(제목 · 재화 줄 · 본문 · 로비 버튼)
- `CardView`(상세·사진 포함) / `CardCellView`(그리드·사진 없음) **2단 분리** —
  카드 1장이 사진 768×960을 물고 있어, 200장 그리드에 전체 카드를 깔면 메모리가 터진다.
  그리드는 축약 타일, 상세만 전체 카드. 사진은 `PlayerPhotoProvider`가 **12장 상한 LRU**로 들고 있다가 화면을 떠날 때 내린다
- `ToastView` — 매니저가 bool만 돌려주므로 성공·실패를 알릴 창구가 필요했다. 흐름 제어기가 `Update`에서 깎는다

**바뀐 기존 코드**

| 파일 | 변경 | 이유 |
|---|---|---|
| `GachaResult` | `InstanceId` 추가 (+ 4인자 생성자) | 뽑기 화면이 **방금 뽑은 카드**를 그려야 한다. 기존엔 `AddCard`의 반환값을 버려 화면이 결과를 특정할 수 없었다 |
| `GachaManager` | `PityLimit` 노출 · Roll1/Roll10이 instanceId를 담아 반환 | 천장 `45 / 50` 표기 · 위와 같음 |
| `LeagueScreen` | 로비 버튼 · 가을야구 버튼 추가 | 로비 중심 흐름으로 재편 |

**검증** — 하니스 **PASS 87 / FAIL 0** (기존 35 + 포스트시즌 15 + 관전 22 + **화면 데이터 경로 30 신설**).
화면 데이터 경로 30건은 실제 CSV로 표시 정보 · 필터 · 뽑기 instanceId · 강화(전용 카드) · 훈련 · 돌파 ·
분해(미리보기/보상/카드 소멸) · 조합 · 제작 비용 · 라인업 슬롯·타순 거절 · 색 변환까지 확인한다.
컴파일은 `net9.0` + `netstandard2.1`(C# 9) 양쪽 통과.

**Desktop ↔ CLI 분업은 문서로 고정했다** — `Design/handoff/디자인_요청서.md`
(단계별 담당 · 화면별 데이터 출처 · 넣으면 바로 반영되는 애셋 경로 · 요청 문구 틀).

**남은 것**

| 항목 | 담당 | 없을 때 |
|---|---|---|
| 실제 구단 로고 10종 | 🔒 작성자 | 팀 컬러 + 이니셜로 대체 |
| 선수 CSV 업데이트 (시그·골글) | 🔒 작성자 | 시그 뽑기 빈 결과 · 골글 제작 실패 유지 |
| 누끼 사진 | 🔒 작성자 | 카드 상세의 사진 자리만 빈다 |
| 카드 베이스 3종 PNG | 🔒 작성자 (보유분) | 팀색 반투명 패널로 대체 |
| ~~뽑기·강화 연출 · 카드 3D 플립~~ | ✅ 세션 54 (2-8) | — |
| 로드맵 11 튜토리얼 | 최종 | — |

---

### 2-8. 연출 (세션 54 완료)

**트윈 코어** — 뷰가 MonoBehaviour가 아니라 코루틴을 못 쓰므로 중앙 틱 방식으로 만들었다.
`AppFlowController.Update`가 `UiAnimator.Tick(Time.unscaledDeltaTime)` 하나만 부른다.

| 파일 | 역할 |
|---|---|
| `UiTween` | 지속시간 · 지연 · 이징 · 완료 콜백을 가진 애니메이션 1개 |
| `UiAnimator` | 활성 트윈 목록을 매 프레임 갱신. Tick 중 추가분은 다음 프레임에 합쳐 목록이 흔들리지 않게 한다 |
| `UiEase` | Linear · InCubic · OutCubic · OutBack · Pulse |
| `UiEffects` | FadeIn · FadeOutAndHide · SlideUp · Pop · Flash · CountUp · Flip · Spin · SpinForever + 파티클 창구 |
| `UiEffectLayer` | 화면 맨 위 파티클 레이어. Image 조각 160개를 돌려쓴다 |
| `UiParticleSettings` | 연출 8종 프리셋(개수 · 스프라이트 · 색 · 속도 · 중력 · 수명 · 회전) |

🔴 **Unity `ParticleSystem`을 쓰지 않은 이유** — ScreenSpace-Overlay 캔버스 위로는 월드 렌더러가 그려지지 않는다.
외부 에셋(`UIParticle` 등)도 결국 파티클을 캔버스 메시로 다시 그려 이 문제를 푼다.
패키지 해석 실패가 전체 컴파일을 깨뜨릴 수 있고 Unity를 켜 검증할 수 없어, **UI 네이티브로 직접 구현**했다.
VFX Graph나 에셋스토어 파티클 프리팹을 쓰려면 캔버스를 `ScreenSpaceCamera`로 바꾸고 전용 카메라를 세워야 한다 (후순위).

**외부 파티클 애셋을 쓰는 방법** — `Assets/Resources/UI/Fx/`의 `spark.png` · `star.png` · `confetti.png`를
같은 이름으로 덮어쓰면 코드 수정 없이 바뀐다. 흰색 기준으로 그리면 프리셋 색으로 틴트된다.
(현재 파일은 파이프라인으로 생성한 기본 도형이다)

**들어간 연출**

| 위치 | 연출 |
|---|---|
| 모든 화면 | 전환 시 페이드 인 (0.16초) |
| 중계 로그 | 행이 아래에서 올라오며 등장 · 홈런은 행 전체 금색 플래시 + 색종이 40개 · 득점은 `+N점` 배지 팝 + 불꽃 14개 |
| 스코어보드 | 점수가 오른 쪽만 팝 + 금색 플래시 + 파티클 |
| 베이스 | 도루 시 주자 점이 출발 → 도착 베이스로 이동. 성공은 도착 베이스 팝 + 파티클, 실패는 붉은 점 |
| 일시정지 | 예약됨 상태에서 `spinner_00~07` 8프레임 회전 (애셋 없으면 Z축 회전으로 대체) |
| 경기 전 | 양 팀 심볼 팝 + 라인업 18행 순차 등장 |
| 경기 후 | FINAL/WIN 팝 + 점수 팝 · 승리면 색종이 |
| 뽑기 | 결과 카드가 0.08초 간격으로 순차 공개 · 5성·시그·골글은 플래시 + 파티클 |
| 카드 상세 | 누르면 가운데에서 눌렸다 펴지며 앞뒤 전환 (오버레이 캔버스라 3D 회전 대신 스케일 플립) |
| 강화 | 대상 줄 플래시 + 팝 + 파티클 |
| 훈련 | 스탯 4줄이 0.06초 간격으로 플래시 + 팝 · 돌파는 금색 플래시 + 별 파티클 |
| 조합 | 승급 시 결과 줄 플래시 + 별 32개 |
| 골글 제작 | 결과 줄 팝 + 금색 별 36개 |
| 가을야구 | 우승 확정 시 1회 — 우리 팀이면 색종이 64개 |
| 로비 | 메뉴 11칸 순차 팝 |
| 토스트 | 아래에서 올라오며 등장, 시간이 지나면 페이드 아웃 |

**검증** — 하니스 **PASS 101 / FAIL 0** (연출 코어 14건 신설: 진행값 · 완료 1회 · 지연 · 취소 · 이징 양 끝 · 프리셋 8종).

### 2-5. 미적용 개선 후보 (작성자 판단 대기)

검수 중 발견했으나 적용하지 않은 것들. 재량으로 판단하며, 다시 꺼내지 않는다.

| 대상 | 내용 |
|---|---|
| `CardCSVLoader` | `ReadCSV`에 `asset == null` 검사 없음 / `ParseHeaders`에 `.Trim()` 없음 / `throw new Exception` → `ArgumentException` |
| `CardMasterData` 계열 | `Position`이 문자열(오타가 컴파일에서 안 잡힘) / `OVR` → `BaseOVR` / `HitterMasterData.Run` → `Speed` / 생성자 매개변수 12개 전부 `int` |
| `LoadHitters`/`LoadPitchers` 공통화 | 보류. 앱 실행당 1회 호출이라 성능 무관, 카드 종류가 2개로 고정. **로더에 검사 로직을 추가하는 시점에 재판단** |
| `InventoryManager.TryExpandCapacityWithGold` | 인벤토리가 `CurrencyManager` + 가격 정책을 앎(SRP). 호출부 0건 + 단일 가격이라 **누진 곡선 도입 시점에 분리** |
| `SingletonBehaviour` 전환 | 검수 차례가 온 매니저부터. `OnSingletonAwake`가 필요한 건 `LineUpManager`(슬롯 초기화) · `LeagueManager`(세이브 서비스 생성) 2개, 나머지는 `Awake` 삭제로 끝 |
| `GoldenGloveCraftManager` 코드 기본값 | 확정치의 1/166(300 / 15,000 / 20). 씬 값이 맞아 지금은 정상이나 **필드 개명 한 번이면 씬 값이 풀려 조용히 무너진다.** 작성자가 인게임 밸런싱 때 정리 |
| 시그니쳐 뽑기 빈 결과 2.94% | 5성 중 15%가 시그 풀을 찾는데 CSV가 비어 있어 `null` 반환(10,000회 중 294회). 천장도 49에서 영구 대기. **시그 CSV 입력으로 해소된다** |
| `BoxScoreBuilder` 투수 등판 순서 | 타석 로그를 전부 돌린 뒤 도루 로그를 처리하므로, 타자를 한 명도 상대하지 않고 **도루자 아웃만 잡은 투수**가 목록 맨 뒤로 간다. 로그에 전역 순번이 없어 보류 |
| `SingletonBehaviour`에 `OnDestroy` 없음 | `Instance`를 비우지 않아 도메인 리로드를 끈 에디터에서 죽은 참조가 남을 수 있다. 실기기에는 영향 없음 |

---

## 3. 프로젝트 불변 규칙

### 3-1. cardId는 append-only 🔴

로스터 SO(`AiTeamRosterData`)와 `StartingSignatureTable`이 **cardId만 저장**하므로,
CSV에서 cardId를 재배치하면 **모든 SO가 예외·로그·컴파일 에러 없이 조용히 오염된다.**

| 허용 | 금지 |
|---|---|
| 새 카드를 맨 뒤 번호로 추가 | 기존 cardId 재배치·재정렬 |
| 기존 카드의 스탯·이름 수정 | 삭제한 cardId 재사용 |
| 시트에서 행 정렬해 보기 (파서가 헤더 기반) | 정렬 결과로 cardId 재부여 |

- 신규 시작 번호: 타자 `160~` / 투수 `50167~`
- 카드 제외 시 **행 삭제보다 cardId를 비워두는 편이 안전** — 검증기가 "없는 cardId"로 잡아준다

### 3-2. 소스 인코딩

- `.cs`는 **UTF-8 BOM 포함**. 루트 `.editorconfig`(`[*.cs] charset = utf-8-bom`)로 강제
- Unity Roslyn은 BOM 없는 소스를 UTF-8로 가정 → CP949 파일의 **문자열 리터럴이 화면·콘솔에 깨져 노출**된다
- CSV는 UTF-8 **BOM 없음** (구글 시트 내보내기 기본값). `TextAsset.bytes`로 받아 직접 디코딩

### 3-3. 테스트는 프로젝트에 넣지 않는다

- 검증은 **스크래치패드 dotnet 하니스**(UnityEngine 최소 셰임 + `JsonUtility` 재현 + 실제 CSV 로드)에서만 한다
- 프로젝트 테스트 파일 **0개 유지**. 하니스는 검증 후 삭제
- **검증 하니스** (세션 46~48): 컴파일 + 런타임 검증 50건. `dotnet run -c Release`로 실행한다
  - 세이브 왕복(리그·포스트시즌) · 손상 세이브 거부 · 일정 결정성/공정성
  - 재화 규약(`SpendAll` 중복·음수 복원) · 인벤 일괄 제거 원자성 · 저장소 원자적 쓰기
  - **실제 CSV 325장 파싱** (cardId 유일성 · 포지션 해석 · OVR = 4스탯 평균 · 팀 10개)
  - **확률 실측**: 조합 승급(75/22.5/2.5%) · 뽑기 등급(70/25/5%) · 천장 50회 · 1000경기 시뮬 지표
  - `JsonUtility`는 셰임에서 리플렉션 기반으로 재현했다 (공개 필드만 직렬화하는 Unity 규칙)
  - 세션 48 추가: **투수 교체 팀 판별** · **인터럽트 교체 3종**(벤치 중복·체력 리셋·강판 재투입) · **실책 진루** ·
    **투수 이닝 = 타석 아웃 + 도루자 아웃** · **세이브 부분 실패 롤백** · **포스트시즌 보상 15건**(도달 단계·중복 수령·세이브 왕복)
- **UI 셰임 추가** (세션 52, 세션 53에 `Resources.UnloadAsset` 등 보강): `UnityUiShim.cs` — `Vector2/3/4` · `Color` · `RectTransform` · `Canvas` · `CanvasScaler` ·
  `Image` · `Button` · `ScrollRect` · `EventSystem` · `InputSystemUIInputModule` · `TMP_Text` 등 최소 표면.
  UI 코드도 Unity를 켜지 않고 컴파일 검증한다
- **컴파일 검증 하니스** (세션 46 신설): 스크래치패드에 `UnityShim.cs`(UnityEngine 최소 셰임) + `harness.csproj`를 두고
  `dotnet build -p:GameRoot=<프로젝트 경로>`로 `Assets/Scripts/**` 전체를 컴파일한다. Unity를 켜지 않고 오류를 잡는다.
- **`StatBaseline` 드리프트는 에디터 메뉴로도 본다** — `Tools/BaseBallManager/StatBaseline 드리프트 측정`.
  하니스를 켜지 않고 Unity 안에서 2-1을 확인할 수 있다 (세션 48 신설)
  셰임이 덮는 범위: `MonoBehaviour` · `ScriptableObject` · `Debug` · `Mathf` · `Random` · `JsonUtility` · `Application` · `Resources` · `TextAsset` + 직렬화 속성 8종
- **API 표면 검사 하니스** (세션 48 신설): 같은 스크립트를 `netstandard2.1` + `LangVersion 9.0` **라이브러리로 한 번 더 컴파일**한다.
  실행용 하니스는 `net9.0`이라 **Unity에 없는 BCL 오버로드를 통과시킨다** — `File.Move(string, string, bool)`을 그렇게 놓쳐
  세션 48 커밋이 Unity에서 `CS1501`로 깨졌다. 프로젝트 설정은 `apiCompatibilityLevel: 6`(.NET Standard 2.1) · `LangVersion 9.0`.
  **하니스가 통과해도 이 검사를 함께 돌려야 Unity를 켜기 전에 잡힌다.**
- 하니스 함정: `SingletonBehaviour.Awake`는 `Instance`가 있으면 조기 반환한다. 섹션마다 새 매니저를 만들려면
  `<Instance>k__BackingField`를 리플렉션으로 null로 밀어야 한다. 미전환 매니저는 백킹 필드가 자기 자신에 있으므로 `BaseType`을 따라 올라가며 찾는다

### 3-4. 코딩 컨벤션 (CLAUDE.md 보충)

- `if` 중괄호: 본문 **한 줄이면 생략, 두 줄 이상이면 사용**
- 주석: **public은 `<summary>` 2줄 이내 / private는 `//` 1줄 이내 / 메서드 내부 주석 금지** (세션 46 확정)
  - **기획서 참조를 달지 않는다.** `(기획서 9.1)` 같은 인용은 주석·`[Header]`·`[Tooltip]`·`Debug` 로그 어디에도 쓰지 않는다.
    작성자가 기획서를 다 알고 있어 장·절 번호가 정보가 되지 않고, 개정되면 번호만 어긋난 채 남는다 (세션 49에 102건 일괄 제거)
  - 근거·트레이드오프는 PROGRESS와 응답에 쓴다. 설명하고 싶은 블록은 주석 대신 **메서드로 추출해 이름으로 드러낸다**
- 검수 응답은 지적과 수정안을 **코드 단에서 함께**, `diff` 블록으로 기존/수정 구분
- 검수에서 발견한 결함의 수정안은 **빈 뼈대가 아니라 완성 코드**로 제시
- 작성자가 지적을 안 고치고 넘어가면 **재량 판단**으로 보고 다시 꺼내지 않는다

---

## 4. 확정 설계 결정

### 4-1. 레이어 구조

```
Cards / CSVParse      마스터 데이터 (CSV) · CardInstance (보유 카드)
Inventory / Gacha / Enhance / Train / Combine / Dismantle / GoldenGlove / Currency
Line Up               슬롯 편성 (야수 9 + 벤치 5 + 투수 SP5·RP5·CP1)
Builders              브릿지 — 게임 데이터 → 시뮬 입력 (양쪽에 의존하므로 별도 폴더)
Simulation            경기 상태·진행 (순수 C#, Cards 무의존)
ProbabilityModels     스탯 → 확률 변환 (순수 C#)
Record                박스스코어 집계 (사후 변환)
League                일정 · 순위표 · 진행 · 포스트시즌
Player / Save         세이브 · 시작 플로우
Core                  SingletonBehaviour
```
**씬 하이어라키** (세션 49) — 매니저 15종을 한글 그룹 7개 아래로 묶었다. 루트 17 → 9개

```
데이터        CardDataManager · PlayerDataManager
보유 관리     InventoryManager · CurrencyManager
카드 수급     GachaManager · GoldenGloveCraftManager · CombineManager · DismantleManager
카드 육성     EnhanceManager · TrainManager · BreakthroughManager
라인업        LineUpManager
리그          LeagueManager · AiRosterManager
게임 흐름     GameFlowManager
```

- `DontDestroyOnLoad`는 **루트에만 걸린다.** 자식이 된 매니저를 살리려고
  `SingletonBehaviour`가 `transform.root.gameObject`에 건다 — 그룹째로 넘어간다
- 중복 인스턴스는 `Destroy(gameObject)`로 **자기 자신만** 지운다. 부모를 지우면 형제 매니저까지 사라진다
- 매니저를 추가할 때는 **반드시 그룹 중 하나의 자식**으로 넣는다. 루트에 두면 그룹 규칙이 무너진다


- **시뮬 코어는 MonoBehaviour 배제** — 리그 일괄 시뮬을 UI 없이 고속 반복하기 위함 (기획서 원칙)
- `Simulation/`은 `Cards/`를 모른다. 강화·훈련이 반영된 **최종값 스냅샷(struct)만** 주입받는다

### 4-2. 규약

| 규약 | 내용 |
|---|---|
| `-1` 센티넬 | 시뮬 코어의 "없음" (베이스 주자, 빈 슬롯, 투수 슬롯 소진). **산술에 들어가는 값에는 쓰지 않는다** (`GetStatBonus` 실패값이 `0`인 이유) |
| `0` 센티넬 | 에디터 SO의 빈 카드 슬롯. cardId가 1부터라 안전하고 `int` 기본값이 곧 빈 슬롯 |
| 컬렉션 반환 | 실패 시 `null`이 아니라 **빈 컬렉션** (CLAUDE.md 3-2) |
| 투수 배열 | `[0]=SP / [1~5]=RP / [6]=CP` 7칸 |
| 공수 판별 | **확률 모델은 `GameState.IsTopInning`을 직접 읽지 않는다.** 호출부가 타석 시작 시점에 캡처한 `isTopInning`을 매개변수로 넘긴다 (`StealCalculator.IsSuccess` · `PitcherChangeEvaluator.GetNextPitcherSlot`) |
| 실패 시 전부 롤백 | `Restore` · `BuildRosters` · 임포터 — **부분 성공 상태를 남기지 않는다** |
| 재화 소모 | `SpendAll`은 "전부 검사 → 전부 차감" 2패스. 강화·훈련·돌파는 되돌릴 수 없어 부분 차감이 곧 영구 손실 |
| 강화 전용 카드 | 인벤토리 카드가 아니라 **재화**(`CurrencyType`). 카드로 만들면 마스터 CSV에 스탯·포지션이 없는 유령 카드가 200장 한도를 먹고 라인업·분해·필터가 전부 예외 분기를 갖는다 |
| 소모 순서 | **재료 소멸보다 결과 확보를 먼저 확인** — 후보 풀이 비면 재료만 먹고 카드를 못 준다 (골글 제작·카드 조합에서 실제로 밟은 교훈) |

### 4-3. 표현할 수 없는 상태는 만들지 않는다

- `AiHitterSlot`에 `battingOrder` 필드 없음 — **배열 인덱스가 곧 타순.** 필드를 두면 "3번이 두 명"이 표현 가능해지고 검증 코드가 필요해진다
- `LeagueTierEntry`에 `LeagueTier` 필드 없음 — 같은 이유. 대가는 인스펙터에 `Element 0`으로만 보이는 것
- 딕셔너리는 **선언 시 생성 + `readonly`** — null이 되는 순간 자체가 없어져 조회 메서드마다 null 검사를 붙이는 구조가 끊긴다

### 4-4. 데이터 소유권

| 데이터 | 소유자 | 이유 |
|---|---|---|
| 카드 마스터 325장 | CSV (구글 시트) | 벌크 + OVR 수식은 시트가 적합 |
| AI 로스터 편성 | SO (`AiTeamRosterData`) | 슬롯별 판단이라 인스펙터가 적합 |
| 티어별 수치 (경기 수·보정·보상) | SO (`LeagueTierTable`) | MonoBehaviour에 두면 씬마다 값이 갈라진다 |
| 확률·비용 튜닝 노브 | `[SerializeField]` | 게임 규칙 수치도 변경 가능성이 있으면 열어둔다 |
| 데이터 계약 (강화 계수 등) | `const` | 세이브에 레벨만 저장되고 스탯은 재계산 → 값 변경이 기존 카드에 소급 적용된다 |

- 순수 C# 클래스는 `[SerializeField]` 불가 → 인스펙터 튜닝이 필요하면 **MonoBehaviour가 생성자로 주입**한다
- `StatBaseline`(카드 풀 평균)은 단일 출처. 네 군데 공식이 같은 값을 쓰는데 복제하면 CSV 개편 시 한쪽만 고쳐진다

### 4-5. 시뮬 엔진 주의점

- **`isTopInning` / `scoreBefore` / `inningRunsBefore`는 `Apply()` 전에 캡처한다.**
  `Apply()` 내부 `AddOut()`이 3아웃 시 공수를 반전시키므로, 이후 참조하면 **엉뚱한 팀**의 상태를 읽는다
  (세션 25·38·48에서 같은 원인으로 버그 3건 발생)
- **`IsTopInning == true`는 초 = 원정 공격 = 홈 수비.** 투수 판별 시 반전 주의
- **`SimulationContext`는 이름과 달리 라인업 배열이 mutable** — `ApplyInterruptDecision`이 직접 덮어쓴다.
  AI 로스터 원본을 그대로 넘기면 대타 교체 1회가 고정 로스터를 영구 오염시킨다 → `CopyLineup`으로 사본 전달
- **`AddOut()`을 두 번 부르거나 그 뒤에 `AddRun()`을 부르는 코드는 3아웃 가드를 반드시 둔다.**
  병살·희생플라이가 그렇다. 가드가 없으면 득점이 공수가 바뀐 **상대 팀**에 들어간다
- **9회 초 종료 시 홈팀이 앞서면 말 공격 없이 종료** (`GameState.AddOut`). 없으면 득실차가 부풀려져 순위 타이브레이커가 왜곡된다
- 진행 루프는 `GameSession` 한 벌 — `SimulateGame()`도 `while (session.StepAtBat()) { }` 로 경유한다.
  두 루프를 따로 두면 일괄 시뮬과 실시간 관전의 규칙이 언젠가 갈라진다

### 4-6. 로드맵 9번 UI 구성 (확정, 코드 착수는 디자인 후)

| 화면 | 띄우는 것 | 데이터 출처 |
|---|---|---|
| 경기 전 | 양 팀 라인업 · 로고 · 선발 · 해당 리그 상대전적 | `SimulationContext` + `TeamRecord` |
| 경기 중 | 이닝/아웃/베이스 · 스코어 · 타석 로그 자동 흐름 · 일시정지 | `GameSession` |
| 경기 후 | 라인스코어 · R/H/E · 선수별 기록 | `BoxScore` |
| 일시정지 | 벤치 야수 / 대기 투수 + 교체 버튼 | `LiveGameController` |

- 실시간 로그는 타석마다 **자동 진행**(탭 넘김 아님) → UI에 코루틴 타이머 필요
- 시뮬 진행 중 조작은 **일시정지뿐**. 스킵 버튼 없음 (기획서 8.5)

---

## 5. 확정 수치

### 5-1. 밸런스 (1000경기 실측 vs KBO)

편차 정규화(`StatBaseline.GetEdge`) 도입으로 튜닝 완료.

| 지표 | 실측 | KBO |
|---|---|---|
| 타율 / 출루율 / 장타율 | .270 / .337 / .401 | .270 / .345 / .400 |
| 삼진율 / 볼넷율 / 홈런율 | 17.6% / 9.0% / 1.8% | 17.5% / 9.3% / 2.0% |
| 경기당 총득점 | 8.53 | ~9.0 |
| 경기당 도루 / 성공률 | 1.42개 / 70.3% | 1.2~1.6 / ~70% |

**의도적으로 수용한 차이**
- 출루율 -.008 — 사구(HBP)가 모델에 없음. 구조적으로 낮게 나오는 것이 정상
- 득점 -0.5 — 진루 규칙 단순화(1·2루 태그업 없음, 실책 후 추가 진루 없음). 여기서 안타 확률을 올리면 타율이 목표를 벗어난다

**v1.0 제외 확정**: 태그업 중 아웃 · 삼중살 · 사구 · 희생번트 · 대주자 교체 · 포지션 스왑 · 타이브레이커 경기 · 일괄 시뮬

### 5-2. 리그 티어 (`LeagueTierTable.asset`, 21행 입력 완료)

| 티어 | 경기 수 | `_statBonus` | `_goldReward` |
|---|---|---|---|
| `Basic1~3` | 9 / 36 / 36 | 0 | 3,000 / 8,000 / 8,000 |
| `Rookie1~3` | 54 | 2 | 15,000 |
| `Amateur1~3` | 81 | 4 | 25,000 |
| `Pro1~3` | 108 | 7 | 40,000 |
| `Minor1~Minor3` | 144 | 10 / 12 / 14 | 60,000 |
| `Major1~Major3` | 144 | 15 / 17 / 19 | 85,000 / 92,500 / 100,000 |
| `Legend1~Legend3` | 144 | 20 / 24 / 30 | 150,000 / 200,000 / 250,000 |

- 골글 포인트는 **프로1부터** 지급 (500~5,000) — 골글 제작을 중후반으로 게이팅
- 경기당 보상 확률 50~100%. **`Basic1`의 0%는 의도된 값** — 9경기짜리 첫 리그는 맛보기라 육성 재료를 주지 않는다
- 경기당 보상은 **승패와 무관하게 지급** — 못 이기는 구간에서 보급이 끊기면 영영 따라잡을 수 없다

### 5-3. `Minor1~Legend3` 난이도 곡선 (세션 48 입력 완료)

| 티어 | `_statBonus` | 이 티어에서 승률 .540이 되는 플레이어 육성량 |
|---|---|---|
| `Minor1` / `Minor2` / `Minor3` | 10 / 12 / 14 | 전 스탯 +8.5 / +10.5 / +12.5 (강화 4~6) |
| `Major1` / `Major2` / `Major3` | 15 / 17 / 19 | +13.5 / +15.5 / +17.5 (강화 7~9) |
| `Legend1` / `Legend2` / `Legend3` | 20 / 24 / 30 | +18.5 / +22.5 / +28.5 (강화 10 + 훈련) |

- 실측 경험식: **`AI _statBonus ≈ 플레이어 스탯 증가분 + 1.5`** 에서 승률 .540 (강화 레벨 N = 전 스탯 +2N)
- 플레이어 상한은 강화 10(+20) + 훈련 50(스탯당 평균 +24.5) = **약 +44.5**.
  `Legend3`의 30은 만렙 플레이어가 확실히 이기는 지점이라 **엔드 콘텐츠 완주 보상**으로 설계된 값이다
- 이전에는 9티어가 전부 10이라 8단계가 같은 경험의 반복이었다. 이제 단조 증가 곡선이 생겼다


### 5-4. 재화 · 제작 비용

**골든글러브 제작** (작성자 확정 — 엔드 콘텐츠이므로 쉽게 만들 수 없어야 함)

| 제작 | 골글 포인트 | 포인트 | 훈련 카드 |
|---|---|---|---|
| 일반 (전체 랜덤) | 50,000 | **2,000,000** | 100 |
| 팀 선택 | 100,000 | 5,000,000 | 300 |

> 포인트는 세션 48에 1,000,000 → **2,000,000**으로 올렸다 (기획서 4장 초안치에서 상향).
> 코드 기본값도 같은 값으로 맞춰 뒀다 — 필드 개명으로 씬 값이 풀려도 조용히 무너지지 않는다.

**포스트시즌 보상** (세션 48 입력 — 도달한 가장 높은 단계 하나만 지급)

| 단계 | 골드 | 골글 포인트 | 시그권 | 골카 전용 |
|---|---|---|---|---|
| 와일드카드 진출 | 50,000 | 100 | 0 | 0 |
| 준플레이오프 진출 | 150,000 | 150 | 0 | 0 |
| 플레이오프 진출 | 300,000 | 200 | 1 | 0 |
| 한국시리즈 진출 | 500,000 | 300 | 1 | 1 |
| **한국시리즈 우승** | 1,000,000 | 500 | 2 | 1 |

- 티어와 무관한 고정값이다. `Minor1` 우승과 `Legend3` 우승이 같은 보상을 준다
- 정규시즌 1위도 골카 전용을 1장 받으므로, **1위 + 한국시리즈 진출이면 한 시즌에 2장**이 들어온다
- ⚠️ 골드가 정규시즌 보상을 크게 앞선다 (`Legend3` 1위 250,000 대비 우승 1,000,000 = 4배 / `Minor1` 기준 16.7배).
  다만 **골드 싱크가 인벤 확장(1회 1,000골드) 하나뿐**이라 이미 중반부터 남아도는 재화다 —
  기획서 9.3의 "확장 비용 누진 여부 TBD"가 풀리기 전에는 여기서 조정해도 의미가 크지 않다


**카드 조합** — 재료 3장 → 1장. 무작위 재료 1장의 등급을 기준으로 승급 판정(3→4: 25%, 4→5: 10%, 연쇄 가능)

| 재료 구성 | 3성 | 4성 | 5성 |
|---|---|---|---|
| 3/3/3 | 74.8% | 22.5% | 2.7% |
| 4/4/4 | 0% | 90.2% | 9.8% |
| 4/5/5 | 0% | 29.8% | 70.2% |

- 시그·골글은 5성 고정이라 승급 없이 같은 종류 안에서 리롤(중복 허용)
- ⚠️ 포인트의 유일한 획득 경로가 **분해**다. 승급 확률이 후하면 잉여 3성이 전부 조합으로 흘러 포인트 경제가 멈춘다
- ⚠️ 골글 조합은 사실상 죽은 기능(3장 태워 1장 = 순손실 2장). 규칙 일관성 때문에 막지 않았을 뿐

**시작 지급**: 일반 뽑기권 **150장** + 시작 시그니쳐 1장 (인벤 한도 200)
- 근거 실측(각 1000회): 50장 37.1% / 100장 94.7% / 120장 97.1% 라인업 완성률
- 뽑기권 재획득 경로가 리그 보상뿐이고 리그는 라인업이 있어야 입장 가능 → 못 채우면 데드락.
  구제 지급·분해 보상 추가 대신 **지급량 상향으로 종결**(작성자 결정)

---

## 6. 과거에 실제로 낸 버그 유형 (재발 방지)

| 유형 | 사례 |
|---|---|
| **인접한 비슷한 메서드를 복사** | `ParseCardGrade`가 `ParseCardType`의 `"N"/"S"/"G"`를 그대로 물려받아 게임이 아예 안 뜸. "모양만 바꾸는" 리팩토링에서도 분기 값이 바뀔 수 있다 |
| **공수 판별 반전** | `GetNextPitcherSlot`이 `!IsTopInning`으로 판별 → 지고 있을 때 마무리 투입. 세션 23에서 고친 버그가 26에 되살아났고, **48에서 세 번째로 재발**(`Apply()` 이후의 `gameState.IsTopInning`을 읽음). 3,000경기 중 이닝 경계 교체의 20%가 반대 팀 기준으로 판단. 이번엔 매개변수로 못 박아 구조적으로 막았다 |
| **`Apply()` 이후 상태 참조** | 인터럽트 교체가 상대 팀에 적용 → AI 벤치가 빈 배열이라 `IndexOutOfRangeException` |
| **얕은 복사 후 원본 Clear** | `InterruptDecision`에 리스트 참조를 넘기고 `_pending.Clear()` → 반환 객체까지 비워짐 |
| **씬 인스펙터 값이 코드 기본값을 덮음** | `_useAiRosterForPlayerTeam`이 씬에 `1`로 남아 **AI 로스터로 리그가 돌고 있었음.** 필드 개명 시 씬 값이 풀리는 것도 같은 계열 |
| **검사 순서** | 카드 조합에서 중복 재료 검사를 존재 검사보다 뒤에 두면 1장으로 3장 조합이 성립(카드 복제) |
| **반환값을 버림** | `Restore`를 `bool`로 바꿨는데 호출부가 안 받아 인벤이 빈 채로 "이어하기 성공" 보고 |
| **선발 기준 오용** | `LineUpAutoFill`이 OVR로 선수 선발 → OVR은 4스탯 단순 평균이라 타자 정확과 상관 +0.33뿐. 타율 .207로 붕괴 |
| **전제가 깨진 공식** | `0.22 + 0.30 * (구위̂ - 정확̂)`는 "두 스탯군 평균이 같다"를 전제. 실제 11점 차라 볼넷 매치업의 96.5%가 하한에 고정 |
| **케이스를 합치며 규칙이 흡수됨** | `switch`에서 `Walk`와 `Error`를 한 케이스로 묶자 실책이 밀어내기로 바뀜(기획서는 전원 +1베이스). 1,000경기 중 58회 진루 누락. **중복 제거는 두 케이스의 규칙이 같을 때만 한다** |
| **쓰이지 않는 검사** | `GameState.IsHitterUsed`를 만들어 두고 읽는 곳이 0곳이라 대타 재투입이 뚫려 있었다. **Mark만 있고 Is가 없으면 그 불변식은 없는 것이다** |
| **재생성으로 상태가 초기화됨** | 인터럽트가 현재 등판 슬롯을 다시 지정하면 `new PitcherState(...)`가 체력을 만땅으로 되돌렸다(20→80). **같은 대상으로의 교체는 no-op이 아니라 리셋이다** |
| **부분 적용 후 실패 반환** | `PlayerSaveService.Load`가 플레이어 데이터를 먼저 덮어쓴 뒤 인벤 복원에 실패 → `false`인데 팀·티어는 이미 바뀜. **검증을 전부 끝낸 뒤 적용한다** |

📝 테스트가 실패하면 **먼저 기대값 산수를 의심한다.** 세션 37·40에서 실패 7건이 전부 테스트 오류였고 프로덕션 코드는 정상이었다.

---

## 7. 세션 이력 요약

| 세션 (기간) | 한 일 |
|---|---|
| 1~5 (06-13 ~ 07-03) | CSV 포맷 확정 · 카드 마스터 데이터 모델(추상 클래스 + 타자/투수) · `CardCSVLoader` · `CardInstance` · `CardDataManager` |
| 6 (07-05) | 인벤토리 시스템 — `InventoryManager` · `CardFilter`(enum + `None` 센티넬 방식) |
| 7 (07-06) | 기획서 최종본 확정 (교체 모드 · 벤치 5칸 · 10연 천장 · KBO 승률 · `ISaveStorage`) + GitHub 마일스톤 12개 |
| 8~9 (07-08) | 뽑기 시스템 — 확률표 · 10연 천장 · 50연 팀 확정 · `PlayerDataManager` |
| 10~11 (07-09 ~ 07-10) | 강화 시스템 — 동일 카드 판정 · 재료 검증 · `ApplyEnhance` |
| 12 (07-11) | 훈련·돌파 — `TrainManager` / `BreakthroughManager` SRP 분리, `_trainDelta`를 `IReadOnlyList`로 노출 |
| 13~14 (07-12 ~ 07-16) | 라인업 — 포지션 enum + 파서, `LineUpManager` 전체 (야수 9 · 벤치 5 · 투수 11) |
| 15~16 (07-17 ~ 07-19) | 시뮬 데이터 구조 — 스냅샷 struct · `SimulationContext` · `GameState` · `PitcherState` |
| 17~20 (07-19 ~ 07-25) | 타석 확률 모델(`BatterOutcomeCalculator`) + 진루 처리(`BaseRunningCalculator`) 전 케이스 |
| 21~23 (07-25 ~ 08-01) | 투구 수 산출 · 투수 피로 패널티 · 교체 판단 · 불펜 운영 |
| 24~25 (08-01 ~ 08-02) | 경기 루프 통합(`GameSimulator`) + 코드 리뷰 반영 (순서 의존성 제거, `-1` 센티넬 도입) |
| 26~27 (08-03 ~ 08-06) | 인터럽트·수동 교체 — `IGameInterruptHandler` · 사용 완료 트래킹 · `LiveGameController` |
| 28 (08-08) | 타순 중복 검증 버그 수정 + 라인업 조회 API. **"시뮬 엔진이 한 번도 실행된 적 없음"(데이터 브릿지 부재) 발견** |
| 29 (08-08) | 데이터 브릿지 — `CardStatsCalculator`(기획서 1.4 공식) · `SimulationContextBuilder` |
| 30~35 (08-09 ~ 08-14) | **리그 시스템 전체.** AI 로스터를 자동 편성 → **SO 기반 수동 편성으로 재설계**(동일 인물 중복 출장 · 난이도 노브 부재 때문) · 에디터 도구 5종(카드 검색 드롭다운) · 씨앗 임포터 · 검증기 · 일정 생성(원형 라운드 로빈) · 순위표 · 리그 진행 · 포스트시즌 · 시즌 저장 · **밸런스 튜닝(편차 정규화)** |
| 36 (08-16) | 플레이어 배관 — 재화 12종 · 분해 · 플레이어 세이브 · 시작 플로우 · 리그 보상/해금 · 라인업 자동 편성 (신규 11 + 수정 16 파일) |
| 37 (08-16) | 경기 기록 데이터 레이어 — 박스스코어 + **9회초 종료 규칙 버그 수정** |
| 38 (08-16) | 전 모듈 통합 테스트 309건 + 버그 3건 수정 (공수 판별 · 인터럽트 크래시 · 자동 편성 기준) |
| 39 (08-17) | 씬 매니저 배선 8개 추가(14종) · 테스트 코드 프로젝트에서 제거 · 티어 테이블 수치 입력 · 티어별 밸런스 실측 |
| 40 (08-18) | 미해결 33건 일괄 처리 — 기획서 정정 · 리그 진행 단위 1경기 확정 · **도루 구현** · 강화 전용 카드(재화로 처리) · **골든글러브 제작** · 시작 시그니쳐 에디터화 · 리그 보상 확장 |
| 41 (08-25) | `GameSession` 구현(로드맵 9번 선행) · 시작 지급 150장 · **카드 조합 시스템** 신규 · 코드 검수 트랙 착수 |
| 42 (08-30) | 검수 #7~10. `SingletonBehaviour` 신설 · `CardInstance` 생성자 위임 · `CardDataManager` `TryAdd`. **`ParseCardGrade` 치명적 버그 발견·수정** |
| 43 (09-12) | 검수 #11~14. `InventoryManager` **List → Dictionary**(경로 18곳 O(n)→O(1)) · `CardFilter.Matches` 이관 · `GachaManager` 천장 카운터 버그 2건 + 풀 캐싱 |
| 44 (09-24) | 검수 #15 `PlayerDataManager` — `SingletonBehaviour` 전환 · `SetPlayerTeam`/`Restore` `bool`화 + 티어 범위 검증 · `PlayerSaveService` 호출부 동반 수정. **PROGRESS.md 2,812줄 → 축약 재구성**(세션별 나열 → 주제별. 완성 메서드 목록·중간 과정·튜닝 전 수치 제거, 살아있는 규칙·미결·수치만 보존. 원본은 git 히스토리) |
| 45 (09-25) | 검수 #16 `EnhanceManager` — `SingletonBehaviour` 전환 · 재료 `List<int>`→`int` 단일화 · **라인업 편성 카드 재료 사용 차단**(`CombineManager`·`DismantleManager`와 규칙 통일) · 실패 경로 로그 보강 · 동일 카드 판정 `IsIdenticalCard`로 통합 · `GetRequiredEnhanceCardType`의 `throw`를 `CurrencyType.None` 반환으로 교체 |
| 46 (09-25) | **검수 방식 전환 — Claude가 직접 리팩토링·커밋.** 주석 규칙 확정(public `<summary>` 2줄 / private `//` 1줄 / 메서드 내부 금지). 검수 #17~18 `TrainManager`·`BreakthroughManager` — `SingletonBehaviour` 전환 · **`ApplyTrain` 반환값 미수신**(재화만 소모되는 경로) 수정 · `TrainStatCount` `public const` 승격으로 매직넘버 `4` 제거 · 돌파 비용 필드 개명 + 중첩 switch 분리 · 대기표의 `LeagueManager` `SetPlayerTeam` 2건 처리 |
| 47 (09-25) | **전 파일 1회차 검수 완료** (45개 / 약 7,000줄). 🔴 수정: `GameSimulator` 자동 교체가 강판 슬롯을 기록하지 않던 문제 · `LineUpManager`가 `TryParse` 실패를 무시해 오타 카드를 LF/SP에 배치하던 문제 · `CurrencyManager.SpendAll`의 같은 재화 중복 차감(음수 가능) · 세이브 재화 복원 실패가 "이어하기 성공"으로 보고되던 문제. 구조: 시뮬 상수 `SimulationContext`/`GameState`로 통일 · 밀어내기 진루 로직 중복 제거 · `RunnerLookup` 신설 · 매니저 7종 `SingletonBehaviour` 전환 · 주석 규칙 일괄 적용(변환기 자동화) · **컴파일 검증 하니스 신설** |
| 47 (09-25) | **전 파일 정밀 검수 완료 + 검증 하니스 91건 구축.** 🔴 수정: `LocalFileStorage` 비원자적 저장(앱이 죽으면 세이브가 잘림) · `CombineManager` 재료 부분 소멸 · `CardCSVLoader`가 null을 Split + 파싱 오류에 행 번호 없음 · 타자/투수 cardId 교차 중복 미검출 · `GachaManager` 빈 카드 풀 미캐시(매 뽑기마다 325장 재순회) · 세이브 복원 검증 4종(티어 범위·진행도 정합성·null 항목·배열 길이). 실측 검증: 조합 승급·뽑기 확률·천장·CSV 325장·1000경기 시뮬 전부 통과. **`StatBaseline` 드리프트 발견(2-1)** |
| 48 (09-26) | **3회차 전수 검수 — 실측 기반.** 🔴 `PitcherChangeEvaluator` 공수 판별 반전 3회차 재발(이닝 경계 교체의 20%가 오판, 마무리 오투입 19.6%→0%) · 인터럽트 교체 3종(벤치 카드 중복 투입 · 같은 슬롯 재지정으로 체력 리셋 · 강판 투수 재투입) · `BaseRunningCalculator` 실책 진루가 밀어내기로 축소(기획서 8.3 위반) · `PlayerSaveService` 부분 적용 롤백. 🟠 `LocalFileStorage` 비원자적 교체(Delete→Move 창) · 도루자 아웃이 투수 이닝에서 누락(경기 27%) · `CombineManager` 씬 배치. 🟡 BOM 없는 `.cs` 5개 · `AssignHitter` 점유 슬롯 덮어쓰기 · `AiRosterManager` 딕셔너리 열거 순서 의존 · 병살/희생플라이 3아웃 가드. 신규: **포스트시즌 보상**(라운드별 절대값 인스펙터 · 중복 수령 차단 · 세이브 왕복) · `LeagueManager.SetInterruptHandler` 주입 경로. 검증: CSV 325장 · AI 로스터 SO 10팀 오류 0 · 일정 공정성 · 하니스 50건 전건 통과 |
| 51 (09-27, Desktop) | **로드맵 9 디자인 착수.** 사진: 원본 = 경기·미디어 사진, **인물 × 연도** 단위(작성자 확정) → `build_photos.py` 재작성 — YuNet 눈·입 + 2차 정밀 검출(해상도·15° 기울기와 무관하게 얼굴 크기 편차 1.1%) · BiRefNet-portrait 누끼(출력 영역만) · 관중 조각 제거 · 배경색 번짐 제거 · 이어하기 · `review.html` 검수 시트. 디자인: Claude 캔버스 시안 v0 7보드(경기 전 · 경기 중 · 일시정지 2 · 경기 후 · 컴포넌트 시트 · 카드 레이어) — 남색(노말 카드)+금색(골글 카드) 톤, 문자중계 강조 4단계, 등급 = OVR 타일 색 + 별 개수. **→ 작성자 피드백으로 v1**: 버튼 흰색·각진 모서리(노란 버튼 금지), 배경 무늬 제거, 금색은 홈런·득점·골글에만, AWAY/HOME 영문, 이닝 ▲(초)/▼(말), 아웃 점 2개, '중계', OVR = 배경 없이 카드 색 숫자(노말 3·4·5성은 색 통일, 별 개수로만 구분), 경기 전 라인업 원정 좌·홈 우 동시 표시, 이름 표기 `이름 '연도`, 결과 헤더 FINAL + WIN/LOSE/DRAW, 카드 앞면 = OVR 숫자·포지션·엠블럼·강화 육각형·사다리꼴 이름 띠만 / 뒷면 = 상세 스탯·강화·훈련·돌파(탭하면 뒤집힘), 강화 +1~+9 단계별 틴트 + +10 홀로. **→ v2**: 별은 등급 수만큼만(빈 별 없음) · 행의 시그/골글 태그 삭제(색으로만 구분) · MY TEAM 금색 · 이닝 숫자 92 · 연도·포지션·별·교체 미리보기 확대 · 카드 글자 전반 확대(OVR 270 · 이름 142 · 로고 180 · 강화 육각형 264) · 별은 카드 위 중앙 · 이름 명판을 종류별로(노말 청사진 HUD / 시그 붓터치 / 골글 금테+월계수 트로피) · 강화 테두리 3단(+1~3 흰 · +4~6 은 · +7~9 금), +9 블랙골드, +10 홀로 톤다운. **→ 카드 v3**: 강화 +1~+9를 한 모양(단색 + 단계 테두리 + 흰 글자)으로 통일하고 저채도로(+9 특수 처리 삭제), 강화 글자 88→100 · 이름 명판 삭제 → 테두리 없는 타원 그림자(종류별 색) · 포지션 92→108. **→ 작성자 승인, 디자인 v1 확정.** 명세 6장(색 토큰 · 서체 · 컴포넌트 · 화면 · 카드 좌표 · 강화 색) 신설, 1~3장의 어긋난 지침 정정, 3-1 애셋 목록 89→68종 개정(`out_dot` 2개 · `stamp_*` · `icon_pos_*` · `scoreboard_bg` · `bg_app` 등 삭제, `card_name_shadow` · `badge_enhance(_holo)` · `icon_inning` · `tag_outline` 등 추가). **→ PNG 애셋 69종 납품**: `Design/ui_assets/render_assets.py`(SVG → 헤드리스 Edge, 투명 배경) → `Assets/UI Assets/{Card 14, Match 35, Currency 12, Common 8}` · `manifest.json`(크기 · 9-슬라이스 테두리 · Image Type · 틴트) · `contact_sheet.png`. 강화 배지는 바탕 · 테두리 · 안쪽 선 3장으로 분리(한 장으로는 두 색 틴트 불가). 재화 아이콘 12종 · 로딩 스피너 8프레임은 이때 새로 디자인. 9-슬라이스는 늘림 시뮬레이션으로 검증 |
| 52 (10-09) | **로드맵 9 경기 UI 구현 완료.** 화면 7장(타이틀 · 팀 선택 · 리그 · 경기 전 · 경기 중 · 일시정지 · 경기 후) · 위젯 10종 · 코어 9종을 코드 조립 방식으로 신설(2-6). 애셋 69종을 `Resources/UI/`로 옮기고 `manifest.json` 규격을 `.meta`에 직접 기록(PPU · 9-슬라이스 20종). `TMP Essential Resources` 추출 + OS 폰트 런타임 TMP 자산. 씬에 `UI`(`GameUiBootstrap`) 배치 → ▶ 한 번으로 플레이 가능. 관전 지원을 위해 `LeagueRunner`를 `BeginPlayerGame`/`CommitPlayerGame`으로 분리. 🔴 교체 모드가 리그 시작 시점에 고정되던 버그 수정(판정을 `LiveGameController`로 이전). 검증 PASS 57 / FAIL 0 |
| 54 (10-09) | **연출 전면 투입.** 트윈 코어 신설(`UiTween` · `UiAnimator` · `UiEase` · `UiEffects`)과 UI 네이티브 파티클(`UiEffectLayer` · `UiParticleSettings` 8종 프리셋 · Image 조각 160개 풀). 화면 전환 페이드 · 중계 행 슬라이드업 · 홈런 색종이 · 득점 배지 팝 · 스코어 플래시 · 도루 주자 이동 · 예약 스피너 8프레임 · 뽑기 순차 공개 · 카드 스케일 플립 · 강화/훈련/돌파/조합/제작/우승 파티클 · 로비 스태거 · 토스트 등장퇴장 (2-8). 파티클 기본 스프라이트 3종을 생성해 `Resources/UI/Fx/`에 넣었고 같은 이름으로 덮어쓰면 외부 애셋으로 교체된다. 🔴 Unity ParticleSystem은 Overlay 캔버스 위로 안 그려져 UI 네이티브로 구현. 검증 PASS 101 / FAIL 0 |
| 53 (10-09) | **UI 전체 구현 — 앱 실행부터 플레이 전반까지.** 화면 19장(타이틀 · 팀선택 · 로비 · 리그 · 가을야구 · 경기 4 · 뽑기 · 인벤토리 · 라인업 · 강화 · 훈련 · 조합 · 분해 · 골글제작 · 카드상세 · 카드선택) · 위젯 16종 · 코어 12종(2-7). 공용화: `CardDisplayInfo`(카드 1장 표시 정보 집약) · `CardPickerScreen`(5개 화면 공용 선택 모달) · `CardActionScreen`(육성 화면 공통 틀) · `CardView`/`CardCellView` 2단 분리 + `PlayerPhotoProvider` 12장 LRU. 🔴 `GachaResult.InstanceId` 신설 — 뽑기 화면이 방금 뽑은 카드를 특정할 수 없던 구멍. `Design/handoff/디자인_요청서.md`로 Desktop↔CLI 분업 고정. 검증 PASS 87 / FAIL 0 |
| 55 (10-11) | **Unity MCP 연결 후 첫 실행 QA.** ▶ 실행으로 전체 흐름 완주 · 이어하기 복원 · 경기 중 연출까지 눈으로 확인하고 레이아웃 결함 7건 수정(2-0-B). 바탕화면에 있던 카드 베이스 3종(`BaseImage_SS/SG/GG`, 1398×1957)을 `Resources/UI/Card` 로 임포트 — 카드가 명세 6-6 좌표대로 렌더되는 것을 확인했다. 에디트 모드 미리보기 도구(`UiPreviewBuilder`) 신설. Unity 버전 표기를 6000.3.6f1 로 정정(초기 커밋부터 그 버전이었고 문서만 틀려 있었다). QA 파이프라인 규칙 7가지 기록 |
