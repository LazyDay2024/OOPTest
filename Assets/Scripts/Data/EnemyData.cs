using UnityEngine;

namespace TowerDefense.Data
{
    /// <summary>
    /// Immutable balance sheet for one enemy type. Designers tune these numbers in
    /// the Inspector; no gameplay code hardcodes them. Fields are private +
    /// read-only properties so nothing can mutate a shared asset at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "TD/Enemy Data", fileName = "EnemyData")]
    public sealed class EnemyData : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private float hp = 20f;
        [SerializeField] private float speed = 2f;
        [SerializeField] private int reward = 5;
        [SerializeField] private int leakDamage = 1;
        [SerializeField] private EnemyLayer layer = EnemyLayer.Ground;

        public GameObject Prefab => prefab;
        public float Hp => hp;
        public float Speed => speed;
        public int Reward => reward;
        public int LeakDamage => leakDamage;
        public EnemyLayer Layer => layer;

        /// <summary>Editor / bootstrap only: fill a code-created instance.</summary>
        internal void Configure(GameObject prefab, float hp, float speed, int reward,
            int leakDamage, EnemyLayer layer)
        {
            this.prefab = prefab;
            this.hp = hp;
            this.speed = speed;
            this.reward = reward;
            this.leakDamage = leakDamage;
            this.layer = layer;
        }
    }
}
