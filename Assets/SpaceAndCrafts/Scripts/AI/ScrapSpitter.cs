using System.Collections;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.AI
{
    /// <summary>
    /// Fourth combat archetype: hovers, telegraphs, spits a scrap blob projectile.
    /// </summary>
    public sealed class ScrapSpitter : MonoBehaviour
    {
        [SerializeField] private float noticeRadius = 7f;
        [SerializeField] private float fireCooldown = 1.8f;
        [SerializeField] private float projectileSpeed = 9f;
        [SerializeField] private float hoverAmp = 0.08f;

        private const float WindupRiseDuration = 0.52f;
        private const float WindupDuration = 0.70f;
        private const float InterruptedWindupRetryDelay = 0.25f;
        private const float InterruptedWindupRecoveryDuration = 0.22f;

        private Transform _splat;
        private float _nextFire;
        private float _windupStartedAt = -1f;
        private bool _aggroReported;
        private bool _defeated;
        private Vector3 _origin;
        private Vector3 _restScale;
        private Transform _mouthGlow;
        private Renderer _mouthRenderer;
        private MaterialPropertyBlock _mouthProperties;
        private Color _mouthRestColor = Color.white;
        private Vector3 _mouthRestPosition;
        private Vector3 _mouthRestScale;
        private Quaternion _restRotation;
        // Negative sentinel: starting at zero gave every fresh Spitter a brief
        // false recoil pulse before it had fired its first shot.
        private float _lastFireTime = -1f;
        private float _interruptedWindupStartedAt = -1f;
        private float _interruptedWindupWeight;
        private float _idlePhaseOffset;
        private static readonly int MouthBaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int MouthColor = Shader.PropertyToID("_Color");
        private static Material _spitMaterial;
        private static Material _spitTrailMaterial;

        public float WindupWeight { get; private set; }
        public float RecoilWeight { get; private set; }
        public int FireCountForDiagnostics { get; private set; }
        public bool IsDefeatedForDiagnostics => _defeated;
        public float LastFireTimeForDiagnostics => _lastFireTime;
        public float IdlePhaseOffsetForDiagnostics => _idlePhaseOffset;

        public void Configure(Vector3 origin)
        {
            _origin = origin;
            transform.position = origin;
        }

        private void Awake()
        {
            _origin = transform.position;
            _restScale = transform.localScale;
            _restRotation = transform.rotation;
            _nextFire = Time.time + 0.6f;
            _windupStartedAt = -1f;
            _idlePhaseOffset = Random.Range(0f, Mathf.PI * 2f);
            _mouthGlow = FindVisualChild("ART_SpitterMouth");
            if (_mouthGlow != null)
            {
                // Keep the warm firing mark close to the body surface and below
                // the eyes. Its former large, forward-projecting bead overlapped
                // the primitive eye pair in gameplay captures, making the face
                // read as a cluster of knobs rather than one Spitter character.
                var authoredMouthPosition = _mouthGlow.localPosition;
                _mouthGlow.localPosition = new Vector3(authoredMouthPosition.x, 0.02f, 0.47f);
                _mouthGlow.localScale *= 0.65f;
                _mouthRestPosition = _mouthGlow.localPosition;
                _mouthRestScale = _mouthGlow.localScale;
                _mouthRenderer = _mouthGlow.GetComponent<Renderer>();
                if (_mouthRenderer != null && _mouthRenderer.sharedMaterial != null)
                {
                    _mouthRestColor = _mouthRenderer.sharedMaterial.HasProperty(MouthBaseColor)
                        ? _mouthRenderer.sharedMaterial.GetColor(MouthBaseColor)
                        : _mouthRenderer.sharedMaterial.HasProperty(MouthColor)
                            ? _mouthRenderer.sharedMaterial.GetColor(MouthColor)
                            : Color.white;
                    _mouthProperties = new MaterialPropertyBlock();
                }
            }
        }

        private void Update()
        {
            if (_defeated)
            {
                return;
            }

            EnsureSplat();
            var bob = Mathf.Sin(Time.time * 1.8f + _idlePhaseOffset) * hoverAmp;
            transform.position = _origin + Vector3.up * bob;

            var inNotice = _splat != null
                && (_splat.position - transform.position).sqrMagnitude < noticeRadius * noticeRadius;
            ReportAggro(inNotice);
            if (!inNotice)
            {
                // Leaving the notice radius interrupts the charge. A short retry
                // delay prevents an immediate restart if Splat brushes the edge.
                if (_windupStartedAt >= 0f)
                {
                    _interruptedWindupWeight = WindupWeight;
                    _interruptedWindupStartedAt = Time.time;
                    _windupStartedAt = -1f;
                    _nextFire = Mathf.Max(_nextFire, Time.time + InterruptedWindupRetryDelay);
                }
            }
            else if (_windupStartedAt < 0f && Time.time >= _nextFire)
            {
                _windupStartedAt = Time.time;
                _interruptedWindupStartedAt = -1f;
                _interruptedWindupWeight = 0f;
                TempSfxPlayer.PlayWarn();
            }

            // Reach the fully charged pose before the shot instead of
            // accelerating straight from the first tell into projectile
            // release. When the player escapes the notice radius, ease the
            // interrupted charge back to rest so the enemy does not snap out
            // of its anticipation pose in one frame.
            if (_windupStartedAt >= 0f)
            {
                WindupWeight = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01((Time.time - _windupStartedAt) / WindupRiseDuration));
            }
            else if (_interruptedWindupStartedAt >= 0f)
            {
                var recovery = Mathf.Clamp01(
                    (Time.time - _interruptedWindupStartedAt) / InterruptedWindupRecoveryDuration);
                WindupWeight = _interruptedWindupWeight * (1f - Mathf.SmoothStep(0f, 1f, recovery));
                if (recovery >= 1f)
                {
                    _interruptedWindupStartedAt = -1f;
                    _interruptedWindupWeight = 0f;
                    WindupWeight = 0f;
                }
            }
            else
            {
                WindupWeight = 0f;
            }
            // Keep the recoil clearly present after the shot, then ease it to rest
            // with zero velocity at the end. The linear envelope stopped abruptly
            // at 0.36 s, making the whole-body return pop on the first neutral frame.
            var recoilProgress = Mathf.Clamp01((Time.time - _lastFireTime) / 0.36f);
            RecoilWeight = 1f - Mathf.SmoothStep(0f, 1f, recoilProgress);

            // Let the whole form anticipate and recoil with the shot; keep its
            // silhouette and scale stable instead of stretching the blob.
            var facing = inNotice ? _splat.position - transform.position : _restRotation * Vector3.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.001f)
            {
                var targetRotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
                // Keep facing/collision upright. EnemyVisualTell applies the
                // idle lean and attack recoil to ART_EnemyMotionRoot; applying
                // the same body pitch and roll here doubled the pose and also
                // tilted the AI root during a shot.
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-5f * Time.deltaTime));
            }

            // Bob the AI root only. The visible child carries the anticipation
            // and recoil offsets so the collision volume does not lurch with it.
            transform.position = _origin + Vector3.up * bob;

            if (_mouthGlow != null)
            {
                // Keep the fused mouth bead on the same recovery envelope as
                // the body; a shorter local timer made the face snap back while
                // the Spitter was still recoiling from its shot.
                var recoil = RecoilWeight;
                var anticipation = WindupWeight;
                // Keep the charged source legible without letting the fused bead
                // cover the Spitter's eyes in the release pose. Return it to its
                // authored size as the recoil resolves.
                var cueScale = Mathf.Lerp(1f, 0.68f, anticipation);
                cueScale = Mathf.Lerp(cueScale, 0.82f, recoil);
                _mouthGlow.localScale = Vector3.Lerp(_mouthGlow.localScale, _mouthRestScale * cueScale,
                    1f - Mathf.Exp(-14f * Time.deltaTime));
                // The shell leans back as it charges. Push the fused mouth bead
                // forward enough to stay visible through that pose, then let it
                // settle back as the shell recoils after the shot.
                _mouthGlow.localPosition = Vector3.Lerp(_mouthGlow.localPosition,
                    _mouthRestPosition + Vector3.forward * (anticipation * 0.045f - recoil * 0.035f),
                    1f - Mathf.Exp(-16f * Time.deltaTime));
                if (_mouthRenderer != null && _mouthProperties != null)
                {
                    var chargedColor = Color.Lerp(_mouthRestColor, new Color(1f, 0.48f, 0.12f), anticipation * 0.8f);
                    // Keep the release flash in the projectile's warm craft-scrap
                    // palette. Pure white made the fused mouth bead read as a
                    // separate gray sphere instead of the hot source of the shot.
                    var firedColor = Color.Lerp(chargedColor, new Color(1f, 0.78f, 0.42f),
                        recoil * 0.85f);
                    _mouthRenderer.GetPropertyBlock(_mouthProperties);
                    _mouthProperties.SetColor(MouthBaseColor, firedColor);
                    _mouthProperties.SetColor(MouthColor, firedColor);
                    _mouthRenderer.SetPropertyBlock(_mouthProperties);
                }
            }

            if (inNotice && _splat != null && _windupStartedAt >= 0f
                && Time.time - _windupStartedAt >= WindupDuration)
                Fire();
        }

        private void Fire()
        {
            _windupStartedAt = -1f;
            _nextFire = Time.time + fireCooldown;
            _lastFireTime = Time.time;
            FireCountForDiagnostics++;
            var spawnPosition = _mouthGlow != null
                ? _mouthGlow.position
                : transform.TransformPoint(Vector3.forward * 0.4f);
            var blob = OrganicMeshFactory.CreateMeshObject(
                "TEMP_ART_ScrapSpit",
                OrganicMeshFactory.SoftBlob(2, 0.25f, 1f, 0.02f),
                null,
                spawnPosition,
                Vector3.one * 0.18f,
                null);
            var renderer = blob.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetSpitMaterial();
            }
            var col = blob.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
            var projectileBody = blob.AddComponent<Rigidbody>();
            projectileBody.isKinematic = true;
            projectileBody.useGravity = false;
            projectileBody.interpolation = RigidbodyInterpolation.Interpolate;
            projectileBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var trail = blob.AddComponent<TrailRenderer>();
            trail.sharedMaterial = GetSpitTrailMaterial();
            // Keep the streak subordinate to the spit itself. The previous
            // 12 cm head-width made a close shot read as a solid orange beam
            // instead of a small blob moving away from the mouth.
            // Keep the streak short enough to read as a moving blob, but leave
            // it visible for a few more frames so the launch direction connects
            // visually to the Spitter's recoil instead of appearing as a bare
            // object that simply materializes in flight.
            trail.time = 0.095f;
            trail.startWidth = 0.025f;
            trail.endWidth = 0.001f;
            trail.minVertexDistance = 0.025f;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.colorGradient = CreateSpitTrailGradient();
            trail.emitting = false;
            var spit = blob.AddComponent<ScrapSpitProjectile>();
            var targetController = _splat.GetComponent<CharacterController>();
            var targetPosition = targetController != null
                ? targetController.transform.TransformPoint(targetController.center)
                : _splat.position + Vector3.up;
            var targetOffset = targetPosition - spawnPosition;
            var horizontalOffset = Vector3.ProjectOnPlane(targetOffset, Vector3.up);
            var horizontalRange = horizontalOffset.magnitude;
            var launchVelocity = targetOffset.normalized * projectileSpeed;
            if (horizontalRange > 0.05f && projectileSpeed > 0.05f)
            {
                // Solve the lower ballistic arc so gravity carries the spit
                // toward the position it was aimed at. A straight-at-target
                // vector followed by gravity consistently dropped shots short,
                // which made combat look arbitrary instead of deliberately led.
                var gravity = ScrapSpitProjectile.Gravity;
                var speedSquared = projectileSpeed * projectileSpeed;
                var discriminant = speedSquared * speedSquared - gravity
                    * (gravity * horizontalRange * horizontalRange
                        + 2f * targetOffset.y * speedSquared);
                if (discriminant >= 0f)
                {
                    var tangent = (speedSquared - Mathf.Sqrt(discriminant))
                        / (gravity * horizontalRange);
                    var cosine = 1f / Mathf.Sqrt(1f + tangent * tangent);
                    var sine = tangent * cosine;
                    launchVelocity = horizontalOffset.normalized * (projectileSpeed * cosine)
                        + Vector3.up * (projectileSpeed * sine);
                }
            }
            spit.LaunchVelocity(launchVelocity, _splat.GetComponent<SplatMotor>());
            Object.Destroy(blob, 3.5f);
        }

        private static Material GetSpitTrailMaterial()
        {
            if (_spitTrailMaterial != null) return _spitTrailMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            if (shader == null) return null;
            _spitTrailMaterial = new Material(shader) { name = "Mat_ScrapSpitTrail" };
            if (_spitTrailMaterial.HasProperty(MouthBaseColor))
                _spitTrailMaterial.SetColor(MouthBaseColor, Color.white);
            if (_spitTrailMaterial.HasProperty(MouthColor))
                _spitTrailMaterial.SetColor(MouthColor, Color.white);
            return _spitTrailMaterial;
        }

        private static Gradient CreateSpitTrailGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.78f, 0.28f), 0f),
                    new GradientColorKey(new Color(1f, 0.24f, 0.035f), 0.48f),
                    new GradientColorKey(new Color(0.45f, 0.055f, 0.02f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.62f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static Material GetSpitMaterial()
        {
            if (_spitMaterial != null)
            {
                return _spitMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                return null;
            }

            // Keep the spit distinct from the overexposed sky and small enough
            // to read as a projectile instead of a second enemy face.
            var color = new Color(0.78f, 0.18f, 0.025f);
            _spitMaterial = new Material(shader) { name = "Mat_ScrapSpitProjectile" };
            if (_spitMaterial.HasProperty("_Metallic"))
            {
                _spitMaterial.SetFloat("_Metallic", 0f);
            }

            if (_spitMaterial.HasProperty("_Smoothness"))
            {
                _spitMaterial.SetFloat("_Smoothness", 0.08f);
            }

            if (_spitMaterial.HasProperty(MouthBaseColor))
            {
                _spitMaterial.SetColor(MouthBaseColor, color);
            }

            if (_spitMaterial.HasProperty(MouthColor))
            {
                _spitMaterial.SetColor(MouthColor, color);
            }

            if (_spitMaterial.HasProperty("_EmissionColor"))
            {
                _spitMaterial.EnableKeyword("_EMISSION");
                _spitMaterial.SetColor("_EmissionColor", color * 0.025f);
            }

            return _spitMaterial;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_defeated)
            {
                return;
            }

            var motor = other.GetComponentInParent<SplatMotor>();
            if (motor == null)
            {
                return;
            }

            var descending = !motor.IsGrounded && other.bounds.center.y > transform.position.y + 0.25f;
            if (descending || motor.PlanarVelocity.sqrMagnitude > 90f)
            {
                Defeat();
                motor.ApplyLaunch(Vector3.up * 6.5f);
            }
        }

        private void Defeat()
        {
            if (_defeated)
            {
                return;
            }

            _defeated = true;
            if (_aggroReported)
            {
                EnemyCombatDirector.Ensure().NotifyEnemyDefeated(this);
                _aggroReported = false;
            }

            TempSfxPlayer.PlayExpose();
            TempCraftSparkBurst.Spawn(transform.position, 12, 0.45f);
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var visualTell = GetComponent<EnemyVisualTell>();
            if (visualTell != null)
            {
                visualTell.enabled = false;
            }

            StartCoroutine(DefeatRoutine());
        }

        private IEnumerator DefeatRoutine()
        {
            const float duration = 0.58f;
            var startPosition = transform.position;
            var startRotation = transform.rotation;
            var away = _splat != null ? transform.position - _splat.position : -transform.forward;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f)
            {
                away = -transform.forward;
                away.y = 0f;
            }
            away.Normalize();
            // Roll around the face normal, not a horizontal world axis. The
            // Spitter's face is surface-mounted, so pitching it away made its
            // eyes slide over the top edge of the sphere during defeat.
            var rollAxis = startRotation * Vector3.forward;
            if (rollAxis.sqrMagnitude < 0.001f)
            {
                rollAxis = Vector3.right;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                var hopThenFall = 0.10f * Mathf.Sin(Mathf.PI * t) - 0.22f * t * t;
                transform.position = startPosition + away * (0.12f * t) + Vector3.up * hopThenFall;
                // A short side-roll keeps both eye and mouth shapes on the visible
                // face while the whole body recoils and drops at stable scale.
                transform.rotation = Quaternion.AngleAxis(34f * t, rollAxis) * startRotation;
                yield return null;
            }

            Destroy(gameObject);
        }

        private void ReportAggro(bool active)
        {
            if (active == _aggroReported)
            {
                return;
            }

            _aggroReported = active;
            var director = EnemyCombatDirector.Ensure();
            if (active)
            {
                director.NotifyAggroEnter(this);
            }
            else
            {
                director.NotifyAggroExit(this);
            }
        }

        private Transform FindVisualChild(string childName)
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

        private void EnsureSplat()
        {
            if (_splat != null)
            {
                return;
            }

            var motor = FindAnyObjectByType<SplatMotor>();
            if (motor != null)
            {
                _splat = motor.transform;
            }
        }
    }

    public sealed class ScrapSpitProjectile : MonoBehaviour
    {
        public const float Gravity = 6f;

        private Vector3 _velocity;
        private Rigidbody _body;
        private SplatMotor _targetMotor;
        private CharacterController _targetController;
        private float _contactRadius;
        private bool _impactHandled;
        private bool _armed;

        public void LaunchVelocity(Vector3 velocity, SplatMotor targetMotor)
        {
            if (_body == null) _body = GetComponent<Rigidbody>();
            _targetMotor = targetMotor;
            _targetController = _targetMotor != null
                ? _targetMotor.GetComponent<CharacterController>() : null;
            var sphere = GetComponent<SphereCollider>();
            var scale = transform.lossyScale;
            _contactRadius = sphere != null
                ? sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))
                : 0.09f;
            _velocity = velocity;
            _armed = true;
            var trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
            }
        }

        private void FixedUpdate()
        {
            if (!_armed)
            {
                return;
            }

            var step = Time.fixedDeltaTime;
            var nextPosition = (_body != null ? _body.position : transform.position)
                + _velocity * step;
            // CharacterController contacts can be missed by a small, fast trigger
            // projectile. Its capsule is the authoritative player hit shape, so
            // explicitly check the projectile's next physics position against it
            // before moving the kinematic body. This keeps hits reliable without
            // adding a Rigidbody to Splat's controller-driven character.
            if (!_impactHandled && _targetMotor != null && OverlapsTargetCapsule(nextPosition))
            {
                HandlePlayerImpact(_targetMotor);
                return;
            }

            if (_body != null)
            {
                _body.MovePosition(nextPosition);
                _body.MoveRotation(_body.rotation * Quaternion.Euler(0f,
                    420f * step, 260f * step));
            }
            else
            {
                transform.position = nextPosition;
                transform.Rotate(0f, 420f * step, 260f * step, Space.Self);
            }
            _velocity.y -= Gravity * step;
        }

        private bool OverlapsTargetCapsule(Vector3 point)
        {
            if (_targetMotor == null) return false;
            var targetTransform = _targetController != null
                ? _targetController.transform : _targetMotor.transform;
            var targetScale = targetTransform.lossyScale;
            var radius = _targetController != null
                ? _targetController.radius * Mathf.Max(Mathf.Abs(targetScale.x), Mathf.Abs(targetScale.z))
                : 0.48f * Mathf.Max(Mathf.Abs(targetScale.x), Mathf.Abs(targetScale.z));
            var height = _targetController != null
                ? _targetController.height * Mathf.Abs(targetScale.y)
                : 1.15f * Mathf.Abs(targetScale.y);
            radius = Mathf.Max(0.01f, radius);
            height = Mathf.Max(height, radius * 2f);
            var center = _targetController != null
                ? targetTransform.TransformPoint(_targetController.center)
                : targetTransform.position + Vector3.up * (height * 0.5f);
            var axisHalfLength = Mathf.Max(0f, height * 0.5f - radius);
            var lower = center - Vector3.up * axisHalfLength;
            var upper = center + Vector3.up * axisHalfLength;
            var closestOnAxis = lower + Vector3.up * Mathf.Clamp(point.y - lower.y,
                0f, upper.y - lower.y);
            var combinedRadius = radius + _contactRadius;
            return (point - closestOnAxis).sqrMagnitude <= combinedRadius * combinedRadius;
        }

        private void OnTriggerEnter(Collider other)
        {
            var motor = other.GetComponentInParent<SplatMotor>();
            if (motor == null)
            {
                return;
            }

            HandlePlayerImpact(motor);
        }

        private void HandlePlayerImpact(SplatMotor motor)
        {
            if (_impactHandled || motor == null) return;
            _impactHandled = true;
            var respawn = motor.GetComponent<PlayerRespawn>();
            if (respawn == null) respawn = motor.GetComponentInParent<PlayerRespawn>();
            respawn?.ApplyKnockback(_velocity.normalized + Vector3.up * 0.4f, 8f);
            TempSfxPlayer.PlayOuch();
            Destroy(gameObject);
        }
    }
}
