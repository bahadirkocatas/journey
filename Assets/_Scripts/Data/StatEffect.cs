namespace Game.Data
{
    /// <summary>
    /// Maç kartlarında oynanan bir seçimin istatistiksel sonucu (gol/asist/reyting).
    ///
    /// 5 kaynaklı genel stat etkisinden (Form/Money/Fame/Relations/Focus) ayrı
    /// tutulur; sadece maç içi kartlarda anlamlıdır.
    /// </summary>
    [System.Serializable]
    public class StatEffect
    {
        public int goals;
        public int assists;
        public float matchRating;
    }
}
