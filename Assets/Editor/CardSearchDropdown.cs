using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
/// <summary>
/// 카드 목록을 팀별로 묶어 보여주는 검색 드롭다운
/// </summary>
public class CardSearchDropdown : AdvancedDropdown
{
    private readonly IReadOnlyList<CardEntry> _entries;
    private readonly Action<int> _onSelected;

    public CardSearchDropdown(AdvancedDropdownState state, IReadOnlyList<CardEntry> entries, Action<int> onSelected) : base(state)
    {
        _entries = entries;
        _onSelected = onSelected;
        minimumSize = new Vector2(320f, 400f);
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        AdvancedDropdownItem root = new AdvancedDropdownItem("카드 선택");

        root.AddChild(new CardDropdownItem("(비어 있음)", 0));

        Dictionary<string, AdvancedDropdownItem> teamNodes =
            new Dictionary<string, AdvancedDropdownItem>();

        foreach (CardEntry entry in _entries)
        {
            if (!teamNodes.TryGetValue(entry.TeamName, out AdvancedDropdownItem teamNode))
            {
                teamNode = new AdvancedDropdownItem(entry.TeamName);
                root.AddChild(teamNode);
                teamNodes.Add(entry.TeamName, teamNode);
            }

            teamNode.AddChild(new CardDropdownItem(entry.Label, entry.CardId));
        }

        return root;
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        if (item is CardDropdownItem cardItem)
        {
            _onSelected?.Invoke(cardItem.CardId);
        }
    }

    /// <summary>
    /// cardId를 실어 나르는 드롭다운 항목.
    /// AdvancedDropdownItem.id는 Unity가 선택 상태 추적에 쓰는 내부 필드라
    /// 우리가 넣은 값이 그대로 돌아온다는 보장이 없다. 별도 필드로 보관한다.
    /// </summary>
    private class CardDropdownItem : AdvancedDropdownItem
    {
        public int CardId { get; }

        public CardDropdownItem(string name, int cardId) : base(name)
        {
            CardId = cardId;
        }
    }
}
