namespace Game.Data
{
    /// <summary>
    /// Bir sezonun kapanışında arşivlenen özet kayıt. JSON'dan yüklenmez;
    /// <c>StatsManager.ArchiveCurrentSeason</c> tarafından runtime'da üretilir ve
    /// <c>StatsManager.careerHistory</c> listesine eklenir. Kariyer özeti ekranlarında
    /// (<c>CareerSummaryUI</c>, <c>DashboardManager</c>) geçmiş sezonları listelemek
    /// için kullanılır.
    /// </summary>
    [System.Serializable]
    public class SeasonHistoryData
    {
        /// <summary>Sezon sonundaki oyuncu yaşı.</summary>
        public int age;

        /// <summary>Sezonun oynandığı takımın adı.</summary>
        public string teamName;

        /// <summary>Sezon boyunca atılan toplam gol.</summary>
        public int goals;

        /// <summary>Sezon boyunca yapılan toplam asist.</summary>
        public int assists;

        /// <summary>Sezon ortalama maç reytingi.</summary>
        public float rating;
    }
}
