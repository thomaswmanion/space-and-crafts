using SpaceAndCrafts.Art;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// TEMP presentation: brightens dormant rainbow gateways when the level completes.
    /// </summary>
    public sealed class GatewayRevealPresenter : MonoBehaviour
    {
        [SerializeField] private Renderer[] dormantGatewayRenderers;
        [SerializeField] private Color awakeMultiply = new Color(1.8f, 1.8f, 1.8f, 1f);
        [SerializeField] private float revealDuration = 1.25f;

        private Bounds _cachedGatewayBounds;
        private bool _hasCachedGatewayBounds;

        private Mesh _signalMesh;
        private GameObject _activeSignalPulse;
        private Material _activeSignalMaterial;
        private GameObject _activeVesperPortrait;
        private SpriteRenderer _activeVesperPortraitRenderer;
        private Vector3 _vesperPortraitRestScale;
        private Renderer _signalBeaconRenderer;
        private Transform _signalBeacon;
        private Material _originalBeaconMaterial;
        private Material _signalBeaconMaterial;
        private Vector3 _beaconRestScale;

        public void SetDormantGatewayRenderers(Renderer[] renderers)
        {
            dormantGatewayRenderers = renderers ?? System.Array.Empty<Renderer>();
            _hasCachedGatewayBounds = false;
            foreach (var renderer in dormantGatewayRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!_hasCachedGatewayBounds)
                {
                    _cachedGatewayBounds = renderer.bounds;
                    _hasCachedGatewayBounds = true;
                }
                else
                {
                    _cachedGatewayBounds.Encapsulate(renderer.bounds);
                }
            }

            Debug.Log($"[Space & Crafts] Cached victory gateway bounds: found={_hasCachedGatewayBounds}, center={_cachedGatewayBounds.center:F2}, size={_cachedGatewayBounds.size:F2}, renderers={dormantGatewayRenderers.Length}.");
        }

        public bool TryGetGatewayBounds(out Bounds bounds)
        {
            if (_hasCachedGatewayBounds)
            {
                bounds = _cachedGatewayBounds;
                return true;
            }

            bounds = default;
            var found = false;
            if (dormantGatewayRenderers == null)
            {
                return false;
            }

            foreach (var renderer in dormantGatewayRenderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found;
        }

        private void OnEnable()
        {
            GameEvents.LevelCompleted += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.LevelCompleted -= HandleLevelCompleted;
            CleanupSignal();
        }

        private void HandleLevelCompleted()
        {
            CleanupSignal();
            StopAllCoroutines();
            StartCoroutine(RevealRoutine());
        }

        private System.Collections.IEnumerator RevealRoutine()
        {
            if (dormantGatewayRenderers == null || dormantGatewayRenderers.Length == 0)
            {
                yield break;
            }

            GameplayFeedback.Toast(
                "Rattle: “I was afraid the road would break. You found another way.”",
                2.8f);

            var start = new Color[dormantGatewayRenderers.Length];
            var end = new Color[dormantGatewayRenderers.Length];
            for (var i = 0; i < dormantGatewayRenderers.Length; i++)
            {
                if (dormantGatewayRenderers[i] == null)
                {
                    continue;
                }

                var mat = dormantGatewayRenderers[i].material;
                start[i] = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                end[i] = start[i] * awakeMultiply;
                end[i].a = 1f;
                if (i % 4 == 0)
                {
                    TempCraftSparkBurst.Spawn(dormantGatewayRenderers[i].bounds.center, 8, 0.5f);
                }
            }

            var elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / revealDuration);
                for (var i = 0; i < dormantGatewayRenderers.Length; i++)
                {
                    if (dormantGatewayRenderers[i] == null)
                    {
                        continue;
                    }

                    var mat = dormantGatewayRenderers[i].material;
                    var c = Color.Lerp(start[i], end[i], t);
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", c);
                    }

                    mat.color = c;
                }

                yield return null;
            }

            // Let Rattle's concession finish before Vesper's promised reply replaces
            // the single shared toast, making the repair resolution legible as a beat.
            yield return new WaitForSeconds(1.55f);
            yield return SendThreeTapSignal();
        }

        private System.Collections.IEnumerator SendThreeTapSignal()
        {
            Renderer farGatewayKeystone = null;
            foreach (var renderer in dormantGatewayRenderers)
            {
                if (renderer != null && renderer.name.Contains("GatewayKeystone")
                    && (farGatewayKeystone == null || renderer.bounds.center.z > farGatewayKeystone.bounds.center.z))
                {
                    farGatewayKeystone = renderer;
                }
            }

            if (farGatewayKeystone == null)
            {
                // Imported gateway models do not expose the graybox keystone name.
                // Fall back to the furthest visible piece so the three-tap signal
                // still travels to the newly commissioned Meshy gateway silhouettes.
                foreach (var renderer in dormantGatewayRenderers)
                {
                    if (renderer != null && (farGatewayKeystone == null
                        || renderer.bounds.center.z > farGatewayKeystone.bounds.center.z))
                    {
                        farGatewayKeystone = renderer;
                    }
                }

                if (farGatewayKeystone == null) yield break;
            }

            var beaconObject = GameObject.Find("ART_GatewayBeacon");
            var beacon = beaconObject != null ? beaconObject.transform : null;
            var beaconRenderer = beaconObject != null ? beaconObject.GetComponent<Renderer>() : null;
            var center = beaconRenderer != null
                ? beaconRenderer.bounds.center
                : farGatewayKeystone.bounds.center - Vector3.up * 3f;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                yield break;
            }

            // Arrival already has a handmade beacon above the terrace. Let Vesper's reply
            // travel through that landmark so the signal has a visible source in the world.
            var originalBeaconMaterial = beaconRenderer != null ? beaconRenderer.sharedMaterial : null;
            var beaconMaterial = originalBeaconMaterial != null ? new Material(originalBeaconMaterial) : null;
            var beaconRestScale = beacon != null ? beacon.localScale : Vector3.one;
            _signalBeaconRenderer = beaconRenderer;
            _signalBeacon = beacon;
            _originalBeaconMaterial = originalBeaconMaterial;
            _signalBeaconMaterial = beaconMaterial;
            _beaconRestScale = beaconRestScale;
            var beaconBaseColor = Color.white;
            var beaconBaseEmission = Color.black;
            if (beaconMaterial != null)
            {
                beaconRenderer.sharedMaterial = beaconMaterial;
                beaconBaseColor = beaconMaterial.HasProperty("_BaseColor")
                    ? beaconMaterial.GetColor("_BaseColor") : beaconMaterial.color;
                if (beaconMaterial.HasProperty("_EmissionColor"))
                {
                    beaconBaseEmission = beaconMaterial.GetColor("_EmissionColor");
                    beaconMaterial.EnableKeyword("_EMISSION");
                }
            }

            GameplayFeedback.Toast("Vesper: “I’m here, Splat. Keep looking.”", 2.8f, playBlip: false);
            GameEvents.RaiseGatewaySignalReceived();
            PlayThreeTapChime(center);
            var portraitSprite = Resources.Load<Sprite>("Characters/VesperSignalPortrait");
            var portraitCamera = Camera.main;
            if (portraitSprite != null)
            {
                _activeVesperPortrait = new GameObject("ART_VesperSignalHologram");
                _activeVesperPortrait.transform.position = center + Vector3.up * 4.2f;
                _activeVesperPortrait.transform.localScale = Vector3.one * 0.32f;
                _vesperPortraitRestScale = _activeVesperPortrait.transform.localScale;
                _activeVesperPortraitRenderer = _activeVesperPortrait.AddComponent<SpriteRenderer>();
                _activeVesperPortraitRenderer.sprite = portraitSprite;
                _activeVesperPortraitRenderer.color = new Color(0.78f, 0.94f, 1f, 0.82f);
                _activeVesperPortraitRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _activeVesperPortraitRenderer.receiveShadows = false;
                if (portraitCamera != null)
                {
                    var facing = portraitCamera.transform.position - _activeVesperPortrait.transform.position;
                    if (facing.sqrMagnitude > 0.001f)
                    {
                        _activeVesperPortrait.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
                    }
                }
            }

            var mesh = OrganicMeshFactory.Torus(0.38f, 0.045f, 40, 12);
            _signalMesh = mesh;
            for (var tap = 0; tap < 3; tap++)
            {
                var pulse = new GameObject($"ART_VesperSignalTap_{tap + 1}");
                _activeSignalPulse = pulse;
                pulse.transform.SetPositionAndRotation(center, Quaternion.Euler(-90f, 0f, 0f));
                pulse.AddComponent<MeshFilter>().sharedMesh = mesh;
                var material = MakeSignalMaterial(shader);
                _activeSignalMaterial = material;
                pulse.AddComponent<MeshRenderer>().sharedMaterial = material;

                var elapsed = 0f;
                const float duration = 0.42f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    pulse.transform.localScale = Vector3.one * Mathf.Lerp(0.15f, 2.4f, t);
                    var color = new Color(0.35f, 0.88f, 1f, 0.82f * (1f - t));
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    else material.color = color;
                    if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", new Color(0.2f, 0.72f, 1f, 1f) * (1.4f * (1f - t)));
                    if (_activeVesperPortrait != null)
                    {
                        var glow = Mathf.Sin(t * Mathf.PI);
                        _activeVesperPortrait.transform.localScale = _vesperPortraitRestScale * (1f + glow * 0.035f);
                        if (portraitCamera != null)
                        {
                            var facing = portraitCamera.transform.position - _activeVesperPortrait.transform.position;
                            if (facing.sqrMagnitude > 0.001f)
                            {
                                _activeVesperPortrait.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
                            }
                        }

                        if (_activeVesperPortraitRenderer != null)
                        {
                            _activeVesperPortraitRenderer.color = new Color(0.78f + glow * 0.12f,
                                0.94f + glow * 0.06f, 1f, 0.72f + glow * 0.18f);
                        }
                    }

                    if (beacon != null && beaconMaterial != null)
                    {
                        var glow = Mathf.Exp(-7f * t);
                        var beaconColor = Color.Lerp(beaconBaseColor, new Color(0.32f, 0.9f, 1f, 1f), glow * 0.78f);
                        if (beaconMaterial.HasProperty("_BaseColor")) beaconMaterial.SetColor("_BaseColor", beaconColor);
                        else beaconMaterial.color = beaconColor;
                        if (beaconMaterial.HasProperty("_EmissionColor"))
                        {
                            var signalEmission = new Color(0.24f, 0.82f, 1f, 1f) * 2.2f;
                            beaconMaterial.SetColor("_EmissionColor", Color.Lerp(beaconBaseEmission, signalEmission, glow));
                        }
                        beacon.localScale = beaconRestScale * (1f + glow * 0.045f);
                    }
                    yield return null;
                }

                Destroy(pulse);
                Destroy(material);
                _activeSignalPulse = null;
                _activeSignalMaterial = null;
                yield return new WaitForSeconds(0.18f);
            }

            CleanupSignal();
            GameplayFeedback.Toast("Splat: “Signal received, map thief. I’m coming.”", 2.1f, playBlip: false);
        }

        private void CleanupSignal()
        {
            DestroyTemporary(_activeSignalPulse);
            DestroyTemporary(_activeSignalMaterial);
            DestroyTemporary(_signalMesh);
            DestroyTemporary(_activeVesperPortrait);
            _activeSignalPulse = null;
            _activeSignalMaterial = null;
            _signalMesh = null;
            _activeVesperPortrait = null;
            _activeVesperPortraitRenderer = null;
            _vesperPortraitRestScale = Vector3.one;

            if (_signalBeacon != null)
            {
                _signalBeacon.localScale = _beaconRestScale;
            }

            if (_signalBeaconRenderer != null && _originalBeaconMaterial != null)
            {
                _signalBeaconRenderer.sharedMaterial = _originalBeaconMaterial;
            }

            DestroyTemporary(_signalBeaconMaterial);
            _signalBeacon = null;
            _signalBeaconRenderer = null;
            _originalBeaconMaterial = null;
            _signalBeaconMaterial = null;
        }

        private static void DestroyTemporary(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        private static void PlayThreeTapChime(Vector3 position)
        {
            const int sampleRate = 22050;
            const float tapSpacing = 0.6f;
            const float tapDuration = 0.34f;
            const int tapCount = 3;
            var tapFrequencies = new[] { 523.25f, 659.25f, 783.99f };
            var clipDuration = (tapCount - 1) * tapSpacing + tapDuration;
            var sampleCount = Mathf.CeilToInt(sampleRate * clipDuration);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var time = i / (float)sampleRate;
                var value = 0f;
                for (var tap = 0; tap < tapCount; tap++)
                {
                    var localTime = time - tap * tapSpacing;
                    if (localTime < 0f || localTime > tapDuration)
                    {
                        continue;
                    }

                    var attack = 1f - Mathf.Exp(-localTime * 90f);
                    var decay = Mathf.Exp(-8.5f * localTime);
                    var frequency = tapFrequencies[tap];
                    var fundamental = Mathf.Sin(2f * Mathf.PI * frequency * localTime);
                    var second = Mathf.Sin(2f * Mathf.PI * frequency * 2.76f * localTime) * 0.22f;
                    var third = Mathf.Sin(2f * Mathf.PI * frequency * 4.08f * localTime) * 0.09f;
                    var shimmer = Mathf.Sin(2f * Mathf.PI * frequency * 6.7f * localTime) * 0.035f;
                    value += (fundamental + second + third + shimmer) * attack * decay * 0.18f;
                }

                samples[i] = value;
            }

            var clip = AudioClip.Create("Vesper_ThreeTap_Response", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);

            var chime = new GameObject("ART_VesperThreeTapChime");
            chime.transform.position = position;
            var source = chime.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0.35f;
            source.volume = 0.48f;
            source.minDistance = 8f;
            source.maxDistance = 110f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.PlayOneShot(clip);

            Object.Destroy(chime, clipDuration + 0.1f);
            Object.Destroy(clip, clipDuration + 0.1f);
        }

        private static Material MakeSignalMaterial(Shader shader)
        {
            var material = new Material(shader) { name = "Mat_VesperThreeTapSignal" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.35f, 0.88f, 1f, 0.82f));
            else material.color = new Color(0.35f, 0.88f, 1f, 0.82f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.2f, 0.72f, 1f, 1f) * 1.4f);
            }
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetInt("_ZWrite", 0);
                material.renderQueue = 3000;
            }
            return material;
        }
    }
}
