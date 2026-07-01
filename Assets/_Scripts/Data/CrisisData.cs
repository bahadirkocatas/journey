using System.Collections.Generic;

namespace Game.Data
{
    /// <summary>Krizin hangi uçta tetiklendiği (<c>crisis.json</c> "condition" alanı).</summary>
    public enum CrisisCondition
    {
        /// <summary>Stat 0'a düştü.</summary>
        Min,

        /// <summary>Stat 100'e ulaştı.</summary>
        Max
    }

    /// <summary>
    /// Bir kriz anını temsil eder. Bir stat kritik değere (0 veya 100) ulaştığında
    /// <see cref="CrisisManager"/> tarafından tetiklenir.
    ///
    /// JSON ile birebir uyumludur (<c>crisis.json</c>).
    /// </summary>
    [System.Serializable]
    public class CrisisData
    {
        /// <summary>Benzersiz kimlik ("crisis_form_min_injury" gibi).</summary>
        public string id;

        /// <summary>Krizi tetikleyen stat adı (<see cref="GameConstants.Stats"/> ile aynı).</summary>
        public string targetStat;

        /// <summary>Krizin Min mi Max mı tarafında tetiklendiği.</summary>
        public CrisisCondition condition;

        /// <summary>Kriz kartının başlığı.</summary>
        public string title;

        /// <summary>Kriz kartının açıklama metni.</summary>
        public string description;

        /// <summary>Kriz ikonunun Resources altındaki yolu.</summary>
        public string iconPath;

        /// <summary>Bu krizin can kaybına mal olup olmadığı.</summary>
        public bool costsLife;

        /// <summary>Kriz çözüldüğünde <see cref="targetStat"/>'ın sıfırlanacağı değer.</summary>
        public int resetValue;

        /// <summary>
        /// Krizin geçerli olduğu minimum faz indeksi (0: Çaylak, 1: Yükseliş, 2: Prime,
        /// 3: Efsane). <c>crisis.json</c>'da şu an karşılığı yok, default (her zaman
        /// geçerli) korunuyor — bkz. REFACTOR_PLAN.md madde 5.
        /// </summary>
        public int minPhaseIndex = 0;

        /// <summary>Krizin geçerli olduğu maksimum faz indeksi. Bkz. <see cref="minPhaseIndex"/>.</summary>
        public int maxPhaseIndex = 10;
    }

    /// <summary>JSON kökü — <c>crisis.json</c>'daki "crises" dizisini taşır.</summary>
    [System.Serializable]
    public class CrisisCollection
    {
        public List<CrisisData> crises;
    }
}
