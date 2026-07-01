using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Oyun sonu senaryolarını (Game Over, Emeklilik vb.) yönetir.
/// BÜYÜK REFACTOR: Artık GameManager veya SeasonManager'dan emir almaz.
/// Olayları (GameManager.OnGameOver ve GameEvents.OnSeasonEnded) dinleyerek otomatik devreye girer.
/// </summary>
public class EndingManager : MonoBehaviour
{
    public static EndingManager Instance;

    [Header("Veri Dosyası")]
    public TextAsset endingJsonFile;
    private List<EndingData> allEndings;

    [Header("Emeklilik Ayarı")]
    public int retirementAge = 38; // Oyuncu bu yaşa gelince otomatik emekli olur

    private void Awake() { if (Instance == null) Instance = this; }

    void Start()
    {
        LoadEndings();
    }

    // ========================================================================
    // 🧠 SİNİR SİSTEMİNE BAĞLANMA (EVENT DİNLEYİCİLERİ)
    // ========================================================================
    private void OnEnable()
    {
        // Can bittiğinde GameManager bu olayı fırlatır
        GameManager.OnGameOver += HandleGameOver;

        // Her sezon sonu yaş kontrolü yap (Emeklilik için)
        GameEvents.OnSeasonEnded += HandleSeasonEnded;
    }

    private void OnDisable()
    {
        GameManager.OnGameOver -= HandleGameOver;
        GameEvents.OnSeasonEnded -= HandleSeasonEnded;
    }

    void LoadEndings()
    {
        if (endingJsonFile != null)
        {
            EndingCollection data = JsonUtility.FromJson<EndingCollection>(endingJsonFile.text);
            if (data != null && data.endings != null)
            {
                allEndings = data.endings;
                Debug.Log($"[ENDING MANAGER]: {allEndings.Count} adet oyun sonu senaryosu başarıyla yüklendi.");
            }
        }
        else Debug.LogError("[HATA] Ending JSON dosyası atanmamış!");
    }

    // ========================================================================
    // 🚨 OYUN SONU TETİKLEYİCİLERİ
    // ========================================================================

    // Canlar (Lives) tükendiğinde otomatik çalışır
    private void HandleGameOver()
    {
        Debug.Log("[ENDING MANAGER]: Tüm canlar tükendi. Kötü son (Game Over) tetikleniyor.");
        TriggerEnding("LivesZero");
    }

    // Sezon bittiğinde oyuncu çok yaşlandıysa emekliliği tetikler
    private void HandleSeasonEnded(int score)
    {
        if (SeasonManager.Instance != null && SeasonManager.Instance.currentYear >= retirementAge)
        {
            Debug.Log($"[ENDING MANAGER]: Oyuncu {retirementAge} yaşına ulaştı. Emeklilik (Retirement) tetikleniyor.");
            TriggerEnding("Retirement");
        }
    }

    // Ana tetikleyici (Dışarıdan özel bir son istenirse veya Event'ler tarafından çağrılır)
    public void TriggerEnding(string triggerType)
    {
        if (allEndings == null || allEndings.Count == 0) return;

        // Uygun sonu bul
        EndingData ending = allEndings.FirstOrDefault(e => e.triggerType == triggerType);

        // Eğer özel bir son yoksa, hata vermemesi için varsayılan olarak ilkini al
        if (ending == null)
        {
            Debug.LogWarning($"[ENDING MANAGER]: '{triggerType}' tipinde bir son bulunamadı! Varsayılan ilk son gösteriliyor.");
            ending = allEndings[0];
        }

        Debug.Log($"[ENDING MANAGER]: Oyun Bitiyor -> {ending.title}");

        // CARD MANAGER'A GÖNDER (Karanlık tema geçişi ve kartı ekrana basmayı o halleder)
        if (CardManager.Instance != null)
        {
            CardManager.Instance.EnterGameOverMode(ending);
        }
    }
}