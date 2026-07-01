using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Sahne içinde yaşayan (DontDestroyOnLoad OLMAYAN) MonoBehaviour singleton'lar için temel sınıf.
    /// Tek sahne mimarisinde sahneye bağlı yöneticiler bundan türer.
    ///
    /// Önemli: Awake mantığını override edilen Awake yerine <see cref="OnSingletonAwake"/> içine yaz.
    /// Böylece "base.Awake() çağırmayı unutma" ve "duplicate örnekte Awake'in geri kalanı yine çalışır"
    /// hataları kökten engellenir.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        protected void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[{typeof(T).Name}] Sahnede birden fazla örnek var. Fazlalık yok ediliyor.");
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;
            OnSingletonAwake();
        }

        /// <summary>Türeyen sınıf Awake mantığını buraya yazar.</summary>
        protected virtual void OnSingletonAwake() { }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
