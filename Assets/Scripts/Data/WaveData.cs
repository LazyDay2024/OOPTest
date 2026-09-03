using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Data
{
    /// <summary>One contiguous block of identical enemies inside a wave.</summary>
    [System.Serializable]
    public sealed class SpawnGroup
    {
        [SerializeField] private EnemyData enemy;
        [SerializeField, Min(1)] private int count = 1;

        public EnemyData Enemy => enemy;
        public int Count => count;

        public SpawnGroup() { }

        public SpawnGroup(EnemyData enemy, int count)
        {
            this.enemy = enemy;
            this.count = count;
        }
    }

    /// <summary>
    /// Ordered list of spawn groups plus the delay between individual spawns.
    /// </summary>
    [CreateAssetMenu(menuName = "TD/Wave Data", fileName = "WaveData")]
    public sealed class WaveData : ScriptableObject
    {
        [SerializeField] private List<SpawnGroup> groups = new();
        [SerializeField, Min(0.05f)] private float spawnInterval = 1f;

        public IReadOnlyList<SpawnGroup> Groups => groups;
        public float SpawnInterval => spawnInterval;

        internal void Configure(List<SpawnGroup> groups, float spawnInterval)
        {
            this.groups = groups;
            this.spawnInterval = spawnInterval;
        }
    }
}
