using UnityEngine;
using Game.Core;

public static class SeasonScoring
{
    public static void EvaluateUCLAndRelegation(int seasonScore, int activeTier, int basePower, int uclThreshold, int relegationThreshold)
    {
        if (StoryManager.Instance == null) return;

        if (seasonScore >= uclThreshold)
        {
            if (activeTier == 1)
                StoryManager.Instance.AddFlag(GameKeys.FLAG_UCL_QUALIFIED);
            else
            {
                StoryManager.Instance.ModifyVariable(GameKeys.VAR_TEAM_TIER_MOD, -1);
                StoryManager.Instance.SetVariable(GameKeys.VAR_TEAM_POWER_MOD, 50 - basePower);
                StoryManager.Instance.RemoveFlag(GameKeys.FLAG_UCL_QUALIFIED);
            }
        }
        else if (seasonScore <= relegationThreshold)
        {
            if (activeTier < 3)
            {
                StoryManager.Instance.ModifyVariable(GameKeys.VAR_TEAM_TIER_MOD, 1);
                StoryManager.Instance.SetVariable(GameKeys.VAR_TEAM_POWER_MOD, 80 - basePower);
                StoryManager.Instance.RemoveFlag(GameKeys.FLAG_UCL_QUALIFIED);
            }
        }
        else
        {
            StoryManager.Instance.RemoveFlag(GameKeys.FLAG_UCL_QUALIFIED);
        }
    }

    public static void EvaluateReview(int score, int goodReviewThreshold, int badReviewThreshold)
    {
        if (GameManager.Instance == null) return;

        if (score >= goodReviewThreshold)
        {
            GameManager.Instance.ApplyStatChange(StatType.Money, 10);
            GameManager.Instance.ApplyStatChange(StatType.Fame, 5);
        }
        else if (score < badReviewThreshold)
        {
            GameManager.Instance.ApplyStatChange(StatType.Fame, -5);
            GameManager.Instance.ApplyStatChange(StatType.Relations, -10);
        }
    }
}