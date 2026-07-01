using UnityEngine;
using System.Collections.Generic;

namespace Game.Data
{
    // --- TEKİL KART YAPISI ---
    [System.Serializable]
    public class CardData
    {
        [Header("--- KİMLİK ---")]
        public string id;
        public string category; // "match_filler", "chain_starter", "ucl_group" vb. (string sabit — JSON'dan genişletilebilir)
        public string scenarioText;
        public string characterImageName;

        // Zincir/hikaye kartı işareti (JSON: isStoryCard) — UCL akışı ve ileride
        // hafızalı zincir kartları bu bayrağa bakacak.
        public bool isStoryCard;

        [Header("--- SİSTEM AYARLARI ---")]
        public int cooldownWeeks; // Kart oynandıktan sonra kaç hafta desteye giremesin

        // KARTIN GÖRÜNME KOŞULU
        public List<RequirementData> requirements;

        // ==========================================================
        // İki seçim (sağ = kabul, sol = red). Eskiden burada ~12 alan iki kez
        // kopyalanıyordu (rightForm/leftForm, rightChoiceText/leftChoiceText vb.);
        // artık tek tip olan ChoiceData ile temsil ediliyor (bkz. ChoiceData.cs).
        //
        // Varsayılan olarak boş bir ChoiceData ile başlatılıyor: JsonUtility flat
        // JSON alanlarını (rightForm, leftChoiceText ...) bu nested yapıya henüz
        // eşleyemiyor (Newtonsoft converter'a geçiş Katman 2'de yapılacak); bu süre
        // boyunca right/left null kalıp NullReferenceException atmasın diye.
        [Header("--- SAĞ SEÇİM (KABUL) ---")]
        public ChoiceData right = new ChoiceData();

        [Header("--- SOL SEÇİM (RED) ---")]
        public ChoiceData left = new ChoiceData();
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
}
