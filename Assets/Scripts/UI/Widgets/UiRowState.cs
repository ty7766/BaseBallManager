/// <summary>
/// 목록 행의 시각 상태 (명세 6-4)
/// </summary>
public enum UiRowState
{
    Normal,     //기본
    Selected,   //선택됨 (흰 테두리)
    Used,       //사용됨 · 강판 (흐림 + 태그)
    Pitching,   //등판 중 (청록 테두리)
    Empty       //빈 슬롯 (점선)
}
