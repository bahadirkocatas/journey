using System;
using System.Collections.Generic;
using Game.Core;
using Game.Data;

namespace Game.Services
{
    /// <summary>
    /// Bir stat değişiminin clamp sonrası hangi sınıra (min/maks) çarptığını belirtir.
    /// </summary>
    public enum StatBoundary
    {
        /// <summary>Sınıra çarpılmadı.</summary>
        None,
        /// <summary>Stat alt sınıra (0) çarptı.</summary>
        Min,
        /// <summary>Stat üst sınıra (100) çarptı.</summary>
        Max
    }

    /// <summary>
    /// <see cref="ApplyChoice"/> sonucunda sınıra çarpan tek bir stat'ı taşır
    /// (çağıran taraf — Katman 3'te GameManager — buradan kriz tetiklemesi yapacak).
    /// </summary>
    public readonly struct StatBoundaryHit
    {
        public StatType Stat { get; }
        public StatBoundary Boundary { get; }

        public StatBoundaryHit(StatType stat, StatBoundary boundary)
        {
            Stat = stat;
            Boundary = boundary;
        }
    }

    /// <summary>
    /// Oyuncunun 5 kaynağını (Form, Money, Fame, Relations, Focus) tutan, okuyan,
    /// değiştiren ve sınırlayan (clamp) TEK doğru kaynak.
    ///
    /// Bu servisten önce stat okuma/yazma <c>GameManager</c>, <c>RequirementService</c>
    /// ve <c>DiceManager</c> arasında 3 ayrı switch'e dağılmıştı (biri eski
    /// <c>CardConstants</c> kullanıyordu, biri küçük-harf literal string kullanıyordu ve
    /// <c>money</c> dalını hiç içermiyordu). <see cref="TryResolve"/> artık string→stat
    /// çözümünün TEK yeridir.
    ///
    /// Saf C# sınıfıdır (MonoBehaviour DEĞİL) — <see cref="GameStateMachine"/> gibi,
    /// sahibi (Katman 3'te <c>GameManager</c>) bir örnek tutar ve <c>new</c> ile oluşturur.
    /// Bilinçli olarak <c>GameEvents</c>'e bağlanmaz: sınır bilgisi yalnızca dönüş
    /// değeriyle (<see cref="StatBoundary"/>) verilir, event fırlatmak çağıranın işidir.
    /// Bu, servisi Unity statik'lerinden bağımsız ve test edilebilir tutar.
    ///
    /// Not: Bu sınıf şu an "dormant"tır — hiçbir call-site (GameManager,
    /// RequirementService, DiceManager) henüz buna bağlanmadı. Bağlama Katman 3/4'te
    /// yapılacak (bkz. <c>docs/REFACTOR_PLAN.md</c>, Katman 2).
    /// </summary>
    public class StatService
    {
        private readonly Dictionary<StatType, int> _stats;
        private readonly int _min;
        private readonly int _max;
        private readonly int _startValue;

        public StatService(int min = 0, int max = 100, int startValue = 50)
        {
            _min = min;
            _max = max;
            _startValue = startValue;

            _stats = new Dictionary<StatType, int>
            {
                { StatType.Form, startValue },
                { StatType.Money, startValue },
                { StatType.Fame, startValue },
                { StatType.Relations, startValue },
                { StatType.Focus, startValue }
            };
        }

        // ── Okuma ────────────────────────────────────────────────────────

        /// <summary>Bir stat'ın güncel değerini döner.</summary>
        public int Get(StatType stat) => _stats[stat];

        /// <summary>
        /// Bir stat'ı isimden (<see cref="GameConstants.Stats"/>) okur.
        /// İsim çözülemezse <see cref="ArgumentException"/> fırlatır.
        /// </summary>
        public int Get(string statName)
        {
            if (!TryResolve(statName, out var stat))
                throw new ArgumentException($"[StatService] Geçersiz stat adı: {statName}", nameof(statName));

            return Get(stat);
        }

        /// <summary>
        /// Bir stat'ı isimden okumayı dener; isim bir stat'a karşılık gelmiyorsa
        /// (ör. PhaseIndex veya bir StoryManager değişkeni ise) <c>false</c> döner.
        /// Fallback zinciri kuran çağıranlar (RequirementService gibi) için.
        /// </summary>
        public bool TryGet(string statName, out int value)
        {
            if (TryResolve(statName, out var stat))
            {
                value = Get(stat);
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>Tüm statların salt-okunur anlık görüntüsü (save/iterasyon için).</summary>
        public IReadOnlyDictionary<StatType, int> All => _stats;

        // ── Yazma (delta) ────────────────────────────────────────────────

        /// <summary>Bir stat'a delta ekler, clamp uygular, sınır bilgisini döner.</summary>
        public StatBoundary Apply(StatType stat, int delta)
        {
            return SetInternal(stat, _stats[stat] + delta);
        }

        /// <summary>İsimden delta uygular. İsim çözülemezse <see cref="ArgumentException"/>.</summary>
        public StatBoundary Apply(string statName, int delta)
        {
            if (!TryResolve(statName, out var stat))
                throw new ArgumentException($"[StatService] Geçersiz stat adı: {statName}", nameof(statName));

            return Apply(stat, delta);
        }

        // ── Yazma (mutlak) ───────────────────────────────────────────────

        /// <summary>Bir stat'ı mutlak değere ayarlar, clamp uygular, sınır bilgisini döner.</summary>
        public StatBoundary Set(StatType stat, int value)
        {
            return SetInternal(stat, value);
        }

        /// <summary>İsimden mutlak değer atar. İsim çözülemezse <see cref="ArgumentException"/>.</summary>
        public StatBoundary Set(string statName, int value)
        {
            if (!TryResolve(statName, out var stat))
                throw new ArgumentException($"[StatService] Geçersiz stat adı: {statName}", nameof(statName));

            return Set(stat, value);
        }

        // ── Toplu ────────────────────────────────────────────────────────

        /// <summary>
        /// Tüm statları belirtilen değere (verilmezse constructor'daki başlangıç
        /// değerine) sıfırlar. Eski <c>GameManager.StartCareer</c>/<c>HandleLifeLost</c>
        /// mantığının karşılığı.
        /// </summary>
        public void ResetAll(int? value = null)
        {
            int resetValue = value ?? _startValue;
            foreach (var stat in Keys)
                _stats[stat] = Clamp(resetValue);
        }

        /// <summary>
        /// Bir <see cref="ChoiceData"/>'nın (kartın sağ/sol seçimi) 5 stat deltasını
        /// tek seferde uygular. Eski <c>GameManager.HandleCardSwiped</c>'teki tekrarlı
        /// 5 satırlık bloğun karşılığı. Sınıra çarpan statları döner (çağıran, Katman
        /// 3'te bunları kriz tetiklemek için kullanacak).
        /// </summary>
        public IReadOnlyList<StatBoundaryHit> ApplyChoice(ChoiceData choice)
        {
            if (choice == null) throw new ArgumentNullException(nameof(choice));

            var hits = new List<StatBoundaryHit>();

            void ApplyAndCollect(StatType stat, int delta)
            {
                var boundary = Apply(stat, delta);
                if (boundary != StatBoundary.None)
                    hits.Add(new StatBoundaryHit(stat, boundary));
            }

            ApplyAndCollect(StatType.Form, choice.form);
            ApplyAndCollect(StatType.Money, choice.money);
            ApplyAndCollect(StatType.Fame, choice.fame);
            ApplyAndCollect(StatType.Relations, choice.relations);
            ApplyAndCollect(StatType.Focus, choice.focus);

            return hits;
        }

        // ── string→StatType TEK çözücü ───────────────────────────────────

        /// <summary>
        /// <see cref="GameConstants.Stats"/> string sabitlerini <see cref="StatType"/>'a
        /// çözen TEK yer. Başka hiçbir dosyada bu eşlemeyi tekrarlayan bir switch
        /// OLMAMALI (eskiden GameManager/RequirementService/DiceManager'da 3 ayrı
        /// vardı — biri "money"yi hiç içermiyordu).
        /// </summary>
        public static bool TryResolve(string statName, out StatType stat)
        {
            switch (statName)
            {
                case GameConstants.Stats.Form: stat = StatType.Form; return true;
                case GameConstants.Stats.Money: stat = StatType.Money; return true;
                case GameConstants.Stats.Fame: stat = StatType.Fame; return true;
                case GameConstants.Stats.Relations: stat = StatType.Relations; return true;
                case GameConstants.Stats.Focus: stat = StatType.Focus; return true;
                default:
                    stat = default;
                    return false;
            }
        }

        // ── Yardımcılar ───────────────────────────────────────────────────

        private static IEnumerable<StatType> Keys => new[]
        {
            StatType.Form, StatType.Money, StatType.Fame, StatType.Relations, StatType.Focus
        };

        private StatBoundary SetInternal(StatType stat, int rawValue)
        {
            int clamped = Clamp(rawValue);
            _stats[stat] = clamped;

            if (clamped <= _min) return StatBoundary.Min;
            if (clamped >= _max) return StatBoundary.Max;
            return StatBoundary.None;
        }

        private int Clamp(int value)
        {
            if (value < _min) return _min;
            if (value > _max) return _max;
            return value;
        }
    }
}
