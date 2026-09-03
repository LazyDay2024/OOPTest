using UnityEngine;

namespace TowerDefense.Buffs
{
    /// <summary>
    /// Reduces enemy movement speed by a fixed percentage for a fixed duration.
    /// Produced by SlowTower's projectile on hit.
    /// </summary>
    public sealed class SlowBuff : Buff
    {
        private readonly float _slowPercent; // 0..1, fraction of speed removed

        public SlowBuff(float slowPercent, float duration)
        {
            _slowPercent = Mathf.Clamp01(slowPercent);
            Duration = Mathf.Max(0f, duration);
        }

        public override float SpeedMultiplier => 1f - _slowPercent;
    }
}
