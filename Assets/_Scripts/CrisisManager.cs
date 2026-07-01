using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Data;

/// <summary>
/// Kriz anlarını yönetir. Bir stat 0 veya 100 olduğunda devreye girer.
/// GameEvents dinleyerek çalışır, doğrudan bağımlılığı yoktur.
/// </summary>
public class CrisisManager : MonoBehaviour
{
    public static CrisisManager Instance { get; private set; }

    [Header("Veri Dosyası")]
    [SerializeField] private TextAsset crisisJsonFile;
    private List<CrisisData> _allCrises = new List<CrisisData>();

    public CrisisData activeCrisis { get; private set; }

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
        LoadCrises();
    }

    private void OnEnable()
    {
        GameEvents.OnStatCritical += HandleStatCritical;
        GameEvents.OnCardResolved += CheckCrisisResolution;
    }

    private void OnDisable()
    {
        GameEvents.OnStatCritical -= HandleStatCritical;
        GameEvents.OnCardResolved -= CheckCrisisResolution;
    }

    // ========================================================================
    // 📦 VERİ YÜKLEME
    // ========================================================================
    private void LoadCrises()
    {
        if (crisisJsonFile == null)
        {
            Debug.LogError("[CrisisManager] Kriz JSON dosyası atanmamış!");
            return;
        }

        try
        {
            var data = JsonUtility.FromJson<CrisisCollection>(crisisJsonFile.text);
            if (data != null && data.crises != null)
            {
                _allCrises = data.crises;
                Debug.Log($"[CrisisManager] {_allCrises.Count} kriz yüklendi.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CrisisManager] JSON okuma hatası: {e.Message}");
        }
    }

    // ========================================================================
    // 🚨 KRİZ TETİKLEME
    // ========================================================================
    private void HandleStatCritical(string statName)
    {
        // Zaten aktif kriz varsa çık
        if (activeCrisis != null) return;

        int currentPhase = GameManager.Instance?.currentPhaseIndex ?? 0;

        // Uygun krizleri bul (stat ve faz filtreleri ile)
        var possibleCrises = _allCrises.Where(c =>
            c.targetStat == statName &&
            currentPhase >= c.minPhaseIndex &&
            currentPhase <= c.maxPhaseIndex
        ).ToList();

        if (possibleCrises.Count == 0)
        {
            // Uygun kriz yoksa sadece can düş ve stat'ı sıfırla
            Debug.LogWarning($"[CrisisManager] '{statName}' için uygun kriz bulunamadı. Sadece can düşürülüyor.");
            GameEvents.OnLifeLost?.Invoke();
            GameManager.Instance?.SetStat(statName, 50);
            return;
        }

        // Rastgele bir kriz seç
        activeCrisis = possibleCrises[Random.Range(0, possibleCrises.Count)];
        Debug.Log($"[CrisisManager] Kriz patladı: {activeCrisis.title}");

        // 1. Can düşür
        GameEvents.OnLifeLost?.Invoke();

        // 2. Kriz modunu başlat
        if (CardManager.Instance != null && !CardManager.Instance.isGameOver)
        {
            CardManager.Instance.EnterCrisisMode(activeCrisis);
        }
    }

    // ========================================================================
    // 🔍 KRİZ ÇÖZÜLDÜ MÜ?
    // ========================================================================
    private void CheckCrisisResolution(CardData card, bool isRight)
    {
        if (activeCrisis == null || card == null) return;

        if (card.id == "CRISIS_CARD")
        {
            ResolveActiveCrisis();
        }
    }

    private void ResolveActiveCrisis()
    {
        if (activeCrisis == null) return;

        Debug.Log($"[CrisisManager] Kriz çözüldü: {activeCrisis.title}");

        // Stat'ı reset değerine getir
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetStat(activeCrisis.targetStat, activeCrisis.resetValue);
        }

        var resolvedCrisis = activeCrisis;
        activeCrisis = null;

        // Kriz modundan çık
        if (CardManager.Instance != null)
        {
            CardManager.Instance.ExitCrisisMode();
        }
    }

    // ========================================================================
    // 🛠️ YARDIMCI METOTLAR (Dışarıdan manuel tetikleme için)
    // ========================================================================
    public void TriggerCrisisManually(string statName)
    {
        HandleStatCritical(statName);
    }

    public bool HasActiveCrisis => activeCrisis != null;
}