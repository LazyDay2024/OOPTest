using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemies;
using TowerDefense.Towers;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    /// <summary>
    /// End-to-end proof of the LSP fix: a ground-only tower never selects an air
    /// enemy — the filtering happens in Tower.FindTarget(), and Enemy.TakeDamage()
    /// stays identical for every subtype.
    /// </summary>
    public sealed class LspTargetingTests
    {
        private readonly List<GameObject> _spawned = new();
        private GameManager _gm;

        [SetUp]
        public void SetUp()
        {
            var gmGo = New("GameManager");
            _gm = gmGo.AddComponent<GameManager>();     // Awake sets GameManager.Instance
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _spawned.Clear();
        }

        [Test]
        public void GroundTower_NeverTargets_AirEnemy_EvenWhenAirIsCloser()
        {
            Enemy ground = SpawnEnemy<GndEnemy>(EnemyLayer.Ground, new Vector3(3f, 0f, 0f));
            Enemy air = SpawnEnemy<AirEnemy>(EnemyLayer.Air, new Vector3(1f, 0f, 0f)); // closer

            Tower tower = SpawnTower<NormalTower>(TargetLayer.Ground, Vector3.zero, range: 50f);

            Enemy picked = tower.FindTarget();

            Assert.AreSame(ground, picked, "ground-only tower must pick the ground enemy");
            Assert.AreNotSame(air, picked, "ground-only tower must never pick the air enemy");
        }

        [Test]
        public void BothTower_CanTarget_AirEnemy()
        {
            SpawnEnemy<GndEnemy>(EnemyLayer.Ground, new Vector3(9f, 0f, 0f));
            Enemy air = SpawnEnemy<AirEnemy>(EnemyLayer.Air, new Vector3(1f, 0f, 0f));

            Tower tower = SpawnTower<NormalTower>(TargetLayer.Both, Vector3.zero, range: 50f);

            Assert.AreSame(air, tower.FindTarget());
        }

        [Test]
        public void TakeDamage_BehavesIdentically_ForGroundAndAir()
        {
            Enemy ground = SpawnEnemy<GndEnemy>(EnemyLayer.Ground, Vector3.zero, hp: 10f);
            Enemy air = SpawnEnemy<AirEnemy>(EnemyLayer.Air, Vector3.zero, hp: 10f);

            ground.TakeDamage(10f);
            air.TakeDamage(10f);

            Assert.IsFalse(ground.Alive, "ground enemy should die at 0 hp");
            Assert.IsFalse(air.Alive, "air enemy should die at 0 hp on the exact same call");
        }

        [UnityTest]
        public IEnumerator EnemyReachingEndOfPath_ReducesBaseHp()
        {
            _gm.Configure(null, null, null, baseHp: 5);

            Enemy runner = SpawnEnemy<GndEnemy>(EnemyLayer.Ground, Vector3.zero, hp: 999f, speed: 50f,
                path: new List<Vector3> { new(0f, 0f, 0f), new(0f, 0f, 1f) });

            float timeout = 3f;
            while (runner != null && runner.Alive && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.Less(_gm.BaseHp, 5, "base HP must drop when an enemy leaks through");
        }

        // ---- helpers -------------------------------------------------------

        private GameObject New(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private Enemy SpawnEnemy<T>(EnemyLayer expectedLayer, Vector3 position, float hp = 100f,
            float speed = 1f, List<Vector3> path = null) where T : Enemy
        {
            var go = New(typeof(T).Name);
            var enemy = go.AddComponent<T>();          // Awake sets Layer

            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.Configure(null, hp, speed, 1, 1, expectedLayer);
            enemy.Initialize(data, path);
            go.transform.position = position;

            Assert.AreEqual(expectedLayer, enemy.Layer);
            return enemy;
        }

        private Tower SpawnTower<T>(TargetLayer targetLayer, Vector3 position, float range) where T : Tower
        {
            var go = New(typeof(T).Name);
            go.transform.position = position;
            var tower = go.AddComponent<T>();

            var data = ScriptableObject.CreateInstance<TowerData>();
            data.Configure(null, null, range, 5f, 1f, 10, targetLayer);
            tower.Initialize(data);
            return tower;
        }
    }
}
