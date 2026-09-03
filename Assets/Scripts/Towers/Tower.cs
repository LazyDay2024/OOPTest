using System;
using TowerDefense.Combat;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemies;
using UnityEngine;

namespace TowerDefense.Towers
{
    /// <summary>
    /// Abstract turret. Owns targeting, range, cooldown and cost. Concrete towers
    /// only decide what happens when they fire (<see cref="Shoot"/>).
    ///
    /// Targeting note (LSP fix): <see cref="FindTarget"/> is implemented once here
    /// and is the single point where <see cref="TargetLayer"/> eligibility is
    /// enforced. A ground-only tower never selects an air enemy, so no enemy ever
    /// needs to "ignore" a shot it shouldn't have received.
    /// </summary>
    public abstract class Tower : MonoBehaviour
    {
        [SerializeField] protected float range = 5f;
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected float cooldown = 1f;
        [SerializeField] protected int cost = 50;
        [SerializeField] protected TargetLayer targetLayer = TargetLayer.Ground;
        [SerializeField] protected GameObject projectilePrefab;

        [SerializeField] private float upgradeCostFactor = 0.75f;

        private int _level = 1;
        private float _currentCooldown;

        public int Cost => cost;
        public float Range => range;
        public float Damage => damage;
        public int Level => _level;
        public TargetLayer TargetLayer => targetLayer;

        /// <summary>Currency needed for the next <see cref="Upgrade"/>; scales with level.</summary>
        public int UpgradeCost => Mathf.RoundToInt(cost * upgradeCostFactor * _level);

        // "position" in the UML diagram == transform.position (MonoBehaviour provides it)

        /// <summary>Populate a freshly instantiated tower from its data asset.</summary>
        public virtual void Initialize(TowerData data)
        {
            range = data.Range;
            damage = data.Damage;
            cooldown = data.Cooldown;
            cost = data.Cost;
            targetLayer = data.TargetLayer;
            if (data.ProjectilePrefab != null)
            {
                projectilePrefab = data.ProjectilePrefab;
            }
        }

        protected virtual void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterTower(this);
            }
        }

        protected virtual void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UnregisterTower(this);
            }
        }

        protected virtual void Update()
        {
            if (_currentCooldown > 0f)
            {
                _currentCooldown -= Time.deltaTime;
            }

            if (!IsReady())
            {
                return;
            }

            Enemy target = FindTarget();
            if (target != null)
            {
                Shoot(target);
                _currentCooldown = cooldown;
            }
        }

        public bool IsReady() => _currentCooldown <= 0f;

        /// <summary>
        /// Nearest eligible living enemy within range, or null. Shared by all
        /// tower types; subclasses are not expected to override this.
        /// </summary>
        public virtual Enemy FindTarget()
        {
            if (GameManager.Instance == null)
            {
                return null;
            }

            Enemy best = null;
            float bestDist = range;

            var enemies = GameManager.Instance.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (e == null || !e.Alive)
                {
                    continue;
                }

                if (!targetLayer.CanTarget(e.Layer))
                {
                    continue; // <-- layer eligibility enforced here and nowhere else
                }

                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d <= bestDist)
                {
                    best = e;
                    bestDist = d;
                }
            }

            return best;
        }

        /// <summary>Fire at the given (already validated) target.</summary>
        public abstract void Shoot(Enemy target);

        public virtual void Upgrade()
        {
            _level++;
            damage *= 1.25f;
            range *= 1.1f;
        }

        /// <summary>
        /// Shared spawn path for every tower type: instantiate the projectile,
        /// hand it this tower's damage plus an optional on-hit side effect, and
        /// register it with the GameManager.
        /// </summary>
        protected Projectile FireProjectile(Enemy target, Action onHitExtra = null)
        {
            if (target == null || projectilePrefab == null)
            {
                return null;
            }

            GameObject shot = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            var projectile = shot.GetComponent<Projectile>();
            if (projectile == null)
            {
                Destroy(shot);
                return null;
            }

            projectile.Init(target, damage, onHitExtra);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterProjectile(projectile);
            }

            return projectile;
        }
    }
}
