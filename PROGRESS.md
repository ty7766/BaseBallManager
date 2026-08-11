# PROGRESS.md — 베이스볼 매니저 진행상황

## ✅ 완료

### 세션 1 (2026-06-13) — 프로젝트 초기 설계
- CLAUDE.md / 기획서 검토 및 맥락 파악
- CSV 포맷 확정 (BatterCards / PitcherCards 분리)

**확정된 CSV 포맷**

`BatterCards.csv`
```
cardId,name,team,year,cardType,grade,position,power,contact,run,defense
```

`PitcherCards.csv`
```
cardId,name,team,year,cardType,grade,position,velo,stuff,control,stamina
```

📝 주요 설계 결정:
- cardType: N(노말) / S(시그니쳐) / G(골든글러브)
- cardId: 두 파일 통틀어 연속 번호 (타자 먼저, 투수 이어서)
- CSV 파싱: 헤더 기반 (컬럼 순서 무관)
- 기존 CardStats.csv는 폐기 → 두 파일로 교체 예정

---

### 세션 2 (2026-06-14) — 카드 마스터 데이터 모델 완성

**완성된 파일 목록**
- `Assets/Scripts/Cards/CardType.cs` — enum CardType { Normal, Signature, GoldenGlove }
- `Assets/Scripts/Cards/CardGrade.cs` — enum CardGrade { Star3=3, Star4=4, Star5=5 }
- `Assets/Scripts/Cards/CardMasterData.cs` — 추상 기반 클래스
- `Assets/Scripts/Cards/HitterMasterData.cs` — 타자 마스터 데이터
- `Assets/Scripts/Cards/PitcherMasterData.cs` — 투수 마스터 데이터

📝 주요 설계 결정:
- 모든 필드는 `public { get; private set; }` — 외부 읽기 허용, 생성 후 불변
- 값 주입은 생성자(constructor)로만 — CSV 로더가 `new HitterMasterData(...)` 호출
- 자식 생성자는 `base(...)` 로 부모 필드를 위임
- `GetStatSum()` 은 `protected abstract` → 자식에서 각 스탯 합산 구현
- `CalculateOVR()` = `GetStatSum() / 4` (정수 나눗셈, 소수점 버림 — 기획서 확정)

---

### 세션 3 (2026-06-15) — CSV 로더 구현 완성

**완성된 파일 목록**
- `Assets/Scripts/CSVParse/CardCSVLoader.cs` — CSV 파싱 로더

**완성된 메서드 구조**
- `ReadCSV(resourcePath)` — Resources.Load + CP949 디코딩
- `ReadAllLines(resourcePath)` — ReadCSV + Split('\n') 묶음
- `ParseHeaders(headerLine)` — 헤더 → Dictionary<string, int>
- `ParseCardType(value)` — "N"/"S"/"G" → CardType enum
- `ParseCardGrade(value)` — "3"/"4"/"5" → CardGrade enum
- `ParseBaseCardData(cols, headers)` — 7개 공통 컬럼 튜플 반환
- `LoadHitters()` — HitterCards.csv → List\<HitterMasterData\>
- `LoadPitchers()` — PitcherCards.csv → List\<PitcherMasterData\>

📝 주요 설계 결정:
- 인코딩: **UTF-8** (`Encoding.UTF8`) — 구글 스프레드시트 CSV 내보내기 기본값
- 파일 관리: **구글 스프레드시트**로 편집 → CSV 내보내기 → `Assets/Resources/Data/` 에 배치 (xlsx 방식 폐기)
- `TextAsset.bytes` 사용 — `.text`는 Unity가 UTF-8로 해석해버리므로 raw 바이트로 받아 직접 디코딩
- 파일명: `BatterCards` → `HitterCards` 로 변경 (C# 클래스명 `HitterMasterData`와 일관성)

---

### 세션 4 (2026-06-28) — CSV 배치 & 로더 동작 확인

- `Assets/Resources/Data/HitterCards.csv` / `PitcherCards.csv` 배치 완료
- `Assets/ScriptsTest/CSVParseTest.cs` 작성 → Unity 플레이 모드에서 정상 로드 확인
- Name / OVR / TeamName 출력 동작 확인

---

### 세션 4 계속 (2026-06-28) — CardInstance 설계 완성

**완성된 파일**
- `Assets/Scripts/Cards/CardInstance.cs`

📝 주요 설계 결정:
- `cardId`만 참조 (CardMasterData 직접 참조 X) — 직렬화 단순화, 역할 분리
- 전 필드 `{ get; private set; }` — 외부 읽기 허용, 수정 차단
- 생성자 매개변수: `instanceId`, `cardId` 두 개만 / 나머지는 초기값 고정
- `trainDelta = new int[4]` — 훈련 스탯 분배값 4종 (같은 레벨이어도 분배 다를 수 있음)

---

### 세션 5 (2026-07-03) — OVR CSV 이관 & CardDataManager 완성

**변경 사항**
- OVR을 C# 코드 계산(`CalculateOVR`) → CSV에서 직접 파싱으로 변경
  - `GetStatSum()` / `CalculateOVR()` 제거
  - `CardMasterData`에 `OVR { get; private set; }` 추가, 생성자로 주입
  - `HitterMasterData` / `PitcherMasterData` — `GetStatSum()` override 제거, 생성자에 `ovr` 추가
  - `CardCSVLoader.ParseBaseCardData` — `headers["OVR"]` 파싱 추가
  - CSV 컬럼: 스탯 오른쪽에 `OVR` 열 추가 (Google Sheets에서 `=INT(AVERAGE(...))` 권장)

**완성된 파일**
- `Assets/Scripts/Cards/CardDataManager.cs` — 싱글톤 MonoBehaviour

📝 주요 설계 결정:
- 싱글톤 MonoBehaviour + `DontDestroyOnLoad` (씬 추가 대비)
- `Dictionary<int, HitterMasterData>` / `Dictionary<int, PitcherMasterData>` 분리 — 타입 안전, O(1) 조회
- `LoadCardMasterData()` — Awake 내 `if (Instance == null)` 블록 안에서만 호출
- `GetHitter(id)` / `GetPitcher(id)` — `TryGetValue` + `LogWarning` + `null` 반환

---

---

### 세션 6 (2026-07-05) — 인벤토리 시스템 완성

**완성된 파일**
- `Assets/Scripts/Cards/CardGrade.cs` — `None = 0` 추가
- `Assets/Scripts/Cards/CardType.cs` — `None = 0` 추가
- `Assets/Scripts/Inventory/PlayerTypeFilter.cs` — enum { All, HitterOnly, PitcherOnly }
- `Assets/Scripts/Inventory/CardFilter.cs` — 필터 조건 데이터 클래스
- `Assets/Scripts/Cards/CardInstance.cs` — `SetLocked(bool)` 추가
- `Assets/Scripts/Cards/CardDataManager.cs` — `GetCardMasterData(int)` 추가
- `Assets/Scripts/Inventory/InventoryManager.cs` — 싱글톤 MonoBehaviour 완성

**완성된 메서드 목록 (InventoryManager)**
- `AddCard(int cardId)` — 한도 초과 시 차단
- `RemoveCard(int instanceId)` — instanceId로 카드 찾아 제거
- `SetLocked(int instanceId, bool locked)` — 잠금 상태 변경
- `ExpandCapacity(int amount)` — 보유 한도 확장 (0 이하 방어)
- `GetFiltered(CardFilter filter)` — 4개 조건 필터링 (PlayerType / Grade / Type / TeamName)

📝 주요 설계 결정:
- nullable(`bool?`, `CardGrade?`) 대신 enum + `None = 0` 센티넬 방식 — 박싱/GC 회피
- `PlayerTypeFilter` enum 별도 파일 분리 — SRP
- `Count`, `IsFull` 계산 프로퍼티(`=>`) — 별도 변수 관리 없이 항상 실시간 계산
- `_maxCapacity` `[SerializeField]` 노출 — 인스펙터에서 조정 가능
- `GetCardMasterData` — 타자/투수 구분 없이 공통 마스터 데이터 조회, 경고 스팸 방지
- `GetFiltered` — `is HitterMasterData`로 타입 판별, early continue 패턴

---

### 세션 7 (2026-07-06) — 기획서 최종본 업데이트 + GitHub 마일스톤 생성

📝 주요 변경/확정 사항:
- `8.6` 신규: 교체 모드(자동/수동) + 일시정지 인터럽트(1/3이닝 경계), 대타·투수 교체 규칙
- 야수 **벤치 5칸** 확정 (선택 슬롯, 6.3)
- **10연 천장** 확정 — 4성 이상 1장 보장 (3장)
- **강화·훈련·돌파 되돌리기 불가** 명시
- **KBO 승률** 계산 방식 확정 — 무승부 제외 (7.7)
- **AI 티어별 능력치 상승** 확정 (수치 TBD)
- 골글 포인트 획득 경로 확정 — 리그 보상 + 골글 분해
- `ISaveStorage` 추상화 + 단일 세이브 슬롯 확정 (10장)
- cardId 대역(타자 1~50000 / 투수 50001~) + 하드코딩 금지 명시
- 투수 역할(SP/RP/CP) CSV 고정, 해당 슬롯에만 배치 명시
- 카드 UI & 렌더링 파이프라인 → 별도 문서 예정 (오프라인 누끼 전처리 + 런타임 레이어 합성)

**GitHub 마일스톤 생성 (12개)**
- 데이터 레이어 / 인벤토리 / 뽑기 / 강화 / 훈련 / 라인업
- 경기 시뮬레이션 / 리그 / UI / 골든글러브 제작 / 카드 분해 / 튜토리얼

---

### 세션 8 (2026-07-08) — 뽑기 시스템 핵심 로직 완성

**완성된 파일 목록**
- `Assets/Scripts/Gacha/GachaType.cs` — enum { Normal, Signature }
- `Assets/Scripts/Gacha/GachaResult.cs` — 1회 뽑기 결과 데이터 (CardId / Grade / Type)
- `Assets/Scripts/Gacha/GachaManager.cs` — 싱글톤 MonoBehaviour
- `Assets/Scripts/Cards/CardDataManager.cs` — GetAllHitters() / GetAllPitchers() 추가

**완성된 메서드 목록 (GachaManager)**
- `Roll1(gachaType)` — 인벤 꽉 찼으면 차단, RollOnce 후 AddCard
- `Roll10(gachaType)` — 10장 공간 체크, 10회 RollOnce, 10연 천장, 일괄 AddCard
- `RollOnce(gachaType)` — DecideGrade → PickCardFromPool → 천장 카운터 증가 → GachaResult 반환
- `DecideGrade(gachaType)` — 확률표 기반 누적 비교로 등급 결정
- `PickCardFromPool(gachaType, grade)` — 등급·타입 필터 후 랜덤 cardId 반환
- `IncrementAndCheckPity(gachaType)` — 종류별 카운터 증가, 50 도달 시 리셋 후 true

📝 주요 설계 결정:
- 확률 필드 `[SerializeField]` 노출 — 인스펙터에서 튜닝 가능
- 시그니쳐 5성 분기는 `PickCardFromPool` 내부에서 처리 (`_gradeSigProbabilitySig = 0.15f`)
- 10연 천장 강제 등급: 뽑기 타입별 Star4/Star5 상대 비율로 결정
- 50연 천장 카운터는 세이브 연동 예정 (현재 메모리에만 존재)

---

### 세션 9 (2026-07-08) — 뽑기 시스템 완성 + PlayerDataManager

**완성된 파일 목록**
- `Assets/Scripts/Player/PlayerDataManager.cs` — 플레이어 선택 팀 보관 싱글톤
- `Assets/Scripts/Gacha/GachaManager.cs` — 50연 천장 처리 추가

**추가/수정된 내용**
- `PlayerDataManager`: `PlayerTeamName { get; private set; }` + `SetPlayerTeam(string)` (IsNullOrEmpty 방어)
- `GachaManager.RollOnce()`: `IncrementAndCheckPity` true 시 `PickTeamConfirmedCard()` 호출 → cardId + grade 교체
- `GachaManager.PickTeamConfirmedCard()`: 자기 팀 + 타입 + 5성 필터로 풀 구성 후 랜덤 반환

📝 주요 설계 결정:
- 50연 천장 발동 시 grade도 Star5로 강제 교체 — 팀 확정 카드는 항상 5성이므로 GachaResult 등급 정합성 보장
- PlayerDataManager를 별도 분리 — 팀 정보는 리그·AI 배치 등 다른 시스템에서도 필요하므로 SRP 적용

---

### 세션 10 (2026-07-09) — 강화 시스템 부분 구현

**완성된 파일 목록**
- `Assets/Scripts/Enhance/EnhanceManager.cs` — 싱글톤 MonoBehaviour (부분 완성)
- `Assets/Scripts/Inventory/InventoryManager.cs` — `GetCard(int)` / `GetAllCards()` 추가

**완성된 메서드 목록 (EnhanceManager)**
- `CanEnhance(targetInstanceId)` — 카드 존재 여부 + 최대 강화 레벨 체크
- `GetIdenticalCards(targetInstanceId)` — 동일 종류·이름·팀 카드 목록 반환 (잠금 카드·자기 자신 제외)

📝 주요 설계 결정:
- `GetAllCards()` 반환 타입 `IReadOnlyList<CardInstance>` — 외부 수정 차단
- 컬렉션 반환 메서드는 `null` 대신 빈 리스트 반환 (호출자 null 체크 불필요)
- 강화 전용 카드(5장) 경로는 CardType 확장 후 추가 예정. 현재는 동일 카드 1장 경로만 구현

---

### 세션 11 (2026-07-10) — 강화 시스템 완성

**완성된 파일 목록**
- `Assets/Scripts/Cards/CardInstance.cs` — `ApplyEnhance()` 구현
- `Assets/Scripts/Enhance/EnhanceManager.cs` — `ValidateMaterials` / `Enhance` 완성

**완성된 메서드 목록**
- `CardInstance.ApplyEnhance()` — `EnhanceLevel++`
- `EnhanceManager.ValidateMaterials(target, materials)` — 재료 유효성 최종 검사
- `EnhanceManager.Enhance(targetInstanceId, materialInstanceIds)` — 강화 전체 흐름 (ID→인스턴스 변환 → 검증 → 재료 소멸 → ApplyEnhance)

📝 주요 설계 결정:
- `ApplyEnhance()`를 `CardInstance`에 둔 이유 — `EnhanceLevel`이 `private set`이라 외부에서 직접 변경 불가. Tell, Don't Ask 원칙
- `ValidateMaterials`는 강화 직전 최종 방어선 — `GetIdenticalCards`(UI 후보 조회)와 역할 분리
- `materials.Count != 1` 체크 — 현재 동일 카드 1장 경로만 지원, 전용 카드 5장 경로 추가 시 조건 확장 예정

---

### 세션 12 (2026-07-11) — 훈련 시스템 완성

**완성된 파일 목록**
- `Assets/Scripts/Cards/CardInstance.cs` — `ApplyTrain()` / `ApplyBreakthrough()` 구현, `_trainDelta` private 필드 + `IReadOnlyList<int>` 노출로 변경
- `Assets/Scripts/Train/TrainManager.cs` — 싱글톤 MonoBehaviour 완성
- `Assets/Scripts/Train/BreakthroughManager.cs` — 싱글톤 MonoBehaviour 완성

**완성된 메서드 목록 (TrainManager)**
- `CanTrain(instanceId)` — 카드 존재 여부 + 돌파 여부 기반 상한(30/50) 체크
- `GetTrainCost(trainLevel)` — 레벨 비례 포인트·훈련 카드 비용 반환 (튜플)
- `Train(instanceId)` — `for 2회 Random(0..3)` 랜덤 분배 → `ApplyTrain(delta)` 호출
- `GetMaxTrainLevel()` — `_maxTrainLevel` 외부 노출 (BreakthroughManager 참조용)

**완성된 메서드 목록 (BreakthroughManager)**
- `CanBreakthrough(instanceId)` — 카드 존재 여부 + 이미 돌파 여부 + TrainLevel == 30 체크
- `GetBreakthroughCost(instanceId)` — CardType·CardGrade switch expression으로 필요 돌파 카드 수 반환
- `Breakthrough(instanceId)` — `CanBreakthrough` 검증 → `ApplyBreakthrough()` 호출

📝 주요 설계 결정:
- `_trainDelta`를 `private int[]` + `public IReadOnlyList<int> TrainDelta =>` 로 분리 — `private set`만으로는 배열 요소 외부 수정을 막을 수 없어 요소 쓰기를 컴파일 타임 차단
- `TrainManager` / `BreakthroughManager` SRP 분리 — 훈련(레벨업)과 돌파(상한 해금)는 다른 책임
- 돌파 카드 비용 차감 미구현 (`CurrencyManager` 추후 연동)
- `GetBreakthroughCost`에서 `instanceId` → `CardId` 경유해 마스터 데이터 조회 (`instanceId`와 `cardId`는 다른 개념)

---

### 세션 13 (2026-07-12) — 코드 스타일 확정 + 라인업 시스템 착수

**CLAUDE.md 추가 항목**
- 3-2. 오류 처리 3원칙 (LogWarning / LogError / throw 구분 기준)
- 3-3. switch expression 사용 기준 (단순 반환 → expression, 로직 포함 → 문)
- 3-4. 멤버 선언 순서 확정
- 코드 확인 시 Read 툴로 직접 읽기 (붙여넣기 요청 금지) 명문화

**완성된 파일 목록**
- `Assets/Scripts/Line Up/Hitters/HitterPosition.cs` — enum + HitterPositionParser (Parse / TryParse)
- `Assets/Scripts/Line Up/Pitchers/PitcherPosition.cs` — enum (SP/RP/CP) + PitcherPositionParser (Parse / TryParse)
- `Assets/Scripts/Line Up/LineUpManager.cs` — 싱글톤 MonoBehaviour (뼈대 + Awake 초기화 + AssignHitter 완성)

**완성된 메서드 목록 (LineUpManager)**
- `Awake()` — 싱글톤 + 슬롯 전체 초기화 (_hitterSlots 9칸 / _benchSlots 5칸 / _pitcherSlots SP5·RP5·CP1, 전부 -1)
- `AssignHitter(slot, instanceId, battingOrder)` — 카드 존재 · 타자 여부 · 타순 범위 · 중복 배치 · 포지션 일치(DH 예외) 검증 후 배치

📝 주요 설계 결정:
- 폴더명: `Line Up/Hitters/`, `Line Up/Pitchers/` (사용자 선택)
- 포지션 enum: `FB/SB/TB` (FirstBase/SecondBase/ThirdBase 약어) — 통일성 우선
- 투수는 `PitcherRole` 대신 `PitcherPosition`으로 통일 — 타자/투수 네이밍 일관성
- 야수 슬롯: `Dictionary<HitterPosition, (int instanceId, int battingOrder)>` (안 B)
- 포지션 불일치 시 LogWarning 없이 silent return false — UI에서 사전 필터링하므로 도달 불가능한 경로

---

### 세션 14 (2026-07-16) — 라인업 시스템 완성

**완성된 파일 목록**
- `Assets/Scripts/Line Up/LineUpManager.cs` — 전체 메서드 구현 완료

**완성된 메서드 목록 (LineUpManager)**
- `IsCardAssigned(instanceId)` — 야수/벤치/투수 슬롯 전체 순회, 중복 배치 여부 반환
- `RemoveHitter(slot)` — 슬롯 비어있는지 체크 후 (-1, 0) 초기화
- `AssignBench(benchIndex, instanceId)` — 인덱스 범위 · 슬롯 · 카드 존재 · 타자 여부 · 중복 배치 검증 후 배치
- `RemoveBench(benchIndex)` — 인덱스 범위 · 슬롯 비어있는지 체크 후 -1 초기화
- `AssignPitcher(position, slotIndex, instanceId)` — 인덱스 범위 · 슬롯 · 카드 존재 · 투수 여부 · 포지션 일치 · 중복 배치 검증 후 배치
- `RemovePitcher(position, slotIndex)` — 인덱스 범위 · 슬롯 비어있는지 체크 후 -1 초기화
- `SetBattingOrder(slot, order)` — 슬롯 비어있는지 · 타순 범위 체크 후 instanceId 유지, battingOrder만 교체
- `IsLineupComplete()` — 야수 9 + 투수 11 슬롯 전부 -1 없으면 true (벤치 제외)

---

---

### 세션 15 (2026-07-17) — 시뮬레이션 데이터 구조 착수

**브랜치**: `feature/simulation-data-structures`

**브랜치 분할 계획 (로드맵 6번)**
- `simulation-data-structures` — 상태 클래스 설계 (현재)
- `simulation-atbat` — 타석 판정 (8.2)
- `simulation-baserunning` — 진루 처리 (8.3)
- `simulation-pitcher` — 투수 체력·교체 (8.4)
- `simulation-game-loop` — 경기 루프 통합 (8.1)

**완성된 파일 목록**
- `Assets/Scripts/Simulation/HitterSnapshot.cs` — 타자 유효 스탯 스냅샷 (struct)
- `Assets/Scripts/Simulation/PitcherSnapshot.cs` — 투수 유효 스탯 스냅샷 (struct)
- `Assets/Scripts/Simulation/SimulationBatterLog.cs` — 타석 1회 결과 로그 (enum BatterOutcome + class)

📝 주요 설계 결정:
- 스냅샷을 struct로 분리 — 시뮬 코어가 Cards 레이어에 의존하지 않도록, 강화·훈련 반영 최종값만 주입
- `SimulationBatterLog`는 경기 단위 임시 데이터 — 다음 경기 시작/나가기 시 폐기
- 시즌 누적 기록(`HitterSeasonStats` 등)은 별도 구조로 추후 구현 예정

---

### 세션 16 (2026-07-19) — 시뮬레이션 데이터 구조 완성

**완성된 파일 목록**
- `Assets/Scripts/Simulation/SimulationContext.cs` — 경기 시작 시 구성되는 불변 입력 데이터 (양 팀 타자/투수 스냅샷 배열, 홈/원정 여부)
- `Assets/Scripts/Simulation/PitcherState.cs` — 현재 등판 투수의 경기 내 상태 (체력, 투구 수, 이닝 실점)
- `Assets/Scripts/Simulation/GameState.cs` — 경기 진행 중 변하는 전체 상태 (이닝, 아웃, 베이스, 득점, 타순, 투수 상태, 경기 종료 여부)

📝 주요 설계 결정:
- `SimulationContext` — 불변 입력 데이터. 배열 길이 검증(타자 9명/투수 7명)을 생성자에서 `ArgumentException`으로 방어
- `PitcherState.GetFatigueRatio()` — `(float)CurrentStamina / Snapshot.Stamina`. 피로 패널티(8.4) 계산의 기반값
- `PitcherState.ConsumePitches()` — `Math.Max(0, ...)` 로 체력 음수 방지
- `GameState.AddOut()` — 3아웃 시 잔루 소멸(-1 초기화) + 이닝 전환 + 투수 `ResetInningStats()` 호출까지 캡슐화
- `GameState.AddRun()` — 9회 이상 말이닝 득점 후 홈팀 리드 시 즉시 끝내기(`IsGameOver = true`)
- 베이스 주자는 `bool` 대신 `int instanceId`(-1=없음) — 진루 처리 시 주자 스탯 조회가 필요하므로

---

### 세션 17 (2026-07-19) — 타석 확률 모델 구현

**브랜치**: `feature/Simulation_BattingProbabilitySystemModel`

**완성된 파일 목록**
- `Assets/Scripts/ProbabilityModels/BatterOutcomeCalculator.cs` — 타석 결과 확률 산출 클래스

**완성된 메서드 목록**
- `Calculate(state, hitter, pitcher, avgDefense)` — 삼진→볼넷→홈런→실책→안타/아웃 순 단계별 탈락 판정, 최종 BatterOutcome 반환
- `CalcStrikeOutProb()` — 투수 구위/구속 vs 타자 정확, 범위 5~40%
- `CalcWalkProb()` — 타자 정확 vs 투수 제구, 범위 2~20%
- `CalcHomeRunProb()` — 타자 파워 vs 투수 구위/구속, 범위 0.5~30%
- `CalcHitProb()` — 타자 정확 vs 투수 구위 (BABIP), 범위 15~45%
- `CalcErrorProb(avgDefense)` — 수비팀 평균 수비 기반, 범위 0.3~3%
- `Roll(probability)` — 0~1 난수로 확률 판정

📝 주요 설계 결정:
- `ProbabilityModels/` 폴더 신설 — 스탯→확률 변환 클래스 전용 (시뮬레이션 상태 클래스와 분리)
- 안타 종류 분배: 파워/주루 기반 `longHitBonus` 보정으로 장타형/컨택형 타자 차별화. 난수 하나로 구간 분배 (3루타/2루타/단타)
- 아웃 종류 분배: `canDoublePlay` 조건(1루 주자 + 2아웃 미만) 판단 후 누적 분기
- 희생플라이: 뜬공 판정 시 `state.ThirdBase != -1 && state.OutCount < 2` 조건 체크
- 고정 상수(0.22f 등)는 기획서 초안 수치 — 시뮬 루프 완성 후 KBO 평균 지표 기준 튜닝 예정

### 세션 18 (2026-07-20) — 진루 처리 착수

**완성된 파일 목록**
- `Assets/Scripts/ProbabilityModels/BaseRunningCalculator.cs` — 뼈대 + 보조 메서드 구현

**완성된 메서드**
- `Score(state)` — `state.AddRun()` 위임
- `TryAdvance(runner)` — 주루 스탯 기반 추가 진루 확률 판정 (`pRun = clamp(0.40 + 0.6 * (주루̂ - 0.5), 0.25, 0.90)`)

**미완성**
- `Apply()` — 타구 종류별 베이스/득점/아웃 갱신 로직 미구현

---

### 세션 19 (2026-07-23) — 진루 처리 Apply() 부분 구현

**수정된 파일 목록**
- `Assets/Scripts/Simulation/GameState.cs` — `SetFirstBase()` / `SetSecondBase()` / `SetThirdBase()` 추가
- `Assets/Scripts/ProbabilityModels/BaseRunningCalculator.cs` — Apply() 시그니처 수정 + 부분 구현

**Apply() 시그니처 수정**
- `SimulationContext context` 매개변수 추가 — 주자 스냅샷 조회를 위해

**완성된 헬퍼 메서드**
- `FindRunnerSnapshot(instanceId, context, isTopInning)` — 공격팀 라인업에서 instanceId로 스냅샷 탐색, 미발견 시 `default` 반환

**완성된 Apply() 케이스**
- `HomeRun` — 전원 득점 + 베이스 전체 클리어
- `Triple` — 전원 득점 + 타자 3루
- `Double` — 3루/2루 주자 득점, 1루 주자 `TryAdvance`(득점 or 3루), 타자 2루
- `Single` — 3루 주자 득점, 2루 주자 `TryAdvance`(득점 or 3루), 1루 주자 `TryAdvance`(3루 or 2루, 베이스 충돌 방지 포함), 타자 1루
- `Walk` — 밀어내기만 (TryAdvance 없음), 만루 시 3루 주자 득점

**미완성 케이스**
- `Error` / `SacrificeFly` / `DoublePlay` / `StrikeOut` / `GroundOut` / `FlyOut`

📝 주요 설계 결정:
- 베이스 이동 순서: 기존 주자 역순(3루→2루→1루) 처리 후 타자 배치 — 덮어쓰기 오류 방지
- `Walk`는 `state.SecondBase` / `state.FirstBase` 값을 그대로 이동 — `FindRunnerSnapshot` 불필요
- 2루 주자 처리 후 `SetSecondBase(-1)` 클리어 필수 — 1루 주자 없을 때 잔류 버그 방지
- 1루 주자 TryAdvance 성공 시 `state.ThirdBase == -1` 확인 후 분기 — 베이스 충돌 방지

---

### 세션 20 (2026-07-25) — 진루 처리 Apply() 완성

**수정된 파일 목록**
- `Assets/Scripts/ProbabilityModels/BaseRunningCalculator.cs` — Apply() 전체 케이스 완성

**완성된 Apply() 케이스 추가**
- `StrikeOut` / `GroundOut` / `FlyOut` — `state.AddOut()` 한 번 (주자 진루 없음)
- `SacrificeFly` — `state.AddOut()` + `TryAdvance`로 태그업 판정, 성공 시만 득점 + 3루 클리어
- `DoublePlay` — `SetFirstBase(-1)` + `state.AddOut()` 두 번 (1루 주자·타자 동시 아웃, 나머지 주자 정지)
- `Error` — Walk와 동일한 강제 +1베이스 로직

📝 의도적 미구현 항목 (향후 고도화 시 재검토):
- FlyOut 시 1·2루 주자 태그업 시도 — SacrificeFly(3루 태그업)로 단순화
- 태그업 중 아웃 — TryAdvance는 Safe/Stay만 반환, Out 없음
- 에러 후 2베이스 이상 추가 진루 — 강제 +1베이스로 단순화
- 삼중살 — 발생 빈도 극히 낮아 생략
- 유기적 수비 시나리오(병살 중 에러 등) — 현 구조에서 표현 불가, 생략

## ✅ 완료

로드맵 5번: **라인업 시스템** — LineUpManager 구현 완료

로드맵 6번 1단계: **시뮬레이션 데이터 구조** — `feature/Simulation_Data-Structure` 브랜치 완료

로드맵 6번 2단계: **타석 확률 모델** — `BatterOutcomeCalculator` 구현 완료

로드맵 6번 3단계: **진루 처리** — `BaseRunningCalculator` 구현 완료 (`feature/Simulation_BattingProbabilitySystemModel` 브랜치)

### 세션 21 (2026-07-25) — 투구 수 산출 구현

**브랜치**: `feature/Simulation_PitchingProbabilitySystemModel`

**완성된 파일**
- `Assets/Scripts/ProbabilityModels/PitchCountCalculator.cs` — 타석당 투구 수 산출

**완성된 메서드 (PitchCountCalculator)**
- `Calculate(outcome, hitter, pitcher)` — 기본 투구 수 + 파울 수 합산 반환
- `CalcBasePitches(outcome)` — 결과별 기본 투구 수 (삼진 3구/볼넷 4구/인플레이 가중 랜덤 1~5구)
- `CalcFouls(hitter, pitcher)` — 파울 발생 확률(`foulChance = clamp(0.28 + 0.40 * (정확̂ - 구위̂), 0.10, 0.55)`) 기반 반복 판정, 상한 18구

📝 주요 설계 결정:
- `PitchCountCalculator`는 순수 C# 클래스 — 타석 결과·스냅샷만 받아 투구 수 계산 (시뮬 상태에 무의존)

---

### 세션 22 (2026-07-29) — 투수 피로 패널티 + 교체 판단 부분 구현

**완성된 파일**
- `Assets/Scripts/Simulation/PitcherState.cs` — `ConsumePitches()` / `GetFatiguedSnapshot()` 추가
- `Assets/Scripts/ProbabilityModels/PitcherChangeEvaluator.cs` — 생성자 + `ShouldChange()` 구현

**완성된 메서드 (PitcherState)**
- `ConsumePitches(pitchCount)` — 투구 수 누적 + `Math.Max(0, ...)` 로 체력 차감 (음수 방지)
- `GetFatiguedSnapshot()` — staminaRatio 기반 피로 계수(fatigue) 계산 후 Velo/Stuff/Control에 적용한 새 PitcherSnapshot 반환. 체력 50% 초과 시 패널티 없음, 이하 시 Mathf.Lerp(1.0, 0.80, t)

**완성된 메서드 (PitcherChangeEvaluator)**
- `ShouldChange(pitcherState, gameState)` — 체력(+1/+2) + 이닝 실점 + 득점권 위기(+1) 점수 합산, forcePull 또는 _pullThreshold 이상 시 true 반환

📝 주요 설계 결정:
- `PitcherChangeEvaluator`는 순수 C# 클래스 유지 — 생성자로 `_pullThreshold` 주입 (기본값 3)
- Inspector 튜닝은 나중에 만들 GameSimulator(MonoBehaviour)에서 `[SerializeField]`로 노출 후 생성자에 전달

---

### 세션 23 (2026-08-01) — 불펜 운영 로직 완성

**완성된 파일**
- `Assets/Scripts/ProbabilityModels/PitcherChangeEvaluator.cs` — `GetNextPitcherSlot()` 추가

**완성된 메서드 (PitcherChangeEvaluator)**
- `GetNextPitcherSlot(currentState, gameState)` — 세이브 상황(9회 이상 + 투구팀 1~3점 리드) 시 6(CP) 반환, 그 외 `PitcherSlotIndex + 1`(다음 RP) 반환

📝 주요 설계 결정:
- 투구팀 판별: `IsTopInning`으로 추론 (앞이닝=홈 투구, 뒷이닝=원정 투구)
- 리드 계산: 절댓값 대신 부호 있는 값 — 지고 있는 팀이 CP를 쓰는 오류 방지
- CP 소진 후(슬롯 6)에서 추가 교체 필요 시 → `PitcherSlotIndex + 1 = 7` (범위 초과)는 게임 루프에서 방어 예정

### 세션 24 (2026-08-01) — 경기 루프 통합 완성

**브랜치**: `feature/simulation_game_loop`

**완성된 파일**
- `Assets/Scripts/Simulation/GameResult.cs` — 경기 결과 데이터 (점수 + 타석 로그)
- `Assets/Scripts/Simulation/GameSimulator.cs` — 경기 시뮬 코어 (순수 C#)

**완성된 메서드 (GameSimulator)**
- `GameSimulator(pullThreshold)` — 4개 계산기(`BatterOutcomeCalculator` / `BaseRunningCalculator` / `PitchCountCalculator` / `PitcherChangeEvaluator`) 필드 초기화
- `SimulateGame(context)` — `GameState` + 로그 리스트 초기화 → `while (!IsGameOver)` 루프 → `GameResult` 반환
- `SimulateAtBat(gameState, context, logs)` — 타석 1회 처리 (① isTopInning 캡처 → ② 타자/투수 스냅샷 → ③ avgDefense → ④ outcome → ⑤ pitchCount + ConsumePitches → ⑥ scoreBefore 캡처 + Apply + AddInningRun → ⑦ AdvanceBatter + 로그 → ⑧ ShouldChange → SubstitutePitcher)
- `CalcAverageDefense(lineup)` — 9명 Defense 합산 / 9 / 100f (0~1 정규화)

📝 주요 설계 결정:
- `GameSimulator`는 순수 C# 클래스 — MonoBehaviour 배제로 리그 일괄 시뮬 시 UI 없이 고속 반복 가능 (기획서 시뮬 코어 분리 원칙)
- 계산기는 필드로 1회 생성 후 재사용 — 매 타석 GC 압박 회피
- `isTopInning` / `scoreBefore`를 `Apply()` 전에 캡처 — `Apply()` 내부 `AddOut()`이 3아웃 시 `IsTopInning` 반전 + 이닝 스탯 리셋을 수행하므로 이후 참조 시 엉뚱한 팀의 상태를 읽는 버그 방지
- `BaseRunningCalculator.Apply()` 시그니처에 `batterInstanceId` 추가 인자 필요 → `hitter.InstanceId` 전달
- `AdvanceBatter()`를 `Apply()` 앞으로 배치 — `Apply()` 후엔 3아웃 시 `IsTopInning`이 반전되어 엉뚱한 팀 타순이 전진하는 버그 방지 (`Apply()`는 배팅 인덱스 미참조하므로 순서 변경 안전)

## ✅ 완료

로드맵 6번 4단계: **투수 체력·자동 교체** — `feature/Simulation_PitchingProbabilitySystemModel` 브랜치 완료 (기획서 8.4)

로드맵 6번 5단계: **경기 루프 통합** — `feature/simulation_game_loop` 브랜치 완료 (기획서 8.1). 로드맵 6번(경기 시뮬레이션 엔진) 전체 완료

### 세션 25 (2026-08-02) — 시뮬 엔진 코드 리뷰 반영

**변경 파일**
- `Assets/Scripts/Simulation/GameSimulator.cs`
- `Assets/Scripts/ProbabilityModels/PitcherChangeEvaluator.cs`

**수정 내용**
- GameSimulator: 미사용 `using Unity.VisualScripting;` 제거
- `PitcherChangeEvaluator.ShouldChange` 시그니처에 `int effectiveInningRuns` 매개변수 추가
  - 기존 `pitcherState.CurrentInningRuns` 직접 참조 제거 → 호출부에서 명시적으로 넘김
  - 향후 "득점+3아웃 동시" outcome 추가 시 발생할 이닝 실점 카운터 붕괴 방어
- `GameSimulator.SimulateAtBat`
  - `inningRunsBefore` Apply 전 캡처, `inningEnded` 판정 추가
  - 이닝 종료된 at-bat에서는 `AddInningRun` 스킵 (다음 이닝 실점 유출 방지)
  - `effectiveInningRuns = inningRunsBefore + runsScored` 계산 후 ShouldChange에 전달
- `PitcherChangeEvaluator.GetNextPitcherSlot`
  - 슬롯 인덱스 상한(6=CP) 초과 시 -1 반환 (센티넬 규약)
  - `GameSimulator.SimulateAtBat`에서 `if (nextSlot != -1)` 가드 추가
  - 극단 시나리오(연장 11회 + CP 방전)에서 IndexOutOfRangeException 방지

📝 주요 설계 결정:
- ShouldChange에 pitcher.CurrentInningRuns를 직접 읽지 않고 매개변수로 받는 이유: 순서 의존성 제거, mutable 상태와 결정 로직 분리
- -1 센티넬 채택 이유: bool+out은 호출부 지저분, throw는 정상 흐름 예외 처리라 부적절. GameState 베이스 주자 -1 규약과 일관성
- CP 소진 후 폴백 = 지친 CP 계속 등판: 실제 야구의 "야수 마운드 등판"은 우리 규칙에 없어 가장 덜 왜곡된 근사

---

### 세션 26 (2026-08-03) — 인터럽트/수동 교체 시뮬 코어 완성

**브랜치**: `feature/Simulation_Interrupt`

**완성된 파일 목록**
- `Assets/Scripts/Simulation/SimulationContext.cs` — `HomeBench` / `AwayBench` 필드 추가
- `Assets/Scripts/Simulation/GameState.cs` — 사용 완료 카드 트래킹 (HashSet 4개 + Mark/Is 메서드 4개)
- `Assets/Scripts/ProbabilityModels/PitcherChangeEvaluator.cs` — `GetNextPitcherSlot` 사용 완료 슬롯 스킵
- `Assets/Scripts/Simulation/HitterSubstitution.cs` — 대타 교체 1건 (struct)
- `Assets/Scripts/Simulation/InterruptDecision.cs` — 교체 지시서 (투수 슬롯 + 대타 목록)
- `Assets/Scripts/Simulation/IGameInterruptHandler.cs` — 인터럽트 핸들러 계약서
- `Assets/Scripts/Simulation/GameSimulator.cs` — `_interruptHandler` 필드 + 훅 호출 + `ApplyInterruptDecision` 구현

**7-1: SimulationContext 벤치 확장**
- `HomeBench` / `AwayBench` 배열 추가 (0~5명, 선택)
- null·Length 검증 제거 — 내부 코드가 만드는 데이터라 방어 시나리오 없음
- AI팀 벤치는 항상 빈 배열 넘김 (기획서 7.8 미러전 = AI 벤치 없음)

**7-2: GameState 사용 완료 트래킹**
- 홈/원정 × 야수/투수로 4개 HashSet 분리 (투수 슬롯 인덱스 겹침 오염 방지)
- 야수 InstanceId / 투수 슬롯 인덱스 기반
- Tell, Don't Ask — HashSet 노출 없이 `MarkHitterUsed` / `MarkPitcherUsed` / `IsHitterUsed` / `IsPitcherUsed` 4개 메서드로 캡슐화
- 스타팅 선수 초기 등록 안 함 — "사용 완료" 정의는 "빠져서 재투입 불가"이지 "출전 중"이 아님

**7-3: GetNextPitcherSlot 사용 완료 슬롯 스킵**
- `isHomePitching = !gameState.IsTopInning`으로 홈/원정 판별
- 세이브 상황 CP 반환 조건에 `!IsPitcherUsed(...)` 추가
- 폴백 순회는 for 루프로 첫 미사용 슬롯 반환, 실패 시 `-1` (세션 25 센티넬 규약)

**7-4: GameSimulator 인터럽트 훅**
- 데이터 구조: `HitterSubstitution` (struct, GC 부담 없음) + `InterruptDecision` (class, `IReadOnlyList` 노출)
- 인터페이스: `IGameInterruptHandler.OnAtBatEnded(state, context) → InterruptDecision`
- `GameSimulator` 생성자에 `interruptHandler = null` 매개변수 추가 — 자동 모드 하위 호환
- 훅 호출 순서: 자동 투수 교체 → 인터럽트 훅 (반대면 인터럽트 교체를 자동 로직이 뒤집을 수 있음)
- `ApplyInterruptDecision`: 대타 교체 (여러 명) → 투수 교체 (-1이면 스킵)
- 각 교체마다 순서: **Mark*Used 등록 → 실제 교체** (반대로 하면 원본 값 소실)

📝 주요 설계 결정:
- 인터페이스 vs 델리게이트 → **인터페이스 채택**. 신호 여러 종류 얽힐 여지, 확장성 우선
- `null` 허용 인터럽트 핸들러 → 자동/수동 모드를 한 클래스로 커버, 리그 일괄 시뮬 시 오버헤드 0
- 라인업 갱신 방식 → **방식 A (context 배열 직접 덮어쓰기)**. 원본 라인업 보존 요구 없음(경기 저장 안 함 - 7.6), 시뮬 로직 무수정
- `SimulationContext` 이름은 "불변 컨텍스트" 인상이지만 실제로는 라인업 배열이 mutable — 향후 XML 주석 보강 필요
- 대타 교체 시 벤치 배열에서 삭제 없음 — 벤치는 재참조 가능한 풀, 재투입 금지는 `_usedHitterInstanceIds`가 담당

---

### 세션 27 (2026-08-06) — LiveGameController 뼈대 완성

**브랜치**: `feature/Simulation_Interrupt` (세션 26에 이어서 진행)

**완성된 파일**
- `Assets/Scripts/Simulation/LiveGameController.cs` — MonoBehaviour + `IGameInterruptHandler` 이중 상속

**완성된 메서드 (LiveGameController)**
- `ReserveHitterSubstitution(battingOrderIndex, benchIndex)` — 범위 검증(0~8/0~4) + 벤치 중복(`Any(sub => sub.BenchIndex == ...)`) + 타순 중복(`Any(sub => sub.BattingOrderIndex == ...)`) 검증 후 `_pendingHitterSubs`에 예약
- `ReservePitcherSubstitution(pitcherSlot)` — 범위 검증(0~6) 후 `_pendingPitcherSlot` 덮어쓰기 (중복 검증 없음, 최신 지시 우선)
- `OnAtBatEnded(state, context)` — 리스트 복사 → `InterruptDecision` 조립 → 버퍼 초기화(`Clear` + `-1`) → 반환

📝 주요 설계 결정:
- **null 대신 항상 InterruptDecision 반환** — 예약 없을 땐 `PitcherSubstitutionSlot=-1` + 빈 리스트. 시뮬 코어의 null 체크 부담 제거, 세션 26 -1 센티넬 규약과 일관
- **리스트 복사 → 원본 클리어 순서** — `_pendingHitterSubs` 참조를 그대로 `InterruptDecision`에 넘긴 뒤 `Clear()`하면 반환 객체까지 비워지는 얕은 복사 버그. `new List<Hittersubstitution>(_pendingHitterSubs)` 로 방어
- **투수 교체는 덮어쓰기 방식** — 대타는 여러 개(리스트)라 서로 충돌 가능 → 중복 검증. 투수는 한 타석당 1건이라 필드 하나로 충분, 사용자가 마음 바뀌면 최신 지시 존중
- **UI 실제 연결은 로드맵 9번에서** — 이번엔 진입점(`Reserve*` public 메서드)만 노출. 버튼 OnClick은 UI 작업 때 연결

**로드맵 7번(경기 중 인터럽트/수동 교체) 완료**
- 시뮬 코어(세션 26) + 컨트롤러(세션 27) 조합으로 인터럽트 시스템 논리적 완결
- UI 없이도 시뮬 흐름 정상 동작 (프로그래밍적으로 `Reserve*` 호출 가능)

---

### 세션 28 (2026-08-08) — 타순 중복 검증 + 라인업 조회 API

**브랜치**: `fix/LineUpManager-Return-Method` (PR #14 머지 완료)

**수정된 파일**
- `Assets/Scripts/Line Up/LineUpManager.cs`

**버그 수정 — 타순 중복 검증**
- `IsBattingOrderTaken(order, excludeSlot)` private 헬퍼 추가
- `AssignHitter` / `SetBattingOrder` 두 곳에서 호출
- 기존에는 두 선수가 같은 타순을 가질 수 있었음 → 타순 배열 정렬 시 인덱스 어긋남
- 검사 순서 정리: **인자 범위 → 대상 존재 → 상태 충돌** (순회 필요한 검사를 뒤로)

**추가된 조회 API**
- `GetHittersInBattingOrder()` — 타순 정렬된 야수 9명 instanceId 배열
- `GetBenchInstanceIds()` — 벤치 5칸 복사본 (빈 칸 -1 유지)
- `GetPitcherInstanceIds(PitcherPosition)` — 역할별 슬롯 복사본

📝 주요 설계 결정:
- `excludeSlot` 매개변수 — 같은 타순으로 재설정하는 정상 호출이 자기 자신에 걸려 실패하는 것 방지
- `GetHittersInBattingOrder`는 LINQ 정렬 대신 `result[battingOrder - 1]` 직접 배치 — O(9) 단일 패스, 추가 할당 없음. 리그 일괄 시뮬에서 경기마다 호출되므로
- 벤치 배열을 **압축하지 않고 -1 유지** — `LiveGameController`의 `benchIndex`(0~4)와 `GameSimulator.ApplyInterruptDecision`의 `attackBench[sub.BenchIndex]`가 UI 슬롯 번호를 그대로 인덱스로 쓰므로, 압축하면 다른 선수가 조용히 교체 투입됨
- 투수 조회는 역할별 메서드 3개 대신 `PitcherPosition` 매개변수 하나로 통합 — 본문이 키만 다르고 동일. OCP
- 세 메서드 모두 내부 배열이 아닌 **복사본** 반환

---

## 🔍 세션 28에서 발견한 로드맵 공백

**시뮬 엔진과 게임 데이터가 연결돼 있지 않음** (전수조사로 확인)

- `HitterSnapshot` / `PitcherSnapshot` / `SimulationContext`를 **생성하는 코드가 프로젝트에 0건**
  (전 브랜치 전 커밋 `git log --all -S` 검색 결과 — 삭제된 것도, 다른 브랜치에 있는 것도 아님)
- 기획서 1.4의 `최종 스탯 = 기본 + 강화(레벨×2) + trainDelta` 공식이 **어디에도 구현 안 됨**
- 결과적으로 **시뮬 엔진은 한 번도 실행된 적 없음**

로드맵 6·7번 자체는 정상 완료. 이 브릿지는 기획서 11장 로드맵에 독립 항목으로 없어서(5번과 6번 사이에 끼어 누락) 아무도 할당받지 않았던 것.
→ **로드맵 8번의 0단계로 편입해 진행**

---

### 세션 29 (2026-08-08) — 데이터 브릿지 완성 (8-0)

**브랜치**: `feature/League-system`

**완성된 파일**
- `Assets/Scripts/Cards/CardStatsCalculator.cs` — 최종 스탯 계산 (static)
- `Assets/Scripts/Builders/SimulationContextBuilder.cs` — 게임 데이터 → 시뮬 입력 변환 (static)

**완성된 메서드 (CardStatsCalculator)**
- `CalculateFinalStat(baseStat, enhanceLevel, trainDelta)` — 기획서 1.4 공식.
  `baseStat + enhanceLevel * 2 + trainDelta`

**완성된 메서드 (SimulationContextBuilder)**
- `Build(isPlayerHome, rotationIndex)` — SimulationContext 조립
- `BuildHitterSnapshot(instanceId)` / `BuildPitcherSnapshot(instanceId)` — 단일 스냅샷
- `BuildHitterSnapshots(int[])` / `BuildPitcherSnapshots(int[])` — 배열 변환
- `BuildPitcherStaff(rotationIndex)` — 투수진 7칸 조립 (SP 1 + RP 5 + CP 1)

📝 주요 설계 결정:
- `CardStatsCalculator`는 **스탯 1개짜리 함수**. 타자/투수 산술이 동일하므로 타입 분기 자체를 없앰. 호출자가 어느 필드에 적용할지 결정
- 강화 계수는 `const` — 세이브엔 `enhanceLevel`만 저장되고 스탯은 매번 재계산되므로, 값 변경 시 기존 세이브 카드 스탯이 소급 변경됨. 튜닝 노브가 아니라 데이터 계약
- 폴더 `Assets/Scripts/Builders/` — 브릿지는 Cards·Simulation 양쪽에 의존. `Simulation/` 폴더를 Cards 무의존으로 유지 (세션 15 원칙)
- 실패 시 `default` 반환 (struct라 null 불가). `BaseRunningCalculator.FindRunnerSnapshot`과 동일 규약 (세션 19)
- 타자/투수 빌더를 제네릭으로 합치지 않음 — 반환 struct에 공통 조상이 없고, 만들면 시뮬 코어가 그 조상에 의존하게 됨. 실제 중복은 2줄뿐
- `BuildPitcherStaff`에서 `rotationIndex % spIds.Length` — 경계 처리를 메서드 안에 가둠. 호출부(리그·포스트시즌·테스트)가 늘어날 예정이라 한 곳만 빠뜨려도 예외
- 배열 인덱스 규약 `[0]=SP / [1~5]=RP / [6]=CP` — 세션 23·25에서 확정된 시뮬 코어 계약
- 벤치 `-1` 가드는 **호출자(`BuildHitterSnapshots`)가 처리** — `-1`은 정상 상태, `LogError`는 이상 상태 신호. 섞으면 로그 신뢰도 붕괴

⚠️ **임시 배선 (8-1에서 반드시 교체)**
- `Build()` 내부 `opponentLineup` / `opponentPitchers` = 플레이어 것 재사용. `TODO(8-1)` 주석 표기됨
- `opponentBench`만 `Array.Empty<HitterSnapshot>()` (기획서 7.8 — AI 벤치 없음)
- 현재 양 팀 데이터가 동일해 홈/원정 배치 오류가 **증상 없이 통과**함. AI 로스터 연결 시 드러남

---

### 세션 30 (2026-08-09) — AI 팀 로스터 착수 (8-1)

**브랜치**: `feature/League-system`

**설계 결정 — AI 로스터 생성 방식 변경**
- 기획서 7.8·10장은 "AI 전용 CSV 직접 세팅"이나, **기존 카드 마스터 풀에서 자동 편성**으로 변경
- 이유: `HitterCards.csv` / `PitcherCards.csv`에 team·position·OVR이 이미 존재. 별도 AI CSV는 중복 데이터 소스가 되어 카드 스탯 수정 시 두 곳을 동기화해야 함
- 티어별 능력치 상승은 어차피 코드 보정값이라 CSV로 담을 수 없음
- "직접 세팅"의 의도(랜덤 생성 공식 금지, KBO 자료 기반)는 마스터 CSV 자체가 이미 충족

**완성된 파일 목록**
- `Assets/Scripts/League/LeagueTier.cs` — 리그 티어 enum 21개 (`Basic1 = 0` ~ `Legend3`)
- `Assets/Scripts/League/AiTeamRoster.cs` — AI 팀 1개의 고정 로스터
- `Assets/Scripts/Builders/AiRosterBuilder.cs` — 스냅샷 변환 (부분 완성)

**완성된 메서드 목록**
- `AiTeamRoster.GetPitcherStaff(rotationIndex)` — 로테이션 반영 투수진 7칸 조립 (`[0]=SP / [1~5]=RP / [6]=CP`)
- `AiRosterBuilder.ToHitterSnapshot(hitterData, tierStatBonus)` — 마스터 데이터 + 티어 보정 → 타자 스냅샷
- `AiRosterBuilder.ToPitcherSnapshot(pitcherData, tierStatBonus)` — 동일 (투수)

📝 주요 설계 결정:
- `LeagueTier`에 `None` 센티넬 없음 — `CardGrade`/`CardType`과 달리 필터용이 아니고 "티어 없음" 상태가 게임에 존재하지 않음. `Basic1 = 0`으로 두어 `(int)tier`를 보정 배열 인덱스로 직접 사용
- AI 선수의 `InstanceId` 자리에 **`CardId`** 주입 — AI는 `CardInstance`가 없으나 시뮬 코어가 주자 식별에 instanceId를 사용(`GameState.FirstBase`, `FindRunnerSnapshot`, `MarkHitterUsed`). 요구조건은 "한 팀 라인업 내 유일 + `-1` 아님"뿐이라 cardId로 충족. 팀 간 유일성은 불필요(공격팀 배열만 순회)
- AI 스탯 공식은 플레이어와 **별개**: `기본 스탯 + 티어 보정` (강화·훈련 없음). `CardStatsCalculator` 미사용 — `CalculateFinalStat(x, 0, 0)`은 무의미한 호출이고 "AI도 강화가 있나" 오해 유발
- `AiRosterBuilder`는 `LeagueTier`가 아니라 `int tierStatBonus`를 받음 — 티어→보정값 테이블은 수치 TBD라 인스펙터 튜닝이 필요한데 `static` 클래스는 `[SerializeField]` 불가. 테이블은 `AiRosterManager`(MonoBehaviour)가 소유 예정. 세션 25 `effectiveInningRuns` 매개변수화와 동일한 판단
- `AiTeamRoster`는 cardId가 아닌 **완성된 스냅샷** 보관 — 기획서 7.8 "매 리그 고정 로스터"라 리그 시작 시 1회 계산이면 충분. 일괄 시뮬 144경기에서 재계산 회피
- `AiTeamRoster` 생성자에 검증 없음 — 호출자는 `AiRosterBuilder` 한 곳뿐이고, 편성 실패는 카드를 고르는 시점에 잡아야 원인에 가까운 에러 메시지가 나옴. **검증은 만드는 쪽, 담는 쪽은 순수 데이터**
- `GetPitcherStaff`에서 SP 인덱스는 `% StartingPitchers.Length`(데이터 길이), RP 복사량은 상수 `5`(시뮬 코어 7칸 규약). **길이는 데이터에서 읽고, 규약은 상수로 박는다**
- 로스터에 벤치 필드 없음 — 기획서 7.8 + 세션 26, AI 팀은 벤치 없음
- 파일 위치: `AiRosterBuilder`만 `Builders/` — Cards·Simulation·League 세 레이어에 걸쳐 있어 세션 29 원칙 적용

**🔴 발견된 블로커 — `Assets/Resources/Data/` CSV가 구버전**
- 두 CSV 모두 데이터 **1행뿐**, `OVR` 열 없음
- `CardCSVLoader.cs:77` `headers["OVR"]` → **`KeyNotFoundException`**. `CardDataManager.Awake()`에서 터져 **게임 실행 자체가 불가**
- 원인: 구글 시트에는 타자/투수 각 100명 이상 + OVR 열이 이미 존재. **CSV 내보내기를 안 했을 뿐** (코드 버그 아님)
- OVR 수식은 `=INT(AVERAGE(...))` 필수 — `AVERAGE`만 쓰면 소수점이 CSV에 나가 `int.Parse` FormatException. 표시 서식으로 자릿수를 줄여도 실제 값은 안 바뀌므로 주의

---

## ⏭️ 다음 할 일

**로드맵 8번: 리그 시스템** (기획서 7장) — 브랜치 `feature/League-system`

| 단계 | 작업 | 상태 |
|---|---|---|
| 8-0 A | 라인업 읽기 API | ✅ 세션 28 |
| 8-0 B | `CardStatsCalculator` — 최종 스탯 계산 | ✅ 세션 29 |
| 8-0 C | `SimulationContextBuilder` — 라인업 → SimulationContext | ✅ 세션 29 |
| 8-1 | AI 팀 로스터 (9팀 미러전, 티어별 능력치 상승) | 🔧 세션 30 진행 중 |
| 8-2 | 일정 생성 (라운드 로빈 / 프로 이상 3연전 / 홈·원정 교대) | ⏭️ |
| 8-3 | 순위표 (KBO 승률·게임차, 타이브레이커 승률→득실차→상대전적) | ⏭️ |
| 8-4 | 리그 진행 (한 경기씩 / 일괄 시뮬) | ⏭️ |
| 8-5 | 포스트시즌 (144경기 한정, 상위 5팀 KBO 사다리) | ⏭️ |
| 8-6 | 시즌 저장/재도전 (리그 단위 저장, 개별 경기 미저장) | ⏭️ |

**브랜치 전략**: `feature/League-system`은 **8-0까지만** 담고, 8-2(일정)부터는 브랜치 재분할

### 세션 31 (2026-08-11) — 상태 점검만 진행 (코드 변경 없음)

- 워킹 트리 클린, 커밋 없음
- 세션 30의 "다음 착수 지점" 7개 항목을 실제 코드와 대조
  - `AiRosterBuilder`의 `static` 키워드는 **이미 반영되어 있었음** (기록만 누락)
  - 🔴 **CSV 블로커는 미해결 상태 그대로** — 두 파일 모두 데이터 1행, `OVR` 열 없음
- CSV 내보내기는 다음 세션으로 미룸 (작성자 결정)

---

### 세션 32 (2026-08-11) — CSV 최신화 완료 + 밸런스 진단

**브랜치**: `feature/League-system`

**🟢 CSV 블로커 해소** (세션 30·31 연속 미해결분)
- `HitterCards.csv` — **159행**, `PitcherCards.csv` — **166행**
- 검증 통과: UTF-8(BOM 없음) · `OVR` 열 존재 · 전 스탯 정수(소수점 0건) · 컬럼 수 일치
- cardId — 타자 `1~159` / 투수 `50001~50166`, 중복·결번 0건, 기획서 대역 규칙 준수
- 10개 팀(LG 삼성 KT 기아 두산 한화 NC 롯데 SSG 키움) = 플레이어 1 + AI 9

**수정된 데이터 오류 3건** (1차 검증 → 작성자 수정 → 재검증 통과)
| 위치 | 증상 | 결과 |
|---|---|---|
| 권동진(KT) | `grade` 빈칸 → `ParseCardGrade("")` throw → 게임 실행 불가 | ✅ |
| 김민혁(KT) | `position`이 `4` → 예외는 없으나 어느 슬롯에도 안 잡히는 유령 카드 | ✅ CF |
| 기아 | `LF` 0명 → AI 로스터 좌익수 슬롯 공백 | ✅ LF=1 |

**AI 로스터 편성 가능성 — 10팀 전부 OK**
- 수비 8포지션 전 팀 충족 / 투수 전 팀 SP≥5 · RP≥7 · CP≥1

**📝 확정 — DH는 CSV에 포지션으로 두지 않는다**
- DH 칸에는 **모든 포지션 카드가 들어갈 수 있으므로** CSV에서 `DH` 표기를 아예 뺌 (실제 `position=DH` 카드 0장)
- 코드는 이미 반영돼 있었음: `LineUpManager.cs:80` — `if (slot != HitterPosition.DH)` 로 포지션 일치 검사 건너뜀
- 세션 30 기록의 "`Position == "DH"` 카드는 수비 8자리 후보에서 자동 탈락" 항목은 **무효**
- → 8-1 `SelectHitters`에서 **DH 슬롯은 수비 카드 중 와일드카드로 채우는 것이 선택이 아니라 필수**

---

## 🔴 밸런스 진단 결과 (세션 32) — 스탯 값이 아니라 **공식**의 문제

CSV 실제 분포로 `BatterOutcomeCalculator`를 해석적으로 평가한 결과 (순차 탈락 구조 반영).

**리그 평균 타자 vs 리그 평균 투수**
| 지표 | 현재 모델 | KBO 실제 | 판정 |
|---|---|---|---|
| 타율 | **.224** | .265~.277 | ❌ 낮음 |
| 출루율 | **.242** | ~.345 | ❌❌ 심각 |
| OPS | **.613** | ~.750 | ❌ 낮음 |
| K% | **25.6%** | ~17.5% | ❌ 높음 |
| BB% | **1.5%** | ~9.3% | ❌❌ 심각 |
| HR% | 2.45% | ~2.0% | ✅ |
| 3루타% | **1.11%** | ~0.3% | ❌ 4배 과다 |

**원인 1 — 볼넷 공식이 구조적으로 붕괴 (최우선)**
- `probWalk = 0.085 + 0.20 * (contact * 0.4 - control)`
- `contact * 0.4`로 타자 항만 감쇠 → contact가 만점 100이어도 `0.4` vs control 평균 `0.647`. **항상 음수**
- 전체 매치업의 **96.5%가 하한 2%에 고정** → 타자 선구안·투수 제구가 결과에 영향 없음
- 실제 도달 범위 `-3.5% ~ 4.4%`. 설계 상한 20%는 **도달 불가능한 죽은 코드**

**원인 2 — 타자 스탯군과 투수 스탯군의 평균 위치가 어긋남**
- 타자 contact 평균 **58.4** vs 투수 pitcherPower(=(stuff+velo)/2) 평균 **70.3**
- 공식 상수(0.22, 0.3 등)는 "양쪽 평균이 같다"를 전제 → 모든 대결이 투수 우위로 기울어 K% 상승·타율 하락

**원인 3 — 클램프 범위가 실제 도달 범위보다 훨씬 넓음** (변별력 미발현)
| 확률 | 설계 클램프 | 실제 도달 범위 |
|---|---|---|
| 삼진 | 5% ~ 40% | 14.7% ~ 33.7% |
| 볼넷 | 2% ~ 20% | -3.5% ~ 4.4% |
| 홈런 | 0.5% ~ 30% | -1.3% ~ 10.1% (하한 포화 12.2%) |
| 안타 | 15% ~ 45% | 20.5% ~ 37.8% |

**원인 4 — 3루타 과다**: `tripleThreshold = 0.04 + ...` → 안타 중 4~7%가 3루타 (KBO는 ~1.5%)

**원인 5 — velo 표준편차 2.5** (65~80에 밀집) → 사실상 상수. 투수 변별력이 stuff(sd 5.4)에만 의존

**✅ 정상 확인된 항목**
- 등급별 평균 OVR 단조 증가 — 타자 3성 57.0 / 4성 61.2 / 5성 66.6, 투수 59.5 / 64.0 / 70.6
- stamina 이봉우리 분포(sd 12.6, SP 60~70 · RP 35~40) — 의도대로

**📌 결론: CSV 재작성이 아니라 공식 상수 조정으로 해결한다**
- CSV의 선수 간 **상대 순위**는 합리적 (김도영 74 최고, 등급별 OVR 단조 증가)
- 문제는 두 스탯군의 **절대 위치 차이**이고, 이는 공식 기준점이 흡수해야 할 몫
- 325명 스탯 재작성보다 상수 5개 조정이 압도적으로 싸고 안전
- **권장 방식**: 각 스탯을 자기 집단 평균 대비 **편차**로 정규화 → 평균 대 평균 매치업이 정확히 기준 상수로 떨어짐 → 상수에 KBO 실제 수치를 넣으면 리그 평균이 자동으로 맞음

---

### 🚩 다음 세션 착수 지점 (8-1 남은 작업, 순서대로)

1. `AiRosterBuilder.SelectHitters` — 수비 8포지션 + DH 와일드카드 + 중복 배치 금지. 8-1에서 가장 까다로움
   - 배정 순서: 수비 8자리(제약 강한 쪽) 먼저 → DH(와일드카드) 나중. 반대로 하면 최고 OVR 선수가 DH로 빠져 포수 자리가 빔
   - **DH는 수비 카드 중에서 뽑는다** (`position=DH` 카드 자체가 없음 — 위 확정 사항)
   - 타순은 OVR 내림차순 (단순 근사, 추후 튜닝 대상)
   - 실패 시 `Array.Empty<HitterSnapshot>()` + `LogError`에 팀명·포지션 포함
2. `AiRosterBuilder.SelectPitchers` — SP5 / RP5 / CP1
3. `AiRosterBuilder.BuildTeam` — 위를 묶어 `AiTeamRoster` 조립
4. `AiRosterManager` (MonoBehaviour 싱글톤) — `[SerializeField] int[]` 티어 보정 테이블 + 9팀 로스터 생성/보관
   - 이때 **티어 보정 후 스탯 100 초과 클램프 여부 결정**. 현재 `BatterOutcomeCalculator`는 `stat / 100f` 정규화 + 최종 확률 `Mathf.Clamp`라 크래시는 없고 상한에 붙는 형태로 흡수됨
5. `SimulationContextBuilder.Build()`의 `TODO(8-1)` 임시 배선(상대팀 = 플레이어 것 재사용) 교체

**⏸️ 밸런스 튜닝은 8-1 완료 후 별도 작업으로 분리**
- 이유: 지금 상수를 고쳐도 **검증할 수단이 없음**. AI 로스터가 완성돼야 실제 경기를 돌려 타율·득점을 측정할 수 있음
- 8-1 완료 → 시뮬 1회 실행 성공 → 그때 상수 튜닝 + 시즌 일괄 시뮬로 지표 확인

**제외 항목** (규칙 복잡성 회피 — 세션 26에서 확정)
- DH 권한 포기 후 투수의 타순 삽입
- 포지션 스왑 (LF↔SS 등)
- 시뮬 중 타순 재배치
- 대주자 교체 (기획서상 추후)

---

### 세션 33 (2026-08-11) — AI 로스터 편성 방식 재설계 (SO 기반)

**브랜치**: `feature/League-system`

**🔄 세션 30 결정 철회 — 자동 OVR 편성 폐기**

세션 30에서 "기존 카드 마스터 풀에서 자동 편성"으로 정했으나, 아래 이유로 **에디터 수동 편성**으로 전환.

- **개발자 편집 권한 없음** — 자동 편성은 OVR 내림차순이라 팀 최고 타자가 1번타자가 됨(실제는 3~4번). 타순·선수 선택을 손댈 방법이 전무
- **동일 인물 중복 출장 버그** — 시그니쳐 추가 시 `FindBestExcluding`이 카드 참조로만 중복을 걸러, 김도영(S)=3B / 김도영(N)=DH 동시 출장. 예외·로그 없음
- **시그니쳐 출시 = AI 전 팀 자동 강화** — 최고 OVR을 고르므로 S/G 카드를 CSV에 넣는 순간 10팀 전원 자동 교체. 난이도 튜닝 노브 없음
- 기획서 7.8·10장 원안("AI 전용 CSV 직접 세팅")이 옳았음. 세션 30이 **"AI 스탯 데이터"(중복 발생)**와 **"AI 편성 데이터"(중복 없음)**를 혼동해 뒤집었던 것

**📐 확정 구조 — 3단 참조**

```
LeagueTierTable (SO 1개, 21행)   티어 → { 로스터 세트 참조, 능력치 보정 }
      ↓
AiRosterSet (SO)                 10팀 참조만 든 얇은 껍데기
      ↓
AiTeamRosterData (SO)            팀 1개. 실제 편집 대상
   타선 9 (배열 순서 = 타순) / 선발 5 (= 로테이션) / 불펜 5 (= 등판 순서) / 마무리 1
```

작업량: 전 티어가 세트 1개 공유 시 **200배정**. 티어마다 따로 만들면 4,200 (21×10×20)

📝 주요 설계 결정:
- **세트와 팀을 2단으로 분리** — 세트는 참조 10개뿐이라 복제 비용 0. 팀 하나만 포크하고 나머지 9팀은 원본 공유 가능. 10팀을 세트 안에 인라인했다면 복제 시 전부 딸려와 LG 수정이 모든 복제본에 반복됨
- **티어별 차이는 기본적으로 `statBonus`로 표현** — 세트 포크는 "선수 구성 자체를 바꿀 때"만. 대부분 티어는 포크 불필요
- **부분 오버라이드 방식 기각** — "티어 행에서 특정 팀·슬롯만 덮어쓰기"는 우선순위 해석이 필요해 "지금 이 티어의 삼성 3번은 누구인가"를 추적하기 어려움. 복제 방식은 티어 테이블 행에 보이는 게 곧 정답
  - 대가: 복제본은 원본 수정을 상속하지 않음. 포크 3~4개 수준에서는 수용 가능, 20개 넘어가면 재검토
- **`battingOrder` 필드 없음** — 배열 인덱스가 곧 타순. 별도 필드를 두면 "3번이 두 명"인 상태가 표현 가능해지고 검증 코드가 필요해짐. **표현할 수 없는 상태는 만들지 않는다**
- **`AiHitterSlot.Position`은 검증 전용** — 시뮬 코어는 수비 포지션을 안 씀(`CalcAverageDefense`가 9명 평균만 냄). "포수 없는 라인업" 검출용이며, DH는 어느 포지션 카드든 올 수 있어 카드에서 역산 불가
- **카드 마스터는 CSV 유지** — 325장 벌크 + OVR 수식은 시트가 적합, 로스터는 슬롯별 판단이라 인스펙터가 적합. 도구를 용도에 맞게 분리
- **씨앗 CSV는 `Assets/Editor/RosterSeed/`로 이동** — `Resources/`에 두면 참조 여부와 무관하게 빌드에 포함됨. 에디터 전용 데이터라 제외
- **로스터 SO는 Resources 미사용** — `AiRosterManager`의 `[SerializeField]` 직접 참조. Resources는 스트리핑 불가

**🔴 확정 규칙 — cardId는 append-only**

로스터 SO가 cardId만 저장하므로, **CSV에서 cardId를 재배치하면 모든 SO가 조용히 오염된다.**
(예: 김도영 = `cardId 50`. 24년 카드를 앞번호로 끼워넣으면 `cardId 50`이 다른 선수로 바뀜. 예외·로그·컴파일 에러 전부 없음)

**신규 카드 추가 시작 번호**: 타자 `160~` / 투수 `50167~` (2026-08-11 기준 최대값 타자 159 · 투수 50166)

| 허용 | 금지 |
|---|---|
| 새 카드를 맨 뒤 번호로 추가 | 기존 cardId 재배치·재정렬 |
| 기존 카드의 스탯·이름 수정 | 삭제한 cardId 재사용 |
| 시트에서 행 정렬해 보기 (파서가 헤더 기반이라 무관) | 정렬 결과로 cardId 재부여 |

- 카드 제외 시 행 삭제보다 **cardId를 비워두는 편이 안전** — 검증기가 "없는 cardId" 로 잡아주는 안전한 실패가 됨

**📝 년도(`year`) 취급 확정**
- 현재 CSV는 전부 `26`. 기획서 38줄은 다년도(24, 25)를 상정
- `CardMasterData.Year`는 현재 **읽는 곳 0곳**. 기획서 535줄(카드 UI에 년도 표시) 대비 필드는 유지
- **로스터 슬롯에 `year` 필드 불필요** — 김도영24/김도영26은 어차피 다른 cardId. 년도는 cardId에 함축됨
- **드롭다운 라벨에는 년도 포함 필수** — 없으면 24/26 김도영을 육안 구분 불가
- **라인업 중복 판정 키 = `(이름, 팀)`** — 기획서 126줄의 강화용 동일성 판정(종류·이름·팀)을 그대로 쓰면 안 됨. `cardType`이 들어가 있어 시그니쳐 김도영 + 일반 김도영이 통과함. 목적이 다른 별개 규칙

**🗂️ 남은 작업 순서**

| # | 작업 | 상태 |
|---|---|---|
| 1 | `AiHitterSlot` (struct) + `AiTeamRosterData` (SO) | ✅ |
| 2 | `AiTeamRoster.cs` 정리 — 자동 편성 메서드 4개 + `DefensePositions` + 잘못된 using 삭제 | ✅ |
| 3 | `CardIdAttribute` + `CardCatalog` + `CardEntry` + `CardSearchDropdown` + `CardIdDrawer` | ✅ |
| 4 | `AiRosterSet` (SO) + `LeagueTierTable` (SO) | ⏭️ |
| 5 | `Editor/AiRosterSeedImporter` — 씨앗 CSV → SO 에셋 일괄 생성 | ⏭️ |
| 6 | `Editor/AiRosterValidator` — 포지션 커버리지 · 중복 인물 · cardId 존재 검증 | ⏭️ |
| 7 | `AiRosterBuilder.BuildTeam` — SO + tierStatBonus → `AiTeamRoster` | ⏭️ |
| 8 | `AiRosterManager` (MonoBehaviour 싱글톤) | ⏭️ |
| 9 | `SimulationContextBuilder.Build()`의 `TODO(8-1)` 임시 배선 교체 | ⏭️ |

⚠️ **드로어 성능 주의** — `OnGUI`는 초당 수십 회 호출. CSV 파싱 결과를 **static 캐시**에 1회만 올릴 것. 매 리페인트 파싱하면 에디터가 얼어붙음

**완성된 에디터 도구 (3번)**
- `Assets/Scripts/Cards/CardIdAttribute.cs` — `PropertyAttribute`. **런타임 폴더에 둬야 함** (런타임 필드가 참조하므로 `Editor/`에 두면 빌드 실패)
  - 타자/투수 구분은 기존 `PlayerTypeFilter` 재사용 — 같은 개념의 enum을 새로 만들지 않음
- `Assets/Editor/CardEntry.cs` — 드롭다운 표시용 struct (CardId / TeamName / Label)
- `Assets/Editor/CardCatalog.cs` — CSV 1회 파싱 후 static 캐시. `Tools/BaseBallManager/카드 카탈로그 새로고침` 메뉴
- `Assets/Editor/CardSearchDropdown.cs` — `AdvancedDropdown` 상속. 팀별 폴더 + 검색창 내장
- `Assets/Editor/CardIdDrawer.cs` — `PropertyDrawer`. cardId를 라벨 버튼으로 그림

📝 에디터 도구 설계 결정:
- **`CardCSVLoader`를 그대로 재사용** — 에디터에 파싱 로직을 다시 짜면 헤더 규칙·인코딩 처리가 두 벌이 되어 열 추가 시 한쪽만 고쳐짐. `Resources.Load`는 에디터에서도 동작
- **`EnsureLoaded`의 catch에서 빈 컬렉션 대입** — `null`로 두면 가드를 통과해 매 프레임 재시도 → 초당 수십 개 에러 로그로 에디터 정지. 빈 컬렉션이면 재시도 중단 + 로그 1회 + NRE 없음
- **라벨은 캐시 시점에 미리 조립** — `OnGUI`에서 문자열 보간하면 325개 × 초당 수십 회 = GC 폭탄
- **`leaf.id`에 cardId를 실어 보냄** — `AdvancedDropdownItem.id`를 활용해 별도 매핑 테이블 불필요. 팀 노드는 자식이 있어 폴더로 동작하므로 `ItemSelected`가 안 불림 → id 불필요
- **드로어 콜백은 `property`를 붙잡지 않음** — 선택은 몇 프레임 뒤에 일어나고 `SerializedProperty`는 그 프레임에만 유효. `serializedObject` + `propertyPath`만 복사해두고 콜백에서 `FindProperty`로 재조회
- **빈 슬롯 센티넬 = `0`** — cardId는 1부터 시작하므로 안전하고, `int` 기본값이 0이라 새 에셋의 빈 슬롯이 자동으로 "(비어 있음)" 표시됨. 시뮬 코어의 `-1` 규약과 다른 이유는 여기선 직렬화 기본값이 그대로 빈 슬롯이 되는 게 이득이기 때문
- **없는 cardId는 버튼에 경고 문구로 표시** — CSV에서 카드를 지웠을 때 눈에 띔. cardId 비워두기 권장 규칙의 실효성이 여기서 나옴

⚠️ **소스 파일 인코딩 혼재** (2026-08-11 확인)
- 대부분의 `.cs`가 **CP949**, `CardSearchDropdown.cs`만 UTF-8
- Roslyn은 BOM 없는 소스를 UTF-8로 가정 → CP949 한글이 깨질 수 있음. 주석은 무해하나 **문자열 리터럴이 깨지면 화면에 그대로 노출**
- 확인법: 인스펙터 cardId 버튼이 `(비어 있음)`으로 보이면 정상, 깨져 보이면 문제
- 해결: **UTF-8 (BOM 포함)**으로 재저장. 프로젝트 루트 `.editorconfig`에 `[*.cs] charset = utf-8-bom` 두면 이후 자동 적용 (기존 파일은 한 번씩 열어 저장 필요)
