using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Data;

/// <summary>
/// Şampiyonlar Ligi Finali senaryosunu ve maç akışını yönetir.
/// GameEvents dinleyerek otomatik sahneye çıkar.
/// </summary>
public class UCLManager : MonoBehaviour
{
    public static UCLManager Instance;

    [Header("UCL Veri Ayarları")]
    [SerializeField] private TextAsset uclCardsFile;
    private List<CardData> _uclCardPool;

    [Header("Atmosfer Renkleri (Görsel Tema)")]
    [SerializeField] private Color themeColor = new Color(0.05f, 0.12f, 0.35f, 1f);
    [SerializeField] private Color panelColor = new Color(0.02f, 0.05f, 0.15f, 1f);
    [SerializeField] private Color iconColor = new Color(0.99f, 0.94f, 0.83f, 1f);

    public bool isFinalRunning { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadUCLCards();
    }

    // ========================================================================
    // EVENT BAĞLANTILARI
    // ========================================================================
    private void OnEnable()
    {
        GameEvents.OnUCLFinalTriggered += StartUCLFinal;
        GameEvents.OnCardResolved += CheckUCLCompletion;
    }

    private void OnDisable()
    {
        GameEvents.OnUCLFinalTriggered -= StartUCLFinal;
        GameEvents.OnCardResolved -= CheckUCLCompletion;
    }

    // ========================================================================
    // VERİ YÜKLEME
    // ========================================================================
    private void LoadUCLCards()
    {
        if (uclCardsFile == null)
        {
            Debug.LogWarning("[UCLManager] UCL kart JSON dosyası atanmamış!");
            return;
        }

        try
        {
            var data = JsonUtility.FromJson<CardCollection>(uclCardsFile.text);
            if (data != null && data.cards != null)
            {
                _uclCardPool = new List<CardData>(data.cards);
                Debug.Log($"[UCLManager] {_uclCardPool.Count} UCL kartı yüklendi.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UCLManager] JSON okuma hatası: {e.Message}");
        }
    }

    // ========================================================================
    // 🏆 FİNALİ BAŞLAT
    // ========================================================================
    private void StartUCLFinal()
    {
        if (_uclCardPool == null || _uclCardPool.Count == 0)
        {
            Debug.LogWarning("[UCLManager] UCL kart havuzu boş, final başlatılamıyor.");
            return;
        }

        if (isFinalRunning) return;

        isFinalRunning = true;
        Debug.Log("<color=blue>[UCL] ŞAMPİYONLAR LİGİ FİNALİ BAŞLIYOR!</color>");

        // Momentum sıfırla
        StoryManager.Instance?.SetVariable("ucl_momentum", 0);

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
        var endNode = new CardData
        {
            id = "ucl_final_end_node",
            category = "ucl_system",
            scenarioText = "LOK_UCL_FINAL_CONCLUDED",
            characterImageName = "ucl_atmosphere",
            right = new ChoiceData { choiceText = "LOK_CONTINUE" },
            left = new ChoiceData { choiceText = "LOK_CONTINUE" }
        };
        finalChain.Add(endNode);

        // Tema değişimi
        if (CardManager.Instance != null)
        {
            var uiHandler = CardManager.Instance.GetComponent<CardUIHandler>();
            if (uiHandler != null)
            {
                // Renkleri uygula (hemen)
                uiHandler.ApplyTheme(ThemeType.Normal, true);
                // Renkleri özel UCL renklerine çevir (isteğe bağlı)
                // Not: Burada doğrudan renk değişimi yapmak istersen, CardUIHandler'a özel bir metod ekleyebilirsin.
                // Şimdilik geçiyoruz, çünkü tema değişimi CardManager'ın içinde zaten yapılıyor.
            }

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
            Debug.LogWarning($"[UCLManager] '{category}' kategorisinde kart bulunamadı!");
        }
    }

    // ========================================================================
    // 🔍 FİNAL BİTTİ Mİ?
    // ========================================================================
    private void CheckUCLCompletion(CardData card, bool isRight)
    {
        if (!isFinalRunning || card == null) return;

        if (card.id == "ucl_final_end_node")
        {
            // Temayı normale döndür
            if (CardManager.Instance != null)
            {
                var uiHandler = CardManager.Instance.GetComponent<CardUIHandler>();
                uiHandler?.ApplyTheme(ThemeType.Normal, true);
            }

            isFinalRunning = false;
            Debug.Log("<color=blue>[UCL] Final Sona Erdi. SeasonManager'a Haber Veriliyor.</color>");
            GameEvents.OnUCLFinalCompleted?.Invoke();
        }
    }
}