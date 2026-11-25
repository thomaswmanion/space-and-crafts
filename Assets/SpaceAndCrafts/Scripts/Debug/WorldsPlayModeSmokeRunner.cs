using System.Collections;
using System.Collections.Generic;
using System.Text;
using SpaceAndCrafts.AI;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.Player;
using Unity.Profiling;
using UnityEngine;

namespace SpaceAndCrafts.DebugTools
{
    /// <summary>
    /// Lightweight PlayMode load+component smoke for Worlds 02–10.
    /// Proves each scene loads with Splat + craft/boss basics — not a full Arrival-style
    /// collect/craft/boss critical-path loop (too heavy to batch across nine worlds).
    /// Driven by WorldsPlayModeSmokeMenu / batchmode EnterPlaymode.
    /// </summary>
    public sealed class WorldsPlayModeSmokeRunner : MonoBehaviour
    {
        public int WorldIndex { get; set; } = 2;

        public static bool LastRunPassed { get; private set; }
        public static string LastReport { get; private set; } = string.Empty;

        private bool _groundPoundChainPassed;
        private string _groundPoundChainReport;
        private bool _traversalPadJumpPassed;
        private string _traversalPadJumpReport;
        private bool _spawnToTraversalPadPassed;
        private string _spawnToTraversalPadReport;
        private bool _world03PursuitCapPassed;
        private bool _world03OptionalSpurPassed;
        private WorldSmokeCameraProfilerRecorder _cameraProfiler;

        private static readonly string[][] InventionNameHints =
        {
            new[] { "ART_Boing", "Boing" }, // world 01 unused here
            new[] { "ART_VineSpooler", "ART_vine_spooler" },
            new[] { "ART_JunkMagnetGauntlet", "ART_junk_magnet_gauntlet" },
            new[] { "ART_PaintBubbleRaft", "ART_paint_bubble_raft" },
            new[] { "ART_WindUpBridgeBug", "ART_windup_bridge_bug" },
            new[] { "ART_HeatTapeTorch", "ART_heat_tape_torch" },
            new[] { "ART_BoomDishRedirector", "ART_boom_dish_redirector" },
            new[] { "ART_VacuumSkimmer", "ART_vacuum_skimmer" },
            new[] { "ART_StarSeedPlanter", "ART_star_seed_planter" },
            new[] { "ART_GlassSailShield", "ART_glass_sail_shield" },
        };

        private void Start()
        {
            Time.maximumDeltaTime = 0.033f;
            Application.targetFrameRate = 60;
            StartCoroutine(RunSmoke());
        }

        private IEnumerator RunSmoke()
        {
            LastRunPassed = false;
            var report = new StringBuilder();
            var pass = true;

            void Fail(string msg)
            {
                pass = false;
                report.AppendLine("FAIL: " + msg);
                Debug.LogError($"[WorldSmoke] World {WorldIndex:00}: {msg}");
            }

            void Ok(string msg)
            {
                report.AppendLine("OK: " + msg);
                Debug.Log($"[WorldSmoke] World {WorldIndex:00}: {msg}");
            }

            yield return null;
            yield return new WaitForSeconds(0.25f);

            if (WorldIndex == 3)
            {
                var profilerObject = new GameObject("WorldSmoke_CameraProfiler");
                _cameraProfiler = profilerObject.AddComponent<WorldSmokeCameraProfilerRecorder>();
                _cameraProfiler.Begin();
            }

            var splat = GameObject.Find("Splat_TEMP");
            if (splat == null)
            {
                Fail("Splat_TEMP missing");
            }
            else
            {
                var cc = splat.GetComponent<CharacterController>();
                if (cc == null)
                {
                    Fail("Splat_TEMP missing CharacterController");
                }
                else
                {
                    Ok("Splat_TEMP + CharacterController present");
                }
            }

            var motor = splat != null ? splat.GetComponent<SplatMotor>() : FindAnyObjectByType<SplatMotor>();
            if (motor != null)
            {
                var settings = GetMotorSettings(motor);
                if (settings == null)
                {
                    Fail("SplatMotor.settings is null");
                }
                else
                {
                    Ok("SplatMotor settings assigned");
                }

                yield return VerifyWorldEmergencyRespawn(motor, Fail, Ok);
            }

            if (WorldIndex >= 3)
            {
                var rig = FindAnyObjectByType<PlatformingCameraRig>();
                var camera = Camera.main;
                if (rig == null)
                {
                    Fail("PlatformingCameraRig missing");
                }
                else
                {
                    var cameraSettings = GetPrivateField<CameraSettings>(rig, "settings");
                    var followTarget = GetPrivateField<Transform>(rig, "followTarget");
                    var rigCamera = GetPrivateField<Camera>(rig, "gameCamera");
                    var collision = GetPrivateField<CameraCollision>(rig, "collision");
                    var collisionSettings = collision != null
                        ? GetPrivateField<CameraSettings>(collision, "settings")
                        : null;
                    if (cameraSettings == null || followTarget != motor?.transform
                        || rigCamera == null || rigCamera != camera || collisionSettings == null)
                    {
                        Fail($"Camera rig wiring invalid (settings={cameraSettings != null}, follow={followTarget == motor?.transform}, mainCamera={camera != null && rigCamera == camera}, collisionSettings={collisionSettings != null})");
                    }
                    else
                    {
                        Ok("Camera rig, collision, settings, follow target, and MainCamera references valid");
                        if (TryCameraObstructionCheck(cameraSettings, collision, out var obstructionReport))
                        {
                            Ok(obstructionReport);
                        }
                        else
                        {
                            Fail(obstructionReport);
                        }
                    }
                }
            }

            if (!HasBuildSiteOrInvention())
            {
                Fail("BuildSite or invention missing");
            }
            else
            {
                Ok("BuildSite or invention present");
            }

            var bossGo = GameObject.Find("ART_WorldBossExam");
            var boss = bossGo != null
                ? bossGo.GetComponent<WorldBossExam>()
                : FindAnyObjectByType<WorldBossExam>(FindObjectsInactive.Include);
            if (boss == null && bossGo == null)
            {
                Fail("ART_WorldBossExam missing");
            }
            else
            {
                Ok("ART_WorldBossExam present");
            }

            if (WorldIndex >= 3 && motor != null)
            {
                var input = motor.GetComponent<SplatInputReader>();
                var interactor = motor.GetComponent<SplatInteractor>();
                var pickups = FindObjectsByType<CollectiblePickup>(FindObjectsInactive.Exclude);
                CollectiblePickup routeTarget = null;
                var routeStart = motor.transform.position;
                var routeStartRotation = motor.transform.rotation;
                var closestObjectiveDistance = float.PositiveInfinity;
                if (WorldIndex == 3)
                {
                    for (var padIndex = 0; padIndex < 4; padIndex++)
                    {
                        var pad = FindByExactName($"ART_TraversalPad_{padIndex}");
                        var padCollider = pad != null ? pad.GetComponent<Collider>() : null;
                        if (padCollider != null)
                        {
                            var bounds = padCollider.bounds;
                            Debug.Log($"[WorldSmoke] World 03 traversal pad {padIndex}: center={bounds.center:F2}, top={bounds.max.y:F2}, size={bounds.size:F2}, spawnDistance={Vector3.ProjectOnPlane(bounds.center - routeStart, Vector3.up).magnitude:F2}m");
                        }
                        else
                        {
                            Debug.LogWarning($"[WorldSmoke] World 03 traversal pad {padIndex} collider missing");
                        }
                    }

                    var cameraOccludersValid = true;
                    for (var archIndex = 0; archIndex < 3; archIndex++)
                    {
                        var arch = FindByExactName($"ART_FoilArch_{archIndex}");
                        var filter = arch != null ? arch.GetComponent<MeshFilter>() : null;
                        var meshCollider = arch != null ? arch.GetComponent<MeshCollider>() : null;
                        cameraOccludersValid &= arch != null && arch.layer == 31
                            && filter != null && filter.sharedMesh != null
                            && meshCollider != null && meshCollider.sharedMesh == filter.sharedMesh;
                    }

                    var playerCameraLayerIgnored = Physics.GetIgnoreLayerCollision(motor.gameObject.layer, 31);
                    if (cameraOccludersValid && playerCameraLayerIgnored)
                    {
                        Ok("World 03 foil arches have matching camera occluder meshes; player collision is ignored on the camera-only layer");
                    }
                    else
                    {
                        Fail($"World 03 camera occluders invalid={cameraOccludersValid}, player-layer-ignore={playerCameraLayerIgnored}");
                    }
                }
                else if (WorldIndex == 10)
                {
                    var encounterPack = FindByExactName("ART_EncounterPack");
                    var encounterMembers = encounterPack != null
                        ? encounterPack.GetComponentsInChildren<Transform>()
                        : System.Array.Empty<Transform>();
                    var encounterCount = 0;
                    var nearestEncounterDistance = float.PositiveInfinity;
                    var pad2 = FindByExactName("ART_TraversalPad_2");
                    var pad3 = FindByExactName("ART_TraversalPad_3");
                    var pad2Center = pad2 != null && pad2.GetComponent<Collider>() != null
                        ? pad2.GetComponent<Collider>().bounds.center
                        : Vector3.one * float.NaN;
                    var pad3Center = pad3 != null && pad3.GetComponent<Collider>() != null
                        ? pad3.GetComponent<Collider>().bounds.center
                        : Vector3.one * float.NaN;
                    var nearestPad2Distance = float.PositiveInfinity;
                    var nearestPad3Distance = float.PositiveInfinity;
                    var nearestPad2SurfaceGap = float.PositiveInfinity;
                    var nearestPad3SurfaceGap = float.PositiveInfinity;
                    var encounterColliderCount = 0;
                    for (var i = 0; i < encounterMembers.Length; i++)
                    {
                        var member = encounterMembers[i];
                        if (member == null || !member.name.StartsWith("ART_ScrapMite_"))
                        {
                            continue;
                        }

                        encounterCount++;
                        var offset = member.position - motor.transform.position;
                        offset.y = 0f;
                        nearestEncounterDistance = Mathf.Min(nearestEncounterDistance, offset.magnitude);
                        offset = member.position - pad2Center;
                        offset.y = 0f;
                        nearestPad2Distance = Mathf.Min(nearestPad2Distance, offset.magnitude);
                        offset = member.position - pad3Center;
                        offset.y = 0f;
                        nearestPad3Distance = Mathf.Min(nearestPad3Distance, offset.magnitude);

                        var memberCollider = member.GetComponent<Collider>();
                        if (memberCollider == null)
                        {
                            memberCollider = member.GetComponentInChildren<Collider>();
                        }

                        if (memberCollider != null && pad2 != null && pad3 != null)
                        {
                            var pad2Collider = pad2.GetComponent<Collider>();
                            var pad3Collider = pad3.GetComponent<Collider>();
                            if (pad2Collider != null && pad3Collider != null)
                            {
                                encounterColliderCount++;
                                nearestPad2SurfaceGap = Mathf.Min(nearestPad2SurfaceGap,
                                    HorizontalBoundsGap(memberCollider.bounds, pad2Collider.bounds));
                                nearestPad3SurfaceGap = Mathf.Min(nearestPad3SurfaceGap,
                                    HorizontalBoundsGap(memberCollider.bounds, pad3Collider.bounds));
                            }
                        }
                    }

                    if (encounterCount >= 8 && nearestEncounterDistance >= 6.4f
                        && nearestPad2Distance >= 1.8f && nearestPad3Distance >= 2.8f
                        && encounterColliderCount == encounterCount
                        && nearestPad2SurfaceGap >= 0.25f && nearestPad3SurfaceGap >= 0.25f)
                    {
                        Ok($"World 10 opening encounter spacing: mites={encounterCount}, spawn={nearestEncounterDistance:F2}m, pad2={nearestPad2Distance:F2}m (surfaceGap={nearestPad2SurfaceGap:F2}m), pad3={nearestPad3Distance:F2}m (surfaceGap={nearestPad3SurfaceGap:F2}m), colliderCoverage={encounterColliderCount}/{encounterCount}");
                    }
                    else
                    {
                        Fail($"World 10 encounter spacing below threshold: mites={encounterCount}, spawn={nearestEncounterDistance:F2}m (>=6.40), pad2={nearestPad2Distance:F2}m (>=1.80, surfaceGap={nearestPad2SurfaceGap:F2}m >=0.25), pad3={nearestPad3Distance:F2}m (>=2.80, surfaceGap={nearestPad3SurfaceGap:F2}m >=0.25), colliderCoverage={encounterColliderCount}/{encounterCount}");
                    }

                    var cathedralOccludersValid = true;
                    for (var archIndex = 0; archIndex < 3; archIndex++)
                    {
                        var arch = FindByExactName($"ART_CathedralArch_{archIndex}");
                        var filter = arch != null ? arch.GetComponent<MeshFilter>() : null;
                        var meshCollider = arch != null ? arch.GetComponent<MeshCollider>() : null;
                        cathedralOccludersValid &= arch != null && arch.layer == 31
                            && filter != null && filter.sharedMesh != null
                            && meshCollider != null && meshCollider.sharedMesh == filter.sharedMesh;
                    }

                    var playerCameraLayerIgnored = Physics.GetIgnoreLayerCollision(motor.gameObject.layer, 31);
                    if (cathedralOccludersValid && playerCameraLayerIgnored)
                    {
                        Ok("World 10 cathedral landmark meshes have camera-only colliders; player collision ignores their layer");
                    }
                    else
                    {
                        Fail($"World 10 camera occluders invalid={cathedralOccludersValid}, player-layer-ignore={playerCameraLayerIgnored}");
                    }
                }

                for (var i = 0; i < pickups.Length; i++)
                {
                    var pickup = pickups[i];
                    if (pickup == null || pickup.Definition == null || !pickup.CanInteract
                        || pickup.Definition.Category != CollectibleCategory.Component)
                    {
                        continue;
                    }

                    var offset = pickup.transform.position - routeStart;
                    offset.y = 0f;
                    if (offset.magnitude < closestObjectiveDistance)
                    {
                        closestObjectiveDistance = offset.magnitude;
                        routeTarget = pickup;
                    }
                }

                if (input == null || interactor == null || routeTarget == null)
                {
                    Fail($"World {WorldIndex:00} continuous route is missing input, interactor, or an active component objective");
                }
                else
                {
                    var targetId = routeTarget.Definition.Id;
                    var routeElapsed = 0f;
                    var totalDistance = 0f;
                    var previousPosition = motor.transform.position;
                    var minimumY = previousPosition.y;
                    var groundedSamples = 0;
                    var shadowSamples = 0;
                    var cameraVisibleSamples = 0;
                    var minCameraX = float.PositiveInfinity;
                    var maxCameraX = float.NegativeInfinity;
                    var minCameraY = float.PositiveInfinity;
                    var maxCameraY = float.NegativeInfinity;
                    var minCameraZ = float.PositiveInfinity;
                    var camera = Camera.main;
                    var sfxAtRouteStart = TempSfxPlayer.Instance;
                    var footstepCountAtRouteStart = sfxAtRouteStart != null
                        ? sfxAtRouteStart.FootstepCuePlayCountForDiagnostics
                        : 0;
                    if (camera == null)
                    {
                        Fail($"World {WorldIndex:00} continuous route has no enabled MainCamera");
                    }
                    const float stopDistance = 0.85f;
                    while (routeTarget != null && routeTarget.CanInteract && routeElapsed < 8f)
                    {
                        var toTarget = routeTarget.transform.position - motor.transform.position;
                        toTarget.y = 0f;
                        if (toTarget.magnitude <= stopDistance)
                        {
                            break;
                        }

                        var forward = camera != null ? camera.transform.forward : motor.transform.forward;
                        var right = camera != null ? camera.transform.right : motor.transform.right;
                        forward.y = 0f;
                        right.y = 0f;
                        forward.Normalize();
                        right.Normalize();
                        input.DebugSetMove(new Vector2(Vector3.Dot(toTarget.normalized, right),
                            Vector3.Dot(toTarget.normalized, forward)));
                        routeElapsed += Time.deltaTime;
                        minimumY = Mathf.Min(minimumY, motor.transform.position.y);
                        if (motor.IsGrounded)
                        {
                            groundedSamples++;
                            if (motor.GetComponent<PlayerLandingShadow>()?.IsVisibleForDiagnostics == true)
                            {
                                shadowSamples++;
                            }
                        }

                        if (camera != null)
                        {
                            var focus = camera.WorldToViewportPoint(motor.transform.position + Vector3.up * 0.5f);
                            minCameraX = Mathf.Min(minCameraX, focus.x);
                            maxCameraX = Mathf.Max(maxCameraX, focus.x);
                            minCameraY = Mathf.Min(minCameraY, focus.y);
                            maxCameraY = Mathf.Max(maxCameraY, focus.y);
                            minCameraZ = Mathf.Min(minCameraZ, focus.z);
                            if (focus.z > 0f && focus.x >= 0.02f && focus.x <= 0.98f
                                && focus.y >= 0.05f && focus.y <= 0.95f)
                            {
                                cameraVisibleSamples++;
                            }
                        }

                        yield return null;
                        totalDistance += Vector3.ProjectOnPlane(
                            motor.transform.position - previousPosition, Vector3.up).magnitude;
                        previousPosition = motor.transform.position;
                    }

                    input.DebugClear();
                    var remaining = routeTarget != null
                        ? Vector3.ProjectOnPlane(routeTarget.transform.position - motor.transform.position, Vector3.up).magnitude
                        : 0f;
                    var focused = interactor.CurrentFocus == routeTarget;
                    Debug.Log($"[WorldSmoke] World {WorldIndex:00} continuous route: objective={targetId}, start={routeStart:F2}, target={routeTarget.transform.position:F2}, initialDistance={closestObjectiveDistance:F2}m, traveled={totalDistance:F2}m, remaining={remaining:F2}m, elapsed={routeElapsed:F2}s, minY={minimumY:F2}, groundedSamples={groundedSamples}, shadowSamples={shadowSamples}, cameraVisibleSamples={cameraVisibleSamples}, cameraViewportX={minCameraX:F2}..{maxCameraX:F2},Y={minCameraY:F2}..{maxCameraY:F2},Zmin={minCameraZ:F2}, cameraPosition={(camera != null ? camera.transform.position.ToString("F2") : "null")}, focused={focused}, collected={!routeTarget.CanInteract}.");
                    if (routeTarget.CanInteract && remaining <= interactor.Radius + 0.2f)
                    {
                        input.DebugPulseInteract();
                        yield return null;
                        input.DebugClear();
                    }

                    if (routeTarget.CanInteract || remaining > interactor.Radius + 0.3f)
                    {
                        Fail($"World {WorldIndex:00} continuous route did not reach {targetId} (remaining={remaining:F2}m, elapsed={routeElapsed:F2}s, minY={minimumY:F2})");
                    }
                    else
                    {
                        var minimumVisibleSamples = Mathf.Max(1, groundedSamples / 2);
                        if (camera == null || cameraVisibleSamples < minimumVisibleSamples)
                        {
                            Fail($"World {WorldIndex:00} camera did not keep Splat in frame ({cameraVisibleSamples} visible samples from {groundedSamples} grounded samples)");
                        }
                        else
                        {
                            Ok($"World {WorldIndex:00} continuous input reached component {targetId} over {totalDistance:F2}m; grounded shadow sampled {shadowSamples}/{groundedSamples} times and camera focus remained visible {cameraVisibleSamples} times");
                        }
                    }

                    var sfxAfterRoute = TempSfxPlayer.Instance;
                    var routeFootstepsPlayed = sfxAfterRoute != null
                        && sfxAfterRoute.AudioSourceReadyForDiagnostics
                        && sfxAfterRoute.HasMovementCueClipsForDiagnostics
                        && sfxAfterRoute.FootstepCuePlayCountForDiagnostics > footstepCountAtRouteStart;
                    if (routeFootstepsPlayed)
                    {
                        Ok("Footstep audio cue fired during continuous route movement");
                    }
                    else
                    {
                        Fail("Continuous route movement produced no configured footstep audio cue");
                    }

                    if (WorldIndex == 10)
                    {
                        yield return VerifyWorld10ApproachCamera(motor, camera,
                            "ART_TraversalPad_2", Fail, Ok);
                        yield return VerifyWorld10ApproachCamera(motor, camera,
                            "ART_TraversalPad_3", Fail, Ok);
                    }

                    if (WorldIndex == 3 && !routeTarget.CanInteract)
                    {
                        yield return VerifySpawnToTraversalPad(motor, input, routeStart, routeStartRotation);
                        var traversalRoutePassed = _spawnToTraversalPadPassed;
                        if (_spawnToTraversalPadPassed)
                        {
                            Ok(_spawnToTraversalPadReport);
                            yield return VerifyWorld03PursuitCap(motor, Fail, Ok);
                            if (!_world03PursuitCapPassed)
                            {
                                traversalRoutePassed = false;
                            }

                            for (var sourceIndex = 0; sourceIndex < 3; sourceIndex++)
                            {
                                var runupFrames = 6;
                                if (sourceIndex == 2)
                                {
                                    yield return WaitForWorld03ThreatTellWindow(motor, input, Ok);
                                }
                                yield return VerifyTraversalPadJump(motor, input, sourceIndex,
                                    sourceIndex + 1, stageAtSource: false, runupFrames: runupFrames);
                                if (_traversalPadJumpPassed)
                                {
                                    Ok(_traversalPadJumpReport);
                                }
                                else
                                {
                                    traversalRoutePassed = false;
                                    Fail(_traversalPadJumpReport);
                                    break;
                                }

                                if (sourceIndex == 1)
                                {
                                    yield return VerifyWorld03OptionalWonderSpur(motor, input, Fail, Ok);
                                    if (!_world03OptionalSpurPassed)
                                    {
                                        traversalRoutePassed = false;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Fail(_spawnToTraversalPadReport);
                        }
                        _traversalPadJumpPassed = traversalRoutePassed;

                        yield return VerifyGroundPoundChain(motor, input, routeStart, routeStartRotation);
                        if (_groundPoundChainPassed)
                        {
                            Ok(_groundPoundChainReport);
                        }
                        else
                        {
                            Fail(_groundPoundChainReport);
                        }

                        yield return VerifyWorld03MiteContact(motor, input, routeStart,
                            routeStartRotation, Fail, Ok);
                    }
                }
            }

            if (_cameraProfiler != null)
            {
                report.AppendLine(_cameraProfiler.FinishReport());
                Destroy(_cameraProfiler.gameObject);
                _cameraProfiler = null;
            }

            Finish(pass, report.ToString());
        }

        private IEnumerator VerifyWorld10ApproachCamera(SplatMotor motor, Camera camera,
            string padName, System.Action<string> fail, System.Action<string> ok)
        {
            var pad = FindByExactName(padName);
            var collider = pad != null ? pad.GetComponent<Collider>() : null;
            var controller = motor != null ? motor.GetComponent<CharacterController>() : null;
            if (collider == null || controller == null || camera == null)
            {
                fail($"World 10 {padName} camera sample missing pad collider, player controller, or main camera");
                yield break;
            }

            var originalPosition = motor.transform.position;
            var originalRotation = motor.transform.rotation;
            var controllerWasEnabled = controller.enabled;
            controller.enabled = false;
            motor.transform.SetPositionAndRotation(
                new Vector3(collider.bounds.center.x,
                    collider.bounds.max.y + controller.height * 0.5f + 0.02f,
                    collider.bounds.center.z),
                pad.transform.rotation);
            controller.enabled = controllerWasEnabled;
            motor.HardResetState();
            Physics.SyncTransforms();

            var rig = FindAnyObjectByType<PlatformingCameraRig>();
            var collision = FindAnyObjectByType<CameraCollision>();
            for (var i = 0; i < 14; i++) yield return null;

            var visibleSamples = 0;
            var landmarkSamples = 0;
            var gatewaySamples = 0;
            var gatewayCoverageTotal = 0f;
            var maximumGatewayCoverage = 0f;
            var gatewayCoverageSamples = 0;
            var minimumPositiveGatewayCoverage = float.PositiveInfinity;
            var gatewayCoreViewportSamples = 0;
            var gatewayCoreClearRaySamples = 0;
            var gatewayCoreCoverageSamples = 0;
            var gatewayCoreCoverageTotal = 0f;
            var encounterVisiblePerFrameTotal = 0;
            var maximumVisibleEncounterMembers = 0;
            var minimumArmDistance = float.PositiveInfinity;
            var maximumHiddenOccluders = 0;
            var landmark = FindByExactName("ART_CathedralArch_0");
            var gatewayCore = FindByExactName("ART_RainbowHub");
            var gatewayCoreRenderer = gatewayCore != null
                ? gatewayCore.GetComponent<Renderer>()
                : null;
            var gatewayRenderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            var encounterPack = FindByExactName("ART_EncounterPack");
            var encounterMembers = encounterPack != null
                ? encounterPack.GetComponentsInChildren<Transform>()
                : System.Array.Empty<Transform>();
            for (var i = 0; i < 18; i++)
            {
                var focus = camera.WorldToViewportPoint(motor.transform.position + Vector3.up * 0.5f);
                if (focus.z > 0f && focus.x >= 0.02f && focus.x <= 0.98f
                    && focus.y >= 0.05f && focus.y <= 0.95f)
                {
                    visibleSamples++;
                }

                if (landmark != null)
                {
                    var landmarkPoint = camera.WorldToViewportPoint(landmark.transform.position);
                    if (landmarkPoint.z > 0f && landmarkPoint.x >= 0f && landmarkPoint.x <= 1f
                        && landmarkPoint.y >= 0f && landmarkPoint.y <= 1f)
                    {
                        landmarkSamples++;
                    }
                }

                var gatewayInFrame = false;
                var gatewayCoverageForFrame = 0f;
                var gatewayFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
                for (var rendererIndex = 0; rendererIndex < gatewayRenderers.Length; rendererIndex++)
                {
                    var renderer = gatewayRenderers[rendererIndex];
                    if (renderer == null || !renderer.enabled
                        || !renderer.gameObject.name.StartsWith("ART_CathedralArch_"))
                    {
                        continue;
                    }

                    if (GeometryUtility.TestPlanesAABB(gatewayFrustumPlanes, renderer.bounds))
                    {
                        gatewayInFrame = true;
                        gatewayCoverageForFrame = Mathf.Max(gatewayCoverageForFrame,
                            ProjectedViewportCoverage(camera, renderer.bounds));
                    }
                }

                if (gatewayInFrame)
                {
                    gatewaySamples++;
                }
                gatewayCoverageTotal += gatewayCoverageForFrame;
                maximumGatewayCoverage = Mathf.Max(maximumGatewayCoverage,
                    gatewayCoverageForFrame);
                if (gatewayCoverageForFrame >= 0.15f)
                {
                    gatewayCoverageSamples++;
                }
                if (gatewayCoverageForFrame > 0f)
                {
                    minimumPositiveGatewayCoverage = Mathf.Min(minimumPositiveGatewayCoverage,
                        gatewayCoverageForFrame);
                }

                if (gatewayCoreRenderer != null && gatewayCoreRenderer.enabled)
                {
                    var coreBounds = gatewayCoreRenderer.bounds;
                    var corePoint = camera.WorldToViewportPoint(coreBounds.center);
                    if (corePoint.z > 0f && corePoint.x >= 0f && corePoint.x <= 1f
                        && corePoint.y >= 0f && corePoint.y <= 1f)
                    {
                        gatewayCoreViewportSamples++;
                    }

                    var coreCoverage = ProjectedViewportCoverage(camera, coreBounds);
                    gatewayCoreCoverageTotal += coreCoverage;
                    if (coreCoverage >= 0.01f)
                    {
                        gatewayCoreCoverageSamples++;
                    }
                    var ray = coreBounds.center - camera.transform.position;
                    var rayDistance = ray.magnitude;
                    if (rayDistance > 0.01f
                        && !Physics.Raycast(camera.transform.position, ray / rayDistance,
                            rayDistance, ~(1 << motor.gameObject.layer),
                            QueryTriggerInteraction.Ignore))
                    {
                        gatewayCoreClearRaySamples++;
                    }
                }

                var visibleEncounterMembers = 0;
                var cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
                for (var memberIndex = 0; memberIndex < encounterMembers.Length; memberIndex++)
                {
                    var member = encounterMembers[memberIndex];
                    if (member == null || !member.name.StartsWith("ART_ScrapMite_"))
                    {
                        continue;
                    }

                    var renderers = member.GetComponentsInChildren<Renderer>();
                    var memberInFrame = false;
                    for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                    {
                        var renderer = renderers[rendererIndex];
                        if (renderer == null || !renderer.enabled)
                        {
                            continue;
                        }

                        if (GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, renderer.bounds))
                        {
                            memberInFrame = true;
                            break;
                        }
                    }

                    if (memberInFrame)
                    {
                        visibleEncounterMembers++;
                    }
                }

                encounterVisiblePerFrameTotal += visibleEncounterMembers;
                maximumVisibleEncounterMembers = Mathf.Max(maximumVisibleEncounterMembers,
                    visibleEncounterMembers);

                if (rig != null)
                {
                    minimumArmDistance = Mathf.Min(minimumArmDistance,
                        rig.OrbitDistanceForDiagnostics);
                }

                if (collision != null)
                {
                    maximumHiddenOccluders = Mathf.Max(maximumHiddenOccluders,
                        collision.HiddenOccluderCountForDiagnostics);
                }

                yield return null;
            }

            controller.enabled = false;
            motor.transform.SetPositionAndRotation(originalPosition, originalRotation);
            controller.enabled = controllerWasEnabled;
            motor.HardResetState();
            Physics.SyncTransforms();
            yield return null;

            var encounterVisibilityPassed = padName != "ART_TraversalPad_3"
                || maximumVisibleEncounterMembers >= 2;
            var passed = visibleSamples >= 15 && minimumArmDistance >= 2.9f
                && encounterVisibilityPassed && gatewaySamples >= 15
                && gatewayCoverageSamples >= 15
                && gatewayCoreViewportSamples >= 15
                && gatewayCoreClearRaySamples >= 15
                && gatewayCoreCoverageSamples >= 15;
            var averageVisibleEncounterMembers = encounterVisiblePerFrameTotal / 18f;
            var averageGatewayCoverage = gatewayCoverageTotal / 18f;
            var averageGatewayCoreCoverage = gatewayCoreCoverageTotal / 18f;
            var message = $"World 10 {padName} camera: player={visibleSamples}/18 visible, arch={landmarkSamples}/18 in frame, gateway={gatewaySamples}/18 in frame (coverage>=15%: {gatewayCoverageSamples}/18, avg={averageGatewayCoverage:P2}, min={minimumPositiveGatewayCoverage:P2}, max={maximumGatewayCoverage:P2}), core={gatewayCoreViewportSamples}/18 in viewport (coverage>=1%: {gatewayCoreCoverageSamples}/18, avg={averageGatewayCoreCoverage:P2}, clearRay={gatewayCoreClearRaySamples}/18), encounterVisible={averageVisibleEncounterMembers:F1}/8 avg (max={maximumVisibleEncounterMembers}), minArm={minimumArmDistance:F2}m, maxHiddenOccluders={maximumHiddenOccluders}";
            if (passed) ok(message);
            else fail(message + " (required player>=15/18, camera arm>=2.90m, gateway>=15/18 with screen coverage>=15%, core>=15/18 with screen coverage>=1% and clear ray, and at least two encounter mites in the pad 3 frustum)");
        }

        private IEnumerator VerifyWorldEmergencyRespawn(SplatMotor motor,
            System.Action<string> fail, System.Action<string> ok)
        {
            var respawn = motor != null ? motor.GetComponent<PlayerRespawn>() : null;
            var controller = motor != null ? motor.GetComponent<CharacterController>() : null;
            if (respawn == null || controller == null)
            {
                fail($"World {WorldIndex:00} emergency recovery missing PlayerRespawn or CharacterController");
                yield break;
            }

            var originalPosition = motor.transform.position;
            var originalRotation = motor.transform.rotation;
            var checkpoint = respawn.CheckpointPosition;
            var rescueProbe = checkpoint - Vector3.up * (respawn.EmergencyFallMargin + 2f);
            controller.enabled = false;
            motor.transform.SetPositionAndRotation(rescueProbe, originalRotation);
            controller.enabled = true;
            motor.HardResetState();
            Physics.SyncTransforms();

            var rescued = false;
            for (var frame = 0; frame < 12; frame++)
            {
                yield return null;
                if (Vector3.Distance(motor.transform.position, checkpoint) <= 1.25f)
                {
                    rescued = true;
                    break;
                }
            }

            var checkpointDistance = Vector3.Distance(motor.transform.position, checkpoint);
            controller.enabled = false;
            motor.transform.SetPositionAndRotation(originalPosition, originalRotation);
            controller.enabled = true;
            motor.HardResetState();
            Physics.SyncTransforms();

            if (rescued)
            {
                ok($"World {WorldIndex:00} checkpoint-relative emergency rescue returned the out-of-bounds probe ({checkpointDistance:F2}m from checkpoint)");
            }
            else
            {
                fail($"World {WorldIndex:00} checkpoint-relative emergency rescue failed ({checkpointDistance:F2}m from checkpoint after 12 frames)");
            }
        }

        private IEnumerator VerifyGroundPoundChain(SplatMotor motor, SplatInputReader input,
            Vector3 safeStart, Quaternion safeRotation)
        {
            _groundPoundChainPassed = false;
            _groundPoundChainReport = "Ground-pound chain did not run";
            var feelPresenter = motor.GetComponent<SplatFeelPresenter>();
            var presenterLandCount = feelPresenter != null
                ? feelPresenter.LandedEventCountForDiagnostics
                : 0;
            var sfx = TempSfxPlayer.Instance;
            var jumpCueCount = sfx != null ? sfx.JumpCuePlayCountForDiagnostics : 0;
            var landCueCount = sfx != null ? sfx.LandCuePlayCountForDiagnostics : 0;
            var groundPoundCueCount = sfx != null ? sfx.GroundPoundCuePlayCountForDiagnostics : 0;
            input.DebugClear();

            var controller = motor.GetComponent<CharacterController>();
            controller.enabled = false;
            motor.transform.SetPositionAndRotation(safeStart, safeRotation);
            controller.enabled = true;
            motor.HardResetState();
            Physics.SyncTransforms();

            var settleFrames = 0;
            while (!motor.IsGrounded && settleFrames < 60)
            {
                settleFrames++;
                yield return null;
            }

            if (!motor.IsGrounded)
            {
                _groundPoundChainReport = "Ground-pound chain could not begin from walkable ground";
                yield break;
            }

            var runupStart = motor.transform.position;
            input.DebugSetMove(Vector2.up);
            for (var i = 0; i < 12; i++) yield return null;
            var runupSpeed = motor.PlanarVelocity.magnitude;
            var jumpStartY = motor.transform.position.y;
            var jumped = false;
            var poundLanded = false;
            var poundInputIssued = false;
            var poundLandingWasPounding = false;
            var poundLandingPlanarSpeed = 0f;
            void OnJump() => jumped = true;
            void OnLand()
            {
                if (poundInputIssued)
                {
                    poundLanded = true;
                    poundLandingWasPounding = motor.IsGroundPounding;
                    poundLandingPlanarSpeed = motor.PlanarVelocity.magnitude;
                }
            }

            motor.Jumped += OnJump;
            motor.Landed += OnLand;
            input.DebugPulseJump(held: true);
            yield return null;

            var elapsed = 0f;
            while ((motor.IsGrounded || motor.VerticalVelocity > 0.05f) && elapsed < 1.2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            var jumpApexGain = motor.transform.position.y - jumpStartY;
            var reachedAirborneApex = !motor.IsGrounded && elapsed < 1.2f;
            var planarSpeedBeforePound = motor.PlanarVelocity.magnitude;
            var poundStarted = false;
            var poundPlanarOnStart = 0f;
            if (reachedAirborneApex)
            {
                input.DebugSetMove(Vector2.down);
                poundInputIssued = true;
                input.DebugPulseJump(held: false);
                yield return null;
                poundStarted = motor.IsGroundPounding;
                poundPlanarOnStart = motor.PlanarVelocity.magnitude;
                input.DebugClear();
            }

            var poundWait = 0f;
            while (poundStarted && !poundLanded && poundWait < 1.5f)
            {
                poundWait += Time.deltaTime;
                yield return null;
            }

            input.DebugClear();
            motor.Jumped -= OnJump;
            motor.Landed -= OnLand;
            var runupDistance = Vector3.ProjectOnPlane(motor.transform.position - runupStart, Vector3.up).magnitude;
            var groundedAfterPound = motor.IsGrounded;
            var momentumRetention = planarSpeedBeforePound > 0.1f
                ? poundLandingPlanarSpeed / planarSpeedBeforePound
                : 0f;
            var jumpFeedback = sfx != null && sfx.AudioSourceReadyForDiagnostics
                && sfx.HasMovementCueClipsForDiagnostics
                && sfx.JumpCuePlayCountForDiagnostics > jumpCueCount;
            var landingFeedback = sfx != null && sfx.AudioSourceReadyForDiagnostics
                && sfx.HasMovementCueClipsForDiagnostics
                && sfx.LandCuePlayCountForDiagnostics > landCueCount;
            var groundPoundAudioFeedback = sfx != null
                && sfx.GroundPoundCuePlayCountForDiagnostics > groundPoundCueCount;
            var presenterFeedback = feelPresenter != null
                && feelPresenter.LandedEventCountForDiagnostics > presenterLandCount;
            var dustObject = feelPresenter != null
                ? feelPresenter.LastLandingDustForDiagnostics
                : null;
            var dustSystem = dustObject != null ? dustObject.GetComponent<ParticleSystem>() : null;
            var dustFeedback = dustSystem != null && dustSystem.particleCount > 0;
            var dustRing = dustObject != null
                ? dustObject.GetComponentInChildren<MeshRenderer>()
                : null;
            var dustRingMesh = dustRing != null ? dustRing.GetComponent<MeshFilter>() : null;
            var dustRingAnimator = dustRing != null
                ? dustRing.GetComponent<SpaceAndCrafts.Art.TempLandDustRingAnimator>()
                : null;
            var dustRingFeedback = dustRing != null && dustRing.enabled
                && dustRingMesh != null && dustRingMesh.sharedMesh != null
                && dustRingMesh.sharedMesh.vertexCount >= 32
                && dustRingAnimator != null && dustRingAnimator.DurationForDiagnostics >= 0.4f;
            _groundPoundChainPassed = runupSpeed >= 0.5f && jumped && reachedAirborneApex
                && jumpApexGain >= 1.5f && poundStarted && poundLanded && groundedAfterPound
                && poundLandingWasPounding && momentumRetention >= 0.3f
                && jumpFeedback && landingFeedback && groundPoundAudioFeedback
                && presenterFeedback && dustFeedback && dustRingFeedback;
            _groundPoundChainReport = $"Run-up→full jump→ground-pound: runup={runupSpeed:F2}m/s over {runupDistance:F2}m, jumped={jumped}, apexGain={jumpApexGain:F2}m, prePoundPlanar={planarSpeedBeforePound:F2}m/s, poundStartPlanar={poundPlanarOnStart:F2}m/s, poundLandingPlanar={poundLandingPlanarSpeed:F2}m/s, planarRetention={momentumRetention:P0}, poundStarted={poundStarted}, poundLanded={poundLanded}, poundingAtLanding={poundLandingWasPounding}, groundedAfter={groundedAfterPound}, jumpSfx={jumpFeedback}, landSfx={landingFeedback}, poundSfx={groundPoundAudioFeedback}, landingPresenter={presenterFeedback}, dustParticles={dustSystem?.particleCount ?? 0}, dustRing={dustRingFeedback}, dustRingProgress={dustRingAnimator?.ProgressForDiagnostics ?? -1f:F2}";
        }

        private IEnumerator VerifyWorld03MiteContact(SplatMotor motor, SplatInputReader input,
            Vector3 safeStart, Quaternion safeRotation, System.Action<string> fail,
            System.Action<string> ok)
        {
            var controller = motor.GetComponent<CharacterController>();
            var respawn = motor.GetComponent<PlayerRespawn>();
            var miteTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
            ScrapMite selectedMite = null;
            var padObject = FindByExactName("ART_TraversalPad_1");
            var selectedPad = padObject != null ? padObject.GetComponent<Collider>() : null;
            var nearestDistance = float.PositiveInfinity;
            if (selectedPad != null)
            {
                for (var miteIndex = 0; miteIndex < miteTransforms.Length; miteIndex++)
                {
                    var miteTransform = miteTransforms[miteIndex];
                    if (miteTransform == null || !miteTransform.name.StartsWith("ART_ScrapMite_")) continue;
                    var mite = miteTransform.GetComponent<ScrapMite>();
                    if (mite == null || mite.IsDefeated) continue;
                    var distance = Vector3.ProjectOnPlane(miteTransform.position - selectedPad.bounds.center,
                        Vector3.up).sqrMagnitude;
                    if (distance >= nearestDistance) continue;
                    nearestDistance = distance;
                    selectedMite = mite;
                }
            }

            if (controller == null || respawn == null || selectedMite == null || selectedPad == null)
            {
                fail("World 03 Mite contact simulation could not find the player respawn service, controller, pad 1, or a live Mite");
                yield break;
            }

            var originalMitePosition = selectedMite.transform.position;
            var originalMiteRotation = selectedMite.transform.rotation;

            input.DebugClear();
            controller.enabled = false;
            motor.transform.SetPositionAndRotation(safeStart, safeRotation);
            controller.enabled = true;
            motor.HardResetState();
            Physics.SyncTransforms();
            var waitForInvulnerability = 0;
            while (respawn.IsInvulnerable && waitForInvulnerability < 120)
            {
                waitForInvulnerability++;
                yield return null;
            }

            var padBounds = selectedPad.bounds;
            var footOffset = motor.transform.position.y - controller.bounds.min.y;
            var contactPosition = new Vector3(padBounds.center.x - 1.5f,
                padBounds.max.y + footOffset, padBounds.center.z);
            var stagedMitePosition = new Vector3(padBounds.center.x,
                originalMitePosition.y, padBounds.center.z);
            selectedMite.transform.SetPositionAndRotation(stagedMitePosition, originalMiteRotation);
            controller.enabled = false;
            motor.transform.SetPositionAndRotation(contactPosition, safeRotation);
            controller.enabled = true;
            motor.HardResetState();
            Physics.SyncTransforms();

            var knockbacksBefore = respawn.KnockbackEventCountForDiagnostics;
            var ouchCueBefore = TempSfxPlayer.Instance != null
                ? TempSfxPlayer.Instance.OuchCuePlayCountForDiagnostics : 0;
            for (var contactFrame = 0; contactFrame < 30
                && respawn.KnockbackEventCountForDiagnostics == knockbacksBefore; contactFrame++)
            {
                // Move through the trigger with the CharacterController so this
                // exercises the same physics callback path as normal gameplay.
                controller.Move(Vector3.right * 0.06f);
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
            }

            var contactCount = respawn.KnockbackEventCountForDiagnostics - knockbacksBefore;
            var appliedSpeed = respawn.LastKnockbackSpeedForDiagnostics;
            var launchPlanarSpeed = motor.PlanarVelocity.magnitude;
            var invulnerabilityApplied = respawn.IsInvulnerable;
            var ouchCuePlayed = TempSfxPlayer.Instance != null
                && TempSfxPlayer.Instance.OuchCuePlayCountForDiagnostics > ouchCueBefore;
            var hitFlinchVisible = selectedMite.ContactFlinchWeightForDiagnostics > 0.05f;
            var forceMatches = Mathf.Abs(appliedSpeed - selectedMite.ContactKnockbackSpeedForDiagnostics) < 0.01f;

            controller.enabled = false;
            motor.transform.SetPositionAndRotation(new Vector3(padBounds.center.x,
                padBounds.max.y + footOffset, padBounds.center.z), safeRotation);
            controller.enabled = true;
            motor.HardResetState();
            Physics.SyncTransforms();
            var recoveredOnPad = false;
            for (var recoveryFrame = 0; recoveryFrame < 45; recoveryFrame++)
            {
                yield return null;
                var planarRecoveryPosition = motor.transform.position;
                planarRecoveryPosition.y = padBounds.center.y;
                if (motor.IsGrounded && padBounds.Contains(planarRecoveryPosition))
                {
                    recoveredOnPad = true;
                    break;
                }
            }

            var passed = contactCount == 1 && forceMatches && appliedSpeed > 0f
                && launchPlanarSpeed > 0.5f && invulnerabilityApplied && ouchCuePlayed
                && hitFlinchVisible && recoveredOnPad;
            var report = $"World 03 {selectedMite.name} contact: eventCount={contactCount}, configuredForce={selectedMite.ContactKnockbackSpeedForDiagnostics:F1}m/s, appliedForce={appliedSpeed:F1}m/s, launchPlanar={launchPlanarSpeed:F2}m/s, invulnerability={invulnerabilityApplied}, ouchCue={ouchCuePlayed}, miteFlinch={hitFlinchVisible}, recoveredOnSafePad={recoveredOnPad}";

            selectedMite.transform.SetPositionAndRotation(originalMitePosition, originalMiteRotation);
            controller.enabled = false;
            motor.transform.SetPositionAndRotation(safeStart, safeRotation);
            controller.enabled = true;
            motor.HardResetState();
            Physics.SyncTransforms();
            if (passed) ok(report);
            else fail(report);
        }

        private IEnumerator VerifyWorld03PursuitCap(SplatMotor motor,
            System.Action<string> fail, System.Action<string> ok)
        {
            _world03PursuitCapPassed = false;
            var controller = motor != null ? motor.GetComponent<CharacterController>() : null;
            var pad = FindByExactName("ART_TraversalPad_1");
            var padCollider = pad != null ? pad.GetComponent<Collider>() : null;
            if (controller == null || padCollider == null)
            {
                fail("World 03 pursuit-cap probe is missing Splat's controller or traversal pad 1");
                yield break;
            }

            var originalPosition = motor.transform.position;
            var originalRotation = motor.transform.rotation;
            var padBounds = padCollider.bounds;
            var footOffset = originalPosition.y - controller.bounds.min.y;
            controller.enabled = false;
            motor.transform.SetPositionAndRotation(new Vector3(padBounds.center.x,
                padBounds.max.y + footOffset, padBounds.center.z), originalRotation);
            controller.enabled = true;
            motor.HardResetState();
            Physics.SyncTransforms();

            var peakAlerted = 0;
            var peakPursuers = 0;
            var mites = FindObjectsByType<ScrapMite>(FindObjectsInactive.Exclude);
            for (var frame = 0; frame < 60; frame++)
            {
                yield return null;
                var alerted = 0;
                var pursuers = 0;
                for (var miteIndex = 0; miteIndex < mites.Length; miteIndex++)
                {
                    var mite = mites[miteIndex];
                    if (mite == null || mite.IsDefeated) continue;
                    if (mite.AlertWeightForDiagnostics >= 0.5f) alerted++;
                    if (mite.IsPursuingPlayerForDiagnostics) pursuers++;
                }

                peakAlerted = Mathf.Max(peakAlerted, alerted);
                peakPursuers = Mathf.Max(peakPursuers, pursuers);
            }

            _world03PursuitCapPassed = peakPursuers == 1;
            controller.enabled = false;
            motor.transform.SetPositionAndRotation(originalPosition, originalRotation);
            controller.enabled = true;
            motor.HardResetState();
            Physics.SyncTransforms();

            var report = $"World 03 pursuit pacing: peakAlerted={peakAlerted}, peakPursuers={peakPursuers}, cap=1";
            if (_world03PursuitCapPassed) ok(report);
            else fail(report);
        }

        private IEnumerator VerifySpawnToTraversalPad(SplatMotor motor, SplatInputReader input,
            Vector3 spawnPosition, Quaternion spawnRotation)
        {
            _spawnToTraversalPadPassed = false;
            _spawnToTraversalPadReport = "Spawn-to-traversal-pad approach did not run";
            var sourcePad = FindByExactName("ART_TraversalPad_0");
            var sourceCollider = sourcePad != null ? sourcePad.GetComponent<Collider>() : null;
            if (sourceCollider == null)
            {
                _spawnToTraversalPadReport = "Spawn-to-traversal-pad approach is missing ART_TraversalPad_0 collider";
                yield break;
            }

            var controller = motor.GetComponent<CharacterController>();
            if (controller == null)
            {
                _spawnToTraversalPadReport = "Spawn-to-traversal-pad approach is missing CharacterController";
                yield break;
            }

            input.DebugClear();
            controller.enabled = false;
            motor.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            controller.enabled = true;
            motor.HardResetState();
            Physics.SyncTransforms();
            var groundedFrames = 0;
            while (!motor.IsGrounded && groundedFrames < 45)
            {
                groundedFrames++;
                yield return null;
            }

            if (!motor.IsGrounded)
            {
                _spawnToTraversalPadReport = "Splat did not settle at the authored World 03 spawn";
                yield break;
            }

            var sourceBounds = sourceCollider.bounds;
            var targetPoint = sourceBounds.center;
            var firstTarget = FindByExactName("ART_TraversalPad_1");
            var firstTargetCollider = firstTarget != null ? firstTarget.GetComponent<Collider>() : null;
            if (firstTargetCollider == null)
            {
                _spawnToTraversalPadReport = "Spawn route is missing ART_TraversalPad_1 collider";
                yield break;
            }

            var jumpDirection = Vector3.ProjectOnPlane(firstTargetCollider.bounds.center - sourceBounds.center,
                Vector3.up).normalized;
            var footOffset = motor.transform.position.y - controller.bounds.min.y;
            targetPoint -= jumpDirection * 0.35f;
            targetPoint.y = sourceBounds.max.y + footOffset;
            var camera = Camera.main;
            var startPosition = motor.transform.position;
            var previousPosition = startPosition;
            var traveled = 0f;
            var elapsed = 0f;
            var enteredSourcePad = false;
            var reachedLaunchPoint = false;
            while (elapsed < 4f)
            {
                var toLaunchPoint = Vector3.ProjectOnPlane(targetPoint - motor.transform.position, Vector3.up);
                var distance = toLaunchPoint.magnitude;
                enteredSourcePad = sourceBounds.Contains(new Vector3(motor.transform.position.x,
                    sourceBounds.center.y, motor.transform.position.z));
                if (enteredSourcePad && motor.IsGrounded && distance <= 0.6f)
                {
                    reachedLaunchPoint = true;
                    break;
                }

                var move = distance > 0.05f
                    ? WorldToMoveInput(toLaunchPoint.normalized, camera, motor.transform)
                    : Vector2.zero;
                if (distance < 1f) move *= Mathf.Clamp01(distance / 0.9f);
                input.DebugSetMove(move);
                yield return null;
                elapsed += Time.deltaTime;
                traveled += Vector3.ProjectOnPlane(motor.transform.position - previousPosition, Vector3.up).magnitude;
                previousPosition = motor.transform.position;
            }

            input.DebugClear();
            yield return null;
            var finalOnPad = sourceBounds.Contains(new Vector3(motor.transform.position.x,
                sourceBounds.center.y, motor.transform.position.z));
            var directDistance = Vector3.ProjectOnPlane(motor.transform.position - startPosition, Vector3.up).magnitude;
            var pathEfficiency = traveled > 0.01f ? directDistance / traveled : 0f;
            var recoveredAfterRelease = motor.IsGrounded;
            _spawnToTraversalPadPassed = reachedLaunchPoint && elapsed <= 1.5f && motor.IsGrounded
                && finalOnPad && directDistance > 1f && traveled <= directDistance * 1.5f + 0.25f
                && recoveredAfterRelease;
            _spawnToTraversalPadReport = $"Authored spawn→ART_TraversalPad_0 approach: start={startPosition:F2}, launch={motor.transform.position:F2}, traveled={traveled:F2}m, direct={directDistance:F2}m, pathEfficiency={pathEfficiency:P0}, elapsed={elapsed:F2}s, reachedLaunchPoint={reachedLaunchPoint}, grounded={motor.IsGrounded}, recoveredAfterRelease={recoveredAfterRelease}, landedOnPad={finalOnPad}";
        }

        private IEnumerator VerifyTraversalPadJump(SplatMotor motor, SplatInputReader input,
            int sourcePadIndex, int targetPadIndex, bool stageAtSource, int runupFrames,
            string sourceNameOverride = null, string targetNameOverride = null)
        {
            _traversalPadJumpPassed = false;
            _traversalPadJumpReport = "Traversal pad jump did not run";
            var respawn = motor.GetComponent<PlayerRespawn>();
            var knockbackEventsBeforeJump = respawn != null
                ? respawn.KnockbackEventCountForDiagnostics : 0;
            var sourceName = sourceNameOverride ?? $"ART_TraversalPad_{sourcePadIndex}";
            var targetName = targetNameOverride ?? $"ART_TraversalPad_{targetPadIndex}";
            var jumpLabel = $"{sourceName}→{targetName}";
            var sourcePad = FindByExactName(sourceName);
            var targetPad = FindByExactName(targetName);
            var sourceCollider = sourcePad != null ? sourcePad.GetComponent<Collider>() : null;
            var targetCollider = targetPad != null ? targetPad.GetComponent<Collider>() : null;
            if (sourceCollider == null || targetCollider == null)
            {
                _traversalPadJumpReport = $"Optional traversal jump {jumpLabel} is missing a source or target collider";
                yield break;
            }

            var sourceBounds = sourceCollider.bounds;
            var targetBounds = targetCollider.bounds;
            var planarDirection = Vector3.ProjectOnPlane(targetBounds.center - sourceBounds.center, Vector3.up).normalized;
            var centerGap = Vector3.ProjectOnPlane(targetBounds.center - sourceBounds.center, Vector3.up).magnitude;
            if (planarDirection.sqrMagnitude < 0.01f || centerGap < 1f)
            {
                _traversalPadJumpReport = $"Traversal pair {jumpLabel} is not a clear gap (centerGap={centerGap:F2}m)";
                yield break;
            }

            var enemyColliders = new List<Collider>();
            var enemyTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
            for (var enemyIndex = 0; enemyIndex < enemyTransforms.Length; enemyIndex++)
            {
                var enemy = enemyTransforms[enemyIndex];
                if (enemy == null || !enemy.name.StartsWith("ART_ScrapMite_")) continue;
                var enemyCollider = enemy.GetComponent<Collider>();
                if (enemyCollider == null) enemyCollider = enemy.GetComponentInChildren<Collider>();
                if (enemyCollider != null) enemyColliders.Add(enemyCollider);
            }

            input.DebugClear();
            var controller = motor.GetComponent<CharacterController>();
            var footOffset = motor.transform.position.y - controller.bounds.min.y;
            var launchPosition = sourceBounds.center - planarDirection * 0.35f;
            launchPosition.y = sourceBounds.max.y + footOffset;
            if (stageAtSource)
            {
                controller.enabled = false;
                motor.transform.SetPositionAndRotation(launchPosition, Quaternion.LookRotation(planarDirection, Vector3.up));
                controller.enabled = true;
                motor.HardResetState();
                Physics.SyncTransforms();
            }

            var groundedFrames = 0;
            while (!motor.IsGrounded && groundedFrames < 45)
            {
                groundedFrames++;
                yield return null;
            }

            var playerOnSource = sourceBounds.Contains(new Vector3(motor.transform.position.x, sourceBounds.center.y, motor.transform.position.z));
            if (!motor.IsGrounded || !playerOnSource)
            {
                input.DebugClear();
                _traversalPadJumpReport = $"Traversal route did not reach grounded source {sourceName} before the next jump";
                yield break;
            }

            if (!stageAtSource && runupFrames == 1)
            {
                var stabilized = false;
                for (var brakeFrame = 0; brakeFrame < 4; brakeFrame++)
                {
                    input.DebugSetMove(Vector2.zero);
                    yield return null;
                    var stillOnSource = sourceBounds.Contains(new Vector3(motor.transform.position.x,
                        sourceBounds.center.y, motor.transform.position.z));
                    if (!motor.IsGrounded || !stillOnSource)
                    {
                        input.DebugClear();
                        _traversalPadJumpReport = $"Traversal landing momentum carried Splat off {sourceName} during {brakeFrame + 1} brake frames";
                        yield break;
                    }

                    if (motor.PlanarVelocity.magnitude <= 0.5f)
                    {
                        stabilized = true;
                        break;
                    }
                }

                if (!stabilized)
                {
                    input.DebugClear();
                    _traversalPadJumpReport = $"Traversal momentum did not settle below 0.5m/s on {sourceName} within four brake frames";
                    yield break;
                }
            }

            var camera = Camera.main;
            var movement = WorldToMoveInput(planarDirection, camera, motor.transform);
            var runupStart = motor.transform.position;
            input.DebugSetMove(movement);
            for (var i = 0; i < runupFrames; i++) yield return null;
            var runupSpeed = motor.PlanarVelocity.magnitude;
            var launchY = motor.transform.position.y;
            var runupDistance = Vector3.ProjectOnPlane(motor.transform.position - runupStart, Vector3.up).magnitude;
            var jumpIssued = false;
            var landed = false;
            var landedOnTarget = false;
            var landingPosition = Vector3.zero;
            var airborneSamples = 0;
            var shadowSamples = 0;
            var cameraVisibleSamples = 0;
            var cameraLineOfSightSamples = 0;
            var cameraLineOfSightChecks = 0;
            var cameraOcclusionFadeSamples = 0;
            var viewportMinX = 1f;
            var viewportMaxX = 0f;
            var viewportMinY = 1f;
            var viewportMaxY = 0f;
            var nearestEnemyTriggerHorizontalGap = float.PositiveInfinity;
            var nearestEnemySolidColliderGap3D = float.PositiveInfinity;
            var enemyTriggerSamples = 0;
            var enemySolidColliderSamples = 0;
            var enemyThreatRangeSamples = 0;
            var enemyTelegraphSamples = 0;
            var threatCueByMite = new Dictionary<string, Vector2Int>();
            var cameraOccluderNames = new HashSet<string>();
            var maxHeight = launchY;
            var landingSafeBounds = targetBounds;
            landingSafeBounds.Expand(new Vector3(-0.5f, 0f, -0.5f));
            void OnJump() => jumpIssued = true;
            void OnLand()
            {
                if (!jumpIssued) return;
                landed = true;
                landingPosition = motor.transform.position;
                var planarLanding = landingPosition;
                planarLanding.y = targetBounds.center.y;
                landedOnTarget = landingSafeBounds.Contains(planarLanding);
            }

            motor.Jumped += OnJump;
            motor.Landed += OnLand;
            var jumpStart = motor.transform.position;
            input.DebugPulseJump(held: true);
            var elapsed = 0f;
            var releasedJump = false;
            while (!landed && elapsed < 1.8f)
            {
                elapsed += Time.deltaTime;
                var toTarget = Vector3.ProjectOnPlane(targetBounds.center - motor.transform.position, Vector3.up);
                var distanceToTarget = toTarget.magnitude;
                var desiredMove = distanceToTarget > 0.95f
                    ? WorldToMoveInput(toTarget.normalized, camera, motor.transform)
                    : Vector2.zero;
                input.DebugSetMove(desiredMove);
                if (!releasedJump && !motor.IsGrounded && motor.VerticalVelocity <= 0f)
                {
                    input.DebugPulseJump(held: false);
                    releasedJump = true;
                }

                maxHeight = Mathf.Max(maxHeight, motor.transform.position.y);
                if (!motor.IsGrounded)
                {
                    airborneSamples++;
                    for (var enemyIndex = 0; enemyIndex < enemyColliders.Count; enemyIndex++)
                    {
                        var enemyCollider = enemyColliders[enemyIndex];
                        if (enemyCollider == null) continue;
                        if (enemyCollider.isTrigger)
                        {
                            nearestEnemyTriggerHorizontalGap = Mathf.Min(nearestEnemyTriggerHorizontalGap,
                                HorizontalBoundsGap(controller.bounds, enemyCollider.bounds));
                            enemyTriggerSamples++;
                            var mite = enemyCollider.GetComponentInParent<ScrapMite>();
                            if (mite != null)
                            {
                                var playerToMite = Vector3.ProjectOnPlane(
                                    motor.transform.position - mite.transform.position, Vector3.up).magnitude;
                                if (playerToMite <= 3.2f)
                                {
                                    enemyThreatRangeSamples++;
                                    threatCueByMite.TryGetValue(mite.name, out var cueCounts);
                                    cueCounts.x++;
                                    if (mite.IsAttackTelegraphingForDiagnostics)
                                    {
                                        enemyTelegraphSamples++;
                                        cueCounts.y++;
                                    }

                                    threatCueByMite[mite.name] = cueCounts;
                                }
                            }
                        }
                        else
                        {
                            nearestEnemySolidColliderGap3D = Mathf.Min(nearestEnemySolidColliderGap3D,
                                BoundsGap3D(controller.bounds, enemyCollider.bounds));
                            enemySolidColliderSamples++;
                        }
                    }

                    if (motor.GetComponent<PlayerLandingShadow>()?.IsVisibleForDiagnostics == true)
                    {
                        shadowSamples++;
                    }

                    if (camera != null)
                    {
                        var focus = camera.WorldToViewportPoint(motor.transform.position + Vector3.up * 0.5f);
                        if (focus.z > 0f)
                        {
                            viewportMinX = Mathf.Min(viewportMinX, focus.x);
                            viewportMaxX = Mathf.Max(viewportMaxX, focus.x);
                            viewportMinY = Mathf.Min(viewportMinY, focus.y);
                            viewportMaxY = Mathf.Max(viewportMaxY, focus.y);
                            if (focus.x >= 0.05f && focus.x <= 0.95f
                                && focus.y >= 0.10f && focus.y <= 0.90f)
                            {
                                cameraVisibleSamples++;
                            }
                        }

                        for (var verticalSample = 0; verticalSample < 3; verticalSample++)
                        {
                            var targetHeight = 0.3f + verticalSample * 0.5f;
                            for (var horizontalSample = -1; horizontalSample <= 1; horizontalSample++)
                            {
                                var focusPoint = motor.transform.position + Vector3.up * targetHeight
                                    + motor.transform.right * (horizontalSample * 0.3f);
                                var sightVector = focusPoint - camera.transform.position;
                                var hasOcclusion = Physics.Raycast(camera.transform.position, sightVector.normalized,
                                    out var sightHit, sightVector.magnitude, ~0, QueryTriggerInteraction.Ignore);
                                cameraLineOfSightChecks++;
                                var occluderRenderer = hasOcclusion
                                    ? sightHit.collider.GetComponent<Renderer>()
                                    : null;
                                var hiddenByCamera = hasOcclusion
                                    && sightHit.collider.gameObject.layer == 31
                                    && (occluderRenderer != null ? occluderRenderer : sightHit.collider.GetComponentInParent<Renderer>())?.forceRenderingOff == true;
                                if (!hasOcclusion || sightHit.collider.GetComponentInParent<SplatMotor>() == motor
                                    || hiddenByCamera)
                                {
                                    cameraLineOfSightSamples++;
                                    if (hiddenByCamera) cameraOcclusionFadeSamples++;
                                }
                                else if (cameraOccluderNames.Count < 8)
                                {
                                    cameraOccluderNames.Add($"{sightHit.collider.name}[L{sightHit.collider.gameObject.layer}]");
                                }
                            }
                        }
                    }
                }

                yield return null;
            }

            input.DebugClear();
            motor.Jumped -= OnJump;
            motor.Landed -= OnLand;
            var recoveredAfterRelease = false;
            var remainedOnTargetAfterRelease = false;
            if (landed)
            {
                yield return null;
                var releasedPosition = motor.transform.position;
                var releasedPlanarPosition = releasedPosition;
                releasedPlanarPosition.y = targetBounds.center.y;
                remainedOnTargetAfterRelease = landingSafeBounds.Contains(releasedPlanarPosition);
                recoveredAfterRelease = motor.IsGrounded && remainedOnTargetAfterRelease;
            }

            var jumpGain = maxHeight - launchY;
            var groundedAfterLanding = landed && motor.IsGrounded;
            var minShadowSamples = Mathf.Max(1, airborneSamples / 2);
            var minViewportSamples = Mathf.Max(1, Mathf.CeilToInt(airborneSamples * 0.9f));
            var inputJumpDistance = Vector3.ProjectOnPlane(landingPosition - jumpStart, Vector3.up).magnitude;
            var landingOffset = Vector3.ProjectOnPlane(landingPosition - targetBounds.center, Vector3.up).magnitude;
            var subjectVisible = cameraVisibleSamples >= minViewportSamples;
            var cameraSightlineReadable = cameraLineOfSightSamples >= Mathf.CeilToInt(cameraLineOfSightChecks * 0.85f);
            var adequateRunup = runupFrames == 0 || runupSpeed > 0.5f;
            var threatCueReadable = enemyThreatRangeSamples == 0
                || enemyTelegraphSamples >= Mathf.CeilToInt(enemyThreatRangeSamples * 0.75f);
            var knockbackEventsDuringJump = respawn != null
                ? respawn.KnockbackEventCountForDiagnostics - knockbackEventsBeforeJump : 0;
            var lastKnockbackSpeed = respawn != null
                ? respawn.LastKnockbackSpeedForDiagnostics : 0f;
            var cueBreakdown = new StringBuilder();
            foreach (var entry in threatCueByMite)
            {
                if (cueBreakdown.Length > 0) cueBreakdown.Append(';');
                cueBreakdown.Append(entry.Key).Append(':')
                    .Append(entry.Value.y).Append('/').Append(entry.Value.x);
            }
            _traversalPadJumpPassed = adequateRunup && jumpIssued && jumpGain >= 1.5f
                && landed && landedOnTarget && groundedAfterLanding && recoveredAfterRelease
                && subjectVisible && cameraSightlineReadable && threatCueReadable
                && shadowSamples >= minShadowSamples;
            _traversalPadJumpReport = $"{jumpLabel} jump: centerGap={centerGap:F2}m, heightDelta={targetBounds.max.y - sourceBounds.max.y:F2}m, runup={runupSpeed:F2}m/s over {runupDistance:F2}m, jumpGain={jumpGain:F2}m, flight={elapsed:F2}s, landingTravel={inputJumpDistance:F2}m, landingOffset={landingOffset:F2}m, landedOnTarget={landedOnTarget}, grounded={groundedAfterLanding}, recoveredAfterRelease={recoveredAfterRelease}, remainedOnTargetAfterRelease={remainedOnTargetAfterRelease}, knockbacks={knockbackEventsDuringJump}, lastKnockbackSpeed={lastKnockbackSpeed:F1}m/s, nearestEnemyTriggerHorizontalGap={nearestEnemyTriggerHorizontalGap:F2}m/{enemyTriggerSamples} samples, nearestEnemySolidColliderGap3D={nearestEnemySolidColliderGap3D:F2}m/{enemySolidColliderSamples} samples, enemyThreatCue={enemyTelegraphSamples}/{enemyThreatRangeSamples} in-range samples (need 75%), cueByMite={cueBreakdown}, threatCueReadable={threatCueReadable}, shadow={shadowSamples}/{airborneSamples}, cameraVisibleInset={cameraVisibleSamples}/{airborneSamples} (need {minViewportSamples}), viewportX={viewportMinX:F2}..{viewportMaxX:F2}, viewportY={viewportMinY:F2}..{viewportMaxY:F2}, lineOfSight={cameraLineOfSightSamples}/{cameraLineOfSightChecks}, occlusionFadeSamples={cameraOcclusionFadeSamples}, occluders={string.Join(",", cameraOccluderNames)}";
        }

        private IEnumerator VerifyWorld03OptionalWonderSpur(SplatMotor motor, SplatInputReader input,
            System.Action<string> fail, System.Action<string> ok)
        {
            _world03OptionalSpurPassed = false;
            var beaconObject = FindByExactName("ART_Wonder_w3_0");
            var beacon = beaconObject != null ? beaconObject.GetComponent<SecretWonderBeacon>() : null;
            if (beacon == null)
            {
                fail("World 03 optional spur has no ART_Wonder_w3_0 SecretWonderBeacon");
                yield break;
            }

            var registry = SecretWonderRegistry.Ensure();
            var wonderId = beacon.WonderId;
            if (registry.HasClaimed(wonderId))
            {
                fail($"World 03 optional spur reward was already claimed before route probe: {wonderId}");
                yield break;
            }

            yield return VerifyTraversalPadJump(motor, input, 2, 90, stageAtSource: true,
                runupFrames: 6, targetNameOverride: "ART_SecretSpur");
            if (!_traversalPadJumpPassed)
            {
                fail(_traversalPadJumpReport);
                yield break;
            }
            ok(_traversalPadJumpReport);

            for (var frame = 0; frame < 90 && !registry.HasClaimed(wonderId); frame++)
            {
                yield return null;
            }
            if (!registry.HasClaimed(wonderId))
            {
                fail($"Landed on ART_SecretSpur but did not collect wonder {wonderId}");
                yield break;
            }
            ok($"Optional World 03 spur reward claimed naturally: {wonderId}");

            yield return VerifyTraversalPadJump(motor, input, 90, 2, stageAtSource: true,
                runupFrames: 6, sourceNameOverride: "ART_SecretSpur");
            if (!_traversalPadJumpPassed)
            {
                fail(_traversalPadJumpReport);
                yield break;
            }
            ok(_traversalPadJumpReport);
            _world03OptionalSpurPassed = true;
        }

        private IEnumerator WaitForWorld03ThreatTellWindow(SplatMotor motor, SplatInputReader input,
            System.Action<string> ok)
        {
            const int maximumWaitFrames = 120;
            const int minimumReactionFrames = 15;
            for (var frame = 0; frame < maximumWaitFrames; frame++)
            {
                input.DebugSetMove(Vector2.zero);
                var mites = FindObjectsByType<ScrapMite>(FindObjectsInactive.Exclude);
                var inRange = 0;
                var telegraphing = 0;
                for (var i = 0; i < mites.Length; i++)
                {
                    var mite = mites[i];
                    if (mite == null) continue;
                    var offset = Vector3.ProjectOnPlane(mite.transform.position - motor.transform.position,
                        Vector3.up);
                    if (offset.magnitude > 3.2f) continue;
                    inRange++;
                    if (mite.IsAttackTelegraphingForDiagnostics) telegraphing++;
                }

                if (frame + 1 >= minimumReactionFrames && inRange >= 2 && telegraphing >= inRange)
                {
                    ok($"World 03 pad 2 threat tell window: {telegraphing}/{inRange} nearby Mites telegraphing after {frame + 1} reaction frames");
                    yield break;
                }
                yield return null;
            }
            input.DebugSetMove(Vector2.zero);
            ok($"World 03 pad 2 threat tell window: bounded {maximumWaitFrames}-frame reaction pause; no full nearby Mite tell window was observed before pad 2→3 jump");
        }

        private static Vector2 WorldToMoveInput(Vector3 worldDirection, Camera camera, Transform fallback)
        {
            var cameraBasis = camera != null && camera.transform.parent != null
                ? camera.transform.parent
                : camera != null ? camera.transform : fallback;
            var forward = cameraBasis.forward;
            var right = cameraBasis.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            return Vector2.ClampMagnitude(new Vector2(Vector3.Dot(worldDirection, right),
                Vector3.Dot(worldDirection, forward)), 1f);
        }

        private bool HasBuildSiteOrInvention()
        {
            if (FindByExactName("ART_BuildSite") != null || FindByExactName("ART_VineBuildSite") != null)
            {
                return true;
            }

            if (FindAnyObjectByType<BuildSite>(FindObjectsInactive.Include) != null)
            {
                return true;
            }

            var hintIndex = WorldIndex - 1;
            if (hintIndex < 0 || hintIndex >= InventionNameHints.Length)
            {
                return false;
            }

            var hints = InventionNameHints[hintIndex];
            for (var h = 0; h < hints.Length; h++)
            {
                if (FindByExactName(hints[h]) != null || FindByNameContains(hints[h]) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static float ProjectedViewportCoverage(Camera camera, Bounds bounds)
        {
            if (camera == null) return 0f;
            var minX = 1f;
            var minY = 1f;
            var maxX = 0f;
            var maxY = 0f;
            var anyInFront = false;
            for (var corner = 0; corner < 8; corner++)
            {
                var world = bounds.center + new Vector3(
                    (corner & 1) == 0 ? -bounds.extents.x : bounds.extents.x,
                    (corner & 2) == 0 ? -bounds.extents.y : bounds.extents.y,
                    (corner & 4) == 0 ? -bounds.extents.z : bounds.extents.z);
                var viewport = camera.WorldToViewportPoint(world);
                if (viewport.z <= camera.nearClipPlane) continue;
                anyInFront = true;
                minX = Mathf.Min(minX, viewport.x);
                minY = Mathf.Min(minY, viewport.y);
                maxX = Mathf.Max(maxX, viewport.x);
                maxY = Mathf.Max(maxY, viewport.y);
            }

            if (!anyInFront) return 0f;
            var width = Mathf.Max(0f, Mathf.Min(1f, maxX) - Mathf.Max(0f, minX));
            var height = Mathf.Max(0f, Mathf.Min(1f, maxY) - Mathf.Max(0f, minY));
            return width * height;
        }

        private static float HorizontalBoundsGap(Bounds a, Bounds b)
        {
            var gapX = Mathf.Max(0f, Mathf.Abs(a.center.x - b.center.x) - a.extents.x - b.extents.x);
            var gapZ = Mathf.Max(0f, Mathf.Abs(a.center.z - b.center.z) - a.extents.z - b.extents.z);
            return Mathf.Sqrt(gapX * gapX + gapZ * gapZ);
        }

        private static float BoundsGap3D(Bounds a, Bounds b)
        {
            var gapX = Mathf.Max(0f, Mathf.Abs(a.center.x - b.center.x) - a.extents.x - b.extents.x);
            var gapY = Mathf.Max(0f, Mathf.Abs(a.center.y - b.center.y) - a.extents.y - b.extents.y);
            var gapZ = Mathf.Max(0f, Mathf.Abs(a.center.z - b.center.z) - a.extents.z - b.extents.z);
            return Mathf.Sqrt(gapX * gapX + gapY * gapY + gapZ * gapZ);
        }

        private static GameObject FindByExactName(string name)
        {
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == name)
                {
                    return transforms[i].gameObject;
                }
            }

            return null;
        }

        private static GameObject FindByNameContains(string fragment)
        {
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name.Contains(fragment))
                {
                    return transforms[i].gameObject;
                }
            }

            return null;
        }

        private static SplatMotorSettings GetMotorSettings(SplatMotor motor)
        {
            var field = typeof(SplatMotor).GetField(
                "settings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(motor) as SplatMotorSettings;
        }

        private static T GetPrivateField<T>(object instance, string fieldName) where T : class
        {
            if (instance == null) return null;
            var field = instance.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(instance) as T;
        }

        private static bool TryCameraObstructionCheck(CameraSettings sourceSettings,
            CameraCollision collision, out string report)
        {
            var originalSettings = GetPrivateField<CameraSettings>(collision, "settings");
            var cameraTransform = GetPrivateField<Transform>(collision, "cameraTransform");
            var pivot = cameraTransform != null ? cameraTransform.parent : null;
            if (sourceSettings == null || originalSettings == null || pivot == null)
            {
                report = "Camera obstruction check missing settings, camera transform, or pivot";
                return false;
            }

            var isolatedSettings = Instantiate(sourceSettings);
            isolatedSettings.CollisionMask = 1 << 31;
            SetPrivateField(collision, "settings", isolatedSettings);
            GameObject blocker = null;
            try
            {
                var unobstructedDistance = collision.ResolveDistance(sourceSettings.Distance);
                blocker = new GameObject("WorldSmoke_CameraOccluder") { layer = 31 };
                blocker.transform.rotation = Quaternion.LookRotation(-pivot.forward, pivot.up);
                var origin = pivot.position + pivot.rotation * (Vector3.up * sourceSettings.Height);
                blocker.transform.position = origin - pivot.forward * (sourceSettings.Distance * 0.35f);
                var box = blocker.AddComponent<BoxCollider>();
                box.size = new Vector3(2f, 2f, 0.35f);
                Physics.SyncTransforms();
                var obstructedDistance = collision.ResolveDistance(sourceSettings.Distance);
                var pulledIn = obstructedDistance < sourceSettings.Distance - 0.5f;
                var clearBaseline = unobstructedDistance >= sourceSettings.Distance - 0.05f;
                report = $"Camera obstruction response: clear={unobstructedDistance:F2}m, blocked={obstructedDistance:F2}m, desired={sourceSettings.Distance:F2}m";
                return pulledIn && clearBaseline;
            }
            finally
            {
                SetPrivateField(collision, "settings", originalSettings);
                if (blocker != null) Destroy(blocker);
                Destroy(isolatedSettings);
            }
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            if (instance == null) return;
            var field = instance.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(instance, value);
        }

        private void Finish(bool pass, string report)
        {
            LastRunPassed = pass;
            LastReport = report;
            var label = $"World {WorldIndex:00}";
            if (pass)
            {
                Debug.Log($"[WorldSmoke] PASS {label}\n{report}");
            }
            else
            {
                Debug.LogError($"[WorldSmoke] FAIL {label}\n{report}");
            }

            WorldsPlayModeSmokeBridge.NotifyFinished(pass, WorldIndex, report);
            Destroy(gameObject);
        }
    }

    /// <summary>Captures short gameplay frame and camera-marker samples for the World 03 smoke route.</summary>
    public sealed class WorldSmokeCameraProfilerRecorder : MonoBehaviour
    {
        private const int SampleCapacity = 1024;
        private const string CollisionMarkerName = "SpaceAndCrafts.Camera.CollisionCast";
        private const string FadeMarkerName = "SpaceAndCrafts.Camera.OcclusionFade";
        private readonly float[] _frameMilliseconds = new float[SampleCapacity];
        private readonly float[] _collisionMilliseconds = new float[SampleCapacity];
        private readonly float[] _fadeMilliseconds = new float[SampleCapacity];
        private readonly float[] _cpuMainMilliseconds = new float[SampleCapacity];
        private readonly float[] _cpuRenderMilliseconds = new float[SampleCapacity];
        private readonly float[] _gpuMilliseconds = new float[SampleCapacity];
        private readonly float[] _presentWaitMilliseconds = new float[SampleCapacity];
        private readonly FrameTiming[] _latestFrameTiming = new FrameTiming[1];
        private ProfilerRecorder _collisionRecorder;
        private ProfilerRecorder _fadeRecorder;
        private float _startTime;
        private bool _recording;
        private int _frameCount;
        private int _collisionCount;
        private int _fadeCount;
        private int _frameTimingCount;

        public void Begin()
        {
            _collisionRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, CollisionMarkerName, SampleCapacity);
            _fadeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, FadeMarkerName, SampleCapacity);
            _startTime = Time.realtimeSinceStartup;
            _recording = true;
        }

        private void Update()
        {
            if (!_recording || Time.realtimeSinceStartup - _startTime < 0.3f)
                return;

            if (_frameCount < SampleCapacity)
                _frameMilliseconds[_frameCount++] = Time.unscaledDeltaTime * 1000f;

            FrameTimingManager.CaptureFrameTimings();
            var timingCount = FrameTimingManager.GetLatestTimings(1, _latestFrameTiming);
            if (timingCount > 0 && _frameTimingCount < SampleCapacity)
            {
                var timing = _latestFrameTiming[0];
                _cpuMainMilliseconds[_frameTimingCount] = (float)timing.cpuMainThreadFrameTime;
                _cpuRenderMilliseconds[_frameTimingCount] = (float)timing.cpuRenderThreadFrameTime;
                _gpuMilliseconds[_frameTimingCount] = (float)timing.gpuFrameTime;
                _presentWaitMilliseconds[_frameTimingCount] = (float)timing.cpuMainThreadPresentWaitTime;
                _frameTimingCount++;
            }

            var collisionNanoseconds = _collisionRecorder.LastValue;
            if (collisionNanoseconds > 0 && _collisionCount < SampleCapacity)
                _collisionMilliseconds[_collisionCount++] = collisionNanoseconds / 1_000_000f;

            var fadeNanoseconds = _fadeRecorder.LastValue;
            if (fadeNanoseconds > 0 && _fadeCount < SampleCapacity)
                _fadeMilliseconds[_fadeCount++] = fadeNanoseconds / 1_000_000f;
        }

        public string FinishReport()
        {
            _recording = false;
            _collisionRecorder.Dispose();
            _fadeRecorder.Dispose();
            var cameraCollision = FindAnyObjectByType<CameraCollision>();
            var collisionCastDiagnostics = cameraCollision != null
                ? $"collisionBufferPeak={cameraCollision.PeakHitCountForDiagnostics}/{cameraCollision.HitBufferCapacityForDiagnostics}, saturatedCasts={cameraCollision.BufferSaturationCountForDiagnostics}"
                : "collision diagnostics unavailable";
            return "World 03 gameplay profiling (batch Editor, after 0.30 s warmup): " +
                $"frameMs[{Summarize(_frameMilliseconds, _frameCount)}], " +
                $"frameTimingCpuMainMs[{Summarize(_cpuMainMilliseconds, _frameTimingCount)}], " +
                $"frameTimingCpuRenderMs[{Summarize(_cpuRenderMilliseconds, _frameTimingCount)}], " +
                $"frameTimingGpuMs[{Summarize(_gpuMilliseconds, _frameTimingCount)}], " +
                $"frameTimingPresentWaitMs[{Summarize(_presentWaitMilliseconds, _frameTimingCount)}], " +
                $"cameraCollisionMs[{Summarize(_collisionMilliseconds, _collisionCount)}], " +
                $"occlusionFadeMs[{Summarize(_fadeMilliseconds, _fadeCount)}], {collisionCastDiagnostics}";
        }

        private static string Summarize(float[] samples, int count)
        {
            if (count <= 0)
                return "no samples";
            System.Array.Sort(samples, 0, count);
            var p95Index = Mathf.Clamp(Mathf.CeilToInt(count * 0.95f) - 1, 0, count - 1);
            return $"n={count}, median={samples[(count - 1) / 2]:F3}, p95={samples[p95Index]:F3}, max={samples[count - 1]:F3}";
        }
    }

    /// <summary>Editor bridge hooks without putting Editor code in this assembly.</summary>
    public static class WorldsPlayModeSmokeBridge
    {
        public static System.Action<bool, int, string> Finished;

        public static void NotifyFinished(bool pass, int worldIndex, string report) =>
            Finished?.Invoke(pass, worldIndex, report);
    }
}
