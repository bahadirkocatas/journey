using UnityEngine;
using System;
using Game.Data;

// Endüstri Standardı Enum (Zaten mevcut, tekrar tanımlamıyoruz)
public enum StatType { Form, Money, Fame, Relations, Focus }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Oyuncu Kimliği")]
    public string playerName = "İsimsiz";

    [Header("Faz (Kariyer Dönemi)")]
    public int currentPhaseIndex = 0;
    public string[] careerPhases = { "Çaylak (18-23)", "Yükseliş (24-28)", "Prime (29-34)", "Efsane (35+)" };

    [Header("Stat Başlangıç ve Limitler (Balancing)")]
    [SerializeField] private int startValue = 50;
    [SerializeField] private int maxStatValue = 100;
    [SerializeField] private int minStatValue = 0;

    [Header("5 Kaynak Sistemi")]
    public int form;
    public int money;
    public int fame;
    public int relations;
    public int focus;

    [Header("UI İndikatörleri (Gösterge Nesneleri)")]
    public GameObject displayForm;
    public GameObject displayMoney;
    public GameObject displayFame;
    public GameObject displayRelations;
    public GameObject displayFocus;

    [Header("Can ve Kalp Sistemi")]
    [SerializeField] private LifeSystem lifeSystem = new LifeSystem();
    public GameObject[] heartIcons; // LifeSystem'e bağlanacak

    private bool crisisTriggeredThisTurn = false;

    // ========================================================================
    //  BAŞLANGIÇ VE EVENT BAĞLANTILARI
    // ========================================================================
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // LifeSystem'e kalp ikonlarını ver
        lifeSystem.HeartIcons = heartIcons;
        lifeSystem.OnGameOver += () => OnGameOver?.Invoke();
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
        lifeSystem.OnGameOver -= () => OnGameOver?.Invoke();
        lifeSystem.OnLifeLost -= HandleLifeLost;
    }

    // ========================================================================
    //  KARİYER BAŞLATMA
    // ========================================================================
    public void StartCareer(string pName)
    {
        playerName = pName;
        form = startValue;
        money = startValue;
        fame = startValue;
        relations = startValue;
        focus = startValue;
        currentPhaseIndex = 0;

        lifeSystem.Initialize(heartIcons);
        GameEvents.OnResourcesUpdated?.Invoke();
    }

    // ========================================================================
    //  🔥 KART OYNANDIĞINDA STAT UYGULAMA (MERKEZ)
    // ========================================================================
    private void HandleCardSwiped(CardData card, bool isRight)
    {
        if (card == null) return;
        crisisTriggeredThisTurn = false;

        // Değişimleri uygula
        if (isRight)
        {
            form += card.right.form;
            money += card.right.money;
            fame += card.right.fame;
            relations += card.right.relations;
            focus += card.right.focus;
        }
        else
        {
            form += card.left.form;
            money += card.left.money;
            fame += card.left.fame;
            relations += card.left.relations;
            focus += card.left.focus;
        }

        // Sınırları kontrol et ve Krizleri tetikle
        ClampAndCheckStats();

        // UI'ı güncelle
        GameEvents.OnResourcesUpdated?.Invoke();
        GameEvents.OnCardResolved?.Invoke(card, isRight);
    }

    // ========================================================================
    //  🛠️ STAT YÖNETİMİ (PUBLIC METODLAR - ESKİ KOD UYUMLULUĞU)
    // ========================================================================
    public void ApplyStatChange(string statName, int amount)
    {
        if (Enum.TryParse(statName, out StatType statEnum))
            ApplyStatChange(statEnum, amount);
        else
            Debug.LogWarning($"[GameManager] Geçersiz stat adı: {statName}");
    }

    public void ApplyStatChange(StatType stat, int amount)
    {
        switch (stat)
        {
            case StatType.Form: form += amount; break;
            case StatType.Money: money += amount; break;
            case StatType.Fame: fame += amount; break;
            case StatType.Relations: relations += amount; break;
            case StatType.Focus: focus += amount; break;
        }
        ClampAndCheckStats();
        GameEvents.OnResourcesUpdated?.Invoke();
    }

    public void SetStat(string statName, int value)
    {
        if (Enum.TryParse(statName, out StatType statEnum))
        {
            switch (statEnum)
            {
                case StatType.Form: form = value; break;
                case StatType.Money: money = value; break;
                case StatType.Fame: fame = value; break;
                case StatType.Relations: relations = value; break;
                case StatType.Focus: focus = value; break;
            }
            ClampAndCheckStats();
            GameEvents.OnResourcesUpdated?.Invoke();
        }
    }

    // ========================================================================
    //  ⚙️ ÖZEL METODLAR (CLAMP & KRİZ KONTROL)
    // ========================================================================
    private void ClampAndCheckStats()
    {
        form = Mathf.Clamp(form, minStatValue, maxStatValue);
        money = Mathf.Clamp(money, minStatValue, maxStatValue);
        fame = Mathf.Clamp(fame, minStatValue, maxStatValue);
        relations = Mathf.Clamp(relations, minStatValue, maxStatValue);
        focus = Mathf.Clamp(focus, minStatValue, maxStatValue);

        CheckCrisis(StatType.Form, form);
        CheckCrisis(StatType.Money, money);
        CheckCrisis(StatType.Fame, fame);
        CheckCrisis(StatType.Relations, relations);
        CheckCrisis(StatType.Focus, focus);
    }

    private void CheckCrisis(StatType stat, int statValue)
    {
        if (crisisTriggeredThisTurn) return;
        if (statValue <= minStatValue || statValue >= maxStatValue)
        {
            crisisTriggeredThisTurn = true;
            GameEvents.OnStatCritical?.Invoke(stat.ToString());
        }
    }

    // ========================================================================
    //  ❤️ CAN KAYBI VE SONRASI (LifeSystem Tarafından Tetiklenir)
    // ========================================================================
    private void HandleLifeLost()
    {
        // Can kaybı sonrası stat'ları sıfırla (reset değeri LifeSystem'den alınır, ama biz sabit 50 kullanalım)
        // LifeSystem zaten ResetStats event'ini fırlatmıyor, doğrudan burada yapalım.
        form = 50;
        money = 50;
        fame = 50;
        relations = 50;
        focus = 50;

        GameEvents.OnResourcesUpdated?.Invoke();
    }

    // ========================================================================
    //  🚀 FAZ İLERLETME
    // ========================================================================
    public void AdvanceToNextPhase()
    {
        currentPhaseIndex++;
        OnPhaseAdvanced?.Invoke();
    }

    // ========================================================================
    //  📢 EVENT'LER (Dışarıya haber verir)
    // ========================================================================
    public static event Action OnPhaseAdvanced;
    public static event Action OnGameOver;
}