using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Data;

namespace Game.Services
{
    /// <summary>
    /// Yüklenen tüm kartları ID ve kategoriye göre indeksleyen, aday kart havuğu
    /// sorgularını (<see cref="GetValidCards"/>) sağlayan salt-okunur kayıt defteri.
    /// </summary>
    public class CardRegistry
    {
        private readonly Dictionary<string, CardData> _idMap = new Dictionary<string, CardData>();
        private readonly Dictionary<string, List<CardData>> _categoryMap = new Dictionary<string, List<CardData>>();
        public List<CardData> AllCards { get; private set; } = new List<CardData>();

        public void LoadFromJSON(TextAsset jsonFile)
        {
            if (jsonFile == null) return;
            try
            {
                var data = JsonUtility.FromJson<CardCollection>(jsonFile.text);
                if (data?.cards != null)
                    RegisterCards(data.cards);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CardRegistry] JSON hatası: {e.Message}");
            }
        }

        public void RegisterCards(List<CardData> cards)
        {
            foreach (var card in cards)
            {
                if (card == null || string.IsNullOrEmpty(card.id)) continue;
                _idMap[card.id] = card;

                string cat = string.IsNullOrEmpty(card.category) ? "uncategorized" : card.category;
                if (!_categoryMap.ContainsKey(cat)) _categoryMap[cat] = new List<CardData>();
                _categoryMap[cat].Add(card);

                if (!AllCards.Contains(card)) AllCards.Add(card);
            }
        }

        public CardData GetCardByID(string id) => _idMap.TryGetValue(id, out var card) ? card : null;

        public List<CardData> GetCardsByCategory(string category) =>
            _categoryMap.TryGetValue(category, out var list) ? list.ToList() : new List<CardData>();

        /// <summary>
        /// Havuzdan seçilebilecek kartları döner. Bu bir havuz seçimi olduğu için
        /// cooldown burada uygulanır (<c>bypassCooldown: false</c>) — zorlanan zincir
        /// devam kartları bu yoldan geçmez, onlar <c>CardManager</c>'daki ayrı yolu kullanır.
        /// </summary>
        public List<CardData> GetValidCards(RequirementService reqService) =>
            AllCards.Where(c => reqService.IsCardEligible(c, bypassCooldown: false)).ToList();
    }
}
