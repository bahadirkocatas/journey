using UnityEngine;
using TMPro;

[System.Serializable]
public class SeasonCalendar
{
    public int startingAge = 18;
    public int currentYear = 18;
    public int currentWeek = 1;
    public int weeksPerSeason = 25;
    public int contractYearsLeft = 2;

    public void Initialize()
    {
        currentYear = startingAge;
        currentWeek = 1;
    }

    public void AdvanceWeek() => currentWeek++;

    public void AdvanceYear()
    {
        currentWeek = 1;
        currentYear++;
        contractYearsLeft--;
    }

    public bool IsSeasonEnd() => currentWeek >= weeksPerSeason;

    public int CalculateContractDuration(int age)
    {
        if (age <= 23) return Random.Range(4, 6);
        if (age <= 30) return Random.Range(3, 5);
        return Random.Range(1, 3);
    }

    public void UpdateUI(TextMeshProUGUI textYearInfo)
    {
        if (textYearInfo != null)
        {
            textYearInfo.text = $"YAÞ: {currentYear} | HAFTA: {currentWeek}/{weeksPerSeason} | KONTRAT: {contractYearsLeft} YIL";
        }
    }
}