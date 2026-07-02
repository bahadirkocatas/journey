using System;

namespace Game.Services
{
    /// <summary>
    /// Zar atışının hangi statı, hangi yönde avantajlı gördüğünü belirtir.
    /// (Eski <c>DiceManager</c>'daki <c>mode</c> string parametresinin — "high"/"low" —
    /// yerini alan tip güvenli karşılığı.)
    /// </summary>
    public enum DiceMode
    {
        High,
        Low
    }

    /// <summary>
    /// Bir zar atışının tüm sonucunu taşıyan, değişmez (immutable) veri yapısı.
    /// UI (DiceManager, Katman 4) bunu doğrudan ekrana basar; hesap mantığı burada bitmiştir.
    /// </summary>
    public readonly struct DiceRoll
    {
        public readonly int Target;
        public readonly int Die;
        public readonly int StatBonus;
        public readonly int MomentumBonus;
        public readonly int Total;
        public readonly bool CriticalSuccess;
        public readonly bool CriticalFailure;
        public readonly bool Success;

        public DiceRoll(int target, int die, int statBonus, int momentumBonus, int total,
            bool criticalSuccess, bool criticalFailure, bool success)
        {
            Target = target;
            Die = die;
            StatBonus = statBonus;
            MomentumBonus = momentumBonus;
            Total = total;
            CriticalSuccess = criticalSuccess;
            CriticalFailure = criticalFailure;
            Success = success;
        }
    }

    /// <summary>
    /// BG3 tarzı zar sisteminin saf mantığı. Hiçbir MonoBehaviour/UnityEngine bağımlılığı
    /// yoktur — eski <c>DiceManager</c>'daki matematik buradan taşındı, UI/animasyon kısmı
    /// (Katman 4) <c>DiceManager</c>'da kalacak.
    ///
    /// Puanlama: Zar (1-20) + Stat Bonus (High/Low Mode) + Momentum Bonus &gt;= Rastgele Hedef (10-15).
    ///
    /// Bilinçli olarak dışarıda bırakılanlar (yukarı-bağımlılık oluşturmamak için):
    /// hangi statın okunacağı (<c>GameManager</c>) ve momentum bonusunun kaynağı
    /// (<c>StoryManager.GetVariable("ucl_momentum")</c>) — bunlar çağıran taraftan
    /// (Katman 4'te <c>DiceManager</c>) parametre olarak gelir.
    /// </summary>
    public class DiceService
    {
        private readonly System.Random _random;

        public DiceService(int seed = -1)
        {
            _random = seed >= 0 ? new System.Random(seed) : new System.Random();
        }

        /// <summary>"high" / "low" string'ini <see cref="DiceMode"/>'a çevirir (tek çözüm noktası).</summary>
        public static DiceMode ParseMode(string mode) =>
            string.Equals(mode, "low", StringComparison.OrdinalIgnoreCase) ? DiceMode.Low : DiceMode.High;

        /// <summary>Rastgele zorluk hedefi üretir (10-15 arası).</summary>
        public int RollTarget() => _random.Next(10, 16);

        /// <summary>1-20 arası bir zar atışı yapar.</summary>
        public int RollDie() => _random.Next(1, 21);

        /// <summary>
        /// Stat değerine ve moda göre bonusu hesaplar (eski <c>DiceManager.CalculateStatBonus</c>
        /// ile birebir aynı kademeler).
        /// </summary>
        public int CalculateStatBonus(int statValue, DiceMode mode)
        {
            if (mode == DiceMode.Low)
            {
                // Düşük kaynak isterse: 0-20(3), 20-60(2), 60-80(1), 80-100(-1)
                if (statValue <= 20) return 3;
                if (statValue <= 60) return 2;
                if (statValue <= 80) return 1;
                return -1;
            }
            else
            {
                // Yüksek kaynak isterse: 80-100(3), 40-80(2), 20-40(1), 0-20(-1)
                if (statValue >= 80) return 3;
                if (statValue >= 40) return 2;
                if (statValue >= 20) return 1;
                return -1;
            }
        }

        /// <summary>
        /// Tam bir zar atışını uçtan uca çözer: hedef belirle, zar at, bonusları topla,
        /// kritik (1/20) ve normal başarı kurallarını uygula.
        /// </summary>
        /// <param name="statValue">İlgili statın güncel değeri (0-100).</param>
        /// <param name="momentumBonus">Ek momentum bonusu (örn. UCL finalinde yarı başarılarından).</param>
        /// <param name="mode">Statın hangi yönde avantajlı sayılacağı.</param>
        public DiceRoll Resolve(int statValue, int momentumBonus, DiceMode mode)
        {
            int target = RollTarget();
            int die = RollDie();
            int statBonus = CalculateStatBonus(statValue, mode);
            int total = die + statBonus + momentumBonus;

            bool criticalSuccess = die == 20;
            bool criticalFailure = die == 1;
            bool success = criticalSuccess || (!criticalFailure && total >= target);

            return new DiceRoll(target, die, statBonus, momentumBonus, total, criticalSuccess, criticalFailure, success);
        }
    }
}
