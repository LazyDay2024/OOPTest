using System.Collections.Generic;
using TowerDefense.Core;
using TowerDefense.Events;
using UnityEngine;

namespace TowerDefense.View
{
    /// <summary>
    /// Visible keep at the end of the path. Builds itself from primitives and
    /// reacts to <see cref="GameEvents"/>: a colour + fill bar that track base HP,
    /// a white flash on each hit, and a distinct look on game over / victory.
    /// Presentation only.
    /// </summary>
    public sealed class BaseView : MonoBehaviour
    {
        [SerializeField] private MapManager map;
        [SerializeField] private Color fullColor = new(0.35f, 0.8f, 0.4f);
        [SerializeField] private Color emptyColor = new(0.85f, 0.25f, 0.2f);

        private Camera _cam;
        private Renderer _coreRenderer;
        private Transform _barFill;
        private Transform _bar;

        private int _maxHp = -1;
        private int _lastHp = -1;
        private float _flash;
        private bool _ended;

        private void Awake()
        {
            _cam = Camera.main;
            Build();
        }

        private void OnEnable()
        {
            GameEvents.OnBaseHpChanged += HandleHp;
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnVictory += HandleVictory;
        }

        private void OnDisable()
        {
            GameEvents.OnBaseHpChanged -= HandleHp;
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnVictory -= HandleVictory;
        }

        private void Build()
        {
            Vector3 pos = transform.position;
            if (map != null)
            {
                List<Vector3> path = map.GetPath();
                if (path != null && path.Count > 0)
                {
                    pos = path[path.Count - 1];
                }
            }

            transform.position = new Vector3(pos.x, 0f, pos.z);

            Block("Keep", new Vector3(0f, 0.6f, 0f), new Vector3(2.2f, 1.2f, 2.2f), new Color(0.5f, 0.5f, 0.55f));
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    Block("Merlon", new Vector3(sx * 0.9f, 1.35f, sz * 0.9f),
                        new Vector3(0.4f, 0.5f, 0.4f), new Color(0.42f, 0.42f, 0.47f));
                }
            }

            _coreRenderer = Block("Core", new Vector3(0f, 1.5f, 0f), new Vector3(0.9f, 0.9f, 0.9f), fullColor)
                .GetComponent<Renderer>();

            _bar = new GameObject("HpBar").transform;
            _bar.SetParent(transform, false);
            _bar.localPosition = new Vector3(0f, 2.5f, 0f);
            Transform bg = Block("BarBg", Vector3.zero, new Vector3(2f, 0.28f, 0.06f), new Color(0.1f, 0.1f, 0.1f)).transform;
            bg.SetParent(_bar, false);
            _barFill = Block("BarFill", Vector3.zero, new Vector3(1.9f, 0.2f, 0.09f), fullColor).transform;
            _barFill.SetParent(_bar, false);
        }

        private void HandleHp(int hp)
        {
            if (_maxHp < 0)
            {
                _maxHp = Mathf.Max(1, hp);
            }

            if (_lastHp >= 0 && hp < _lastHp)
            {
                _flash = 1f; // took damage
            }

            _lastHp = hp;
            float ratio = Mathf.Clamp01(hp / (float)_maxHp);

            if (_barFill != null)
            {
                Vector3 s = _barFill.localScale;
                _barFill.localScale = new Vector3(1.9f * ratio, s.y, s.z);
                _barFill.localPosition = new Vector3(-0.95f * (1f - ratio), 0f, 0.01f);
            }

            if (_coreRenderer != null && !_ended)
            {
                _coreRenderer.material.color = Color.Lerp(emptyColor, fullColor, ratio);
            }
        }

        private void HandleGameOver()
        {
            _ended = true;
            Tint(new Color(0.15f, 0.15f, 0.17f));
        }

        private void HandleVictory()
        {
            _ended = true;
            Tint(new Color(1f, 0.85f, 0.3f));
        }

        private void Update()
        {
            if (_flash > 0f)
            {
                _flash = Mathf.Max(0f, _flash - Time.deltaTime * 4f);
                if (_coreRenderer != null && !_ended)
                {
                    float ratio = _maxHp > 0 ? Mathf.Clamp01(_lastHp / (float)_maxHp) : 1f;
                    _coreRenderer.material.color =
                        Color.Lerp(Color.Lerp(emptyColor, fullColor, ratio), Color.white, _flash);
                }
            }

            if (_bar != null)
            {
                if (_cam == null)
                {
                    _cam = Camera.main;
                }

                if (_cam != null)
                {
                    _bar.rotation = Quaternion.LookRotation(_bar.position - _cam.transform.position);
                }
            }
        }

        private void Tint(Color color)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (r.transform == _barFill || r.name == "BarBg")
                {
                    continue;
                }

                r.material.color = color;
            }
        }

        private GameObject Block(string name, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                renderer.material = new Material(renderer.sharedMaterial) { color = color };
            }

            return go;
        }

        internal void Configure(MapManager map)
        {
            this.map = map;
        }
    }
}
