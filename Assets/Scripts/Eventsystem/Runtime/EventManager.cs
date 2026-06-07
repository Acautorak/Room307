using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace StoryEvents
{
    /// <summary>
    /// Core runtime. Place on a persistent GameObject in your main scene (DontDestroyOnLoad).
    ///
    /// Responsibilities:
    ///   - Fires a timer every intervalSeconds
    ///   - Picks a random eligible chain and queues it (max 2 pending)
    ///   - Drives the EventPanel through a chain node by node
    ///   - Saves and loads all event state
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Content")]
        [Tooltip("All event chains in the game. Drag every EventChainSO here.")]
        [SerializeField] private EventChainSO[] allChains;

        [Tooltip("The shared GameContext ScriptableObject asset.")]
        [SerializeField] private GameContextSO gameContext;

        [Header("UI")]
        [Tooltip("The single EventPanel MonoBehaviour in the scene.")]
        [SerializeField] private EventPanel eventPanel;

        [Tooltip("The notification button MonoBehaviour in the scene.")]
        [SerializeField] private NotificationButton notificationButton;

        [Header("Timing")]
        [Tooltip("Seconds between random chain selections.")]
        [SerializeField] private float intervalSeconds = 30f;

        // ── Events ───────────────────────────────────────────────────────────

        /// <summary>Fires whenever the pending queue count changes (0, 1, or 2).</summary>
        public UnityEvent<int> OnPendingCountChanged = new();

        /// <summary>Fires when a chain is fully completed.</summary>
        public UnityEvent<string> OnChainCompleted = new();

        // ── State ────────────────────────────────────────────────────────────

        private readonly HashSet<string> _seenChainIds = new();
        private readonly Queue<EventChainSO> _pendingQueue = new();
        private EventChainSO _activeChain;
        private bool _isChainActive;
        private float _timer;

        // For mid-chain save/resume: track node depth as we walk
        private int _activeNodeDepth;

        // ── Constants ────────────────────────────────────────────────────────

        private const int MaxPending = 2;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            LoadState();
        }

        private void Start()
        {
            // If there was a mid-chain active session, resume it
            // (handled inside LoadState → ResumeActiveChain)
        }

        private void Update()
        {
            if (_isChainActive) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = intervalSeconds;
                TrySelectChain();
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveState();
        }

        private void OnApplicationQuit()
        {
            SaveState();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Open the event panel for the next pending chain.
        /// Called by the NotificationButton when the player taps it.
        /// </summary>
        public void OpenNextPendingEvent()
        {
            if (_isChainActive || _pendingQueue.Count == 0) return;

            _activeChain = _pendingQueue.Dequeue();
            _activeNodeDepth = 0;
            _isChainActive = true;

            OnPendingCountChanged.Invoke(_pendingQueue.Count);
            SaveState();

            ShowNode(_activeChain.entryNode);
        }

        /// <summary>
        /// Force-queue a specific chain immediately (e.g. from a tutorial trigger).
        /// </summary>
        public void ForceQueueChain(EventChainSO chain)
        {
            EnqueueChain(chain);
        }

        // ── Chain selection ──────────────────────────────────────────────────

        private void TrySelectChain()
        {
            if (_pendingQueue.Count >= MaxPending) return;

            EventChainSO picked = PickChain();
            if (picked == null) return;

            EnqueueChain(picked);
        }

        private void EnqueueChain(EventChainSO chain)
        {
            _pendingQueue.Enqueue(chain);
            OnPendingCountChanged.Invoke(_pendingQueue.Count);
            SaveState();
        }

        private EventChainSO PickChain()
        {
            var pool = allChains
                .Where(c => c != null)
                .Where(c => c.isRepeatable || !_seenChainIds.Contains(c.chainId))
                .Where(c => !_pendingQueue.Contains(c))
                .Where(c => c != _activeChain)
                .Where(c => MeetsRequirements(c))
                .ToList();

            if (pool.Count == 0) return null;

            float total = pool.Sum(c => c.weight);
            float roll = Random.value * total;

            foreach (var chain in pool)
            {
                roll -= chain.weight;
                if (roll <= 0f) return chain;
            }

            return pool[^1];
        }

        private bool MeetsRequirements(EventChainSO chain)
        {
            if (chain.requiredCompletedChainIds == null || chain.requiredCompletedChainIds.Length == 0)
                return true;

            return chain.requiredCompletedChainIds.All(id => _seenChainIds.Contains(id));
        }

        // ── Chain walking ────────────────────────────────────────────────────

        private void ShowNode(EventNodeSO node)
        {
            // Resolve blank title to chain default
            string title = string.IsNullOrEmpty(node.title)
                ? _activeChain.defaultTitle
                : node.title;

            eventPanel.Show(node, title, OnChoiceMade);
        }

        private void OnChoiceMade(ChoiceSO choice)
        {
            _activeNodeDepth++;

            // Apply any immediate mid-chain outcome on the choice itself
            choice?.immediateOutcome?.Apply(gameContext);

            // Determine next node
            EventNodeSO nextNode = choice?.nextNode
                ?? (choice == null ? GetCurrentContinueNode() : null);

            if (nextNode != null && !nextNode.isEndNode)
            {
                SaveState(); // save progress mid-chain
                ShowNode(nextNode);
                return;
            }

            // End of chain — apply the final outcome
            EventNodeSO endNode = nextNode ?? GetCurrentNode();
            endNode?.outcome?.Apply(gameContext);

            CompleteActiveChain();
        }

        // Helpers to get nodes — stored via depth for resume, so we walk the tree
        // In practice for resume we store the active chain + depth, not the node reference
        private EventNodeSO GetCurrentContinueNode()
        {
            // This is called when the panel's "continue" button is pressed with no choices
            // The panel passes null for choice, and we need the continue node from the
            // currently displayed node. EventPanel caches the current node for us.
            return eventPanel.CurrentNode?.continueNode;
        }

        private EventNodeSO GetCurrentNode()
        {
            return eventPanel.CurrentNode;
        }

        private void CompleteActiveChain()
        {
            if (_activeChain != null)
            {
                _seenChainIds.Add(_activeChain.chainId);
                OnChainCompleted.Invoke(_activeChain.chainId);
            }

            _activeChain = null;
            _activeNodeDepth = -1;
            _isChainActive = false;

            eventPanel.Hide();
            SaveState();
        }

        // ── Save / Load ──────────────────────────────────────────────────────

        private void SaveState()
        {
            var data = new EventSaveData
            {
                seenChainIds = _seenChainIds.ToList(),
                pendingChainIds = _pendingQueue.Select(c => c.chainId).ToList(),
                activeChainId = _activeChain?.chainId,
                activeNodeDepth = _activeNodeDepth,
                timerRemaining = _timer,
                gameContext = gameContext.ToSaveData()
            };

            SaveService.Save(data);
        }

        private void LoadState()
        {
            EventSaveData data = SaveService.Load();

            // Restore seen IDs
            _seenChainIds.Clear();
            foreach (var id in data.seenChainIds)
                _seenChainIds.Add(id);

            // Restore timer
            _timer = data.timerRemaining > 0 ? data.timerRemaining : intervalSeconds;

            // Restore game context
            if (data.gameContext != null)
                gameContext.FromSaveData(data.gameContext);

            // Restore pending queue (look up assets by chainId)
            _pendingQueue.Clear();
            foreach (var id in data.pendingChainIds)
            {
                var chain = FindChainById(id);
                if (chain != null) _pendingQueue.Enqueue(chain);
            }

            OnPendingCountChanged.Invoke(_pendingQueue.Count);

            // Resume mid-chain if applicable
            if (!string.IsNullOrEmpty(data.activeChainId))
            {
                _activeChain = FindChainById(data.activeChainId);
                if (_activeChain != null)
                {
                    _activeNodeDepth = data.activeNodeDepth;
                    _isChainActive = true;

                    // Walk the chain to the saved depth to find the resume node
                    EventNodeSO resumeNode = WalkToDepth(_activeChain.entryNode, _activeNodeDepth);
                    if (resumeNode != null)
                    {
                        // Small delay so the scene finishes loading before showing the panel
                        StartCoroutine(ShowNodeNextFrame(resumeNode));
                    }
                    else
                    {
                        // Couldn't find node — abort gracefully
                        CompleteActiveChain();
                    }
                }
            }
        }

        private IEnumerator ShowNodeNextFrame(EventNodeSO node)
        {
            yield return null;
            ShowNode(node);
        }

        /// <summary>
        /// Walk the chain tree depth-first following the first path (continue or first choice)
        /// to find the node at the saved depth. This is a best-effort resume for linear sections.
        /// Branching chains resume at the branch point, letting the player re-make the choice.
        /// </summary>
        private EventNodeSO WalkToDepth(EventNodeSO startNode, int targetDepth)
        {
            EventNodeSO current = startNode;
            for (int i = 0; i < targetDepth && current != null; i++)
            {
                if (current.HasChoices)
                    break; // resume at choice point so player can re-choose

                current = current.continueNode;
            }
            return current;
        }

        private EventChainSO FindChainById(string id)
        {
            foreach (var chain in allChains)
                if (chain != null && chain.chainId == id) return chain;
            return null;
        }

        // ── New game ─────────────────────────────────────────────────────────

        /// <summary>
        /// Call this when starting a new game to wipe all event progress.
        /// </summary>
        public void ResetAllProgress()
        {
            _seenChainIds.Clear();
            _pendingQueue.Clear();
            _activeChain = null;
            _isChainActive = false;
            _timer = intervalSeconds;
            _activeNodeDepth = -1;

            gameContext.ResetToDefaults();
            SaveService.DeleteSave();

            eventPanel.Hide();
            OnPendingCountChanged.Invoke(0);
        }
    }
}
