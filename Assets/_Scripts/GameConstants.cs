namespace Game.Core
{
    /// <summary>
    /// Projedeki tüm "sihirli string"leri (magic string) tek çatı altında toplar.
    /// Eski <c>CardConstants</c> ve <c>GameKeys</c> dosyalarının yerini alır.
    ///
    /// Not: Kart kategorileri ve requirement key'leri JSON ile BİREBİR aynıdır; designer
    /// JSON'a yeni kategori ekleyebilsin diye bilinçli olarak string sabit tutulmuştur (enum değil).
    /// </summary>
    public static class GameConstants
    {
        /// <summary>Lokalizasyon anahtarı ekleri ve önekleri.</summary>
        public static class Loc
        {
            public const string Prefix = "LOK_";
            public const string SuffixDesc = "_DESC";
            public const string SuffixRight = "_R";
            public const string SuffixLeft = "_L";
            public const string PhaseIndicator = "_PHS";
        }

        /// <summary>Kart kategorileri (cards.json "category" alanı ile aynı).</summary>
        public static class Categories
        {
            public const string SeasonStart = "season_start";
            public const string SeasonEnd = "season_end";
            public const string MatchFiller = "match_filler";
            public const string MatchTransition = "match_transition";
            public const string ChainStarter = "chain_starter";
            public const string DailyFiller = "daily_filler";
            public const string UclGroup = "ucl_group";
            public const string UclSystem = "ucl_system";
        }

        /// <summary>Requirement (koşul) tipleri (RequirementData "type" alanı ile aynı).</summary>
        public static class ReqTypes
        {
            public const string Flag = "Flag";
            public const string Int = "Int";
            public const string Relation = "Relation";
        }

        /// <summary>Stat / kaynak isimleri (requirement key'leri ve crisis "targetStat" ile aynı).</summary>
        public static class Stats
        {
            public const string PhaseIndex = "Phase_Index";
            public const string Form = "Form";
            public const string Money = "Money";
            public const string Fame = "Fame";
            public const string Relations = "Relations";
            public const string Focus = "Focus";
        }

        /// <summary>Kriz koşulu yönü (crisis.json "condition" alanı ile aynı).</summary>
        public static class CrisisConditions
        {
            public const string Min = "Min"; // Stat 0'a düştü
            public const string Max = "Max"; // Stat 100'e ulaştı
        }

        /// <summary>StoryManager bayrakları (Flags).</summary>
        public static class Flags
        {
            public const string UclQualified = "next_season_ucl";
            public const string UclEliminationShown = "ucl_elimination_shown";
            public const string PlayerNation = "Player_Nation";
        }

        /// <summary>StoryManager sayısal değişkenleri (Variables).</summary>
        public static class Vars
        {
            public const string UclProgress = "ucl_progress";
            public const string UclMomentum = "ucl_momentum";
            public const string TeamTierMod = "team_tier_modifier";
            public const string TeamPowerMod = "team_power_modifier";
        }

        /// <summary>StoryManager metin değişkenleri (Strings).</summary>
        public static class StoryStrings
        {
            public const string PlayerNationName = "Player_Nation_Name";
            public const string TeamName = "Team_Name";
        }

        /// <summary>Sistem (yapay) kart ID'leri — SystemCardFactory üretir, çeşitli manager'lar yakalar.</summary>
        public static class SystemCardIds
        {
            public const string Crisis = "CRISIS_CARD";
            public const string GameOver = "GAMEOVER_CARD";
            public const string UclFinalEndNode = "ucl_final_end_node";
            public const string UclEliminated = "ucl_eliminated";
        }
    }
}
