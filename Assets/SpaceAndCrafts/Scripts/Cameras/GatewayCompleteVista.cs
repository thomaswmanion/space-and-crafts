using System.Collections;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.Player;
using UnityEngine;

namespace SpaceAndCrafts.Cameras
{
    /// <summary>Briefly stages Splat and the revealed gateways together after the boss.</summary>
    public sealed class GatewayCompleteVista : MonoBehaviour
    {
        [SerializeField] private PlatformingCameraRig cameraRig;
        [SerializeField] private Transform gatewayFocus;
        [SerializeField] private float holdSeconds = 5.8f;
        [SerializeField] private float transitionSeconds = 0.9f;
        [SerializeField] private float sideOffset = 4f;
        [SerializeField] private float backOffset = 5.5f;
        [SerializeField] private float heightOffset = 4f;
        [SerializeField, Range(0.3f, 0.7f)] private float targetBetweenPlayerAndGate = 0.42f;
        [SerializeField] private float vistaFieldOfView = 62f;

        private Camera _shotCamera;
        private Transform _cameraParent;
        private Vector3 _cameraLocalPosition;
        private Quaternion _cameraLocalRotation;
        private Vector3 _cameraLocalScale;
        private float _cameraFieldOfView;
        private bool _cameraRigWasEnabled;
        private bool _cameraRigStateSaved;
        private SplatMotor _splatMotor;
        private bool _motorWasEnabled;
        private bool _motorStateSaved;

        private void OnEnable()
        {
            GameEvents.LevelCompleted += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.LevelCompleted -= HandleLevelCompleted;
            StopAllCoroutines();
            RestoreShot();
        }

        public void Bind(PlatformingCameraRig rig, Transform focus)
        {
            cameraRig = rig;
            gatewayFocus = focus;
        }

        private void HandleLevelCompleted()
        {
            if (_shotCamera != null)
            {
                return;
            }

            var playerObject = GameObject.FindGameObjectWithTag("Player");
            var camera = Camera.main;
            if (playerObject == null || camera == null)
            {
                return;
            }

            var player = playerObject.transform;
            var gatewayBounds = GetGatewayBounds();
            var towardGate = gatewayBounds.center - player.position;
            towardGate.y = 0f;
            if (towardGate.sqrMagnitude < 0.01f)
            {
                towardGate = player.forward;
                towardGate.y = 0f;
            }

            var forward = towardGate.normalized;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var shotPosition = player.position + right * sideOffset
                - forward * backOffset + Vector3.up * heightOffset;
            var playerFocus = player.position + Vector3.up * 1.15f;
            var shotTarget = Vector3.Lerp(playerFocus, gatewayBounds.center, targetBetweenPlayerAndGate);
            var shotRotation = Quaternion.LookRotation(shotTarget - shotPosition, Vector3.up);

            _shotCamera = camera;
            _cameraParent = camera.transform.parent;
            _cameraLocalPosition = camera.transform.localPosition;
            _cameraLocalRotation = camera.transform.localRotation;
            _cameraLocalScale = camera.transform.localScale;
            _cameraFieldOfView = camera.fieldOfView;
            if (cameraRig != null)
            {
                _cameraRigWasEnabled = cameraRig.enabled;
                _cameraRigStateSaved = true;
                cameraRig.enabled = false;
            }

            _splatMotor = playerObject.GetComponent<SplatMotor>();
            if (_splatMotor != null)
            {
                _motorWasEnabled = _splatMotor.enabled;
                _motorStateSaved = true;
                _splatMotor.enabled = false;
            }

            camera.transform.SetParent(null, true);
            Debug.Log($"[Space & Crafts] Gateway victory shot: player={player.position:F2}, gatewayBoundsCenter={gatewayBounds.center:F2}, gatewayBoundsSize={gatewayBounds.size:F2}, cameraFrom={camera.transform.position:F2}, cameraTo={shotPosition:F2}, target={shotTarget:F2}.");
            StartCoroutine(PlayShot(shotPosition, shotRotation, shotTarget));
        }

        private Bounds GetGatewayBounds()
        {
            var presenter = FindAnyObjectByType<GatewayRevealPresenter>();
            if (presenter != null && presenter.TryGetGatewayBounds(out var bounds))
            {
                return bounds;
            }

            // ArrivalMeshyPathDress can instantiate these portals before or
            // without a serialized reveal presenter. Resolve the live landmarks
            // by their stable runtime names so the shot still frames real art.
            var found = false;
            var liveBounds = default(Bounds);
            for (var i = 0; i < 3; i++)
            {
                var gateway = GameObject.Find($"ART_DistantMeshyGateway_{i}");
                if (gateway == null)
                {
                    continue;
                }

                var renderers = gateway.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    if (!found)
                    {
                        liveBounds = renderer.bounds;
                        found = true;
                    }
                    else
                    {
                        liveBounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            if (found)
            {
                Debug.Log($"[Space & Crafts] Victory shot resolved live gateway bounds: center={liveBounds.center:F2}, size={liveBounds.size:F2}.");
                return liveBounds;
            }

            return new Bounds(gatewayFocus != null ? gatewayFocus.position : Vector3.zero, Vector3.one * 4f);
        }

        private IEnumerator PlayShot(Vector3 targetPosition, Quaternion targetRotation, Vector3 lookTarget)
        {
            var camera = _shotCamera;
            if (camera == null)
            {
                RestoreShot();
                yield break;
            }

            var startPosition = camera.transform.position;
            var startRotation = camera.transform.rotation;
            var startFov = camera.fieldOfView;
            var elapsed = 0f;
            var duration = Mathf.Max(0.1f, transitionSeconds);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                camera.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, t),
                    Quaternion.Slerp(startRotation, targetRotation, t));
                camera.fieldOfView = Mathf.Lerp(startFov, vistaFieldOfView, t);
                yield return null;
            }

            camera.transform.SetPositionAndRotation(targetPosition, targetRotation);
            camera.fieldOfView = vistaFieldOfView;
            var player = GameObject.FindGameObjectWithTag("Player");
            var playerViewport = player != null
                ? camera.WorldToViewportPoint(player.transform.position + Vector3.up * 1.15f)
                : Vector3.zero;
            var targetViewport = camera.WorldToViewportPoint(lookTarget);
            var gatewayViewport = camera.WorldToViewportPoint(GetGatewayBounds().center);
            Debug.Log($"[Space & Crafts] Gateway victory shot settled: camera={camera.transform.position:F2}, rotation={camera.transform.eulerAngles:F1}, fov={camera.fieldOfView:F1}, playerViewport={playerViewport:F2}, targetViewport={targetViewport:F2}, gatewayViewport={gatewayViewport:F2}.");
            for (var i = 0; i < 3; i++)
            {
                var gateway = GameObject.Find($"ART_DistantMeshyGateway_{i}");
                if (gateway == null)
                {
                    Debug.LogWarning($"[Space & Crafts] Victory gateway {i} root is missing at shot settle.");
                    continue;
                }

                var lod = gateway.GetComponentInChildren<LODGroup>(true);
                foreach (var renderer in gateway.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer != null)
                    {
                        var visibleByCamera = (camera.cullingMask & (1 << renderer.gameObject.layer)) != 0;
                        Debug.Log($"[Space & Crafts] Victory gateway renderer: index={i}, name={renderer.name}, active={renderer.gameObject.activeInHierarchy}, enabled={renderer.enabled}, forceOff={renderer.forceRenderingOff}, visible={renderer.isVisible}, layer={renderer.gameObject.layer}, cameraLayerVisible={visibleByCamera}, bounds={renderer.bounds.center:F2}/{renderer.bounds.size:F2}, lodGroup={(lod != null ? lod.enabled.ToString() : "none")}, lodSize={(lod != null ? lod.size : 0f):F2}, materials={renderer.sharedMaterials.Length}.");
                    }
                }
            }

            yield return new WaitForSeconds(Mathf.Max(0f, holdSeconds - duration));
            RestoreShot();
        }

        private void RestoreShot()
        {
            if (_shotCamera != null)
            {
                if (_cameraParent != null)
                {
                    _shotCamera.transform.SetParent(_cameraParent, false);
                    _shotCamera.transform.localPosition = _cameraLocalPosition;
                    _shotCamera.transform.localRotation = _cameraLocalRotation;
                    _shotCamera.transform.localScale = _cameraLocalScale;
                }

                _shotCamera.fieldOfView = _cameraFieldOfView;
            }

            if (cameraRig != null && _cameraRigStateSaved)
            {
                cameraRig.enabled = _cameraRigWasEnabled;
            }

            if (_splatMotor != null && _motorStateSaved)
            {
                _splatMotor.enabled = _motorWasEnabled;
            }

            _shotCamera = null;
            _cameraParent = null;
            _cameraRigStateSaved = false;
            _motorStateSaved = false;
            _splatMotor = null;
        }
    }
}
