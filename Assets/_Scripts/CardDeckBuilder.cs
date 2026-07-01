using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardDeckBuilder
{
    private readonly CardRegistry _registry;
    private readonly RequirementService _reqService;

    public CardDeckBuilder(CardRegistry registry, RequirementService reqService)
    {
        _registry = registry;
        _reqService = reqService;
    }

    public List<CardData> BuildSeasonDeck(int targetSize, int matchesPerSeason, int maxChains)
    {
        var valid = _registry.GetValidCards(_reqService);
        if (valid.Count == 0) return new List<CardData>();

        var startCards = Filter(valid, CardConstants.CategorySeasonStart);
        var endCards = Filter(valid, CardConstants.CategorySeasonEnd);
        var matchCards = Filter(valid, CardConstants.CategoryMatchFiller);
        var transCards = Filter(valid, CardConstants.CategoryMatchTransition);
        var chainCards = Filter(valid, CardConstants.CategoryChainStarter);
        var dailyCards = Filter(valid, CardConstants.CategoryDailyFiller);

        Shuffle(startCards); Shuffle(endCards); Shuffle(matchCards);
        Shuffle(transCards); Shuffle(chainCards); Shuffle(dailyCards);

        int actualMatches = Mathf.Min(matchesPerSeason, matchCards.Count);
        var result = new List<CardData>();

        if (startCards.Any()) result.Add(startCards.First());
        if (endCards.Any()) result.Add(endCards.First()); // Geçici, sona ekleyeceðiz

        var fillerPool = new List<CardData>();
        fillerPool.AddRange(chainCards.Take(maxChains));

        int slotsLeft = targetSize - result.Count - (actualMatches * 2) - fillerPool.Count;
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