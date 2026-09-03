using System.Collections.Generic;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemies;
using TowerDefense.Events;
using UnityEngine;

namespace TowerDefense.Waves
{
    /// <summary>
    /// Drives enemy spawning. On each wave it flattens every <see cref="SpawnGroup"/>
    /// into a queue and releases one enemy per <see cref="_spawnInterval"/>.
    /// </summary>
    public sealed class WaveManager : MonoBehaviour
    {
        [SerializeField] private List<WaveData> waves = new();
        [SerializeField] private MapManager map;
        [SerializeField] private float firstWaveDelay = 1f;
        [SerializeField] private float delayBetweenWaves = 3f;
        [SerializeField] private bool autoStartWaves = true;

        private int _currentWave = -1;
        private float _spawnInterval = 1f;
        private readonly Queue<EnemyData> _spawnQueue = new();
        private float _spawnTimer;
        private float _nextWaveTimer;
        private bool _waveActive;

        /// <summary>0-based index of the wave in progress / last started.</summary>
        public int CurrentWave => _currentWave;
        public int WaveCount => waves.Count;
        public bool WaveInProgress => _waveActive;

        /// <summary>
        /// True once the final wave has been started and fully cleared (queue
        /// drained and no enemies left alive).
        /// </summary>
        public bool AllWavesComplete =>
            _currentWave >= waves.Count - 1 && !_waveActive && _spawnQueue.Count == 0;

        private void Start()
        {
            if (autoStartWaves)
            {
                _nextWaveTimer = firstWaveDelay;
            }
        }

        private void Update()
        {
            if (_nextWaveTimer > 0f)
            {
                _nextWaveTimer -= Time.deltaTime;
                if (_nextWaveTimer <= 0f)
                {
                    StartNextWave();
                }
            }

            if (!_waveActive)
            {
                return;
            }

            if (_spawnQueue.Count > 0)
            {
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0f)
                {
                    SpawnOne();
                    _spawnTimer = _spawnInterval;
                }
                return;
            }

            // Queue drained: wave ends once the field is clear.
            if (NoLiveEnemies())
            {
                _waveActive = false;
                if (autoStartWaves && _currentWave < waves.Count - 1)
                {
                    _nextWaveTimer = delayBetweenWaves;
                }
            }
        }

        /// <summary>
        /// Advance to the next wave and build its spawn queue from every group.
        /// </summary>
        public void StartNextWave()
        {
            if (_currentWave >= waves.Count - 1 || _waveActive)
            {
                return;
            }

            _currentWave++;
            WaveData wave = waves[_currentWave];
            _spawnInterval = wave.SpawnInterval;
            _spawnQueue.Clear();

            foreach (SpawnGroup group in wave.Groups)
            {
                if (group?.Enemy == null)
                {
                    continue;
                }

                for (int i = 0; i < group.Count; i++)
                {
                    _spawnQueue.Enqueue(group.Enemy);
                }
            }

            _spawnTimer = 0f;
            _waveActive = _spawnQueue.Count > 0;
            GameEvents.RaiseWaveStarted(_currentWave + 1);
        }

        private void SpawnOne()
        {
            EnemyData data = _spawnQueue.Dequeue();
            List<Vector3> path = map.GetPath();
            if (data.Prefab == null || path.Count == 0)
            {
                return;
            }

            GameObject obj = Instantiate(data.Prefab, path[0], Quaternion.identity);
            var enemy = obj.GetComponent<Enemy>();
            if (enemy == null)
            {
                Destroy(obj);
                return;
            }

            enemy.Initialize(data, path);
        }

        private static bool NoLiveEnemies()
        {
            return GameManager.Instance == null || GameManager.Instance.Enemies.Count == 0;
        }

        internal void Configure(List<WaveData> waves, MapManager map)
        {
            this.waves = waves;
            this.map = map;
        }
    }
}
