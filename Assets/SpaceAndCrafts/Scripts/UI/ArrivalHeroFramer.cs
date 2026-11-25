using UnityEngine;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// First seconds of Arrival: keep Splat framed and lit so the hero is never “missing”.
    /// </summary>
    public sealed class ArrivalHeroFramer : MonoBehaviour
    {
        private float _until;
        private Transform _splat;
        private Light _rim;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            TrySpawn(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode _)
        {
            TrySpawn(scene);
        }

        private static void TrySpawn(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || scene.name == null || !scene.name.Contains("Arrival"))
            {
                return;
            }

            if (Object.FindAnyObjectByType<ArrivalHeroFramer>() != null)
            {
                return;
            }

            var go = new GameObject("SYS_ArrivalHeroFramer");
            go.AddComponent<ArrivalHeroFramer>();
        }

        private void Start()
        {
            _until = Time.unscaledTime + 6f;
            var splatGo = GameObject.Find("Splat_TEMP");
            _splat = splatGo != null ? splatGo.transform : null;
            if (_splat == null)
            {
                return;
            }

            var rimGo = new GameObject("LIT_SplatRim");
            rimGo.transform.SetParent(transform, false);
            _rim = rimGo.AddComponent<Light>();
            _rim.type = LightType.Point;
            _rim.color = new Color(1f, 0.85f, 1f);
            _rim.intensity = 1.35f;
            _rim.range = 5f;
        }

        private void LateUpdate()
        {
            if (_splat == null || _rim == null)
            {
                return;
            }

            // Rim only — do not fight PlatformingCameraRig.
            _rim.transform.position = _splat.position + new Vector3(-0.8f, 1.6f, -1.2f);
            _rim.enabled = Time.unscaledTime < _until + 8f;
            if (!_rim.enabled)
            {
                Destroy(gameObject);
            }
        }
    }
}
