using System.Collections.Generic;

namespace Game.Data
{
    /// <summary>
    /// Bir kartın TEK bir seçimini (sağ VEYA sol) temsil eder.
    ///
    /// Eskiden CardData içinde "rightForm/leftForm", "rightChoiceText/leftChoiceText" gibi
    /// ~12 alan iki kez kopyalanıyordu. Artık CardData iki <see cref="ChoiceData"/> tutuyor
    /// (right & left); bu sayede tüm "if (isRight) ... else ..." dalları teke iniyor.
    /// </summary>
    [System.Serializable]
    public class ChoiceData
    {
        // ── Metinler ───────────────────────────────────────────────
        public string choiceText;   // Seçenek metni / lokalizasyon anahtarı
        public string lockedText;    // Kilitliyken gösterilecek metin

        // ── Koşullar ───────────────────────────────────────────────
        public List<RequirementData> requirements;        // Seçeneğin KİLİTLİ olma koşulu
        public List<RequirementData> successRequirements; // Risk/zar başarı koşulu

        // ── Dallanma (zincir & risk sonucu) ───────────────────────
        public string successCardID;  // Başarı → bu karta geç
        public string failureCardID;  // Başarısızlık → bu karta geç

        // ── Risk / Zar (BG3 tarzı) ────────────────────────────────
        // Alan adları cards.json / cards_ucl.json ile doğrulandı: JSON'daki
        // isRightRisk/isLeftRisk, rightRiskStat/leftRiskStat, rightRiskMode/leftRiskMode
        // önekleri CardData'nın ayrı right/left tutmasıyla zaten kalkıyor.
        public bool isRisk;     // Bu seçim bir zar atışı tetikler mi?
        public string riskStat; // Zar bonusu hangi statdan gelir? ("Form"/"Focus"/"Fame"/"Relations")
        public string riskMode; // "high" (yüksek stat avantajlı) | "low" (düşük stat avantajlı)

        // ── Stat etkileri (5 kaynak) ──────────────────────────────
        public int form;
        public int money;
        public int fame;
        public int relations;
        public int focus;

        // ── NPC ilişkisi ──────────────────────────────────────────
        public string npcName;
        public int npcChange;

        // ── Hafıza (StoryManager'a yazılır) ───────────────────────
        public string addFlag;                     // Eklenecek bayrak (eski: addFlagRight/addFlagLeft)
        public List<HiddenEffect> hiddenEffects;    // Arka plan değişkenleri

        // ── Maç içi istatistik ────────────────────────────────────
        public StatEffect statEffect;

        /// <summary>Bu seçimin görünür bir stat etkisi var mı? (önizleme/indikatör için)</summary>
        public bool HasVisibleStatEffect =>
            form != 0 || money != 0 || fame != 0 || relations != 0 || focus != 0;
    }
}