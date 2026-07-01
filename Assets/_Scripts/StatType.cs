namespace Game.Core
{
    /// <summary>
    /// Oyuncunun 5 kaynağını (resource) temsil eden enum. Eskiden <c>GameManager.cs</c>
    /// içinde global namespace'te tanımlıydı; birden fazla servis (StatService,
    /// RequirementService, DiceManager) kullanacağı için <see cref="Game.Core"/>'a
    /// taşındı. String karşılıkları <see cref="GameConstants.Stats"/> içindedir;
    /// string→enum çözümü tek yerde (<c>Game.Services.StatService.TryResolve</c>)
    /// yapılır, başka hiçbir yerde tekrarlanmamalıdır.
    /// </summary>
    public enum StatType
    {
        Form,
        Money,
        Fame,
        Relations,
        Focus
    }
}
