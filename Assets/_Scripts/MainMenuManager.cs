using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Game.Data;

/// <summary>
/// Ana Men� Y�neticisi. 
/// MVP M�MAR�S�: Hi�bir bekleme yapmaz, di�er y�neticilerin veriyi Awake'te y�kledi�ini varsayar ve direkt �al���r.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Ana Paneller")]
    public GameObject mainPanel;
    public GameObject gameUIPanel;

    [Header("Sihirbaz Ad�mlar�")]
    public GameObject step1_Identity;
    public GameObject step3_Contract;

    [Header("Ana Men� Butonlar�")]
    public Button continueButton;
    public Button newCareerButton;
    public Button quitButton;

    [Header("--- ADIM 1: K�ML�K ---")]
    public TMP_InputField nameInput;
    public Button openNationSelectorButton;
    public TextMeshProUGUI btnNationText;
    public Button step1NextButton;

    [Header("Uyruk Se�im Paneli")]
    public GameObject nationalitySelectionPanel;
    public Transform nationalityContent;
    public GameObject nationButtonPrefab;

    [Header("--- ADIM 2: KONTRAT ---")]
    public Transform contractContent;
    public GameObject contractButtonPrefab;

    // --- VER� HAFIZASI ---
    private string selectedNationID = "";
    private List<string> nations = new List<string>();
    private List<string> nationIDs = new List<string>();

    private void Start()
    {
        // B�Y�K REFACTOR: Bekleme (WaitUntil) yamas�n� S�LD�K! 
        // Data Manager'lar (Team, Country) art�k i�ini Awake'te bitirdi�i i�in buras� �im�ek gibi akar.
        InitializeMenu();
    }

    private void InitializeMenu()
    {
        LoadCountryData();
        ShowMainPanel();

        // Buton Dinleyicileri (Listeners)
        if (newCareerButton) newCareerButton.onClick.AddListener(StartCareerWizard);
        if (quitButton) quitButton.onClick.AddListener(Application.Quit);

        if (openNationSelectorButton) openNationSelectorButton.onClick.AddListener(OpenNationSelector);
        if (step1NextButton) step1NextButton.onClick.AddListener(FinishStep1);

        GenerateNationButtons();
        UpdateNationText();

        // Kay�t Kontrol�
        if (SaveManager.Instance != null && SaveManager.Instance.HasSaveFile())
        {
            if (continueButton)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.onClick.AddListener(ContinueCareer);
            }
        }
        else
        {
            if (continueButton) continueButton.gameObject.SetActive(false);
        }

        // Yeni Kariyer (Kay�t Silme ��lemi)
        newCareerButton.onClick.AddListener(() => {
            if (SaveManager.Instance != null) SaveManager.Instance.DeleteSave();
            StartCareerWizard();
        });
    }

    private void LoadCountryData()
    {
        nations.Clear();
        nationIDs.Clear();

        if (CountryManager.Instance != null && CountryManager.Instance.playableCountries != null)
        {
            foreach (var country in CountryManager.Instance.playableCountries)
            {
                nations.Add(country.name);
                nationIDs.Add(country.id);
            }

            if (nationIDs.Count > 0)
            {
                selectedNationID = nationIDs[0];
            }
        }
    }

    // ========================================================================
    // ARAY�Z VE PANEL GE���LER�
    // ========================================================================
    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        step1_Identity.SetActive(false);
        step3_Contract.SetActive(false);
        if (gameUIPanel) gameUIPanel.SetActive(false);
        CloseNationSelector();
    }

    public void StartCareerWizard()
    {
        mainPanel.SetActive(false);
        ShowStep1();
    }

    // ========================================================================
    // ADIM 1: K�ML�K (�S�M VE �LKE SE��M�)
    // ========================================================================
    public void ShowStep1()
    {
        step1_Identity.SetActive(true);
        step3_Contract.SetActive(false);
        UpdateNationText();
    }

    private void UpdateNationText()
    {
        if (nations.Count == 0 || btnNationText == null) return;

        int index = nationIDs.IndexOf(selectedNationID);
        if (index < 0) index = 0;

        btnNationText.text = nations[index].ToUpper();
    }

    public void OpenNationSelector() { if (nationalitySelectionPanel) nationalitySelectionPanel.SetActive(true); }
    public void CloseNationSelector() { if (nationalitySelectionPanel) nationalitySelectionPanel.SetActive(false); }

    private void OnNationSelected(int index)
    {
        if (index < 0 || index >= nationIDs.Count) return;

        selectedNationID = nationIDs[index];
        UpdateNationText();
        CloseNationSelector();
    }

    private void GenerateNationButtons()
    {
        if (!nationalityContent || !nationButtonPrefab) return;

        // Content i�ini temizle
        foreach (Transform child in nationalityContent) Destroy(child.gameObject);

        for (int i = 0; i < nations.Count; i++)
        {
            // Prefab yarat, zorla aktif et ve scale de�erini 1 yap
            GameObject btnObj = Instantiate(nationButtonPrefab, nationalityContent);
            btnObj.SetActive(true);
            btnObj.transform.localScale = Vector3.one;

            // Metni Ayarla
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText) btnText.text = nations[i];

            // Bayrak �konunu Ayarla
            Image[] images = btnObj.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img.gameObject != btnObj && (img.name.Contains("Icon") || img.transform.parent == btnObj.transform))
                {
                    Sprite flagSprite = Resources.Load<Sprite>("Flags/" + nationIDs[i]);
                    if (flagSprite != null) img.sprite = flagSprite;
                }
            }

            // T�klama Olay�n� Ba�la
            int capturedIndex = i;
            Button btn = btnObj.GetComponent<Button>() ?? btnObj.GetComponentInChildren<Button>();

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnNationSelected(capturedIndex));
            }
        }
    }

    public void FinishStep1()
    {
        if (nameInput == null || string.IsNullOrEmpty(nameInput.text)) return;
        ShowStep3();
    }

    // ========================================================================
    // ADIM 2: KONTRAT SE��M�
    // ========================================================================
    public void ShowStep3()
    {
        step1_Identity.SetActive(false);
        step3_Contract.SetActive(true);
        SetupContractButtons();
    }

    private void SetupContractButtons()
    {
        if (contractContent == null || contractButtonPrefab == null) return;

        foreach (Transform child in contractContent) Destroy(child.gameObject);

        if (TeamManager.Instance == null || TeamManager.Instance.allTeams == null) return;

        // F�LTRE: Sadece se�ilen �lkenin 3. seviye tak�mlar�
        List<TeamData> validTeams = TeamManager.Instance.allTeams
            .Where(t => t != null && t.countryID == selectedNationID && t.tier == 3)
            .OrderBy(x => Random.value)
            .Take(3)
            .ToList();

        foreach (var team in validTeams)
        {
            GameObject btnObj = Instantiate(contractButtonPrefab, contractContent);
            btnObj.SetActive(true);
            btnObj.transform.localScale = Vector3.one;

            Color primaryCol = Color.white;
            Color secondaryCol = Color.black;

            if (!string.IsNullOrEmpty(team.primaryColor)) ColorUtility.TryParseHtmlString(team.primaryColor, out primaryCol);
            if (!string.IsNullOrEmpty(team.secondaryColor)) ColorUtility.TryParseHtmlString(team.secondaryColor, out secondaryCol);

            // Renklendirme
            Image outlineImage = btnObj.GetComponent<Image>();
            if (outlineImage != null) outlineImage.color = secondaryCol;

            Image[] childImages = btnObj.GetComponentsInChildren<Image>();
            foreach (var img in childImages)
            {
                if (img.gameObject != btnObj) img.color = primaryCol;
            }

            // Metinler
            TextMeshProUGUI[] texts = btnObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0)
            {
                texts[0].text = team.name.ToUpper();
                texts[0].color = secondaryCol;
            }
            if (texts.Length > 1)
            {
                texts[1].text = $"{team.budget}M�";
                texts[1].color = secondaryCol;
            }

            // T�klama Ba�lant�s�
            string capturedTeamID = team.id;
            Button btn = btnObj.GetComponent<Button>() ?? btnObj.GetComponentInChildren<Button>();

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnContractSigned(capturedTeamID));
            }
        }
    }

    private void OnContractSigned(string teamID)
    {
        string playerName = nameInput.text;

        if (TeamManager.Instance != null) TeamManager.Instance.SetPlayerTeam(teamID);
        if (GameManager.Instance != null) GameManager.Instance.StartCareer(playerName);
        if (StoryManager.Instance != null) StoryManager.Instance.SetString("Player_Nation", selectedNationID);

        step3_Contract.SetActive(false);
        if (gameUIPanel) gameUIPanel.SetActive(true);

        if (SeasonManager.Instance != null) SeasonManager.Instance.StartNewCareer();
    }

    // ========================================================================
    // OYUN KAYIT ��LEMLER�
    // ========================================================================
    public void ContinueCareer()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.LoadGame())
        {
            mainPanel.SetActive(false);
            if (gameUIPanel) gameUIPanel.SetActive(true);

            if (SeasonManager.Instance != null) SeasonManager.Instance.ResumeCareer();
        }
    }
}