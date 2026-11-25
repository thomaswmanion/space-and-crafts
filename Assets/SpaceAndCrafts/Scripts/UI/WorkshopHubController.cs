using SpaceAndCrafts.Core;
using SpaceAndCrafts.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Workshop hub controls: Jump → world select / Arrival, H → Home, R → reset save (dev).
    /// </summary>
    public sealed class WorkshopHubController : MonoBehaviour
    {
        [SerializeField] private WorldSelectBoard worldSelect;
        [SerializeField] private WorkshopHubDirector director;
        [SerializeField] private string homeSceneName = "HomeMenu";

        private void Awake()
        {
            ProgressSaveBridge.Ensure();
            if (worldSelect == null)
            {
                worldSelect = FindAnyObjectByType<WorldSelectBoard>();
            }

            if (director == null)
            {
                director = FindAnyObjectByType<WorkshopHubDirector>();
            }
        }

        private void Update()
        {
            if (WasConfirmPressed())
            {
                if (worldSelect != null)
                {
                    worldSelect.ConfirmSelection();
                }
                else
                {
                    SceneManager.LoadScene("RainbowWorld_Arrival");
                }
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                if (Application.CanStreamedLevelBeLoaded(homeSceneName))
                {
                    SceneManager.LoadScene(homeSceneName);
                }
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                director?.ApplyFromSave();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                PlayerProgressSave.ResetAll();
                director?.ApplyFromSave();
            }
        }

        private static bool WasConfirmPressed()
        {
            return Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.JoystickButton0);
        }
    }
}
