using System.Collections.Generic;

namespace Game.Data
{
    /// <summary>
    /// Tek bir koþul (logic gate). Bir kartýn görünmesi, bir seçeneðin kilitli olmasý veya
    /// bir riskin baþarýlý sayýlmasý için kullanýlýr.
    ///
    /// JSON ile birebir uyumludur; designer elle yazdýðý için alanlar bilinçli olarak string.
    /// </summary>
    [System.Serializable]
    public class RequirementData
    {
        /// <summary>"Flag" | "Int" (statlar/deðiþkenler) | "Relation" (NPC iliþkisi)</summary>
        public string type;

        /// <summary>"mafya_dostu" (flag), "Form"/"Focus" (stat), "Alex" (NPC adý) ...</summary>
        public string key;

        /// <summary>"==", "!=", ">", "<", ">=", "<="</summary>
        public string comparison;

        /// <summary>Sayýsal karþýlaþtýrma deðeri (Int / Relation için).</summary>
        public int valueInt;

        /// <summary>Flag karþýlaþtýrma deðeri ("true"/"false" gibi).</summary>
        public string valueString;
    }
}