using System.Collections.Generic;

namespace Game.Data
{
    /// <summary>
    /// Bir futbol takımını temsil eder. Transfer sisteminde, ana menü takım seçiminde
    /// ve kart metinlerinde (rivalTeamID vb.) kullanılır.
    ///
    /// JSON ile birebir uyumludur (<c>teams.json</c>).
    /// </summary>
    [System.Serializable]
    public class TeamData
    {
        /// <summary>Benzersiz kimlik ("galatasaray", "fenerbahce" gibi).</summary>
        public string id;

        /// <summary>Görünen takım adı.</summary>
        public string name;

        /// <summary>Kısa ad ("GAL", "FEN" gibi). Skor tabelası için.</summary>
        public string shortName;

        /// <summary>Takımın bağlı olduğu ülkenin id'si (<see cref="CountryData.id"/>).</summary>
        public string countryID;

        /// <summary>Takımın şehri ("Istanbul", "Trabzon" gibi). Derbi kontrolü için.</summary>
        public string city;

        /// <summary>Lig seviyesi (1 = üst seviye).</summary>
        public int tier;

        /// <summary>Forma/tema birincil rengi (hex, "#RRGGBB").</summary>
        public string primaryColor;

        /// <summary>Forma/tema ikincil rengi (hex, "#RRGGBB").</summary>
        public string secondaryColor;

        /// <summary>Transfer bütçesi.</summary>
        public int budget;

        /// <summary>Şampiyonluk sayısı.</summary>
        public int championships;

        /// <summary>Rakip takımın id'si. Derbi kontrolü için.</summary>
        public string rivalTeamID;

        /// <summary>Taraftar kitlesi etiketi (designer verisi).</summary>
        public string fanBase;

        /// <summary>Kulüp kültürü etiketi (designer verisi).</summary>
        public string clubCulture;

        /// <summary>Takım gücü (maç simülasyonunda kullanılır).</summary>
        public int power;
    }

    /// <summary>JSON kökü — <c>teams.json</c>'daki "teams" dizisini taşır.</summary>
    [System.Serializable]
    public class TeamCollection
    {
        public List<TeamData> teams;
    }
}
