using System;

namespace TowerDefense.Data
{
    /// <summary>
    /// Which enemy planes a tower is allowed to fire at. Declared as [Flags] so a
    /// tower can cover Ground only, Air only, or Both.
    /// </summary>
    [Flags]
    public enum TargetLayer
    {
        None = 0,
        Ground = 1,
        Air = 2,
        Both = Ground | Air
    }

    public static class TargetLayerExtensions
    {
        /// <summary>
        /// True when a tower with this <see cref="TargetLayer"/> is permitted to
        /// target an enemy on the given <see cref="EnemyLayer"/>.
        /// This is the single place layer eligibility is decided (LSP fix):
        /// enemies never silently ignore damage in TakeDamage().
        /// </summary>
        public static bool CanTarget(this TargetLayer mask, EnemyLayer enemyLayer)
        {
            TargetLayer bit = (TargetLayer)(1 << (int)enemyLayer);
            return (mask & bit) != 0;
        }
    }
}
