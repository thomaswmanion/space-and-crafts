using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using SpaceAndCrafts.Core;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Title/Workshop world select ring — lists the available kingdoms and future destinations.
    /// </summary>
    public sealed class WorldSelectBoard : MonoBehaviour
    {
        [SerializeField] private string[] worldNames =
        {
            "01 Rainbow Kingdom",
            "02 Beeping Caves Kingdom",
            "03 Leaf Lands",
            "04 Bubble Bazaar",
            "05 Clockwork Crater",
            "06 Mountain Lands",
            "07 Echo Atelier",
            "08 Tide Tinker Yard",
            "09 Nebula Nursery",
            "10 Scrap Cathedral",
        };

        [SerializeField] private string[] sceneNames =
        {
            "RainbowWorld_Arrival",
            "World02_CorkscrewCanopy",
            "World03_MagnetMarsh",
            "World04_BubbleBazaar",
            "World05_ClockworkCrater",
            "World06_FrostedFilament",
            "World07_EchoAtelier",
            "World08_TideTinkerYard",
            "World09_NebulaNursery",
            "World10_ScrapCathedral",
        };

        [SerializeField] private TextMesh label;
        [SerializeField] private WorldCatalog worldCatalog;
        [SerializeField] private int index;
        private Color _readyLabelColor = Color.white;
        private int _previousStickDirection;

        private void Awake()
        {
            if (label == null)
            {
                var named = transform.Find("WorldSelect");
                if (named != null)
                {
                    label = named.GetComponent<TextMesh>();
                }
            }

            if (label != null)
            {
                _readyLabelColor = label.color;
            }

            Refresh();
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            var gamepad = Gamepad.current;
            var stickX = gamepad != null ? gamepad.leftStick.ReadValue().x : 0f;
            var stickDirection = stickX <= -0.65f ? -1 : stickX >= 0.65f ? 1 : 0;
            // Treat crossing the stick dead zone as a single nudge. Releasing to
            // neutral re-arms it, so a held analog stick cannot skip through every
            // future kingdom while the player is reading the destination label.
            var stickNudge = stickDirection != 0 && stickDirection != _previousStickDirection;
            _previousStickDirection = stickDirection;
            var moveLeft = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)
                || (gamepad != null && gamepad.dpad.left.wasPressedThisFrame)
                || (stickNudge && stickDirection < 0);
            var moveRight = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)
                || (gamepad != null && gamepad.dpad.right.wasPressedThisFrame)
                || (stickNudge && stickDirection > 0);
            var count = SelectionCount;
            if (count == 0 || moveLeft == moveRight)
            {
                return;
            }

            var direction = moveLeft ? -1 : 1;
            index = (Mathf.Clamp(index, 0, count - 1) + direction + count) % count;
            Refresh();
        }

        public void ConfirmSelection()
        {
            if (sceneNames == null || sceneNames.Length == 0)
            {
                return;
            }

            var i = Mathf.Clamp(index, 0, sceneNames.Length - 1);
            if (!IsPlayable(i))
            {
                GameplayFeedback.Toast("That kingdom is still being built. Rainbow Kingdom is ready now.");
                return;
            }

            var scene = sceneNames[i];
            if (Application.CanStreamedLevelBeLoaded(scene))
            {
                // Selecting a destination starts a new run there. Only the
                // Home-menu Continue action hydrates saved per-run progress.
                PlayerProgressSave.BeginFreshSession();
                PlayerProgressSave.ClearQuestSubstepsForWorld(scene);
                PlayerProgressSave.RecordWorldStart(scene);
                SceneManager.LoadScene(scene);
            }
            else
            {
                // Catalog readiness and Build Settings can briefly diverge while
                // a kingdom is being integrated. Never send a selected later
                // destination to Rainbow Kingdom as a silent fallback.
                GameplayFeedback.Toast($"{GetWorldName(i)} is not available in this build yet.");
            }
        }

        public string CurrentWorldLabel => SelectionCount > 0
            ? GetWorldLabel(Mathf.Clamp(index, 0, SelectionCount - 1))
            : "Rainbow Kingdom";

        private int SelectionCount => sceneNames != null && sceneNames.Length > 0
            ? sceneNames.Length
            : worldNames != null ? worldNames.Length : 0;

        private void Refresh()
        {
            var count = SelectionCount;
            if (label == null || count == 0)
            {
                return;
            }

            var i = Mathf.Clamp(index, 0, count - 1);
            var playable = IsPlayable(i);
            label.text = $"World  <  {GetWorldLabel(i)}  >";
            label.color = playable ? _readyLabelColor : Color.Lerp(_readyLabelColor, Color.gray, 0.48f);
        }

        private string GetWorldLabel(int worldIndex)
        {
            var name = GetWorldName(worldIndex);
            return IsPlayable(worldIndex) ? name : $"{name} · SOON";
        }

        private string GetWorldName(int worldIndex)
        {
            var definition = worldCatalog != null ? worldCatalog.GetByIndex(worldIndex + 1) : null;
            if (definition != null && !string.IsNullOrWhiteSpace(definition.displayName))
            {
                var displayName = definition.displayName.Trim();
                var regionSeparator = displayName.IndexOf('—');
                return regionSeparator > 0
                    ? displayName.Substring(0, regionSeparator).Trim()
                    : displayName;
            }

            if (worldNames != null && worldIndex >= 0 && worldIndex < worldNames.Length
                && !string.IsNullOrWhiteSpace(worldNames[worldIndex]))
            {
                return worldNames[worldIndex].Trim();
            }

            return $"World {worldIndex + 1:00}";
        }

        private bool IsPlayable(int worldIndex)
        {
            // The catalog is the source of truth for world readiness. If an
            // older menu has no catalog reference, keep World 01 as a safe
            // fallback and never unlock an unverified scaffold destination.
            if (worldCatalog == null)
            {
                return worldIndex == 0;
            }

            var definition = worldCatalog.GetByIndex(worldIndex + 1);
            return definition != null ? definition.systemsPlayable : worldIndex == 0;
        }
    }
}
