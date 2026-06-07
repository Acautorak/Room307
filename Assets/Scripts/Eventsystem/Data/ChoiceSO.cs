using UnityEngine;

namespace StoryEvents
{
    /// <summary>
    /// One choice button. Reusable across nodes — e.g. a shared "Ignore" choice
    /// can be dropped onto multiple nodes without duplication.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Choice")]
    public class ChoiceSO : ScriptableObject
    {
        [Tooltip("Text displayed on the button.")]
        public string label = "Continue";

        [Tooltip("The node to show after this choice is picked. Leave null to end the chain.")]
        public EventNodeSO nextNode;

        [Tooltip("Optional: an outcome to apply immediately when this choice is picked (mid-chain effect).")]
        public EventOutcomeSO immediateOutcome;
    }
}
