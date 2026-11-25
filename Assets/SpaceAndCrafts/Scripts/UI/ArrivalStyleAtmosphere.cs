using SpaceAndCrafts.Art;
using UnityEngine;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Arrival's Nordic twilight sky.
    /// Runtime-only — does not replace authored geometry; frames the fjord when HUD is ignored.
    /// </summary>
    public sealed class ArrivalStyleAtmosphere : MonoBehaviour
    {
        private Camera _mainCamera;
        private CameraClearFlags _previousClearFlags;
        private Color _previousBackgroundColor;
        private Material _previousSkybox;
        private Material _arrivalSkybox;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Try(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode _)
        {
            Try(scene);
        }

        private static void Try(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || scene.name == null || !scene.name.Contains("Arrival"))
            {
                return;
            }

            if (Object.FindAnyObjectByType<ArrivalStyleAtmosphere>() != null)
            {
                return;
            }

            var go = new GameObject("SYS_ArrivalStyleAtmosphere");
            go.AddComponent<ArrivalStyleAtmosphere>().Build();
        }

        private void Build()
        {
            ConfigureNordicSky();

            RenderSettings.ambientIntensity = Mathf.Min(RenderSettings.ambientIntensity, 0.55f);

        }

        private void ConfigureNordicSky()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                return;
            }

            _previousClearFlags = _mainCamera.clearFlags;
            _previousBackgroundColor = _mainCamera.backgroundColor;
            _previousSkybox = RenderSettings.skybox;

            var panorama = Resources.Load<Texture2D>("Environment/ArrivalNordicFjordPanoramaPaintedV1")
                ?? Resources.Load<Texture2D>("Environment/ArrivalNordicFjordPanorama");
            var panoramaShader = panorama != null ? Shader.Find("Skybox/Panoramic") : null;
            var usePanorama = panoramaShader != null;
            var shader = usePanorama ? panoramaShader : Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                _mainCamera.backgroundColor = new Color(0.27f, 0.39f, 0.52f);
                return;
            }

            _arrivalSkybox = new Material(shader) { name = "Mat_ArrivalNordicFjordSky" };
            if (usePanorama)
            {
                _arrivalSkybox.SetTexture("_MainTex", panorama);
                if (_arrivalSkybox.HasProperty("_ImageType")) _arrivalSkybox.SetFloat("_ImageType", 0f);
                // Skybox/Panoramic enum: 0 = six-frame cubemap, 1 = equirectangular latitude/longitude.
                if (_arrivalSkybox.HasProperty("_Mapping")) _arrivalSkybox.SetFloat("_Mapping", 1f);
                if (_arrivalSkybox.HasProperty("_Rotation")) _arrivalSkybox.SetFloat("_Rotation", 0f);
                if (_arrivalSkybox.HasProperty("_Tint"))
                {
                    _arrivalSkybox.SetColor("_Tint", new Color(0.82f, 0.9f, 1f, 1f));
                }

                if (_arrivalSkybox.HasProperty("_Exposure"))
                {
                    _arrivalSkybox.SetFloat("_Exposure", 0.78f);
                }
            }
            else
            {
                if (_arrivalSkybox.HasProperty("_SkyTint"))
                {
                    _arrivalSkybox.SetColor("_SkyTint", new Color(0.58f, 0.68f, 0.8f));
                }

                if (_arrivalSkybox.HasProperty("_GroundColor"))
                {
                    _arrivalSkybox.SetColor("_GroundColor", new Color(0.22f, 0.31f, 0.33f));
                }

                if (_arrivalSkybox.HasProperty("_AtmosphereThickness"))
                {
                    _arrivalSkybox.SetFloat("_AtmosphereThickness", 1.15f);
                }

                if (_arrivalSkybox.HasProperty("_Exposure"))
                {
                    _arrivalSkybox.SetFloat("_Exposure", 0.78f);
                }

                // The aurora curtains and beacon carry the bright focal light; avoid a default sun disk.
                if (_arrivalSkybox.HasProperty("_SunDisk"))
                {
                    _arrivalSkybox.SetFloat("_SunDisk", 0f);
                }
            }

            RenderSettings.skybox = _arrivalSkybox;
            _mainCamera.clearFlags = CameraClearFlags.Skybox;
        }

        private void OnDestroy()
        {
            if (_mainCamera != null)
            {
                _mainCamera.clearFlags = _previousClearFlags;
                _mainCamera.backgroundColor = _previousBackgroundColor;
            }

            if (_arrivalSkybox != null && RenderSettings.skybox == _arrivalSkybox)
            {
                RenderSettings.skybox = _previousSkybox;
            }

            if (_arrivalSkybox != null)
            {
                if (Application.isPlaying) Destroy(_arrivalSkybox);
                else DestroyImmediate(_arrivalSkybox);
            }
        }

    }
}
