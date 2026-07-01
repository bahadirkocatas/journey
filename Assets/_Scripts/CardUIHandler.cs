using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Game.Data;

public enum ThemeType { Normal, Crisis, GameOver }

public class CardUIHandler : MonoBehaviour
{
    public static CardUIHandler Instance { get; private set; }

    [Header("UI Bağlantıları")]
    [SerializeField] private TextMeshProUGUI scenarioText;
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI rightChoiceTMP;
    [SerializeField] private TextMeshProUGUI leftChoiceTMP;

    [Header("Preview (Önizleme) Metinleri")]
    [SerializeField] private TextMeshProUGUI rightPreviewText;
    [SerializeField] private TextMeshProUGUI leftPreviewText;

    [Header("Atmosfer Renkleri")]
    [SerializeField] private Image gameBackgroundPanel;
    [SerializeField] private Image topBarPanel;
    [SerializeField] private Image bottomBarPanel;

    [Header("Renk Paleti - Normal")]
    [SerializeField] private Color normalBgColor = new Color(0.09f, 0.16f, 0.2f);
    [SerializeField] private Color normalPanelColor = new Color(0.05f, 0.09f, 0.11f);
    [SerializeField] private Color normalIconBgColor = new Color(0.18f, 0.25f, 0.3f);

    [Header("Renk Paleti - Kriz")]
    [SerializeField] private Color crisisBgColor = new Color(0.3f, 0.05f, 0.05f);
    [SerializeField] private Color crisisPanelColor = new Color(0.15f, 0.02f, 0.02f);
    [SerializeField] private Color crisisIconBgColor = new Color(0.25f, 0f, 0f);

    [Header("Renk Paleti - Game Over")]
    [SerializeField] private Color gameOverBgColor = new Color(0.05f, 0.05f, 0.05f);
    [SerializeField] private Color gameOverPanelColor = new Color(0.02f, 0.02f, 0.02f);
    [SerializeField] private Color gameOverIconBgColor = new Color(0.2f, 0.2f, 0.2f);

    private Coroutine _themeCoroutine;
    private ResourceDisplay _blinkingDisplay;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ApplyTheme(ThemeType.Normal, true);
    }

    private void OnEnable()
    {
        GameEvents.OnCardDragged += UpdatePreviewAndIndicators;
    }

    private void OnDisable()
    {
        GameEvents.OnCardDragged -= UpdatePreviewAndIndicators;
    }

    // ========================================================================
    //  KART GÖRÜNÜMÜ (Yeni kart yüklendiğinde)
    // ========================================================================
    public void UpdateCardDisplay(CardData card, bool isRightLocked, bool isLeftLocked)
    {
        if (card == null) return;

        // Senaryo metni
        scenarioText.text = GetLocalizedText(card.scenarioText, CardConstants.SuffixDesc);

        // Sağ seçenek
        if (isRightLocked && !string.IsNullOrEmpty(card.right.lockedText))
            rightChoiceTMP.text = "🔒 " + GetLocalizedText(card.right.lockedText, CardConstants.SuffixRight);
        else
            rightChoiceTMP.text = GetLocalizedText(card.right.choiceText, CardConstants.SuffixRight);

        // Sol seçenek
        if (isLeftLocked && !string.IsNullOrEmpty(card.left.lockedText))
            leftChoiceTMP.text = "🔒 " + GetLocalizedText(card.left.lockedText, CardConstants.SuffixLeft);
        else
            leftChoiceTMP.text = GetLocalizedText(card.left.choiceText, CardConstants.SuffixLeft);

        // Karakter görseli
        SetCharacterImage(card.characterImageName);

        // Preview metinlerini temizle (kart değişince eski preview'lar kaybolsun)
        if (rightPreviewText) rightPreviewText.text = "";
        if (leftPreviewText) leftPreviewText.text = "";
    }

    private string GetLocalizedText(string key, string suffix)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (!key.StartsWith("LOK_")) return key;

        if (LocalizationManager.Instance == null) return key;
        return key.Contains(CardConstants.PhaseIndicator) || key.EndsWith(suffix)
            ? LocalizationManager.Instance.GetLocalizedValue(key)
            : LocalizationManager.Instance.GetMasterValue(key, suffix);
    }

    private void SetCharacterImage(string imageName)
    {
        if (characterImage == null || string.IsNullOrEmpty(imageName)) return;

        Sprite sprite = Resources.Load<Sprite>(imageName);
        if (!sprite) sprite = Resources.Load<Sprite>("CrisisIcons/" + imageName);
        if (!sprite) sprite = Resources.Load<Sprite>("Characters/" + imageName);
        characterImage.sprite = sprite;
    }

    // ========================================================================
    //  ÖNİZLEME VE İNDİKATÖRLER (CardController sürüklerken)
    // ========================================================================
    private void UpdatePreviewAndIndicators(bool isRight, CardData card)
    {
        if (card == null)
        {
            // İndikatörleri kapat
            SetIndicatorsActive(false);
            if (rightPreviewText) rightPreviewText.text = "";
            if (leftPreviewText) leftPreviewText.text = "";
            return;
        }

        // İndikatörleri göster
        ShowIndicators(isRight, card);

        // Preview metnini oluştur
        string preview = BuildPreviewText(card, isRight);
        if (isRight && rightPreviewText) rightPreviewText.text = preview;
        else if (!isRight && leftPreviewText) leftPreviewText.text = preview;
    }

    private string BuildPreviewText(CardData card, bool isRight)
    {
        int dForm = isRight ? card.right.form : card.left.form;
        int dMoney = isRight ? card.right.money : card.left.money;
        int dFame = isRight ? card.right.fame : card.left.fame;
        int dRel = isRight ? card.right.relations : card.left.relations;
        int dFoc = isRight ? card.right.focus : card.left.focus;

        bool hasEffect = (dForm != 0 || dMoney != 0 || dFame != 0 || dRel != 0 || dFoc != 0);
        if (!hasEffect) return "";

        string text = "\n<color=yellow><size=70%>";
        if (dForm != 0) text += $"FRM: {(dForm > 0 ? "+" : "")}{dForm}  ";
        if (dMoney != 0) text += $"PAR: {(dMoney > 0 ? "+" : "")}{dMoney}  ";
        if (dFame != 0) text += $"ŞÖH: {(dFame > 0 ? "+" : "")}{dFame}  ";
        if (dRel != 0) text += $"İLŞ: {(dRel > 0 ? "+" : "")}{dRel}  ";
        if (dFoc != 0) text += $"ODK: {(dFoc > 0 ? "+" : "")}{dFoc}  ";
        text += "</size></color>";
        return text;
    }

    private void ShowIndicators(bool isRight, CardData card)
    {
        if (GameManager.Instance == null) return;

        bool showForm = isRight ? card.right.form != 0 : card.left.form != 0;
        bool showMoney = isRight ? card.right.money != 0 : card.left.money != 0;
        bool showFame = isRight ? card.right.fame != 0 : card.left.fame != 0;
        bool showRel = isRight ? card.right.relations != 0 : card.left.relations != 0;
        bool showFoc = isRight ? card.right.focus != 0 : card.left.focus != 0;

        SetGameObjectActive(GameManager.Instance.displayForm, showForm);
        SetGameObjectActive(GameManager.Instance.displayMoney, showMoney);
        SetGameObjectActive(GameManager.Instance.displayFame, showFame);
        SetGameObjectActive(GameManager.Instance.displayRelations, showRel);
        SetGameObjectActive(GameManager.Instance.displayFocus, showFoc);
    }

    private void SetIndicatorsActive(bool active)
    {
        if (GameManager.Instance == null) return;
        SetGameObjectActive(GameManager.Instance.displayForm, active);
        SetGameObjectActive(GameManager.Instance.displayMoney, active);
        SetGameObjectActive(GameManager.Instance.displayFame, active);
        SetGameObjectActive(GameManager.Instance.displayRelations, active);
        SetGameObjectActive(GameManager.Instance.displayFocus, active);
    }

    private void SetGameObjectActive(GameObject obj, bool active)
    {
        if (obj != null && obj.activeSelf != active) obj.SetActive(active);
    }

    // ========================================================================
    //  TEMA VE RENK DEĞİŞİMLERİ
    // ========================================================================
    public void ApplyTheme(ThemeType type, bool instant = false)
    {
        Color bg, panel, icon;
        switch (type)
        {
            case ThemeType.Crisis:
                bg = crisisBgColor; panel = crisisPanelColor; icon = crisisIconBgColor;
                break;
            case ThemeType.GameOver:
                bg = gameOverBgColor; panel = gameOverPanelColor; icon = gameOverIconBgColor;
                break;
            default:
                bg = normalBgColor; panel = normalPanelColor; icon = normalIconBgColor;
                break;
        }

        if (_themeCoroutine != null) StopCoroutine(_themeCoroutine);

        if (instant)
        {
            ApplyColors(bg, panel, icon);
        }
        else
        {
            _themeCoroutine = StartCoroutine(AnimateTheme(bg, panel, icon));
        }
    }

    private IEnumerator AnimateTheme(Color targetBg, Color targetPanel, Color targetIcon)
    {
        float duration = 0.5f;
        float time = 0;
        Color startBg = gameBackgroundPanel.color;
        Color startPanel = topBarPanel.color;
        Color startIcon = normalIconBgColor;

        while (time < duration)
        {
            float t = time / duration;
            gameBackgroundPanel.color = Color.Lerp(startBg, targetBg, t);
            topBarPanel.color = Color.Lerp(startPanel, targetPanel, t);
            bottomBarPanel.color = Color.Lerp(startPanel, targetPanel, t);
            SetAllIconsColor(Color.Lerp(startIcon, targetIcon, t));
            time += Time.deltaTime;
            yield return null;
        }
        ApplyColors(targetBg, targetPanel, targetIcon);
    }

    private void ApplyColors(Color bg, Color panel, Color icon)
    {
        gameBackgroundPanel.color = bg;
        topBarPanel.color = panel;
        bottomBarPanel.color = panel;
        SetAllIconsColor(icon);
    }

    private void SetAllIconsColor(Color color)
    {
        if (GameManager.Instance == null) return;
        SetIconColor(GameManager.Instance.displayForm, color);
        SetIconColor(GameManager.Instance.displayMoney, color);
        SetIconColor(GameManager.Instance.displayFame, color);
        SetIconColor(GameManager.Instance.displayRelations, color);
        SetIconColor(GameManager.Instance.displayFocus, color);
    }

    private void SetIconColor(GameObject obj, Color color)
    {
        if (obj != null && obj.TryGetComponent<ResourceDisplay>(out var rd))
            rd.SetBackgroundColor(color);
    }

    // ========================================================================
    //  KRİZ YANIP SÖNME (Blink)
    // ========================================================================
    public void ManageBlinkingDisplay(string statName, bool activate)
    {
        if (GameManager.Instance == null) return;

        if (activate)
        {
            GameObject target = statName switch
            {
                "Form" => GameManager.Instance.displayForm,
                "Money" => GameManager.Instance.displayMoney,
                "Fame" => GameManager.Instance.displayFame,
                "Relations" => GameManager.Instance.displayRelations,
                "Focus" => GameManager.Instance.displayFocus,
                _ => null
            };
            if (target != null && target.TryGetComponent<ResourceDisplay>(out var rd))
            {
                _blinkingDisplay = rd;
                rd.StartBlinking();
            }
        }
        else
        {
            if (_blinkingDisplay != null)
            {
                _blinkingDisplay.StopBlinking();
                _blinkingDisplay = null;
            }
        }
    }
}