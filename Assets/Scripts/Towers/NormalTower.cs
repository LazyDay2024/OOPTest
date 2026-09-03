using TowerDefense.Enemies;

namespace TowerDefense.Towers
{
    /// <summary>
    /// Plain damage tower. Fires a projectile that deals <see cref="Tower.damage"/>
    /// with no side effect.
    /// </summary>
    public sealed class NormalTower : Tower
    {
        public override void Shoot(Enemy target)
        {
            FireProjectile(target);
        }
    }
}
