using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Single source of truth for the level's path and build grid. Enemies read
    /// the path from here; the Player asks here whether a cell is buildable.
    /// </summary>
    public sealed class MapManager : MonoBehaviour
    {
        [Header("Path")]
        [SerializeField] private Transform[] waypoints = System.Array.Empty<Transform>();

        [Header("Build grid")]
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2Int gridMin = new(-12, -12);
        [SerializeField] private Vector2Int gridMax = new(12, 12);
        [SerializeField] private bool blockCellsUnderPath = true;

        private readonly Dictionary<Vector2Int, bool> _occupied = new();
        private List<Vector3> _pathCache;

        public int WaypointCount => waypoints.Length;

        private void Awake()
        {
            if (blockCellsUnderPath)
            {
                BlockPathCells();
            }
        }

        /// <summary>World-space path points, in order. Cached after first call.</summary>
        public List<Vector3> GetPath()
        {
            if (_pathCache == null)
            {
                _pathCache = new List<Vector3>(waypoints.Length);
                foreach (Transform w in waypoints)
                {
                    if (w != null)
                    {
                        _pathCache.Add(w.position);
                    }
                }
            }

            return _pathCache;
        }

        public bool IsBuildable(Vector3 worldPosition)
        {
            Vector2Int cell = ToCell(worldPosition);
            if (cell.x < gridMin.x || cell.x > gridMax.x || cell.y < gridMin.y || cell.y > gridMax.y)
            {
                return false;
            }

            return !_occupied.TryGetValue(cell, out bool used) || !used;
        }

        /// <summary>Call after a tower is successfully placed on this cell.</summary>
        public void MarkOccupied(Vector3 worldPosition)
        {
            _occupied[ToCell(worldPosition)] = true;
        }

        /// <summary>Call after a tower on this cell is sold / removed.</summary>
        public void MarkFree(Vector3 worldPosition)
        {
            _occupied[ToCell(worldPosition)] = false;
        }

        public Vector2Int ToCell(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / cellSize),
                Mathf.FloorToInt(worldPosition.z / cellSize));
        }

        /// <summary>World-space centre of the grid cell that contains the point.</summary>
        public Vector3 SnapToCell(Vector3 worldPosition)
        {
            Vector2Int c = ToCell(worldPosition);
            return new Vector3((c.x + 0.5f) * cellSize, worldPosition.y, (c.y + 0.5f) * cellSize);
        }

        private void BlockPathCells()
        {
            List<Vector3> path = GetPath();
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 a = path[i];
                Vector3 b = path[i + 1];
                float length = Vector3.Distance(a, b);
                int steps = Mathf.CeilToInt(length / (cellSize * 0.5f));
                for (int s = 0; s <= steps; s++)
                {
                    Vector3 p = Vector3.Lerp(a, b, steps == 0 ? 0f : (float)s / steps);
                    _occupied[ToCell(p)] = true;
                }
            }
        }

        /// <summary>Editor / bootstrap only.</summary>
        internal void Configure(Transform[] waypoints, float cellSize)
        {
            this.waypoints = waypoints;
            this.cellSize = cellSize;
            _pathCache = null;
        }

        private void OnDrawGizmosSelected()
        {
            if (waypoints == null || waypoints.Length < 2)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                }
            }
        }
    }
}
