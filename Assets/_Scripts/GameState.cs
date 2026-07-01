namespace Game.Core
{
    /// <summary>
    /// Oyunun yüksek seviye akış durumu. Önceki dağınık boolean bayrakların
    /// (isCrisisActive, isGameOver, isTimeFrozenForUCL, isFinalRunning...) yerini alır.
    /// </summary>
    public enum GameState
    {
        Boot,           // İlk açılış / veri yükleme
        MainMenu,       // Ana menü ve kariyer sihirbazı
        Playing,        // Normal kart akışı
        Crisis,         // Bir stat kritik oldu; kriz kartı çözülmeyi bekliyor
        UclFinal,       // Şampiyonlar Ligi finali akışı (zaman donar)
        Transfer,       // Sezon sonu transfer paneli açık
        GameOver,       // Oyun bitti (can tükendi / emeklilik)
        CareerSummary   // Kariyer özeti gösteriliyor
    }
}
