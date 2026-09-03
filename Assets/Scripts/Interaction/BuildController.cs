using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Towers;
using UnityEngine;

namespace TowerDefense.Interaction
{
    /// <summary>
    /// The player-facing interaction layer: pick a tower from the build bar, see
    /// a ghost + range preview follow the cursor, left-click a buildable cell to
    /// place it, or left-click a placed tower to open a panel that upgrades or
    /// sells it. All economy / placement rules stay in <see cref="Player"/> and
    /// <see cref="MapManager"/>; this class only turns input into calls on them.
    ///
    /// Input goes through IMGUI (<see cref="Event.current"/>) so it works with the
    /// project's "Input System (New)" setting without any extra dependency.
    /// </summary>
    public sealed class BuildController : MonoBehaviour
    {
        private enum Mode { Idle, Placing }

        [SerializeField] private Player player;
        [SerializeField] private MapManager map;
        [SerializeField] private Camera cam;
        [SerializeField] private TowerData[] catalog = System.Array.Empty<TowerData>();
        [SerializeField] private float groundY = 0f;
        [SerializeField] private float towerPickRadius = 0.7f;

        private Mode _mode = Mode.Idle;
        private TowerData _selectedData;
        private Tower _selectedTower;

        private GameObject _ghost;
        private Renderer _ghostRenderer;
        private RangeIndicator _ghostRange;
        private RangeIndicator _selectionRange;

        private Vector3 _cursorWorld;
        private bool _cursorValid;
        private bool _placementOk;

        private Rect _barRect;
        private Rect _panelRect;

        private static readonly Color OkColor = new(0.35f, 1f, 0.4f, 1f);
        private static readonly Color BadColor = new(1f, 0.35f, 0.3f, 1f);
        private static readonly Color SelColor = new(0.4f, 0.85f, 1f, 1f);

        private void Awake()
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            _ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ghost.name = "BuildGhost";
            _ghost.transform.SetParent(transform, false);
            _ghost.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            Collider ghostCol = _ghost.GetComponent<Collider>();
            if (ghostCol != null)
            {
                Destroy(ghostCol);
            }

            _ghostRenderer = _ghost.GetComponent<Renderer>();
            _ghost.SetActive(false);

            _ghostRange = new GameObject("GhostRange").AddComponent<RangeIndicator>();
            _ghostRange.transform.SetParent(transform, false);
            _selectionRange = new GameObject("SelectionRange").AddComponent<RangeIndicator>();
            _selectionRange.transform.SetParent(transform, false);
        }

        private void Update()
        {
            // Ghost follows the cursor position cached during the last OnGUI pass.
            bool showGhost = _mode == Mode.Placing && _selectedData != null && _cursorValid;
            _ghost.SetActive(showGhost);

            if (showGhost)
            {
                _ghost.transform.position = _cursorWorld + Vector3.up * 0.5f;
                Color c = _placementOk ? OkColor : BadColor;
                if (_ghostRenderer != null)
                {
                    _ghostRenderer.material.color = c;
                }

                _ghostRange.Show(_cursorWorld, _selectedData.Range, c);
            }
            else
            {
                _ghostRange.Hide();
            }

            if (_selectedTower != null)
            {
                _selectionRange.Show(_selectedTower.transform.position, _selectedTower.Range, SelColor);
            }
            else
            {
                _selectionRange.Hide();
            }
        }

        private void OnGUI()
        {
            RefreshCursor();
            DrawBuildBar();
            DrawSelectedPanel();
            HandleWorldInput();
        }

        // ---- input -------------------------------------------------------

        private void RefreshCursor()
        {
            _cursorValid = false;
            if (cam == null || map == null)
            {
                return;
            }

            Vector2 m = Event.current.mousePosition;
            var screen = new Vector3(m.x, Screen.height - m.y, 0f);
            Ray ray = cam.ScreenPointToRay(screen);
            if (Mathf.Abs(ray.direction.y) < 1e-5f)
            {
                return;
            }

            float t = (groundY - ray.origin.y) / ray.direction.y;
            if (t <= 0f)
            {
                return;
            }

            Vector3 hit = ray.origin + ray.direction * t;
            _cursorWorld = map.SnapToCell(hit);
            _cursorWorld.y = groundY;
            _cursorValid = true;
            _placementOk = _selectedData != null
                           && player != null
                           && player.CanAfford(_selectedData.Cost)
                           && map.IsBuildable(_cursorWorld);
        }

        private void HandleWorldInput()
        {
            Event e = Event.current;

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    CancelPlacing();
                    _selectedTower = null;
                }
                else if (e.keyCode is >= KeyCode.Alpha1 and <= KeyCode.Alpha9)
                {
                    int idx = e.keyCode - KeyCode.Alpha1;
                    if (idx < catalog.Length)
                    {
                        BeginPlacing(catalog[idx]);
                    }
                }

                return;
            }

            if (e.type == EventType.MouseDown && e.button == 1)
            {
                CancelPlacing();
                e.Use();
                return;
            }

            if (e.type != EventType.MouseDown || e.button != 0)
            {
                return;
            }

            if (PointerOverUi(e.mousePosition))
            {
                return; // let the GUI handle its own click
            }

            if (_mode == Mode.Placing)
            {
                if (_cursorValid && _placementOk)
                {
                    player.PlaceTower(_selectedData, _cursorWorld); // stay in Placing for rapid building
                }
            }
            else if (_cursorValid)
            {
                _selectedTower = FindTowerNear(_cursorWorld);
            }

            e.Use();
        }

        private bool PointerOverUi(Vector2 mouse)
        {
            return _barRect.Contains(mouse) || (_selectedTower != null && _panelRect.Contains(mouse));
        }

        private Tower FindTowerNear(Vector3 world)
        {
            if (GameManager.Instance == null)
            {
                return null;
            }

            Tower best = null;
            float bestDist = towerPickRadius;
            var flat = new Vector2(world.x, world.z);

            foreach (Tower t in GameManager.Instance.Towers)
            {
                if (t == null)
                {
                    continue;
                }

                float d = Vector2.Distance(flat, new Vector2(t.transform.position.x, t.transform.position.z));
                if (d <= bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }

            return best;
        }

        private void BeginPlacing(TowerData data)
        {
            _mode = Mode.Placing;
            _selectedData = data;
            _selectedTower = null;
        }

        private void CancelPlacing()
        {
            _mode = Mode.Idle;
            _selectedData = null;
        }

        // ---- GUI --------------------------------------------------------

        private void DrawBuildBar()
        {
            float w = Mathf.Min(560f, Screen.width - 16f);
            _barRect = new Rect(8f, Screen.height - 40f, w, 32f);

            GUILayout.BeginArea(_barRect);
            GUILayout.BeginHorizontal();

            for (int i = 0; i < catalog.Length; i++)
            {
                TowerData data = catalog[i];
                if (data == null)
                {
                    continue;
                }

                bool active = _mode == Mode.Placing && _selectedData == data;
                string label = $"{(i + 1)}  {data.name}  ${data.Cost}{(active ? "  ◄" : string.Empty)}";
                GUI.enabled = player == null || player.CanAfford(data.Cost) || active;
                if (GUILayout.Button(label, GUILayout.Height(28f)))
                {
                    if (active)
                    {
                        CancelPlacing();
                    }
                    else
                    {
                        BeginPlacing(data);
                    }
                }

                GUI.enabled = true;
            }

            if (_mode == Mode.Placing && GUILayout.Button("Cancel (Esc)", GUILayout.Height(28f)))
            {
                CancelPlacing();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawSelectedPanel()
        {
            if (_selectedTower == null)
            {
                return;
            }

            _panelRect = new Rect(Screen.width - 244f, Screen.height - 156f, 236f, 148f);
            GUILayout.BeginArea(_panelRect, GUI.skin.box);

            GUILayout.Label($"{_selectedTower.GetType().Name}  (Lv {_selectedTower.Level})");
            GUILayout.Label($"Damage {_selectedTower.Damage:0.#}   Range {_selectedTower.Range:0.#}");

            int upCost = _selectedTower.UpgradeCost;
            GUI.enabled = player != null && player.CanAfford(upCost);
            if (GUILayout.Button($"Upgrade  ${upCost}"))
            {
                player.TryUpgrade(_selectedTower);
            }

            GUI.enabled = true;
            if (GUILayout.Button("Sell"))
            {
                player.SellTower(_selectedTower);
                _selectedTower = null;
            }

            if (GUILayout.Button("Close"))
            {
                _selectedTower = null;
            }

            GUILayout.EndArea();
        }

        internal void Configure(Player player, MapManager map, Camera cam, TowerData[] catalog)
        {
            this.player = player;
            this.map = map;
            this.cam = cam;
            this.catalog = catalog;
        }
    }
}
