using UnityEngine;
using System;
using Game.Core;
using Game.Data;
using Game.Services;
using Game.Gameplay;

public class GameManager : Singleton<GameManager>
{
    [Header("Oyuncu Kimliği")]
    public string playerName = "İsimsiz";

    [Header("Faz (Kariyer Dönemi)")]
    public int currentPhaseIndex = 0;
    public string[] careerPhases = { "Çaylak (18-23)", "Yükseliş (24-28)", "Prime (29-34)", "Efsane (35+)" };

    [Header("Stat Başlangıç ve Limitler (Balancing)")]
    [SerializeField] private int startValue = 50;
    [SerializeField] private int maxStatValue = 100;
    [SerializeField] private int minStatValue = 0;

    /// <summary>
    /// Oyuncunun 5 kaynağının TEK doğru kaynağı. Sahibi burasıdır (bkz.
    /// docs/REFACTOR_PLAN.md Katman 2 — StatService).
    /// </summary>
    public StatService Stats { get; private set; }

    // Geçici uyumluluk property'leri: form/money/fame/relations/focus'u OKUYAN eski
    // tüketiciler (DiceManager, RequirementService, SaveManager, StatsManager,
    // TeamManager) bu adımda değişmeden derlensin diye korunuyor. Hiçbiri bu alanlara
    // YAZMIYOR. Tüketiciler doğrudan Stats'a geçtiğinde bu property'ler kaldırılacak.
    public int form => Stats.Get(StatType.Form);
    public int money => Stats.Get(StatType.Money);
    public int fame => Stats.Get(StatType.Fame);
    public int relations => Stats.Get(StatType.Relations);
    public int focus => Stats.Get(StatType.Focus);

    [Header("UI İndikatörleri (Gösterge Nesneleri)")]
    public GameObject displayForm;
    public GameObject displayMoney;
    public GameObject displayFame;
    public GameObject displayRelations;
    public GameObject displayFocus;

    [Header("Can ve Kalp Sistemi")]
    [SerializeField] private LifeSystem lifeSystem = new LifeSystem();
    public GameObject[] heartIcons; // LifeSystem'e bağlanacak

    /// <summary>Kalan can sayısı. SaveManager save/load sırasında bunu okur/yazar.</summary>
    public int CurrentLives => lifeSystem.CurrentLives;

    /// <summary>
    /// Oyunun yüksek seviye akış durumu — TEK doğru kaynak (bkz.
    /// docs/REFACTOR_PLAN.md Katman 0 — GameStateMachine). Değişimler
    /// <see cref="GameEvents.OnGameStateChanged"/>'e köprülenir.
    /// </summary>
    public GameStateMachine State { get; private set; }

    // ========================================================================
    //  BAŞLANGIÇ VE EVENT BAĞLANTILARI
    // ========================================================================
    protected override void OnSingletonAwake()
    {
        Stats = new StatService(minStatValue, maxStatValue, startValue);

        State = new GameStateMachine();
        State.OnStateChanged += HandleStateChanged;

        // LifeSystem'e kalp ikonlarını ver
        lifeSystem.HeartIcons = heartIcons;
        lifeSystem.OnGameOver += HandleGameOver;
        lifeSystem.OnLifeLost += HandleLifeLost;
    }

    private void OnEnable()
    {
        GameEvents.OnCardSwiped += HandleCardSwiped;
        GameEvents.OnLifeLost += lifeSystem.LoseLife; // Dışarıdan gelen can kaybı talepleri
    }

    private void OnDisable()
    {
        GameEvents.OnCardSwiped -= HandleCardSwiped;
        GameEvents.OnLifeLost -= lifeSystem.LoseLife;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        State.OnStateChanged -= HandleStateChanged;
        lifeSystem.OnGameOver -= HandleGameOver;
        lifeSystem.OnLifeLost -= HandleLifeLost;
    }

    private void HandleStateChanged(GameState previous, GameState current)
    {
        GameEvents.OnGameStateChanged?.Invoke(previous, current);
    }

    // ========================================================================
    //  KARİYER BAŞLATMA
    // ========================================================================
    public void StartCareer(string pName)
    {
        playerName = pName;
        Stats.ResetAll(startValue);
        currentPhaseIndex = 0;

        lifeSystem.Initialize(heartIcons);
        State.Set(GameState.Playing);
        GameEvents.OnResourcesUpdated?.Invoke();
    }

    // ========================================================================
    //  🔥 KART OYNANDIĞINDA STAT UYGULAMA (MERKEZ)
    // ========================================================================
    private void HandleCardSwiped(CardData card, bool isRight)
    {
        if (card == null) return;

        var hits = Stats.ApplyChoice(isRight ? card.right : card.left);

        // Eski davranışla birebir: kart başına en fazla bir kriz tetiklenir — sınıra
        // çarpan statlardan ilki (Form→Money→Fame→Relations→Focus sırasıyla) kazanır.
        if (hits.Count > 0)
            GameEvents.OnStatCritical?.Invoke(hits[0].Stat.ToString());

        // UI'ı güncelle
        GameEvents.OnResourcesUpdated?.Invoke();
        GameEvents.OnCardResolved?.Invoke(card, isRight);
    }

    // ========================================================================
    //  🛠️ STAT YÖNETİMİ (PUBLIC METODLAR - ESKİ KOD UYUMLULUĞU)
    // ========================================================================
    // Not: Bu üç metot bilinçli olarak kriz TETİKLEMEZ (sezon ödülleri, save/load,
    // kriz-reset gibi "kart dışı" stat değişimleri için kullanılır). Kriz yalnızca
    // HandleCardSwiped'ten fırlatılır — bkz. docs/REFACTOR_PLAN.md Katman 3.
    public void ApplyStatChange(string statName, int amount)
    {
        if (StatService.TryResolve(statName, out var statEnum))
            ApplyStatChange(statEnum, amount);
        else
            Debug.LogWarning($"[GameManager] Geçersiz stat adı: {statName}");
    }

    public void ApplyStatChange(StatType stat, int amount)
    {
        Stats.Apply(stat, amount);
        GameEvents.OnResourcesUpdated?.Invoke();
    }

    public void SetStat(string statName, int value)
    {
        if (StatService.TryResolve(statName, out var statEnum))
        {
            Stats.Set(statEnum, value);
            GameEvents.OnResourcesUpdated?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[GameManager] Geçersiz stat adı: {statName}");
        }
    }

    // ========================================================================
    //  ❤️ CAN KAYBI VE SONRASI (LifeSystem Tarafından Tetiklenir)
    // ========================================================================
    /// <summary>Save/load sırasında canı doğrudan geri yükler (yan etki üretmeden).</summary>
    public void RestoreLives(int lives) => lifeSystem.SetLives(lives);

    private void HandleLifeLost()
    {
        // Can kaybı sonrası stat'ları başlangıç değerine sıfırla
        Stats.ResetAll();
        GameEvents.OnResourcesUpdated?.Invoke();
    }

    private void HandleGameOver()
    {
        State.Set(GameState.GameOver);
        OnGameOver?.Invoke();
        GameEvents.OnGameOver?.Invoke();
    }

    // ========================================================================
    //  🚀 FAZ İLERLETME
    // ========================================================================
    public void AdvanceToNextPhase()
    {
        currentPhaseIndex++;
        GameEvents.OnPhaseAdvanced?.Invoke();
    }

    // ========================================================================
    //  📢 EVENT'LER (Dışarıya haber verir — geriye dönük uyumluluk)
    // ========================================================================
    public static event Action OnGameOver;
}
