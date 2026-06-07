using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StoryEvents
{
    /// <summary>
    /// The notification button in the HUD. Shows a count bubble when events are pending.
    ///
    /// Prefab structure expected:
    ///   NotificationButton
    ///   ├── Button              (Button component on root or child)
    ///   ├── IconImage           (Image — the envelope / scroll icon)
    ///   └── BadgeRoot           (GameObject, shown/hidden)
    ///       └── BadgeText       (TextMeshProUGUI — shows "1" or "2")
    /// </summary>
    public class NotificationButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private TextMeshProUGUI badgeText;
        [SerializeField] private EventManager eventManager;

        [Header("Pulse animation (optional)")]
        [Tooltip("Assign an Animator with a Pulse trigger, or leave null.")]
        [SerializeField] private Animator animator;
        private static readonly int PulseTrigger = Animator.StringToHash("Pulse");

        private void Awake()
        {
            button.onClick.AddListener(OnButtonClicked);
            SetCount(0);
        }

        private void OnEnable()
        {
            eventManager.OnPendingCountChanged.AddListener(SetCount);
        }

        private void OnDisable()
        {
            eventManager.OnPendingCountChanged.RemoveListener(SetCount);
        }

        private void OnButtonClicked()
        {
            eventManager.OpenNextPendingEvent();
        }

        /// <summary>
        /// Update the badge count. Called by EventManager via UnityEvent.
        /// </summary>
        public void SetCount(int count)
        {
            bool hasPending = count > 0;
            badgeRoot.SetActive(hasPending);

            if (hasPending)
            {
                badgeText.text = count.ToString();

                if (animator != null)
                    animator.SetTrigger(PulseTrigger);
            }

            button.interactable = hasPending;
        }
    }
}
