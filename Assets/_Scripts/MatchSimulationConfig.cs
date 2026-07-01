using UnityEngine;

[CreateAssetMenu(fileName = "MatchSimulationConfig", menuName = "Game/Simulation Config")]
public class MatchSimulationConfig : ScriptableObject
{
    [Header("Genel Zorluk (Balancing)")]
    public int powerThresholdHigh = 85;
    public int powerThresholdMid = 70;
    public int powerThresholdLow = 50;

    [Header("Pozisyon Sayısı (Chances)")]
    public int maxChancesHighMin = 2;
    public int maxChancesHighMax = 5;
    public int maxChancesMidMin = 1;
    public int maxChancesMidMax = 4;
    public int maxChancesLowMin = 0;
    public int maxChancesLowMax = 3;

    [Header("Kişilik Etkileri")]
    public int selfishThreshold = 30;
    public int chaosThreshold = 30;
    public int fameWalkerFormThreshold = 30;
    public int fameWalkerFameThreshold = 80;

    [Header("Gol Dönüşüm Oranı")]
    public float conversionFormWeight = 0.6f;
    public float conversionFocusWeight = 0.4f;
    public float selfishBonus = 0.3f;
    public float assistProbability = 0.6f;

    [Header("Maç Reytingi")]
    public float baseRating = 6.0f;
    public float goalRatingBonus = 1.0f;
    public float assistRatingBonus = 0.5f;
    public float poorFormPenalty = 1.5f;
    public float maxRating = 10f;

    [Header("Rastgelelik")]
    public int chanceRollMin = 0;
    public int chanceRollMax = 100;

    // ❌ ÖDÜL ALANLARI TAMAMEN KALDIRILDI
}