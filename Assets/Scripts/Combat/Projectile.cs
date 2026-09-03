using System;
using TowerDefense.Core;
using TowerDefense.Enemies;
using UnityEngine;

namespace TowerDefense.Combat
{
    /// <summary>
    /// Homing shot. Carries its own damage value and hit behaviour. Checks the
    /// target every frame because the enemy may die (or be despawned) before the
    /// shot lands.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float hitRadius = 0.15f;
        [SerializeField] private float maxLifetime = 5f;

        private Enemy _target;
        private float _damage;
        private Action _onHitExtra;
        private bool _consumed;
        private float _age;

        /// <param name="target">Enemy to chase.</param>
        /// <param name="damage">Damage dealt on contact.</param>
        /// <param name="onHitExtra">Optional side effect on hit (e.g. apply a slow buff).</param>
        public void Init(Enemy target, float damage, Action onHitExtra = null)
        {
            _target = target;
            _damage = damage;
            _onHitExtra = onHitExtra;
            _consumed = false;
            _age = 0f;
        }

        private void Update()
        {
            if (_consumed)
            {
                return;
            }

            _age += Time.deltaTime;
            if (_age >= maxLifetime)
            {
                Despawn();
                return;
            }

            if (_target == null || !_target.Alive)
            {
                Despawn();
                return;
            }

            Vector3 targetPos = _target.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) <= hitRadius)
            {
                OnHit();
            }
        }

        private void OnHit()
        {
            _consumed = true;

            if (_target != null && _target.Alive)
            {
                _target.TakeDamage(_damage);
            }

            _onHitExtra?.Invoke();
            Despawn();
        }

        private void Despawn()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UnregisterProjectile(this);
            }

            Destroy(gameObject);
        }
    }
}
