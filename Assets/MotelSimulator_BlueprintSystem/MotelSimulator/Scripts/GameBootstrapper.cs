using UnityEngine;
using MotelSimulator.Save;
using MotelSimulator.UI;
using MotelSimulator.Blueprint;

namespace MotelSimulator
{
    /// <summary>
    /// Drop this on a persistent "GameManager" GameObject in your first scene.
    /// It ensures initialization order and wires the back-button between panels.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Scene References")]
        public SaveSystem saveSystem;
        public MotelLobbyManager lobbyManager;
        public BlueprintManager blueprintManager;

        private void Awake()
        {
            // Make sure SaveSystem exists (could also be pre-placed in scene)
            if (SaveSystem.Instance == null && saveSystem != null)
                saveSystem.gameObject.SetActive(true);
        }

        private void Start()
        {
            // Wire blueprint's close → lobby return
            var closeBtn = blueprintManager.closeButton;
            closeBtn.onClick.AddListener(() => lobbyManager.ReturnToLobby());

            // Start on lobby
            blueprintManager.gameObject.SetActive(false);
            lobbyManager.gameObject.SetActive(true);
        }
    }
}
