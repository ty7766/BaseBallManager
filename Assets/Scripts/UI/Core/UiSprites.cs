using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources/UI 아래 납품 스프라이트를 이름으로 캐시 로드한다.
/// 없는 이름은 null을 돌려주고 호출부는 단색 사각형으로 떨어진다
/// </summary>
public static class UiSprites
{
    public const string PanelLarge = "Match/panel_large";
    public const string PanelSmall = "Match/panel_small";
    public const string RowNormal = "Match/row_normal";
    public const string RowSelected = "Match/row_selected";
    public const string RowUsed = "Match/row_used";
    public const string RowEmpty = "Match/row_empty";
    public const string RowPitching = "Match/row_pitching";
    public const string ButtonNormal = "Match/button_normal";
    public const string ButtonPrimary = "Match/button_primary";
    public const string ButtonDisabled = "Match/button_disabled";
    public const string BaseDiamondEmpty = "Match/base_diamond_empty";
    public const string BaseDiamondOccupied = "Match/base_diamond_occupied";
    public const string BaseHome = "Match/base_home";
    public const string OutDotEmpty = "Match/out_dot_empty";
    public const string OutDotFilled = "Match/out_dot_filled";
    public const string StaminaFrame = "Match/stamina_bar_frame";
    public const string StaminaFill = "Match/stamina_bar_fill";
    public const string StaminaTick = "Match/stamina_bar_tick";
    public const string LogInningChip = "Match/log_inning_chip";
    public const string LogInningHeader = "Match/log_inning_header";
    public const string LogRowOffense = "Match/log_row_offense";
    public const string LogRowScore = "Match/log_row_score";
    public const string LogRowHomeRun = "Match/log_row_homerun";
    public const string BadgeScore = "Match/badge_score";
    public const string TagOutline = "Match/tag_outline";
    public const string TabOn = "Match/tab_on";
    public const string IconPause = "Match/icon_pause";
    public const string IconResume = "Match/icon_resume";
    public const string IconSwap = "Match/icon_swap";
    public const string IconInning = "Match/icon_inning";
    public const string IconBack = "Match/icon_back";
    public const string IconStar = "Card/icon_star";
    public const string CardStarBack = "Card/card_star_back";
    public const string CardStarFill = "Card/card_star_fill";
    public const string CardSlotEmpty = "Card/card_slot_empty";
    public const string IconLock = "Card/icon_lock";
    public const string Spinner = "Common/spinner_00";

    private const string ResourceRoot = "UI/";

    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(32);
    private static readonly HashSet<string> Warned = new HashSet<string>();

    /// <summary>
    /// 스프라이트 1장. 경로는 Resources/UI 기준 상대 경로다 ("Match/panel_large")
    /// </summary>
    public static Sprite Get(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return null;

        if (Cache.TryGetValue(relativePath, out Sprite cached))
            return cached;

        Sprite loaded = Resources.Load<Sprite>(ResourceRoot + relativePath);

        if (loaded == null && Warned.Add(relativePath))
            Debug.LogWarning($"[UiSprites]: '{ResourceRoot}{relativePath}' 스프라이트를 찾지 못했습니다");

        Cache[relativePath] = loaded;

        return loaded;
    }
}
