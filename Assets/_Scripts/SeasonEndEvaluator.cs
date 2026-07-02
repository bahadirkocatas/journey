using Game.Core;

/// <summary>
/// Sezon sonu skorunun UCL/küme düşme ve genel yönetici değerlendirmesine nasıl
/// dönüştüğünü hesaplayan SAF (yan etkisiz) sınıf. Eski <c>SeasonScoring</c>'in yerini
/// alır (bkz. REFACTOR_PLAN.md madde 9) — <c>SeasonScoring</c> doğrudan
/// <c>StoryManager</c>/<c>GameManager</c> üzerinde yan etki uyguluyordu, bu sınıf yalnızca
/// bir <see cref="UclAndRelegationResult"/>/<see cref="ReviewResult"/> döner; uygulamayı
/// çağıran taraf (<c>SeasonManager</c>) yapar.
/// </summary>
public static class SeasonEndEvaluator
{
    /// <summary>
    /// UCL kalifikasyonu / küme düşme değerlendirmesinin sonucu. Hangi flag'in
    /// eklenip/kaldırılacağını ve hangi değişkenlerin set edileceğini taşır — uygulamaz.
    /// </summary>
    public readonly struct UclAndRelegationResult
    {
        /// <summary>
        /// null = <see cref="GameConstants.Flags.UclQualified"/>'a dokunma; true = ekle;
        /// false = kaldır. (Eski kodda küme düşme sınırının altında ama tier zaten en
        /// alttaysa — <c>activeTier &gt;= 3</c> — hiçbir şey yapılmıyordu, bu üçüncü hâl
        /// o davranışı korumak için var.)
        /// </summary>
        public readonly bool? QualifiedForUcl;
        /// <summary><see cref="GameConstants.Vars.TeamTierMod"/> için uygulanacak değişim (0 = dokunma).</summary>
        public readonly int TeamTierModDelta;
        /// <summary><see cref="GameConstants.Vars.TeamPowerMod"/> için set edilecek değer (null = dokunma).</summary>
        public readonly int? TeamPowerModValue;

        public UclAndRelegationResult(bool? qualifiedForUcl, int teamTierModDelta, int? teamPowerModValue)
        {
            QualifiedForUcl = qualifiedForUcl;
            TeamTierModDelta = teamTierModDelta;
            TeamPowerModValue = teamPowerModValue;
        }
    }

    /// <summary>
    /// Genel yönetici değerlendirmesinin (basın/yönetim tepkisi) sonucu. Hangi statların
    /// ne kadar değişeceğini taşır — uygulamaz.
    /// </summary>
    public readonly struct ReviewResult
    {
        public readonly int MoneyDelta;
        public readonly int FameDelta;
        public readonly int RelationsDelta;

        public ReviewResult(int moneyDelta, int fameDelta, int relationsDelta)
        {
            MoneyDelta = moneyDelta;
            FameDelta = fameDelta;
            RelationsDelta = relationsDelta;
        }

        public static readonly ReviewResult None = new ReviewResult(0, 0, 0);
    }

    /// <summary>
    /// Sezon skoruna göre UCL kalifikasyonu / küme düşme durumunu hesaplar (eski
    /// <c>SeasonScoring.EvaluateUCLAndRelegation</c> ile birebir aynı eşik/mantık).
    /// </summary>
    public static UclAndRelegationResult EvaluateUclAndRelegation(
        int seasonScore, int activeTier, int basePower, int uclThreshold, int relegationThreshold)
    {
        if (seasonScore >= uclThreshold)
        {
            if (activeTier == 1)
                return new UclAndRelegationResult(true, 0, null);

            return new UclAndRelegationResult(false, -1, 50 - basePower);
        }

        if (seasonScore <= relegationThreshold)
        {
            if (activeTier < 3)
                return new UclAndRelegationResult(false, 1, 80 - basePower);

            // Eski kod: tier zaten en altta (>= 3) ise hiçbir şeye dokunulmuyordu.
            return new UclAndRelegationResult(null, 0, null);
        }

        return new UclAndRelegationResult(false, 0, null);
    }

    /// <summary>
    /// Sezon skoruna göre basın/yönetim tepkisini hesaplar (eski
    /// <c>SeasonScoring.EvaluateReview</c> ile birebir aynı eşik/mantık).
    /// </summary>
    public static ReviewResult EvaluateReview(int score, int goodReviewThreshold, int badReviewThreshold)
    {
        if (score >= goodReviewThreshold)
            return new ReviewResult(moneyDelta: 10, fameDelta: 5, relationsDelta: 0);

        if (score < badReviewThreshold)
            return new ReviewResult(moneyDelta: 0, fameDelta: -5, relationsDelta: -10);

        return ReviewResult.None;
    }
}
