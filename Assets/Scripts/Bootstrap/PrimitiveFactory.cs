using TowerDefense.Combat;
using TowerDefense.Enemies;
using TowerDefense.Towers;
using UnityEngine;

namespace TowerDefense.Bootstrap
{
    /// <summary>
    /// Builds throw-away primitive "prefabs" at runtime so the game is playable
    /// with no art assets and no manual Inspector wiring. Templates are parented
    /// under an inactive root so their own Update never runs; instantiated copies
    /// land at the scene root and come alive normally.
    /// </summary>
    internal static class PrimitiveFactory
    {
        public static GameObject CreateInactiveRoot()
        {
            var root = new GameObject("TD_Templates");
            root.SetActive(false);
            return root;
        }

        public static GameObject CreateEnemyTemplate<TEnemy>(string name, Color color, Transform root)
            where TEnemy : Enemy
        {
            GameObject go = MakeShape(name, PrimitiveType.Capsule, color, root, new Vector3(0.6f, 0.6f, 0.6f));
            go.AddComponent<TEnemy>();
            return go;
        }

        public static GameObject CreateTowerTemplate<TTower>(string name, Color color, Transform root)
            where TTower : Tower
        {
            GameObject go = MakeShape(name, PrimitiveType.Cube, color, root, new Vector3(0.8f, 1f, 0.8f));

            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "Barrel";
            barrel.transform.SetParent(go.transform, false);
            barrel.transform.localScale = new Vector3(0.25f, 0.25f, 0.9f);
            barrel.transform.localPosition = new Vector3(0f, 0.3f, 0.5f);
            RemoveCollider(barrel);
            Tint(barrel, color * 0.7f);

            go.AddComponent<TTower>();
            return go;
        }

        public static GameObject CreateProjectileTemplate(string name, Color color, Transform root)
        {
            GameObject go = MakeShape(name, PrimitiveType.Sphere, color, root, Vector3.one * 0.25f);
            go.AddComponent<Projectile>();
            return go;
        }

        private static GameObject MakeShape(string name, PrimitiveType shape, Color color,
            Transform root, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localScale = scale;
            RemoveCollider(go);
            Tint(go, color);
            return go;
        }

        private static void RemoveCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.Destroy(col);
            }
        }

        private static void Tint(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return;
            }

            // CreatePrimitive already assigns the pipeline's default lit material;
            // clone it so each template gets its own colour.
            var mat = new Material(renderer.sharedMaterial) { color = color };
            renderer.sharedMaterial = mat;
        }
    }
}
