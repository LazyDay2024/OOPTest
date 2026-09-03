using TowerDefense.Buffs;
using TowerDefense.Data;
using TowerDefense.Enemies;
using UnityEngine;

namespace TowerDefense.Towers
{
    /// <summary>
    /// Fires a projectile that also applies a <see cref="SlowBuff"/> on hit.
    /// <see cref="slowPercent"/> / <see cref="slowDuration"/> are this tower's own
    /// state, not duplicated from the base.
    /// </summary>
    public sealed class SlowTower : Tower
    {
        [SerializeField, Range(0f, 1f)] private float slowPercent = 0.35f;
        [SerializeField] private float slowDuration = 2f;

        public float SlowPercent => slowPercent;
        public float SlowDuration => slowDuration;

        public override void Initialize(TowerData data)
        {
            base.Initialize(data);
            if (data is SlowTowerData slowData)
            {
                slowPercent = slowData.SlowPercent;
                slowDuration = slowData.SlowDuration;
            }
        }

        public override void Shoot(Enemy target)
        {
            // Capture locals so the closure does not depend on mutable fields.
            float percent = slowPercent;
            float duration = slowDuration;
            Enemy victim = target;

            FireProjectile(target, onHitExtra: () =>
            {
                if (victim != null && victim.Alive)
                {
                    victim.ApplyBuff(new SlowBuff(percent, duration));
                }
            });
        }
    }
}
