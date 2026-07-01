using UnityEngine;
using System.Collections.Generic;
using System;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    [Header("Veri Dosyası")]
    [SerializeField] private TextAsset jsonFile;

    [Header("Sezon Ayarları")]
    [SerializeField] private int matchesPerSeason = 3;
    [SerializeField] private int maxChainPerSeason = 3;

    // Servisler
    private RequirementService _reqService;
    private CardRegistry _registry;
    private CardDeckBuilder _deckBuilder;
    private CardUIHandler _uiHandler;

    // Veri – public get, private set
    public CardData currentCard { get; private set; }
    public bool isGameOver { get; private set; }
    public bool isCrisisActive { get; private set; }

    // Cache (performans) – public get
    public bool isRightLocked { get; private set; }
    public bool isLeftLocked { get; private set; }

    // Queue
    private readonly Queue<CardData> _eventQueue = new Queue<CardData>();
    public int QueueCount => _eventQueue.Count;
    private string _forcedNextCardID = "";
    private CrisisData _pendingCrisis;

    public int queueCount => _eventQueue.Count;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        _reqService = new RequirementService();
        _registry = new CardRegistry();
        _deckBuilder = new CardDeckBuilder(_registry, _reqService);
        _uiHandler = GetComponent<CardUIHandler>();
        if (_uiHandler == null) _uiHandler = gameObject.AddComponent<CardUIHandler>();

        _registry.LoadFromJSON(jsonFile);
    }

    private void OnEnable() => GameEvents.OnCardResolved += HandleCardResolved;
    private void OnDisable() => GameEvents.OnCardResolved -= HandleCardResolved;

    // ============================================================
    //  DESTE YÖNETİMİ
    // ============================================================
    public void BuildSeasonDeck(int targetSize)
    {
        _eventQueue.Clear();
        var deck = _deckBuilder.BuildSeasonDeck(targetSize, matchesPerSeason, maxChainPerSeason);
        foreach (var card in deck) _eventQueue.Enqueue(card);
    }

    public void InjectMilestoneCards(List<CardData> cards, bool putToFront)
    {
        if (cards == null || cards.Count == 0) return;
        var temp = new List<CardData>(_eventQueue);
        _eventQueue.Clear();

        if (putToFront)
        {
            foreach (var c in cards) _eventQueue.Enqueue(c);
            foreach (var c in temp) _eventQueue.Enqueue(c);
        }
        else
        {
            foreach (var c in temp) _eventQueue.Enqueue(c);
            foreach (var c in cards) _eventQueue.Enqueue(c);
        }
    }

    public void QueueNextCard(string cardID) => _forcedNextCardID = cardID;
    public CardData GetCardByID(string id) => _registry.GetCardByID(id);

    // ============================================================
    //  KART YÜKLEME (AKIŞ)
    // ============================================================
    public void LoadNewCard()
    {
        if (isGameOver) return;

        if (_pendingCrisis != null)
        {
            LoadCrisisCard(_pendingCrisis);
            _pendingCrisis = null;
            return;
        }
        if (isCrisisActive) return;

        // Zorunlu kart
        if (!string.IsNullOrEmpty(_forcedNextCardID))
        {
            var target = _registry.GetCardByID(_forcedNextCardID);
            _forcedNextCardID = "";
            if (target != null && _reqService.CheckRequirements(target.requirements, $"card_{target.id}"))
            {
                SetCurrentCard(target);
                return;
            }
        }

        // Kuyruk
        while (_eventQueue.Count > 0)
        {
            var next = _eventQueue.Dequeue();
            if (next != null && _reqService.CheckRequirements(next.requirements, $"card_{next.id}"))
            {
                SetCurrentCard(next);
                return;
            }
        }

        // Yedek
        var fallback = _registry.AllCards.Count > 0 ? _registry.AllCards[0] : null;
        if (fallback != null) SetCurrentCard(fallback);
    }

    private void SetCurrentCard(CardData card)
    {
        currentCard = card;
        isRightLocked = !_reqService.CheckRequirements(card.rightRequirements, $"right_{card.id}");
        isLeftLocked = !_reqService.CheckRequirements(card.leftRequirements, $"left_{card.id}");

        if (_uiHandler != null)
            _uiHandler.UpdateCardDisplay(card, isRightLocked, isLeftLocked);

        // Yeni kart yüklendi – CardController reset için dinleyebilir
        GameEvents.OnCardLoaded?.Invoke();

    }

    // ============================================================
    //  KRİZ VE OYUN SONU
    // ============================================================
    public void EnterCrisisMode(CrisisData crisis)
    {
        if (isCrisisActive || _pendingCrisis != null) return;
        _pendingCrisis = crisis;
        isCrisisActive = true;
        _uiHandler?.ApplyTheme(ThemeType.Crisis);
        LoadNewCard();
    }

    private void LoadCrisisCard(CrisisData crisis)
    {
        _uiHandler?.ManageBlinkingDisplay(crisis.targetStat, true);

        var c = new CardData
        {
            id = "CRISIS_CARD",
            scenarioText = $"<color=#FF5555>KRİZ: {crisis.title}</color>\n\n{crisis.description}",
            characterImageName = crisis.iconPath,
            rightChoiceText = "BEDELİ ÖDE",
            leftChoiceText = "BEDELİ ÖDE"
        };
        SetCurrentCard(c);
    }

    public void ExitCrisisMode()
    {
        if (isGameOver) return;
        isCrisisActive = false;
        _pendingCrisis = null;
        _uiHandler?.ManageBlinkingDisplay("", false);
        _uiHandler?.ApplyTheme(ThemeType.Normal);
        LoadNewCard();
    }

    public void EnterGameOverMode(EndingData ending)
    {
        isGameOver = true;
        isCrisisActive = false;
        _uiHandler?.ApplyTheme(ThemeType.GameOver, true);

        var c = new CardData
        {
            id = "GAMEOVER_CARD",
            scenarioText = $"<color=#888888>SON:</color> {ending.title}\n\n{ending.description}",
            characterImageName = ending.iconPath,
            rightChoiceText = ending.buttonText,
            leftChoiceText = ending.buttonText
        };
        SetCurrentCard(c);
    }

    // ============================================================
    //  OLAY BAĞLANTILARI
    // ============================================================
    private void HandleCardResolved(CardData processedCard, bool isRight)
    {
        if (processedCard == null || isGameOver) return;

        if (processedCard.id == "CRISIS_CARD")
        {
            ExitCrisisMode();
            return;
        }

        string nextID = isRight ? processedCard.rightSuccessCardID : processedCard.leftSuccessCardID;
        if (!string.IsNullOrEmpty(nextID))
        {
            QueueNextCard(nextID);
            LoadNewCard();
        }
        else
        {
            GameEvents.OnCardFlowEnded?.Invoke();
        }
    }

    public bool CheckRequirements(List<RequirementData> reqs, string cacheKey = "") =>
        _reqService.CheckRequirements(reqs, cacheKey);
}