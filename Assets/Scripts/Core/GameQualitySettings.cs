using UnityEngine;

namespace SpaceAndCrafts.Core
{
    public sealed class GameQualitySettings : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Time.fixedDeltaTime = 1f / 60f;
            QualitySettings.vSyncCount = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.lodBias = 1.4f;
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 60f;
            QualitySettings.shadowCascades = 2;
            QualitySettings.realtimeReflectionProbes = true;
        }
    }
}
