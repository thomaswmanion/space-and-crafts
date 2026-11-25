using System.Collections;
using SpaceAndCrafts.Audio;
using UnityEngine;

namespace SpaceAndCrafts.Crafting
{
    /// <summary>
    /// World 2 invention: Pingline Spooler. The legacy class and invention ID are retained
    /// so serialized World 2 scene references and save data keep working during the retheme.
    /// </summary>
    public sealed class VineSpooler : MonoBehaviour
    {
        [SerializeField] private float maxRange = 18f;
        [SerializeField] private float pullSpeed = 22f;
        [SerializeField] private LayerMask grappleMask = ~0;
        [SerializeField] private string inventionId = "vine_spooler";

        private bool _latched;
        private Vector3 _latchPoint;
        private Transform _owner;
        private LineRenderer _pingRing;
        private Material _pingMaterial;
        private Coroutine _pingRoutine;
        private readonly Vector3[] _ringPoints = new Vector3[33];

        public bool IsLatched => _latched;
        public string InventionId => inventionId;

        public void AttachOwner(Transform owner) => _owner = owner;

        public bool TryGrapple(Vector3 origin, Vector3 direction)
        {
            if (_owner == null)
            {
                return false;
            }

            if (!Physics.Raycast(origin, direction.normalized, out var hit, maxRange, grappleMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            LatchTo(hit.point);
            return true;
        }

        /// <summary>Direct latch used by teach pads / scripted yanks (no raycast).</summary>
        public void LatchTo(Vector3 worldPoint)
        {
            if (_owner == null)
            {
                return;
            }

            _latchPoint = worldPoint;
            _latched = true;
            TempSfxPlayer.PlayPingline();
            if (_pingRoutine != null) StopCoroutine(_pingRoutine);
            _pingRoutine = StartCoroutine(ShowAnchorPing());
        }

        public void Release()
        {
            _latched = false;
        }

        private IEnumerator ShowAnchorPing()
        {
            EnsurePingRing();
            if (_pingRing == null) yield break;

            var cameraTransform = Camera.main != null ? Camera.main.transform : null;
            _pingRing.transform.SetPositionAndRotation(
                _latchPoint + Vector3.up * 0.08f,
                cameraTransform != null
                    ? Quaternion.LookRotation(-cameraTransform.forward, cameraTransform.up)
                    : Quaternion.LookRotation(Vector3.forward, Vector3.up));
            _pingRing.enabled = true;

            for (var pulse = 0; pulse < 2; pulse++)
            {
                const float duration = 0.24f;
                var elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    var radius = Mathf.Lerp(0.14f, 0.92f, Mathf.SmoothStep(0f, 1f, t));
                    var color = new Color(0.48f, 0.92f, 1f, (1f - t) * 0.9f);
                    _pingRing.startColor = color;
                    _pingRing.endColor = color;
                    for (var i = 0; i < _ringPoints.Length; i++)
                    {
                        var angle = i / 32f * Mathf.PI * 2f;
                        _ringPoints[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                    }
                    _pingRing.SetPositions(_ringPoints);
                    yield return null;
                }
                if (pulse == 0) yield return new WaitForSeconds(0.065f);
            }

            _pingRing.enabled = false;
            _pingRoutine = null;
        }

        private void EnsurePingRing()
        {
            if (_pingRing != null) return;
            var ringObject = new GameObject("VFX_PinglineAnchorPulse");
            ringObject.transform.SetParent(transform, true);
            _pingRing = ringObject.AddComponent<LineRenderer>();
            _pingRing.useWorldSpace = false;
            _pingRing.loop = true;
            _pingRing.positionCount = _ringPoints.Length;
            _pingRing.startWidth = 0.045f;
            _pingRing.endWidth = 0.012f;
            _pingRing.numCapVertices = 3;
            _pingRing.enabled = false;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _pingMaterial = new Material(shader) { name = "Mat_PinglinePulse_Runtime" };
                if (_pingMaterial.HasProperty("_Surface")) _pingMaterial.SetFloat("_Surface", 1f);
                if (_pingMaterial.HasProperty("_ZWrite")) _pingMaterial.SetFloat("_ZWrite", 0f);
                _pingMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _pingMaterial.renderQueue = 3000;
                if (_pingMaterial.HasProperty("_BaseColor")) _pingMaterial.SetColor("_BaseColor", new Color(0.48f, 0.92f, 1f, 0.9f));
                if (_pingMaterial.HasProperty("_Color")) _pingMaterial.SetColor("_Color", new Color(0.48f, 0.92f, 1f, 0.9f));
                _pingRing.sharedMaterial = _pingMaterial;
            }
        }

        private void OnDestroy()
        {
            if (_pingMaterial != null) Destroy(_pingMaterial);
        }

        private void FixedUpdate()
        {
            if (!_latched || _owner == null)
            {
                return;
            }

            var motor = _owner.GetComponent<Player.SplatMotor>();
            if (motor == null)
            {
                return;
            }

            var to = _latchPoint - _owner.position;
            if (to.magnitude < 0.6f)
            {
                Release();
                return;
            }

            motor.ApplyLaunch(to.normalized * pullSpeed * Time.fixedDeltaTime * 8f);
        }
    }
}
