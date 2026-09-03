using TowerDefense.Data;

namespace TowerDefense.Enemies
{
    /// <summary>
    /// Ground unit. Adds nothing to <see cref="Enemy"/> except declaring its
    /// travel plane.
    /// </summary>
    public sealed class GndEnemy : Enemy
    {
        private void Awake()
        {
            Layer = EnemyLayer.Ground;
        }
    }
}
