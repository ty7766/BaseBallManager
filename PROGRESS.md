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

## ⏭️ 다음 할 일

1. BatterCards.csv / PitcherCards.csv 파일 생성 및 데이터 입력 (개발자 직접)
2. **C# 카드 데이터 모델 설계**
   - `CardMasterData` (CSV 읽기 전용 원본)
   - `CardInstance` (보유 카드 — 강화/훈련 상태 포함)
3. CSV 로더 구현
4. OVR 계산 로직
