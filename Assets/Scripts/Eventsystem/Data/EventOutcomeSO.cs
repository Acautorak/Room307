using UnityEngine;

namespace StoryEvents
{
    /// <summary>
    /// Abstract base for all event outcomes.
    /// Subclass this and implement Apply() to create any game effect.
    /// Drag the outcome asset onto the end node in the Inspector.
    /// </summary>
    public abstract class EventOutcomeSO : ScriptableObject
    {
        [TextArea(2, 4)]
        [Tooltip("Designer note — what does this outcome do? Not shown in game.")]
        public string designerNote;

        /// <summary>
        /// Called when the chain reaches this end node and the player confirms.
        /// </summary>
        public abstract void Apply(GameContextSO context);

        /// <summary>
        /// Optional: return a localised summary shown to the player before they confirm.
        /// Return null to show nothing.
        /// </summary>
        public virtual string GetPreviewText() => null;
    }
}
