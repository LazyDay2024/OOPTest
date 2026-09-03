using TowerDefense.Enemies;

namespace TowerDefense.Buffs
{
    /// <summary>
    /// Base class for a timed status effect attached to an <see cref="Enemy"/>.
    /// Subclasses override only what they change (e.g. <see cref="SpeedMultiplier"/>);
    /// the Enemy applies every buff the same way, so new effects can be added
    /// without touching Enemy (open/closed).
    /// </summary>
    public abstract class Buff
    {
        public float Duration { get; protected set; }
        protected float Elapsed;

        public bool IsExpired => Elapsed >= Duration;

        /// <summary>Multiplicative factor this buff contributes to enemy speed.</summary>
        public virtual float SpeedMultiplier => 1f;

        public virtual void OnApply(Enemy enemy) { }

        public virtual void Tick(float deltaTime, Enemy enemy)
        {
            Elapsed += deltaTime;
        }

        public virtual void OnRemove(Enemy enemy) { }

        /// <summary>
        /// Reset the timer when the same effect is re-applied instead of stacking
        /// a second instance.
        /// </summary>
        public void Refresh()
        {
            Elapsed = 0f;
        }
    }
}
