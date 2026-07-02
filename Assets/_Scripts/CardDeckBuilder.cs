using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Core;
using Game.Data;

namespace Game.Services
{
    /// <summary>
    /// Havuzdaki geçerli (requirement + cooldown filtresinden geçmiş) kartlardan
    /// bir sezonluk deste sırası kurar: sezon başı/sonu, maç dolgusu/geçişi, zincir
    /// başlatıcı ve günlük dolgu kartlarını hedef boyuta göre yerleştirir.
    /// </summary>
    public class CardDeckBuilder
    {
        private readonly CardRegistry _registry;
        private readonly RequirementService _reqService;

        public CardDeckBuilder(CardRegistry registry, RequirementService reqService)
        {
            _registry = registry;
            _reqService = reqService;
        }

        /// <summary>
        /// Sezonluk kart destesini kurar. Havuz, <see cref="CardRegistry.GetValidCards"/>
        /// üzerinden alınır — requirement ve cooldown filtresi burada zaten uygulanmıştır.
        /// </summary>
        public List<CardData> BuildSeasonDeck(int targetSize, int matchesPerSeason, int maxChains)
        {
            var valid = _registry.GetValidCards(_reqService);
            if (valid.Count == 0) return new List<CardData>();

            var startCards = Filter(valid, GameConstants.Categories.SeasonStart);
            var endCards = Filter(valid, GameConstants.Categories.SeasonEnd);
            var matchCards = Filter(valid, GameConstants.Categories.MatchFiller);
            var transCards = Filter(valid, GameConstants.Categories.MatchTransition);
            var chainCards = Filter(valid, GameConstants.Categories.ChainStarter);
            var dailyCards = Filter(valid, GameConstants.Categories.DailyFiller);

            Shuffle(startCards); Shuffle(endCards); Shuffle(matchCards);
            Shuffle(transCards); Shuffle(chainCards); Shuffle(dailyCards);

            int actualMatches = Mathf.Min(matchesPerSeason, matchCards.Count);

            // Sezon başı/sonu kartları için ayrılan slot sayısı (final listede ayrıca eklenir).
            int reservedCount = (startCards.Any() ? 1 : 0) + (endCards.Any() ? 1 : 0);

            var fillerPool = new List<CardData>();
            fillerPool.AddRange(chainCards.Take(maxChains));

            int slotsLeft = targetSize - reservedCount - (actualMatches * 2) - fillerPool.Count;
            if (slotsLeft > 0 && dailyCards.Any())
            {
                for (int i = 0; i < slotsLeft; i++)
                    fillerPool.Add(dailyCards[i % dailyCards.Count]);
            }
            Shuffle(fillerPool);

            int gapCount = actualMatches + 1;
            var gaps = new List<List<CardData>>(gapCount);
            for (int i = 0; i < gapCount; i++) gaps.Add(new List<CardData>());

            for (int i = 0; i < fillerPool.Count; i++)
                gaps[i % gapCount].Add(fillerPool[i]);

            var final = new List<CardData>();
            if (startCards.Any()) final.Add(startCards.First());

            for (int i = 0; i < gapCount; i++)
            {
                final.AddRange(gaps[i]);
                if (i < actualMatches)
                {
                    if (i < transCards.Count) final.Add(transCards[i]);
                    if (i < matchCards.Count) final.Add(matchCards[i]);
                }
            }

            if (endCards.Any()) final.Add(endCards.First());

            Debug.Log($"[DeckBuilder] Deste boyutu: {final.Count}");
            return final;
        }

        private List<CardData> Filter(List<CardData> pool, string category) =>
            pool.Where(c => string.Equals(c.category, category, System.StringComparison.OrdinalIgnoreCase)).ToList();

        private void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
    }
}