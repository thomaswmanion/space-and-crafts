using SpaceAndCrafts.Art;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Prevents gulch softlock confusion: reminds player to craft before the mandatory launch.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class InventionRequiredHint : MonoBehaviour
    {
        [SerializeField] private string requiredInventionId = "boing_o_matic";
        [SerializeField] private string message = "Need Boing-O-Matic first — head back to the workshop!";
        [SerializeField] private float cooldown = 4f;

        private bool _hasInvention;
        private float _nextToastTime;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnEnable()
        {
            GameEvents.BuildCompleted += HandleBuildCompleted;
            RefreshInventionState();
        }

        private void OnDisable()
        {
            GameEvents.BuildCompleted -= HandleBuildCompleted;
        }

        private void HandleBuildCompleted(string inventionId)
        {
            if (InventionCompatibility.Satisfies(requiredInventionId, inventionId))
            {
                _hasInvention = true;
            }
        }

        private void RefreshInventionState()
        {
            var session = FindAnyObjectByType<LevelSession>();
            if (session != null && session.HasInvention(requiredInventionId))
            {
                _hasInvention = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time < _nextToastTime)
            {
                return;
            }

            if (other.GetComponentInParent<Player.SplatMotor>() == null)
            {
                return;
            }

            RefreshInventionState();
            if (_hasInvention)
            {
                return;
            }

            GameplayFeedback.Toast(message);
            TempCraftSparkBurst.Spawn(transform.position + Vector3.up * 0.5f, 4, 0.25f);
            _nextToastTime = Time.time + cooldown;
        }
    }
}
