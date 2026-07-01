using UnityEngine;
using System.Collections.Generic;
using Game.Data;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;

    [Header("Simülasyon Ayarları")]
    [SerializeField] private MatchSimulationConfig simulationConfig;

    // Servisler
    private MatchSimulator _matchSimulator;

    // --- VERİ DEPOSU ---
    [Header("Sezonluk İstatistikler")]
    public int seasonGoals;
    public int seasonAssists;
    public float averageRating;
    public int matchesPlayed;

    [Header("Kariyer Toplamları")]
    public int totalGoals;
    public int totalAssists;
    public List<SeasonHistoryData> careerHistory = new List<SeasonHistoryData>();

    [Header("Hesaplanan Değerler")]
    public int marketValue;
    public float seasonPerformanceScore;
    private float totalRatingSum;

    // ❌ earnedAwards kaldırıldı.
    // ❌ awardsJsonFile kaldırıldı.
    // ❌ allAwards kaldırıldı.

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (simulationConfig != null)
            _matchSimulator = new MatchSimulator(simulationConfig);
        else
            Debug.LogError("[StatsManager] MatchSimulationConfig atanmamış!");

        ResetSeasonStats();
    }

    private void OnEnable()
    {
        GameEvents.OnCardResolved += HandleCardResolved;
        GameEvents.OnWeekAdvanced += HandleWeekAdvanced;
        GameEvents.OnSeasonEnded += HandleSeasonEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnCardResolved -= HandleCardResolved;
        GameEvents.OnWeekAdvanced -= HandleWeekAdvanced;
        GameEvents.OnSeasonEnded -= HandleSeasonEnded;
    }

    // ========================================================================
    //  EVENT YÖNETİCİLERİ
    // ========================================================================
    private void HandleCardResolved(CardData card, bool isRight)
    {
        if (card == null) return;
        var effect = isRight ? card.right.statEffect : card.left.statEffect;
        if (effect != null && (effect.goals > 0 || effect.assists > 0 || effect.matchRating > 0))
        {
            AddMatchStats(effect.goals, effect.assists, effect.matchRating);
        }
    }

    private void HandleWeekAdvanced(int currentWeek)
    {
        SimulateWeeklyMatch();
    }

    private void HandleSeasonEnded(int score)
    {
        EndSeasonCalculation();
        ArchiveCurrentSeason();
    }

    // ========================================================================
    //  ⚽ MAÇ SİMÜLASYONU
    // ========================================================================
    public void SimulateWeeklyMatch()
    {
        if (_matchSimulator == null || TeamManager.Instance == null || GameManager.Instance == null)
            return;

        int activePower = TeamManager.Instance.GetActivePower();
        int form = GameManager.Instance.form;
        int focus = GameManager.Instance.focus;
        int fame = GameManager.Instance.fame;
        int relations = GameManager.Instance.relations;

        var (goals, assists, rating) = _matchSimulator.SimulateMatch(
            activePower, form, focus, fame, relations
        );

        AddMatchStats(goals, assists, rating);
    }

    // ========================================================================
    //  📊 VERİ EKLEME VE YÖNETİM
    // ========================================================================
    public void AddMatchStats(int goals, int assists, float rating)
    {
        matchesPlayed++;
        seasonGoals += goals;
        seasonAssists += assists;

        totalRatingSum += rating;
        averageRating = totalRatingSum / matchesPlayed;

        totalGoals += goals;
        totalAssists += assists;

        CalculatePerformance();
    }

    private void CalculatePerformance()
    {
        float rawScore = (averageRating * 10f) + (seasonGoals * 2f) + (seasonAssists * 1f);
        seasonPerformanceScore = Mathf.Clamp(rawScore, 0, 100);

        if (GameManager.Instance != null)
            marketValue = (int)(seasonPerformanceScore * 1.5f) + GameManager.Instance.fame;
    }

    // ========================================================================
    //  🏁 SEZON SONU İŞLEMLERİ (Ödül yok, sadece arşiv)
    // ========================================================================
    public void EndSeasonCalculation()
    {
        CalculatePerformance();

        // ❌ CheckForAwards() kaldırıldı.
        // Sadece bayrak temizliği varsa onu yap
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.RemoveFlag("league_won_this_season");
            StoryManager.Instance.RemoveFlag("ucl_won_this_season");
        }
    }

    public void ArchiveCurrentSeason()
    {
        var history = new SeasonHistoryData
        {
            age = SeasonManager.Instance?.currentYear ?? 18,
            teamName = TeamManager.Instance?.currentTeam?.name ?? "Kulüpsüz",
            goals = seasonGoals,
            assists = seasonAssists,
            rating = averageRating
        };
        careerHistory.Add(history);
    }

    // ========================================================================
    //  🧹 TEMİZLİK
    // ========================================================================
    public void ResetSeasonStats()
    {
        seasonGoals = 0;
        seasonAssists = 0;
        matchesPlayed = 0;
        totalRatingSum = 0;
        averageRating = 6.0f;
        seasonPerformanceScore = 50;
    }

    // ========================================================================
    //  📈 KARİYER SKORU (Ödül yok, sadece gol+asist+fame)
    // ========================================================================
    public int CalculateCareerScore()
    {
        int fame = GameManager.Instance?.fame ?? 0;
        return (totalGoals * 2) + (totalAssists * 1) + (fame * 2);
        // ❌ earnedAwards çarpanı kaldırıldı.
    }
}