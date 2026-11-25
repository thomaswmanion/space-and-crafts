using UnityEngine;

namespace SpaceAndCrafts.AI
{
    /// <summary>
    /// Small authored pose layer for the disconnected, commissioned Scrap Mite mesh.
    /// The mesh parts keep their imported silhouettes; these pivots add counter-swing,
    /// anticipation, and contact recovery without scaling the character.
    /// </summary>
    public sealed class ScrapMiteCommissionedAnimator : MonoBehaviour
    {
        private const int WalkableGroundMask = 1 << 0; // Match the Arrival world's walkable ground layer.
        private ScrapMite _mite;
        private Transform _player;
        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _head;
        private Transform _body;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Transform _leftFoot;
        private Transform _rightFoot;
        private Renderer _leftFootRenderer;
        private Renderer _rightFootRenderer;
        private Transform _antenna;
        private Quaternion _leftArmRest;
        private Quaternion _rightArmRest;
        private Quaternion _headRest;
        private Quaternion _leftLegRest;
        private Quaternion _rightLegRest;
        private Quaternion _leftFootRest;
        private Quaternion _rightFootRest;
        private Quaternion _antennaRest;
        private Vector3 _bodyRestPosition;
        private Quaternion _bodyRestRotation;
        private Vector3 _lastRootPosition;
        private Renderer[] _visualRenderers;
        private float _visualGroundGap = float.NaN;
        private float _phase;
        private float _headYaw;
        private float _nextIdleGlanceAt;
        private float _idleGlanceStartedAt = -1f;
        private float _idleGlanceDuration;
        private float _idleGlanceTargetYaw;
        private float _leftFootGroundGap = float.NaN;
        private float _rightFootGroundGap = float.NaN;

        public Vector3 LeftLegLocalEulerForDiagnostics => _leftLeg != null ? _leftLeg.localEulerAngles : Vector3.zero;
        public Vector3 RightLegLocalEulerForDiagnostics => _rightLeg != null ? _rightLeg.localEulerAngles : Vector3.zero;
        public float GaitPhaseForDiagnostics => _phase;
        public float HeadYawForDiagnostics => _headYaw;
        public float AttackStrokeWeightForDiagnostics => _mite != null
            ? _mite.AttackStrokeWeightForDiagnostics : 0f;
        // When hosted by the live Mite component, it owns post-pose seating and
        // computes the gap from the active commissioned rig's mesh vertices.
        // Use that measurement instead of the standalone-preview fallback, which
        // is intentionally not run on AI-owned rigs.
        public float VisualGroundGapForDiagnostics => _mite != null
            ? _mite.CommissionedVisualGroundGapForDiagnostics
            : _visualGroundGap;
        public string FootGroundGapsForDiagnostics =>
            $"left={_leftFootGroundGap:F3},right={_rightFootGroundGap:F3}";

        private void Awake()
        {
            _mite = GetComponentInParent<ScrapMite>();
            var splat = FindAnyObjectByType<SpaceAndCrafts.Player.SplatMotor>();
            _player = splat != null ? splat.transform : null;
            _leftArm = FindDeep(transform, "ART_MiteArm_L");
            _rightArm = FindDeep(transform, "ART_MiteArm_R");
            _head = FindDeep(transform, "ART_MiteHead");
            _body = FindDeep(transform, "ART_MiteBody");
            _leftLeg = FindDeep(transform, "ART_MiteLeg_0");
            _rightLeg = FindDeep(transform, "ART_MiteLeg_1");
            _leftFoot = FindDeep(transform, "ART_MiteFoot_L");
            _rightFoot = FindDeep(transform, "ART_MiteFoot_R");
            _leftFootRenderer = _leftFoot != null ? _leftFoot.GetComponentInChildren<Renderer>(true) : null;
            _rightFootRenderer = _rightFoot != null ? _rightFoot.GetComponentInChildren<Renderer>(true) : null;
            _antenna = FindDeep(transform, "ART_MiteAntenna");
            if (_leftArm != null) _leftArmRest = _leftArm.localRotation;
            if (_rightArm != null) _rightArmRest = _rightArm.localRotation;
            if (_head != null) _headRest = _head.localRotation;
            if (_leftLeg != null) _leftLegRest = _leftLeg.localRotation;
            if (_rightLeg != null) _rightLegRest = _rightLeg.localRotation;
            if (_leftFoot != null) _leftFootRest = _leftFoot.localRotation;
            if (_rightFoot != null) _rightFootRest = _rightFoot.localRotation;
            if (_antenna != null) _antennaRest = _antenna.localRotation;
            if (_body != null)
            {
                _bodyRestPosition = _body.localPosition;
                _bodyRestRotation = _body.localRotation;
            }
            _visualRenderers = GetComponentsInChildren<Renderer>(true);
            _lastRootPosition = transform.parent != null ? transform.parent.position : transform.position;
            _phase = Random.Range(0f, Mathf.PI * 2f);
            _nextIdleGlanceAt = Time.time + Random.Range(0.9f, 1.8f);
        }

        private void LateUpdate()
        {
            var root = transform.parent != null ? transform.parent : transform;
            var delta = root.position - _lastRootPosition;
            _lastRootPosition = root.position;
            delta.y = 0f;
            var speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            var moveWeight = _mite != null
                ? _mite.CommissionedGaitWeight
                : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 1.55f, speed));
            _phase += Time.deltaTime * (2.1f + speed * 2.3f);
            // The live Mite AI advances this cycle from actual horizontal travel.
            // Share that phase with the visible pivot rig so a foot swing does not
            // continue running on a separate clock while the root moves.
            var gaitPhase = _mite != null ? _mite.CommissionedGaitPhase : _phase;

            var impact = _mite != null ? _mite.ContactFlinchWeightForDiagnostics : 0f;
            var defeat = _mite != null ? _mite.DefeatPoseWeight : 0f;
            var poseAuthority = 1f - defeat;
            var alert = (_mite != null ? _mite.AlertWeightForDiagnostics : 0f) * poseAuthority;
            var attackSettle = _mite != null ? _mite.AttackSettleWeightForDiagnostics : 0f;
            var anticipation = (_mite != null ? _mite.AttackTelegraphWeightForDiagnostics : 0f)
                * (1f - attackSettle * 0.55f) * poseAuthority;
            var attackStroke = (_mite != null ? _mite.AttackStrokeWeightForDiagnostics : 0f) * poseAuthority;
            // The imported pivots are not a continuous skinned surface at each
            // joint. Keep the counter-swing inside those visible seams; the old
            // 13° arm and 20° leg arcs opened mesh islands into a disconnected pose.
            var gait = Mathf.Sin(gaitPhase) * moveWeight * 6f;
            // The commissioned pivot rig's idle cues were only 1–3 degrees and
            // 1.5 mm of body motion, which read as a frozen prop at play distance.
            // Give the existing joints a soft breathing/weight-shift cycle while
            // keeping the authored mesh scale and planted foot contact unchanged.
            var idle = Mathf.Sin(_phase * 0.8f) * (1f - moveWeight) * 7f;
            // The commissioned shoulders fanned both segmented arms wide during
            // the lunge, making their joints read detached. Keep the held tell,
            // then let it release as the leading arm strikes; this separates the
            // anticipation and attack silhouettes instead of layering both.
            var brace = alert * 3.5f + anticipation * 6f * (1f - attackStroke);
            var recoil = impact * 10f;
            // A mirrored brace held both commissioned forearms in the same
            // silhouette during the lunge, which read more like a frozen pose
            // than an attack. Let the left arm lead the existing body stroke;
            // the right arm stays mostly in a counter-brace.
            var strikeLead = attackStroke * 8f;

            // Mirrored Z rotations raise both arms for a tell and counter-swing
            // their hands as the body walks. The mesh and character root stay scaled 1:1.
            if (_leftArm != null)
            {
                _leftArm.localRotation = _leftArmRest
                    * Quaternion.Euler(0f, 0f, -idle - gait - brace - strikeLead + recoil);
            }
            if (_rightArm != null)
            {
                _rightArm.localRotation = _rightArmRest
                    * Quaternion.Euler(0f, 0f, idle + gait + brace - strikeLead * 0.25f - recoil);
            }
            if (_leftLeg != null)
            {
                _leftLeg.localRotation = _leftLegRest
                    * Quaternion.Euler(Mathf.Sin(gaitPhase) * moveWeight * 12f, 0f,
                        Mathf.Sin(_phase * 0.5f) * (1f - moveWeight) * 3f);
            }
            if (_rightLeg != null)
            {
                _rightLeg.localRotation = _rightLegRest
                    * Quaternion.Euler(-Mathf.Sin(gaitPhase) * moveWeight * 12f, 0f,
                        -Mathf.Sin(_phase * 0.5f) * (1f - moveWeight) * 3f);
            }
            if (_leftFoot != null)
            {
                // Counter-rotate the imported lower-foot islands slightly so the
                // ankle follows through instead of making thigh and foot one stiff
                // block. Keep the toe lift subtle to preserve the measured support.
                _leftFoot.localRotation = _leftFootRest
                    * Quaternion.Euler(-Mathf.Sin(gaitPhase) * moveWeight * 7f, 0f,
                        Mathf.Sin(_phase * 0.5f) * (1f - moveWeight) * 1.5f);
            }
            if (_rightFoot != null)
            {
                _rightFoot.localRotation = _rightFootRest
                    * Quaternion.Euler(Mathf.Sin(gaitPhase) * moveWeight * 7f, 0f,
                        -Mathf.Sin(_phase * 0.5f) * (1f - moveWeight) * 1.5f);
            }
            if (_body != null)
            {
                // Keep gait bob subtle, but give idle breathing enough vertical
                // travel to read at encounter scale without stretching the model
                // or changing the support feet.
                var bob = Mathf.Abs(Mathf.Sin(gaitPhase)) * moveWeight * 0.005f
                    + Mathf.Sin(_phase * 0.8f) * (1f - moveWeight) * 0.013f;
                // Carry the parent's short attack stroke into the visible pivot
                // rig. The anticipation is a compact load-back; the pulse then
                // drives one forward beat without changing scale or lifting feet.
                var impactAway = _mite != null
                    ? _mite.ContactFlinchDirectionLocal : Vector3.back;
                _body.localPosition = _bodyRestPosition + Vector3.up * bob
                    + Vector3.forward * (attackStroke * 0.11f - anticipation * 0.04f)
                    + Vector3.down * (anticipation * 0.018f)
                    + impactAway * (impact * 0.065f);
                _body.localRotation = _bodyRestRotation * Quaternion.Euler(
                    -anticipation * 5f + attackStroke * 11f + impact * 10f,
                    0f,
                    Mathf.Sin(_phase * 0.8f) * (1f - moveWeight) * 5f
                    - Mathf.Sign(impactAway.x) * impact * 8f);
            }
            if (_antenna != null)
            {
                // The antenna is an imported island hinged near its base; the old
                // 14° gait sweep visibly pulled its tips away from their sockets.
                var followThrough = Mathf.Sin(_phase - 0.35f) * (2f + moveWeight * 1.2f);
                _antenna.localRotation = _antennaRest
                    * Quaternion.Euler(-followThrough + anticipation * 2f
                        + attackStroke * 2f - impact * 3f,
                        0f, Mathf.Sin(_phase * 0.7f) * (1.8f + moveWeight * 1.2f));
            }
            if (_head != null)
            {
                var nod = Mathf.Sin(_phase * 0.5f) * (8f + moveWeight * 2f);
                if (_player == null)
                {
                    var splat = FindAnyObjectByType<SpaceAndCrafts.Player.SplatMotor>();
                    _player = splat != null ? splat.transform : null;
                }
                var targetYaw = 0f;
                if (_player != null)
                {
                    var toPlayer = root.InverseTransformDirection(_player.position - root.position);
                    toPlayer.y = 0f;
                    if (toPlayer.sqrMagnitude > 0.01f)
                        targetYaw = Mathf.Clamp(Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg, -38f, 38f);
                }
                // The previous idle head motion was a small sinusoidal nod, so
                // a calm Mite still read like a statue. Add a sparse, eased scan
                // only when it is stationary, unthreatened, and far from Splat.
                // Keep this on the head joint: never bob the AI root or loosen
                // the commissioned feet merely to suggest life.
                var playerFar = _player == null || (root.position - _player.position).sqrMagnitude > 42.25f;
                var canGlance = moveWeight < 0.12f && alert < 0.15f && anticipation < 0.05f
                    && attackStroke < 0.05f && impact < 0.05f && defeat < 0.01f && playerFar;
                var idleGlanceWeight = 0f;
                if (canGlance)
                {
                    if (_idleGlanceStartedAt < 0f && Time.time >= _nextIdleGlanceAt)
                    {
                        _idleGlanceStartedAt = Time.time;
                        _idleGlanceDuration = Random.Range(0.72f, 0.96f);
                        _idleGlanceTargetYaw = Random.value < 0.5f ? -28f : 28f;
                    }

                    if (_idleGlanceStartedAt >= 0f)
                    {
                        var glanceElapsed = Time.time - _idleGlanceStartedAt;
                        if (glanceElapsed >= _idleGlanceDuration)
                        {
                            _idleGlanceStartedAt = -1f;
                            _nextIdleGlanceAt = Time.time + Random.Range(2.6f, 4.2f);
                        }
                        else
                        {
                            var glanceIn = Mathf.SmoothStep(0f, 1f,
                                Mathf.InverseLerp(0f, 0.20f, glanceElapsed));
                            var glanceOut = 1f - Mathf.SmoothStep(0f, 1f,
                                Mathf.InverseLerp(_idleGlanceDuration - 0.22f,
                                    _idleGlanceDuration, glanceElapsed));
                            idleGlanceWeight = glanceIn * glanceOut;
                        }
                    }
                }
                else if (_idleGlanceStartedAt >= 0f)
                {
                    // Combat or locomotion can take control immediately; do not
                    // leave a stale look target waiting to resume later.
                    _idleGlanceStartedAt = -1f;
                    _nextIdleGlanceAt = Time.time + Random.Range(1.4f, 2.4f);
                }

                targetYaw = Mathf.Clamp(targetYaw + _idleGlanceTargetYaw * idleGlanceWeight,
                    -38f, 38f);
                _headYaw = Mathf.LerpAngle(_headYaw, targetYaw, 1f - Mathf.Exp(-Time.deltaTime * 7f));
                _head.localRotation = _headRest
                    * Quaternion.Euler(nod + anticipation * 5f + attackStroke * 11f - impact * 9f
                            - idleGlanceWeight * 2.5f,
                        _headYaw, Mathf.Sin(_phase * 0.42f) * 2.5f + alert * 3f
                            + _idleGlanceTargetYaw * idleGlanceWeight * 0.12f);
            }

            // ScrapMite's defeat coroutine also poses the commissioned parts in
            // Update, but this LateUpdate animator owns those same pivots and
            // previously replaced the leg/antenna collapse every rendered frame.
            // Blend a short, scale-free fall into the authored pivot rig here so
            // the active visual can finish the defeat reaction.
            if (defeat > 0f)
            {
                var away = _mite.ContactFlinchDirectionLocal;
                if (away.sqrMagnitude < 0.001f) away = -Vector3.forward;
                away.Normalize();
                var fallAxis = Vector3.Cross(Vector3.up, away).normalized;
                if (fallAxis.sqrMagnitude < 0.001f) fallAxis = Vector3.right;
                var bodyFall = _bodyRestRotation * Quaternion.AngleAxis(52f, fallAxis);
                var sideRoll = -Mathf.Sign(away.x) * 12f;
                if (_body != null)
                {
                    _body.localRotation = Quaternion.Slerp(_bodyRestRotation, bodyFall, defeat);
                    _body.localPosition = _bodyRestPosition + away * (0.08f * defeat)
                        + Vector3.down * (0.06f * defeat);
                }
                if (_head != null)
                {
                    var headFall = _headRest * Quaternion.Euler(26f, 0f, sideRoll);
                    _head.localRotation = Quaternion.Slerp(_head.localRotation, headFall, defeat);
                }
                if (_leftArm != null)
                {
                    _leftArm.localRotation = Quaternion.Slerp(_leftArm.localRotation, _leftArmRest, defeat);
                }
                if (_rightArm != null)
                {
                    _rightArm.localRotation = Quaternion.Slerp(_rightArm.localRotation, _rightArmRest, defeat);
                }
                // This LateUpdate owns the commissioned leg pivots and would
                // otherwise replace their collapse with the idle gait each
                // frame. Reapply the readable tuck after gait, and settle the
                // ankle pivots with it so they do not keep stepping in place.
                var leftSide = Mathf.Sign(_leftLegRest.eulerAngles.z);
                var rightSide = Mathf.Sign(_rightLegRest.eulerAngles.z);
                if (_leftLeg != null)
                {
                    var leftFall = _leftLegRest * Quaternion.Euler(-22f, 0f,
                        Mathf.Approximately(leftSide, 0f) ? 12f : leftSide * 12f);
                    _leftLeg.localRotation = Quaternion.Slerp(_leftLeg.localRotation,
                        leftFall, defeat);
                }
                if (_rightLeg != null)
                {
                    var rightFall = _rightLegRest * Quaternion.Euler(-22f, 0f,
                        Mathf.Approximately(rightSide, 0f) ? -12f : rightSide * 12f);
                    _rightLeg.localRotation = Quaternion.Slerp(_rightLeg.localRotation,
                        rightFall, defeat);
                }
                if (_leftFoot != null)
                {
                    _leftFoot.localRotation = Quaternion.Slerp(_leftFoot.localRotation,
                        _leftFootRest * Quaternion.Euler(9f, 0f, 0f), defeat);
                }
                if (_rightFoot != null)
                {
                    _rightFoot.localRotation = Quaternion.Slerp(_rightFoot.localRotation,
                        _rightFootRest * Quaternion.Euler(9f, 0f, 0f), defeat);
                }
                if (_antenna != null)
                {
                    var antennaFall = _antennaRest * Quaternion.Euler(36f, 0f, sideRoll * 1.4f);
                    _antenna.localRotation = Quaternion.Slerp(_antenna.localRotation, antennaFall, defeat);
                }
            }

            // ScrapMite owns the selected support surface and already runs the
            // commissioned visual's bounds correction. Apply it after this pose
            // so a second independent raycast cannot choose a different floor on
            // a step-down or overlapping pad. Standalone preview rigs retain the
            // local probe fallback.
            if (_mite != null)
            {
                _mite.AlignCommissionedVisualAfterPose();
                var supportY = _mite.GroundYForVisuals;
                _leftFootGroundGap = _leftFootRenderer != null && _leftFootRenderer.enabled
                    ? _leftFootRenderer.bounds.min.y - supportY : float.NaN;
                _rightFootGroundGap = _rightFootRenderer != null && _rightFootRenderer.enabled
                    ? _rightFootRenderer.bounds.min.y - supportY : float.NaN;
            }
            else
            {
                SeatVisibleRigToWalkableGround();
            }
        }

        private void SeatVisibleRigToWalkableGround()
        {
            if (_visualRenderers == null || _visualRenderers.Length == 0) return;

            var lowestY = float.PositiveInfinity;
            for (var i = 0; i < _visualRenderers.Length; i++)
            {
                var renderer = _visualRenderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                lowestY = Mathf.Min(lowestY, renderer.bounds.min.y);
            }
            if (float.IsInfinity(lowestY)) return;

            var rayOrigin = transform.position + Vector3.up * 1.5f;
            var hits = Physics.RaycastAll(rayOrigin, Vector3.down, 5f,
                WalkableGroundMask, QueryTriggerInteraction.Ignore);
            var bestDistance = float.PositiveInfinity;
            var groundY = float.NaN;
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.normal.y < 0.65f) continue;
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                if (_mite != null && hit.collider.GetComponentInParent<ScrapMite>() == _mite) continue;

                var distance = Mathf.Abs(hit.point.y - lowestY);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                groundY = hit.point.y;
            }
            if (float.IsNaN(groundY) || bestDistance > 0.65f) return;

            const float clearance = 0.006f;
            // Height changes in the patrol capture can approach half a metre within
            // one render interval. Permit the authored visual root to catch up in
            // a few frames while still bounding per-frame motion at low frame rates.
            const float maxCorrectionSpeed = 15f;
            const float maxCorrectionPerFrame = 0.24f;
            var requestedCorrection = groundY + clearance - lowestY;
            var correction = Mathf.Clamp(requestedCorrection,
                -Mathf.Min(maxCorrectionPerFrame, maxCorrectionSpeed * Time.deltaTime),
                Mathf.Min(maxCorrectionPerFrame, maxCorrectionSpeed * Time.deltaTime));
            transform.position += Vector3.up * correction;
            _visualGroundGap = lowestY + correction - groundY;
        }

        private static Transform FindDeep(Transform root, string objectName)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == objectName) return transforms[i];
            }
            return null;
        }
    }
}
