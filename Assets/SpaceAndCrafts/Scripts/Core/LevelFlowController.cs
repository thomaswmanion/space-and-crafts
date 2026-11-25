using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.Core
{
    /// <summary>
    /// Scene-level restart / complete glue. Not a god GameManager.
    /// </summary>
    public sealed class LevelFlowController : MonoBehaviour
    {
        [SerializeField] private string workshopSceneName = "WorkshopHub";
        [SerializeField] private float returnToHubDelay = 7.1f;

        private bool _returning;

        private void OnEnable()
        {
            GameEvents.LevelRestartRequested += ReloadActiveScene;
            GameEvents.LevelCompleted += HandleLevelCompleted;
            ProgressSaveBridge.Ensure();
        }

        private void OnDisable()
        {
            GameEvents.LevelRestartRequested -= ReloadActiveScene;
            GameEvents.LevelCompleted -= HandleLevelCompleted;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                GameEvents.RaiseLevelRestartRequested();
            }
        }

        private void ReloadActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            PlayerProgressSave.BeginFreshSession();
            SceneManager.LoadScene(scene.name);
        }

        private void HandleLevelCompleted()
        {
            Debug.Log("[Space & Crafts] Level complete — returning to Workshop hub.");
            if (_returning)
            {
                return;
            }

            _returning = true;
            Invoke(nameof(ReturnToWorkshop), Mathf.Max(0.5f, returnToHubDelay));
        }

        private void ReturnToWorkshop()
        {
            if (Application.CanStreamedLevelBeLoaded(workshopSceneName))
            {
                SceneManager.LoadScene(workshopSceneName);
                return;
            }

            // Fallback if hub not in build settings yet.
            if (Application.CanStreamedLevelBeLoaded("HomeMenu"))
            {
                SceneManager.LoadScene("HomeMenu");
            }
        }
    }
}
