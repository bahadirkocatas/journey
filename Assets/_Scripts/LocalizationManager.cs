using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Oyunun tüm dil desteðini yöneten merkezi sistem.
/// "Master Logic" desteði ile kariyer fazýna göre dinamik anahtar üretimi yapar.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    [Header("Dil Ayarlarý")]
    public string currentLanguage = "tr"; // "tr" veya "en"

    private Dictionary<string, string> localizedText;
    private bool isReady = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLanguage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadLanguage()
    {
        localizedText = new Dictionary<string, string>();

        // Dosya yolu: Resources/Languages/lang_tr veya lang_en
        string fileName = "Languages/lang_" + currentLanguage;
        TextAsset textAsset = Resources.Load<TextAsset>(fileName);

        if (textAsset != null)
        {
            SimpleJsonParse(textAsset.text);
            Debug.Log($"[LOCALIZATION] {currentLanguage} dili ve {localizedText.Count} anahtar baþarýyla yüklendi.");
            isReady = true;
        }
        else
        {
            Debug.LogError($"[HATA] Dil dosyasý bulunamadý! Yol: Resources/{fileName}.");
        }
    }

    private void SimpleJsonParse(string json)
    {
        // Flat JSON objelerini okumak için Regex
        MatchCollection matches = Regex.Matches(json, "\"([^\"]+)\"\\s*:\\s*\"([^\"]+)\"");
        foreach (Match match in matches)
        {
            string key = match.Groups[1].Value;
            string value = match.Groups[2].Value;

            if (!localizedText.ContainsKey(key))
                localizedText.Add(key, value);
        }
    }

    /// <summary>
    /// Statik bir anahtar için çeviri döndürür. (Eski sistemle uyumlu)
    /// </summary>
    public string GetLocalizedValue(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (!key.StartsWith("LOK_")) return key;

        if (localizedText != null && localizedText.ContainsKey(key))
        {
            return localizedText[key];
        }
        return key;
    }

    /// <summary>
    /// MASTER LOGIC: Kök anahtarý faz ve tip (DESC/R/L) ile birleþtirerek çeviriyi döndürür.
    /// Örnek: "LOK_UCL_PRE_19" + "_DESC" -> "LOK_UCL_P0_PRE_19_DESC"
    /// </summary>
    /// <param name="rootKey">JSON'daki kök anahtar (Örn: LOK_UCL_PRE_19)</param>
    /// <param name="suffix">Metin tipi (Örn: _DESC, _R, _L)</param>
    public string GetMasterValue(string rootKey, string suffix)
    {
        if (string.IsNullOrEmpty(rootKey)) return "";
        if (!rootKey.StartsWith("LOK_")) return rootKey;

        // 1. Mevcut Faz Index'ini al (0, 1, 2, 3)
        int phase = (GameManager.Instance != null) ? GameManager.Instance.currentPhaseIndex : 0;

        // 2. Anahtarý parçalara ayýr (LOK, UCL, PRE...)
        string[] parts = rootKey.Split('_');

        // Standart bir LOK anahtarý en az 3 parça olmalý (LOK_CONTEXT_KEY)
        if (parts.Length < 3) return GetLocalizedValue(rootKey + suffix);

        // 3. Yeni anahtarý inþa et: LOK + CONTEXT + P{phase} + REST + SUFFIX
        // Örn: LOK_UCL_PRE_19 -> LOK_UCL_P0_PRE_19_DESC
        string context = parts[1]; // Örn: UCL, BDO, WC

        // Kök anahtardan sonraki kýsýmlarý birleþtir
        string restOfKey = "";
        for (int i = 2; i < parts.Length; i++)
        {
            restOfKey += parts[i] + (i == parts.Length - 1 ? "" : "_");
        }

        string fullDynamicKey = $"LOK_{context}_PHS{phase}_{restOfKey}{suffix}";

        // 4. Sözlükte ara, yoksa statik anahtarý dene
        if (localizedText != null && localizedText.ContainsKey(fullDynamicKey))
        {
            return localizedText[fullDynamicKey];
        }

        // Eðer dinamik anahtar bulunamazsa (faza özel metin yazýlmamýþsa) düz anahtarý dene
        return GetLocalizedValue(rootKey + suffix);
    }

    public void ChangeLanguage(string langCode)
    {
        currentLanguage = langCode;
        LoadLanguage();
    }
}