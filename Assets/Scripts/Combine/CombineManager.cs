using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 조합 - 같은 종류 카드 3장을 소모해 랜덤 1장을 얻는다
/// </summary>
public class CombineManager : SingletonBehaviour<CombineManager>
{
    [Header("노말 승급 확률 (시그·골글은 5성 고정이라 승급 없음)")]
    [SerializeField, Range(0f, 1f), Tooltip("기준 등급이 3성일 때 4성으로 오를 확률")]
    private float _upgradeChanceStar3 = 0.25f;
    [SerializeField, Range(0f, 1f), Tooltip("기준 등급이 4성일 때 5성으로 오를 확률. 3성에서 4성으로 오른 뒤에도 한 번 더 적용된다")]
    private float _upgradeChanceStar4 = 0.1f;

    [SerializeField, Tooltip("조합에 필요한 재료 카드 수")]
    private int _materialCount = 3;

    //조합 때마다 새 List를 만들지 않도록 재사용하는 후보 버퍼
    private readonly List<int> _candidateBuffer = new List<int>(128);

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

        int pickedIndex = Random.Range(0, materialInstanceIds.Count);
        CardInstance pickedCard = InventoryManager.Instance.GetCard(materialInstanceIds[pickedIndex]);
        CardMasterData pickedMaster = CardDataManager.Instance.GetCardMasterData(pickedCard.CardId);

        CardType cardType = pickedMaster.CardType;
        CardGrade baseGrade = pickedMaster.CardGrade;
        CardGrade resultGrade = DecideResultGrade(cardType, baseGrade);

        CollectCandidates(cardType, resultGrade);

        if (_candidateBuffer.Count == 0)
        {
            Debug.LogWarning($"[CombineManager] : 조합 결과로 줄 카드가 없습니다 ({cardType} {resultGrade} 마스터 데이터 미입력)");
            return null;
        }

        int resultCardId = _candidateBuffer[Random.Range(0, _candidateBuffer.Count)];

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
        if (cardType != CardType.Normal)
            return baseGrade;

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
