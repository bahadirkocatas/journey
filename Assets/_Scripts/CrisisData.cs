using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CrisisData
{
    public string id;
    public string targetStat;
    public string condition;

    public string title;
    public string description;
    public string iconPath;

    public int resetValue;

    // --- YENÝ EKLENEN FAZ FÝLTRESÝ ---
    // Hangi fazlar arasýnda geçerli? (0: Çaylak, 1: Yükseliþ, 2: Prime, 3: Efsane)
    // Varsayýlan deðerler: 0 ve 10 (Yani her zaman geçerli)
    public int minPhaseIndex = 0;
    public int maxPhaseIndex = 10;
}

[System.Serializable]
public class CrisisCollection
{
    public List<CrisisData> crises;
}