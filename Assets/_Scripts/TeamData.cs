using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TeamData
{
    public string id;
    public string name;
    public string shortName; // YENÝ: "GS", "FB", "BJK" gibi. Skor tabelasý için.
    public string countryID;
    public string city;      // YENÝ: "Istanbul", "Trabzon". Derbi kontrolü için.
    public int tier;
    public string primaryColor;
    public string secondaryColor;
    public int budget;
    public int championships;
    public string rivalTeamID;
    public string fanBase;
    public string clubCulture;
    public int power;
}

[System.Serializable]
public class TeamCollection
{
    public TeamData[] teams;
}