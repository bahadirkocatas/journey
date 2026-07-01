using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Transfer paneli, teklif butonlarý ve UI etkileþimlerini yönetir. (Single Responsibility Principle)
/// </summary>
public class TransferUIManager : MonoBehaviour
{
    public static TransferUIManager Instance;

    [Header("UI Panelleri")]
    public GameObject transferPanel;
    public Transform transferContent;
    public GameObject transferButtonPrefab;

    private int lastPerformanceScore;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (transferPanel) transferPanel.SetActive(false);
    }

    public void ShowTransferOptions(int score, bool isOptional, int minTargetPower, int maxTargetPower)
    {
        if (transferPanel == null || TeamManager.Instance == null) return;

        transferPanel.SetActive(true);
        lastPerformanceScore = score;

        // Paneli Temizle
        foreach (Transform child in transferContent) Destroy(child.gameObject);

        List<TeamData> candidates = new List<TeamData>();
        string currentTeamID = TeamManager.Instance.currentTeam != null ? TeamManager.Instance.currentTeam.id : "";
        string currentNationID = StoryManager.Instance.GetString(GameKeys.FLAG_PLAYER_NATION);

        // 1. Yerel Takýmlar (Kendi Liginden)
        var locals = TeamManager.Instance.allTeams
            .Where(t => t.id != currentTeamID && t.countryID == currentNationID && t.power >= minTargetPower && t.power <= maxTargetPower)
            .OrderBy(x => Random.value).Take(2).ToList();
        candidates.AddRange(locals);

        // 2. Global Takýmlar (Baþarýlýysa)
        if (score >= 75)
        {
            var globals = TeamManager.Instance.allTeams
                .Where(t => t.id != currentTeamID && t.countryID != currentNationID && t.power >= minTargetPower && t.power <= maxTargetPower)
                .OrderBy(x => Random.value).Take(2).ToList();
            candidates.AddRange(globals);
        }

        // 3. Kalma Opsiyonu
        if (isOptional) CreateStayButton();

        // 4. Transfer Butonlarýný Üret
        foreach (var team in candidates) CreateTransferButton(team);
    }

    private void CreateTransferButton(TeamData team)
    {
        GameObject btnObj = Instantiate(transferButtonPrefab, transferContent);
        Color pri = Color.white; Color sec = Color.black;

        if (!string.IsNullOrEmpty(team.primaryColor)) ColorUtility.TryParseHtmlString(team.primaryColor, out pri);
        if (!string.IsNullOrEmpty(team.secondaryColor)) ColorUtility.TryParseHtmlString(team.secondaryColor, out sec);

        btnObj.GetComponent<Image>().color = sec;
        btnObj.GetComponentsInChildren<Image>().First(i => i.gameObject != btnObj).color = pri;

        TextMeshProUGUI[] texts = btnObj.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length > 0) { texts[0].text = team.name.ToUpper(); texts[0].color = sec; }
        if (texts.Length > 1)
        {
            int years = SeasonManager.Instance.CalculateContractDuration(SeasonManager.Instance.currentYear);
            texts[1].text = $"{team.budget}M€ | {years} YIL";
            texts[1].color = sec;
        }

        btnObj.GetComponent<Button>().onClick.AddListener(() => SignContract(team));
    }

    private void CreateStayButton()
    {
        GameObject btnObj = Instantiate(transferButtonPrefab, transferContent);
        btnObj.GetComponent<Image>().color = Color.gray;

        TextMeshProUGUI[] texts = btnObj.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length > 0) texts[0].text = "TAKIMDA KAL";
        if (texts.Length > 1) texts[1].text = "Sözleþme Yenile";

        btnObj.GetComponent<Button>().onClick.AddListener(() => SignContract(null));
    }

    private void SignContract(TeamData newTeam)
    {
        if (transferPanel) transferPanel.SetActive(false);

        // Sezon yöneticisine imzanýn atýldýðýný bildirir
        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.OnContractSigned(newTeam);
        }
    }
}