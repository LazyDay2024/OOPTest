using System.Collections.Generic;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemies;
using TowerDefense.Interaction;
using TowerDefense.Towers;
using TowerDefense.View;
using TowerDefense.Waves;
using UnityEngine;

namespace TowerDefense.Bootstrap
{
    /// <summary>
    /// Drop this single component into an empty scene and press Play. It builds
    /// the whole game in code — path, grid, managers, data assets, primitive
    /// "prefabs" — with no Inspector wiring required. Handy because the project
    /// has to run without opening the editor to author prefabs / ScriptableObjects.
    ///
    /// If a <see cref="GameManager"/> already exists (e.g. a hand-built scene),
    /// this does nothing.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Base / economy")]
        [SerializeField] private int baseHp = 20;
        [SerializeField] private int startingCurrency = 350;

        [Header("Path (world-space waypoints, XZ plane)")]
        [SerializeField]
        private Vector3[] waypoints =
        {
            new(-9f, 0.5f, -2f),
            new(-9f, 0.5f, 4f),
            new(-3f, 0.5f, 4f),
            new(-3f, 0.5f, -3f),
            new(3f, 0.5f, -3f),
            new(3f, 0.5f, 4f),
            new(9f, 0.5f, 4f),
            new(9f, 0.5f, -2f),
        };

        [Header("Demo content")]
        [SerializeField] private bool spawnEnvironment = true;
        [SerializeField] private bool showPathAndBase = true;    // visible road + keep
        [SerializeField] private bool interactiveBuild = true;   // click-to-build / sell UI
        [SerializeField] private bool autoPlaceDemoTowers = false;
        [SerializeField] private bool showDebugHud = true;

        private GameObject _templateRoot;

        private void Awake()
        {
            if (GameManager.Instance != null)
            {
                enabled = false;
                return;
            }

            BuildTemplates(out GameObject gnd, out GameObject air,
                out GameObject normalTower, out GameObject slowTower, out GameObject projectile);

            BuildData(gnd, air, normalTower, slowTower, projectile,
                out EnemyData gndData, out EnemyData airData,
                out TowerData normalData, out SlowTowerData slowData,
                out List<WaveData> waves);

            MapManager map = BuildMap();
            Player player = BuildPlayer(map);
            WaveManager waveManager = BuildWaveManager(waves, map);
            BuildGameManager(player, waveManager, map);

            if (spawnEnvironment)
            {
                BuildEnvironment();
            }

            if (showPathAndBase)
            {
                new GameObject("PathRenderer").AddComponent<PathRenderer>().Configure(map);

                GameObject baseGo = NewInactive("Base", out BaseView baseView);
                baseGo.transform.position = waypoints.Length > 0 ? waypoints[^1] : Vector3.zero;
                baseView.Configure(map);
                baseGo.SetActive(true);
            }

            if (interactiveBuild)
            {
                var bcGo = new GameObject("BuildController");
                var buildController = bcGo.AddComponent<BuildController>();
                buildController.Configure(player, map, Camera.main,
                    new TowerData[] { normalData, slowData });
            }

            if (autoPlaceDemoTowers)
            {
                PlaceDemoTowers(player, normalData, slowData);
            }

            if (showDebugHud && GetComponent<DebugHud>() == null)
            {
                gameObject.AddComponent<DebugHud>();
            }
        }

        // ---- templates -------------------------------------------------------

        private void BuildTemplates(out GameObject gnd, out GameObject air,
            out GameObject normalTower, out GameObject slowTower, out GameObject projectile)
        {
            _templateRoot = PrimitiveFactory.CreateInactiveRoot();
            Transform root = _templateRoot.transform;

            gnd = PrimitiveFactory.CreateEnemyTemplate<GndEnemy>("GndEnemy", new Color(0.85f, 0.3f, 0.3f), root);
            air = PrimitiveFactory.CreateEnemyTemplate<AirEnemy>("AirEnemy", new Color(0.95f, 0.75f, 0.2f), root);
            normalTower = PrimitiveFactory.CreateTowerTemplate<NormalTower>("NormalTower", new Color(0.3f, 0.55f, 0.95f), root);
            slowTower = PrimitiveFactory.CreateTowerTemplate<SlowTower>("SlowTower", new Color(0.35f, 0.85f, 0.9f), root);
            projectile = PrimitiveFactory.CreateProjectileTemplate("Projectile", Color.white, root);
        }

        // ---- data assets (created in memory) --------------------------------

        private void BuildData(GameObject gnd, GameObject air, GameObject normalTower,
            GameObject slowTower, GameObject projectile,
            out EnemyData gndData, out EnemyData airData,
            out TowerData normalData, out SlowTowerData slowData, out List<WaveData> waves)
        {
            gndData = ScriptableObject.CreateInstance<EnemyData>();
            gndData.name = "GndEnemyData";
            gndData.Configure(gnd, hp: 30f, speed: 2.2f, reward: 8, leakDamage: 1, EnemyLayer.Ground);

            airData = ScriptableObject.CreateInstance<EnemyData>();
            airData.name = "AirEnemyData";
            airData.Configure(air, hp: 22f, speed: 3f, reward: 10, leakDamage: 1, EnemyLayer.Air);

            normalData = ScriptableObject.CreateInstance<TowerData>();
            normalData.name = "NormalTowerData";
            normalData.Configure(normalTower, projectile, range: 4.5f, damage: 10f,
                cooldown: 0.7f, cost: 60, TargetLayer.Ground);

            slowData = ScriptableObject.CreateInstance<SlowTowerData>();
            slowData.name = "SlowTowerData";
            slowData.Configure(slowTower, projectile, range: 4f, damage: 4f,
                cooldown: 1.1f, cost: 80, TargetLayer.Both);
            slowData.ConfigureSlow(slowPercent: 0.45f, slowDuration: 2.5f);

            waves = new List<WaveData>
            {
                MakeWave("Wave1", 1f, (gndData, 6)),
                MakeWave("Wave2", 0.85f, (gndData, 8), (airData, 3)),
                MakeWave("Wave3", 0.7f, (gndData, 10), (airData, 6)),
            };
        }

        private static WaveData MakeWave(string name, float interval, params (EnemyData enemy, int count)[] groups)
        {
            var wave = ScriptableObject.CreateInstance<WaveData>();
            wave.name = name;
            var list = new List<SpawnGroup>(groups.Length);
            foreach ((EnemyData enemy, int count) in groups)
            {
                list.Add(new SpawnGroup(enemy, count));
            }

            wave.Configure(list, interval);
            return wave;
        }

        // ---- managers ------------------------------------------------------

        private MapManager BuildMap()
        {
            var pathRoot = new GameObject("Path");
            var transforms = new Transform[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                var wp = new GameObject($"WP_{i}");
                wp.transform.SetParent(pathRoot.transform);
                wp.transform.position = waypoints[i];
                transforms[i] = wp.transform;
            }

            GameObject go = NewInactive("MapManager", out MapManager map);
            map.Configure(transforms, cellSize: 1f);
            go.SetActive(true);
            return map;
        }

        private Player BuildPlayer(MapManager map)
        {
            GameObject go = NewInactive("Player", out Player player);
            player.Configure(map, startingCurrency);
            go.SetActive(true);
            return player;
        }

        private WaveManager BuildWaveManager(List<WaveData> waves, MapManager map)
        {
            GameObject go = NewInactive("WaveManager", out WaveManager waveManager);
            waveManager.Configure(waves, map);
            go.SetActive(true);
            return waveManager;
        }

        private void BuildGameManager(Player player, WaveManager waveManager, MapManager map)
        {
            GameObject go = NewInactive("GameManager", out GameManager gameManager);
            gameManager.Configure(player, waveManager, map, baseHp);
            go.SetActive(true);
        }

        // ---- demo towers + scenery ----------------------------------------

        private void PlaceDemoTowers(Player player, TowerData normalData, SlowTowerData slowData)
        {
            // Positions chosen to sit beside the path, not on it.
            player.PlaceTower(normalData, new Vector3(-6f, 0f, 1f));
            player.PlaceTower(normalData, new Vector3(0f, 0f, 1f));
            player.PlaceTower(slowData, new Vector3(6f, 0f, 1f));
        }

        private void BuildEnvironment()
        {
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.transform.position = new Vector3(0f, 19f, -18f);
                cam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
                camGo.AddComponent<AudioListener>();
            }

            if (FindAnyObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            ground.transform.position = Vector3.zero;
        }

        // ---- helpers -----------------------------------------------------

        private static GameObject NewInactive<T>(string name, out T component) where T : Component
        {
            var go = new GameObject(name);
            go.SetActive(false);         // defer Awake until after Configure()
            component = go.AddComponent<T>();
            return go;
        }
    }
}
