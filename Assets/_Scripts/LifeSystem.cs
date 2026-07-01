using UnityEngine;

[System.Serializable]
public class LifeSystem
{
    [Header("Can Ayarlarý")]
    [SerializeField] private int maxLives = 3;
    [SerializeField] private int resetValueAfterDeath = 50;

    public int CurrentLives { get; private set; }
    public GameObject[] HeartIcons { private get; set; }

    // Dýþarýya (UI/Manager) haber vermek için event'ler
    public event System.Action OnLifeLost;
    public event System.Action OnGameOver;

    public void Initialize(GameObject[] heartIcons)
    {
        HeartIcons = heartIcons;
        CurrentLives = maxLives;
        UpdateUI();
    }

    public void LoseLife()
    {
        if (CurrentLives <= 0) return;

        CurrentLives--;
        OnLifeLost?.Invoke(); // Ses, efekt, vs. buraya baðlanýr
        UpdateUI();

        if (CurrentLives <= 0)
        {
            OnGameOver?.Invoke();
        }
        else
        {
            // Can gitti ama oyun devam ediyor: Stat'larý sýfýrla (GameManager bunu dinleyecek)
            ResetStats();
        }
    }

    public void ResetStats()
    {
        // Stat'larý sýfýrla (GameManager'daki stat'lara uygulanmak üzere event fýrlat)
        // Bunu GameManager HandleLifeLost içinde yapacak, burada sadece sinyal veriyoruz.
        // Not: Bu metot doðrudan stat sýfýrlamaz, sadece dýþarýya haber verir.
        // GameManager bu event'i dinleyip stat'larý sýfýrlayacak.
    }

    private void UpdateUI()
    {
        if (HeartIcons == null) return;
        for (int i = 0; i < HeartIcons.Length; i++)
        {
            if (HeartIcons[i] != null)
                HeartIcons[i].SetActive(i < CurrentLives);
        }
    }
}