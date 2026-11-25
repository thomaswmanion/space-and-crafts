using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Enables a terrace (or other) Boing pad once the Arrival invention is crafted.
    /// Optional ghost foreshadows the locked pad before unlock.
    /// </summary>
    public sealed class BoingPadUnlocker : MonoBehaviour
    {
        [SerializeField] private string requiredInventionId = "boing_o_matic";
        [SerializeField] private GameObject padRoot;
        [SerializeField] private GameObject ghostRoot;
        [SerializeField] private string unlockToast = "Boing pad online!";
        [SerializeField] private bool showToast = true;

        private void Awake()
        {
            Refresh(false);
        }

        private void OnEnable()
        {
            GameEvents.BuildCompleted += HandleBuildCompleted;
            var session = FindAnyObjectByType<LevelSession>();
            if (session != null && session.HasInvention(requiredInventionId))
            {
                Refresh(true);
            }
        }

        private void OnDisable()
        {
            GameEvents.BuildCompleted -= HandleBuildCompleted;
        }

        private void HandleBuildCompleted(string inventionId)
        {
            if (!InventionCompatibility.Satisfies(requiredInventionId, inventionId))
            {
                return;
            }

            Refresh(true);
            var sparkAt = padRoot != null ? padRoot.transform.position : transform.position;
            TempCraftSparkBurst.Spawn(sparkAt + Vector3.up * 0.6f, 9, 0.4f);
            if (showToast)
            {
                GameplayFeedback.Toast(unlockToast);
            }
            else
            {
                TempSfxPlayer.PlayToastBlip();
            }
        }

        private void Refresh(bool unlocked)
        {
            if (padRoot != null)
            {
                padRoot.SetActive(unlocked);
            }

            if (ghostRoot != null)
            {
                ghostRoot.SetActive(!unlocked);
            }
        }
    }
}
