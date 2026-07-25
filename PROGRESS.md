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

## 🔧 진행 중

로드맵 6번 4단계: `simulation-pitcher` 브랜치 — 투수 체력·교체 (기획서 8.4)

**완성된 파일**
- `Assets/Scripts/ProbabilityModels/PitchCountCalculator.cs` — 타석당 투구 수 계산

**완성된 메서드 (PitchCountCalculator)**
- `Calculate(outcome, hitter, pitcher)` — CalcBasePitches + CalcFouls 합산 반환
- `CalcBasePitches(outcome)` — 삼진→3, 볼넷→4, 인플레이→가중 랜덤 {1:15%/2:25%/3:30%/4:20%/5:10%}
- `CalcFouls(hitter, pitcher)` — foulChance 기반 while 루프, 상한 18개

📝 주요 설계 결정:
- 파울 상한: 기획서 12개 → **18개로 변경** (튜닝 결정)

## ⏭️ 다음 할 일

`simulation-pitcher` 브랜치 계속:
- `PitcherState.GetFatiguedSnapshot()` — 피로 패널티 적용 스냅샷 반환 (퀘스트 안내 완료, 구현 전)
- `PitcherChangeEvaluator.cs` — 교체 판단 + 불펜 운영
