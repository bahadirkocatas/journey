using System;

namespace Game.Services
{
    /// <summary>
    /// Maç simülasyonunu yapar. Hiçbir MonoBehaviour bağımlılığı yoktur.
    /// </summary>
    public class MatchSimulator
    {
        private readonly MatchSimulationConfig _config;
        private readonly System.Random _random;

        public MatchSimulator(MatchSimulationConfig config, int seed = -1)
        {
            _config = config;
            _random = seed >= 0 ? new System.Random(seed) : new System.Random();
        }

        /// <summary>
        /// Maçı simüle eder.
        /// </summary>
        /// <param name="activePower">Takım gücü (0-100)</param>
        /// <param name="form">Oyuncu formu (0-100)</param>
        /// <param name="focus">Odak (0-100)</param>
        /// <param name="fame">Şöhret (0-100)</param>
        /// <param name="relations">İlişki (0-100)</param>
        /// <returns>(Gol, Asist, Rating)</returns>
        public (int goals, int assists, float rating) SimulateMatch(
            int activePower,
            int form,
            int focus,
            int fame,
            int relations)
        {
            // 1. Pozisyon sayısını hesapla
            int maxChances = CalculateMaxChances(activePower);
            if (maxChances == 0)
                return (0, 0, GenerateRating(0, 0, form));

            // 2. Kişilik özelliklerini belirle
            bool isSelfish = relations < _config.selfishThreshold;
            bool isChaos = focus < _config.chaosThreshold;
            bool isFameWalker = form < _config.fameWalkerFormThreshold && fame > _config.fameWalkerFameThreshold;

            int goals = 0, assists = 0;

            // 3. Kaos kontrolü
            if (isChaos)
            {
                // %80 ihtimalle hiçbir şey yapmaz, %20 ihtimalle tüm pozisyonları gole çevirir
                if (_random.NextDouble() < 0.8f)
                    return (0, 0, GenerateRating(0, 0, form));
                else
                    return (maxChances, 0, GenerateRating(maxChances, 0, form));
            }

            // 4. Şovmen (Fame Walker) kontrolü
            if (isFameWalker)
            {
                // %30 ihtimalle 1 gol atar
                if (_random.NextDouble() < 0.3f)
                    goals = 1;
            }
            else
            {
                // 5. Normal simülasyon
                for (int i = 0; i < maxChances; i++)
                {
                    float convertChance = (form * _config.conversionFormWeight + focus * _config.conversionFocusWeight) / 100f;
                    if (isSelfish) convertChance += _config.selfishBonus;

                    if (_random.NextDouble() < convertChance)
                    {
                        if (isSelfish || _random.NextDouble() < _config.assistProbability)
                            goals++;
                        else
                            assists++;
                    }
                }
            }

            // 6. Bencil oyuncu asist yapamaz
            if (isSelfish) assists = 0;

            // 7. Rating hesapla
            float rating = GenerateRating(goals, assists, form);

            return (goals, assists, rating);
        }

        private int CalculateMaxChances(int activePower)
        {
            int roll = _random.Next(_config.chanceRollMin, _config.chanceRollMax + 1);

            if (activePower > _config.powerThresholdHigh)
                return roll > 20 ? _random.Next(_config.maxChancesHighMin, _config.maxChancesHighMax + 1) : 1;
            else if (activePower > _config.powerThresholdMid)
                return roll > 30 ? _random.Next(_config.maxChancesMidMin, _config.maxChancesMidMax + 1) : 0;
            else if (activePower > _config.powerThresholdLow)
                return roll > 40 ? _random.Next(_config.maxChancesLowMin, _config.maxChancesLowMax + 1) : 0;
            else
                return roll > 50 ? _random.Next(_config.maxChancesLowMin, _config.maxChancesLowMax + 1) : 0;
        }

        private float GenerateRating(int goals, int assists, int form)
        {
            float rating = _config.baseRating
                         + (goals * _config.goalRatingBonus)
                         + (assists * _config.assistRatingBonus);

            if (goals == 0 && assists == 0 && form < 50)
                rating -= _config.poorFormPenalty * (float)_random.NextDouble();

            return Math.Clamp(rating, 0, _config.maxRating);
        }
    }
}
