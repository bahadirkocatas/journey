using System.Collections.Generic;

namespace Game.Data
{
    /// <summary>
    /// Bir oyun sonu senaryosunu (Game Over, Emeklilik vb.) temsil eder.
    /// <see cref="EndingManager"/> tarafından tetiklenir.
    ///
    /// JSON ile birebir uyumludur (<c>endings.json</c>).
    /// </summary>
    [System.Serializable]
    public class EndingData
    {
        /// <summary>Benzersiz kimlik ("ending_lives_zero" gibi).</summary>
        public string id;

        /// <summary>
        /// Bu sonu tetikleyen olay türü: "LivesZero" (canlar tükendi), "Retirement"
        /// (emeklilik yaşına ulaşıldı), "Specific" (özel/ID bazlı son). Enum'a
        /// çevrilmedi — kart kategorileri gibi string sabit kalması bilinçli bir
        /// mimari karar (bkz. REFACTOR_PLAN.md madde 4): "Specific" ileride ID bazlı
        /// özel sonlara açık kalsın diye.
        /// </summary>
        public string triggerType;

        /// <summary>Son ekranının başlığı.</summary>
        public string title;

        /// <summary>Son ekranının açıklama metni.</summary>
        public string description;

        /// <summary>Son ikonunun Resources altındaki yolu.</summary>
        public string iconPath;

        /// <summary>Son ekranındaki aksiyon butonunun metni ("VEDA ET", "JÜBİLE YAP" gibi).</summary>
        public string buttonText;
    }

    /// <summary>JSON kökü — <c>endings.json</c>'daki "endings" dizisini taşır.</summary>
    [System.Serializable]
    public class EndingCollection
    {
        public List<EndingData> endings;
    }
}
