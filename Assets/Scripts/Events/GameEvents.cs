using System;
using UnityEngine;

namespace TowerDefense.Events
{
    /// <summary>
    /// One-way notification hub. Core systems (GameManager, Player, WaveManager)
    /// raise these when state changes; UI or audio can subscribe later in
    /// separate files without the core ever referencing them.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<int> OnCurrencyChanged;
        public static event Action<int> OnBaseHpChanged;
        public static event Action<int> OnWaveStarted;   // 1-based wave number
        public static event Action OnGameOver;
        public static event Action OnVictory;

        public static void RaiseCurrencyChanged(int value) => OnCurrencyChanged?.Invoke(value);
        public static void RaiseBaseHpChanged(int value) => OnBaseHpChanged?.Invoke(value);
        public static void RaiseWaveStarted(int waveNumber) => OnWaveStarted?.Invoke(waveNumber);
        public static void RaiseGameOver() => OnGameOver?.Invoke();
        public static void RaiseVictory() => OnVictory?.Invoke();

        /// <summary>
        /// Static event fields survive Play-mode restarts when Domain Reload is
        /// disabled, which would leak dead subscribers. Clear them on load.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            OnCurrencyChanged = null;
            OnBaseHpChanged = null;
            OnWaveStarted = null;
            OnGameOver = null;
            OnVictory = null;
        }
    }
}
