using UnityEngine;

namespace TowerDefense.Interaction
{
    /// <summary>
    /// A flat circle drawn with a <see cref="LineRenderer"/>. Used to preview a
    /// tower's range while placing and to outline the selected tower.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RangeIndicator : MonoBehaviour
    {
        [SerializeField] private int segments = 48;
        [SerializeField] private float lineWidth = 0.06f;
        [SerializeField] private float yOffset = 0.05f;

        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            if (_line == null)
            {
                _line = gameObject.AddComponent<LineRenderer>();
            }

            Shader shader = Shader.Find("Sprites/Default")
                            ?? Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Unlit/Color");
            _line.material = new Material(shader);
            _line.useWorldSpace = true;
            _line.loop = true;
            _line.widthMultiplier = lineWidth;
            _line.numCornerVertices = 0;
            _line.numCapVertices = 0;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.positionCount = 0;
            _line.enabled = false;
        }

        public void Show(Vector3 center, float radius, Color color)
        {
            if (_line.positionCount != segments)
            {
                _line.positionCount = segments;
            }

            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                _line.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, yOffset, Mathf.Sin(a) * radius));
            }

            _line.startColor = color;
            _line.endColor = color;
            _line.enabled = true;
        }

        public void Hide()
        {
            if (_line != null)
            {
                _line.enabled = false;
            }
        }
    }
}
