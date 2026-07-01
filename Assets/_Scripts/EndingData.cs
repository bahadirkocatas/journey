using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EndingData
{
    public string id;
    public string triggerType; // "LivesZero" (Can Bitti), "Retirement" (Yaþlandý), "Specific" (Özel)
    public string title;
    public string description;
    public string iconPath;

    public string buttonText; // "JÜBÝLE YAP", "VEDA ET"
}

[System.Serializable]
public class EndingCollection
{
    public List<EndingData> endings;
}