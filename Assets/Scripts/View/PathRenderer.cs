using System.Collections.Generic;
using TowerDefense.Core;
using UnityEngine;

namespace TowerDefense.View
{
    /// <summary>
    /// Draws a visible road along the map's waypoints at runtime: a ribbon of
    /// flat segments, rounded joints at each corner, a start pad, and a dashed
    /// centre line showing travel direction. Pure presentation — it reads the
    /// path from <see cref="MapManager"/> and never affects gameplay.
    /// </summary>
    public sealed class PathRenderer : MonoBehaviour
    {
        [SerializeField] private MapManager map;
        [SerializeField] private float roadWidth = 1.3f;
        [SerializeField] private float roadY = 0.03f;
        [SerializeField] private float dashSpacing = 1.1f;
        [SerializeField] private Color roadColor = new(0.16f, 0.16f, 0.18f);
        [SerializeField] private Color jointColor = new(0.22f, 0.22f, 0.25f);
        [SerializeField] private Color dashColor = new(0.85f, 0.8f, 0.35f);
        [SerializeField] private Color startColor = new(0.3f, 0.7f, 0.35f);

        private Transform _root;

        private void Start()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            if (map == null)
            {
                return;
            }

            List<Vector3> path = map.GetPath();
            if (path == null || path.Count < 2)
            {
                return;
            }

            if (_root != null)
            {
                Destroy(_root.gameObject);
            }

            _root = new GameObject("Road").transform;
            _root.SetParent(transform, false);

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 a = Flat(path[i]);
                Vector3 b = Flat(path[i + 1]);
                Vector3 dir = b - a;
                float len = dir.magnitude;
                if (len < 1e-3f)
                {
                    continue;
                }

                Quaternion rot = Quaternion.LookRotation(dir);
                Segment("Seg", (a + b) * 0.5f, rot,
                    new Vector3(roadWidth, 0.06f, len + roadWidth), roadColor);

                int dashes = Mathf.FloorToInt(len / dashSpacing);
                for (int d = 1; d < dashes; d++)
                {
                    Vector3 p = Vector3.Lerp(a, b, d / (float)dashes);
                    Segment("Dash", p + Vector3.up * 0.02f, rot,
                        new Vector3(0.16f, 0.04f, 0.45f), dashColor);
                }
            }

            foreach (Vector3 wp in path)
            {
                Segment("Joint", Flat(wp), Quaternion.identity,
                    new Vector3(roadWidth, 0.06f, roadWidth), jointColor);
            }

            Segment("Start", Flat(path[0]), Quaternion.identity,
                new Vector3(roadWidth * 1.5f, 0.07f, roadWidth * 1.5f), startColor);
        }

        private Vector3 Flat(Vector3 v) => new(v.x, roadY, v.z);

        private void Segment(string name, Vector3 pos, Quaternion rot, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial = new Material(renderer.sharedMaterial) { color = color };
            }
        }

        internal void Configure(MapManager map)
        {
            this.map = map;
        }
    }
}
