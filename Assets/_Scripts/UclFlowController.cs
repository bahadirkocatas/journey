using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Data;
using Game.Services;

/// <summary>
/// Şampiyonlar Ligi akışının tek sahibi: grup eleme kartları, final zincirinin tetiklenmesi/
/// yürütülmesi ve bitişinin haber verilmesi. Eskiden bu sorumluluk <c>UCLManager</c> (final
/// zinciri) ile <c>SeasonManager</c> (grup kartları + final tetikleme kararı) arasında
/// bölünmüştü ve UCL "çalışıyor mu" durumu iki ayrı boolean'da (<c>UCLManager.isFinalRunning</c>,
/// <c>SeasonManager.isTimeFrozenForUCL</c>) tutulurdu (bkz. REFACTOR_PLAN.md madde 7-H,
/// madde 13.2). Artık bu durumun TEK doğru kaynağı <see cref="IsFinalRunning"/>'dir;
/// <c>SeasonManager</c> sezon sınırına ulaştığında <see cref="TryHandleSeasonEnd"/> ile
/// buraya delege eder.
///
/// Not: risk/zar (BG3 tarzı) akışının buraya bağlanması bilinçli olarak Katman 4'e
/// (UI katmanındaki <c>DiceManager</c> migrasyonu) ertelendi — bkz. madde 13.1.
/// </summary>
public class UclFlowController : Singleton<UclFlowController>
{
    [Header("UCL Veri Ayarları")]
    [SerializeField] private TextAsset uclCardsFile;
    private List<CardData> _uclCardPool;

    [Header("Atmosfer Renkleri (Görsel Tema)")]
    [SerializeField] private Color themeColor = new Color(0.05f, 0.12f, 0.35f, 1f);
    [SerializeField] private Color panelColor = new Color(0.02f, 0.05f, 0.15f, 1f);
    [SerializeField] private Color iconColor = new Color(0.99f, 0.94f, 0.83f, 1f);

    /// <summary>UCL final zinciri şu an oynanıyor mu? (eski: iki ayrı bayrağın birleştiği tek kaynak.)</summary>
    public bool IsFinalRunning { get; private set; }

    protected override void OnSingletonAwake()
    {
        LoadUCLCards();
    }

    // ========================================================================
    // EVENT BAĞLANTILARI
    // ========================================================================
    private void OnEnable()
    {
        GameEvents.OnUclFinalTriggered += StartUCLFinal;
        GameEvents.OnCardResolved += CheckUCLCompletion;
    }

    private void OnDisable()
    {
        GameEvents.OnUclFinalTriggered -= StartUCLFinal;
        GameEvents.OnCardResolved -= CheckUCLCompletion;
    }

    // ========================================================================
    // VERİ YÜKLEME
    // ========================================================================
    private void LoadUCLCards()
    {
        if (uclCardsFile == null)
        {
            Debug.LogWarning("[UclFlowController] UCL kart JSON dosyası atanmamış!");
            return;
        }

        try
        {
            var data = JsonUtility.FromJson<CardCollection>(uclCardsFile.text);
            if (data != null && data.cards != null)
            {
                _uclCardPool = new List<CardData>(data.cards);
                Debug.Log($"[UclFlowController] {_uclCardPool.Count} UCL kartı yüklendi.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UclFlowController] JSON okuma hatası: {e.Message}");
        }
    }

    // ========================================================================
    // SEZON AKIŞI İLE TEMAS NOKTALARI (SeasonManager tarafından çağrılır)
    // ========================================================================

    /// <summary>
    /// Yeni sezon başlarken (SeasonManager.StartNewSeasonRoutine'den) UCL kalifiyeyse grup
    /// eleme kartlarını kuyruğa ekler. Eskiden bu enjeksiyon SeasonManager'ın kendi
    /// içindeydi (bkz. madde 13.2).
    /// </summary>
    public void InjectGroupStageCardsIfQualified()
    {
        if (StoryManager.Instance == null || !StoryManager.Instance.HasFlag(GameConstants.Flags.UclQualified))
            return;

        var uclCards = new List<CardData>();
        for (int i = 1; i <= 2; i++)
            uclCards.Add(SystemCardFactory.CreateUclGroupCard(i));

        CardManager.Instance?.InjectMilestoneCards(uclCards, false);
    }

    /// <summary>
    /// Sezon sınırına ulaşıldığında SeasonManager tarafından çağrılır (yalnızca
    /// <see cref="IsFinalRunning"/> false iken erişilir — final sırasında SeasonManager
    /// takvimi zaten dondurur). UCL kalifikasyon durumuna göre finali tetikleyebilir,
    /// eleme kartı enjekte edebilir ya da kalan kart kuyruğunu boşaltabilir.
    /// </summary>
    /// <returns>
    /// <c>true</c>: sezon sonu ERTELENMELİ (bu haftaki iş bitmedi, SeasonManager
    /// <c>EndSeason</c>'ı çağırmamalı). <c>false</c>: sezon gerçekten bitti.
    /// </returns>
    public bool TryHandleSeasonEnd()
    {
        if (StoryManager.Instance != null && StoryManager.Instance.HasFlag(GameConstants.Flags.UclQualified))
        {
            if (StoryManager.Instance.GetVariable(GameConstants.Vars.UclProgress) >= 2)
            {
                if (!IsFinalRunning)
                {
                    GameEvents.OnUclFinalTriggered?.Invoke();
                    StoryManager.Instance.SetVariable(GameConstants.Vars.UclProgress, 0);
                    CardManager.Instance?.LoadNewCard();
                    return true;
                }
            }
            else if (!StoryManager.Instance.HasFlag(GameConstants.Flags.UclEliminationShown))
            {
                var failCard = SystemCardFactory.CreateUclEliminatedCard();
                CardManager.Instance?.InjectMilestoneCards(new List<CardData> { failCard }, true);
                StoryManager.Instance.AddFlag(GameConstants.Flags.UclEliminationShown);
                StoryManager.Instance.SetVariable(GameConstants.Vars.UclProgress, 0);
            }
        }

        if (CardManager.Instance != null && CardManager.Instance.QueueCount > 0)
        {
            CardManager.Instance.LoadNewCard();
            return true;
        }

        StoryManager.Instance?.RemoveFlag(GameConstants.Flags.UclEliminationShown);
        return false;
    }

    // ========================================================================
    // 🏆 FİNALİ BAŞLAT
    // ========================================================================
    private void StartUCLFinal()
    {
        if (_uclCardPool == null || _uclCardPool.Count == 0)
        {
            Debug.LogWarning("[UclFlowController] UCL kart havuzu boş, final başlatılamıyor.");
            return;
        }

        if (IsFinalRunning) return;

        IsFinalRunning = true;
        Debug.Log("<color=blue>[UCL] ŞAMPİYONLAR LİGİ FİNALİ BAŞLIYOR!</color>");

        // Momentum sıfırla
        StoryManager.Instance?.SetVariable(GameConstants.Vars.UclMomentum, 0);

        // Final zincirini oluştur
        var finalChain = new List<CardData>();

        // Sırayla kartları ekle (eğer varsa)
        AddCardIfExists(finalChain, "pre_match");
        AddCardIfExists(finalChain, "tunnel");
        AddCardIfExists(finalChain, "first_half", true); // _start ile biten
        AddCardIfExists(finalChain, "half_time");
        AddCardIfExists(finalChain, "second_half", true);
        AddCardIfExists(finalChain, "final_whistle");

        // Bitiş düğümü (sistem kartı)
        finalChain.Add(SystemCardFactory.CreateUclFinalEndNode());

        if (CardManager.Instance != null)
        {
            var uiHandler = CardManager.Instance.GetComponent<CardUIHandler>();
            uiHandler?.ApplyTheme(ThemeType.Normal, true);

            // Kartları enjekte et
            CardManager.Instance.InjectMilestoneCards(finalChain, true);
        }
    }

    private void AddCardIfExists(List<CardData> chain, string category, bool requireStartSuffix = false)
    {
        var candidates = _uclCardPool.Where(c => c.category == category).ToList();

        if (requireStartSuffix)
        {
            candidates = candidates.Where(c => c.id.EndsWith("_start")).ToList();
        }

        if (candidates.Count > 0)
        {
            // Rastgele birini seç
            var selected = candidates[Random.Range(0, candidates.Count)];
            chain.Add(selected);
        }
        else
        {
            Debug.LogWarning($"[UclFlowController] '{category}' kategorisinde kart bulunamadı!");
        }
    }

    // ========================================================================
    // 🔍 FİNAL BİTTİ Mİ?
    // ========================================================================
    private void CheckUCLCompletion(CardData card, bool isRight)
    {
        if (!IsFinalRunning || card == null) return;

        if (card.id == GameConstants.SystemCardIds.UclFinalEndNode)
        {
            // Temayı normale döndür
            if (CardManager.Instance != null)
            {
                var uiHandler = CardManager.Instance.GetComponent<CardUIHandler>();
                uiHandler?.ApplyTheme(ThemeType.Normal, true);
            }

            IsFinalRunning = false;
            Debug.Log("<color=blue>[UCL] Final Sona Erdi. SeasonManager'a Haber Veriliyor.</color>");
            GameEvents.OnUclFinalCompleted?.Invoke();
        }
    }
}
