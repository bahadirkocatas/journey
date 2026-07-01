using System.Collections.Generic;
using Game.Core;
using Game.Data;

namespace Game.Services
{
    /// <summary>
    /// Bir kartın/seçeneğin görünürlük koşullarını (<see cref="RequirementData"/>) ve
    /// bekleme süresini (cooldown) değerlendiren servis. Sonuçları kart bazında
    /// cache'ler (<see cref="ClearCache"/> sahibi — Katman 3'te <c>CardManager</c> —
    /// tarafından ilgili state değiştiğinde çağrılmalı).
    ///
    /// Not (Katman 3'e ertelendi): <see cref="GetCurrentInt"/> hâlâ
    /// <c>GameManager</c>'ın düz alanlarını (form/money/...) okuyor. <c>StatService</c>
    /// henüz "dormant" (bkz. <c>Services/StatService.cs</c>) — canlı bir sahibi yok.
    /// Değer kaynağı, <c>GameManager</c> bir <c>StatService</c> örneğini
    /// sahiplendiğinde (Katman 3) atomik olarak <c>StatService.TryGet</c>'e taşınacak.
    /// </summary>
    public class RequirementService
    {
        private readonly Dictionary<string, bool> _cache = new Dictionary<string, bool>();

        public bool CheckRequirements(List<RequirementData> reqs, string cacheKey = "")
        {
            if (reqs == null || reqs.Count == 0) return true;

            if (!string.IsNullOrEmpty(cacheKey) && _cache.TryGetValue(cacheKey, out bool cached))
                return cached;

            bool result = EvaluateAll(reqs);
            if (!string.IsNullOrEmpty(cacheKey)) _cache[cacheKey] = result;
            return result;
        }

        /// <summary>
        /// Bir kartın deste havuzunda/kuyrukta seçilebilir olup olmadığını belirler:
        /// requirement'ları geçmeli VE cooldown'da olmamalı. Cooldown her çağrıda canlı
        /// değerlendirilir (cache'e girmez).
        ///
        /// Not: <c>successCardID</c> ile zorlanan zincir devam kartları bu metottan
        /// GEÇMEZ — onlar bilinçli olarak <see cref="CheckRequirements"/>'ı doğrudan
        /// kullanır ve cooldown'ı bypass eder (anlatı akışı kesilmesin diye).
        /// </summary>
        public bool IsCardEligible(CardData card)
        {
            if (card == null) return false;
            if (StoryManager.Instance != null && StoryManager.Instance.IsOnCooldown(card.id))
                return false;

            return CheckRequirements(card.requirements, $"card_{card.id}");
        }

        public void ClearCache() => _cache.Clear();

        private bool EvaluateAll(List<RequirementData> reqs)
        {
            foreach (var req in reqs)
                if (!EvaluateSingle(req)) return false;
            return true;
        }

        private bool EvaluateSingle(RequirementData req)
        {
            switch (req.type)
            {
                case GameConstants.ReqTypes.Flag: return EvaluateFlag(req);
                case GameConstants.ReqTypes.Int:
                case GameConstants.ReqTypes.Relation: return EvaluateInt(req);
                default: return true;
            }
        }

        private bool EvaluateFlag(RequirementData req)
        {
            bool hasFlag = StoryManager.Instance != null && StoryManager.Instance.HasFlag(req.key);
            bool expected = string.IsNullOrEmpty(req.valueString) || req.valueString.ToLower() == "true";
            return CompareBool(hasFlag, expected, req.comparison);
        }

        private bool EvaluateInt(RequirementData req)
        {
            int val = GetCurrentInt(req.key);
            return CompareInt(val, req.valueInt, req.comparison);
        }

        private int GetCurrentInt(string key)
        {
            if (GameManager.Instance == null) return 0;
            switch (key)
            {
                case GameConstants.Stats.PhaseIndex: return GameManager.Instance.currentPhaseIndex;
                case GameConstants.Stats.Form: return GameManager.Instance.form;
                case GameConstants.Stats.Money: return GameManager.Instance.money;
                case GameConstants.Stats.Fame: return GameManager.Instance.fame;
                case GameConstants.Stats.Relations: return GameManager.Instance.relations;
                case GameConstants.Stats.Focus: return GameManager.Instance.focus;
                default: return StoryManager.Instance?.GetVariable(key) ?? 0;
            }
        }

        private bool CompareBool(bool a, bool b, string op)
        {
            op = string.IsNullOrEmpty(op) ? "==" : op;
            return op == "==" ? a == b : a != b;
        }

        private bool CompareInt(int a, int b, string op)
        {
            op = string.IsNullOrEmpty(op) ? "==" : op;
            return op switch
            {
                ">" => a > b,
                "<" => a < b,
                ">=" => a >= b,
                "<=" => a <= b,
                "!=" => a != b,
                _ => a == b
            };
        }
    }
}
