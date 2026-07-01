namespace Game.Data
{
    /// <summary>
    /// Görünmez (arka planda tutulan) bir değişkeni değiştiren etki.
    ///
    /// Oyuncuya doğrudan gösterilmeyen ilerleme değişkenleri için kullanılır
    /// (örn. "ucl_momentum"). <see cref="StoryManager"/>'ın generic değişken
    /// deposuna yazılır.
    /// </summary>
    [System.Serializable]
    public class HiddenEffect
    {
        /// <summary>Değiştirilecek değişkenin adı. Örn: "ucl_momentum".</summary>
        public string variableName;

        /// <summary>Değişim miktarı. Örn: +1, -1.</summary>
        public int changeAmount;
    }
}
