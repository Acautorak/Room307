using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MotelSimulator.Data;

namespace MotelSimulator.UI
{
    /// <summary>
    /// One button/card in the motel lobby grid.
    /// Attach to the ApartmentCard prefab.
    /// </summary>
    public class ApartmentCard : MonoBehaviour
    {
        [Header("References")]
        public TextMeshProUGUI unitLabel;
        public TextMeshProUGUI roomCountLabel;
        public Button openButton;
        public Image statusIndicator;

        private ApartmentSaveData _data;
        private System.Action<ApartmentSaveData> _onOpen;

        public void Init(ApartmentSaveData data, System.Action<ApartmentSaveData> onOpen)
        {
            _data = data;
            _onOpen = onOpen;

            unitLabel.text = data.apartmentName;
            Refresh();
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(() => _onOpen?.Invoke(_data));
        }

        public void Refresh()
        {
            int count = _data.rooms?.Count ?? 0;
            roomCountLabel.text = count == 0 ? "Empty" : $"{count} room{(count == 1 ? "" : "s")}";
            statusIndicator.color = count == 0
                ? new Color(0.5f, 0.5f, 0.5f)
                : new Color(0.3f, 0.85f, 0.45f);
        }
    }
}
