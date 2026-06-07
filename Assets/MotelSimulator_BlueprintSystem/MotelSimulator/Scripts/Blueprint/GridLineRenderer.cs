using UnityEngine;
using UnityEngine.UI;

namespace MotelSimulator.Blueprint
{
    /// <summary>
    /// Dynamically draws grid lines on a UI canvas using a pool of Images.
    /// Attach to a child RawImage or Panel inside the blueprint area.
    /// Set columns/rows to match GridDrawTool.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class GridLineRenderer : MonoBehaviour
    {
        public int columns = 20;
        public int rows = 14;
        public Color lineColor = new Color(0.6f, 0.7f, 0.8f, 0.25f);
        public float lineThickness = 1f;

        private RectTransform _rt;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            DrawLines();
        }

        private void DrawLines()
        {
            var size = _rt.rect.size;
            if (size == Vector2.zero)
            {
                // Defer one frame if layout hasn't run yet
                Invoke(nameof(DrawLines), 0f);
                return;
            }

            float cellW = size.x / columns;
            float cellH = size.y / rows;

            // Vertical lines
            for (int c = 0; c <= columns; c++)
                CreateLine(new Vector2(c * cellW - size.x * 0.5f, 0),
                           new Vector2(lineThickness, size.y));

            // Horizontal lines
            for (int r = 0; r <= rows; r++)
                CreateLine(new Vector2(0, r * cellH - size.y * 0.5f),
                           new Vector2(size.x, lineThickness));
        }

        private void CreateLine(Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject("Line", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.GetComponent<Image>().color = lineColor;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }
    }
}
