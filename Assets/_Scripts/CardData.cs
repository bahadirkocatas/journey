using UnityEngine;
using System.Collections.Generic;

// --- KOÞUL YAPISI (LOGIC GATE) ---
[System.Serializable]
public class RequirementData
{
    // type: "Flag", "Int" (Statlar), "Relation" (NPC Ýliþkisi)
    public string type;

    // key: "mafya_dostu", "Form", "Alex" (NPC adý)
    public string key;

    // comparison: "==", "!=", ">", "<", ">=", "<="
    public string comparison;

    public int valueInt;       // Puanlar ve Ýliþkiler için
    public string valueString; // Flag isimleri için
}

// --- GÝZLÝ ETKÝ YAPISI ---
[System.Serializable]
public class HiddenEffect
{
    public string variableName; // Örn: "ucl_momentum"
    public int changeAmount;    // Örn: +1, -1
}

// --- ÝSTATÝSTÝK ETKÝSÝ (Maç Kartlarý Ýçin) ---
[System.Serializable]
public class StatEffect
{
    public int goals;
    public int assists;
    public float matchRating;
}

// --- TEKÝL KART YAPISI ---
[System.Serializable]
public class CardData
{
    [Header("--- KÝMLÝK ---")]
    public string id;
    public string category; // "match_filler", "chain_starter", "ucl_group" vb.
    public string scenarioText;
    public string characterImageName;

    [Header("--- SÝSTEM AYARLARI ---")]
    public int cooldownWeeks; // Kart oynandýktan sonra kaç hafta desteye giremesin

    // KARTIN GÖRÜNME KOÞULU
    public List<RequirementData> requirements;

    // ==========================================================
    [Header("--- SAÐ SEÇÝM (KABUL) ---")]
    public string rightChoiceText;
    public string rightLockedText;

    public List<RequirementData> rightRequirements; // Seçeneðin kilitli olma koþulu
    public List<RequirementData> rightSuccessRequirements; // Baþarý koþulu (Zar/Zincir için)

    public string rightSuccessCardID;
    public string rightFailureCardID;

    [Header("--- SAÐ ETKÝLER ---")]
    public int rightForm;
    public int rightMoney;
    public int rightFame;
    public int rightRelations;
    public int rightFocus;
    public string rightNpcName;
    public int rightNpcChange;
    public string addFlagRight; // Hafýzaya eklenecek bayrak
    public List<HiddenEffect> rightHiddenEffects; // Arka plan deðiþkenleri
    public StatEffect rightStatEffect; // Maç içi atýlan gol/asist

    // ==========================================================
    [Header("--- SOL SEÇÝM (RED) ---")]
    public string leftChoiceText;
    public string leftLockedText;

    public List<RequirementData> leftRequirements;
    public List<RequirementData> leftSuccessRequirements;

    public string leftSuccessCardID;
    public string leftFailureCardID;

    [Header("--- SOL ETKÝLER ---")]
    public int leftForm;
    public int leftMoney;
    public int leftFame;
    public int leftRelations;
    public int leftFocus;
    public string leftNpcName;
    public int leftNpcChange;
    public string addFlagLeft;
    public List<HiddenEffect> leftHiddenEffects;
    public StatEffect leftStatEffect;
}

[System.Serializable]
public class CardPool
{
    public string poolId;
    public List<CardData> cards;
}

[System.Serializable]
public class CardCollection
{
    public List<CardData> cards;
    public List<CardPool> pools;
}