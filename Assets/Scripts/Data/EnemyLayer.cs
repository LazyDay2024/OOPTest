namespace TowerDefense.Data
{
    /// <summary>
    /// Physical plane an enemy travels on. Set once by the concrete Enemy
    /// subclass (see GndEnemy / AirEnemy) and never changed at runtime.
    /// </summary>
    public enum EnemyLayer
    {
        Ground = 0,
        Air = 1
    }
}
