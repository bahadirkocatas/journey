using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Linq;

/// <summary>
/// Oyun sonu kariyer özeti ekranı. MVP sürümü: Award ve Trait sistemi devre dışı.
/// </summary>
public class CareerSummaryUI : MonoBehaviour
{
    [Header("Ana Panel")]
    public GameObject summaryPanel;

    [Header("Sayfalar (GameObjects)")]
    public GameObject page1_Headline;
    public GameObject page2_Stats;
    public GameObject page3_Legacy;

    [Header("Navigasyon")]
    public Button nextButton;
    public Button prevButton;
    public Button mainMenuButton;
    public TextMeshProUGUI pageNumberText;

    [Header("Sayfa 1: Manşet")]
    public TextMeshProUGUI p1_TitleText;
    public TextMeshProUGUI p1_ScoreText;
    public TextMeshProUGUI p1_IntroText;
    public Image p1_PlayerPhoto;

    [Header("Sayfa 2: İstatistikler")]
    public TextMeshProUGUI p2_SeasonsListText;
    public TextMeshProUGUI p2_HighlightsText;

    [Header("Sayfa 3: Miras")]
    public TextMeshProUGUI p3_TrophiesText;
    public TextMeshProUGUI p3_StoryText;

    private int currentPage = 1;
    private int maxPages = 3;

    // ========================================================================
    //  BAŞLANGIÇ
    // ========================================================================
    private void Start()
    {
        if (summaryPanel) summaryPanel.SetActive(false);

        if (nextButton) nextButton.onClick.AddListener(NextPage);
        if (prevButton) prevButton.onClick.AddListener(PrevPage);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    // ========================================================================
    //  GÖSTERME
    // ========================================================================
    public void ShowSummary()
    {
        if (summaryPanel) summaryPanel.SetActive(true);

        SetupPage1();
        SetupPage2();
        SetupPage3();

        ShowPage(1);
    }

    // ========================================================================
    //  SAYFA GEÇİŞLERİ
    // ========================================================================
    private void ShowPage(int pageIndex)
    {
        currentPage = pageIndex;

        if (page1_Headline) page1_Headline.SetActive(currentPage == 1);
        if (page2_Stats) page2_Stats.SetActive(currentPage == 2);
        if (page3_Legacy) page3_Legacy.SetActive(currentPage == 3);

        if (prevButton) prevButton.gameObject.SetActive(currentPage > 1);
        if (nextButton) nextButton.gameObject.SetActive(currentPage < maxPages);
        if (mainMenuButton) mainMenuButton.gameObject.SetActive(currentPage == maxPages);

        if (pageNumberText) pageNumberText.text = $"SAYFA {currentPage}/{maxPages}";
    }

    public void NextPage()
    {
        if (currentPage < maxPages) ShowPage(currentPage + 1);
    }

    public void PrevPage()
    {
        if (currentPage > 1) ShowPage(currentPage - 1);
    }

    private void ReturnToMainMenu()
    {
        // Ana menüye dön (SceneManager yüklenebilir)
        // SceneManager.LoadScene("MainMenu");
        Debug.Log("[CareerSummaryUI] Ana menüye dönüş tetiklendi.");
    }

    // ========================================================================
    //  SAYFA 1: MANŞET
    // ========================================================================
    private void SetupPage1()
    {
        int finalScore = 0;
        if (StatsManager.Instance != null)
        {
            finalScore = StatsManager.Instance.CalculateCareerScore();
            p1_ScoreText.text = $"KARİYER PUANI: {finalScore}";
        }

        // Başlık – sadece puana dayalı (Trait/Origin yok)
        string title = "EMEKLİ OYUNCU";
        if (finalScore >= 2000) title = "DÜNYA EFSANESİ";
        else if (finalScore >= 1200) title = "KÜRESEL YILDIZ";
        else if (finalScore >= 600) title = "GÜVENİLİR PROFESYONEL";
        else if (finalScore >= 300) title = "ROTASYON OYUNCUSU";
        else title = "HAYAL KIRIKLIĞI";

        p1_TitleText.text = title;
        p1_IntroText.text = "Futbol dünyası şokta. İyisiyle kötüsüyle kariyerin sona erdi...";
    }

    // ========================================================================
    //  SAYFA 2: İSTATİSTİKLER
    // ========================================================================
    private void SetupPage2()
    {
        if (StatsManager.Instance == null) return;

        var history = StatsManager.Instance.careerHistory;
        if (history == null || history.Count == 0)
        {
            p2_SeasonsListText.text = "Henüz sezon kaydı yok.";
            p2_HighlightsText.text = "";
            return;
        }

        // Sezon listesi
        var sbSeasons = new StringBuilder();
        foreach (var h in history)
        {
            int seasonNumber = h.age - 18 + 1;
            sbSeasons.AppendLine($"<b>SEZON {seasonNumber} ({h.age} Yaş):</b> {h.teamName}");
            sbSeasons.AppendLine($"   <color=#555>{h.goals} Gol - {h.assists} Asist | Ort: {h.rating:F1}</color>");
            sbSeasons.AppendLine("-----------------------------------");
        }
        p2_SeasonsListText.text = sbSeasons.ToString();

        // Özet istatistikler
        var sbHigh = new StringBuilder();

        // En iyi sezon (gol+asist toplamına göre)
        var bestSeason = history.OrderByDescending(x => x.goals + x.assists).FirstOrDefault();
        if (bestSeason != null)
            sbHigh.AppendLine($"🔥 <b>PRIME DÖNEMİ:</b> {bestSeason.age} Yaşında ({bestSeason.goals}G {bestSeason.assists}A)");

        // En çok oynanan takım
        var favTeam = history.GroupBy(x => x.teamName)
                             .OrderByDescending(g => g.Count())
                             .FirstOrDefault();
        if (favTeam != null)
            sbHigh.AppendLine($"🏠 <b>EVİ:</b> {favTeam.Key} ({favTeam.Count()} Sezon)");

        // En çok gol atılan takım
        var goalTeam = history.GroupBy(x => x.teamName)
                              .OrderByDescending(g => g.Sum(s => s.goals))
                              .FirstOrDefault();
        if (goalTeam != null)
            sbHigh.AppendLine($"⚽ <b>EN ÇOK GOL:</b> {goalTeam.Key} ({goalTeam.Sum(s => s.goals)} Gol)");

        p2_HighlightsText.text = sbHigh.ToString();
    }

    // ========================================================================
    //  SAYFA 3: MİRAS (Award YOK, SADECE HİKAYE BAYRAKLARI)
    // ========================================================================
    private void SetupPage3()
    {
        // Trophy kısmı – MVP'de award olmadığı için boş bırakıyoruz veya mesaj gösteriyoruz
        p3_TrophiesText.text = "<i>MVP sürümünde ödül sistemi devre dışı.</i>";

        // Hikaye kısmı – sadece StoryManager bayrakları
        var sbStory = new StringBuilder();
        sbStory.AppendLine("<b>--- HİKAYE ---</b>");

        if (StatsManager.Instance != null)
        {
            sbStory.AppendLine($"Kariyerin boyunca toplam {StatsManager.Instance.matchesPlayed} maça çıktın.");
            sbStory.AppendLine($"Ağları {StatsManager.Instance.totalGoals} kez sarstın ve {StatsManager.Instance.totalAssists} asist yaptın.");
        }

        // StoryManager bayrakları (varsa)
        if (StoryManager.Instance != null)
        {
            if (StoryManager.Instance.HasFlag("mafya_dostu"))
                sbStory.AppendLine("- <color=red>Kariyerin boyunca yeraltı dünyasıyla ilişkin çok konuşuldu.</color>");
            if (StoryManager.Instance.HasFlag("alex_dost"))
                sbStory.AppendLine("- En büyük rakibinle sahanın dışında dostluk kurdun.");
            if (StoryManager.Instance.HasFlag("media_darling"))
                sbStory.AppendLine("- Magazin basınının ve kameraların her zaman sevgilisi oldun.");
            if (StoryManager.Instance.HasFlag("fan_hero"))
                sbStory.AppendLine("- Taraftarlar seni efsane olarak hatırlayacak.");
        }

        p3_StoryText.text = sbStory.ToString();
    }
}