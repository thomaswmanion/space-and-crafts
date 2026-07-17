using SpaceAndCrafts.Enemies;
using SpaceAndCrafts.Interaction;
using UnityEngine;

namespace SpaceAndCrafts.Core
{
    public sealed class LevelGoal : MonoBehaviour, IInteractable
    {
        [SerializeField, Min(0)] private int requiredCrystals = 6;
        [SerializeField] private string levelName = "Rainbow Rescue";

        private string statusMessage;
        private float statusUntil;

        public string Prompt => "Open the star gate";

        public void Interact()
        {
            int crystals = GameManager.Instance == null ? 0 : GameManager.Instance.CrystalCount;
            int enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length;
            RainbowKingHostage king = FindAnyObjectByType<RainbowKingHostage>();
            if (crystals < requiredCrystals)
            {
                statusMessage = $"The gate needs {requiredCrystals} crystals. You have {crystals}.";
            }
            else if (enemies > 0)
            {
                statusMessage = $"The Rainbow Kingdom is not safe yet! {enemies} enemies remain.";
            }
            else if (king != null && !king.IsFreed)
            {
                statusMessage = "The Rainbow King is still trapped inside the castle!";
            }
            else
            {
                statusMessage = $"AREA CLEARED: {levelName}! The Rainbow Rider spaceship is now ready for launch.";
            }
            statusUntil = Time.time + 5f;
        }

        private void OnGUI()
        {
            if (Time.time >= statusUntil)
            {
                return;
            }

            GUIStyle style = new GUIStyle(GUI.skin.box) { fontSize = 24, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(Screen.width * 0.5f - 320f, 35f, 640f, 100f), statusMessage, style);
        }
    }
}
