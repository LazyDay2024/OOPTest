using System.Collections.Generic;
using TowerDefense.Data;
using TowerDefense.Events;
using TowerDefense.Towers;
using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Owns the player's economy and the towers they have placed. Currency is
    /// private and only moves through the methods here.
    /// </summary>
    public sealed class Player : MonoBehaviour
    {
        [SerializeField] private int startingCurrency = 200;
        [SerializeField, Range(0f, 1f)] private float sellRefundFraction = 0.5f;
        [SerializeField] private MapManager map;

        private int _currency;
        private readonly List<Tower> _placedTowers = new();

        public int Currency => _currency;
        public IReadOnlyList<Tower> PlacedTowers => _placedTowers;

        private void Awake()
        {
            _currency = startingCurrency;
        }

        private void Start()
        {
            GameEvents.RaiseCurrencyChanged(_currency);
        }

        public bool CanAfford(int cost) => _currency >= cost;

        public void AddCurrency(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            _currency = Mathf.Max(0, _currency + amount);
            GameEvents.RaiseCurrencyChanged(_currency);
        }

        /// <summary>Deduct <paramref name="amount"/> if affordable. Returns success.</summary>
        public bool TrySpend(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (!CanAfford(amount))
            {
                return false;
            }

            _currency -= amount;
            GameEvents.RaiseCurrencyChanged(_currency);
            return true;
        }

        /// <summary>
        /// Try to buy and place a tower. Verifies funds and buildability before
        /// anything is instantiated or any currency is spent.
        /// </summary>
        public bool PlaceTower(TowerData data, Vector3 position)
        {
            if (data == null || data.Prefab == null)
            {
                return false;
            }

            if (!CanAfford(data.Cost))
            {
                return false;
            }

            if (map != null && !map.IsBuildable(position))
            {
                return false;
            }

            GameObject towerObj = Instantiate(data.Prefab, position, Quaternion.identity);
            var tower = towerObj.GetComponent<Tower>();
            if (tower == null)
            {
                Destroy(towerObj);
                return false;
            }

            tower.Initialize(data);
            TrySpend(data.Cost); // affordability already checked above
            _placedTowers.Add(tower);
            map?.MarkOccupied(position);
            return true;
        }

        /// <summary>Spend the tower's upgrade cost and level it up. Returns success.</summary>
        public bool TryUpgrade(Tower tower)
        {
            if (tower == null || !_placedTowers.Contains(tower))
            {
                return false;
            }

            if (!TrySpend(tower.UpgradeCost))
            {
                return false;
            }

            tower.Upgrade();
            return true;
        }

        /// <summary>Remove a placed tower and refund part of its cost.</summary>
        public void SellTower(Tower tower)
        {
            if (tower == null || !_placedTowers.Remove(tower))
            {
                return;
            }

            int refund = Mathf.RoundToInt(tower.Cost * sellRefundFraction);
            map?.MarkFree(tower.transform.position);
            AddCurrency(refund);
            Destroy(tower.gameObject);
        }

        internal void Configure(MapManager map, int startingCurrency)
        {
            this.map = map;
            this.startingCurrency = startingCurrency;
        }
    }
}
