using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Sahneler arası yaşayan (DontDestroyOnLoad) MonoBehaviour singleton'lar için temel sınıf.
    /// Tek sahne mimarisinde de güvenle çalışır. Veri/sistem yöneticileri (Team, Country, Save,
    /// Localization vb.) bundan türer.
    ///
    /// Awake mantığını <see cref="OnSingletonAwake"/> içine yaz.
    /// </summary>
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : PersistentSingleton<T>
    {
        public static T Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        [Tooltip("Kapatılırsa sahne değişiminde yok olur (tek sahnede fark etmez).")]
        [SerializeField] private bool _persistAcrossScenes = true;

        protected void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[{typeof(T).Name}] Kalıcı örnek zaten var. Fazlalık yok ediliyor.");
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;

            // DontDestroyOnLoad yalnızca kök (parent'sız) nesnelerde geçerlidir; aksi halde Unity uyarı verir.
            if (_persistAcrossScenes && transform.parent == null)
                DontDestroyOnLoad(gameObject);

            OnSingletonAwake();
        }

        protected virtual void OnSingletonAwake() { }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
