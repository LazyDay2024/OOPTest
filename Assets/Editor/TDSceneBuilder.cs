using System.Collections.Generic;
using System.IO;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemies;
using TowerDefense.Interaction;
using TowerDefense.Towers;
using TowerDefense.Waves;
using TowerDefense.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// Generates real project assets — primitive prefabs, ScriptableObject data,
    /// and a fully wired scene — so the game can be opened and played in the
    /// editor without any hand wiring.
    ///
    /// Run from the menu (Tools ▸ Tower Defense ▸ Build Demo Project) or headless:
    ///   Unity.exe -batchmode -quit -projectPath &lt;proj&gt; \
    ///     -executeMethod TowerDefense.EditorTools.TDSceneBuilder.BuildDemoProject
    /// </summary>
    public static class TDSceneBuilder
    {
        private const string ScriptableRoot = "Assets/ScriptableObjects";
        private const string PrefabRoot = "Assets/Prefabs";
        private const string ScenePath = "Assets/Scenes/TowerDefense.unity";

        [MenuItem("Tools/Tower Defense/Build Demo Project")]
        public static void BuildDemoProject()
        {
            EnsureFolders();

            // --- prefabs ---------------------------------------------------
            GameObject gndPrefab = SaveEnemyPrefab<GndEnemy>("GndEnemy", new Color(0.85f, 0.3f, 0.3f));
            GameObject airPrefab = SaveEnemyPrefab<AirEnemy>("AirEnemy", new Color(0.95f, 0.75f, 0.2f));
            GameObject projPrefab = SaveProjectilePrefab();
            GameObject normalPrefab = SaveTowerPrefab<NormalTower>("NormalTower", new Color(0.3f, 0.55f, 0.95f));
            GameObject slowPrefab = SaveTowerPrefab<SlowTower>("SlowTower", new Color(0.35f, 0.85f, 0.9f));

            // --- data ----------------------------------------------------
            EnemyData gndData = CreateEnemyData("GndEnemyData", gndPrefab, 30f, 2.2f, 8, 1, EnemyLayer.Ground);
            EnemyData airData = CreateEnemyData("AirEnemyData", airPrefab, 22f, 3f, 10, 1, EnemyLayer.Air);

            TowerData normalData = CreateTowerData("NormalTowerData", normalPrefab, projPrefab,
                4.5f, 10f, 0.7f, 60, TargetLayer.Ground);
            SlowTowerData slowData = CreateSlowTowerData("SlowTowerData", slowPrefab, projPrefab,
                4f, 4f, 1.1f, 80, TargetLayer.Both, 0.45f, 2.5f);

            var waves = new List<WaveData>
            {
                CreateWave("Wave1", 1.0f, (gndData, 6)),
                CreateWave("Wave2", 0.85f, (gndData, 8), (airData, 3)),
                CreateWave("Wave3", 0.70f, (gndData, 10), (airData, 6)),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // --- scene -------------------------------------------------
            BuildScene(normalData, slowData, waves);

            Debug.Log("[TDSceneBuilder] Demo project built. Open " + ScenePath + " and press Play.");
        }

        // ----------------------------------------------------------------

        private static void EnsureFolders()
        {
            foreach (string path in new[]
            {
                ScriptableRoot, ScriptableRoot + "/Enemies", ScriptableRoot + "/Towers", ScriptableRoot + "/Waves",
                PrefabRoot, PrefabRoot + "/Enemies", PrefabRoot + "/Towers", PrefabRoot + "/Projectiles",
                "Assets/Scenes",
            })
            {
                if (!AssetDatabase.IsValidFolder(path))
                {
                    string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                    string leaf = Path.GetFileName(path);
                    AssetDatabase.CreateFolder(parent, leaf);
                }
            }
        }

        private static GameObject SaveEnemyPrefab<T>(string name, Color color) where T : Enemy
        {
            GameObject go = MakePrimitive(name, PrimitiveType.Capsule, color, new Vector3(0.6f, 0.6f, 0.6f));
            go.AddComponent<T>();
            return SavePrefab(go, $"{PrefabRoot}/Enemies/{name}.prefab");
        }

        private static GameObject SaveTowerPrefab<T>(string name, Color color) where T : Tower
        {
            GameObject go = MakePrimitive(name, PrimitiveType.Cube, color, new Vector3(0.8f, 1f, 0.8f));
            GameObject barrel = MakePrimitive("Barrel", PrimitiveType.Cube, color * 0.7f, new Vector3(0.25f, 0.25f, 0.9f));
            barrel.transform.SetParent(go.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0.3f, 0.5f);
            go.AddComponent<T>();
            return SavePrefab(go, $"{PrefabRoot}/Towers/{name}.prefab");
        }

        private static GameObject SaveProjectilePrefab()
        {
            GameObject go = MakePrimitive("Projectile", PrimitiveType.Sphere, Color.white, Vector3.one * 0.25f);
            go.AddComponent<TowerDefense.Combat.Projectile>();
            return SavePrefab(go, $"{PrefabRoot}/Projectiles/Projectile.prefab");
        }

        private static GameObject MakePrimitive(string name, PrimitiveType shape, Color color, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.localScale = scale;

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(renderer.sharedMaterial) { color = color };
                renderer.sharedMaterial = mat;
            }

            return go;
        }

        private static GameObject SavePrefab(GameObject go, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ---- data creation -------------------------------------------------

        private static EnemyData CreateEnemyData(string name, GameObject prefab, float hp, float speed,
            int reward, int leak, EnemyLayer layer)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.Configure(prefab, hp, speed, reward, leak, layer);
            AssetDatabase.CreateAsset(data, $"{ScriptableRoot}/Enemies/{name}.asset");
            return data;
        }

        private static TowerData CreateTowerData(string name, GameObject prefab, GameObject proj,
            float range, float damage, float cooldown, int cost, TargetLayer layer)
        {
            var data = ScriptableObject.CreateInstance<TowerData>();
            data.Configure(prefab, proj, range, damage, cooldown, cost, layer);
            AssetDatabase.CreateAsset(data, $"{ScriptableRoot}/Towers/{name}.asset");
            return data;
        }

        private static SlowTowerData CreateSlowTowerData(string name, GameObject prefab, GameObject proj,
            float range, float damage, float cooldown, int cost, TargetLayer layer,
            float slowPercent, float slowDuration)
        {
            var data = ScriptableObject.CreateInstance<SlowTowerData>();
            data.Configure(prefab, proj, range, damage, cooldown, cost, layer);
            data.ConfigureSlow(slowPercent, slowDuration);
            AssetDatabase.CreateAsset(data, $"{ScriptableRoot}/Towers/{name}.asset");
            return data;
        }

        private static WaveData CreateWave(string name, float interval, params (EnemyData enemy, int count)[] groups)
        {
            var wave = ScriptableObject.CreateInstance<WaveData>();
            var list = new List<SpawnGroup>();
            foreach ((EnemyData enemy, int count) in groups)
            {
                list.Add(new SpawnGroup(enemy, count));
            }

            wave.Configure(list, interval);
            AssetDatabase.CreateAsset(wave, $"{ScriptableRoot}/Waves/{name}.asset");
            return wave;
        }

        // ---- scene assembly ----------------------------------------------

        private static void BuildScene(TowerData normalData, SlowTowerData slowData, List<WaveData> waves)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 19f, -18f), Quaternion.Euler(52f, 0f, 0f));

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject ground = MakePrimitive("Ground", PrimitiveType.Plane, new Color(0.2f, 0.35f, 0.2f), new Vector3(4f, 1f, 4f));

            Vector3[] pts =
            {
                new(-9f, 0.5f, -2f), new(-9f, 0.5f, 4f), new(-3f, 0.5f, 4f), new(-3f, 0.5f, -3f),
                new(3f, 0.5f, -3f), new(3f, 0.5f, 4f), new(9f, 0.5f, 4f), new(9f, 0.5f, -2f),
            };
            var pathRoot = new GameObject("Path");
            var transforms = new Transform[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                var wp = new GameObject($"WP_{i}");
                wp.transform.SetParent(pathRoot.transform);
                wp.transform.position = pts[i];
                transforms[i] = wp.transform;
            }

            var mapGo = new GameObject("MapManager");
            var map = mapGo.AddComponent<MapManager>();
            map.Configure(transforms, 1f);

            var playerGo = new GameObject("Player");
            var player = playerGo.AddComponent<Player>();
            player.Configure(map, 350);

            var waveGo = new GameObject("WaveManager");
            var waveManager = waveGo.AddComponent<WaveManager>();
            waveManager.Configure(waves, map);

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.Configure(player, waveManager, map, 20);

            var hudGo = new GameObject("DebugHud");
            hudGo.AddComponent<DebugHud>();

            // Visible road along the waypoints + a keep at the path end.
            var pathViewGo = new GameObject("PathRenderer");
            pathViewGo.AddComponent<TowerDefense.View.PathRenderer>().Configure(map);

            var baseGo = new GameObject("Base");
            baseGo.transform.position = new Vector3(pts[pts.Length - 1].x, 0f, pts[pts.Length - 1].z);
            baseGo.AddComponent<TowerDefense.View.BaseView>().Configure(map);

            // Interactive build / sell layer (click a cell to build, click a
            // placed tower to upgrade or sell).
            var buildGo = new GameObject("BuildController");
            var buildController = buildGo.AddComponent<BuildController>();
            buildController.Configure(player, map, cam, new TowerData[] { normalData, slowData });

            foreach (GameObject root in new[]
            {
                camGo, lightGo, ground, pathRoot, mapGo, playerGo, waveGo, gmGo, hudGo,
                pathViewGo, baseGo, buildGo,
            })
            {
                EditorUtility.SetDirty(root);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
