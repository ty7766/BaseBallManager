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
                GetEntries(cardIdAttribute.Filter),
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