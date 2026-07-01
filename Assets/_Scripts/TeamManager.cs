using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance { get; private set; }

    [Header("Veri Dosyası")]
    [SerializeField] private TextAsset teamsJsonFile;

    [Header("Takım Verileri")]
    public List<TeamData> allTeams = new List<TeamData>(); // ← public yapıldı

    public TeamData currentTeam { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadTeams();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ========================================================================
    // 📦 VERİ YÜKLEME
    // ========================================================================
    private void LoadTeams()
    {
        if (teamsJsonFile == null)
        {
            Debug.LogError("[TeamManager] JSON dosyası atanmamış!");
            return;
        }

        try
        {
            var data = JsonUtility.FromJson<TeamCollection>(teamsJsonFile.text);
            if (data != null && data.teams != null)
            {
                allTeams = new List<TeamData>(data.teams);
                Debug.Log($"[TeamManager] {allTeams.Count} takım başarıyla yüklendi.");
            }
            else
            {
                Debug.LogWarning("[TeamManager] JSON geçersiz veya boş!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TeamManager] JSON okuma hatası: {e.Message}");
        }
    }

    // ========================================================================
    // 🏷️ TAKIM SEÇİMİ
    // ========================================================================
    public void SetPlayerTeam(string teamId)
    {
        if (string.IsNullOrEmpty(teamId))
        {
            Debug.LogWarning("[TeamManager] Geçersiz takım ID'si!");
            return;
        }

        var team = allTeams.FirstOrDefault(t => t.id == teamId);
        if (team == null)
        {
            Debug.LogWarning($"[TeamManager] Takım bulunamadı: {teamId}");
            return;
        }

        currentTeam = team;
        Debug.Log($"[TeamManager] Takım seçildi: {team.name}");

        // StoryManager'a kaydet
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.SetString("Team_Name", team.name);
            StoryManager.Instance.SetVariable("team_power_modifier", 0);
            StoryManager.Instance.SetVariable("team_tier_modifier", 0);
        }

        GameEvents.OnTeamChanged?.Invoke(team);
    }

    // ========================================================================
    // 📊 TAKIM BİLGİLERİ (Hesaplamalar)
    // ========================================================================
    public int GetActivePower()
    {
        if (currentTeam == null) return 50;

        int basePower = currentTeam.power;
        int powerMod = StoryManager.Instance?.GetVariable("team_power_modifier") ?? 0;
        int relations = GameManager.Instance?.relations ?? 50;

        int chemistry = 0;
        if (relations > 80) chemistry = 5;
        else if (relations < 30) chemistry = -5;

        return Mathf.Clamp(basePower + powerMod + chemistry, 10, 100);
    }

    public int GetActiveTier()
    {
        if (currentTeam == null) return 3;

        int baseTier = currentTeam.tier;
        int tierMod = StoryManager.Instance?.GetVariable("team_tier_modifier") ?? 0;

        return Mathf.Clamp(baseTier + tierMod, 1, 3);
    }

    public List<TeamData> GetTeamsByTier(int tier)
    {
        return allTeams.Where(t => t.tier == tier).ToList();
    }

    // ========================================================================
    // 🛠️ YARDIMCI METOTLAR
    // ========================================================================
    public TeamData GetTeamByID(string id)
    {
        return allTeams.FirstOrDefault(t => t.id == id);
    }

    public List<TeamData> GetAllTeams() => new List<TeamData>(allTeams);
}