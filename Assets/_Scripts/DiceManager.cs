using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using Random = UnityEngine.Random;

/// <summary>
/// BG3 tarzý zar sistemi. 
/// Puanlama: Momentum (FH+SH) + Stat Bonus (Low/High Mode) + Zar (1-20) >= Random Target (10-15)
/// </summary>
public class DiceManager : MonoBehaviour
{
    public static DiceManager Instance;

    public static event Action OnDiceRolling;
    public static event Action<bool> OnDiceFinished;

    [Header("UI - Zar Paneli")]
    public GameObject dicePanel;
    public TextMeshProUGUI textTitle;   // "HEDEF: 12"
    public TextMeshProUGUI textBonus;   // "MOMENTUM (+2) & FOCUS (+3)"
    public TextMeshProUGUI textRoll;    // Zarýn kendisi (Dönen sayý)
    public TextMeshProUGUI textResult;  // "BAÞARILI!"
    public Button rollButton;

    private int targetDifficulty;
    private int totalBonus;
    private bool isRolling = false;

    private System.Action<bool> onRollComplete;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (dicePanel) dicePanel.SetActive(false);
    }

    /// <summary>
    /// Zar sürecini baþlatýr.
    /// </summary>
    /// <param name="statName">Hangi stat kontrol edilecek (focus, fame vb.)</param>
    /// <param name="mode">"high" veya "low" kontrol modu</param>
    public void StartDiceRoll(string statName, string mode, System.Action<bool> callback)
    {
        onRollComplete = callback;
        isRolling = false;

        // 1. Rastgele Hedef Belirle (10 - 15)
        targetDifficulty = Random.Range(10, 16);

        // 2. Bonuslarý Hesapla
        int momentumBonus = GetMomentumBonus();
        int statBonus = CalculateStatBonus(statName, mode);
        totalBonus = momentumBonus + statBonus;

        // 3. UI Hazýrlýðý
        dicePanel.SetActive(true);
        textTitle.text = $"HEDEF: {targetDifficulty}";
        textBonus.text = $"MOMENTUM (+{momentumBonus}) | {statName.ToUpper()} (+{statBonus})";
        textRoll.text = "D20";
        textResult.text = "Kaderini Belirle...";
        textResult.color = Color.white;
        rollButton.interactable = true;

        rollButton.onClick.RemoveAllListeners();
        rollButton.onClick.AddListener(RollTheDice);
    }

    void RollTheDice()
    {
        if (isRolling) return;
        StartCoroutine(RollAnimation());
    }

    IEnumerator RollAnimation()
    {
        isRolling = true;
        OnDiceRolling?.Invoke();
        rollButton.interactable = false;

        float duration = 1.2f;
        float timer = 0;
        int finalRoll = 0;

        // Görsel Zar Dönme Efekti
        while (timer < duration)
        {
            finalRoll = Random.Range(1, 21);
            textRoll.text = finalRoll.ToString();
            timer += Time.deltaTime;
            yield return null;
        }

        // Final Hesaplama
        int totalScore = finalRoll + totalBonus;
        bool isSuccess = false;

        // Kritik Kontroller
        if (finalRoll == 20) isSuccess = true;
        else if (finalRoll == 1) isSuccess = false;
        else isSuccess = totalScore >= targetDifficulty;

        OnDiceFinished?.Invoke(isSuccess);
        // Sonuç Metni
        if (isSuccess)
        {
            textResult.text = finalRoll == 20 ? "KRÝTÝK BAÞARI!" : $"BAÞARILI! ({totalScore} >= {targetDifficulty})";
            textResult.color = Color.green;
        }
        else
        {
            textResult.text = finalRoll == 1 ? "KRÝTÝK HATA!" : $"BAÞARISIZ... ({totalScore} < {targetDifficulty})";
            textResult.color = Color.red;
        }

        yield return new WaitForSeconds(2.0f);
        dicePanel.SetActive(false);
        onRollComplete?.Invoke(isSuccess);
    }

    // --- MATEMATÝKSEL MANTIK ---

    int CalculateStatBonus(string statName, string mode)
    {
        int val = GetStatValue(statName);

        if (mode.ToLower() == "low")
        {
            // Düþük kaynak isterse: 0-20(3), 20-60(2), 60-80(1), 80-100(0)
            if (val <= 20) return 3;
            if (val <= 60) return 2;
            if (val <= 80) return 1;
            return -1;
        }
        else // "high" mode
        {
            // Yüksek kaynak isterse: 100-80(3), 80-40(2), 40-20(1), 0-20(0)
            if (val >= 80) return 3;
            if (val >= 40) return 2;
            if (val >= 20) return 1;
            return -1;
        }
    }

    int GetMomentumBonus()
    {
        if (StoryManager.Instance == null) return 0;
        // ucl_momentum her yarý baþarýsýnda +1 artýyor (FH+SH = Max 2)
        return StoryManager.Instance.GetVariable("ucl_momentum");
    }

    int GetStatValue(string name)
    {
        if (GameManager.Instance == null) return 50;
        switch (name.ToLower())
        {
            case "form": return GameManager.Instance.form;
            case "fame": return GameManager.Instance.fame;
            case "focus": return GameManager.Instance.focus;
            case "relations": return GameManager.Instance.relations;
            default: return 50;
        }
    }
}