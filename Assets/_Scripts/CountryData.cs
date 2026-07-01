using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CountryData
{
    public string id;
    public string name;
    public string flagIconName;
    public string primaryColor;
    public string secondaryColor;
    public int worldCupsWon;
    public int leagueTier;
    public bool isPlayableOrigin;

    // YENÝ: Ülke Ligi Kalite Puaný (Global Transfer için)
    // Ýngiltere: 100, Ýspanya: 95, Türkiye: 70, Alt Ligler: 40 gibi.
    public int leagueQualityScore;

    public string rivalCountryID;
    public string playStyle;
    public string winHeadline;
}

[System.Serializable]
public class CountryCollection
{
    public CountryData[] countries;
}