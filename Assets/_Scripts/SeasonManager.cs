using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Data;

/// <summary>
/// İnce zaman koordinatörü: takvimi ilerletir, sezon sınırını algılar, deste kurar ve
/// sezon-sonu/transfer akışını orkestre eder. UCL akışının (grup kartları, final tetikleme/
/// zincir) sahipliği <see cref="UclFlowController"/>'a, sezon-sonu skor matematiği
/// <see cref="SeasonEndEvaluator"/>'a devredildi (bkz. REFACTOR_PLAN.md madde 13.2) — bu
/// dosya artık yalnızca ZAMAN akışından ve bu iki servisin sonuçlarını uygulamaktan
/// sorumlu.
/// </summary>
public class SeasonManager : Singleton<SeasonManager>
{
    [Header("Zaman Motoru")]
    [SerializeField] private SeasonCalendar calendar = new SeasonCalendar();
    public int CalculateContractDuration(int age) => calendar.CalculateContractDuration(age);

    [Header("Denge Ayarları (Balancing)")]
    [SerializeField] private int scoreForOptionalTransfer = 85;
    [SerializeField] private int scoreForGoodReview = 80;
    [SerializeField] private int scoreForBadReview = 30;
    [SerializeField] private int scoreForUCLQualification = 90;
    [SerializeField] private int scoreForRelegation = 40;

    [Header("Veri Dosyaları")]
    public TextAsset scheduleJsonFile;
    private List<SeasonSchedule> allSchedules;

    [Header("UI")]
    public TextMeshProUGUI textYearInfo;

    // --- Geriye Dönük Uyumluluk (Facade) ---
    public int currentYear { get => calendar.currentYear; set => calendar.currentYear = value; }
    public int currentWeek { get => calendar.currentWeek; set => calendar.currentWeek = value; }
    public int contractYearsLeft { get => calendar.contractYearsLeft; set => calendar.contractYearsLeft = value; }
    public int weeksPerSeason => calendar.weeksPerSeason;

    protected override void OnSingletonAwake()
    {
        LoadSchedule();
    }

    private void Start()
    {
        calendar.UpdateUI(textYearInfo);
    }

    private void OnEnable()
    {
        GameEvents.OnCardFlowEnded += AdvanceWeek;
    }

    private void OnDisable()
    {
        GameEvents.OnCardFlowEnded -= AdvanceWeek;
    }

    public void StartNewCareer()
    {
        calendar.Initialize();
        contractYearsLeft = calendar.CalculateContractDuration(currentYear);
        StartNewSeasonRoutine();
        calendar.UpdateUI(textYearInfo);
        CardManager.Instance?.LoadNewCard();
    }

    public void ResumeCareer()
    {
        StartNewSeasonRoutine();
        calendar.UpdateUI(textYearInfo);
        CardManager.Instance?.LoadNewCard();
    }

    private void LoadSchedule()
    {
        if (scheduleJsonFile != null)
        {
            var data = JsonUtility.FromJson<ScheduleCollection>(scheduleJsonFile.text);
            allSchedules = data?.schedules;
        }
    }

    // ========================================================================
    //  ZAMAN İLERLEME VE AKIŞ
    // ========================================================================
    public void AdvanceWeek()
    {
        // UCL finali sırasında takvim donar — sadece final zincirinin bir sonraki
        // kartını yükle. UCL durumunun tek doğru kaynağı UclFlowController'dır.
        if (UclFlowController.Instance != null && UclFlowController.Instance.IsFinalRunning)
        {
            CardManager.Instance?.LoadNewCard();
            return;
        }

        if (!calendar.IsSeasonEnd())
        {
            calendar.AdvanceWeek();
            GameEvents.OnWeekAdvanced?.Invoke(currentWeek);
        }

        calendar.UpdateUI(textYearInfo);

        if (calendar.IsSeasonEnd())
        {
            HandleSeasonEnd();
            return;
        }

        CardManager.Instance?.LoadNewCard();
        SaveManager.Instance?.SaveGame();
    }

    /// <summary>
    /// Sezon sınırına ulaşıldı. Önce UCL'e (final tetikleme/eleme/kalan kuyruk) söz hakkı
    /// verilir; UCL bu haftaki işi bitirmediğini söylerse (<c>true</c> döner) sezon sonu
    /// ertelenir, aksi halde gerçekten sona erer.
    /// </summary>
    private void HandleSeasonEnd()
    {
        bool deferred = UclFlowController.Instance != null && UclFlowController.Instance.TryHandleSeasonEnd();
        if (!deferred) EndSeason();
    }

    private void StartNewSeasonRoutine()
    {
        if (CardManager.Instance == null) return;

        int seasonIndex = currentYear - calendar.startingAge + 1;
        CardManager.Instance.BuildSeasonDeck(weeksPerSeason);

        UclFlowController.Instance?.InjectGroupStageCardsIfQualified();

        // Hikaye Kartları
        if (allSchedules != null)
        {
            var schedule = allSchedules.FirstOrDefault(s => s.seasonNumber == seasonIndex);
            if (schedule?.storyStarterIDs != null)
            {
                var storyCards = new List<CardData>();
                foreach (string id in schedule.storyStarterIDs)
                {
                    var c = CardManager.Instance.GetCardByID(id);
                    if (c != null) storyCards.Add(c);
                }
                CardManager.Instance.InjectMilestoneCards(storyCards, false);
            }
        }
    }

    // ========================================================================
    //  SEZON SONU VE TRANSFER
    // ========================================================================
    private void EndSeason()
    {
        int score = (StatsManager.Instance != null) ? (int)StatsManager.Instance.seasonPerformanceScore : 50;
        GameEvents.OnSeasonEnded?.Invoke(score);

        if (TeamManager.Instance != null)
        {
            int activePower = TeamManager.Instance.GetActivePower();
            int seasonScore = activePower + ((StatsManager.Instance?.seasonGoals ?? 0) + (StatsManager.Instance?.seasonAssists ?? 0)) / 2;
            int activeTier = TeamManager.Instance.GetActiveTier();
            int basePower = TeamManager.Instance.currentTeam != null ? TeamManager.Instance.currentTeam.power : 50;

            var uclResult = SeasonEndEvaluator.EvaluateUclAndRelegation(
                seasonScore, activeTier, basePower, scoreForUCLQualification, scoreForRelegation);
            ApplyUclAndRelegationResult(uclResult);
        }

        calendar.AdvanceYear();
        CheckPhaseProgression();
        StartNewSeasonRoutine();

        if (contractYearsLeft <= 0)
            TriggerTransferPhase(score, false);
        else if (score >= scoreForOptionalTransfer)
            TriggerTransferPhase(score, true);
        else
        {
            var reviewResult = SeasonEndEvaluator.EvaluateReview(score, scoreForGoodReview, scoreForBadReview);
            ApplyReviewResult(reviewResult);
            StatsManager.Instance?.ResetSeasonStats();
            calendar.UpdateUI(textYearInfo);
            CardManager.Instance?.LoadNewCard();
        }
    }

    /// <summary>SeasonEndEvaluator'ın saf sonucunu StoryManager üzerinde uygular (yan etki burada).</summary>
    private void ApplyUclAndRelegationResult(SeasonEndEvaluator.UclAndRelegationResult result)
    {
        if (StoryManager.Instance == null) return;

        if (result.QualifiedForUcl.HasValue)
        {
            if (result.QualifiedForUcl.Value)
                StoryManager.Instance.AddFlag(GameConstants.Flags.UclQualified);
            else
                StoryManager.Instance.RemoveFlag(GameConstants.Flags.UclQualified);
        }

        if (result.TeamTierModDelta != 0)
            StoryManager.Instance.ModifyVariable(GameConstants.Vars.TeamTierMod, result.TeamTierModDelta);

        if (result.TeamPowerModValue.HasValue)
            StoryManager.Instance.SetVariable(GameConstants.Vars.TeamPowerMod, result.TeamPowerModValue.Value);
    }

    /// <summary>SeasonEndEvaluator'ın saf sonucunu GameManager üzerinde uygular (yan etki burada).</summary>
    private void ApplyReviewResult(SeasonEndEvaluator.ReviewResult result)
    {
        if (GameManager.Instance == null) return;

        if (result.MoneyDelta != 0) GameManager.Instance.ApplyStatChange(StatType.Money, result.MoneyDelta);
        if (result.FameDelta != 0) GameManager.Instance.ApplyStatChange(StatType.Fame, result.FameDelta);
        if (result.RelationsDelta != 0) GameManager.Instance.ApplyStatChange(StatType.Relations, result.RelationsDelta);
    }

    private void TriggerTransferPhase(int score, bool isOptional)
    {
        int minPower = Mathf.Clamp(score - 10, 40, 90);
        int maxPower = Mathf.Clamp(score + 10, 50, 100);

        TransferUIManager.Instance?.ShowTransferOptions(score, isOptional, minPower, maxPower);
    }

    public void OnContractSigned(TeamData newTeam)
    {
        if (GameManager.Instance == null) return;

        if (newTeam != null)
        {
            TeamManager.Instance?.SetPlayerTeam(newTeam.id);
            GameManager.Instance.ApplyStatChange(StatType.Money, 20);
            GameManager.Instance.ApplyStatChange(StatType.Fame, 10);
            GameManager.Instance.SetStat(GameConstants.Stats.Relations, 50);
        }
        else
        {
            GameManager.Instance.ApplyStatChange(StatType.Money, 5);
            GameManager.Instance.ApplyStatChange(StatType.Fame, 5);
            GameManager.Instance.ApplyStatChange(StatType.Relations, 20);
        }

        contractYearsLeft = calendar.CalculateContractDuration(currentYear);
        StatsManager.Instance?.ResetSeasonStats();
        calendar.UpdateUI(textYearInfo);
        CardManager.Instance?.LoadNewCard();
    }

    private void CheckPhaseProgression()
    {
        if (GameManager.Instance == null) return;
        if (currentYear == 24 || currentYear == 29 || currentYear == 35)
            GameManager.Instance.AdvanceToNextPhase();
    }

    // Eski sistemler için yedek
    public void UpdateTimeUI() => calendar.UpdateUI(textYearInfo);
}
