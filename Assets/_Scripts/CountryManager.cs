using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CountryManager : MonoBehaviour
{
    public static CountryManager Instance;

    public TextAsset countriesJsonFile; // countries.json buraya sürüklenecek
    public List<CountryData> allCountries;
    public List<CountryData> playableCountries; // Sadece seçilebilir olanlar

    public CountryData playerNation; // Oyuncunun seçtiði ülke

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // BÜYÜK MÝMARÝ DÜZELTME: Veri okuma iþlemi Start'tan Awake'e taþýndý!
            // Böylece oyunun ilk karesinde (frame) tüm ülkeler, UI kodlarý (MainMenuManager) çalýþmadan önce hazýr olur.
            LoadCountries();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start metodunu tamamen sildik, çünkü artýk her þeyi Awake'te çözüyoruz.

    void LoadCountries()
    {
        if (countriesJsonFile != null)
        {
            CountryCollection data = JsonUtility.FromJson<CountryCollection>(countriesJsonFile.text);
            allCountries = new List<CountryData>(data.countries);

            // Sadece oynanabilir olanlarý filtrele
            playableCountries = allCountries.Where(c => c.isPlayableOrigin).ToList();

            Debug.Log($"[COUNTRY MANAGER]: {playableCountries.Count} seçilebilir ülke Awake'de (Erken Yükleme) baþarýyla belleðe alýndý.");
        }
        else
        {
            Debug.LogError("[HATA] CountryManager: JSON dosyasý atanmamýþ!");
        }
    }

    public CountryData GetCountryByID(string id)
    {
        return allCountries.FirstOrDefault(c => c.id == id);
    }

    // Bayrak ikonunu getiren yardýmcý fonksiyon
    public Sprite GetFlagSprite(string iconName)
    {
        return Resources.Load<Sprite>($"Flags/{iconName}");
    }

    public void SetPlayerNation(string countryId)
    {
        playerNation = GetCountryByID(countryId);
        if (playerNation != null)
        {
            // StoryManager'a kaydet (Kartlarda kullanmak için)
            StoryManager.Instance.SetString("Player_Nation_Name", playerNation.name);
            Debug.Log("Oyuncu Uyruðu Seçildi: " + playerNation.name);
        }
    }
}