using System;
using UnityEngine;
/// <summary>
/// 티어 1개의 리그 설정
/// </summary>

[Serializable]
public struct LeagueTierEntry
{
    public AiRosterSet RosterSet => _rosterSet;
    public int StatBonus => _statBonus;
    public int GameCount => _gameCount;
    public int SeriesLength => _seriesLength;
    public int GoldReward => _goldReward;

    public int GoldenGlovePointReward => _goldenGlovePointReward;
    public int SignatureTicketReward => _signatureTicketReward;

    public int PerGameBreakthroughCardChance => _perGameBreakthroughCardChance;
    public int PerGameEnhanceCardChance => _perGameEnhanceCardChance;

    [SerializeField]
    private AiRosterSet _rosterSet;

    [SerializeField]
    private int _statBonus;

    [Tooltip("정규시즌 경기 수 (기획서 7.2)")]
    [SerializeField]
    private int _gameCount;

    [Tooltip("같은 팀과 연속으로 치르는 경기 수. 프로 리그부터 3 (기획서 7.3)")]
    [SerializeField]
    private int _seriesLength;

    [Tooltip("리그 종료 시 지급 골드의 기준값. 실제 지급액은 여기에 순위 배수를 곱한다 (기획서 7.9)")]
    [SerializeField]
    private int _goldReward;

    [Header("리그 종료 보상 - 부가분 (기획서 7.9). 골드와 같은 순위 배수를 적용한다")]
    [Tooltip("골든글러브 포인트 기준값. 골글 제작의 유일한 대량 획득 경로 (기획서 4장 · 9.1)")]
    [SerializeField]
    private int _goldenGlovePointReward;

    [Tooltip("시그니쳐 뽑기권 기준값 (기획서 9.1)")]
    [SerializeField]
    private int _signatureTicketReward;

    [Header("경기당 보상 (기획서 9.1 - 훈련돌파·강화 전용 카드의 획득 경로)")]
    [Tooltip("경기 1건마다 훈련돌파 카드가 나올 확률 (%). 0이면 나오지 않음")]
    [Range(0, 100)]
    [SerializeField]
    private int _perGameBreakthroughCardChance;

    [Tooltip("경기 1건마다 강화 전용 카드가 나올 확률 (%). 등급은 별도 가중치로 정해진다")]
    [Range(0, 100)]
    [SerializeField]
    private int _perGameEnhanceCardChance;
}
