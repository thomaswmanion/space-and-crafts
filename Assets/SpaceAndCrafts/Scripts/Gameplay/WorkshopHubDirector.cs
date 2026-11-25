using SpaceAndCrafts.Core;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Evolving Workshop hub — shelves/lights grow with save progress (Odyssey Cap kingdom energy).
    /// </summary>
    public sealed class WorkshopHubDirector : MonoBehaviour
    {
        [SerializeField] private Transform[] tierRoots;
        [SerializeField] private TextMesh statusLabel;
        [SerializeField] private TextMesh inventoryLabel;
        [SerializeField] private Light keyLight;

        private void Start()
        {
            ProgressSaveBridge.Ensure();
            ApplyFromSave();
        }

        public void ApplyFromSave()
        {
            var data = PlayerProgressSave.Current;
            var tier = Mathf.Clamp(data.workshopTier, 0, tierRoots != null ? tierRoots.Length : 0);

            if (tierRoots != null)
            {
                for (var i = 0; i < tierRoots.Length; i++)
                {
                    if (tierRoots[i] != null)
                    {
                        tierRoots[i].gameObject.SetActive(i < Mathf.Max(1, tier));
                    }
                }
            }

            if (keyLight != null)
            {
                keyLight.intensity = 0.85f + tier * 0.35f;
                keyLight.color = Color.Lerp(
                    new Color(1f, 0.85f, 0.65f),
                    new Color(0.75f, 0.55f, 1f),
                    Mathf.Clamp01(tier / 6f));
            }

            if (statusLabel != null)
            {
                statusLabel.text =
                    $"Splat's Workshop  ·  Tier {tier}\n" +
                    $"Worlds cleared {data.completedWorldIds.Count}  ·  Inventions {data.unlockedInventionIds.Count}\n" +
                    $"Wonders {data.claimedWonderIds.Count}  ·  Sparks {data.sparkCount}";
            }

            if (inventoryLabel != null)
            {
                inventoryLabel.text = data.unlockedInventionIds.Count == 0
                    ? "Workbench empty — craft something in a world!"
                    : "Inventions:\n- " + string.Join("\n- ", data.unlockedInventionIds);
            }
        }

        public void Configure(Transform[] tiers, TextMesh status, TextMesh inventory, Light light)
        {
            tierRoots = tiers;
            statusLabel = status;
            inventoryLabel = inventory;
            keyLight = light;
        }
    }
}
