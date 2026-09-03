using UnityEngine;

namespace TowerDefense.Data
{
    /// <summary>
    /// Adds the slow-effect numbers that only a SlowTower needs. Meaningful
    /// inheritance: these fields genuinely belong to the subtype, they are not
    /// duplicated from the parent.
    /// </summary>
    [CreateAssetMenu(menuName = "TD/Slow Tower Data", fileName = "SlowTowerData")]
    public sealed class SlowTowerData : TowerData
    {
        [SerializeField, Range(0f, 1f)] private float slowPercent = 0.35f;
        [SerializeField] private float slowDuration = 2f;

        public float SlowPercent => slowPercent;
        public float SlowDuration => slowDuration;

        internal void ConfigureSlow(float slowPercent, float slowDuration)
        {
            this.slowPercent = slowPercent;
            this.slowDuration = slowDuration;
        }
    }
}
