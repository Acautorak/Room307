using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MotelSimulator.Blueprint
{
    /// <summary>
    /// Attach to the transparent grid overlay panel.
    /// Fires OnRectConfirmed(RectInt gridRect) when the player finishes drawing.
    /// </summary>
    public class GridDrawTool : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Grid Settings")]
        public int columns = 20;
        public int rows = 14;
        public int minCells = 1;       // minimum 1×1 cells

        [Header("Preview")]
        public Image previewImage;     // a colored rect shown while dragging
        public Color previewColor = new Color(1f, 1f, 1f, 0.35f);

        public System.Action<RectInt> OnRectConfirmed;

        private RectTransform _rt;
        private Vector2Int _startCell;
        private bool _drawing;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            if (previewImage != null)
            {
                previewImage.color = previewColor;
                previewImage.gameObject.SetActive(false);
            }
        }

        // ── IPointer handlers ────────────────────────────────────────────────────

        public void OnPointerDown(PointerEventData e)
        {
            if (!gameObject.activeSelf) return;
            _startCell = ScreenToCell(e.position);
            _drawing = true;
            UpdatePreview(_startCell, _startCell);
            if (previewImage) previewImage.gameObject.SetActive(true);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_drawing) return;
            var currentCell = ScreenToCell(e.position);
            UpdatePreview(_startCell, currentCell);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!_drawing) return;
            _drawing = false;
            if (previewImage) previewImage.gameObject.SetActive(false);

            var endCell = ScreenToCell(e.position);
            var rect = CellsToRect(_startCell, endCell);

            if (rect.width >= minCells && rect.height >= minCells)
                OnRectConfirmed?.Invoke(rect);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private Vector2Int ScreenToCell(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rt, screenPos, null, out Vector2 local);

            var size = _rt.rect.size;
            float cx = (local.x + size.x * 0.5f) / size.x * columns;
            float cy = (local.y + size.y * 0.5f) / size.y * rows;

            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(cx), 0, columns - 1),
                Mathf.Clamp(Mathf.FloorToInt(cy), 0, rows - 1));
        }

        private RectInt CellsToRect(Vector2Int a, Vector2Int b)
        {
            int x = Mathf.Min(a.x, b.x);
            int y = Mathf.Min(a.y, b.y);
            int w = Mathf.Abs(a.x - b.x) + 1;
            int h = Mathf.Abs(a.y - b.y) + 1;
            return new RectInt(x, y, w, h);
        }

        private void UpdatePreview(Vector2Int a, Vector2Int b)
        {
            if (previewImage == null) return;
            var gridRect = CellsToRect(a, b);
            var pixelRect = GridRectToPixel(gridRect);
            previewImage.rectTransform.anchoredPosition = new Vector2(pixelRect.x, pixelRect.y);
            previewImage.rectTransform.sizeDelta = new Vector2(pixelRect.width, pixelRect.height);
        }

        /// <summary>Returns pixel rect relative to the grid panel's bottom-left.</summary>
        public Rect GridRectToPixel(RectInt gridRect)
        {
            var size = _rt.rect.size;
            float cellW = size.x / columns;
            float cellH = size.y / rows;

            float px = gridRect.x * cellW - size.x * 0.5f;
            float py = gridRect.y * cellH - size.y * 0.5f;
            float pw = gridRect.width * cellW;
            float ph = gridRect.height * cellH;

            return new Rect(px, py, pw, ph);
        }

        public void SetActive(bool active)
        {
            enabled = active;
            if (!active && previewImage) previewImage.gameObject.SetActive(false);
        }
    }
}
