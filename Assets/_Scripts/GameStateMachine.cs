using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Oyunun yüksek seviye durumunu yöneten merkezi state machine.
    /// Saf C# sınıfıdır (MonoBehaviour değil); sahibi (GameManager) bir örnek tutar ve
    /// durum değişimlerini <see cref="GameEvents.OnGameStateChanged"/> üzerinden yayınlar.
    ///
    /// Amaç: "Şu an kriz mi, final mi, oyun bitti mi?" sorusunun TEK bir doğru kaynağı olması.
    /// </summary>
    public class GameStateMachine
    {
        public GameState Current { get; private set; } = GameState.Boot;
        public GameState Previous { get; private set; } = GameState.Boot;

        /// <summary>(önceki, yeni) durum. GameManager bunu GameEvents'e köprüler.</summary>
        public event Action<GameState, GameState> OnStateChanged;

        public bool Is(GameState state) => Current == state;

        /// <summary>Kartların çözülebildiği (swipe edilip etkilerinin uygulanabildiği) durumlar.</summary>
        public bool CanResolveCards =>
            Current == GameState.Playing ||
            Current == GameState.Crisis ||
            Current == GameState.UclFinal;

        public void Set(GameState next)
        {
            if (Current == next) return;

            if (!IsTransitionAllowed(Current, next))
            {
                Debug.LogWarning($"[GameStateMachine] Geçersiz geçiş engellendi: {Current} -> {next}");
                return;
            }

            Previous = Current;
            Current = next;
            OnStateChanged?.Invoke(Previous, Current);
        }

        /// <summary>
        /// Tek gerçek değişmez (invariant): GameOver terminal bir durumdur; oradan sadece
        /// kariyer özetine veya (yeniden başlamak için) ana menüye geçilebilir.
        /// Diğer tüm geçişlere izin verilir (MVP'de aşırı kısıtlama ters teper).
        /// </summary>
        private static bool IsTransitionAllowed(GameState from, GameState to)
        {
            if (from == GameState.GameOver)
                return to == GameState.CareerSummary || to == GameState.MainMenu;

            return true;
        }
    }
}
