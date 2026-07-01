using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Oyun içi profil ve istatistik tablosunun gösterildiði bilgi paneli.
/// TAM MVP SÜRÜMÜ: Trait/Özellik sekmeleri, baðlantýlarý ve arayüz mantýðý tamamen SÝLÝNDÝ. Sadece düz istatistik gösterir.
/// </summary>
public class DashboardManager : MonoBehaviour
{
    [Header("Ana Panel")]
    public GameObject dashboardPanel;
    private RectTransform dashboardRect;
    public float animationDuration = 0.5f;

    [Header("--- PROFÝL BÝLGÝLERÝ ---")]
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtTeam;
    public TextMeshProUGUI txtAgeSeason;
    public TextMeshProUGUI txtMarketValue;

    [Header("--- ÝSTATÝSTÝK TABLOSU ---")]
    public Transform statsContent;
    public GameObject statsRowPrefab;

    [Header("Sistem Butonlarý")]
    public Button openButton;
    public Button closeButton;

    void Start()
    {
        if (dashboardPanel != null) dashboardRect = dashboardPanel.GetComponent<RectTransform>();

        if (dashboardPanel) dashboardPanel.SetActive(false);

        if (openButton) openButton.onClick.AddListener(OpenDashboard);
        if (closeButton) closeButton.onClick.AddListener(CloseDashboard);
    }

    public void OpenDashboard()
    {
        if (dashboardPanel == null || dashboardRect == null) return;

        dashboardPanel.SetActive(true);
        UpdateProfileHeader();
        UpdateStatsTable();

        StopAllCoroutines();
        StartCoroutine(AnimatePanel(new Vector2(0, -Screen.height), Vector2.zero, true));
    }

    public void CloseDashboard()
    {
        if (dashboardPanel == null || dashboardRect == null) return;

        Time.timeScale = 1;

        StopAllCoroutines();
        StartCoroutine(AnimatePanel(Vector2.zero, new Vector2(0, -Screen.height), false));
    }

    IEnumerator AnimatePanel(Vector2 startPos, Vector2 endPos, bool freezeGameAtEnd)
    {
        float elapsed = 0f;
        dashboardRect.anchoredPosition = startPos;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;
            t = t * t * (3f - 2f * t);

            dashboardRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        dashboardRect.anchoredPosition = endPos;

        if (freezeGameAtEnd) Time.timeScale = 0;
        else dashboardPanel.SetActive(false);
    }

    void UpdateProfileHeader()
    {
        if (GameManager.Instance == null) return;

        txtName.text = GameManager.Instance.playerName.ToUpper();

        if (TeamManager.Instance != null && TeamManager.Instance.currentTeam != null)
            txtTeam.text = TeamManager.Instance.currentTeam.name.ToUpper();

        if (SeasonManager.Instance != null)
        {
            int age = SeasonManager.Instance.currentYear;
            int seasonCount = age - 18 + 1;
            txtAgeSeason.text = $"{age} YAÞ  |  {seasonCount}. SEZON";
        }

        if (StatsManager.Instance != null)
        {
            txtMarketValue.text = $"DEÐER: {StatsManager.Instance.marketValue}M€";
        }
    }

    void UpdateStatsTable()
    {
        foreach (Transform child in statsContent) Destroy(child.gameObject);

        if (StatsManager.Instance == null) return;

        // 1. GEÇMÝÞ SEZONLARI LÝSTELE
        foreach (var history in StatsManager.Instance.careerHistory)
        {
            CreateStatRow(
                history.age.ToString(),
                history.teamName,
                history.goals.ToString(),
                history.assists.ToString(),
                history.rating.ToString("F1")
            );
        }

        // 2. GÜNCEL SEZONU (CANLI) LÝSTELE
        string currentTeamName = "KULÜPSÜZ";
        if (TeamManager.Instance != null && TeamManager.Instance.currentTeam != null)
            currentTeamName = TeamManager.Instance.currentTeam.name;

        CreateStatRow(
            SeasonManager.Instance.currentYear.ToString() + " (ÞU AN)",
            currentTeamName,
            StatsManager.Instance.seasonGoals.ToString(),
            StatsManager.Instance.seasonAssists.ToString(),
            StatsManager.Instance.averageRating.ToString("F1"),
            true
        );
    }

    void CreateStatRow(string col1, string col2, string col3, string col4, string col5, bool isCurrent = false)
    {
        GameObject row = Instantiate(statsRowPrefab, statsContent);

        TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();

        if (texts.Length >= 5)
        {
            texts[0].text = col1;
            texts[1].text = col2;
            texts[2].text = col3;
            texts[3].text = col4;
            texts[4].text = col5;

            if (isCurrent)
            {
                foreach (var txt in texts) txt.color = new Color(0.2f, 1f, 0.4f); // Neon Yeþil
            }
        }
    }
}