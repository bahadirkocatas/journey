using System.Collections.Generic;

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
            case CardConstants.ReqTypeFlag: return EvaluateFlag(req);
            case CardConstants.ReqTypeInt:
            case CardConstants.ReqTypeRelation: return EvaluateInt(req);
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
            case CardConstants.StatPhaseIndex: return GameManager.Instance.currentPhaseIndex;
            case CardConstants.StatForm: return GameManager.Instance.form;
            case CardConstants.StatMoney: return GameManager.Instance.money;
            case CardConstants.StatFame: return GameManager.Instance.fame;
            case CardConstants.StatRelations: return GameManager.Instance.relations;
            case CardConstants.StatFocus: return GameManager.Instance.focus;
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