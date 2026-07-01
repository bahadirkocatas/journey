using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Data;

public class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance;

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

    private bool isTimeFrozenForUCL = false;

    // --- Geriye Dönük Uyumluluk (Facade) ---
    public int currentYear { get => calendar.currentYear; set => calendar.currentYear = value; }
    public int currentWeek { get => calendar.currentWeek; set => calendar.currentWeek = value; }
    public int contractYearsLeft { get => calendar.contractYearsLeft; set => calendar.contractYearsLeft = value; }
    public int weeksPerSeason => calendar.weeksPerSeason;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        LoadSchedule();
        calendar.UpdateUI(textYearInfo);
    }

    private void OnEnable()
    {
        GameEvents.OnUCLFinalCompleted += HandleUCLFinalFinished;
        GameEvents.OnCardFlowEnded += AdvanceWeek; // 🆕 Yeni bağlantı
    }

    private void OnDisable()
    {
        GameEvents.OnUCLFinalCompleted -= HandleUCLFinalFinished;
        GameEvents.OnCardFlowEnded -= AdvanceWeek;
    }

    private void HandleUCLFinalFinished() => isTimeFrozenForUCL = false;

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
        if (isTimeFrozenForUCL)
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
            CheckSeasonEndState();
            return;
        }

        CardManager.Instance?.LoadNewCard();
        SaveManager.Instance?.SaveGame();
    }

    private void CheckSeasonEndState()
    {
        if (StoryManager.Instance == null) return;

        if (StoryManager.Instance.HasFlag(GameKeys.FLAG_UCL_QUALIFIED))
        {
            if (StoryManager.Instance.GetVariable(GameKeys.VAR_UCL_PROGRESS) >= 2)
            {
                if (!isTimeFrozenForUCL)
                {
                    isTimeFrozenForUCL = true;
                    GameEvents.OnUCLFinalTriggered?.Invoke();
                    StoryManager.Instance.SetVariable(GameKeys.VAR_UCL_PROGRESS, 0);
                    CardManager.Instance?.LoadNewCard();
                    return;
                }
            }
            else if (!StoryManager.Instance.HasFlag(GameKeys.FLAG_UCL_ELIMINATION_SHOWN))
            {
                var failCard = new CardData
                {
                    id = "ucl_eliminated",
                    category = GameKeys.CAT_UCL_GROUP,
                    scenarioText = "LOK_UCL_ELIMINATED",
                    characterImageName = "ucl_atmosphere",
                    right = new ChoiceData { choiceText = "LOK_CONTINUE" },
                    left = new ChoiceData { choiceText = "LOK_CONTINUE" }
                };
                CardManager.Instance?.InjectMilestoneCards(new List<CardData> { failCard }, true);
                StoryManager.Instance.AddFlag(GameKeys.FLAG_UCL_ELIMINATION_SHOWN);
                StoryManager.Instance.SetVariable(GameKeys.VAR_UCL_PROGRESS, 0);
            }
        }

        if (CardManager.Instance != null && CardManager.Instance.QueueCount > 0)
            CardManager.Instance.LoadNewCard();
        else
        {
            StoryManager.Instance.RemoveFlag(GameKeys.FLAG_UCL_ELIMINATION_SHOWN);
            EndSeason();
        }
    }

    private void StartNewSeasonRoutine()
    {
        if (CardManager.Instance == null) return;

        int seasonIndex = currentYear - calendar.startingAge + 1;
        CardManager.Instance.BuildSeasonDeck(weeksPerSeason);

        // UCL Grup Kartları
        if (StoryManager.Instance != null && StoryManager.Instance.HasFlag(GameKeys.FLAG_UCL_QUALIFIED))
        {
            var uclCards = new List<CardData>();
            for (int i = 1; i <= 2; i++)
            {
                uclCards.Add(new CardData
                {
                    id = "dyn_ucl_group_" + i,
                    category = GameKeys.CAT_UCL_GROUP,
                    scenarioText = $"Şampiyonlar Ligi eleme maçındasın. Grup {i}/2.",
                    characterImageName = "ucl_atmosphere",
                    right = new ChoiceData
                    {
                        choiceText = "Takım İçin Savaş",
                        relations = 10,
                        focus = -5,
                        form = -10,
                        hiddenEffects = new List<HiddenEffect> { new HiddenEffect { variableName = GameKeys.VAR_UCL_PROGRESS, changeAmount = 1 } }
                    },
                    left = new ChoiceData
                    {
                        choiceText = "Bireysel Oyna",
                        fame = 10,
                        relations = -10,
                        form = -10,
                        hiddenEffects = new List<HiddenEffect> { new HiddenEffect { variableName = GameKeys.VAR_UCL_PROGRESS, changeAmount = 1 } }
                    }
                });
            }
            CardManager.Instance.InjectMilestoneCards(uclCards, false);
        }

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

            SeasonScoring.EvaluateUCLAndRelegation(seasonScore, activeTier, basePower, scoreForUCLQualification, scoreForRelegation);
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
            SeasonScoring.EvaluateReview(score, scoreForGoodReview, scoreForBadReview);
            StatsManager.Instance?.ResetSeasonStats();
            calendar.UpdateUI(textYearInfo);
            CardManager.Instance?.LoadNewCard();
        }
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
            GameManager.Instance.SetStat("Relations", 50);
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