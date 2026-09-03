using UnityEngine;

namespace TowerDefense.Data
{
    /// <summary>
    /// Balance sheet for a tower type. <see cref="SlowTowerData"/> extends this
    /// with slow-specific numbers instead of the base carrying fields it does
    /// not use.
    /// </summary>
    [CreateAssetMenu(menuName = "TD/Tower Data", fileName = "TowerData")]
    public class TowerData : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float range = 5f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float cooldown = 1f;
        [SerializeField] private int cost = 50;
        [SerializeField] private TargetLayer targetLayer = TargetLayer.Ground;

        public GameObject Prefab => prefab;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float Range => range;
        public float Damage => damage;
        public float Cooldown => cooldown;
        public int Cost => cost;
        public TargetLayer TargetLayer => targetLayer;

        internal void Configure(GameObject prefab, GameObject projectilePrefab, float range,
            float damage, float cooldown, int cost, TargetLayer targetLayer)
        {
            this.prefab = prefab;
            this.projectilePrefab = projectilePrefab;
            this.range = range;
            this.damage = damage;
            this.cooldown = cooldown;
            this.cost = cost;
            this.targetLayer = targetLayer;
        }
    }
}
