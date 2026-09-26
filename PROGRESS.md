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
| **9 경기 UI** | ⏸️ **UI 디자인 대기.** `GameSession`까지 준비 완료 |
| **11 튜토리얼** | ⏭️ 미착수 (최종 작업) |

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

### 2-1. 🔴 `StatBaseline`이 현재 카드풀과 어긋남 (작성자 판단 필요)

검증 하니스가 실측한 **유일한 미해결 항목**. 코드 결함이 아니라 데이터·튜닝 문제라 Claude가 건드리지 않았다.

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

**영향** (1000경기 실측): 타율 .256(기준 .270) / 장타율 .363(.401) / 경기당 득점 7.37(8.53).
타자 쪽 드리프트가 투수 쪽보다 커서 공격 지표가 일관되게 낮다.

**지금 상수를 낮추면 안 되는 이유**: 현재 CSV는 타자 159 / 투수 166장으로
**시그니쳐·골든글러브가 아직 미입력**(2-3의 5번)이다. 이들은 5성 고정이라 스탯이 높아,
입력되면 풀 평균이 다시 올라간다. **시그·골글 CSV 입력 후 재측정하는 것이 맞다.**

측정 방법은 3-3의 검증 하니스(`StatBaseline이 카드풀 평균과 일치` 항목)가 수치까지 찍어준다.

### 2-2. 미처리 호출부 대기표

검수로 public 시그니처가 바뀌어 아직 안 고친 곳. **그 파일 검수 차례에 함께 처리한다.**

**현재 비어 있다.** 세션 46부터 파급 호출부를 즉시 고치므로 이 표는 원칙적으로 쌓이지 않는다.

세션 46에서 바뀐 public 시그니처(호출부 모두 동반 수정 완료):
`CurrencyManager.Restore` `void`→`bool` / `StealCalculator.IsSuccess`에서 `GameState` 매개변수 제거 /
`TrainManager.GetMaxTrainLevel()`→`MaxTrainLevel` 프로퍼티 / `Hittersubstitution`→`HitterSubstitution`

### 2-3. Unity 에디터 작업 (코드 아님)

| # | 항목 | 비고 |
|---|---|---|
| 1 | `GachaManager` 인스펙터 — **천장 50** 확인 | `_pityLimit`은 씬에 직렬화된 적 없는 새 필드 |
| 2 | 같은 인스펙터 — 확률 6개 확인 | `_gradeSigProbabilitySig` → `_signatureChanceOnStar5` 개명으로 기존 값이 풀리고 코드 기본값 0.15 적용 |
| ~~3~~ | ~~`CombineManager` 씬 배치~~ | ✅ 세션 48에서 씬 YAML에 직접 배치 (매니저 **15종**). `_upgradeChanceStar3` 0.25 / `_upgradeChanceStar4` 0.1 / `_materialCount` 3 직렬화 완료 |
| 4 | `.meta` 생성 확인 | `GameSession` · `CombineManager` · `CombineResult` · `SingletonBehaviour` |
| 5 | 시그니쳐 · 골든글러브 마스터 CSV 입력 | 🔒 작성자. cardId **append-only** — 타자 `160~` / 투수 `50167~` |
| 6 | `StartingSignatureTable._entries` 채우기 | **5번이 끝나야 가능.** 현재 비어 있어 전 팀이 시작 카드 없이 출발 |
| 7 | `Minor1~Legend3` `_statBonus` 밸런스 | 🔒 작성자. 현재 9티어 전부 `10`이라 티어가 올라도 AI가 안 강해짐 (근거는 5-3) |
| 8 | `TrainManager` 인스펙터 — `_statPointPerTrain` **2** 확인 | 씬에 직렬화된 적 없는 새 필드 |
| 9 | `BreakthroughManager` 인스펙터 — 비용 5개 확인 | 필드 개명(`_breakthroughCardCost_X` → `_xCost`)으로 씬 값이 풀린다. 코드 기본값은 기획서 확정치(50/20/10/5/3)와 동일 |
| 10 | `LeagueManager` 인스펙터 — 강화 전용 카드 등급 분배 확인 | `_star3RewardPercent` 60 / `_star4RewardPercent` 30. 씬에 직렬화된 적 없는 새 필드 |
| 11 | `.meta` 생성 확인 | `ProbabilityModels/RunnerLookup` · `Simulation/HitterSubstitution`(파일명 대소문자 변경) |
| 12 | 매니저 15종 스크립트 참조 확인 | 7개가 `SingletonBehaviour<T>` 상속으로 바뀜 — 씬 컴포넌트는 그대로지만 한 번 열어 확인 |
| 13 | 🔒 시그니쳐·골글 CSV 입력 후 `StatBaseline` 재측정 | 2-1 참고. 하니스가 수치를 찍어준다 |
| 14 | 🔒 `LeagueManager` 인스펙터 — **포스트시즌 보상 5칸** | 세션 48 신설. 와일드카드 / 준PO / PO / 한국시리즈 / **우승** 각각 골드·골글포인트·시그권·골카전용. **전부 0이라 지금은 아무것도 지급되지 않는다** |
| 15 | 🔒 `GoldenGloveCraftManager` 제작 비용 | 씬 값은 확정치(50,000 / 1,000,000 / 100)라 정상. 코드 기본값만 낮게 남아 있다 — 작성자가 플레이하며 조정 |

### 2-4. 코드 쪽 대기

| 항목 | 상태 |
|---|---|
| 로드맵 9번 경기 UI | ⏸️ UI 디자인 대기. 화면 4개 구성은 확정(4-6), `GameSession` 준비 완료 |
| `LiveGameController` 씬 배치 + `LeagueManager.SetInterruptHandler` 호출 | ⏸️ **주입 경로는 세션 48에서 뚫어 뒀다.** UI 착수 시 컨트롤러를 만들어 연결만 하면 수동 교체가 리그·포스트시즌에 걸린다 |
| 무승부 상대전적 표시 (`TeamRecord._drawsAgainst`) | ⏸️ UI에 "1무" 자리가 있는지 확정 후 판단 |
| 로드맵 11번 튜토리얼 | ⏭️ 최종 |

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
- **컴파일 검증 하니스** (세션 46 신설): 스크래치패드에 `UnityShim.cs`(UnityEngine 최소 셰임) + `harness.csproj`를 두고
  `dotnet build -p:GameRoot=<프로젝트 경로>`로 `Assets/Scripts/**` 전체를 컴파일한다. Unity를 켜지 않고 오류를 잡는다.
  셰임이 덮는 범위: `MonoBehaviour` · `ScriptableObject` · `Debug` · `Mathf` · `Random` · `JsonUtility` · `Application` · `Resources` · `TextAsset` + 직렬화 속성 8종
- 하니스 함정: `SingletonBehaviour.Awake`는 `Instance`가 있으면 조기 반환한다. 섹션마다 새 매니저를 만들려면
  `<Instance>k__BackingField`를 리플렉션으로 null로 밀어야 한다. 미전환 매니저는 백킹 필드가 자기 자신에 있으므로 `BaseType`을 따라 올라가며 찾는다

### 3-4. 코딩 컨벤션 (CLAUDE.md 보충)

- `if` 중괄호: 본문 **한 줄이면 생략, 두 줄 이상이면 사용**
- 주석: **public은 `<summary>` 2줄 이내 / private는 `//` 1줄 이내 / 메서드 내부 주석 금지** (세션 46 확정)
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
| `Minor1~Legend3` | 144 | **10 (🔒 조정 대기)** | 60,000 ~ 250,000 |

- 골글 포인트는 **프로1부터** 지급 (500~5,000) — 골글 제작을 중후반으로 게이팅
- 경기당 보상 확률 50~100%. **`Basic1`의 0%는 의도된 값** — 9경기짜리 첫 리그는 맛보기라 육성 재료를 주지 않는다
- 경기당 보상은 **승패와 무관하게 지급** — 못 이기는 구간에서 보급이 끊기면 영영 따라잡을 수 없다

### 5-3. 🔒 `Minor1~Legend3` 밸런스 — 작성자 담당

Claude는 이 구간 수치를 건드리지 않는다. 근거만 남긴다.

- 실측 경험식: **`AI _statBonus ≈ 플레이어 스탯 증가분 + 1.5`** 에서 승률 .540 (강화 레벨 N = 전 스탯 +2N)
- 현재 9티어가 전부 `_statBonus` 10 → 8단계가 같은 경험의 반복이고, 강화 6이면 이미 .731
- 강화 10(+20) 플레이어를 .540에 묶으려면 `_statBonus ≈ 21` 필요. 훈련(총 +50)까지 얹으면 더 올라간다
- 육성이 티어 난이도를 압도해 **"불가능 → 자명" 사이의 놀 수 있는 구간이 좁다**

### 5-4. 재화 · 제작 비용

**골든글러브 제작** (작성자 확정 — 엔드 콘텐츠이므로 쉽게 만들 수 없어야 함)

| 제작 | 골글 포인트 | 포인트 | 훈련 카드 |
|---|---|---|---|
| 일반 (전체 랜덤) | 50,000 | 1,000,000 | 100 |
| 팀 선택 | 100,000 | 5,000,000 | 300 |

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
