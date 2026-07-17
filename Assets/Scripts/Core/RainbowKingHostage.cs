using SpaceAndCrafts.Enemies;
using UnityEngine;

namespace SpaceAndCrafts.Core
{
    public sealed class RainbowKingHostage : MonoBehaviour
    {
        [SerializeField] private EnemyHealth robotBoss;
        [SerializeField] private GameObject energyCage;

        private float rescueMessageUntil;

        public bool IsFreed { get; private set; }

        public void Configure(EnemyHealth boss, GameObject cage)
        {
            robotBoss = boss;
            energyCage = cage;
        }

        private void Update()
        {
            if (!IsFreed && (robotBoss == null || !robotBoss.IsAlive))
            {
                IsFreed = true;
                rescueMessageUntil = Time.time + 7f;
                if (energyCage != null)
                {
                    energyCage.SetActive(false);
                }
            }
        }

        private void OnGUI()
        {
            if (Time.time >= rescueMessageUntil)
            {
                return;
            }

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 25,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Box(
                new Rect(Screen.width * 0.5f - 340f, 35f, 680f, 105f),
                "The Rainbow Warden is defeated! Splat has freed the Rainbow King!",
                style);
        }
    }
}
