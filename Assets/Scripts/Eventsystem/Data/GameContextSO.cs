using UnityEngine;

namespace StoryEvents
{
    /// <summary>
    /// Central game state. Drag this asset into any system that needs to read or write game data.
    /// Outcomes subclass EventOutcomeSO and call into this directly.
    /// Add your own fields here as your game grows.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Game Context")]
    public class GameContextSO : ScriptableObject
    {
        [Header("Example Stats — replace with your own")]
        public int gold = 100;
        public int prestige = 0;
        public int sanity = 0;
        public int xp = 500;

        [Header("Flags — set by outcomes, read by conditions")]
        public bool[] flags = new bool[64]; // expand as needed

        /// <summary>
        /// Reset to initial values. Call this on new game.
        /// </summary>
        public void ResetToDefaults()
        {
            gold = 100;
            prestige = 0;
            sanity = 0;
            xp = 500;
            flags = new bool[64];
        }

        /// <summary>
        /// Serialize game state to a plain data struct for saving.
        /// </summary>
        public GameContextData ToSaveData() => new GameContextData
        {
            gold = gold,
            prestige = prestige,
            sanity = sanity,
            xp = xp,
            flags = (bool[])flags.Clone()
        };

        /// <summary>
        /// Restore game state from saved data.
        /// </summary>
        public void FromSaveData(GameContextData data)
        {
            gold = data.gold;
            prestige = data.prestige;
            sanity = data.sanity;
            xp = data.xp;
            flags = data.flags ?? new bool[64];
        }
    }

    [System.Serializable]
    public class GameContextData
    {
        public int gold;
        public int prestige;
        public int sanity;
        public int xp;
        public bool[] flags;
    }
}
