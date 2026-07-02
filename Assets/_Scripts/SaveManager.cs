using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class GameSaveData
{
    // --- K�ML�K ---
    public string playerName;
    public string playerNationID;
    public string playerTeamID;

    // --- ZAMAN ---
    public int currentYear;
    public int currentWeek;
    public int contractYearsLeft;
    public int currentPhaseIndex;

    // --- KAYNAKLAR ---
    public int form;
    public int money;
    public int fame;
    public int relations;
    public int focus;
    public int lives;

    // --- �STAT�ST�K VE PERFORMANS ---
    public int seasonGoals;
    public int seasonAssists;
    public int totalGoals;
    public int totalAssists;
    public int matchesPlayed;
    public float averageRating;

    // --- OYUN AKI� HAFIZASI ---
    public List<string> storyFlags;

    // Dictionary JSON'a direkt kaydedilemez, listelerle ayr��t�r�yoruz
    public List<string> variableKeys;
    public List<int> variableValues;

    public List<string> cooldownKeys;
    public List<int> cooldownValues;
}

/// <summary>
/// Oyunun t�m verilerini JSON format�nda kal�c� olarak diske yazar ve okur.
/// TAM MVP S�R�M�: Origin, Trait ve Memory verileri tamamen temizlendi. Saf performans ve durum kayd� yapar.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    private string saveFilePath;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);

        saveFilePath = Path.Combine(Application.persistentDataPath, "career_save.json");
    }

    public void SaveGame()
    {
        if (GameManager.Instance == null || StoryManager.Instance == null) return;

        GameSaveData data = new GameSaveData();

        // 1. Kimlik ve Tak�m
        data.playerName = GameManager.Instance.playerName;
        data.playerNationID = StoryManager.Instance.GetString(GameKeys.FLAG_PLAYER_NATION);
        data.playerTeamID = TeamManager.Instance != null && TeamManager.Instance.currentTeam != null ? TeamManager.Instance.currentTeam.id : "";

        // 2. Zaman ve Faz
        if (SeasonManager.Instance != null)
        {
            data.currentYear = SeasonManager.Instance.currentYear;
            data.currentWeek = SeasonManager.Instance.currentWeek;
            data.contractYearsLeft = SeasonManager.Instance.contractYearsLeft;
        }
        data.currentPhaseIndex = GameManager.Instance.currentPhaseIndex;

        // 3. Kaynaklar ve Can
        data.form = GameManager.Instance.form;
        data.money = GameManager.Instance.money;
        data.fame = GameManager.Instance.fame;
        data.relations = GameManager.Instance.relations;
        data.focus = GameManager.Instance.focus;
        data.lives = GameManager.Instance.CurrentLives;

        // 4. �statistikler
        if (StatsManager.Instance != null)
        {
            data.seasonGoals = StatsManager.Instance.seasonGoals;
            data.seasonAssists = StatsManager.Instance.seasonAssists;
            data.totalGoals = StatsManager.Instance.totalGoals;
            data.totalAssists = StatsManager.Instance.totalAssists;
            data.matchesPlayed = StatsManager.Instance.matchesPlayed;
            data.averageRating = StatsManager.Instance.averageRating;
        }

        // 5. Oyun Ak�� Haf�zas�
        data.storyFlags = new List<string>(StoryManager.Instance.storyFlags);

        data.variableKeys = new List<string>(StoryManager.Instance.storyVariables.Keys);
        data.variableValues = new List<int>(StoryManager.Instance.storyVariables.Values);

        data.cooldownKeys = new List<string>(StoryManager.Instance.cardCooldowns.Keys);
        data.cooldownValues = new List<int>(StoryManager.Instance.cardCooldowns.Values);

        // Dosyaya Yaz
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"[SAVE MANAGER]: MVP Oyun Kaydedildi -> {saveFilePath}");
    }

    public bool LoadGame()
    {
        if (!HasSaveFile()) return false;

        try
        {
            string json = File.ReadAllText(saveFilePath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

            // 1. Kimlik ve Tak�m
            GameManager.Instance.playerName = data.playerName;
            StoryManager.Instance.SetString(GameKeys.FLAG_PLAYER_NATION, data.playerNationID);
            if (TeamManager.Instance != null && !string.IsNullOrEmpty(data.playerTeamID))
            {
                TeamManager.Instance.SetPlayerTeam(data.playerTeamID);
            }

            // 2. Zaman
            if (SeasonManager.Instance != null)
            {
                SeasonManager.Instance.currentYear = data.currentYear;
                SeasonManager.Instance.currentWeek = data.currentWeek;
                SeasonManager.Instance.contractYearsLeft = data.contractYearsLeft;
            }
            GameManager.Instance.currentPhaseIndex = data.currentPhaseIndex;

            // 3. Kaynaklar ve Can
            GameManager.Instance.SetStat("Form", data.form);
            GameManager.Instance.SetStat("Money", data.money);
            GameManager.Instance.SetStat("Fame", data.fame);
            GameManager.Instance.SetStat("Relations", data.relations);
            GameManager.Instance.SetStat("Focus", data.focus);

            GameManager.Instance.RestoreLives(data.lives);

            // 4. �statistikler
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.seasonGoals = data.seasonGoals;
                StatsManager.Instance.seasonAssists = data.seasonAssists;
                StatsManager.Instance.totalGoals = data.totalGoals;
                StatsManager.Instance.totalAssists = data.totalAssists;
                StatsManager.Instance.matchesPlayed = data.matchesPlayed;
                StatsManager.Instance.averageRating = data.averageRating == 0 ? 6.0f : data.averageRating;
            }

            // 5. Oyun Ak�� Haf�zas�n� Geri Y�kleme
            StoryManager.Instance.storyFlags = new HashSet<string>(data.storyFlags);

            StoryManager.Instance.storyVariables.Clear();
            for (int i = 0; i < data.variableKeys.Count; i++)
            {
                StoryManager.Instance.storyVariables.Add(data.variableKeys[i], data.variableValues[i]);
            }

            StoryManager.Instance.cardCooldowns.Clear();
            for (int i = 0; i < data.cooldownKeys.Count; i++)
            {
                StoryManager.Instance.cardCooldowns.Add(data.cooldownKeys[i], data.cooldownValues[i]);
            }

            Debug.Log("<color=green>[SAVE MANAGER]: MVP Kay�tl� Oyun Ba�ar�yla Y�klendi!</color>");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SAVE MANAGER]: Kay�t y�klenirken hata: " + e.Message);
            return false;
        }
    }

    public bool HasSaveFile()
    {
        return File.Exists(saveFilePath);
    }

    public void DeleteSave()
    {
        if (HasSaveFile())
        {
            File.Delete(saveFilePath);
            Debug.Log("[SAVE MANAGER]: Kay�t Dosyas� Silindi.");
        }
    }
}