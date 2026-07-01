using System;

public static class GameEvents
{
    public static Action<CardData, bool> OnCardSwiped;
    public static Action<CardData, bool> OnCardResolved;
    public static Action<string> OnStatCritical;
    public static Action OnLifeLost;
    public static Action<int> OnWeekAdvanced;
    public static Action<int> OnSeasonEnded;
    public static Action OnUCLFinalTriggered;
    public static Action OnUCLFinalCompleted;
    public static Action OnResourcesUpdated;
    public static Action OnCardFlowEnded;
    public static Action OnCardLoaded; // CardController reset için
    public static Action<bool, CardData> OnCardDragged; // CardController sürüklerken

    // 🆕 TeamManager için
    public static Action<TeamData> OnTeamChanged;
}