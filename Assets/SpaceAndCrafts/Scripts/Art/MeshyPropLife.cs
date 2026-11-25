using Unity.Profiling;
using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Restrained ambience for Meshy props. Grounded scenery stays planted; foliage
    /// can sway subtly, while emissive accents pulse without deforming object roots.
    /// </summary>
    public sealed class MeshyPropLife : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new("MeshyPropLife.Update");
        [SerializeField] private float bobMeters = 0.035f;
        [SerializeField] private float bobHz = 0.32f;
        [SerializeField] private float yawDeg = 3f;
        [SerializeField] private float squash = 0.01f;
        [SerializeField] private float emitPulse = 0.35f;
        [SerializeField] private float phase;

        private Vector3 _rest;
        private Vector3 _restScale;
        private Quaternion _restRot;
        private Renderer[] _renderers;
        private Color[] _baseEmit;
        private MaterialPropertyBlock _block;

        public void Configure(float bob, float hz, float yaw, float sq, float emit)
        {
            bobMeters = bob;
            bobHz = hz;
            yawDeg = yaw;
            squash = sq;
            emitPulse = emit;
        }

        public void ConfigureForArrival(Transform prop)
        {
            if (prop == null)
            {
                Configure(0f, 0.32f, 0f, 0f, 0.04f);
                return;
            }

            var name = prop.name ?? string.Empty;
            var isFoliage = name.Contains("Tree") || name.Contains("Plant");
            Configure(
                isFoliage ? 0.004f : 0f,
                isFoliage ? 0.16f : 0.32f,
                isFoliage ? 0.75f : 0f,
                0f,
                isFoliage ? 0.08f : 0.04f);
        }

        private void Awake()
        {
            _rest = transform.localPosition;
            _restScale = transform.localScale;
            _restRot = transform.localRotation;
            if (phase <= 0f)
            {
                phase = Random.Range(0f, Mathf.PI * 2f);
            }

            CacheRendererState();
        }

        private void CacheRendererState()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _baseEmit = new Color[_renderers.Length];
            _block = new MaterialPropertyBlock();
            for (var i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null)
                {
                    continue;
                }

                var mat = r.sharedMaterial;
                if (mat != null && mat.HasProperty("_EmissionColor"))
                {
                    _baseEmit[i] = mat.GetColor("_EmissionColor");
                }
                else
                {
                    _baseEmit[i] = new Color(0.15f, 0.08f, 0.25f) * 0.4f;
                }
            }
        }

        private void Update()
        {
            using (UpdateMarker.Auto()) UpdateCore();
        }

        private void UpdateCore()
        {
            var t = Time.time * bobHz * Mathf.PI * 2f + phase;
            var bob = Mathf.Sin(t);
            // Avoid a shared wobble/squash on rocks, gateways, and other hard props.
            // Only explicitly configured foliage receives the small authored sway.
            transform.localPosition = _rest + Vector3.up * (bob * bobMeters);
            transform.localRotation = _restRot * Quaternion.Euler(
                Mathf.Sin(t * 0.55f) * (yawDeg * 0.45f),
                Mathf.Sin(t * 0.7f) * yawDeg,
                Mathf.Cos(t * 0.4f) * (yawDeg * 0.35f));
            var s = 1f + bob * squash;
            transform.localScale = new Vector3(_restScale.x * (2f - s), _restScale.y * s, _restScale.z * (2f - s));

            // Unity can reload managed scripts while a Play Mode session is active.
            // Serialized fields survive that reload, but this native property-block
            // handle does not. Rebuild the transient cache before the next update.
            if (_renderers == null || _baseEmit == null || _block == null
                || _baseEmit.Length != _renderers.Length)
            {
                CacheRendererState();
            }

            var pulse = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t * 1.6f));
            for (var i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null)
                {
                    continue;
                }

                r.GetPropertyBlock(_block);
                _block.SetColor("_EmissionColor", _baseEmit[i] + Color.white * (emitPulse * pulse * 0.28f));
                r.SetPropertyBlock(_block);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void HookSceneLoaded()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            WireArrivalMeshy(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode _)
        {
            WireArrivalMeshy(scene);
        }

        private static void WireArrivalMeshy(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || scene.name == null || !scene.name.Contains("Arrival"))
            {
                return;
            }

            // Meshy hero duplicate next to organic Splat reads as a creepy statue — hide it.
            var meshyHero = GameObject.Find("ART_Meshy_Splat_Hero");
            if (meshyHero != null && GameObject.Find("Splat_TEMP") != null)
            {
                meshyHero.SetActive(false);
            }

            foreach (var t in Object.FindObjectsByType<Transform>())
            {
                if (t == null || !t.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var n = t.name;
                if (n == null)
                {
                    continue;
                }

                // Only prop roots / visual swaps — not every nested mesh child.
                var isPropRoot = n.StartsWith("ART_Meshy_");
                var isVisual = n.Contains("Meshy") && n.EndsWith("Visual");
                if (!isPropRoot && !isVisual)
                {
                    continue;
                }

                if (t.GetComponent<MeshyPropLife>() != null)
                {
                    continue;
                }

                // Don't stack life under a parent that already lives.
                var parentLife = t.parent != null ? t.parent.GetComponentInParent<MeshyPropLife>() : null;
                if (parentLife != null)
                {
                    continue;
                }

                var isGameplayActor = n.Contains("Enemy") || n.Contains("Mite") || n.Contains("Spitter")
                    || n.Contains("Roller") || n.Contains("Sentinel") || n.Contains("Boss")
                    || (n.Contains("Scrap") && !n.Contains("RockCluster"));
                if (isGameplayActor)
                {
                    // Keep any imported skeletal idle clip, but don't move an AI root
                    // with the generic set-dressing bob/turn/squash driver.
                    if (t.GetComponent<MeshyIdleAnim>() == null)
                    {
                        t.gameObject.AddComponent<MeshyIdleAnim>();
                    }

                    continue;
                }

                var life = t.gameObject.AddComponent<MeshyPropLife>();
                life.ConfigureForArrival(t);

                if (t.GetComponent<MeshyIdleAnim>() == null)
                {
                    t.gameObject.AddComponent<MeshyIdleAnim>();
                }
            }

            var label = GameObject.Find("ART_MeshyShowcaseLabel");
            if (label != null)
            {
                var tm = label.GetComponent<TextMesh>();
                if (tm != null)
                {
                    tm.text = "Living scrap curios";
                    tm.color = new Color(1f, 0.92f, 0.55f, 0.75f);
                    tm.characterSize = 0.08f;
                }
            }
        }
    }
}
