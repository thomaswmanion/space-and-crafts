using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.Cameras
{
    /// <summary>
    /// TEMP opening beat: briefly aims the orbit camera at the primary gateway landmark.
    /// </summary>
    public sealed class ArrivalOpeningVista : MonoBehaviour
    {
        [SerializeField] private PlatformingCameraRig cameraRig;
        [SerializeField] private Transform lookTarget;
        [SerializeField] private float delay = 0.25f;
        [SerializeField] private float duration = 1.8f;
        [SerializeField] private float pitchDegrees = 12f;

        private Renderer _signalBeaconRenderer;
        private Transform _signalBeacon;
        private Material _signalOriginalMaterial;
        private Material _signalMaterial;
        private Vector3 _signalRestScale;
        private GameObject _signalPortrait;
        private SpriteRenderer _signalPortraitRenderer;
        private Vector3 _signalPortraitRestScale;

        private void OnDestroy()
        {
            RestoreDistantBeaconSignal();
        }

        private void Start()
        {
            StartCoroutine(ShowDistantBeaconSignal());

            if (cameraRig == null)
            {
                cameraRig = FindAnyObjectByType<PlatformingCameraRig>();
            }

            if (lookTarget == null)
            {
                var gateway = GameObject.Find("TEMP_ART_RainbowGateway_Primary");
                if (gateway != null)
                {
                    lookTarget = gateway.transform;
                }
            }

            if (cameraRig == null || lookTarget == null)
            {
                return;
            }

            StartCoroutine(GuideAfterDelay());
        }

        private System.Collections.IEnumerator GuideAfterDelay()
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            var point = lookTarget.position + Vector3.up * 6f;
            cameraRig.GuideLookAt(point, duration, pitchDegrees);
            StoryBeatDirector.Ensure().PlayLookAt(lookTarget, duration * 0.35f);
        }

        private System.Collections.IEnumerator ShowDistantBeaconSignal()
        {
            // Give the opening promise a quiet in-world echo while Splat first sees the gate.
            // The finale remains the brighter, audible answer with expanding signal rings.
            yield return new WaitForSeconds(0.55f);

            var beaconObject = GameObject.Find("ART_GatewayBeacon");
            var beaconRenderer = beaconObject != null ? beaconObject.GetComponent<Renderer>() : null;
            if (beaconObject == null || beaconRenderer == null || beaconRenderer.sharedMaterial == null)
            {
                yield break;
            }

            var beacon = beaconObject.transform;
            var restScale = beacon.localScale;
            var originalMaterial = beaconRenderer.sharedMaterial;
            var signalMaterial = new Material(originalMaterial) { name = "ArrivalOpeningSignal_Instance" };
            _signalBeaconRenderer = beaconRenderer;
            _signalBeacon = beacon;
            _signalOriginalMaterial = originalMaterial;
            _signalMaterial = signalMaterial;
            _signalRestScale = restScale;
            beaconRenderer.sharedMaterial = signalMaterial;
            CreateSignalPortrait(beaconRenderer.bounds.center + Vector3.up * 4.2f);

            var baseColor = signalMaterial.HasProperty("_BaseColor")
                ? signalMaterial.GetColor("_BaseColor") : signalMaterial.color;
            var baseEmission = signalMaterial.HasProperty("_EmissionColor")
                ? signalMaterial.GetColor("_EmissionColor") : Color.black;
            if (signalMaterial.HasProperty("_EmissionColor"))
            {
                signalMaterial.EnableKeyword("_EMISSION");
            }

            const float pulseDuration = 0.38f;
            for (var tap = 0; tap < 3; tap++)
            {
                var elapsed = 0f;
                while (elapsed < pulseDuration)
                {
                    elapsed += Time.deltaTime;
                    var phase = Mathf.Clamp01(elapsed / pulseDuration);
                    var glow = Mathf.Sin(phase * Mathf.PI);
                    var color = Color.Lerp(baseColor, new Color(0.40f, 0.90f, 1f, 1f), glow * 0.72f);
                    if (signalMaterial.HasProperty("_BaseColor")) signalMaterial.SetColor("_BaseColor", color);
                    else signalMaterial.color = color;
                    if (signalMaterial.HasProperty("_EmissionColor"))
                    {
                        var emission = new Color(0.18f, 0.72f, 0.95f, 1f) * 1.35f;
                        signalMaterial.SetColor("_EmissionColor", Color.Lerp(baseEmission, emission, glow));
                    }

                    beacon.localScale = restScale * (1f + glow * 0.035f);
                    UpdateSignalPortrait(glow);
                    yield return null;
                }

                if (tap < 2)
                {
                    yield return new WaitForSeconds(0.24f);
                }
            }

            RestoreDistantBeaconSignal();
        }

        private void RestoreDistantBeaconSignal()
        {
            if (_signalBeaconRenderer != null && _signalBeaconRenderer.sharedMaterial == _signalMaterial)
            {
                _signalBeaconRenderer.sharedMaterial = _signalOriginalMaterial;
            }

            if (_signalBeacon != null)
            {
                _signalBeacon.localScale = _signalRestScale;
            }

            if (_signalMaterial != null)
            {
                Destroy(_signalMaterial);
            }

            if (_signalPortrait != null)
            {
                Destroy(_signalPortrait);
            }

            _signalBeaconRenderer = null;
            _signalBeacon = null;
            _signalOriginalMaterial = null;
            _signalMaterial = null;
            _signalRestScale = Vector3.one;
            _signalPortrait = null;
            _signalPortraitRenderer = null;
            _signalPortraitRestScale = Vector3.one;
        }

        private void CreateSignalPortrait(Vector3 position)
        {
            var portrait = Resources.Load<Sprite>("Characters/VesperSignalPortrait");
            if (portrait == null)
            {
                return;
            }

            _signalPortrait = new GameObject("ART_VesperOpeningSignalHologram");
            _signalPortrait.transform.position = position;
            _signalPortrait.transform.localScale = Vector3.one * 0.32f;
            _signalPortraitRestScale = _signalPortrait.transform.localScale;
            _signalPortraitRenderer = _signalPortrait.AddComponent<SpriteRenderer>();
            _signalPortraitRenderer.sprite = portrait;
            _signalPortraitRenderer.color = new Color(0.66f, 0.88f, 1f, 0.62f);
            _signalPortraitRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _signalPortraitRenderer.receiveShadows = false;
            _signalPortraitRenderer.sortingOrder = 2;
            FacePortraitToCamera();
        }

        private void UpdateSignalPortrait(float glow)
        {
            if (_signalPortrait == null || _signalPortraitRenderer == null)
            {
                return;
            }

            _signalPortrait.transform.localScale = _signalPortraitRestScale * (1f + glow * 0.06f);
            _signalPortraitRenderer.color = new Color(0.66f + glow * 0.12f,
                0.88f + glow * 0.08f, 1f, 0.32f + glow * 0.55f);
            FacePortraitToCamera();
        }

        private void FacePortraitToCamera()
        {
            var camera = Camera.main;
            if (_signalPortrait == null || camera == null)
            {
                return;
            }

            var facing = camera.transform.position - _signalPortrait.transform.position;
            if (facing.sqrMagnitude > 0.001f)
            {
                _signalPortrait.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            }
        }
    }
}
