/// <summary>
/// Projedeki "Sihirli String" (Magic String) hatalarýný önlemek için merkezi sabitler (Constants).
/// </summary>
public static class CardConstants
{
    // Dil (Localization) Sabitleri
    public const string SuffixDesc = "_DESC";
    public const string SuffixRight = "_R";
    public const string SuffixLeft = "_L";
    public const string PhaseIndicator = "_PHS";

    // Kategori Sabitleri
    public const string CategorySeasonStart = "season_start";
    public const string CategorySeasonEnd = "season_end";
    public const string CategoryMatchFiller = "match_filler";
    public const string CategoryMatchTransition = "match_transition";
    public const string CategoryChainStarter = "chain_starter";
    public const string CategoryDailyFiller = "daily_filler";

    // Koþul (Requirement) Tipleri
    public const string ReqTypeFlag = "Flag";
    public const string ReqTypeInt = "Int";
    public const string ReqTypeRelation = "Relation";

    // Stat (Kaynak) Ýsimleri
    public const string StatPhaseIndex = "Phase_Index";
    public const string StatForm = "Form";
    public const string StatMoney = "Money";
    public const string StatFame = "Fame";
    public const string StatRelations = "Relations";
    public const string StatFocus = "Focus";
}