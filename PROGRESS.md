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
| 4 | `AiRosterSet` (SO) + `LeagueTierTable` (SO) | ✅ 세션 34 |
| 5 | `Editor/AiRosterSeedImporter` — 씨앗 CSV → SO 에셋 일괄 생성 | ✅ 세션 34 (실행 검증 미완) |
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

**🐛 테스트에서 발견·수정한 버그 2건** (에디터 실동작 확인 중)

| 증상 | 원인 | 해결 |
|---|---|---|
| 카드를 고르면 `[!] 알 수 없는 cardId: -2` | `AdvancedDropdownItem.id`는 Unity가 선택 상태·검색 트리 관리에 쓰는 **내부 필드**. 우리가 심은 cardId가 보존되지 않고 내부 값으로 덮임 | `CardDropdownItem : AdvancedDropdownItem` 중첩 클래스로 **cardId를 자체 필드에 보관**. `ItemSelected`에서 `is` 패턴으로 꺼냄 |
| LF 슬롯인데 SS 카드도 선택 가능 | 드로어가 `_cardId` 필드만 보므로 형제 필드 `_position`을 알 방법이 없었음 | `CardIdAttribute`에 `PositionFieldName` 추가 + 드로어가 **propertyPath 문자열을 잘라 붙여** 형제 필드 조회 |
| 선발 슬롯인데 RP 카드가 나옴 | 투수 배열엔 읽을 형제 필드가 없음. **필드 자체가 역할을 결정**(`_startingPitcherCardIds` = SP) | `CardIdAttribute`에 `FixedPosition` 추가. `[CardId(..., fixedPosition: nameof(PitcherPosition.SP))]` |

📝 수정 관련 설계 결정:
- **외부 라이브러리의 범용 필드(`id`/`tag`/`userData`)에 내 데이터를 얹지 않는다** — 소유권이 없어 언제 덮어써질지 모름. 내 데이터는 내 타입에 담는다
- `is CardDropdownItem` 검사 덕에 팀 폴더 클릭 시 콜백이 안 불림 (별도 분기 불필요)
- **형제 필드 조회는 경로 문자열 조작** — `SerializedProperty`에 형제 접근 API가 없음. `_lineup.Array.data[0]._cardId` → 마지막 점까지 자르고 `_position` 부착
- **필드 이름은 `nameof(_position)`** — 문자열 리터럴이면 필드명 변경 시 조용히 필터가 꺼짐. `nameof`는 컴파일러가 잡아줌
- **포지션 필드를 못 찾으면 필터 없이 전체 노출** — 아무것도 못 고르는 것보다 관대한 실패가 나음
- **DH 슬롯은 필터 건너뜀** — 세션 33 확정(DH = 와일드카드)의 코드상 구현부
- `CardEntry`에 `Position`(CSV 표기 문자열) 추가. `HitterPositionParser.TryParse`로 `1B`↔`FB` 변환 (세션 13 자산 재사용)
- **포지션 출처가 둘로 갈림** — 타자는 슬롯마다 값이 다르니 형제 필드를 읽고, 투수는 배열 이름이 곧 역할이니 특성에 고정값. `BuildEntries`가 ① 고정 포지션 → ② 형제 필드 → ③ 필터 없음 순으로 판단
- **`nameof(PitcherPosition.SP)`** — enum 멤버명이 CSV 표기와 동일. `"SP"` 리터럴은 오타 시 목록만 조용히 비지만 `nameof`는 컴파일 에러
- **`fixedPosition:` 이름표 필수** — 생성자에 `string` 매개변수가 연달아 있어 위치 인자로 넘기면 `positionFieldName`에 잘못 들어감
- 두 포지션 매개변수 모두 기본값 `null` — 포지션 개념이 없는 슬롯은 무수정으로 동작 (OCP)

**✅ 8-1 편집 도구 실동작 확인 완료** — 포지션별 필터링 / 검색 / 선택 / 저장 전부 정상

---

### 🔧 소스 파일 인코딩 정리 (2026-08-11 완료)

**문제**: `Assets/` 하위 `.cs` 대부분이 **CP949**로 저장돼 있었음. Unity의 Roslyn 컴파일러는 BOM 없는 소스를 **UTF-8로 가정**하므로 한글이 깨짐. 주석은 무해하나 **문자열 리터럴이 깨지면 화면·콘솔에 그대로 노출**됨

**조치**
- `.cs` **44개를 UTF-8(BOM 포함)로 일괄 변환**
- 변환 전 **CP949 왕복 검증**(재인코딩 결과가 원본 바이트와 일치하는지) 통과분만 적용 — 무손실 확인
- 프로젝트 루트에 **`.editorconfig`** 생성 → `[*.cs] charset = utf-8-bom`. 이후 저장분은 Visual Studio가 자동 적용

⚠️ **주의**: 변환 시점에 Visual Studio에 열려 있던 파일은 VS 메모리에 옛 인코딩으로 남아 있음. 그대로 저장하면 되돌아가므로 **VS를 한 번 닫았다 열 것**

---

### 세션 34 (2026-08-13) — 티어 테이블 + 씨앗 임포터 (8-1의 4·5번)

**브랜치**: `feature/League-system`

**완성된 파일 목록**
- `Assets/Scripts/League/AiRosterSet.cs` — 팀 10개를 묶은 로스터 세트 (SO)
- `Assets/Scripts/League/LeagueTierEntry.cs` — 티어 1개 설정 (`[Serializable] struct`)
- `Assets/Scripts/League/LeagueTierTable.cs` — 티어 21행 테이블 (SO)
- `Assets/Editor/AiRosterSeedImporter.cs` — 씨앗 CSV → SO 에셋 일괄 생성 (**Claude 작성**)

**완성된 메서드 목록 (LeagueTierTable)**
- `OnValidate()` — `_entries.Length != TierCount`면 `Array.Resize`로 21칸 복구
- `GetRosterSet(tier)` — 실패 시 `null`
- `GetStatBonus(tier)` — 실패 시 `0`
- `TryGetEntry(tier, out entry)` — `(int)tier` 인덱싱 + 범위 검사 공통부

**완성된 메서드 목록 (AiRosterSeedImporter)**
- `Import()` — 메뉴 진입점 `Tools/BaseBallManager/AI 로스터 씨앗 임포트`
- `TryReadSeed(path, requiredColumns, out headers, out rows)` — `AssetDatabase`로 CSV 로드 + 필수 열 존재 검사
- `ParseHeaders(headerLine)` — 로컬 헤더 파서
- `FillHitters` / `FillPitchers` — 행 → `TeamDraft` 채우기, 반환값은 오류 건수
- `TrySetPitcher(slots, order, cardId, ...)` — 투수 배열 범위·중복 검사 후 배치
- `GetOrCreateDraft` / `WriteTeamAsset` / `WriteRosterSetAsset` / `WriteIntArray` / `EnsureFolder`
- `TeamDraft` (private 중첩 class) — CSV 행을 모으는 임시 그릇

**🔴 SO 에셋 저장 위치 정정 — `Assets/Data/League/`**
- `Assets/Editor/` **안의 에셋은 스크립트뿐 아니라 전부 빌드에서 제외**됨
- 로스터 SO는 `AiRosterManager`가 참조할 런타임 데이터 → `Editor/` 밖이어야 함
- 에디터에선 정상 동작하고 **빌드에서만 참조가 끊기는** 유형이라 재현이 어려움
- 씨앗 CSV가 `Assets/Editor/RosterSeed/`에 있는 건 그대로 유지 (임포터만 읽음, 런타임 무관)
- 생성 경로: `Assets/Data/League/{rosterSet}/AiTeamRoster_{팀명}.asset` + `AiRosterSet_{rosterSet}.asset`

📝 주요 설계 결정:
- `LeagueTierEntry`는 **`[Serializable] struct`** — `[SerializeField]`는 필드를 표시할 뿐 타입이 직렬화 가능해야 함. `[Serializable]` 없으면 인스펙터에 배열이 아예 안 뜨고 값도 저장 안 됨(무경고). `class`면 `new LeagueTierEntry[21]` 직후 요소가 전부 `null`이라 NRE 창이 생기는데 struct는 그 구간 자체가 없음
- `LeagueTierEntry`에 `LeagueTier` 필드 없음 — **배열 인덱스가 곧 티어**. 세션 33의 `battingOrder` 필드 제거와 같은 원칙(표현할 수 없는 상태는 만들지 않는다). 대가는 인스펙터에 `Element 0`으로만 보이는 것
- 범위 검사는 `TierCount` 상수가 아니라 **`_entries.Length`** 기준 — 인스펙터에서 크기가 줄면 상수 검사는 통과하고 인덱싱에서 터짐
- `GetStatBonus` 실패값은 **`0`** — 이 값은 `AiRosterBuilder`에서 스탯에 **그대로 더해짐**. `-1` 센티넬을 쓰면 전 선수 스탯이 조용히 1 깎임. 시뮬 코어의 `-1` 규약은 산술에 안 들어가는 값에만 적용
- 티어 보정 테이블 소유자가 `AiRosterManager`(세션 30 기록) → **`LeagueTierTable` SO로 확정 변경**. MonoBehaviour에 두면 씬마다 값이 갈라짐. `AiRosterManager`는 테이블 참조 1개만 보유
- `_statBonus`는 전부 0으로 시작 — 밸런스 튜닝은 8-1 완료 후 별도 작업(세션 32 결론)
- **임포터가 private 필드에 쓰는 방식 = `SerializedObject`** — `#if UNITY_EDITOR` 세터를 다는 대안은 런타임 데이터 클래스에 수정 경로를 여는 것이라 기각. 대신 필드명이 문자열이 되므로 파일 상단 `const`로 모으고, `WriteTeamAsset` 진입 직후 5개를 한꺼번에 조회해 하나라도 `null`이면 즉시 중단(조용한 빈 에셋 방지)
- **오류 1건이라도 있으면 에셋을 하나도 만들지 않고 중단** — 7팀만 만들어진 중간 상태가 최악. 대신 오류 로그는 행마다 전부 찍어 한 번에 고칠 수 있게 함. 로그에 파일명 + CSV 실제 줄 번호(`i + 2`) 포함
- `Dictionary`(조회) + `List`(순서) 병행 — `Dictionary` 열거 순서는 명세상 미보장. `_teams` 배열 순서가 임포트마다 흔들리면 `.asset` diff가 지저분해지고 8-2 일정 생성이 순서에 의존할 경우 재현 불가
- **기존 에셋은 `DeleteAsset` 없이 덮어쓰기** — 지우면 GUID가 바뀌어 `AiRosterSet`→팀, `LeagueTierTable`→세트 참조가 전부 끊김
- 빈 슬롯 판정 `!= 0` — 세션 33의 `0` 센티넬 재사용. `int[]` 기본값이 0이라 초기화 코드 불필요
- `ParseHeaders`를 임포터에 다시 작성 — `CardCSVLoader.ParseHeaders`는 `private` 인스턴스 메서드. 에디터 도구 하나 때문에 런타임 클래스의 공개 계약을 넓히지 않음. 공유되는 건 6줄짜리 알고리즘뿐
- `Resources.Load` 불가 → `AssetDatabase.LoadAssetAtPath` — 씨앗 CSV가 `Resources/` 밖. 세션 33의 `CardCatalog`가 `CardCSVLoader`를 재사용할 수 있었던 것과 갈리는 지점
- **임포터는 로스터 내용을 검증하지 않음** — 존재하지 않는 cardId·포지션 커버리지·동일 인물 중복은 6번 `AiRosterValidator`의 몫. 임포터에 섞으면 인스펙터에서 직접 고친 로스터는 검증을 못 받음

**🐛 코드 리뷰 지적** → ✅ 세션 35에서 이미 반영돼 있음을 확인 (`LeagueTierTable.cs:50`에 `길이 : {_entries.Length}` 존재)

---

### 세션 35 (2026-08-14) — 임포터 실행 + 로스터 에셋 생성 완료 (8-1의 5번 마감)

**브랜치**: `feature/League-system`

**세션 34 미완 3건 전부 해소**
1. `Assets/Editor/AiTeamRoster.asset` 삭제 — 커밋 `4a8f786`에서 이미 처리돼 있었음 (기록만 누락)
2. 임포터 실행 성공 — `Assets/Data/League/Normal/`에 **팀 10개 + 세트 1개** 생성
3. `LeagueTierTable.asset` 생성 + **21행 전부** `AiRosterSet_Normal` 연결 (`_statBonus`는 전부 0)

**씨앗 CSV 사전 검증 (임포트 전 수행, 오류 0건)**
- 10팀 × 타자 9 / 투수 11 = 200행, 세트 1개(`Normal`)
- 타순 1~9 완전성·중복 / 수비 8포지션 + DH 1칸 / SP5·RP5·CP1 + order 연번 — 10팀 전부 통과
- cardId 200개 전부 마스터 CSV 실존, 씨앗의 team·name·position이 마스터와 일치
- **동일 인물(이름+팀) 중복 출장 0건**

**생성 에셋 내용 검증 (.asset YAML 직접 확인)**
- `AiRosterSet_Normal._teams` 10칸 = 서로 다른 팀 10개 (누락·중복 0)
- LG 기준 타순·포지션 매핑 정상 (1번 오스틴 `1B`→`FB`=3 …), SP1=50109 / CP=50106 — 씨앗과 일치

📝 기록: `Assets/Data/`는 아직 git untracked. 다음 커밋에 포함할 것

---

### 세션 35 (계속) — 8-1 코드 전량 완성 (7 · 8 · 9 · 6번)

**작업 순서 변경** — `6번(Validator)`을 맨 뒤로 미루고 **7 → 8 → 9 → 6** 순으로 진행.
씨앗 CSV를 이미 전수 검증(오류 0건)해 Validator가 당장 잡을 대상이 없었고, 프로젝트 최대 리스크인
"시뮬 엔진이 한 번도 실행된 적 없음"(세션 28)을 뚫는 경로가 7·8·9였기 때문.

**완성된 파일 목록**
- `Assets/Scripts/Builders/AiRosterBuilder.cs` — `BuildTeam` / `BuildLineup` / `BuildPitchers` 추가 (7번)
- `Assets/Scripts/League/AiRosterManager.cs` — MonoBehaviour 싱글톤 신규 (8번)
- `Assets/Scripts/Builders/SimulationContextBuilder.cs` — `Build()` 시그니처 교체 + `CopyLineup` 추가 (9번)
- `Assets/Editor/AiRosterValidator.cs` — 로스터 SO 검증 메뉴 신규 (6번)
- `Assets/Editor/CardEntry.cs` / `CardCatalog.cs` — `Name` 필드 추가 (동일 인물 판정 키 `(이름, 팀)`에 필요)

**7번 `AiRosterBuilder.BuildTeam`**
- 흐름: null 체크 → 타선 → 선발 → 불펜 → 마무리, 하나라도 실패하면 `null` 반환
- 컬렉션은 실패 시 빈 배열, `BuildTeam`은 실패 시 `null` (CLAUDE.md 3-2). 호출자가 `Length`만 보면 판정되므로 `bool` + `out` 불필요
- `BuildLineup` 진입부에 `slots.Count != 9` 검사 추가 — 인스펙터에서 배열을 7칸으로 줄이면 `new HitterSnapshot[9]`와 어긋나 **뒤 2칸이 `default`(스탯 0)인 채로 통과**함. `BuildTeam`의 길이 검사도 배열이 9칸이라 못 잡음
- `BuildTeam`에서 SP/RP 길이를 재검사하는 이유 — `AiTeamRoster.GetPitcherStaff`가 `Array.Copy(..., 5)`로 RP 5칸을 상수 가정. 통과시키면 예외가 **경기 시뮬 도중**에 터져 원인 추적이 어려움
- 에러 로그에 팀명 + 타순/슬롯 번호 + 포지션을 담음 — 200칸 중 어디인지 바로 찾기 위함. `CardDataManager`가 남기는 조회 실패 로그와 2줄이 되지만, 앞줄은 사실·뒷줄은 맥락으로 역할이 다름

**8번 `AiRosterManager`**
- `[SerializeField] LeagueTierTable` 참조 1개만 보유 (세션 34 결정 — 보정 테이블 소유자는 SO)
- `BuildRosters(tier)` — 세트/플레이어팀 확인 → 팀 순회 → `Dictionary<string, AiTeamRoster>` 적재. 반환 `bool`
- **플레이어 팀은 건너뜀** (기획서 7.8 — 나를 제외한 9팀). 결과적으로 `GetRoster`는 "AI 팀 조회"라는 뜻이 정확해짐
- **실패 시 `_rosters.Clear()` 후 반환** — 3팀만 든 딕셔너리로 리그가 시작되면 일정 생성에서 엉뚱한 곳이 터짐. 전부 성공 아니면 아무것도 없음
- `_currentTier`는 **성공했을 때만** 갱신 — 실패했는데 티어만 바뀌어 있으면 이후 조회가 거짓말을 함
- 팀명 중복 검사 포함 — `Dictionary.Add`가 던지는 대신 원인이 보이는 로그로 대체
- `PlayerDataManager.Instance == null` 가드 — 씬 배치 누락 시 NRE 대신 원인 로그

**9번 `SimulationContextBuilder.Build()` 배선 교체 (TODO(8-1) 제거)**
- 새 시그니처: `Build(AiTeamRoster opponent, bool isPlayerHome, int playerRotationIndex, int opponentRotationIndex)`
- **`AiRosterManager`를 직접 부르지 않고 `AiTeamRoster`를 인자로 받음** — 누가 누구와 붙는지는 8-2(일정)의 책임. 빌더는 입력만 받는 순수 변환으로 유지
- 로테이션 인덱스를 플레이어/상대 **따로 받음** — 리그에서 두 팀의 선발 순번은 독립적으로 진행됨
- **`CopyLineup`으로 AI 라인업 사본을 넘김** — `GameSimulator.ApplyInterruptDecision`이 `context.HomeLineup`/`AwayLineup`에 **직접 덮어씀**(세션 26 방식 A). 원본을 그대로 넘기면 대타 교체 1회가 고정 로스터를 영구 오염시켜 다음 경기부터 다른 선수가 나옴. 144경기 일괄 시뮬에서 조용히 누적되는 유형
- `GetPitcherStaff`는 호출마다 새 배열을 만들므로 사본 불필요
- 호출자가 아직 없어 시그니처 변경의 파급 없음 (`git grep` 확인)

**6번 `AiRosterValidator`** — 메뉴 `Tools/BaseBallManager/AI 로스터 검증`
- 검사 항목: 팀명 존재 / 타선 9칸 / 빈 슬롯(`0`) / cardId 실존 / 타자·투수 종류 일치 / 슬롯 포지션 일치(DH 제외) / **수비 8자리 + DH 각 1회** / **cardId 중복** / **동일 인물(이름+팀) 중복** / SP5·RP5·CP1 개수 / 투수 역할 일치 / 세트의 팀 10개·빈 칸·중복
- 프로젝트 전체 에셋을 `AssetDatabase.FindAssets`로 훑음 — 임포터가 만든 것뿐 아니라 **인스펙터에서 손으로 고친 것도** 대상 (세션 34에서 임포터에 검증을 넣지 않은 이유가 이것)
- `Debug.LogError(msg, asset)` 형태로 **에셋을 context에 넣음** — 콘솔 로그를 클릭하면 해당 에셋이 핑됨
- **소속팀 불일치는 `LogWarning`** — 타 팀 선수 편성은 난이도 조정 의도일 수 있어 실패로 취급하지 않음
- `CardCatalog` 재사용 (CSV 재파싱 안 함). `CardEntry`에 `Name` 추가 — 라벨 문자열을 파싱해 이름을 꺼내는 건 표시 형식 변경에 깨짐

---

**✅ 8-1 완료 확정** — `AI 로스터 검증` 메뉴 실행 결과 `검증 통과 - 팀 10개 / 세트 1개`

---

### 세션 35 (계속) — 8-2 리그 일정 생성

**완성된 파일 목록**
- `Assets/Scripts/League/LeagueGame.cs` — 경기 1건 (홈/원정 + `Contains` / `GetOpponent`)
- `Assets/Scripts/League/LeagueGameDay.cs` — 하루치 5경기. **0번 칸이 항상 플레이어 경기**
- `Assets/Scripts/League/LeagueSchedule.cs` — 리그 1회분 전체 일정
- `Assets/Scripts/League/LeagueScheduleGenerator.cs` — 생성기 (static)
- `Assets/Editor/LeagueTierDefaultsFiller.cs` — 티어 21행에 기획서 7.2 경기 수·연전 수 일괄 입력
- `Assets/Scripts/League/LeagueTierEntry.cs` / `LeagueTierTable.cs` — `_gameCount` / `_seriesLength` + getter 2개 추가

📝 주요 설계 결정:
- **AI끼리의 경기도 일정에 포함** — 기획서 7.7 순위표는 10팀 전체의 승/패/무를 요구하고 해금 조건이 "정규시즌 2위 이상"이므로, 내 경기만 만들면 8-3에서 나머지 팀 성적의 출처가 없어짐. 하루 = 5경기(내 경기 1 + AI끼리 4)
- **원형 방식(circle method) 라운드 로빈** — 0번(플레이어)을 고정축으로 두고 나머지 9팀을 회전. 부수 효과로 **플레이어 경기가 항상 대진 0번 칸**에 오므로 `LeagueGameDay`에 별도 인덱스 필드가 불필요
- **연전은 "라운드 통째로 반복"** — 라운드 하나를 `seriesLength`번 반복하면 플레이어뿐 아니라 10팀 전부가 같은 상대와 연전이 됨. 플레이어만 3연전으로 맞추면 AI끼리는 매일 상대가 바뀌어 규칙이 갈라짐
- **`gameCount % seriesLength != 0`이면 생성 거부** — 연전이 중간에 끊기면 팀별 총 경기 수가 어긋나 순위표가 불공정해짐. 기획서 수치는 전부 나누어떨어짐(108/3, 144/3)
- **일수 = 라운드 경계로만 끊음** — 라운드 하나에 전 팀이 정확히 1경기씩 하므로 어디서 끊어도 팀별 경기 수가 같음
- **경기 수·연전 수를 `LeagueTierTable`에 둠** — 티어별 데이터의 소유자가 이미 이 SO. 인스펙터 튜닝 가능. 21행 수기 입력은 오타 위험이 커 채우기 메뉴를 함께 제공
- 생성기는 SO를 모름 (`gameCount` / `seriesLength`를 인자로 받음) — `AiRosterBuilder`가 `tierStatBonus`를 받는 것과 같은 판단

**🔬 홈/원정 배정 방식은 측정해서 골랐음**

초안(`라운드 번호마다 반전`)은 144경기에서 **팀별 홈경기가 56~89로 편중**됐음. 홈팀은 말 공격·끝내기 이점이 있어 순위표가 왜곡됨.
3가지 방식 × 사이클 반전 유무를 전부 계산해 비교한 뒤 **`대진 자리(i)별 교대 + 9라운드 사이클마다 전체 반전`** 채택.

| 리그 | 경기 수 | 채택안 홈경기 | 이상적 |
|---|---|---|---|
| 베이직2 | 36 | 18~18 | 18 |
| 아마추어 | 81 | 40~41 | 40 |
| 프로 | 108 | 54~54 | 54 |
| 마이너~ | 144 | 71~73 | 72 |

**검증 완료 (생성 로직을 그대로 옮겨 전 티어 계산)**
- 하루 5경기 / 10팀이 정확히 1번씩 출전 / 자기 자신과의 대진 0건
- 팀별 총 경기 수 전부 동일, 플레이어 홈·원정 완전 교대
- 연전 구간에서 상대가 바뀌지 않음

**⚠️ 기획서 확인 필요 2건 (현재는 문구 그대로 구현해 둠)**
1. **3연전 중에도 홈/원정이 매 경기 바뀜** — 기획서 7.3에 "프로부터 3연전"과 "내 팀은 경기마다 홈/원정 번갈아"가 함께 적혀 있음. 실제 야구는 3연전을 한 구장에서 치름. 현재는 후자를 그대로 따름
2. **144경기 + 3연전이면 상대별 경기 수가 15~18로 갈림** — 144가 27(9팀 × 3연전)의 배수가 아니어서 구조적으로 발생. 135(27×5)나 162(27×6)로 바꾸면 균등해짐

---

**✅ 8-2 컴파일 통과 확인** (2026-08-14)

---

### 세션 35 (계속) — 8-3 순위표

**완성된 파일 목록**
- `Assets/Scripts/League/TeamRecord.cs` — 팀 1개 성적 (승·패·무 + 득실 + 상대전적)
- `Assets/Scripts/League/LeagueStandingRow.cs` — 순위표 한 줄 (순위 + 성적 + 게임차)
- `Assets/Scripts/League/LeagueStandings.cs` — 순위표 본체

**완성된 메서드 목록**
- `TeamRecord.AddResult(opponent, runsScored, runsAllowed)` — 승/패/무 판정 + 득실 누적 + 상대전적 기록
- `TeamRecord.GetWinsAgainst / GetLossesAgainst` — 타이브레이커 3순위용
- `LeagueStandings.ApplyGameResult(game, homeScore, awayScore)` — 양 팀에 동시 반영
- `LeagueStandings.GetRanking()` — 승률 → 득실차 → 상대전적 정렬 + 게임차 계산

📝 주요 설계 결정:
- **승률에서 무승부 제외** (기획서 7.7) — `Wins / (Wins + Losses)`. 분모가 0이면 `0f`
- **상대전적을 `TeamRecord`가 직접 들고 있음** — 팀명별 승/패 딕셔너리 2개. "그 팀의 성적"이라는 책임 안에 들어감
- **타이브레이커를 한 비교 함수에 다 넣지 않고 2단계로 분리** — 상대전적은 비교 대상 두 팀에 따라 결과가 달라져(비추이적) 정렬기가 불안정해짐. ① 승률·득실차로 먼저 정렬 → ② **동률 구간만 잘라내** 그 구간 안의 상대전적으로 재정렬
- **동률 구간이 3팀 이상이어도 동작** — 구간에 속한 팀들끼리의 전적만 합산해 승률을 냄. 2팀 동률은 이 규칙의 특수한 경우일 뿐이라 분기 불필요
- **마지막 기준은 팀명(`CompareOrdinal`)** — `List.Sort`는 불안정 정렬이라 완전 동률이면 실행할 때마다 순서가 달라질 수 있음. 순위표가 새로고침마다 바뀌면 버그로 오인됨
- `ApplyGameResult`는 **한 팀이라도 없으면 아무것도 반영하지 않음** — 한쪽만 반영되면 승수 총합 ≠ 패수 총합이 되어 이후 순위가 조용히 틀어짐
- 게임차 = `((1위 승 - 팀 승) + (팀 패 - 1위 패)) / 2`
- `LeagueStandings`는 `GameResult`가 아니라 **점수 두 개(int)를 받음** — Simulation 레이어 의존을 만들지 않기 위함. 연결은 8-4가 담당

**검증 완료 (로직을 그대로 옮겨 계산, 오류 0건)**
- 10승5패5무 → 승률 .667 (무승부 제외 확인), 게임차 계산 일치
- 승률 동률 → 득실차로 갈림 / 승률·득실차 동률 → 상대전적으로 갈림
- 3팀 원형 동률(전원 2승2패·득실 동일) → 팀명 순으로 결과 고정
- 90경기 무작위 리그: 승 총합 = 패 총합, 득점 총합 = 실점 총합, 경기 수 정합, 정렬 역전 0건

---

**✅ 8-3 컴파일 통과 확인** (2026-08-14)

---

### 세션 35 (계속) — 8-4 리그 진행 + 🎉 **시뮬레이션 첫 실행 성공**

**플레이어 데이터 공백 대응: 방식 3 채택** (작성자 결정)
- AI 로스터를 플레이어 팀으로 임시 사용해 리그 로직을 먼저 완성. 실제 인벤토리·라인업 공급은 UI/세이브 작업에서 한 번에 해결
- `LeagueRunner._useAiRosterForPlayerTeam` / `LeagueManager`의 인스펙터 토글로 노출. 끄면 곧바로 실제 플레이어 라인업 경로를 탐

**완성된 파일 목록**
- `Assets/Scripts/League/LeagueGameScore.cs` — 경기 1건 최종 점수
- `Assets/Scripts/League/LeagueDayResult.cs` — 하루치 결과 (5경기 점수 + 플레이어 경기 상세)
- `Assets/Scripts/League/LeagueSeason.cs` — 진행 상태 (일정 + 순위표 + 진행도)
- `Assets/Scripts/League/LeagueRunner.cs` — 하루 단위 시뮬 + 순위표 반영 (순수 C#)
- `Assets/Scripts/League/LeagueManager.cs` — MonoBehaviour 싱글톤 진입점
- `Assets/Scripts/Builders/SimulationContextBuilder.cs` — `BuildAiVersusAi` 추가
- `Assets/Scripts/League/AiRosterManager.cs` — **세트 10팀 전부 보관하도록 변경**

📝 주요 설계 결정:
- **`AiRosterManager`가 플레이어 팀도 보관하도록 되돌림** (세션 35 앞부분 결정 수정) — 방식 3이 플레이어 팀 로스터를 필요로 함. 부수 효과로 `PlayerDataManager` 의존이 사라져 더 단순해짐. 상대 9팀 필터는 `GetOpponentTeamNames(playerTeamName)` 한 곳으로 모음
- **하루 5경기를 다 시뮬한 뒤 순위표에 한꺼번에 반영** — 중간 실패로 하루가 반만 반영되면 팀별 경기 수가 어긋나 순위가 조용히 틀어짐
- **선발 로테이션 인덱스 = 그 팀이 치른 경기 수** (`TeamRecord.GamePlayedCount`) — 별도 상태를 두지 않음. 기획서 6.2(3연전에서도 로테이션 연속)가 자동으로 충족됨. 단, **결과 반영 전에 읽어야 함**
- **일괄 시뮬은 중간 날짜의 타석 로그를 보관하지 않음** — 144일치 로그는 메모리만 먹음. 기획서 8.5(일괄 = 박스스코어 위주)와 일치
- `LeagueRunner`는 순수 C#, MonoBehaviour 의존 없음 (`IReadOnlyDictionary`로 로스터 주입) — 리그 일괄 시뮬 고속화 원칙 유지
- AI끼리의 경기는 `IsPlayerHome = false`로 둠 — 시뮬 코어는 이 값을 읽지 않고 UI 표기용 (전수 확인)

**🎉 시뮬레이션 첫 실행 (Unity 밖 검증 하니스, 프로젝트 미포함)**

`Assets/Scripts` **런타임 61개 전체**를 UnityEngine 최소 셰임과 함께 dotnet으로 컴파일 → **오류 0 · 경고 0**.
씨앗 CSV 로스터로 `Minor1`(144경기 · 3연전) 한 시즌 실행.

| 항목 | 결과 |
|---|---|
| 진행 | 144일 / **720경기** / **0.10초** / 오류 로그 0건 |
| 정합성 | 승합 684 = 패합 684 ✅ / 무 72(짝수) ✅ / 득점합 = 실점합 (4639) ✅ / 전 팀 144경기 ✅ |
| 순위 | 1위 삼성 89승48패7무 (.650) ~ 10위 키움 50승90패4무 (.357) |

- 5위 한화(.4783)와 6위 롯데(.4779)가 표시상 둘 다 `.478`이지만 실제 승률이 달라 정렬은 정상. **순위표 UI는 승률을 3자리로 반올림해 보여주므로 동률처럼 보일 수 있음**

**🔴 밸런스 실측 (300경기 표본) — 세션 32의 해석적 진단이 대체로 확인됨**

| 지표 | 실측 | KBO | 세션 32 추정 | 판정 |
|---|---|---|---|---|
| 타율 | .241 | .265~.277 | .224 | ❌ 낮음 |
| 출루율 | **.250** | ~.345 | .242 | ❌❌ 심각 |
| 장타율 | .418 | ~.400 | — | ⚠️ 높음 |
| OPS | .668 | ~.750 | .613 | ❌ 낮음 |
| 삼진율 | 24.2% | ~17.5% | 25.6% | ❌ 높음 |
| 볼넷율 | **1.4%** | ~9.3% | 1.5% | ❌❌ 심각 |
| 홈런율 | **3.4%** | ~2.0% | 2.45% | ❌ 과다 (추정보다 나쁨) |
| 3루타/안타 | 4.5% | ~1.5% | — | ❌ 3배 과다 |
| 실책율 | 0.7% | ~1.2% | — | ⚠️ 낮음 |
| 경기당 총득점 | 5.93 | ~9.0 | — | ❌ 낮음 |
| 타석당 투구수 | 3.25 | ~3.9 | — | ⚠️ 낮음 |

- **볼넷 공식 붕괴가 최우선 과제로 재확인** (세션 32 원인 1). 출루율이 타율과 거의 같다는 건 볼넷이 사실상 없다는 뜻
- 세션 32 추정과 다른 점: **홈런은 "정상"이 아니라 과다**(3.4%). 장타율이 KBO보다 높은데 타율·출루율은 낮은 기형적 구조
- 타석당 투구수가 낮은 것도 볼넷 부족의 결과로 보임

**🧹 정리**: `InventoryManager.cs`의 미사용 `using System.Runtime.InteropServices.WindowsRuntime;` 제거 (자동 임포트 잔여물)

---

## ⏭️ 다음 세션 착수 지점

### 세션 35 (계속) — 🎯 밸런스 튜닝 완료 (세션 32 진단 5건 해소)

**근본 원인: 서로 다른 평균의 스탯을 직접 뺐다**

`0.22 + 0.30 * (구위̂ - 정확̂)` 같은 공식은 **"두 스탯군의 평균이 같다"** 를 전제한다.
실제 카드 풀은 타자 정확 평균 **60.9**, 투수 구위·구속 평균 **72.0** — 11점 차이.
그래서 평균끼리 붙어도 늘 투수 우위로 기울었고, 볼넷 공식은 `contact * 0.4`로 타자 항을 감쇠까지 시켜 전체 매치업의 96.5%가 하한에 붙어 있었다.

**해결: 편차 정규화 (세션 32 권고안 채택)**

각 스탯을 **자기 집단 평균 대비 편차**(20점 = 1.0)로 바꾼 뒤 비교한다.
→ 평균 대 평균 대결이 정확히 기준 상수로 떨어지고, 기준 상수에 **KBO 실제 지표를 그대로 넣으면 리그 평균이 자동으로 맞는다.**

**완성/수정된 파일**
- `Assets/Scripts/ProbabilityModels/StatBaseline.cs` — **신규**. 카드 풀 평균 기준값 + `GetEdge` 단일 출처
- `BatterOutcomeCalculator.cs` — 5개 확률 전부 편차 기반으로 재작성, 기준 상수를 이름 있는 `const`로 분리
- `PitchCountCalculator.cs` — 파울 확률에 같은 결함이 있어 동일 방식으로 수정
- `BaseRunningCalculator.cs` — `TryAdvance`도 `run̂ - 0.5` 하드코딩이라 동일 방식으로 수정

📝 주요 설계 결정:
- **기준값을 `StatBaseline` 한 곳에 모음** — 네 군데 공식이 같은 평균값을 쓰는데 클래스마다 복제하면 CSV 개편 시 한쪽만 고쳐져 조용히 어긋남
- **상수를 `const`로 두고 SO로 빼지 않음** — 확률 계산기는 순수 C#이라 `[SerializeField]` 불가(세션 22 판단과 동일). 인스펙터 튜닝이 필요해지면 `GameSimulator`가 생성자로 주입하는 방식으로 확장
- 클램프 범위를 실제 도달 범위에 맞게 조정 (삼진 5~40%, 볼넷 2~25%, 홈런 0.2~10%, 안타 12~50%)

**📊 튜닝 결과 (300경기 실측)**

| 지표 | 튜닝 전 | 튜닝 후 | KBO |
|---|---|---|---|
| 타율 | .241 | **.275** | .265~.277 |
| 출루율 | .250 | **.337** | ~.345 |
| 장타율 | .418 | **.401** | ~.400 |
| OPS | .668 | **.739** | ~.750 |
| 삼진율 | 24.2% | **17.7%** | ~17.5% |
| 볼넷율 | 1.4% | **9.0%** | ~9.3% |
| 홈런율 | 3.4% | **2.1%** | ~2.0% |
| 3루타/안타 | 4.5% | **1.3%** | ~1.5% |
| 실책율 | 0.7% | **1.0%** | ~1.2% |
| 경기당 총득점 | 5.93 | **8.24** | ~9.0 |
| 타석당 투구수 | 3.25 | **3.98** | ~3.9 |

**선수 변별력 확인 (클램프 포화 없음)**
| 항목 | 최상위 카드 | 최하위 카드 |
|---|---|---|
| 삼진율 (정확 86 / 38) | 6.5% | 30.5% |
| 볼넷율 (정확 86 / 38) | 17.3% | 2.9% |
| 홈런율 (파워 82 / 52) | 4.2% | 0.4% |

**⚠️ 남은 차이와 그 이유 (의도적 수용)**
- **출루율 -.008**: 사구(HBP)가 모델에 없음. KBO 출루율은 사구 ~1.2%p를 포함하므로 **구조적으로 낮게 나오는 것이 정상**
- **경기당 득점 -0.8**: 진루 규칙 단순화(세션 20 의도적 미구현 — 1·2루 태그업 없음, 도루 없음, 실책 후 추가 진루 없음) 때문. 여기서 안타 확률을 더 올리면 타율이 목표를 벗어남 → **타격 지표 정확도를 우선**

**📌 순위표에서 오해하기 쉬운 표시 2가지 (버그 아님)**
- 승률이 화면상 같아 보여도(`.478` vs `.478`) 실제 값이 달라 순위가 갈릴 수 있음 — 3자리 반올림 표시 때문
- **게임차가 같은데 순위가 다를 수 있음** (예: 1위 86승51패 / 2위 88승53패 → 둘 다 0게임차). KBO도 동일하며 순위 기준은 승률

---

### 세션 35 (계속) — 8-5 포스트시즌 + 8-6 시즌 저장 → **로드맵 8번 전체 완료**

**완성된 파일 목록**
- `Assets/Scripts/League/LeagueGameContextFactory.cs` — 리그·포스트시즌이 공유하는 컨텍스트 생성부 (`LeagueRunner`에서 추출)
- `Assets/Scripts/League/PostSeasonRound.cs` — 단계 enum (WC / 준PO / PO / KS)
- `Assets/Scripts/League/PostSeasonSeries.cs` — 시리즈 1개 (대진 · 승수 · 경기 기록)
- `Assets/Scripts/League/PostSeasonRunner.cs` — 대진표 구성 + 시리즈 진행
- `Assets/Scripts/Save/ISaveStorage.cs` — 저장소 인터페이스 (기획서 10장)
- `Assets/Scripts/Save/LocalFileStorage.cs` — 로컬 JSON 파일 저장소
- `Assets/Scripts/League/LeagueSaveData.cs` — 저장 DTO 2개
- `Assets/Scripts/League/LeagueSaveService.cs` — 정규시즌 진행도 저장·복원
- `LeagueManager` — `StartPostSeason` / `SaveLeague` / `LoadLeague` / `DeleteSavedLeague` / `HasSavedLeague` 추가

**8-5 설계 결정**
- **와일드카드 1승 어드밴티지는 "시작 승수"로 표현** — `HigherSeedWins`를 1에서 시작하고 2승 도달 시 진출. 4위는 1승, 5위는 2연승이 필요해져 기획서 7.5와 정확히 일치. 별도 예외 규칙이 필요 없음
- **무승부는 승수를 올리지 않아 자동으로 재경기가 됨** — KBO 포스트시즌 규칙과 같음. 대신 무승부 반복으로 무한 루프가 되지 않도록 시리즈당 `WinsToClinch * 2 + 5` 상한
- **홈/원정은 단계별 배정표(`bool[]`)** — 5전제 `홈홈원원홈`, 7전제 `홈홈원원원홈홈`. 재경기로 배정표를 넘어가면 상위 시드 홈으로 처리
- **선발 로테이션을 정규시즌에서 이어감** — 팀별 누적 경기 수를 `Dictionary`에 옮겨 담고 경기마다 증가 (기획서 6.2)
- `Create()`가 조건(정규시즌 종료 / 144경기 / 5팀 이상)을 검사하고 실패 시 `null` — 144경기 미만 리그는 `LogWarning` 후 `null` (기획서 7.5 - 포스트시즌 없이 최종 순위로 마감)

**8-6 설계 결정**
- **일정은 저장하지 않고 재생성** — 같은 입력이면 같은 일정이 나오므로 저장할 이유가 없음. 대신 **팀 순서(0번=플레이어)** 를 저장해야 재생성 결과가 일치함
- **경기 수·연전 수를 세이브에 함께 저장** — 복원 시 티어 테이블이 아니라 **저장 당시 값**으로 일정을 다시 만듦. 시즌 도중 인스펙터에서 테이블을 고쳐도 진행 중인 시즌이 깨지지 않음
- **상대전적은 나란한 배열 3개로 폄** — `JsonUtility`는 `Dictionary`를 직렬화하지 못함. 복원 시 길이가 어긋나면 `LogError` 후 상대전적만 버림
- **복원 전용 생성자 추가** (`TeamRecord` / `LeagueStandings` / `LeagueSeason`) — 기존 누적 경로(`AddResult`)는 그대로 두고 별도 입구를 냄. `private set`을 열지 않아 일반 코드에서는 여전히 수정 불가
- 저장소 교체 지점은 `LeagueManager.Awake()`의 `new LocalFileStorage()` 한 줄 — 서버 저장으로 바꿀 때 여기만 바뀜

**✅ 검증 (하니스 실행, 오류 0건)**

포스트시즌
| 단계 | 결과 | 경기 수 |
|---|---|---|
| 와일드카드 | KT 2-0 두산 → KT | **1경기** (4위 어드밴티지 정상 동작) |
| 준플레이오프 | KT 3-1 LG → KT | 4경기 |
| 플레이오프 | 삼성 3-2 KT → 삼성 | 5경기 |
| 한국시리즈 | 삼성 4-2 한화 → **삼성 우승** | 6경기 |

홈/원정 배정도 배정표대로 적용됨 (준PO: LG홈 2 → KT홈 2 / KS: 한화홈 2 → 삼성홈 3 → 한화홈 1)

세이브 왕복 (프로1, 108경기 중 40일차에서 저장)
| 항목 | 결과 |
|---|---|
| 진행도 | ✅ 40일차 그대로 |
| 티어·플레이어 팀 | ✅ |
| 승·패·무·득실 (10팀) | ✅ |
| 상대전적 (10팀 × 9상대) | ✅ |
| 일정 재생성 | ✅ **108일 전량 비교 일치** |
| 이어하기 | ✅ 68일 추가 진행 → 전 팀 108경기 완료 |
| 파일 크기 | 약 7KB |

**⚠️ 의도적으로 넣지 않은 것 (기획서 대비)**
- **포스트시즌 진행도는 저장하지 않음** — 기획서 7.6은 "리그 진행도 저장"만 명시. 포스트시즌은 최대 18경기라 한 번에 진행하는 흐름을 전제함. 필요해지면 `PostSeasonSaveData` 추가
- **순위 동률 시 타이브레이커 경기 미구현** (기획서 7.5) — 순위표가 이미 승률→득실차→상대전적 3단계로 가르므로 실제로 완전 동률이 남을 확률이 극히 낮음. 필요 여부 판단 필요

---

## ✅ 로드맵 8번(리그 시스템) 전체 완료

| 단계 | 상태 |
|---|---|
| 8-0 데이터 브릿지 / 8-1 AI 로스터 / 8-2 일정 / 8-3 순위표 / 8-4 진행 / 8-5 포스트시즌 / 8-6 저장 | ✅ |
| 밸런스 튜닝 | ✅ |

---

### 세션 36 (2026-08-16) — 플레이어 배관 완성 (재화 · 분해 · 세이브 · 시작 플로우 · 보상/해금)

**작업 방식**: 작성자 요청으로 Claude가 전 구간 직접 작성

**착수 배경 — 전수조사로 확인한 공백**

로드맵 6·7·8번(시뮬·인터럽트·리그)은 완료됐지만 **플레이어가 게임에 존재하지 않는 상태**였다.
리그가 돌아가던 것은 `_useAiRosterForPlayerTeam = true`(플레이어 자리에 AI 로스터를 끼움) 덕분.
기획서 핵심 루프(뽑기 → 육성 → 라인업 → 리그 → 보상)에서 **화살표가 하나도 연결돼 있지 않았음**.

| 끊긴 곳 | 확인 방법 | 증상 |
|---|---|---|
| 재화 시스템 자체가 없음 | `CurrencyManager` 파일 0건 | 뽑기·훈련·돌파가 **공짜 무한** |
| `GetTrainCost`를 아무도 호출 안 함 | 전 파일 검색 | 세션 12 기록대로 비용 미구현 |
| 카드 분해 없음 | 관련 파일 0건 | 200장 한도가 차면 **영구 잠김** |
| 플레이어 세이브 없음 | `Save/`에 리그 저장만 | **앱 끄면 카드 전멸** |
| 게임 시작 플로우 없음 | 기획서 5장 대응 코드 0건 | 신규 진입 경로 없음 |
| 리그 보상·해금 없음 | "2위 이상" 판정 0건 | 이겨도 아무 일 없음 |

→ 로드맵 9번(경기 UI)보다 **아래층**이므로 순서를 앞당겨 처리 (UI는 보여줄 데이터가 있어야 함)

**완성된 파일 목록 (신규 11개)**
- `Assets/Scripts/Currency/CurrencyType.cs` — 재화 12종 enum (기획서 9.1)
- `Assets/Scripts/Currency/CurrencyCost.cs` — 재화 1종의 소모량 (readonly struct)
- `Assets/Scripts/Currency/CurrencyManager.cs` — 싱글톤 MonoBehaviour
- `Assets/Scripts/Dismantle/DismantleResult.cs` — 분해 1건 보상
- `Assets/Scripts/Dismantle/DismantleManager.cs` — 싱글톤 MonoBehaviour (기획서 9.2)
- `Assets/Scripts/Player/PlayerSaveData.cs` — 저장 DTO 3개
- `Assets/Scripts/Player/PlayerSaveService.cs` — 플레이어 진행 데이터 저장·복원
- `Assets/Scripts/Player/GameFlowManager.cs` — 신규 시작 / 이어하기 / 저장 진입점 (기획서 5장)
- `Assets/Scripts/League/LeagueRewardResult.cs` — 보상 수령 결과
- `Assets/Scripts/League/LeagueRewardService.cs` — 종료 보상 + 티어 해금 (기획서 7.1 · 7.9)
- `Assets/Scripts/Line Up/LineUpAutoFill.cs` — 라인업 자동 편성

**수정된 파일 목록 (16개)**
- `GachaManager` — 뽑기권 소모 + 천장 카운터 getter/`RestorePityCounts`
- `TrainManager` — `SpendAll(포인트 + 훈련카드)`, 기본 비용값 부여
- `BreakthroughManager` — 돌파 카드 차감
- `InventoryManager` — `AddCard` 반환값 `int`, `TryExpandCapacityWithGold`, `Restore`, `NextInstanceId`
- `CardInstance` — 세이브 복원 전용 생성자
- `CardStatsCalculator` — `CalculateFinalOVR` 추가 (기획서 1.3 표시용 OVR)
- `LineUpManager` — `IsCardAssigned` public화, `GetHitterSlot`, `ClearAll`
- `PlayerDataManager` — 해금 티어 / 튜토리얼 완료 플래그 / `Restore`
- `LeagueTierEntry` · `LeagueTierTable` — `_goldReward`, `_rankGoldMultipliers`, getter 2개
- `LeagueSeason` · `LeagueSaveData` · `LeagueSaveService` — `RewardsGranted` 저장
- `LeagueManager` — 해금 검사 · 라인업 완성 검사 · `ClaimSeasonRewards`, 토글 기본값 `false`
- `SimulationContextBuilder` — 라인업 미완성 가드

📝 주요 설계 결정:
- **`Dictionary<CurrencyType, int>` 채택** — 재화 12종에 골글 제작 재료(TBD)가 더 붙을 수 있음. 필드 12개면 프로퍼티 12 + 메서드 24개가 되고 항목 추가마다 클래스 수정(OCP 위반). 대가인 직렬화는 **나란한 배열 2개**로 폄 — 세션 35의 상대전적 저장과 같은 규약
- **`SpendAll`은 "전부 검사 → 전부 차감" 2패스** — 훈련은 포인트 + 훈련카드 동시 소모(기획서 2.2)인데 순차 차감하면 뒤에서 실패했을 때 앞의 재화만 사라짐. **강화·훈련·돌파는 되돌릴 수 없어**(기획서 2장) 부분 차감이 곧 영구 손실
- **뽑기권은 뽑은 뒤에 차감** — 먼저 차감하면 카드 풀이 비었을 때 환불 로직이 필요해짐. `CanAfford`로 사전 확인 → 성공분만 `Spend`. 10연은 **실제로 나온 장수만큼만** 차감
- **`Spend` 실패는 `LogWarning`, `type == None`은 `LogError`** — 재화 부족은 플레이어가 흔히 만드는 예상 가능한 실패, None은 호출부 버그 (CLAUDE.md 3-2)
- **`CurrencyManager`는 금고일 뿐 지급 규칙을 갖지 않음** — 시작 보상은 `GameFlowManager`, 리그 보상은 `LeagueRewardService`, 분해 보상은 `DismantleManager` 소유. 매니저가 스스로에게 재화를 주면 세이브 복원 시 초기값이 덧씌워짐
- **세이브를 플레이어/리그 2개로 분리** — 수명이 다름. 리그는 재도전 시 버려지지만(기획서 7.6) 카드·재화·해금은 계속 유지됨
- **라인업 복원은 검증 우회 없이 `Assign*` 경로 재사용** — 별도 주입구를 내면 검증이 두 벌이 됨. CSV에서 카드 포지션이 바뀌는 등으로 무효가 된 편성은 여기서 걸러져 빈 슬롯이 되고, 그 결과 "리그 입장 불가"라는 **눈에 보이는 실패**가 됨
- **복원 순서 = 팀·해금 → 재화 → 카드 → 천장 → 라인업** — 라인업 검증이 인벤토리를 참조하므로 카드보다 뒤여야 함
- **`InventoryManager.Restore`가 `nextInstanceId` 정합성 검사** — 발급 ID가 보유 카드 ID보다 작으면 다음 뽑기가 기존 카드와 같은 ID를 받아 라인업 참조가 조용히 엉킴
- **분해는 카드 소멸 → 보상 지급 순서** — 반대로 하면 제거 실패 시 보상만 남아 카드가 복제됨
- **분해 성장 보정은 종류 배수를 타지 않음** — `(등급값 × 종류배수) + 성장분`. 성장분까지 곱하면 5성 골글을 풀강해 분해하는 쪽이 압도적으로 이득이 되어 육성 동기가 뒤집힘
- **`RewardsGranted`를 세이브에 포함** — 없으면 "저장 → 보상 수령 → 재실행 → 다시 수령" 무한 반복이 가능
- **보상 수치는 `LeagueTierTable` SO 소유** — 티어별 데이터의 소유자는 SO라는 세션 34 결정 유지. 순위 배수는 티어 무관이라 SO 본체 필드
- **`_useAiRosterForPlayerTeam` 기본값 `false` + 헤더를 "디버그"로 변경** — 공급 경로가 생겼으므로 실제 게임 경로가 기본. 필드 자체는 남김(하니스가 라인업 없이 리그를 돌리는 데 사용)
- **`LineUpAutoFill`은 수비 8자리 먼저, DH 나중** — 제약이 강한 쪽부터. 반대면 최고 OVR 선수가 DH로 빠져 포수 자리가 빔 (세션 32와 같은 판단)
- **자동 편성 기준은 최종 OVR** (`CalculateFinalOVR`) — 강화·훈련이 반영돼야 육성한 카드가 우선 배치됨

**✅ 검증 (Unity 밖 dotnet 하니스, 프로젝트 미포함)**

런타임 **81개 전체 컴파일 — 오류 0**. (경고 12건은 전부 CS0649/CS0414로, 셰임이 `[SerializeField]` 주입을 모르는 탓. Unity에서는 발생하지 않음)

기능 테스트 **69개 전부 통과**

| 구간 | 확인 내용 |
|---|---|
| 게임 시작 | 잘못된 팀 거부 / 뽑기권 50 지급 / **튜토리얼 보상 중복 차단** / Basic1만 해금 |
| 뽑기권 | 10연 5회 = 50장 + 뽑기권 0 / 부족 시 1연·10연 차단 / 차단 시 카드 안 늘어남 |
| 훈련 | 포인트만 있고 훈련카드 없으면 실패 + **포인트 미차감**(SpendAll 2패스 확인) |
| 분해 | 잠금 차단 / **라인업 편성 카드 차단** / 포인트 획득 / 카드 소멸 |
| 자동 편성 | 타순 9명 중복 0 / 20칸 전부 다른 카드 |
| 세이브 왕복 | 팀·해금·튜토리얼·재화·카드 수·발급 ID·천장·훈련 레벨·**훈련 분배값**·타순 전부 일치 |
| 보상·해금 | 진행 중 수령 차단 / 1위 → 해금 / 이미 열린 티어는 재해금 없음 / 최하위는 해금·골카 없음 / **중복 수령 차단** |

---

## 🔴 세션 36에서 발견한 기획 이슈 — 시작 뽑기권 50개로는 라인업을 못 짠다

자동 편성으로 **50회 시행 측정** (뽑기 = 전 팀·전 포지션 랜덤이므로 포지션 편중이 발생).

| 항목 | 결과 |
|---|---|
| 라인업 20칸 완성에 필요한 카드 수 | **평균 64.2장** (최소 30 / 최대 100) |
| **시작 지급분 50장만으로 완성** | **12/50회 (24%)** |

- 원인: 수비 8포지션 + SP5 · RP5 · CP1을 **전부** 채워야 하는데(기획서 6.3), 랜덤 뽑기는 포수·마무리처럼 풀이 얇은 자리를 자주 비움
- 즉 신규 유저의 **76%가 첫 리그에 입장하지 못한다**
- 선택지 (작성자 판단 필요)
  1. 시작 뽑기권을 50 → 80~100개로 상향
  2. 시작 시 **포지션이 보장된 기본 로스터**를 지급 (컴프야 방식)
  3. 리그 입장 조건을 완화 (기획서 6.3 "한 자리라도 비면 입장 불가"와 충돌)
- ⚠️ 현재 CSV에 **시그니쳐 카드가 0장**이라 기획서 5장의 "기본 시그니쳐 1장 지급"도 실제로는 지급되지 않음 (`LogWarning` 후 팀 선택만 성공 처리)

---

### 세션 37 (2026-08-16) — 경기 기록 데이터 레이어 (로드맵 9번의 선행 작업)

**브랜치**: `feature/game-record-system` (세션 36 작업분은 `feature/currency-save-system`)

**착수 배경 — UI보다 데이터가 먼저 없었음**

기획서 8.5는 ① 실시간 로그 ② 박스스코어(이닝별 득점 + 선수별 기록)를 요구하는데,
`SimulationBatterLog`가 가진 것은 `Outcome / PitchCount / RunsScored / BatterName` 4개뿐이었다.
**이닝도, 어느 팀 공격인지도, 투수가 누구인지도, 누가 홈을 밟았는지도 기록되지 않아** 화면을 그릴 재료 자체가 없었다.

⚠️ **UI(씬·Canvas·MonoBehaviour)는 이번 세션에서 손대지 않음** — 화면 구성 뼈대를 먼저 잡기로 함(작성자 결정).
로그 문자열 포맷터도 "어떻게 보여줄지"라 UI 설계 영역이므로 제외했다.

**완성된 파일 목록 (신규 5개)**
- `Assets/Scripts/Record/HitterGameStats.cs` — 타자 1명의 경기 기록 (타석·타수·안타·2·3루타·홈런·타점·득점·볼넷·삼진)
- `Assets/Scripts/Record/PitcherGameStats.cs` — 투수 1명의 경기 기록 (아웃·투구수·상대타자·피안타·피홈런·실점·볼넷·탈삼진)
- `Assets/Scripts/Record/InningScore.cs` — 라인스코어 한 칸 (+ `HomePlayed`)
- `Assets/Scripts/Record/BoxScore.cs` — 박스스코어 본체 (R/H/E + 선수별 목록 4종)
- `Assets/Scripts/Record/BoxScoreBuilder.cs` — 타석 로그 → 박스스코어 집계 (static)

**수정된 파일 목록 (3개)**
- `SimulationBatterLog` — `Inning` / `IsTopInning` / `OutCountBefore` / `BatterInstanceId` / `PitcherInstanceId` / `PitcherName` / `ScoredRunnerIds` 추가
- `BaseRunningCalculator` — `Apply()`에 `List<int> scoredRunnerIds` 매개변수 추가, `Score()`가 홈을 밟은 주자를 기록
- `GameSimulator` — 확장 로그 채우기 + 득점 주자 버퍼

📝 주요 설계 결정:
- **집계를 시뮬 코어에 넣지 않고 사후 변환(`BoxScoreBuilder`)으로 분리** — 일괄 시뮬은 경기마다 박스스코어가 필요 없다(기획서 8.5는 일괄 = 박스스코어 위주지만, 그것도 "본 경기"에 한함). 720경기마다 선수 객체 32개를 만들면 순수 낭비. 필요한 경기에서만 호출
- **`GameResult`는 손대지 않음** — 로그만 들고 있으면 언제든 집계할 수 있음. 시뮬 코어의 반환 계약을 넓히지 않았다
- **득점 주자 버퍼는 `GameSimulator`가 소유하고 재사용** — 타석마다 `List`를 새로 만들면 일괄 시뮬에서 GC 압박. `Clear()` 후 넘기고, 로그에는 그 타석 몫만 `ToArray()`로 복사 (득점 없으면 `Array.Empty`라 할당 0)
- **선수별 득점(R)을 위해 `Score()` 시그니처를 바꾼 이유** — `RunsScored`는 "그 타석에서 몇 점 났나"일 뿐 **누가 홈을 밟았는지**를 담지 못한다. KBO 박스스코어 표준 열(타수-득점-안타-타점)에서 득점이 빠지면 반쪽이 됨
- **실책 득점은 타점에서 제외** — 야구 기록 규칙. 병살타·밀어내기 볼넷 득점은 타점으로 인정
- **타수 = 타석 - 볼넷 - 희생플라이** — 이 프로젝트엔 몸에 맞는 공·희생번트가 없어 두 항목만 빠짐
- **`InningScore.HomePlayed`로 "0점"과 "공격 없음"을 구분** — 라인스코어에 `0`이 아니라 `X`를 찍어야 하는 칸이 있음
- **`Dictionary`(조회) + `List`(순서) 병행** — 등장 순서가 곧 타순·등판 순서. `Dictionary` 열거 순서는 명세상 미보장이라 표시용으로 쓸 수 없음 (세션 34와 같은 판단)
- **득점 주자가 타석 기록에 없으면 `LogError`** — 도달 불가능한 경로(홈을 밟으려면 먼저 타석에 섰어야 함)라, 발생한다면 진루 로직 버그 신호

**✅ 검증**

**① Unity 실컴파일 통과 (가장 확실한 증거)**
- `Library/ScriptAssemblies/Assembly-CSharp.dll`이 전 소스보다 최신이고, 신규 타입
  (`BoxScoreBuilder` / `HitterGameStats` / `CurrencyManager` / `LineUpAutoFill` / `PlayerSaveService`)이 **DLL에 실제 포함**됨
- `Editor.log`에 `error CS` **0건**
- → 셰임 기반 dotnet 컴파일보다 강한 신호. Unity 6000.0.49f1 Roslyn이 직접 통과시킨 것

**② 하니스 클린 빌드 + 기능 테스트 109개 전부 통과** (세션 36의 69개 + 이번 40개)

*집계 총합 대조 (실제 200경기)*

| 검사 | 내용 |
|---|---|
| 라인스코어 합 = 최종 점수 | 이닝별 득점을 다 더하면 경기 스코어와 일치 |
| **선수별 득점 합 = 팀 득점** | 득점 주자 추적이 정확한지 (이번 변경의 핵심) |
| 투수 실점 합 = 상대 팀 득점 | 실점 귀속이 정확한지 |
| 선수별 안타 합 = 팀 안타 / 타석 합 = 로그 수 | 누락·중복 0 |
| 홈 투수 상대 타자 = 원정 타자 타석 | 공수 귀속이 뒤바뀌지 않았는지 |

*규칙 단위 검증 (합성 로그 — 총합이 맞아도 규칙이 서로 상쇄돼 틀릴 수 있으므로 별도로 확인)*

| 검사 | 결과 |
|---|---|
| 홈런 = 안타 1 + 홈런 1 + 타점 3 + 본인 득점 1 | ✅ |
| 주자 득점 누적 (한 선수가 2회 득점) | ✅ |
| 볼넷·희생플라이는 타수 제외 / 실책은 타수 포함·타점 제외 | ✅ |
| 실책은 **수비팀**에 귀속 (초 → 홈 실책) | ✅ |
| 병살 아웃 2개 / 이닝 표기 `1 1/3` | ✅ |
| 말 이닝 아웃은 원정 투수에 귀속 (공수 귀속 역전 없음) | ✅ |
| 타석 기록 없는 주자가 득점해도 유령 선수를 만들지 않음 | ✅ |
| 재화 오버플로·0 이하 지급·None 지급 거부 / SpendAll 부분 차감 방지 | ✅ |
| 발급 ID 역전된 세이브 거부 / 깨진 훈련 분배값 4칸 확보 | ✅ |
| 자동 편성 연속 호출 안전 (ClearAll 동작) | ✅ |

경기당 평균: 득점 7.21 · 안타 17.83 · 실책 0.61 · 타석 78.3 / 연장 15건(200경기 중), 최장 11회

📝 **검증 과정 기록**: 규칙 단위 검사 최초 작성 시 6건이 실패했으나, 전부 **테스트 기대값 산수 오류**였다
(말 이닝 땅볼을 홈 투수 아웃으로 잘못 셈, 홈런 타자의 2타석째 실책을 안타로 셈 등).
합성 로그로부터 정답을 손으로 먼저 계산한 뒤 실제 산출값을 덤프해 대조했고, **6개 항목 전부 손 계산과 일치**했다.
프로덕션 코드는 수정하지 않았다.

---

## ✅ 세션 37에서 발견·수정한 규칙 오류 — 홈팀이 이겨도 마지막 회 말을 쳤다

`InningScore.HomePlayed`를 만들면서 드러났다. 수정 전 **200경기 전부 마지막 이닝 말 공격을 치렀다(생략 0건).**

`GameState.AddOut()`이 초 이닝 3아웃 시 **점수와 무관하게** 말 이닝으로 넘기고, 경기 종료 판정은 말 이닝이 끝난 뒤에만 했다.
그래서 홈팀이 9회초까지 리드 중이어도 9회말을 진행했다. 실제 야구는 이 시점에 경기가 끝난다.

| 수정 전 측정 (200경기) | 값 |
|---|---|
| 실제 야구라면 치르지 않았을 마지막 회 말 | **84경기 (42%)** |
| 그 이닝에서 홈팀이 추가한 득점 | **29점** (경기당 0.145점) |

**영향**: 승패는 안 바뀌지만 **득실차가 부풀려졌다.** 득실차는 순위 타이브레이커 2순위(기획서 7.7)라
홈경기가 많은 팀이 유리해지고, 홈 타자 개인 기록도 함께 부풀려졌다.

**수정** (`GameState.AddOut()`, 작성자 지시로 반영)
```
if (IsTopInning)
{
    //9회 초가 끝난 시점에 홈팀이 앞서 있으면 말 공격 없이 종료
    if (Inning >= 9 && HomeScore > AwayScore) { IsGameOver = true; return; }
    IsTopInning = false;
    ...
}
```
- 초 이닝 3아웃 시점(`IsTopInning`이 아직 true)에 판정하므로 **9회 초가 끝나는 그 순간** 종료된다
- 동점이거나 원정팀이 앞서면 평소대로 말 공격을 진행 (끝내기는 기존 `AddRun()`이 담당)
- 연장 11회에도 동일하게 적용됨

**수정 후 검증 (2000경기)**

| 항목 | 결과 |
|---|---|
| 규칙 위반 (리드 중인데 말을 침) | **0경기** |
| 정상 종료 (9회 초에서 끝남) | **755경기 (37.8%)** |
| 원정 총득점 vs 홈 총득점 | 8094 vs 7456 |

- 홈팀 득점이 낮아진 것은 **정상**이다. 실제 야구도 홈팀은 이기고 있으면 마지막 공격을 하지 않으므로 타석 수가 적다
- 이 프로젝트는 홈 어드밴티지(홈팀 승률 보정)를 따로 모델링하지 않으므로, 홈팀이 원정팀보다 조금 적게 득점하는 형태가 된다.
  리그 일정이 팀별 홈·원정 경기 수를 맞추므로(세션 35) 순위에는 편향이 생기지 않는다

---

### 세션 38 (2026-08-16) — 전 모듈 · 통합 테스트 (UI 착수 전 전수 검증)

**목적**: UI로 넘어가기 전에 로직을 전부 검증해, 남은 작업이 "화면 디자인"만 되도록 함 (작성자 지시)

**방식**: dotnet 하니스에 모듈 테스트 + 플레이어 여정 통합 테스트를 작성해 실행.
테스트 코드는 검증 후 삭제 (프로젝트에 넣지 않음 - 스크래치패드 전용)

**✅ 최종 결과: 309개 전부 통과**

| 구간 | 검증 내용 |
|---|---|
| 카드·마스터 | CSV 325장 로드 / cardId 유일·대역 / OVR = 4스탯 평균 / 최종 스탯·OVR 공식 / 포지션 파서 |
| 인벤토리 | 발급 ID / 중복 보유 / 잠금 / 200장 한도 / 골드 확장 / 필터 4종 |
| 강화 | 동일성 판정 / 잠금 제외 / 재료 소멸 / 최대 10 / 만렙 시 재료 보존 |
| 훈련·돌파 | 상한 30→50 / 레벨당 +2 분배 / 비용 비례 / 등급별 돌파 카드 / 중복 돌파 차단 |
| 라인업 | 포지션 제약 / DH 와일드카드 / 중복 배치 / 타순 중복·범위 / 벤치 / 완성 판정 |
| 경기 상태머신 | 이닝 전환 / 잔루 소멸 / 끝내기 / **9회초 종료** / 연장 11회·무승부 / 투수 피로 |
| 진루 규칙 | 8.3 표 전 항목 (홈런·3루타·2루타·단타·볼넷·실책·병살·희플·아웃) |
| 투수 교체 | 점수제 / 세이브 상황 / 사용 슬롯 스킵 / 소진 시 -1 / 투구 수 하한 |
| 리그 일정 | 3개 티어 × (하루 5경기·10팀 1회·경기 수 동일·홈 편중·연전 유지·홈원정 교대) |
| 순위표 | 승률 무승부 제외 / 게임차 / 득실차·상대전적 타이브레이커 |
| 포스트시즌 | WC 1승 어드밴티지 / 무승부 재경기 / 7전 4선승 / 144경기 미만 차단 |
| 인터럽트 | 예약 검증 / 버퍼 클리어 / **벤치 없는 팀 보호** |
| 기록 | 박스스코어 규칙 17건 + 300경기 집계 정합성 |
| 재화·세이브 | 오버플로 / SpendAll 원자성 / 발급 ID 역전 거부 / 저장소 CRUD |
| **통합** | 신규 시작 → 뽑기 → 강화·훈련 → 분해 → 라인업 → 144경기 → 포스트시즌 → 보상·해금 → 저장 → 앱 재시작 → 이어하기 |

---

## 🐛 세션 38에서 발견·수정한 버그 2건

**① `PitcherChangeEvaluator.GetNextPitcherSlot` — 공수 판별이 반대**

```csharp
bool isHomePitching = !gameState.IsTopInning;   // 잘못됨
```
`IsTopInning == true`는 **초 = 원정 공격 = 홈 수비**다. `GameSimulator`는 그렇게 쓰는데
(`defPitcherState = isTopInning ? HomePitcherState : ...`) 이 클래스만 반대로 판별했다.

| 결과 | 증상 |
|---|---|
| `IsPitcherUsed(isHomePitching, ...)` | **상대 팀의 사용 완료 목록**을 조회 → 이미 내려간 투수 재등판 / 멀쩡한 투수 스킵 |
| 세이브 상황 판정 | 점수차를 상대 팀 기준으로 계산 → **지고 있을 때 마무리 투입**, 이기고 있을 때 미투입 |

세션 23에서 "지고 있는 팀이 CP를 쓰는 오류 방지"로 부호 있는 점수차를 도입했는데,
세션 26에서 팀 판별을 뒤집으면서 그 버그가 되살아나 있었다. → `= gameState.IsTopInning`으로 수정

**② `GameSimulator.ApplyInterruptDecision` — 이닝이 넘어간 뒤의 공수를 읽어 교체가 상대 팀에 적용 (크래시)**

`ApplyInterruptDecision`이 `state.IsTopInning`을 **`Apply()` 이후에** 읽었다.
`Apply()`는 3아웃이면 공수를 뒤집으므로, **타석이 이닝을 끝내는 순간 예약된 대타가 상대 팀 라인업에 들어갔다.**
AI 팀은 벤치가 `Array.Empty`라 `attackBench[sub.BenchIndex]`에서 **IndexOutOfRangeException**으로 경기가 죽는다.

`SimulateAtBat`은 이미 같은 이유로 `isTopInning`을 미리 잡아두고 있었는데(세션 25) 인터럽트만 실시간 값을 읽었다.

→ 잡아둔 `isTopInning`을 인자로 넘기도록 수정 + 벤치·타순 인덱스 범위 가드 추가
(벤치 없는 팀에 지시가 와도 `LogWarning` 후 건너뜀)

---

## 🐛 세션 38 추가 수정 ③ — `LineUpAutoFill`이 OVR로 선수를 골라 타격 라인업이 무너짐

처음엔 "플레이어 라인업이 AI보다 약하다"를 **CSV 데이터 문제**로 의심했으나, 분포를 뜯어보니 아니었다.

**CSV는 야구적으로 타당하다**

| 스탯 | 50 미만 | 최소 | 패턴 |
|---|---|---|---|
| 타자 power · defense | **0건** | 52 | — |
| 투수 velo · stuff · control | **0건** | 55~65 | — |
| 타자 `run` | 45건 | 35 | 포수 41.6 · 1루수 47.7 vs **중견수 70.9** · 유격수 62.2 |
| 투수 `stamina` | 108건 | 35 | RP 38.5 · CP 40.0 vs **SP 64.6** |

50 미만이 흩어져 있지 않고 **느려야 하는 포지션 · 짧게 던지는 보직에만** 정확히 몰려 있다.
등급별 OVR도 단조 증가(타자 57.0/61.2/66.6, 투수 59.5/64.0/70.6). 데이터는 정상이다.
다만 기획서 1.3의 "세부 스탯 최저 50" 문구와는 충돌하므로 **기획서 쪽을 정정할 것**
(예: "최저 30, 포지션·보직 특성에 따라 예외").

**진짜 원인 = OVR을 선발 기준으로 쓴 것**

`OVR = 4스탯 단순 평균`(기획서 1.3)은 표시용으로는 맞지만 선수 선발 기준으로는 부적합하다.

```
타자 OVR ~ contact   +0.329   ← 타율·삼진을 결정하는 스탯인데 거의 무관
타자 OVR ~ power     +0.758
투수 OVR ~ stamina   +0.919   ← OVR이 사실상 지구력 순위
투수 OVR ~ stuff     +0.856
```

대표 사례: **박해민(LG CF) OVR 70 = 파워68 정확48 주루84 수비80**.
정확 48은 하위권인데 주루·수비 덕에 OVR 전체 4위 → 자동 편성이 최우선으로 뽑는다.

| 선발 기준 | 정확 평균 | 주루 평균 |
|---|---|---|
| OVR 상위 9명 | **66.0** | 71.1 |
| 타격(파워+정확) 상위 9명 | **78.2** | 54.1 |

**수정**: `LineUpAutoFill`의 선발 기준을 OVR → **역할별 편성 점수**로 교체
- 타자: `(파워 + 정확) × 2 + 주루 + 수비` — 타격 2배 가중, 주루·수비도 진루·실책에 쓰이므로 절반 반영
- 투수: `(구위 + 제구) × 2 + 구속`, **선발만 지구력 추가** — 불펜은 지구력이 35~45로 설계돼 OVR이 구조적으로 낮음

**수정 효과 (각 400경기 실측)**

| 지표 | 수정 전 (OVR 기준) | 수정 후 (편성 점수) | AI 로스터 | KBO |
|---|---|---|---|---|
| 타율 | .207 | **.279** | .266 | .270 |
| 출루율 | .252 | **.347** | .331 | .345 |
| 삼진율 | 22.8% | **16.9%** | 17.7% | 17.5% |
| 경기당 득점 | 4.40 | **9.05** | 8.61 | 9.0 |
| 타자 정확 평균 | 52.2 (-0.44) | **63.0 (+0.10)** | 62.1 (+0.06) | — |

플레이어 라인업이 AI 로스터와 대등한 수준으로 올라왔다.

**난이도 확인**: 플레이어(자동 편성) vs AI 400경기 → **208승 187패 5무 (승률 .527)**
144경기 환산 약 **76승**. 2위 해금 커트라인이 대략 78승이므로 **뽑기·육성을 조금 더 해야 넘는 수준**이다.
기획서 7.1(정규시즌 2위 이상 해금)의 난이도 곡선으로는 타당해 보인다.

⚠️ 남은 차이: 파워는 여전히 64.1(-0.14) vs AI 72.0(+0.25). 홈런율 1.74% vs 2.05%.
뽑기가 전 팀·전 포지션 랜덤이고 수비 8자리를 반드시 채워야 하는 제약 때문이라 구조적이다.

📌 **미해결 (세션 32에서 지적, 여전히 남음)**: 투수 `velo` 표준편차 **2.5** (65~80)로 사실상 상수.
투수 변별력이 `stuff`(sd 5.4)에만 의존한다. CSV 개편 시 함께 볼 것.

---

### 세션 39 (2026-08-17) — 씬 매니저 배선 + 실경로 관통 검증

**착수 배경 — 로직은 다 됐는데 시동 배선이 없었다**

세션 38까지의 검증 309건은 전부 **Unity 밖 dotnet 하니스**에서 돌린 것이라,
`SampleScene`에 매니저가 실제로 놓여 있는지는 아무도 확인하지 않았다.
열어보니 **14개 중 5개만** 배치돼 있었다 (GameFlow / Currency / Dismantle / League / AiRoster).
이 상태로 UI를 만들면 첫 버튼에서 죽고, 그게 UI 버그인지 배선 누락인지 구분이 안 된다.

**배선한 매니저 (8개 신규)**

`CardDataManager` · `PlayerDataManager` · `InventoryManager` · `GachaManager` ·
`LineUpManager` · `EnhanceManager` · `TrainManager` · `BreakthroughManager`

- `.unity` YAML을 직접 편집해 배치 (GameObject + Transform + MonoBehaviour + SceneRoots 등록)
- `[SerializeField]` 기본값도 함께 기입 — 인스펙터 표시값과 코드 의도를 일치시킴
- 검증: 앵커 55개 전부 유일 / SceneRoots 16개 전부 해소 / dangling 참조 0
- `LiveGameController`는 경기 씬 전용이라 제외

📝 주요 설계 결정:
- **1매니저 = 1 GameObject 유지** (한 오브젝트에 몰아 붙이지 않음). 매니저들의 싱글톤 중복 처리가
  `Destroy(gameObject)`라 **컴포넌트가 아니라 오브젝트 전체를 지운다.** 한 오브젝트에 8개를 몰면
  `DontDestroyOnLoad` 사본과 새 씬 사본이 겹치는 순간 중복 판정된 하나가 **나머지 7개까지 통째로** 날린다
- Awake 실행 순서 의존성 없음을 사전 확인 — 8개 전부 Awake에서 다른 매니저 `Instance`를 참조하지 않음

**✅ 검증 (하니스 재구축, 실제 매니저 경로 그대로)**

런타임 **86개 전체 컴파일 — 오류 0**

| 관통 단계 | 결과 |
|---|---|
| 매니저 11종 Awake → Instance 생성 | ✅ |
| CSV 마스터 로드 | ✅ 타자 159 + 투수 166 = **325장** |
| `StartNewGame("삼성")` / 잘못된 팀 거부 | ✅ |
| `CompleteTutorial()` → 뽑기권 50 / 중복 차단 | ✅ |
| 10연 × 5회 → 카드 50장 + 뽑기권 0 | ✅ |
| 뽑기권 없을 때 10연 차단 | ✅ |
| 예상 밖 `LogError` | ✅ 0건 |

---

## 🐛 세션 39에서 발견한 규칙 위반 — `GachaManager.Roll10`이 실패 시 `null` 반환

CLAUDE.md 3-2: *"컬렉션을 반환하는 메서드는 실패 시 `null` 대신 **빈 컬렉션** 반환"*

`Roll10`은 **세 갈래 실패 경로 전부** `return null`이다 (`GachaManager.cs:92 / 98 / 106`)
— 인벤토리 공간 부족 / `CurrencyManager` 없음 / 뽑기권 부족.

관통 테스트에서 실제로 `NullReferenceException`으로 죽었다. UI를 붙이면 이렇게 터진다.

```csharp
foreach (GachaResult r in GachaManager.Instance.Roll10(GachaType.Normal))  // ← 뽑기권 0이면 NRE
    ShowCard(r);
```

뽑기권 부족은 **플레이어가 가장 흔하게 만드는 실패**라 반드시 밟는 경로다.
`Roll1`은 단일 객체 반환이라 `null`이 규약상 맞다 — `Roll10`만 해당된다.

⏭️ **수정 예정** (작성자 직접): `return null` 3곳 → `return new List<GachaResult>()`
그 김에 `LineUpAutoFill` 등 다른 컬렉션 반환 메서드도 같은 기준으로 훑어볼 것

---

## 📊 세션 39 실측 — 시작 뽑기권 50개 이슈 재측정 (세션 36 미결 ①)

세션 36 측정(24%)은 **OVR 기준 자동 편성** 시절 값이다.
세션 38에서 `LineUpAutoFill`을 편성 점수 기준으로 고쳤으므로 **1000회 재시행**했다.

| 항목 | 결과 (1000회) |
|---|---|
| 시작 50장으로 라인업 완성 | **371/1000 (37.1%)** |
| 완성에 필요한 카드 수 | 평균 **67.1장** / 중앙값 60 / 최소 50 / 최대 200 |

**지급량별 완성률 (같은 1000회 표본에서 추정)**

| 시작 지급 | 완성률 |
|---|---|
| 50장 | 37.1% |
| 80장 | 83.7% |
| **100장** | **94.7%** |
| 120장 | 97.1% |

- 24% → 37%로 나아졌지만 50장으로는 **신규 유저 63%가 첫 리그에 입장하지 못했다**
- 병목은 **SP 5칸**과 포수 — 관통 로그에서도 `SP 카드가 부족합니다`가 먼저 떴다
- 곡선이 80장 부근에서 완만해짐 → 80~100장이 비용 대비 효율 구간

## ✅ 결론 — 시작 지급 100장으로 확정 (작성자 결정, 에디터 반영 완료)

`SampleScene.unity` → `GameFlowManager._tutorialTicketReward: 100`

**100장 기준 직접 재측정 (추정이 아닌 실측 1000회)**

| 항목 | 결과 |
|---|---|
| 라인업 완성 | **947/1000 (94.7%)** ← 추정치와 정확히 일치 |
| 완성에 필요한 카드 수 | 평균 101.5장 / 중앙값 100 / 최소 100 / 최대 200 |

- 63% 실패 → **5.3% 실패**로 해소. 세션 36 미결 ①은 종결
- ⚠️ **잔여 5.3%(53/1000)는 100장으로도 라인업을 못 채운다.** 이들에겐 추가 뽑기 경로가 필요한데
  현재 신규 유저가 뽑기권을 더 얻을 수단은 리그 보상뿐이고, 리그는 라인업이 있어야 입장 가능 →
  **데드락**. 분해 보상 또는 초기 골드→뽑기권 교환 같은 탈출구가 있어야 한다 (미결)

⚠️ **코드 기본값과 씬 값이 어긋나 있음**: `GameFlowManager.cs:20`은 여전히 `= 50`이고
씬 인스펙터가 100으로 덮고 있다. 지금 동작에는 문제없지만, 새 씬에 매니저를 다시 놓으면 조용히 50으로 돌아간다.
→ 코드 기본값도 100으로 맞출 것

**여전히 미결**: CSV에 시그니쳐 카드가 **0장**이라 기획서 5장의 기본 시그니쳐 지급이 동작하지 않음
(`GameFlowManager.cs:88` LogWarning만 찍고 팀 선택은 성공 처리)

---

### 세션 39 계속 — 테스트 코드 제거 · 티어 테이블 입력 · 밸런스 실측

**프로젝트에서 테스트 코드 완전 제거 (작성자 지시)**
- 씬의 `TestObject`(=`CSVParseTest`) 3블록 + SceneRoots 참조 제거
- `Assets/ScriptsTest/` 폴더째 삭제 (`.cs` + `.meta` + 폴더 `.meta`)
- 제거 사유: `CardDataManager`가 Awake에서 이미 로드하는 CSV를 Start에서 **한 번 더 파싱**했고,
  `hitters[0]`을 인덱스로 바로 써서 CSV가 비면 `IndexOutOfRangeException`으로 즉사했다
- 씬 무결성: 앵커 55→52 · 루트 16→15 · 매니저 14→13 · dangling 0
- 📌 **하니스는 앞으로도 스크래치패드 전용.** 프로젝트에 테스트 파일 0개 유지

**코드 수정 2건 (작성자 요청)**
- `GachaManager.Roll10` — 실패 경로 3곳 `return null` → `return new List<GachaResult>()`
- `GameFlowManager.cs:20` — `_tutorialTicketReward` 기본값 50 → **100** (씬 값과 일치)
- 관통 검증 **10/10 통과** (이전 9/10에서 규칙 위반 1건 해소)

**`LeagueTierTable.asset` 수치 입력 완료 (작성자, 에디터)**

| 티어 | 경기 수 | `_statBonus` | `_goldReward` |
|---|---|---|---|
| `Basic1~3` | 9 / 36 / 36 | 0 | 3000 / 8000 / 8000 |
| `Rookie1~3` | 54 | 2 | 15000 |
| `Amateur1~3` | 81 | 4 | 25000 |
| `Pro1~3` | 108 | 7 | 40000 |
| `Minor1~Legend3` | 144 | 10 | 60000 |

`_rankGoldMultipliers` = 1.0 / 0.8 / 0.6 / 0.5 / 0.4 / 0.35 / 0.3 / 0.25 / 0.2 / 0.15 (코드 기본값 유지)

⚠️ **`Amateur3` 한 칸이 `_statBonus: 0`으로 빠져 있음** (형제인 `Amateur1·2`는 4).
`Amateur3`가 앞 티어보다 쉬워지고 `Pro1`에서 0→7 절벽이 생긴다. → **4로 수정 필요**

---

## 📊 세션 39 실측 — 티어별 밸런스 (`_statBonus` 곡선 검증)

육성 0(강화·훈련 없음) + 자동 편성 플레이어 vs 씨앗 CSV AI 로스터 10팀.

| 티어 | 보정 | 승률 | 144환산 | 2위 해금 |
|---|---|---|---|---|
| `Basic1~3` | +0 | **.592** | 85.3승 | 가능 |
| `Rookie1~3` | +2 | .462 | 66.5승 | 어려움 |
| `Amateur1~3` | +4 | .305 | 44.0승 | 어려움 |
| `Pro1~3` | +7 | .269 | 38.7승 | 어려움 |
| `Minor~Legend` | +10 | **.149** | 21.4승 | 어려움 |

**곡선이 매우 가파르다.** 전 스탯 **+2만으로 .592 → .462** (해금선 아래로 추락).
AI 보정은 타자·투수 **양쪽에 동시 적용**되므로 플레이어가 두 번 손해를 본다.
그래서 스탯 1~2 차이가 승률에 과하게 증폭된다.

**티어별 필요 육성량 (강화 레벨, 목표 승률 .540)**

| 티어 | 보정 | 강화0 | 강화2 | 강화4 | 강화6 | 강화8 | 강화10 | 필요 |
|---|---|---|---|---|---|---|---|---|
| `Basic1~3` | +0 | .592 | .802 | .925 | .981 | .991 | 1.000 | +0 |
| `Rookie1~3` | +2 | .462 | .710 | .849 | .944 | .969 | 1.000 | **+2** |
| `Amateur1~3` | +4 | .335 | .572 | .775 | .914 | .944 | 1.000 | **+2** |
| `Pro1~3` | +7 | .250 | .352 | .679 | .854 | .914 | .994 | **+4** |
| `Minor~Legend` | +10 | .153 | .253 | .468 | .731 | .885 | .938 | **+6** |

📐 **도출한 경험식**: `AI _statBonus ≈ 플레이어 스탯 증가분 + 1.5` 에서 승률 .540이 된다
(강화 레벨 N = 전 스탯 +2N. 위 표 4개 지점에서 모두 ±1 이내로 맞음)

**해석**
- 육성이 티어 난이도를 **완전히 압도한다**. 강화 10이면 전 티어가 .94~1.00으로 뭉개짐
- 반대로 육성이 부족하면 손도 못 댐 (.15~.25). 즉 **"불가능 → 자명" 사이의 놀 수 있는 구간이 매우 좁다**
- 현재 최고 난이도(+10)조차 **강화 6이면 .731**로 여유. 만렙 플레이어를 붙잡을 티어가 없다
- 강화는 최대 +20인데 훈련이 총합 +50(스탯당 평균 +12.5)을 더 얹으므로, 완성 카드는 `_statBonus` 10을 한참 넘어선다

## 🔒 Minor1~Legend3 (144경기 9티어) 밸런스 — **작성자가 직접 조정**

작성자 결정. Claude는 이 구간 수치를 건드리지 않는다.

참고로 남기는 근거:
- 현재 9티어가 `_statBonus` 10 · `_goldReward` 60000으로 **전부 동일** → 8단계가 같은 경험의 반복
- 위 경험식으로 계산하면, 강화 10(+20) 플레이어를 .540에 묶으려면 `_statBonus ≈ 21`이 필요하다.
  훈련까지 만렙이면 더 올라간다
- 즉 이 구간을 10 → 20대 중후반까지 계단식으로 올려야 후반 육성에 의미가 생긴다

---

## ⏭️ 다음 세션 착수 지점

**먼저 처리할 것 (코드 이전 · 데이터/판단)**

| # | 항목 | 현재 상태 |
|---|---|---|
| 1 | `GachaManager.Roll10` null 반환 3곳 | ✅ 완료 |
| 2 | 시작 뽑기권 100장 상향 (씬 + 코드 기본값) | ✅ 완료 (실측 94.7%) |
| 3 | `LeagueTierTable` `_goldReward` · `_statBonus` | ✅ 완료 (작성자 입력) |
| 4 | `Amateur3`의 `_statBonus` 0 → 4 | ✅ 완료 (21칸 전부 검증) |
| 5 | `Minor1~Legend3` 밸런스 | 🔒 작성자 담당 |
| 6 | CSV 시그니쳐 카드 0장 | 데이터 미입력 |
| 7 | 100장으로도 라인업 못 채우는 5.3% 탈출구 | 미결 (데드락) |

## 🎨 로드맵 9번 경기 UI — 화면 구성 확정 (세션 39, 작성자 결정)

### ⏸️ 코드 착수 보류 — **UI 디자인 구성이 끝난 뒤 진행** (작성자 지시)

아래 화면 목록과 설계 판단은 확정됐다. 디자인이 나오면 `GameSession`부터 착수한다.

**확정 화면 4개**

| 화면 | 띄우는 것 | 데이터 출처 (전부 준비 완료) |
|---|---|---|
| 경기 전 | 양 팀 라인업 · 로고 · 선발 투수 · **해당 리그 상대전적** | `SimulationContext` + `TeamRecord` |
| 경기 중 | 이닝/아웃/베이스 · 스코어 · 타석 로그 **자동 흐름** · 일시정지 버튼 | `GameState` + `SimulationBatterLog` |
| 경기 후 | 라인스코어 · R/H/E · 선수별 기록 | `BoxScore` |
| 일시정지 (수동 모드) | 벤치 야수 / 대기 투수 + 교체 버튼 | `LiveGameController` |

**확정 사항**
- **실시간 로그는 타석마다 자동 진행** (탭 넘김 아님) → UI 쪽에 코루틴 타이머가 필요
- **시뮬 진행 중 플레이어 조작은 일시정지뿐** — 그 외 입력 없음
- 박스스코어는 경기 후에만
- 스킵 버튼 없음 (기획서 8.5)

⚠️ **상대전적 표시의 제약**: `TeamRecord`는 `GetWinsAgainst` / `GetLossesAgainst`만 있고
**무승부는 상대별로 쌓지 않는다** (`AddResult`가 무승부 시 `Draws++`만 함).
"3승 2패"는 되지만 "3승 2패 1무"는 못 만든다. 경기 전 화면에 무를 표시하려면 `_drawsAgainst` 추가 필요.
순위 타이브레이커는 무를 안 쓰므로(기획서 7.7) **표시용으로만 필요한지 판단할 것**

### 📌 코드 착수 시 첫 작업 — `GameSession` (설계 완료, 미구현)

`Assets/Scripts/Simulation/GameSession.cs` (순수 C#)

**왜 필요한가**: `SimulateGame()`이 `while (!IsGameOver)`로 경기를 통째로 돌리고,
`GameState`·`logs`가 지역 변수라 밖에서 볼 수 없다. 자동 진행 UI는 타석 단위로 멈출 수 있어야 한다.

**왜 `SimulateAtBat`을 public으로 열기만 하지 않는가**: 그러면 UI가 `GameState`와 `logs`를 직접 들고
루프를 돌려야 해서 진행 책임이 UI로 샌다. 일괄 시뮬과 실시간 관전이 서로 다른 루프를 갖게 되어
언젠가 규칙이 갈라진다. 상태를 가진 쪽이 진행도 책임진다 (SRP).

| 멤버 | 역할 |
|---|---|
| `IsGameOver` / `State` / `Logs` | UI가 읽는 진행 상태 |
| `StepAtBat()` | 타석 1회 전진, 방금 끝난 타석 로그 반환 (끝났으면 null) |
| `BuildResult()` | 종료 후 `GameResult` 반환, 진행 중이면 null |

`GameSimulator` 수정 2건: ① `SimulateAtBat`을 `internal`/`public`로 ② `SimulateGame()`이 `GameSession`을 쓰도록 재작성 (진행 규칙을 한 벌로 유지)

📌 **`StepAtBat` 구현 주의**: `SimulateAtBat`은 로그를 `logs`에 **append만 하고 반환하지 않는다.**
호출 전후 `_logs.Count`를 비교해 이번 타석 몫을 꺼낼 것. `_logs[^1]`을 그냥 읽으면
한 타석이 로그를 2개 남기도록 바뀌었을 때 조용히 틀린다.

---

**남은 로드맵**: 9번 경기 UI(디자인 대기) / 10번 골든글러브 제작 / 11번 튜토리얼

**로드맵 10번 잔여** (재화·분해는 세션 36에서 완료)
- 골든글러브 제작 (`GoldenGloveCraftManager`) — 기획서 4장. 일반 제작 / 팀 선택 제작
- 강화 전용 카드 5장 경로 (`EnhanceManager.ValidateMaterials`의 `materials.Count != 1` 조건 확장, 세션 11 예고분)
- 재화 밸런싱 수치 확정 (기획서 12장 TBD 다수)

**Unity에서 할 것 (코드 아님)**
1. ~~`Amateur3`의 `_statBonus` 0 → 4~~ ✅ 완료
2. `Minor1~Legend3` 밸런스 조정 (🔒 작성자 담당 — 근거는 위 실측 표 참조)

**로드맵 진행 현황**

| 로드맵 | 상태 |
|---|---|
| 1~8번 (데이터 → 리그·포스트시즌) | ✅ 완료 |
| 9번 경기 UI | ⏸️ **화면 구성 확정, 디자인 대기 중** |
| 10번 골든글러브 제작 | ⏭️ 미착수 (재화·분해만 세션 36 완료) |
| 11번 튜토리얼 | ⏭️ 미착수 (최종) |

📌 세션 39 기준, 씬 배선 완료 + 실경로 관통 검증 통과로
**"플레이어 데이터 공급 수단이 없다"는 세션 34의 블로커는 해소됐다**
(신규 시작 → 뽑기 100장 → 자동 편성 → 리그 입장까지 실제로 이어짐).
