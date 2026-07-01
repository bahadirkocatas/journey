using System;
using Game.Data; // CardData, TeamData (Game.Data katmanından gelir — bir sonraki batch'te yazılacak)

namespace Game.Core
{
    /// <summary>
    /// Oyunun merkezi olay (event) yolu. Tüm gevşek bağlı iletişim buradan geçer.
    /// Manager'lar birbirine doğrudan referans vermek yerine bu olayları dinler/fırlatır.
    ///
    /// Bu dosya, eskiden DAĞINIK olan tüm event'leri tek yerde toplar:
    ///   - eski GameEvents (kart/stat/sezon olayları)
    ///   - eski GameManager.OnPhaseAdvanced / OnGameOver (static event'ler)
    ///   - eski DiceManager.OnDiceRolling / OnDiceFinished (static event'ler)
    /// </summary>
    public static class GameEvents
    {
        // ── Kart Akışı ─────────────────────────────────────────────────────
        public static Action<CardData, bool> OnCardSwiped;    // (kart, sağ mı) – fiziksel kaydırma bitti
        public static Action<CardData, bool> OnCardResolved;  // (kart, sağ mı) – etkiler uygulandı, kart çözüldü
        public static Action OnCardLoaded;                    // yeni kart ekrana yüklendi
        public static Action<bool, CardData> OnCardDragged;   // (sağ mı, kart) – sürükleme önizlemesi (null kart = temizle)
        public static Action OnCardFlowEnded;                 // zincir/akış bitti, hafta ilerleyebilir

        // ── Statlar & Kaynaklar ────────────────────────────────────────────
        public static Action OnResourcesUpdated;              // bir stat değişti (UI / bar güncelle)
        public static Action<string> OnStatCritical;          // stat 0 veya 100'e ulaştı (statName)
        public static Action OnLifeLost;                      // bir can kaybedildi

        // ── Zaman & Sezon ──────────────────────────────────────────────────
        public static Action<int> OnWeekAdvanced;             // (currentWeek)
        public static Action<int> OnSeasonEnded;              // (sezon performans skoru)
        public static Action OnPhaseAdvanced;                 // kariyer fazı ilerledi (Çaylak → Yükseliş → ...)

        // ── UCL Akışı ──────────────────────────────────────────────────────
        public static Action OnUclFinalTriggered;             // (eski: OnUCLFinalTriggered)
        public static Action OnUclFinalCompleted;             // (eski: OnUCLFinalCompleted)

        // ── Takım ──────────────────────────────────────────────────────────
        public static Action<TeamData> OnTeamChanged;

        // ── Zar / Risk (BG3 tarzı) ─────────────────────────────────────────
        public static Action OnDiceRolling;
        public static Action<bool> OnDiceFinished;            // (başarılı mı)

        // ── Oyun Durumu & Sonu ─────────────────────────────────────────────
        public static Action<GameState, GameState> OnGameStateChanged; // (önceki, yeni)
        public static Action OnGameOver;

        /// <summary>
        /// Tüm abonelikleri temizler. Yeni oyun başlatırken / sahne yeniden yüklenirken çağrılmalı.
        /// Unity'de "Domain Reload" kapalıysa static event'ler önceki oturumdan kalıntı (dangling)
        /// referans tutar; bu metot o sızıntıyı önler.
        /// </summary>
        public static void ClearAll()
        {
            OnCardSwiped = null;
            OnCardResolved = null;
            OnCardLoaded = null;
            OnCardDragged = null;
            OnCardFlowEnded = null;

            OnResourcesUpdated = null;
            OnStatCritical = null;
            OnLifeLost = null;

            OnWeekAdvanced = null;
            OnSeasonEnded = null;
            OnPhaseAdvanced = null;

            OnUclFinalTriggered = null;
            OnUclFinalCompleted = null;

            OnTeamChanged = null;

            OnDiceRolling = null;
            OnDiceFinished = null;

            OnGameStateChanged = null;
            OnGameOver = null;
        }
    }
}
