/// <summary>
/// 카드 마스터 풀에서 AI 팀 로스터 편성
/// </summary>
public static class AiRosterBuilder
{
    //마스터 데이터 + 티어 보정 = AI 타자 스냅샷
    private static HitterSnapshot ToHitterSnapshot(HitterMasterData hitterData, int tierStatBonus)
    {
        return new HitterSnapshot(hitterData.CardId, hitterData.Name, hitterData.Power + tierStatBonus, hitterData.Contact + tierStatBonus, hitterData.Run + tierStatBonus, hitterData.Defense + tierStatBonus);
    }

    //마스터 데이터 + 티어 보정 = AI 투수 스냅샷
    private static PitcherSnapshot ToPitcherSnapshot(PitcherMasterData pitcherData, int tierStatBonus)
    {
        return new PitcherSnapshot(pitcherData.CardId, pitcherData.Name, pitcherData.Velocity + tierStatBonus, pitcherData.Stuff + tierStatBonus, pitcherData.Control + tierStatBonus, pitcherData.Stamina + tierStatBonus);
    }
}
