using SpaceAndCrafts.Core;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.Enemies;
using UnityEngine;

namespace SpaceAndCrafts.UI
{
    public sealed class GameHud : MonoBehaviour
    {
        private PlayerHealth health;
        private PlayerAbilities abilities;
        private PlayerCombat combat;
        private EnemyHealth boss;

        private void Start()
        {
            health = FindAnyObjectByType<PlayerHealth>();
            abilities = FindAnyObjectByType<PlayerAbilities>();
            combat = FindAnyObjectByType<PlayerCombat>();
            foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (enemy.name.Contains("BOSS"))
                {
                    boss = enemy;
                    break;
                }
            }
        }

        private void OnGUI()
        {
            int count = GameManager.Instance == null ? 0 : GameManager.Instance.CrystalCount;
            GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            GUIStyle help = new GUIStyle(GUI.skin.label) { fontSize = 17 };
            GUI.Label(new Rect(20f, 15f, 400f, 40f), $"RAINBOW KINGDOM   ◆ {count}", title);
            int currentHealth = health == null ? 0 : health.CurrentHealth;
            int maxHealth = health == null ? 0 : health.MaxHealth;
            GUI.Label(new Rect(20f, 52f, 600f, 30f), $"Health: {new string('♥', currentHealth)}{new string('♡', Mathf.Max(0, maxHealth - currentHealth))}", help);

            string invisibleStatus = "READY";
            if (abilities != null && abilities.IsInvisible)
            {
                invisibleStatus = $"ACTIVE {abilities.InvisibleTimeRemaining:0.0}s";
            }
            else if (abilities != null && abilities.InvisibleCooldownRemaining > 0f)
            {
                invisibleStatus = $"COOLDOWN {abilities.InvisibleCooldownRemaining:0.0}s";
            }

            string waterStatus = combat != null && combat.HasWaterGun ? "Water Gun: Q / Left Trigger" : "Find the Water Gun!";
            GUI.Label(new Rect(20f, 80f, 650f, 30f), $"Invisible: {invisibleStatus}   •   {waterStatus}", help);
            GUI.Label(new Rect(20f, Screen.height - 36f, 950f, 30f), "WASD move • Space jump/stomp • F/Left Click punch • Shift invisible • E interact • Right Mouse camera", help);

            if (boss != null && boss.IsAlive)
            {
                float width = Mathf.Min(620f, Screen.width * 0.55f);
                float x = (Screen.width - width) * 0.5f;
                float ratio = boss.MaxHealth <= 0 ? 0f : (float)boss.CurrentHealth / boss.MaxHealth;
                GUI.Box(new Rect(x, Screen.height - 92f, width, 30f), string.Empty);
                Color oldColor = GUI.color;
                GUI.color = new Color(0.85f, 0.08f, 0.15f);
                GUI.Box(new Rect(x + 4f, Screen.height - 88f, (width - 8f) * ratio, 22f), string.Empty);
                GUI.color = oldColor;
                GUI.Label(new Rect(x, Screen.height - 123f, width, 30f), "THE RAINBOW WARDEN", new GUIStyle(help) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            }
        }
    }
}
