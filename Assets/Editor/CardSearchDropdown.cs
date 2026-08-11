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

        // 비우기 항목 — 맨 위에 오도록 팀 노드보다 먼저 추가
        root.AddChild(new AdvancedDropdownItem("(비어 있음)") { id = 0 });

        // 팀명 → 팀 폴더 노드. 같은 팀 카드를 한 폴더로 모으기 위한 임시 색인
        Dictionary<string, AdvancedDropdownItem> teamNodes =
            new Dictionary<string, AdvancedDropdownItem>();

        foreach (CardEntry entry in _entries)
        {
            // 해당 팀 폴더가 아직 없으면 만들어서 root에 붙인다
            if (!teamNodes.TryGetValue(entry.TeamName, out AdvancedDropdownItem teamNode))
            {
                teamNode = new AdvancedDropdownItem(entry.TeamName);
                root.AddChild(teamNode);
                teamNodes.Add(entry.TeamName, teamNode);
            }

            // 카드 항목. id에 cardId를 실어 보낸다
            AdvancedDropdownItem leaf = new AdvancedDropdownItem(entry.Label);
            leaf.id = entry.CardId;
            teamNode.AddChild(leaf);
        }

        return root;
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        _onSelected?.Invoke(item.id);
    }
}
