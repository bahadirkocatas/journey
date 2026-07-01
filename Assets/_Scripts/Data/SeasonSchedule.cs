using System.Collections.Generic;

namespace Game.Data
{
    /// <summary>
    /// Belirli bir sezona bağlı, zamanlanmış kart enjeksiyonu (schedule verisinden
    /// okunur). <see cref="SeasonManager"/> her sezon başında ilgili kaydı arayıp
    /// <c>storyStarterIDs</c>'deki kartları desteye enjekte eder.
    /// </summary>
    [System.Serializable]
    public class SeasonSchedule
    {
        /// <summary>Bu programın uygulanacağı sezon indeksi.</summary>
        public int seasonNumber;

        /// <summary>
        /// Sezon başında enjekte edilecek kart ID'leri (<c>CardManager.GetCardByID</c>
        /// ile çözülür). Silinen Origin/Story sistemiyle (bkz. REFACTOR_PLAN.md madde 3)
        /// **ilgisiz** — bu alan MVP-sonrası planlanan hafızalı zincir kartları
        /// (madde 4) için bir kanca; içeriği <c>stories.json</c> değil, kart ID'leridir.
        /// </summary>
        public List<string> storyStarterIDs;
    }

    /// <summary>JSON kökü — schedule verisindeki "schedules" dizisini taşır.</summary>
    [System.Serializable]
    public class ScheduleCollection
    {
        public List<SeasonSchedule> schedules;
    }
}
