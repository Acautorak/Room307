using UnityEngine;

namespace StoryEvents
{
    /// <summary>
    /// One complete event chain. Designers create one asset per event story.
    /// The chain starts at entryNode and walks until an end node is reached.
    ///
    /// Create via: right-click in Project → CK2Events → Event Chain
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Event Chain")]
    public class EventChainSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique string ID used for save/load and seen-tracking. Must be unique across all chains.")]
        public string chainId;

        [Tooltip("Default title inherited by nodes that leave their title blank.")]
        public string defaultTitle = "An Event";

        [Header("Chain")]
        [Tooltip("The first node shown when this chain fires.")]
        public EventNodeSO entryNode;

        [Header("Selection")]
        [Tooltip("Relative weight for random selection. Higher = more likely to be picked.")]
        [Range(0.1f, 10f)]
        public float weight = 1f;

        [Tooltip("If false, this chain will never fire again once completed.")]
        public bool isRepeatable = false;

        [Tooltip("If set, this chain will only fire after the listed chain IDs have been completed.")]
        public string[] requiredCompletedChainIds;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-generate a chain ID from the asset name if left blank
            if (string.IsNullOrEmpty(chainId))
                chainId = name.ToLower().Replace(" ", "_");
        }
#endif
    }
}
