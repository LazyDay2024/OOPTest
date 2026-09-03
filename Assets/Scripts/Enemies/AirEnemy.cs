using TowerDefense.Data;
using UnityEngine;

namespace TowerDefense.Enemies
{
    /// <summary>
    /// Air unit. Declares its travel plane and flies at a raised height. It does
    /// NOT override <see cref="Enemy.TakeDamage"/>: preventing ground-only towers
    /// from hitting it is Tower.FindTarget()'s job, not this class's.
    /// </summary>
    public sealed class AirEnemy : Enemy
    {
        [SerializeField] private float flyHeight = 1.5f;

        protected override float PathHeightOffset => flyHeight;

        private void Awake()
        {
            Layer = EnemyLayer.Air;
        }
    }
}
