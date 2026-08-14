using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

/// <summary>
/// [CardId] 특성이 붙은 int 필드를 카드 검색 드롭다운으로 그린다.
/// </summary>
[CustomPropertyDrawer(typeof(CardIdAttribute))]
public class CardIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property,
GUIContent label)
    {
        //int 외의 타입에 붙은 경우 방어
        if (property.propertyType != SerializedPropertyType.Integer)
        {
            EditorGUI.LabelField(position, label.text, "[CardId]는 int 필드에만 사용 가능");
            return;
        }

        label = EditorGUI.BeginProperty(position, label, property);

        Rect fieldRect = EditorGUI.PrefixLabel(position, label);

        if (GUI.Button(fieldRect, GetButtonText(property.intValue), EditorStyles.popup))
        {
            CardIdAttribute cardIdAttribute = (CardIdAttribute)attribute;

            //콜백은 몇 프레임 뒤에 실행된다.
            //SerializedProperty는 그때 이미 무효이므로 경로만 붙잡아 둔다.
            SerializedObject owner = property.serializedObject;
            string propertyPath = property.propertyPath;

            CardSearchDropdown dropdown = new CardSearchDropdown(
                new AdvancedDropdownState(),
                BuildEntries(cardIdAttribute, property),
                selectedCardId =>
                {
                    SerializedProperty target = owner.FindProperty(propertyPath);
                    if (target == null)
                        return;

                    target.intValue = selectedCardId;
                    owner.ApplyModifiedProperties();
                });

            dropdown.Show(fieldRect);
        }

        EditorGUI.EndProperty();
    }

    //타자/투수 필터 + 수비 포지션 조건까지 적용한 최종 목록
    private IReadOnlyList<CardEntry> BuildEntries(CardIdAttribute cardIdAttribute, SerializedProperty property)
    {
        IReadOnlyList<CardEntry> source = GetEntries(cardIdAttribute.Filter);

        //① 필드 자체에 포지션이 고정된 경우 (투수 배열 - SP/RP/CP)
        if (!string.IsNullOrEmpty(cardIdAttribute.FixedPosition))
            return FilterByPositionText(source, cardIdAttribute.FixedPosition);

        //② 슬롯마다 포지션이 달라지는 경우 (타자 라인업)
        //   포지션 필드를 못 찾으면 필터 없이 전체 노출.
        //   아무것도 못 고르는 것보다 관대하게 동작하는 편이 낫다.
        if (!TryGetSlotPosition(property, cardIdAttribute.PositionFieldName, out HitterPosition slotPosition))
            return source;

        //DH는 모든 포지션 카드가 들어갈 수 있는 와일드카드
        if (slotPosition == HitterPosition.DH)
            return source;

        List<CardEntry> filtered = new List<CardEntry>();
        foreach (CardEntry entry in source)
        {
            if (HitterPositionParser.TryParse(entry.Position, out HitterPosition cardPosition)
                && cardPosition == slotPosition)
            {
                filtered.Add(entry);
            }
        }
        return filtered;
    }

    //CSV 표기 문자열이 일치하는 카드만 추린다
    private IReadOnlyList<CardEntry> FilterByPositionText(IReadOnlyList<CardEntry> source, string position)
    {
        List<CardEntry> filtered = new List<CardEntry>();
        foreach (CardEntry entry in source)
        {
            if (entry.Position == position)
            {
                filtered.Add(entry);
            }
        }
        return filtered;
    }

    //같은 구조체 안에 있는 포지션 필드를 경로로 찾아 읽는다
    private bool TryGetSlotPosition(SerializedProperty property, string fieldName, out HitterPosition position)
    {
        position = default;

        if (string.IsNullOrEmpty(fieldName))
            return false;

        //"_lineup.Array.data[0]._cardId" -> "_lineup.Array.data[0]._position"
        int lastDot = property.propertyPath.LastIndexOf('.');
        if (lastDot < 0)
            return false;

        string siblingPath = property.propertyPath.Substring(0, lastDot + 1) + fieldName;
        SerializedProperty sibling = property.serializedObject.FindProperty(siblingPath);
        if (sibling == null || sibling.propertyType != SerializedPropertyType.Enum)
            return false;

        position = (HitterPosition)sibling.enumValueIndex;
        return true;
    }

    //특성의 필터에 맞는 카드 목록 반환
    private IReadOnlyList<CardEntry> GetEntries(PlayerTypeFilter filter)
    {
        switch (filter)
        {
            case PlayerTypeFilter.HitterOnly:
                return CardCatalog.Hitters;
            case PlayerTypeFilter.PitcherOnly:
                return CardCatalog.Pitchers;
            default:
                IReadOnlyList<CardEntry> hitters = CardCatalog.Hitters;
                IReadOnlyList<CardEntry> pitchers = CardCatalog.Pitchers;

                List<CardEntry> all = new List<CardEntry>(hitters.Count + pitchers.Count);
                all.AddRange(hitters);
                all.AddRange(pitchers);
                return all;
        }
    }

    //현재 cardId를 사람이 읽을 수 있는 버튼 문구로
    private string GetButtonText(int cardId)
    {
        if (cardId == 0)
            return "(비어 있음)";

        if (CardCatalog.TryGet(cardId, out CardEntry entry))
            return entry.Label;

        return $"[!] 알 수 없는 cardId: {cardId}";
    }
}
