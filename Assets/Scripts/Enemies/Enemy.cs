using System.Collections.Generic;
using TowerDefense.Buffs;
using TowerDefense.Core;
using TowerDefense.Data;
using UnityEngine;

namespace TowerDefense.Enemies
{
    /// <summary>
    /// Abstract walker that follows the map path and can be damaged and buffed.
    /// Every concrete subtype shares this exact behaviour. In particular
    /// <see cref="TakeDamage"/> is identical for all subtypes: there is no hidden
    /// "this enemy ignores damage" branch. Whether a given tower is allowed to
    /// hit a given enemy is decided earlier, in Tower.FindTarget().
    /// </summary>
    public abstract class Enemy : MonoBehaviour
    {
        [SerializeField] private float hp;
        [SerializeField] private float speed;
        [SerializeField] private int reward;
        [SerializeField] private int leakDamage = 1;

        private float _maxHp;
        private float _baseSpeed;
        private int _pathIndex;
        private List<Vector3> _path;
        private readonly List<Buff> _buffs = new();

        /// <summary>Travel plane. Assigned by the concrete subclass in Awake, never here.</summary>
        public EnemyLayer Layer { get; protected set; }

        public bool Alive { get; private set; }
        public float Hp => hp;
        public float MaxHp => _maxHp;
        public float Speed => speed;              // effective speed after buffs
        public int Reward => reward;
        public int LeakDamage => leakDamage;
        public IReadOnlyList<Buff> Buffs => _buffs;

        /// <summary>Extra vertical offset applied to the path target (cosmetic only).</summary>
        protected virtual float PathHeightOffset => 0f;

        /// <summary>Populate a freshly instantiated enemy from its data asset.</summary>
        public void Initialize(EnemyData data, List<Vector3> path)
        {
            _maxHp = data.Hp;
            hp = data.Hp;
            _baseSpeed = data.Speed;
            speed = data.Speed;
            reward = data.Reward;
            leakDamage = data.LeakDamage;
            _path = path;
            _pathIndex = 0;
            Alive = true;

            if (_path != null && _path.Count > 0)
            {
                transform.position = _path[0] + Vector3.up * PathHeightOffset;
            }
        }

        protected virtual void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterEnemy(this);
            }
        }

        protected virtual void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UnregisterEnemy(this);
            }
        }

        protected virtual void Update()
        {
            if (!Alive)
            {
                return;
            }

            TickBuffs(Time.deltaTime);
            Move();
        }

        // ---- Damage ---------------------------------------------------------

        /// <summary>
        /// Apply raw damage. Same for every subtype. Override is allowed only for
        /// a genuine mechanic (e.g. armour that scales damage), never to make an
        /// enemy that shouldn't have been targeted shrug the hit off.
        /// </summary>
        public virtual void TakeDamage(float amount)
        {
            if (!Alive)
            {
                return;
            }

            hp -= Mathf.Max(0f, amount);
            if (hp <= 0f)
            {
                Die(grantReward: true);
            }
        }

        // ---- Movement -----------------------------------------------------

        public virtual void Move()
        {
            if (_path == null || _path.Count == 0)
            {
                return;
            }

            if (_pathIndex >= _path.Count)
            {
                ReachBase();
                return;
            }

            Vector3 target = _path[_pathIndex] + Vector3.up * PathHeightOffset;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                _pathIndex++;
            }
        }

        private void ReachBase()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ApplyLeak(this);
            }

            Die(grantReward: false);
        }

        // ---- Buffs ------------------------------------------------------------

        /// <summary>
        /// Attach a buff. Re-applying the same buff type refreshes its timer
        /// instead of stacking a second copy.
        /// </summary>
        public void ApplyBuff(Buff buff)
        {
            if (buff == null || !Alive)
            {
                return;
            }

            for (int i = 0; i < _buffs.Count; i++)
            {
                if (_buffs[i].GetType() == buff.GetType())
                {
                    _buffs[i].Refresh();
                    RecalculateStats();
                    return;
                }
            }

            _buffs.Add(buff);
            buff.OnApply(this);
            RecalculateStats();
        }

        private void TickBuffs(float deltaTime)
        {
            if (_buffs.Count == 0)
            {
                return;
            }

            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                _buffs[i].Tick(deltaTime, this);
                if (_buffs[i].IsExpired)
                {
                    _buffs[i].OnRemove(this);
                    _buffs.RemoveAt(i);
                }
            }

            RecalculateStats();
        }

        private void RecalculateStats()
        {
            float multiplier = 1f;
            for (int i = 0; i < _buffs.Count; i++)
            {
                multiplier *= _buffs[i].SpeedMultiplier;
            }

            speed = _baseSpeed * multiplier;
        }

        // ---- Lifetime -------------------------------------------------------

        private void Die(bool grantReward)
        {
            if (!Alive)
            {
                return;
            }

            Alive = false;

            if (grantReward && GameManager.Instance != null)
            {
                GameManager.Instance.GiveReward(reward);
            }

            Destroy(gameObject);
        }
    }
}
