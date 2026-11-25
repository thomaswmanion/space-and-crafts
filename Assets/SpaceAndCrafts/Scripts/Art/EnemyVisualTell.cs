using SpaceAndCrafts.AI;
using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Adds restrained visual motion without changing an enemy's AI root or collision motion.
    /// Wired onto existing AI roots in Arrival and numbered worlds — no Meshy regen.
    /// </summary>
    public sealed class EnemyVisualTell : MonoBehaviour
    {
        [SerializeField] private float hopMeters = 0.22f;
        [SerializeField] private float hopHz = 2.4f;
        [SerializeField] private float leanDeg = 14f;
        [SerializeField] private Color warnColor = new(1f, 0.35f, 0.15f, 0.85f);

        private Quaternion _restRot;
        private Vector3 _restScale;
        private Vector3 _motionRootRestPosition;
        private Transform _motionRoot;
        private float _phase;
        private Transform _halo;
        private Light _glow;
        private Transform _player;
        private readonly Transform[] _eyes = new Transform[2];
        private readonly Transform[] _pupils = new Transform[2];
        private readonly Vector3[] _eyeRestScales = new Vector3[2];
        private readonly Vector3[] _pupilRestPositions = new Vector3[2];
        private Vector2 _gazeOffset;
        private Vector2 _gazeVelocity;
        private Vector2 _idleGlance;
        private float _nextGlanceAt;
        private float _glanceUntil;
        private float _nextBlinkAt;
        private float _blinkStartedAt;
        private bool _blinkActive;

        public Vector3 MotionRootLocalEulerForDiagnostics =>
            _motionRoot != null ? _motionRoot.localEulerAngles : Vector3.zero;
        public Vector3 MotionRootLocalPositionForDiagnostics =>
            _motionRoot != null ? _motionRoot.localPosition : Vector3.zero;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Wire(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode _)
        {
            Wire(scene);
        }

        private static void Wire(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.name))
            {
                return;
            }

            var isArrival = scene.name.Contains("Arrival");
            var isNumberedWorld = scene.name.StartsWith("World", System.StringComparison.OrdinalIgnoreCase);
            if (!isArrival && !isNumberedWorld)
            {
                return;
            }

            // Each archetype already animates its own legs, body, charge, or attack.
            // Secondary shell motion runs on a visual child so it cannot rotate a
            // collision root or lift grounded feet.
            AttachAll<ScrapMite>(0f, 1.15f, 0f);
            AttachAll<ScrapRoller>(0f, 0.6f, 0f);
            AttachAll<ScrapFloater>(0f, 0.45f, 2f);
            // The Spitter already hovers at its AI root and uses a separate
            // attack recoil. Give its visible shell a slow, restrained idle
            // sway so the neutral state reads as alive without adding another
            // vertical bob or distorting the blob silhouette.
            AttachAll<ScrapSpitter>(0f, 0.5f, 6f);
            AttachAll<ScrapStormSentinel>(0f, 0.45f, 0f);
            // These two appearance cleanups were authored specifically for Arrival's graybox set dressing.
            if (isArrival)
            {
                HideFigurinePads();
            }
        }

        /// <summary>Yellow museum discs under Scrap foes read as figurine bases — kill them.</summary>
        private static void HideFigurinePads()
        {
            var foePts = new System.Collections.Generic.List<Vector3>(32);
            CollectFoePoints<ScrapMite>(foePts);
            CollectFoePoints<ScrapRoller>(foePts);
            CollectFoePoints<ScrapFloater>(foePts);
            CollectFoePoints<ScrapSpitter>(foePts);
            CollectFoePoints<ScrapStormSentinel>(foePts);
            if (foePts.Count == 0)
            {
                return;
            }

            foreach (var rend in Object.FindObjectsByType<MeshRenderer>())
            {
                if (rend == null || !rend.enabled)
                {
                    continue;
                }

                var go = rend.gameObject;
                var n = go.name;
                if (n.Contains("WarnHalo") || n.Contains("CraftLip") || n.Contains("Runway")
                    || n.Contains("Beacon") || n.Contains("Crumb") || n.Contains("Splat")
                    || n.Contains("Floor") || n.Contains("Ground") || n.Contains("Path"))
                {
                    continue;
                }

                var size = rend.bounds.size;
                var flat = size.y < 0.55f && size.x > 0.6f && size.z > 0.6f && size.x < 6f && size.z < 6f;
                if (!flat)
                {
                    continue;
                }

                var mat = rend.sharedMaterial;
                var yellow = false;
                if (mat != null)
                {
                    var c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                    yellow = c.r > 0.7f && c.g > 0.45f && c.b < 0.55f && (c.r + c.g) > c.b * 2.2f;
                }

                var namedPad = n.IndexOf("Pedestal", System.StringComparison.OrdinalIgnoreCase) >= 0
                               || n.IndexOf("Pad", System.StringComparison.OrdinalIgnoreCase) >= 0
                               || n.IndexOf("Disc", System.StringComparison.OrdinalIgnoreCase) >= 0
                               || n.IndexOf("Plinth", System.StringComparison.OrdinalIgnoreCase) >= 0
                               || n.IndexOf("Cylinder", System.StringComparison.OrdinalIgnoreCase) >= 0
                               || n.IndexOf("Platform", System.StringComparison.OrdinalIgnoreCase) >= 0;

                var roundish = Mathf.Abs(size.x - size.z) < size.x * 0.35f && size.x < 3.6f;
                // Round yellow/named pads, or small round discs under foes = figurine bases.
                if (!yellow && !namedPad && !roundish)
                {
                    continue;
                }

                var center = rend.bounds.center;
                for (var i = 0; i < foePts.Count; i++)
                {
                    var d = foePts[i] - center;
                    d.y = 0f;
                    if (d.sqrMagnitude < 3.4f * 3.4f && Mathf.Abs(foePts[i].y - center.y) < 2.5f)
                    {
                        go.SetActive(false);
                        break;
                    }
                }
            }
        }

        private static void CollectFoePoints<T>(System.Collections.Generic.List<Vector3> pts) where T : Component
        {
            foreach (var c in Object.FindObjectsByType<T>())
            {
                if (c != null && !IsUnderShowcase(c.transform))
                {
                    pts.Add(c.transform.position);
                }
            }
        }

        private static bool IsUnderShowcase(Transform t)
        {
            while (t != null)
            {
                var n = t.name;
                if (n.Contains("Showcase") || n.Contains("MeshyCritical"))
                {
                    return true;
                }

                t = t.parent;
            }

            return false;
        }

        private static void AttachAll<T>(float hop, float hz, float lean) where T : Component
        {
            foreach (var c in Object.FindObjectsByType<T>())
            {
                if (c == null || c.GetComponent<EnemyVisualTell>() != null)
                {
                    continue;
                }

                if (IsUnderShowcase(c.transform))
                {
                    continue;
                }

                var tell = c.gameObject.AddComponent<EnemyVisualTell>();
                tell.hopMeters = hop;
                tell.hopHz = hz;
                tell.leanDeg = lean;
            }
        }

        private void Awake()
        {
            _motionRoot = CreateMotionRoot();
            _restRot = _motionRoot.localRotation;
            _restScale = _motionRoot.localScale;
            _motionRootRestPosition = _motionRoot.localPosition;
            _phase = Random.Range(0f, Mathf.PI * 2f);
            _nextBlinkAt = Time.time + Random.Range(1.8f, 3.5f);
            _nextGlanceAt = Time.time + Random.Range(0.6f, 1.6f);
            BuildHalo();
            if (gameObject.scene.name.Contains("Arrival"))
            {
                BuildFaceTell();
            }
        }

        private Transform CreateMotionRoot()
        {
            var existing = transform.Find("ART_EnemyMotionRoot");
            if (existing != null)
            {
                return existing;
            }

            var motion = new GameObject("ART_EnemyMotionRoot").transform;
            motion.SetParent(transform, false);
            motion.localPosition = Vector3.zero;
            motion.localRotation = Quaternion.identity;
            motion.localScale = Vector3.one;

            var visualChildren = new Transform[transform.childCount - 1];
            var visualCount = 0;
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child == motion
                    || child.GetComponentInChildren<Renderer>(true) == null
                    || child.GetComponentInChildren<Collider>(true) != null
                    || child.GetComponentInChildren<Rigidbody>(true) != null)
                {
                    continue;
                }

                visualChildren[visualCount++] = child;
            }

            if (GetComponent<ScrapRoller>() != null && visualCount > 0)
            {
                // The roller's attack rotates its rendered shell. Put that pivot
                // at the existing model center while SetParent(..., true) below
                // preserves the authored pose; spinning around the AI root made
                // the legs and shell arc through the floor.
                var bounds = new Bounds(visualChildren[0].GetComponentInChildren<Renderer>(true).bounds.center,
                    Vector3.zero);
                for (var i = 0; i < visualCount; i++)
                {
                    var renderers = visualChildren[i].GetComponentsInChildren<Renderer>(true);
                    for (var j = 0; j < renderers.Length; j++)
                    {
                        if (renderers[j] != null && renderers[j].enabled)
                        {
                            bounds.Encapsulate(renderers[j].bounds);
                        }
                    }
                }

                motion.localPosition = transform.InverseTransformPoint(bounds.center);
            }

            for (var i = 0; i < visualCount; i++)
            {
                visualChildren[i].SetParent(motion, true);
            }

            return motion;
        }

        /// <summary>Meshy shells often hide TEMP eyes — stamp readable cartoon eyes so foes aren't statues.</summary>
        private void BuildFaceTell()
        {
            if (FindDescendant("ART_EnemyFaceTell") != null)
            {
                return;
            }

            // These archetypes already have authored focal eyes: the Roller has
            // a concentric lens, the Floater a central optic, and the Mite's
            // organic builder supplies tracked, blinking eyes and pupils. A
            // rigged Sentinel candidate also brings its own face; stamping the
            // shared root-level pair onto it leaves detached eyes below the head.
            // Duplicating those with detached white spheres makes them read as
            // uncanny layered faces, so keep their existing faces intact.
            var sentinel = GetComponent<ScrapStormSentinel>();
            var sentinelAnimator = sentinel != null
                ? GetComponentInChildren<Animator>(true) : null;
            var hasRiggedSentinelFace = sentinelAnimator != null
                && sentinelAnimator.runtimeAnimatorController != null;
            if (GetComponent<ScrapRoller>() != null || GetComponent<ScrapFloater>() != null
                || GetComponent<ScrapMite>() != null || hasRiggedSentinelFace)
            {
                return;
            }

            var face = new GameObject("ART_EnemyFaceTell");
            face.transform.SetParent(_motionRoot != null ? _motionRoot : transform, false);
            // The ScrapSpitter's blob is under 1.1 m tall. Its former shared
            // eye anchor sat above the body and made both eyes appear detached.
            var isSpitter = GetComponent<ScrapSpitter>() != null;
            face.transform.localPosition = isSpitter
                ? new Vector3(0f, 0.28f, 0.43f)
                : new Vector3(0f, 0.85f, 0.55f) - _motionRootRestPosition;

            if (isSpitter)
            {
                // Use shallow oval eye patches embedded into the Spitter shell.
                // The earlier round eyes projected as separate googly spheres,
                // and their low placement merged them with the mouth cue in the
                // player-camera release capture. Keep the same gaze animation,
                // but raise the pair and reduce their depth so they follow the
                // body's surface rather than sitting proud of it.
                _eyes[0] = MakeEye(face.transform, new Vector3(-0.14f, 0f, 0f), out _pupils[0], 0.18f, 0.32f);
                _eyes[1] = MakeEye(face.transform, new Vector3(0.14f, 0f, 0f), out _pupils[1], 0.18f, 0.32f);
                _eyes[0].localScale = new Vector3(0.18f, 0.20f, 0.06f);
                _eyes[1].localScale = new Vector3(0.18f, 0.20f, 0.06f);
            }
            else
            {
                _eyes[0] = MakeEye(face.transform, new Vector3(-0.22f, 0.05f, 0f), out _pupils[0]);
                _eyes[1] = MakeEye(face.transform, new Vector3(0.22f, 0.05f, 0f), out _pupils[1]);
            }

            for (var i = 0; i < _eyes.Length; i++)
            {
                _eyeRestScales[i] = _eyes[i].localScale;
                _pupilRestPositions[i] = _pupils[i].localPosition;
            }
        }

        private Transform FindDescendant(string childName)
        {
            foreach (var child in transform.GetComponentsInChildren<Transform>(true))
            {
                if (child != transform && child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform MakeEye(Transform parent, Vector3 localPos, out Transform pupil,
            float eyeScale = 0.28f, float pupilForward = 0.35f)
        {
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "ART_EnemyEye";
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPos;
            eye.transform.localScale = Vector3.one * eyeScale;
            Object.Destroy(eye.GetComponent<Collider>());
            TintLit(eye, Color.white, 0.8f);

            pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            pupil.name = "ART_EnemyPupil";
            pupil.SetParent(eye.transform, false);
            pupil.localPosition = new Vector3(0f, 0f, pupilForward);
            pupil.localScale = Vector3.one * 0.45f;
            Object.Destroy(pupil.GetComponent<Collider>());
            TintLit(pupil.gameObject, new Color(0.05f, 0.02f, 0.08f), 0.05f);
            return eye.transform;
        }

        private static void TintLit(GameObject go, Color color, float emission)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                return;
            }

            var mat = new Material(shader) { name = go.name + "_Mat" };
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emission);
            }

            rend.sharedMaterial = mat;
        }

        private void BuildHalo()
        {
            // A horizontal warning ring at the Spitter's root height bisects
            // its hovering body. Its face and wind-up already provide a local
            // attack tell, so reserve the ring for ground-level archetypes.
            if (GetComponent<ScrapSpitter>() == null)
            {
            // Thin threat ring — not a solid yellow figurine disc.
            var halo = new GameObject("ART_EnemyWarnHalo");
            halo.transform.SetParent(transform, false);
            halo.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            var filter = halo.AddComponent<MeshFilter>();
            filter.sharedMesh = OrganicMeshFactory.Torus(0.55f, 0.035f, 28, 10);
            var rend = halo.AddComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader) { name = "Mat_EnemyHalo" };
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", warnColor);
                }

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    // Keep the warning halo readable without washing out the
                    // enemy's small articulated feet during close combat.
                    mat.SetColor("_EmissionColor", warnColor * 0.28f);
                }

                rend.sharedMaterial = mat;
            }

            _halo = halo.transform;
            _halo.localScale = Vector3.one;
            // Idle foes: no disc/ring underfoot (figurine read). Threat ring only when near.
            _halo.gameObject.SetActive(false);
            }

            var lightGo = new GameObject("LIT_EnemyTell");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = Vector3.up * 1.1f;
            _glow = lightGo.AddComponent<Light>();
            _glow.type = LightType.Point;
            _glow.color = warnColor;
            _glow.intensity = 0.35f;
            _glow.range = 3.5f;
        }

        private void Update()
        {
            var t = Time.time * hopHz * Mathf.PI * 2f + _phase;
            // A single smooth hop per cycle; Abs(Sin) doubled the bounce rate and read as jelly. 
            var hop = Mathf.Max(0f, Mathf.Sin(t)) * hopMeters;
            var lean = Mathf.Sin(t * 0.7f) * leanDeg;
            if (_motionRoot != null)
            {
                var spitter = GetComponent<ScrapSpitter>();
                var windup = spitter != null ? spitter.WindupWeight : 0f;
                var recoil = spitter != null ? spitter.RecoilWeight : 0f;
                var roller = GetComponent<ScrapRoller>();
                var rollerWindup = roller != null ? roller.WindupProgressForDiagnostics : 0f;
                var rollerRecoil = roller != null ? roller.ChargeRecoilForDiagnostics : 0f;
                var rollerRoll = roller != null ? roller.ChargeRollAngleForVisuals : 0f;
                // Recoil the rendered shell away from the player, then snap it
                // forward after firing. Keep this on the visual child so the
                // trigger volume and enemy navigation root stay stable.
                // The Spitter now keeps its AI/collision root upright, so let
                // its visible shell carry a slightly clearer but still compact
                // anticipation/recoil. Keep Roller tuning on its own envelope.
                var spitterWindup = spitter != null ? windup : 0f;
                var spitterRecoil = spitter != null ? recoil : 0f;
                // The earlier player-camera release capture showed the face sliding
                // toward the underside as the shell pitched back. Keep the clearer
                // translation cue, but restrain Spitter pitch so its eyes and mouth
                // remain readable through the charge/release. Roller tuning remains
                // independent; all scale and AI/collision transforms stay fixed.
                var attackOffset = Vector3.forward * (-0.16f * spitterWindup + 0.24f * spitterRecoil
                                                       - 0.18f * rollerWindup + 0.12f * rollerRecoil)
                    + Vector3.up * (0.02f * spitterWindup + 0.05f * spitterRecoil
                                    - 0.045f * rollerWindup + 0.035f * rollerRecoil);
                var attackTilt = Quaternion.Euler(-8f * spitterWindup + 11f * spitterRecoil
                                                   - 12f * rollerWindup + 8f * rollerRecoil,
                    0f, 0f);
                _motionRoot.localPosition = _motionRootRestPosition + Vector3.up * hop + attackOffset;
                var chargeRoll = Quaternion.AngleAxis(rollerRoll, Vector3.right);
                _motionRoot.localRotation = _restRot * Quaternion.Euler(0f, lean * 0.35f, lean)
                    * attackTilt * chargeRoll;
                _motionRoot.localScale = _restScale;
            }

            var hot = NearPlayer(5.5f);
            if (_halo != null)
            {
                // ScrapMite keeps its AI root slightly above the walkable surface
                // so the curled legs can reach down to it. Anchor the warning
                // ring to that sampled surface instead of letting it cut through
                // the lower body and obscure the stance animation.
                var mite = GetComponent<ScrapMite>();
                if (mite != null)
                {
                    var ringPosition = _halo.localPosition;
                    ringPosition.y = mite.GroundYForVisuals - transform.position.y + 0.012f;
                    _halo.localPosition = ringPosition;
                }

                _halo.gameObject.SetActive(hot);
                if (hot)
                {
                    var pulse = 1f + Mathf.Sin(Time.time * 3.2f + _phase) * 0.018f;
                    _halo.localScale = Vector3.one * pulse;
                    _halo.Rotate(0f, 28f * Time.deltaTime, 0f, Space.Self);
                }
            }

            if (_glow != null)
            {
                _glow.intensity = hot ? 0.55f : 0.3f + 0.04f * Mathf.Sin(Time.time * 0.55f + _phase);
            }

            AnimateFaceTell();
        }

        private void AnimateFaceTell()
        {
            if (_eyes[0] == null || _eyes[1] == null || _pupils[0] == null || _pupils[1] == null)
            {
                return;
            }

            if (_player == null)
            {
                var splat = GameObject.Find("Splat_TEMP");
                if (splat != null) _player = splat.transform;
            }

            var target = Vector2.zero;
            if (_player != null && _motionRoot != null
                && (_player.position - transform.position).sqrMagnitude < 10f * 10f)
            {
                var toward = _motionRoot.InverseTransformDirection(_player.position - _motionRoot.position);
                target = new Vector2(Mathf.Clamp(toward.x * 0.04f, -0.035f, 0.035f),
                    Mathf.Clamp(toward.y * 0.03f, -0.025f, 0.025f));
            }
            else
            {
                if (Time.time >= _nextGlanceAt)
                {
                    _idleGlance = new Vector2(Random.Range(-0.03f, 0.03f), Random.Range(-0.018f, 0.018f));
                    _glanceUntil = Time.time + Random.Range(0.3f, 0.55f);
                    _nextGlanceAt = _glanceUntil + Random.Range(1.8f, 3.1f);
                }

                if (Time.time < _glanceUntil) target = _idleGlance;
            }

            _gazeOffset = Vector2.SmoothDamp(_gazeOffset, target, ref _gazeVelocity, 0.1f);
            if (!_blinkActive && Time.time >= _nextBlinkAt)
            {
                _blinkActive = true;
                _blinkStartedAt = Time.time;
            }

            const float closeSeconds = 0.07f;
            const float reopenSeconds = 0.12f;
            var elapsed = _blinkActive ? Time.time - _blinkStartedAt : 0f;
            var blink = _blinkActive
                ? Mathf.Clamp01(elapsed < closeSeconds ? elapsed / closeSeconds
                    : 1f - (elapsed - closeSeconds) / reopenSeconds)
                : 0f;
            // Give the Scrap Spitter a face-level acting cue while it charges:
            // narrow the eyes into a focused squint, then let the existing recoil
            // envelope close them briefly on the shot. This keeps the body shape
            // stable and makes anticipation readable even when its spherical form
            // has little silhouette change.
            var spitter = GetComponent<ScrapSpitter>();
            var spitterFocus = spitter != null ? spitter.WindupWeight : 0f;
            var spitterRecoilBlink = spitter != null ? spitter.RecoilWeight * 0.78f : 0f;
            blink = Mathf.Max(blink, spitterRecoilBlink);
            blink = Mathf.SmoothStep(0f, 1f, blink);
            for (var i = 0; i < _eyes.Length; i++)
            {
                var eyeScale = _eyeRestScales[i];
                // Make the full charge read as a deliberate squint at gameplay
                // distance; keep the body scale and eye width unchanged.
                var focusedHeight = Mathf.Lerp(1f, 0.42f, spitterFocus);
                eyeScale.y *= focusedHeight * Mathf.Lerp(1f, 0.12f, blink);
                _eyes[i].localScale = eyeScale;
                _pupils[i].localPosition = _pupilRestPositions[i]
                    + new Vector3(_gazeOffset.x, _gazeOffset.y, 0f);
            }

            if (_blinkActive && elapsed >= closeSeconds + reopenSeconds)
            {
                _blinkActive = false;
                _nextBlinkAt = Time.time + Random.Range(2.8f, 5.2f);
            }
        }

        private bool NearPlayer(float radius)
        {
            if (_player == null)
            {
                var splat = GameObject.Find("Splat_TEMP");
                if (splat != null)
                {
                    _player = splat.transform;
                }
            }

            if (_player == null)
            {
                return false;
            }

            return (_player.position - transform.position).sqrMagnitude < radius * radius;
        }
    }
}
