using System.Collections.Generic;

namespace StoryEvents
{
    /// <summary>
    /// All event system state needed to fully restore a session.
    /// Serialized to JSON via SaveService.
    /// </summary>
    [System.Serializable]
    public class EventSaveData
    {
        /// <summary>Chain IDs that have been completed at least once.</summary>
        public List<string> seenChainIds = new();

        /// <summary>
        /// Queue of pending chain IDs (max 2). These were selected by the timer
        /// but not yet played — persisted so they survive a reload.
        /// </summary>
        public List<string> pendingChainIds = new();

        /// <summary>
        /// The chain currently mid-playthrough (null if none).
        /// Allows resuming a chain after a game quit.
        /// </summary>
        public string activeChainId;

        /// <summary>
        /// The node index within the active chain's linear walk order.
        /// Used to restore mid-chain position. -1 means not mid-chain.
        /// </summary>
        public int activeNodeDepth = -1;

        /// <summary>Seconds remaining on the selection timer at save time.</summary>
        public float timerRemaining;

        /// <summary>Game context (stats, flags) bundled with the event save.</summary>
        public GameContextData gameContext;
    }
}
