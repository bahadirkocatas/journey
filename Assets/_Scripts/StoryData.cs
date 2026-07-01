using UnityEngine;
using System.Collections.Generic;

// 1. TEK BÝR ETKÝNÝN YAPISI (Örn: "Money: +500")
[System.Serializable]
public class StoryEffect
{
    public string type; // Örn: "Resource", "Relationship", "Money"
    public string key;  // Örn: "Form", "Media", "StartingCash"
    public int value;   // Örn: 10, -5, 5000
}

// 2. TEK BÝR HÝKAYENÝN YAPISI (Örn: "Sokak Çocuðu")
[System.Serializable]
public class StoryData
{
    public string id;             // Benzersiz kimlik (örn: "story_street")
    public string title;          // Baþlýk
    public string description;    // Açýklama metni
    public string icon_path;      // Görselin yolu  

    // Bu hikayeyi seçince kazanýlan baþlangýç etkileri
    public List<StoryEffect> effects;

    // Bu hikayeyi seçince kazanýlan etiketler (Tags)
    // Örn: ["origin_street", "volatile_temper"]
    public List<string> narrative_tags;
}

// 3. JSON DOSYASININ ANA KAPSAYICISI
[System.Serializable]
public class StoryCollection
{
    public List<StoryData> stories;
}