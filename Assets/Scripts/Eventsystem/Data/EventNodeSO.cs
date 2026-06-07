using UnityEngine;

namespace StoryEvents
{
    /// <summary>
    /// One "page" in an event chain — one panel shown to the player.
    ///
    /// - If choices is empty: a single "Continue" button advances to continueNode.
    /// - If choices has entries: those buttons are shown; each choice points to its own nextNode.
    /// - If isEndNode is true: applying the outcome ends the chain.
    ///
    /// The same title is reused across a chain by default — designers can override per node.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Event Node")]
    public class EventNodeSO : ScriptableObject
    {
        [Header("Display")]
        [Tooltip("Header image shown at the top of the panel.")]
        public Sprite artwork;

        [Tooltip("Title text. Leave blank to inherit from the chain's default title.")]
        public string title;

        [TextArea(4, 12)]
        [Tooltip("Body text shown beneath the title.")]
        public string bodyText;

        [Header("Navigation")]
        [Tooltip("Choices shown as buttons. Leave empty for a single Continue button.")]
        public ChoiceSO[] choices;

        [Tooltip("Next node when there are no choices (Continue path). Leave null if this is an end node.")]
        public EventNodeSO continueNode;

        [Header("End Node")]
        [Tooltip("Mark true on the final node in a chain.")]
        public bool isEndNode;

        [Tooltip("Outcome applied when this end node is resolved. Only used when isEndNode is true.")]
        public EventOutcomeSO outcome;

        /// <summary>
        /// True if this node has explicit choices for the player.
        /// </summary>
        public bool HasChoices => choices != null && choices.Length > 0;
    }
}
