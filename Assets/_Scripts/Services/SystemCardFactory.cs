using System.Collections.Generic;
using Game.Core;
using Game.Data;

namespace Game.Services
{
    /// <summary>
    /// JSON'dan değil, kod içinde elle kurulan (synthetic) tüm <see cref="CardData"/>
    /// örneklerini tek yerden üretir.
    ///
    /// Eskiden bu üretim <c>CardManager</c> (kriz/gameover), <c>SeasonManager</c>
    /// (UCL eleme/grup kartları) ve <c>UCLManager</c> (final bitiş düğümü) olmak üzere
    /// 3 ayrı manager'a dağılmıştı (bkz. REFACTOR_PLAN.md madde 7-H). Bu sınıf yalnızca
    /// kart ÜRETİMİNİ merkezîleştirir; zincir kurma, rastgele seçim, UI tema geçişleri ve
    /// kriz-çıkışının hangi manager'da işleneceği bu sınıfın sorumluluğu dışındadır
    /// (Katman 3'te ilgili manager'lar bu fabrikaya bağlanacak).
    ///
    /// Durumsuz (static) ve saf — hiçbir Unity/manager bağımlılığı yoktur.
    /// </summary>
    public static class SystemCardFactory
    {
        /// <summary>Kriz kartı üretir. Oyuncunun bedeli ödemekten başka seçeneği yoktur.</summary>
        public static CardData CreateCrisisCard(CrisisData crisis)
        {
            return new CardData
            {
                id = GameConstants.SystemCardIds.Crisis,
                scenarioText = $"<color=#FF5555>KRİZ: {crisis.title}</color>\n\n{crisis.description}",
                characterImageName = crisis.iconPath,
                right = new ChoiceData { choiceText = "BEDELİ ÖDE" },
                left = new ChoiceData { choiceText = "BEDELİ ÖDE" }
            };
        }

        /// <summary>Oyun sonu (ending) kartı üretir.</summary>
        public static CardData CreateGameOverCard(EndingData ending)
        {
            return new CardData
            {
                id = GameConstants.SystemCardIds.GameOver,
                scenarioText = $"<color=#888888>SON:</color> {ending.title}\n\n{ending.description}",
                characterImageName = ending.iconPath,
                right = new ChoiceData { choiceText = ending.buttonText },
                left = new ChoiceData { choiceText = ending.buttonText }
            };
        }

        /// <summary>UCL grup elemesinde başarısız olunduğunda gösterilen kart.</summary>
        public static CardData CreateUclEliminatedCard()
        {
            return new CardData
            {
                id = GameConstants.SystemCardIds.UclEliminated,
                category = GameConstants.Categories.UclGroup,
                scenarioText = "LOK_UCL_ELIMINATED",
                characterImageName = "ucl_atmosphere",
                right = new ChoiceData { choiceText = "LOK_CONTINUE" },
                left = new ChoiceData { choiceText = "LOK_CONTINUE" }
            };
        }

        /// <summary>
        /// UCL grup eleme maçlarından biri. <paramref name="index"/> kart ID'sine ve
        /// gösterilen metne yansır (1'den başlar).
        /// </summary>
        public static CardData CreateUclGroupCard(int index)
        {
            return new CardData
            {
                id = "dyn_ucl_group_" + index,
                category = GameConstants.Categories.UclGroup,
                scenarioText = $"Şampiyonlar Ligi eleme maçındasın. Grup {index}/2.",
                characterImageName = "ucl_atmosphere",
                right = new ChoiceData
                {
                    choiceText = "Takım İçin Savaş",
                    relations = 10,
                    focus = -5,
                    form = -10,
                    hiddenEffects = new List<HiddenEffect>
                    {
                        new HiddenEffect { variableName = GameConstants.Vars.UclProgress, changeAmount = 1 }
                    }
                },
                left = new ChoiceData
                {
                    choiceText = "Bireysel Oyna",
                    fame = 10,
                    relations = -10,
                    form = -10,
                    hiddenEffects = new List<HiddenEffect>
                    {
                        new HiddenEffect { variableName = GameConstants.Vars.UclProgress, changeAmount = 1 }
                    }
                }
            };
        }

        /// <summary>UCL finali zincirinin bitiş düğümü (sistem kartı).</summary>
        public static CardData CreateUclFinalEndNode()
        {
            return new CardData
            {
                id = GameConstants.SystemCardIds.UclFinalEndNode,
                category = GameConstants.Categories.UclSystem,
                scenarioText = "LOK_UCL_FINAL_CONCLUDED",
                characterImageName = "ucl_atmosphere",
                right = new ChoiceData { choiceText = "LOK_CONTINUE" },
                left = new ChoiceData { choiceText = "LOK_CONTINUE" }
            };
        }
    }
}
