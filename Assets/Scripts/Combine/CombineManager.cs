using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 조합 - 같은 종류 카드 3장을 소모해 랜덤 1장을 얻는다
/// </summary>
/// <remarks>
/// 재료의 등급은 섞여도 되고, 결과 등급은 <b>재료 3장 중 무작위 1장의 등급</b>을 기준으로 승급을 굴려 정한다.
/// 그래서 결과는 재료 최저 등급 아래로 내려가지 않고, 상위 등급 재료를 많이 넣을수록 상위가 나올 확률이 오른다.
/// 시그니쳐·골든글러브는 5성 고정(기획서 1.2)이라 승급이 없고, 같은 종류 안에서 다른 카드를 뽑는 리롤이 된다.
/// </remarks>
public class CombineManager : MonoBehaviour
{
    public static CombineManager Instance { get; private set; }

    [Header("노말 승급 확률 (시그·골글은 5성 고정이라 승급 없음)")]
    [SerializeField, Range(0f, 1f), Tooltip("기준 등급이 3성일 때 4성으로 오를 확률")]
    private float _upgradeChanceStar3 = 0.25f;
    [SerializeField, Range(0f, 1f), Tooltip("기준 등급이 4성일 때 5성으로 오를 확률. 3성에서 4성으로 오른 뒤에도 한 번 더 적용된다")]
    private float _upgradeChanceStar4 = 0.1f;

    [SerializeField, Tooltip("조합에 필요한 재료 카드 수")]
    private int _materialCount = 3;

    //조합 때마다 새 List를 만들지 않도록 재사용하는 후보 버퍼
    private readonly List<int> _candidateBuffer = new List<int>(128);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 조합에 필요한 재료 카드 수 (UI 슬롯 구성용)
    /// </summary>
    public int GetMaterialCount()
    {
        return _materialCount;
    }

    /// <summary>
    /// 재료로 조합할 수 있는지 검사한다 (개수 · 중복 · 존재 · 잠금 · 라인업 · 종류 일치)
    /// </summary>
    public bool CanCombine(IReadOnlyList<int> materialInstanceIds)
    {
        if (materialInstanceIds == null || materialInstanceIds.Count != _materialCount)
        {
            Debug.LogWarning($"[CombineManager] : 조합에는 카드 {_materialCount}장이 필요합니다");
            return false;
        }

        //같은 카드를 여러 슬롯에 넣었는지 먼저 본다.
        //이 검사를 뒤로 미루면 1장이 존재·잠금·라인업 검사를 모두 통과한 뒤
        //Combine에서 RemoveCard가 세 번 호출돼 1장으로 3장짜리 조합이 성립한다 (카드 복제)
        for (int i = 0; i < materialInstanceIds.Count; i++)
        {
            for (int j = i + 1; j < materialInstanceIds.Count; j++)
            {
                if (materialInstanceIds[i] == materialInstanceIds[j])
                {
                    Debug.LogWarning($"[CombineManager] : 같은 카드를 재료로 중복 지정했습니다 (instanceId {materialInstanceIds[i]})");
                    return false;
                }
            }
        }

        //첫 카드의 종류를 기준으로 잡고 나머지와 비교한다
        CardType baseCardType = CardType.None;

        for (int i = 0; i < materialInstanceIds.Count; i++)
        {
            int instanceId = materialInstanceIds[i];
            CardInstance card = InventoryManager.Instance.GetCard(instanceId);

            if (card == null)
            {
                Debug.LogWarning($"[CombineManager] : 재료 카드가 인벤토리에 없습니다 (instanceId {instanceId})");
                return false;
            }

            //실수 방지용 잠금 (분해와 같은 규칙 - 기획서 9.2)
            if (card.IsLocked)
            {
                Debug.LogWarning($"[CombineManager] : 잠금된 카드는 조합할 수 없습니다 (instanceId {instanceId})");
                return false;
            }

            if (LineUpManager.Instance.IsCardAssigned(instanceId))
            {
                Debug.LogWarning($"[CombineManager] : 라인업에 편성된 카드는 조합할 수 없습니다 (instanceId {instanceId})");
                return false;
            }

            CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(card.CardId);

            //인벤에는 있는데 마스터에 없다 - 유저 실수가 아니라 CSV와 세이브가 어긋난 데이터 불일치
            if (masterData == null)
            {
                Debug.LogError($"[CombineManager] : 마스터 데이터를 찾지 못했습니다 (cardId {card.CardId})");
                return false;
            }

            if (i == 0)
            {
                baseCardType = masterData.CardType;
                continue;
            }

            //노말은 노말끼리, 시그는 시그끼리, 골글은 골글끼리만
            if (masterData.CardType != baseCardType)
            {
                Debug.LogWarning($"[CombineManager] : 같은 종류의 카드끼리만 조합할 수 있습니다 ({baseCardType} / {masterData.CardType})");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 조합 실행. 재료를 소멸시키고 새 카드 1장을 지급한다. 실패 시 null
    /// </summary>
    public CombineResult Combine(IReadOnlyList<int> materialInstanceIds)
    {
        if (!CanCombine(materialInstanceIds))
            return null;

        //기준 등급 = 재료 3장 중 무작위 1장의 등급.
        //3장의 등급 분포가 그대로 확률이 되므로 "5성을 많이 넣을수록 5성이 잘 나온다"가 별도 가중치 없이 성립하고,
        //재료에 없는 하위 등급은 애초에 뽑히지 않는다 (4·5·5를 넣으면 3성이 나올 수 없음)
        int pickedIndex = Random.Range(0, materialInstanceIds.Count);
        CardInstance pickedCard = InventoryManager.Instance.GetCard(materialInstanceIds[pickedIndex]);
        CardMasterData pickedMaster = CardDataManager.Instance.GetCardMasterData(pickedCard.CardId);

        //CanCombine이 3장 모두 같은 종류임을 보장하므로 기준 카드의 종류를 그대로 쓴다
        CardType cardType = pickedMaster.CardType;
        CardGrade baseGrade = pickedMaster.CardGrade;
        CardGrade resultGrade = DecideResultGrade(cardType, baseGrade);

        //후보 풀을 재료 소멸보다 먼저 확인한다.
        //순서를 바꾸면 해당 종류·등급의 마스터 데이터가 없을 때 재료만 먹고 카드를 못 준다
        CollectCandidates(cardType, resultGrade);

        if (_candidateBuffer.Count == 0)
        {
            Debug.LogWarning($"[CombineManager] : 조합 결과로 줄 카드가 없습니다 ({cardType} {resultGrade} 마스터 데이터 미입력)");
            return null;
        }

        //중복 카드가 나올 수 있다 - 재료로 넣은 카드와 같은 cardId도 후보에 남긴다
        int resultCardId = _candidateBuffer[Random.Range(0, _candidateBuffer.Count)];

        //재료 소멸 -> 지급 순서.
        //3장이 빠진 뒤에 1장을 넣으므로 보유 한도(200장)에 걸릴 일이 없다
        for (int i = 0; i < materialInstanceIds.Count; i++)
        {
            if (!InventoryManager.Instance.RemoveCard(materialInstanceIds[i]))
            {
                Debug.LogError($"[CombineManager] : 재료 소멸에 실패했습니다 (instanceId {materialInstanceIds[i]}). 재료 {i}장이 이미 사라진 상태입니다");
                return null;
            }
        }

        int resultInstanceId = InventoryManager.Instance.AddCard(resultCardId);

        if (resultInstanceId == -1)
        {
            Debug.LogError($"[CombineManager] : 결과 카드 지급에 실패했습니다 (cardId {resultCardId}). 재료 {_materialCount}장이 소멸했습니다");
            return null;
        }

        return new CombineResult(resultInstanceId, resultCardId, cardType, baseGrade, resultGrade);
    }

    //결과 등급 결정. 노말만 승급하고, 시그·골글은 5성 고정이라 기준 등급을 그대로 돌려준다
    private CardGrade DecideResultGrade(CardType cardType, CardGrade baseGrade)
    {
        //시그니쳐·골든글러브는 전부 5성이라 올릴 자리가 없다 (기획서 1.2)
        if (cardType != CardType.Normal)
            return baseGrade;

        //3성이 4성으로 오르면 거기서 한 번 더 굴린다.
        //이 연쇄 덕분에 3성 3장으로도 5성이 나올 수 있고, 그 확률은 두 확률의 곱이라 자연히 희박해진다
        if (baseGrade == CardGrade.Star3)
        {
            if (Random.value >= _upgradeChanceStar3)
                return CardGrade.Star3;

            return Random.value < _upgradeChanceStar4 ? CardGrade.Star5 : CardGrade.Star4;
        }

        if (baseGrade == CardGrade.Star4)
            return Random.value < _upgradeChanceStar4 ? CardGrade.Star5 : CardGrade.Star4;

        return CardGrade.Star5;
    }

    //결과 종류·등급에 맞는 카드 풀을 _candidateBuffer에 채운다
    private void CollectCandidates(CardType cardType, CardGrade grade)
    {
        _candidateBuffer.Clear();

        //타자·투수를 가리지 않고 모두 후보에 넣는다 (뽑기와 같은 규칙)
        foreach (HitterMasterData hitter in CardDataManager.Instance.GetAllHitters())
        {
            if (hitter.CardType == cardType && hitter.CardGrade == grade)
                _candidateBuffer.Add(hitter.CardId);
        }

        foreach (PitcherMasterData pitcher in CardDataManager.Instance.GetAllPitchers())
        {
            if (pitcher.CardType == cardType && pitcher.CardGrade == grade)
                _candidateBuffer.Add(pitcher.CardId);
        }
    }
}
