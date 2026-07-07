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

## ⏭️ 다음 할 일

1. 뽑기 시스템 테스트 (`GachaTest.cs` 작성 → Unity 플레이 모드 확인)
   - Roll1 / Roll10 정상 동작 확인
   - 10연 천장 보장 확인
   - 인벤토리 한도 초과 차단 확인
2. 50연 천장 — 자팀 확정 카드 교체 로직 구현
