using UnityEngine;

namespace SpaceAndCrafts.Audio
{
    /// <summary>
    /// Soft zone-based music color — shifts WorldMusicDirector combat/ambient blend by volume.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ZoneAmbientDirector : MonoBehaviour
    {
        [SerializeField] private float ambientBoost = 0.15f;

        private WorldMusicDirector _music;
        private int _playersInside;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<Player.SplatMotor>() == null)
            {
                return;
            }

            _playersInside++;
            if (_playersInside == 1)
            {
                EnsureMusic()?.SetCombatIntensity(Mathf.Clamp01(ambientBoost));
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<Player.SplatMotor>() == null)
            {
                return;
            }

            _playersInside = Mathf.Max(0, _playersInside - 1);
            if (_playersInside == 0)
            {
                EnsureMusic()?.SetCombatIntensity(0f);
            }
        }

        private WorldMusicDirector EnsureMusic()
        {
            if (_music == null)
            {
                _music = FindAnyObjectByType<WorldMusicDirector>();
            }

            return _music;
        }
    }
}
