using TowerDefense.Core;
using TowerDefense.Events;
using UnityEngine;

namespace TowerDefense.Bootstrap
{
    /// <summary>
    /// Minimal on-screen readout driven purely by <see cref="GameEvents"/> — a
    /// stand-in for real UI, and a demonstration that the core raises events
    /// without knowing anything about what listens.
    /// </summary>
    public sealed class DebugHud : MonoBehaviour
    {
        private int _currency;
        private int _baseHp;
        private int _wave;
        private string _endState = string.Empty;

        private void OnEnable()
        {
            GameEvents.OnCurrencyChanged += HandleCurrency;
            GameEvents.OnBaseHpChanged += HandleBaseHp;
            GameEvents.OnWaveStarted += HandleWave;
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnVictory += HandleVictory;
        }

        private void OnDisable()
        {
            GameEvents.OnCurrencyChanged -= HandleCurrency;
            GameEvents.OnBaseHpChanged -= HandleBaseHp;
            GameEvents.OnWaveStarted -= HandleWave;
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnVictory -= HandleVictory;
        }

        private void HandleCurrency(int value) => _currency = value;
        private void HandleBaseHp(int value) => _baseHp = value;
        private void HandleWave(int value) => _wave = value;
        private void HandleGameOver() => _endState = "GAME OVER";
        private void HandleVictory() => _endState = "VICTORY";

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            GUI.Label(new Rect(12, 8, 400, 26), $"Currency: {_currency}", style);
            GUI.Label(new Rect(12, 34, 400, 26), $"Base HP:  {_baseHp}", style);
            GUI.Label(new Rect(12, 60, 400, 26), $"Wave:     {_wave}", style);

            int enemies = GameManager.Instance != null ? GameManager.Instance.Enemies.Count : 0;
            GUI.Label(new Rect(12, 86, 400, 26), $"Enemies:  {enemies}", style);

            if (!string.IsNullOrEmpty(_endState))
            {
                var big = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(0, Screen.height / 2f - 30f, Screen.width, 60f), _endState,
                    new GUIStyle(big) { alignment = TextAnchor.MiddleCenter });
            }
        }
    }
}
