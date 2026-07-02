using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// Oyuncunun kariyer boyunca sahip olduğu can (kalp) sayısını yönetir.
    ///
    /// Game.Core'daki altyapı sınıflarının (Singleton, GameStateMachine, GameConstants)
    /// aksine yeniden kullanılabilir bir altyapı parçası değil, somut bir oyun mekaniği
    /// (kariyer canı + kalp ikon UI'ı); bu yüzden Game.Gameplay'e ait — sahibi
    /// GameManager ile aynı katmanda durur.
    /// </summary>
    [System.Serializable]
    public class LifeSystem
    {
        [Header("Can Ayarları")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private int resetValueAfterDeath = 50;

        public int CurrentLives { get; private set; }
        public GameObject[] HeartIcons { private get; set; }

        // Dışarıya (UI/Manager) haber vermek için event'ler
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
            OnLifeLost?.Invoke(); // Ses, efekt, vs. buraya bağlanır
            UpdateUI();

            if (CurrentLives <= 0)
            {
                OnGameOver?.Invoke();
            }
        }

        /// <summary>
        /// Save/load restore'u için canı doğrudan (event fırlatmadan) ayarlar.
        /// </summary>
        public void SetLives(int lives)
        {
            CurrentLives = Mathf.Clamp(lives, 0, maxLives);
            UpdateUI();
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
}
