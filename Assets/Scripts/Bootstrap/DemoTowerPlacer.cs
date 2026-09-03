using TowerDefense.Core;
using TowerDefense.Data;
using UnityEngine;

namespace TowerDefense.Bootstrap
{
    /// <summary>
    /// Places a few starter towers once at Start. Used by the hand-built scene,
    /// where <see cref="Player"/> only exists after Play begins.
    /// </summary>
    public sealed class DemoTowerPlacer : MonoBehaviour
    {
        [SerializeField] private Player player;
        [SerializeField] private TowerData normalTower;
        [SerializeField] private TowerData slowTower;

        [SerializeField]
        private Vector3[] normalSpots = { new(-6f, 0f, 1f), new(0f, 0f, 1f) };

        [SerializeField]
        private Vector3[] slowSpots = { new(6f, 0f, 1f) };

        private void Start()
        {
            if (player == null)
            {
                return;
            }

            if (normalTower != null)
            {
                foreach (Vector3 p in normalSpots)
                {
                    player.PlaceTower(normalTower, p);
                }
            }

            if (slowTower != null)
            {
                foreach (Vector3 p in slowSpots)
                {
                    player.PlaceTower(slowTower, p);
                }
            }
        }

        internal void Configure(Player player, TowerData normalTower, TowerData slowTower)
        {
            this.player = player;
            this.normalTower = normalTower;
            this.slowTower = slowTower;
        }
    }
}
