using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StoryEvents
{
    /// <summary>
    /// The single reusable event panel. One instance lives in the scene forever.
    ///
    /// Prefab structure expected:
    ///   EventPanel
    ///   ├── ArtworkImage        (Image)
    ///   ├── TitleText           (TextMeshProUGUI)
    ///   ├── BodyScrollView      (ScrollRect)
    ///   │   └── Viewport/Content
    ///   │       └── BodyText    (TextMeshProUGUI)
    ///   └── ChoiceContainer     (VerticalLayoutGroup + ContentSizeFitter)
    ///       └── [ChoiceButton prefab spawned at runtime]
    /// </summary>
    public class EventPanel : MonoBehaviour
    {
        // ── Inspector references ─────────────────────────────────────────────

        [Header("Panel Elements")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Transform choiceContainer;

        [Header("Choice Button Prefab")]
        [Tooltip("Prefab with a Button component and a child TextMeshProUGUI for the label.")]
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Animation (optional)")]
        [Tooltip("Assign an Animator with Show/Hide trigger parameters, or leave null.")]
        [SerializeField] private Animator animator;
        private static readonly int ShowTrigger = Animator.StringToHash("Show");
        private static readonly int HideTrigger = Animator.StringToHash("Hide");

        // ── Runtime state ────────────────────────────────────────────────────

        /// <summary>The node currently being displayed. EventManager reads this.</summary>
        public EventNodeSO CurrentNode { get; private set; }

        private Action<ChoiceSO> _onChoiceMade;
        private readonly List<GameObject> _spawnedButtons = new();

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Populate and show the panel for a given node.
        /// </summary>
        public void Show(EventNodeSO node, string resolvedTitle, Action<ChoiceSO> onChoiceMade)
        {
            CurrentNode = node;
            _onChoiceMade = onChoiceMade;

            // Content
            artworkImage.sprite = node.artwork;
            artworkImage.gameObject.SetActive(node.artwork != null);
            titleText.text = resolvedTitle;
            bodyText.text = node.bodyText;

            // Choices
            ClearChoiceButtons();

            if (node.HasChoices)
            {
                foreach (var choice in node.choices)
                    SpawnChoiceButton(choice.label, () => ConfirmChoice(choice));
            }
            else
            {
                SpawnChoiceButton("Continue", () => ConfirmChoice(null));
            }

            // Show
            gameObject.SetActive(true);

            if (animator != null)
                animator.SetTrigger(ShowTrigger);
        }

        /// <summary>
        /// Hide the panel.
        /// </summary>
        public void Hide()
        {
            if (animator != null)
            {
                animator.SetTrigger(HideTrigger);
                // Deactivate after animation — hook this up via an Animation Event
                // calling HideImmediate() on this component, or use a coroutine.
            }
            else
            {
                HideImmediate();
            }
        }

        /// <summary>
        /// Deactivate immediately — call this from an Animation Event at the end
        /// of the hide animation, or directly if there's no animation.
        /// </summary>
        public void HideImmediate()
        {
            gameObject.SetActive(false);
            CurrentNode = null;
        }

        // ── Internals ────────────────────────────────────────────────────────

        private void ConfirmChoice(ChoiceSO choice)
        {
            // Disable all buttons immediately to prevent double-tap
            foreach (var btn in _spawnedButtons)
            {
                var b = btn.GetComponent<Button>();
                if (b != null) b.interactable = false;
            }

            _onChoiceMade?.Invoke(choice);
        }

        private void SpawnChoiceButton(string label, Action onClick)
        {
            GameObject go = Instantiate(choiceButtonPrefab, choiceContainer);
            _spawnedButtons.Add(go);

            // Set label — works whether the TMP is directly on the prefab root
            // or on a child named "Label"
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = label;

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onClick());
            }
        }

        private void ClearChoiceButtons()
        {
            foreach (var go in _spawnedButtons)
            {
                if (go != null) Destroy(go);
            }
            _spawnedButtons.Clear();
        }
    }
}
