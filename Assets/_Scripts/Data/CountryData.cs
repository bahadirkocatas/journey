using System.Collections.Generic;

namespace Game.Data
{
    /// <summary>
    /// Bir ülkeyi (uyruğu) temsil eder. Ülke seçim ekranında, transfer sisteminde ve
    /// kart metinlerinde (winHeadline, rivalCountryID vb.) kullanılır.
    ///
    /// JSON ile birebir uyumludur (<c>countries.json</c>).
    /// </summary>
    [System.Serializable]
    public class CountryData
    {
        /// <summary>Benzersiz kimlik ("turkey", "england" gibi).</summary>
        public string id;

        /// <summary>Görünen ülke adı.</summary>
        public string name;

        /// <summary>Bayrak sprite'ının Resources/Flags altındaki dosya adı.</summary>
        public string flagIconName;

        /// <summary>Forma/tema birincil rengi (hex, "#RRGGBB").</summary>
        public string primaryColor;

        /// <summary>Forma/tema ikincil rengi (hex, "#RRGGBB").</summary>
        public string secondaryColor;

        /// <summary>Dünya Kupası şampiyonluk sayısı (designer verisi, henüz UI'da kullanılmıyor).</summary>
        public int worldCupsWon;

        /// <summary>Lig seviyesi (designer verisi, henüz UI'da kullanılmıyor).</summary>
        public int leagueTier;

        /// <summary>
        /// Ülke seçim ekranında oynanabilir mi (filtre bayrağı).
        /// NOT: Adı yanıltıcı olabilir — silinen Origin/Story seçim sistemiyle
        /// (bkz. REFACTOR_PLAN.md madde 3) hiçbir ilgisi yoktur, sadece
        /// <see cref="CountryManager"/>'ın seçilebilir ülkeleri filtrelemesi için kullanılır.
        /// </summary>
        public bool isPlayableOrigin;

        /// <summary>Ülke Ligi Kalite Puanı (Global Transfer için). Örn. İngiltere: 100, Türkiye: 70.</summary>
        public int leagueQualityScore;

        /// <summary>Rakip ülkenin id'si (designer verisi, henüz UI'da kullanılmıyor).</summary>
        public string rivalCountryID;

        /// <summary>Oynanış stili etiketi (designer verisi, henüz UI'da kullanılmıyor).</summary>
        public string playStyle;

        /// <summary>Bu ülkeyle Dünya Kupası kazanınca gösterilecek manşet metni.</summary>
        public string winHeadline;
    }

    /// <summary>JSON kökü — <c>countries.json</c>'daki "countries" dizisini taşır.</summary>
    [System.Serializable]
    public class CountryCollection
    {
        public List<CountryData> countries;
    }
}
