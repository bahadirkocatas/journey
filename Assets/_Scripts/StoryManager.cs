using UnityEngine;
using System.Collections.Generic;
using Game.Data;

/// <summary>
/// Oyunun salt hafıza merkezi. Bayrakları (Flags) ve Değişkenleri (Variables) tutar.
/// BÜYÜK MVP GÜNCELLEMESİ: Origin (Başlangıç Hikayesi), Trait (Yetenek) ve Json'dan 
/// hikaye okuma sistemleri tamamen SİLİNDİ.
/// Artık sadece oynanan kartların bıraktığı izleri (Flag/Variable) ve Cooldown'ları hatırlar.
/// </summary>
public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance;

    // --- VERİ DEPOLARI (SAF HAFIZA) ---
    // Not: SaveManager'ın erişebilmesi için hepsi public bırakıldı.
    public HashSet<string> storyFlags = new HashSet<string>();
    public Dictionary<string, int> storyVariables = new Dictionary<string, int>();
    public Dictionary<string, string> storyStrings = new Dictionary<string, string>(); // Dinamik UI metinleri için (Örn: Takım Adı)
    public Dictionary<string, int> cardCooldowns = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Kart atıldığında veya hafta geçtiğinde haberimiz olacak.
        GameEvents.OnCardResolved += HandleCardResolved;
        GameEvents.OnWeekAdvanced += HandleWeekAdvanced;
    }

    private void OnDisable()
    {
        GameEvents.OnCardResolved -= HandleCardResolved;
        GameEvents.OnWeekAdvanced -= HandleWeekAdvanced;
    }

    // ========================================================================
    // 🃏 KART ÇÖZÜMLEME VE HAFIZAYA YAZMA
    // ========================================================================
    private void HandleCardResolved(CardData card, bool isRight)
    {
        if (card == null) return;

        // Kartın bekleme süresi (Cooldown) varsa hafızaya al
        if (card.cooldownWeeks > 0)
        {
            SetCooldown(card.id, card.cooldownWeeks);
        }

        // Seçime göre gizli etkileri ve bayrakları uygula
        if (isRight)
        {
            if (!string.IsNullOrEmpty(card.right.addFlag)) AddFlag(card.right.addFlag);

            if (card.right.hiddenEffects != null)
            {
                foreach (var eff in card.right.hiddenEffects) ModifyVariable(eff.variableName, eff.changeAmount);
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(card.left.addFlag)) AddFlag(card.left.addFlag);

            if (card.left.hiddenEffects != null)
            {
                foreach (var eff in card.left.hiddenEffects) ModifyVariable(eff.variableName, eff.changeAmount);
            }
        }
    }

    // Her hafta geçtiğinde cooldownları (bekleme sürelerini) düşür
    private void HandleWeekAdvanced(int currentWeek)
    {
        List<string> keys = new List<string>(cardCooldowns.Keys);
        foreach (string key in keys)
        {
            if (cardCooldowns[key] > 0) cardCooldowns[key]--;
        }
    }

    // ========================================================================
    // 🚩 BAYRAK VE DEĞİŞKEN (FLAG & VARIABLE) YÖNETİMİ
    // ========================================================================
    public void AddFlag(string flag) { if (!string.IsNullOrEmpty(flag)) storyFlags.Add(flag); }
    public void RemoveFlag(string flag) { if (storyFlags.Contains(flag)) storyFlags.Remove(flag); }
    public bool HasFlag(string flag) { return storyFlags.Contains(flag); }

    public void SetVariable(string key, int value) { storyVariables[key] = value; }
    public void ModifyVariable(string key, int change)
    {
        if (storyVariables.ContainsKey(key)) storyVariables[key] += change;
        else storyVariables[key] = change;
    }
    public int GetVariable(string key) { return storyVariables.ContainsKey(key) ? storyVariables[key] : 0; }

    public void SetString(string key, string value) { storyStrings[key] = value; }
    public string GetString(string key) => storyStrings.ContainsKey(key) ? storyStrings[key] : "";

    public void SetCooldown(string cardID, int weeks) { cardCooldowns[cardID] = weeks; }
    public bool IsOnCooldown(string cardID) { return cardCooldowns.ContainsKey(cardID) && cardCooldowns[cardID] > 0; }
}