using SpaceAndCrafts.Enemies;
using SpaceAndCrafts.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceAndCrafts.Core
{
    public sealed class RainbowSpaceship : MonoBehaviour, IInteractable
    {
        [SerializeField] private RainbowKingHostage rainbowKing;

        private bool travelMenuOpen;
        private string statusMessage;
        private float statusUntil;

        public bool IsUnlocked
        {
            get
            {
                int remainingEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length;
                return remainingEnemies == 0 && rainbowKing != null && rainbowKing.IsFreed;
            }
        }

        public string Prompt => IsUnlocked ? "Board the Rainbow Rider" : "Check the locked spaceship";

        public void Configure(RainbowKingHostage king)
        {
            rainbowKing = king;
        }

        public void Interact()
        {
            if (!IsUnlocked)
            {
                int remainingEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length;
                statusMessage = remainingEnemies > 0
                    ? $"The launch shield is active. Clear Rainbow World first! {remainingEnemies} enemies remain."
                    : "The Rainbow King must be rescued before the ship can launch.";
                statusUntil = Time.time + 5f;
                return;
            }

            travelMenuOpen = !travelMenuOpen;
        }

        private void Update()
        {
            if (!travelMenuOpen)
            {
                return;
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    travelMenuOpen = false;
                }
                else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                {
                    SelectRoute("World 2");
                }
                else if (Keyboard.current.digit3Key.wasPressedThisFrame)
                {
                    SelectRoute("World 3");
                }
                else if (Keyboard.current.digit4Key.wasPressedThisFrame)
                {
                    SelectRoute("World 4");
                }
                else if (Keyboard.current.digit5Key.wasPressedThisFrame)
                {
                    SelectRoute("World 5: Crystal Town");
                }
            }
        }

        private void SelectRoute(string destination)
        {
            travelMenuOpen = false;
            statusMessage = $"Rainbow route selected: {destination}. This route will activate when that world is built!";
            statusUntil = Time.time + 6f;
        }

        private void OnGUI()
        {
            GUIStyle box = new GUIStyle(GUI.skin.box)
            {
                fontSize = 23,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };

            if (travelMenuOpen)
            {
                GUI.Box(
                    new Rect(Screen.width * 0.5f - 360f, Screen.height * 0.5f - 145f, 720f, 290f),
                    "RAINBOW RIDER\n\n2 — World 2\n3 — World 3\n4 — World 4\n5 — Crystal Town\n\nEach destination follows a different rainbow route. Press Escape to leave.",
                    box);
            }
            else if (Time.time < statusUntil)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 340f, 35f, 680f, 100f), statusMessage, box);
            }
        }
    }
}
