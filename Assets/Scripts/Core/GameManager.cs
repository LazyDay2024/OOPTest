using System.Collections.Generic;
using TowerDefense.Combat;
using TowerDefense.Enemies;
using TowerDefense.Events;
using TowerDefense.Towers;
using TowerDefense.Waves;
using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Central coordinator and lightweight service locator. Holds the live lists
    /// of towers / enemies / projectiles, tracks base HP, routes rewards to the
    /// Player, and decides win / loss each frame.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private int baseHp = 20;
        [SerializeField] private Player player;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private MapManager map;

        private readonly List<Tower> _towers = new();
        private readonly List<Enemy> _enemies = new();
        private readonly List<Projectile> _projectiles = new();

        private bool _isGameOver;
        private bool _isVictory;

        public int BaseHp => baseHp;
        public bool IsGameOver => _isGameOver;
        public bool IsVictory => _isVictory;
        public Player Player => player;
        public MapManager Map => map;
        public WaveManager Waves => waveManager;

        public IReadOnlyList<Tower> Towers => _towers;
        public IReadOnlyList<Enemy> Enemies => _enemies;
        public IReadOnlyList<Projectile> Projectiles => _projectiles;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            GameEvents.RaiseBaseHpChanged(baseHp);
        }

        private void Update()
        {
            CheckGameState();
        }

        // ---- Registration ------------------------------------------------------

        public void RegisterTower(Tower t) { if (t != null && !_towers.Contains(t)) _towers.Add(t); }
        public void UnregisterTower(Tower t) { _towers.Remove(t); }

        public void RegisterEnemy(Enemy e) { if (e != null && !_enemies.Contains(e)) _enemies.Add(e); }
        public void UnregisterEnemy(Enemy e) { _enemies.Remove(e); }

        public void RegisterProjectile(Projectile p) { if (p != null && !_projectiles.Contains(p)) _projectiles.Add(p); }
        public void UnregisterProjectile(Projectile p) { _projectiles.Remove(p); }

        // ---- Gameplay hooks --------------------------------------------------

        /// <summary>Award currency for a kill. Called by Enemy on death.</summary>
        public void GiveReward(int amount)
        {
            if (player != null)
            {
                player.AddCurrency(amount);
            }
        }

        /// <summary>An enemy reached the base. Reduce base HP by its leak damage.</summary>
        public void ApplyLeak(Enemy enemy)
        {
            int dmg = enemy != null ? Mathf.Max(1, enemy.LeakDamage) : 1;
            baseHp = Mathf.Max(0, baseHp - dmg);
            GameEvents.RaiseBaseHpChanged(baseHp);
        }

        // ---- Win / loss ----------------------------------------------------

        private void CheckGameState()
        {
            if (baseHp <= 0 && !_isGameOver)
            {
                _isGameOver = true;
                GameEvents.RaiseGameOver();
                return;
            }

            bool allWavesDone = waveManager == null || waveManager.AllWavesComplete;
            if (allWavesDone && _enemies.Count == 0 && !_isVictory && !_isGameOver)
            {
                _isVictory = true;
                GameEvents.RaiseVictory();
            }
        }

        internal void Configure(Player player, WaveManager waveManager, MapManager map, int baseHp)
        {
            this.player = player;
            this.waveManager = waveManager;
            this.map = map;
            this.baseHp = baseHp;
        }
    }
}
