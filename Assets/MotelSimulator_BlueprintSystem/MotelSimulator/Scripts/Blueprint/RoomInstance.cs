using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MotelSimulator.Data;

namespace MotelSimulator.Blueprint
{
    /// <summary>
    /// A placed room rectangle inside the blueprint canvas.
    /// Attach to the room prefab that contains: Image (background), outline Image, TextMeshProUGUI label.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class RoomInstance : MonoBehaviour
    {
        [Header("References")]
        public Image backgroundImage;
        public Image outlineImage;
        public TextMeshProUGUI labelText;
        public Button selectButton;

        // Runtime data
        public string RoomId { get; private set; }
        public RoomType RoomType { get; private set; }
        public RectInt GridRect { get; private set; }   // x,y,w,h in grid cells

        private RoomColorConfigSO _colorConfig;
        private BlueprintManager _manager;

        public void Init(string id, RectInt gridRect, RoomType type,
                         RoomColorConfigSO colorConfig, BlueprintManager manager)
        {
            RoomId = id;
            GridRect = gridRect;
            _colorConfig = colorConfig;
            _manager = manager;

            SetType(type);
            selectButton?.onClick.AddListener(OnClicked);
        }

        public void SetType(RoomType type)
        {
            RoomType = type;
            if (_colorConfig != null)
            {
                backgroundImage.color = _colorConfig.GetColor(type);
                labelText.text = _colorConfig.GetDisplayName(type);
            }
        }

        public void SetSelected(bool selected)
        {
            if (outlineImage != null)
                outlineImage.color = selected ? Color.white : new Color(0, 0, 0, 0.4f);
        }

        private void OnClicked() => _manager?.OnRoomClicked(this);
    }
}
