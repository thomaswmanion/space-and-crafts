using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpaceAndCrafts.AI;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEngine;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.DebugTools
{
    /// <summary>
    /// Runtime Arrival critical-path smoke (not a feel playtest).
    /// Driven by editor menu / batchmode EnterPlaymode.
    /// </summary>
    public sealed class ArrivalPlayModeSmokeRunner : MonoBehaviour
    {
        private sealed class AggregateProfilerCapture
        {
            public string Name;
            public ProfilerRecorder Recorder;
            public readonly float[] SamplesMs = new float[512];
            public int SampleCount;
        }

        private sealed class RenderCounterCapture
        {
            public string Name;
            public ProfilerRecorder Recorder;
            public readonly long[] Samples = new long[512];
            public int SampleCount;
        }

        private sealed class VisibleMeshComplexity
        {
            public string Name;
            public string HierarchyPath;
            public string RendererType;
            public string ShadowMode;
            public long Triangles;
        }


        private const int FrameTimeSampleCapacity = 8192;
        public static bool LastRunPassed { get; private set; }
        public static string LastReport { get; private set; } = string.Empty;
        private readonly float[] _frameTimeSamplesMs = new float[FrameTimeSampleCapacity];
        private int _frameTimeSampleCount;
        private readonly float[] _focusedFrameSamplesMs = new float[512];
        private readonly float[] _motorMarkerSamplesMs = new float[512];
        private readonly float[] _shadowMarkerSamplesMs = new float[512];
        private readonly float[] _mainThreadSamplesMs = new float[512];
        private readonly float[] _cameraMarkerSamplesMs = new float[512];
        private readonly float[] _animatorUpdateSamplesMs = new float[512];
        private readonly float[] _animatorLateUpdateSamplesMs = new float[512];
        private readonly float[] _cpuMainFrameSamplesMs = new float[512];
        private readonly float[] _cpuRenderFrameSamplesMs = new float[512];
        private readonly float[] _gpuFrameSamplesMs = new float[512];
        private readonly float[] _presentWaitSamplesMs = new float[512];
        private readonly float[] _gcAllocatedBytesSamples = new float[512];
        private readonly int[] _gcGenerationZeroSamples = new int[512];
        private readonly FrameTiming[] _latestFrameTiming = new FrameTiming[1];
        private readonly List<AggregateProfilerCapture> _aggregateProfilerCaptures = new();
        private readonly List<RenderCounterCapture> _renderCounterCaptures = new();
        private int _focusedFrameSampleCount;
        private int _motorMarkerSampleCount;
        private int _shadowMarkerSampleCount;
        private int _mainThreadSampleCount;
        private int _frameTimingSampleCount;
        private ProfilerRecorder _motorMarkerRecorder;
        private ProfilerRecorder _shadowMarkerRecorder;
        private ProfilerRecorder _mainThreadRecorder;
        private ProfilerRecorder _cameraMarkerRecorder;
        private ProfilerRecorder _animatorUpdateRecorder;
        private ProfilerRecorder _animatorLateUpdateRecorder;
        private ProfilerRecorder _gcAllocatedRecorder;
        private bool _captureFocusedFrameSamples;
        private string _focusedFramePhase = string.Empty;
        private readonly System.Text.StringBuilder _focusedFrameReports = new();

        private void Update()
        {
            if (_frameTimeSampleCount < _frameTimeSamplesMs.Length)
            {
                _frameTimeSamplesMs[_frameTimeSampleCount++] = Time.unscaledDeltaTime * 1000f;
            }

            if (_captureFocusedFrameSamples && _focusedFrameSampleCount < _focusedFrameSamplesMs.Length)
            {
                var sampleIndex = _focusedFrameSampleCount++;
                _focusedFrameSamplesMs[sampleIndex] = Time.unscaledDeltaTime * 1000f;
                _motorMarkerSamplesMs[sampleIndex] = _motorMarkerRecorder.Valid
                    ? _motorMarkerRecorder.LastValue / 1000000f : 0f;
                _shadowMarkerSamplesMs[sampleIndex] = _shadowMarkerRecorder.Valid
                    ? _shadowMarkerRecorder.LastValue / 1000000f : 0f;
                _mainThreadSamplesMs[sampleIndex] = _mainThreadRecorder.Valid
                    ? _mainThreadRecorder.LastValue / 1000000f : 0f;
                _gcAllocatedBytesSamples[sampleIndex] = _gcAllocatedRecorder.Valid
                    ? _gcAllocatedRecorder.LastValue : 0f;
                _gcGenerationZeroSamples[sampleIndex] = System.GC.CollectionCount(0);
                _cameraMarkerSamplesMs[sampleIndex] = _cameraMarkerRecorder.Valid
                    ? _cameraMarkerRecorder.LastValue / 1000000f : 0f;
                _animatorUpdateSamplesMs[sampleIndex] = _animatorUpdateRecorder.Valid
                    ? _animatorUpdateRecorder.LastValue / 1000000f : 0f;
                _animatorLateUpdateSamplesMs[sampleIndex] = _animatorLateUpdateRecorder.Valid
                    ? _animatorLateUpdateRecorder.LastValue / 1000000f : 0f;
                if (_motorMarkerRecorder.Valid) _motorMarkerSampleCount++;
                if (_shadowMarkerRecorder.Valid) _shadowMarkerSampleCount++;
                if (_mainThreadRecorder.Valid) _mainThreadSampleCount++;
                for (var markerIndex = 0; markerIndex < _aggregateProfilerCaptures.Count; markerIndex++)
                {
                    var capture = _aggregateProfilerCaptures[markerIndex];
                    if (capture.Recorder.Valid && capture.SampleCount < capture.SamplesMs.Length)
                    {
                        capture.SamplesMs[capture.SampleCount++] = capture.Recorder.LastValue / 1000000f;
                    }
                }
                for (var counterIndex = 0; counterIndex < _renderCounterCaptures.Count; counterIndex++)
                {
                    var capture = _renderCounterCaptures[counterIndex];
                    if (capture.Recorder.Valid && capture.SampleCount < capture.Samples.Length)
                    {
                        capture.Samples[capture.SampleCount++] = capture.Recorder.LastValue;
                    }
                }
                FrameTimingManager.CaptureFrameTimings();
                var timingCount = FrameTimingManager.GetLatestTimings(1, _latestFrameTiming);
                if (timingCount > 0 && _frameTimingSampleCount < _cpuMainFrameSamplesMs.Length)
                {
                    var timingIndex = _frameTimingSampleCount++;
                    var timing = _latestFrameTiming[0];
                    _cpuMainFrameSamplesMs[timingIndex] = (float)timing.cpuMainThreadFrameTime;
                    _cpuRenderFrameSamplesMs[timingIndex] = (float)timing.cpuRenderThreadFrameTime;
                    _gpuFrameSamplesMs[timingIndex] = (float)timing.gpuFrameTime;
                    _presentWaitSamplesMs[timingIndex] = (float)timing.cpuMainThreadPresentWaitTime;
                }
            }
        }

        private void Start()
        {
            // Keep characterization stable in batchmode (large hitch dt was crushing jump apex).
            Time.maximumDeltaTime = 0.033f;
            Application.targetFrameRate = 60;
            InitializeAggregateProfilerCaptures();
            LogVisibleMeshComplexity();
            StartCoroutine(RunSmoke());
        }

        private void LogVisibleMeshComplexity()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[ArrivalSmoke] Visible mesh complexity unavailable: MainCamera missing");
                return;
            }

            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            var visibleMeshes = new List<VisibleMeshComplexity>(renderers.Length);
            long totalTriangles = 0;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled
                    || (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0
                    || !GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                {
                    continue;
                }

                var meshFilter = renderer.GetComponent<MeshFilter>();
                var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
                if (mesh == null && renderer is SkinnedMeshRenderer skinned)
                    mesh = skinned.sharedMesh;
                if (mesh == null) continue;

                long triangleCount = 0;
                for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    triangleCount += mesh.GetIndexCount(submesh) / 3;
                if (triangleCount <= 0) continue;
                totalTriangles += triangleCount;
                visibleMeshes.Add(new VisibleMeshComplexity
                {
                    Name = renderer.name,
                    HierarchyPath = GetHierarchyPath(renderer.transform),
                    RendererType = renderer.GetType().Name,
                    ShadowMode = renderer.shadowCastingMode.ToString(),
                    Triangles = triangleCount
                });
            }

            visibleMeshes.Sort((a, b) => b.Triangles.CompareTo(a.Triangles));
            var topCount = Mathf.Min(12, visibleMeshes.Count);
            var topMeshes = new List<string>(topCount);
            for (var i = 0; i < topCount; i++)
                topMeshes.Add($"{visibleMeshes[i].HierarchyPath}[{visibleMeshes[i].RendererType},shadows={visibleMeshes[i].ShadowMode}]:{visibleMeshes[i].Triangles}");
            Debug.Log($"[ArrivalSmoke] Camera-frustum mesh complexity: renderers={visibleMeshes.Count}, estimatedTriangles={totalTriangles}, top={string.Join(";", topMeshes)}");
        }

        private static string GetHierarchyPath(Transform node)
        {
            if (node == null) return string.Empty;
            var path = node.name;
            while (node.parent != null)
            {
                node = node.parent;
                path = node.name + "/" + path;
            }
            return path;
        }

        private void InitializeAggregateProfilerCaptures()
        {
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            var candidateNames = new HashSet<string>
            {
                "Update.ScriptRunBehaviourUpdate",
                "Update.ScriptRunDelayedTasks",
                "PreLateUpdate.ScriptRunBehaviourLateUpdate",
                "Camera.Render",
                "RenderLoop.Draw",
                "Gfx.WaitForRenderThread",
                "Gfx.WaitForPresentOnGfxThread",
                "GC.Collect",
                "GarbageCollector.CollectIncremental",
                "GC.Alloc",
                "ScrapMite.Update",
                "ScrapStormSentinel.Update",
                "EnemyCombatDirector.Update",
                "RainbowSpiderAnimatorDriver.Update",
                "SplatInteractor.Update",
                "InteractPromptHud.Update",
                "ArrivalObjectiveCoach.Update",
                "WorldMusicDirector.Update",
                "MeshyPropLife.Update",
                "PickupMotion.Update"
            };
            for (var i = 0; i < handles.Count; i++)
            {
                var handle = handles[i];
                if (!handle.Valid) continue;
                var description = ProfilerRecorderHandle.GetDescription(handle);
                if (!candidateNames.Contains(description.Name)) continue;
                var recorder = ProfilerRecorder.StartNew(description.Category, description.Name, 128);
                _aggregateProfilerCaptures.Add(new AggregateProfilerCapture
                {
                    Name = $"{description.Category.Name}:{description.Name}",
                    Recorder = recorder
                });
            }

            var names = new List<string>(_aggregateProfilerCaptures.Count);
            for (var i = 0; i < _aggregateProfilerCaptures.Count; i++)
                names.Add(_aggregateProfilerCaptures[i].Name);
            Debug.Log($"[ArrivalSmoke] Aggregate profiler markers available: {(names.Count > 0 ? string.Join(", ", names) : "none")}");

            var renderCounterNames = new[]
            {
                "Draw Calls Count", "SetPass Calls Count", "Triangles Count", "Vertices Count"
            };
            for (var i = 0; i < renderCounterNames.Length; i++)
            {
                var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Render,
                    renderCounterNames[i], 128);
                if (recorder.Valid)
                {
                    _renderCounterCaptures.Add(new RenderCounterCapture
                    {
                        Name = renderCounterNames[i],
                        Recorder = recorder
                    });
                }
                else
                {
                    recorder.Dispose();
                }
            }
            var counterNames = new List<string>(_renderCounterCaptures.Count);
            for (var i = 0; i < _renderCounterCaptures.Count; i++)
                counterNames.Add(_renderCounterCaptures[i].Name);
            Debug.Log($"[ArrivalSmoke] Render counters available: {(counterNames.Count > 0 ? string.Join(", ", counterNames) : "none")}");
        }

        private IEnumerator RunSmoke()
        {
            LastRunPassed = false;
            var report = new System.Text.StringBuilder();
            var pass = true;

            void Fail(string msg)
            {
                pass = false;
                report.AppendLine("FAIL: " + msg);
                Debug.LogError("[ArrivalSmoke] " + msg);
            }

            void Ok(string msg)
            {
                report.AppendLine("OK: " + msg);
                Debug.Log("[ArrivalSmoke] " + msg);
            }

            yield return null;
            yield return new WaitForSeconds(0.35f);

            var motor = FindAnyObjectByType<SplatMotor>();
            var inventory = motor != null ? motor.GetComponent<InventoryComponent>() : null;
            var respawn = motor != null ? motor.GetComponent<PlayerRespawn>() : null;
            var buildSite = FindAnyObjectByType<BuildSite>();
            var session = FindAnyObjectByType<LevelSession>();
            var boss = FindAnyObjectByType<ScrapStormSentinel>();
            var cameraRig = FindAnyObjectByType<PlatformingCameraRig>();
            var fall = FindAnyObjectByType<FallVolume>();
            var skeletonAnimator = FindAnyObjectByType<SplatSkeletonAnimator>();
            var splatInteractor = motor != null ? motor.GetComponent<SplatInteractor>() : null;
            var meshyAnimator = motor != null ? SplatMeshyAnimatorDriver.FindForPlayer(motor.transform) : null;
            var activeMeshyVisual = motor != null && SplatMeshyVisualMode.IsEnabledOn(motor.transform)
                && meshyAnimator != null && meshyAnimator.enabled;

            var resolvedMeshyVisual = motor != null ? SplatMeshyVisualMode.ResolveVisual(motor.transform) : null;
            Debug.Log("[ArrivalSmoke] Splat rig selection: player=" + (motor != null ? motor.name : "missing")
                + ", modeEnabled=" + (motor != null && SplatMeshyVisualMode.IsEnabledOn(motor.transform))
                + ", resolvedVisual=" + (resolvedMeshyVisual != null ? resolvedMeshyVisual.name : "missing")
                + ", meshDriver=" + (meshyAnimator != null ? meshyAnimator.name : "missing")
                + ", meshDriverEnabled=" + (meshyAnimator != null && meshyAnimator.enabled)
                + ", meshController=" + (meshyAnimator != null ? meshyAnimator.RuntimeControllerNameForDiagnostics : "missing")
                + ", legacyAnimator=" + (skeletonAnimator != null ? skeletonAnimator.name : "missing")
                + ", legacyEnabled=" + (skeletonAnimator != null && skeletonAnimator.enabled));

            if (motor == null || inventory == null || respawn == null || buildSite == null || session == null || boss == null || fall == null || (skeletonAnimator == null && meshyAnimator == null) || splatInteractor == null)
            {
                Fail("Missing critical Arrival components for smoke.");
                Finish(pass, report.ToString());
                yield break;
            }

            Ok("Critical components present");

            var startupCamera = Camera.main;
            if (cameraRig == null || startupCamera == null)
            {
                Fail("Arrival startup follow camera is missing");
            }
            else
            {
                var cameraArmDistance = cameraRig.CameraArmDistanceForDiagnostics;
                var playerFocusViewport = startupCamera.WorldToViewportPoint(
                    motor.transform.position + Vector3.up * 0.5f);
                Debug.Log($"[ArrivalSmoke] Boot framing diagnostics: player={motor.transform.position:F2}, grounded={motor.IsGrounded}, orbitDistanceLocal={cameraRig.OrbitDistanceForDiagnostics:F2}, cameraArmWorld={cameraArmDistance:F2}, playerFocusViewport={playerFocusViewport:F2}, camera={startupCamera.transform.position:F2}/{startupCamera.transform.eulerAngles:F1}.");
                if (!motor.IsGrounded)
                {
                    Fail($"Splat is not grounded at the Arrival startup pose (position={motor.transform.position:F2})");
                }
                else
                {
                    Ok("Splat is grounded at the Arrival startup pose");
                }

                if (cameraArmDistance < 4f)
                {
                    Fail($"Arrival startup camera is compressed against geometry (arm={cameraArmDistance:F2} world units)");
                }
                else
                {
                    Ok($"Arrival startup camera retains a readable follow distance ({cameraArmDistance:F2} world units)");
                }

                if (playerFocusViewport.z <= 0f || playerFocusViewport.y < 0.35f || playerFocusViewport.y > 0.65f)
                {
                    Fail($"Arrival startup camera aim pushes Splat's body center away from the readable frame (viewport={playerFocusViewport:F2})");
                }
                else
                {
                    Ok($"Arrival startup camera aims Splat's body center in the readable frame (viewport Y={playerFocusViewport.y:F2})");
                }
            }

            var before = motor.transform.position;
            respawn.SetCheckpoint(before, motor.transform.rotation);
            Teleport(motor, before + Vector3.up * 12f);
            yield return new WaitForSeconds(0.1f);
            respawn.RespawnAtCheckpoint();
            yield return null;
            if ((motor.transform.position - before).sqrMagnitude > 4f)
            {
                Fail($"Respawn did not return near checkpoint (pos={motor.transform.position})");
            }
            else
            {
                Ok("PlayerRespawn returns to checkpoint");
            }

            // Fall volume → checkpoint respawn.
            var fallCol = fall.GetComponent<Collider>();
            if (fallCol != null)
            {
                var fallTriggerCount = 0;
                var lastFallWasFjord = false;
                void OnFallRescued(bool landedInFjord)
                {
                    fallTriggerCount++;
                    lastFallWasFjord = landedInFjord;
                }

                fall.PlayerRescued += OnFallRescued;
                Teleport(motor, new Vector3(
                    fallCol.bounds.center.x,
                    fallCol.bounds.max.y + 2f,
                    fallCol.bounds.center.z));
                Physics.SyncTransforms();
                var fallWait = 0f;
                while (fallTriggerCount == 0 && fallWait < 1.5f)
                {
                    fallWait += Time.deltaTime;
                    yield return null;
                }

                var fallAtCheckpoint = (motor.transform.position - before).sqrMagnitude <= 9f;
                var splashAlive = FindAnyObjectByType<ArrivalFjordSplashEffect>() != null;
                fall.PlayerRescued -= OnFallRescued;
                if (fallTriggerCount == 0)
                {
                    Fail("Player did not enter the FallVolume trigger from above");
                }
                else if (!fallAtCheckpoint)
                {
                    Fail($"FallVolume did not respawn near checkpoint (pos={motor.transform.position})");
                }
                else if (!lastFallWasFjord || !splashAlive)
                {
                    Fail($"Fjord fall rescue did not create a splash (overFjord={lastFallWasFjord}, effectAlive={splashAlive})");
                }
                else
                {
                    Ok("Falling through the fjord catch triggers checkpoint rescue and creates splash effect");
                }

                var fallbackStart = before + Vector3.right * (ArrivalFjordBackdrop.FjordCatchSpan + 80f)
                                    - Vector3.up * (respawn.EmergencyFallMargin + 1f);
                Teleport(motor, fallbackStart);
                Physics.SyncTransforms();
                yield return null;
                if ((motor.transform.position - before).sqrMagnitude > 9f)
                {
                    Fail($"Emergency rescue did not return a fall outside the fjord to checkpoint (pos={motor.transform.position})");
                }
                else if (fallTriggerCount != 1)
                {
                    Fail("Outside-fjord emergency rescue unexpectedly entered the scenic FallVolume");
                }
                else
                {
                    Ok("Checkpoint-relative emergency rescue catches a fall outside the fjord trigger");
                }
            }
            else
            {
                Fail("FallVolume missing collider");
            }

            // Motor characterization (not “feels excellent” — proves jump/move respond near SO targets).
            var inputReader = motor.GetComponent<SplatInputReader>();
            var settings = GetMotorSettings(motor);
            Keyboard reviewKeyboard = null;
            var reviewKeyboardWasAdded = false;
            var syntheticKeyboardInputUnavailable = false;
            void ReleaseReviewKeyboard()
            {
                if (reviewKeyboardWasAdded && reviewKeyboard != null)
                {
                    InputSystem.RemoveDevice(reviewKeyboard);
                    reviewKeyboard = null;
                    reviewKeyboardWasAdded = false;
                }
            }

            if (inputReader != null && settings != null)
            {
                if (settings.CoyoteTime >= 0.12f && settings.CoyoteTime <= 0.15f)
                {
                    Ok($"Coyote time is within the feel target ({settings.CoyoteTime:F2}s)");
                }
                else
                {
                    Fail($"Coyote time is outside the 0.12–0.15s feel target ({settings.CoyoteTime:F2}s)");
                }

                if (settings.JumpBufferTime >= 0.15f && settings.JumpBufferTime <= 0.16f)
                {
                    Ok($"Jump input buffer meets the 0.15s feel target ({settings.JumpBufferTime:F2}s)");
                }
                else
                {
                    Fail($"Jump input buffer is below the 0.15s feel target ({settings.JumpBufferTime:F2}s)");
                }

                // Isolate jump/camera SO characterization from Meshy showcase launch/knock storms.
                var mutedBoings = MuteMeshyShowcaseLaunchers();
                Teleport(motor, before + Vector3.up * 0.05f);
                motor.HardResetState();
                Physics.SyncTransforms();
                var groundWait = 0f;
                while (!motor.IsGrounded && groundWait < 1f)
                {
                    groundWait += Time.deltaTime;
                    yield return null;
                }

                // Keep the input safely inside the authored 0.12–0.15 s grace
                // window; the off-edge detection itself already consumes a frame.
                yield return new WaitForSeconds(0.03f);
                // Exercise the production InputAction bindings with held keyboard
                // states before the debug-assisted critical-path smoke. This is
                // repeatable runtime input evidence, not a human-feel playtest.
                reviewKeyboard = Keyboard.current;
                if (reviewKeyboard == null)
                {
                    reviewKeyboard = InputSystem.AddDevice<Keyboard>();
                    reviewKeyboardWasAdded = reviewKeyboard != null;
                }

                if (reviewKeyboard == null)
                {
                    Fail("Unity Input System could not provide a keyboard for the Arrival input-path review");
                }
                else
                {
                    Debug.Log($"[ArrivalSmoke] InputAction keyboard device: name={reviewKeyboard.displayName}, synthesizedForReview={reviewKeyboardWasAdded}.");
                    inputReader.DebugClear();
                    var keyboardMoveStart = motor.transform.position;
                    var springBeforeKeyboardMove = inventory.Has("component_spring");
                    var keyboardMoveElapsed = 0f;
                    var observedForwardInput = 0f;
                    var cameraVisibleMoveSamples = 0;
                    StartFocusedFrameTiming("production W movement");
                    InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState(Key.W));
                    while (keyboardMoveElapsed < 0.75f)
                    {
                        keyboardMoveElapsed += Time.deltaTime;
                        observedForwardInput = Mathf.Max(observedForwardInput, inputReader.Move.y);
                        if (startupCamera != null)
                        {
                            var moveFocus = startupCamera.WorldToViewportPoint(
                                motor.transform.position + Vector3.up * 0.5f);
                            if (moveFocus.z > 0f && moveFocus.x >= 0.05f && moveFocus.x <= 0.95f
                                && moveFocus.y >= 0.15f && moveFocus.y <= 0.85f)
                            {
                                cameraVisibleMoveSamples++;
                            }
                        }

                        yield return null;
                    }

                    InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
                    yield return new WaitForSeconds(0.12f);
                    StopFocusedFrameTiming();
                    var keyboardMoveDelta = motor.transform.position - keyboardMoveStart;
                    var keyboardMoveDistance = new Vector2(keyboardMoveDelta.x, keyboardMoveDelta.z).magnitude;
                    var springAfterKeyboardMove = inventory.Has("component_spring");

                    Debug.Log($"[ArrivalSmoke] Real InputAction W hold: duration={keyboardMoveElapsed:F2}s, observedMoveY={observedForwardInput:F2}, horizontalDistance={keyboardMoveDistance:F2}m, cameraVisibleSamples={cameraVisibleMoveSamples}, springOwned={springAfterKeyboardMove}.");
                    if (reviewKeyboardWasAdded && observedForwardInput < 0.9f)
                    {
                        syntheticKeyboardInputUnavailable = true;
                        Debug.LogWarning("[ArrivalSmoke] Skipping the production W assertion because the Batch Mode synthetic keyboard did not deliver the action; this run provides no keyboard movement evidence.");
                    }
                    else if (observedForwardInput < 0.9f)
                    {
                        Fail($"Keyboard W did not reach the production Move InputAction (observed Y={observedForwardInput:F2})");
                    }
                    else if (keyboardMoveDistance < 2.5f)
                    {
                        Fail($"Production W input moved too little in 0.75 seconds ({keyboardMoveDistance:F2}m)");
                    }
                    else if (cameraVisibleMoveSamples < 20)
                    {
                        Fail($"Production camera did not keep Splat in frame during W input ({cameraVisibleMoveSamples} visible samples)");
                    }
                    else if (springAfterKeyboardMove != springBeforeKeyboardMove)
                    {
                        Fail("The W-input review unexpectedly changed spring inventory");
                    }
                    else
                    {
                        Ok($"Production W InputAction moved Splat {keyboardMoveDistance:F2}m in {keyboardMoveElapsed:F2}s with camera focus visible for {cameraVisibleMoveSamples} samples");
                    }

                    // Return to the authored start so the existing spring-route
                    // and pickup interaction checks remain independent.
                    Teleport(motor, before + Vector3.up * 0.05f);
                    motor.HardResetState();
                    Physics.SyncTransforms();
                    var groundedAfterKeyboardMove = 0f;
                    while (!motor.IsGrounded && groundedAfterKeyboardMove < 1f)
                    {
                        groundedAfterKeyboardMove += Time.deltaTime;
                        yield return null;
                    }

                    yield return new WaitForSeconds(0.2f);

                    var jumpStartY = motor.transform.position.y;
                    var jumpApexY = jumpStartY;
                    var jumpEventCount = 0;
                    var landingEventCount = 0;
                    var jumpAirSamples = 0;
                    var landingShadow = motor.GetComponent<PlayerLandingShadow>();
                    var jumpShadowVisibleSamples = 0;
                    var jumpShadowViewportVisibleSamples = 0;
                    var jumpShadowViewportMinY = 1f;
                    var jumpShadowViewportMaxY = 0f;
                    var maximumShadowHorizontalError = 0f;
                    var jumpCameraVisibleSamples = 0;
                    var inputActionJumpViewportMinY = 1f;
                    var inputActionJumpViewportMaxY = 0f;
                    void OnKeyboardJumped() => jumpEventCount++;
                    void OnKeyboardLanded() => landingEventCount++;
                    motor.Jumped += OnKeyboardJumped;
                    motor.Landed += OnKeyboardLanded;
                    StartFocusedFrameTiming("production Space jump");
                    InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState(Key.Space));
                    yield return new WaitForSeconds(0.12f);
                    InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
                    var jumpReviewElapsed = 0f;
                    while (jumpReviewElapsed < 1.4f && landingEventCount == 0)
                    {
                        jumpReviewElapsed += Time.deltaTime;
                        jumpApexY = Mathf.Max(jumpApexY, motor.transform.position.y);
                        if (!motor.IsGrounded)
                        {
                            jumpAirSamples++;
                            if (landingShadow != null && landingShadow.IsVisibleForDiagnostics)
                            {
                                jumpShadowVisibleSamples++;
                                var shadowOffset = landingShadow.GroundPointForDiagnostics - motor.transform.position;
                                maximumShadowHorizontalError = Mathf.Max(maximumShadowHorizontalError,
                                    new Vector2(shadowOffset.x, shadowOffset.z).magnitude);
                                if (startupCamera != null)
                                {
                                    var shadowViewport = startupCamera.WorldToViewportPoint(
                                        landingShadow.GroundPointForDiagnostics);
                                    if (shadowViewport.z > 0f && shadowViewport.x >= 0f
                                        && shadowViewport.x <= 1f && shadowViewport.y >= 0f
                                        && shadowViewport.y <= 1f)
                                    {
                                        jumpShadowViewportVisibleSamples++;
                                        jumpShadowViewportMinY = Mathf.Min(jumpShadowViewportMinY,
                                            shadowViewport.y);
                                        jumpShadowViewportMaxY = Mathf.Max(jumpShadowViewportMaxY,
                                            shadowViewport.y);
                                    }
                                }
                            }
                        }

                        if (startupCamera != null)
                        {
                            var jumpFocus = startupCamera.WorldToViewportPoint(
                                motor.transform.position + Vector3.up * 0.5f);
                            if (jumpFocus.z > 0f && jumpFocus.x >= 0.05f && jumpFocus.x <= 0.95f
                                && jumpFocus.y >= 0.15f && jumpFocus.y <= 0.85f)
                            {
                                jumpCameraVisibleSamples++;
                                inputActionJumpViewportMinY = Mathf.Min(inputActionJumpViewportMinY, jumpFocus.y);
                                inputActionJumpViewportMaxY = Mathf.Max(inputActionJumpViewportMaxY, jumpFocus.y);
                            }
                        }

                        yield return null;
                    }

                    StopFocusedFrameTiming();
                    motor.Jumped -= OnKeyboardJumped;
                    motor.Landed -= OnKeyboardLanded;
                    var inputActionJumpGain = jumpApexY - jumpStartY;
                    Debug.Log($"[ArrivalSmoke] Real InputAction Space press: jumpEvents={jumpEventCount}, landingEvents={landingEventCount}, jumpGain={inputActionJumpGain:F2}m, airSamples={jumpAirSamples}, cameraVisibleSamples={jumpCameraVisibleSamples}, shadowVisibleSamples={jumpShadowVisibleSamples}, shadowViewportVisibleSamples={jumpShadowViewportVisibleSamples}, shadowViewportY={jumpShadowViewportMinY:F2}..{jumpShadowViewportMaxY:F2}, maxShadowXZError={maximumShadowHorizontalError:F2}m, viewportY={inputActionJumpViewportMinY:F2}..{inputActionJumpViewportMaxY:F2}.");
                    if (reviewKeyboardWasAdded && jumpEventCount == 0)
                    {
                        syntheticKeyboardInputUnavailable = true;
                        Debug.LogWarning("[ArrivalSmoke] Skipping the production Space assertion because the Batch Mode synthetic keyboard did not deliver the action; this run provides no keyboard jump evidence.");
                    }
                    else if (jumpEventCount == 0)
                    {
                        Fail("Keyboard Space did not trigger the production Jump InputAction");
                    }
                    else if (inputActionJumpGain < 1f || jumpAirSamples < 3)
                    {
                        Fail($"Production Space input produced an incomplete jump arc (gain={inputActionJumpGain:F2}m, airSamples={jumpAirSamples})");
                    }
                    else if (landingEventCount == 0)
                    {
                        Fail("Production Space input did not return Splat to a landing within 1.4 seconds");
                    }
                    else if (jumpAirSamples < 10 || jumpCameraVisibleSamples
                        < Mathf.Max(10, Mathf.CeilToInt(jumpAirSamples * 0.85f)))
                    {
                        Fail($"Production camera lost Splat during the keyboard jump ({jumpCameraVisibleSamples}/{jumpAirSamples} visible air samples; minimum={Mathf.Max(10, Mathf.CeilToInt(jumpAirSamples * 0.85f))})");
                    }
                    else if (landingShadow == null || jumpShadowVisibleSamples < Mathf.Max(5, jumpAirSamples / 2)
                        || maximumShadowHorizontalError > 0.15f)
                    {
                        Fail($"Projected landing shadow did not track under Splat during the keyboard jump (shadow={landingShadow != null}, visible={jumpShadowVisibleSamples}/{jumpAirSamples}, maxXZError={maximumShadowHorizontalError:F2}m)");
                    }
                    else if (jumpShadowViewportVisibleSamples < Mathf.Max(5,
                        Mathf.CeilToInt(jumpShadowVisibleSamples * 0.7f)))
                    {
                        Fail($"Production camera lost the projected landing shadow during the keyboard jump (viewport={jumpShadowViewportVisibleSamples}/{jumpShadowVisibleSamples}, yRange={jumpShadowViewportMinY:F2}..{jumpShadowViewportMaxY:F2})");
                    }
                    else
                    {
                        Ok($"Production Space InputAction reached {inputActionJumpGain:F2}m and landed after {jumpReviewElapsed:F2}s; camera focus remained visible across {jumpCameraVisibleSamples} samples and the landing shadow stayed beneath Splat for {jumpShadowVisibleSamples} samples ({jumpShadowViewportVisibleSamples} in frame)");
                    }

                    Teleport(motor, before + Vector3.up * 0.05f);
                    motor.HardResetState();
                    Physics.SyncTransforms();
                    var groundedAfterKeyboardJump = 0f;
                    while (!motor.IsGrounded && groundedAfterKeyboardJump < 1f)
                    {
                        groundedAfterKeyboardJump += Time.deltaTime;
                        yield return null;
                    }

                    yield return new WaitForSeconds(0.2f);
                }

                // Exercise a continuous spawn-to-pickup route with the real motor.
                // The spring is authored on the first raised fjord pad about 18 m
                // ahead of the Arrival start; this deliberately avoids teleporting
                // to the component so camera follow and pickup contact run together.
                const string springId = "component_spring";
                var hadSpringBeforeRoute = inventory.Has(springId);
                var routeStart = motor.transform.position;
                var springPickup = FindObjectsByType<CollectiblePickup>()
                    .FirstOrDefault(pickup => pickup != null && pickup.Definition != null
                        && pickup.Definition.Id == springId && pickup.gameObject.activeInHierarchy
                        && pickup.CanInteract);
                var routeElapsed = 0f;
                var nextRouteDiagnostic = 1f;
                var routeCamera = Camera.main;
                if (!hadSpringBeforeRoute && springPickup == null)
                {
                    Fail("The authored spring pickup is missing from the active Arrival scene");
                }
                else if (!hadSpringBeforeRoute && routeCamera == null)
                {
                    Fail("Cannot drive the spawn-to-spring route without the Arrival camera");
                }
                else if (!hadSpringBeforeRoute)
                {
                    while (routeElapsed < 5f)
                    {
                        if (inventory.Has(springId))
                        {
                            break;
                        }

                        var toSpring = springPickup.transform.position - motor.transform.position;
                        toSpring.y = 0f;
                        var distanceToSpring = toSpring.magnitude;
                        if (distanceToSpring <= 1.8f)
                        {
                            break;
                        }

                        var cameraForward = routeCamera.transform.forward;
                        cameraForward.y = 0f;
                        cameraForward.Normalize();
                        var cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;
                        var towardSpring = toSpring / distanceToSpring;
                        var routeInput = new Vector2(
                            Vector3.Dot(towardSpring, cameraRight),
                            Vector3.Dot(towardSpring, cameraForward));
                        routeInput *= Mathf.Min(1f, distanceToSpring / 5f);
                        inputReader.DebugSetMove(routeInput);
                        routeElapsed += Time.deltaTime;
                        if (routeElapsed >= nextRouteDiagnostic)
                        {
                            var routeForward = routeCamera.transform.forward;
                            routeForward.y = 0f;
                            routeForward.Normalize();
                            Debug.Log($"[ArrivalSmoke] Route steering at {routeElapsed:F1}s: player={motor.transform.position:F2}, target={springPickup.transform.position:F2}, input={routeInput:F2}, planarSpeed={motor.PlanarVelocity.magnitude:F2}, grounded={motor.IsGrounded}, cameraYaw={routeCamera.transform.eulerAngles.y:F1}, blockers={DescribeForwardObstructions(motor, routeForward, 1.5f)}.");
                            nextRouteDiagnostic += 1f;
                        }
                        yield return null;
                    }
                }
                else
                {
                    while (routeElapsed < 5f)
                    {
                        var toSpring = springPickup.transform.position - motor.transform.position;
                        toSpring.y = 0f;
                        var distanceToSpring = toSpring.magnitude;
                        if (distanceToSpring <= 1.8f)
                        {
                            break;
                        }

                        var cameraForward = routeCamera.transform.forward;
                        cameraForward.y = 0f;
                        cameraForward.Normalize();
                        var cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;
                        var towardSpring = toSpring / distanceToSpring;
                        var routeInput = new Vector2(
                            Vector3.Dot(towardSpring, cameraRight),
                            Vector3.Dot(towardSpring, cameraForward));
                        routeInput *= Mathf.Min(1f, distanceToSpring / 5f);
                        inputReader.DebugSetMove(routeInput);
                        routeElapsed += Time.deltaTime;
                        yield return null;
                    }
                }

                inputReader.DebugClear();
                yield return new WaitForSeconds(0.2f);
                var routeDelta = motor.transform.position - routeStart;
                var routeDistance = new Vector2(routeDelta.x, routeDelta.z).magnitude;
                var collectedSpringOnRoute = !hadSpringBeforeRoute && inventory.Has(springId);
                var springFocusReached = springPickup != null
                    && splatInteractor.CurrentFocus == springPickup;
                var routeViewport = startupCamera != null
                    ? startupCamera.WorldToViewportPoint(motor.transform.position + Vector3.up * 0.5f)
                    : Vector3.zero;
                var springDistanceAtRouteEnd = springPickup != null
                    ? Vector3.Distance(motor.transform.position, springPickup.transform.position)
                    : float.PositiveInfinity;
                Debug.Log($"[ArrivalSmoke] Spawn-to-spring route: start={routeStart:F2}, end={motor.transform.position:F2}, horizontalDistance={routeDistance:F2}, spring={ (springPickup != null ? springPickup.transform.position.ToString("F2") : "missing")}, springDistance={springDistanceAtRouteEnd:F2}, elapsed={routeElapsed:F2}, springOwnedBefore={hadSpringBeforeRoute}, springOwnedAfter={inventory.Has(springId)}, interactorFocusReached={springFocusReached}, focus={routeViewport:F2}.");
                if (routeDistance < 8f)
                {
                    var routeForward = routeCamera != null ? routeCamera.transform.forward : Vector3.forward;
                    routeForward.y = 0f;
                    routeForward.Normalize();
                    Debug.Log($"[ArrivalSmoke] Route obstruction probe: forward={routeForward:F2}, hits={DescribeForwardObstructions(motor, routeForward, 3f)}.");

                    Fail($"Continuous Arrival start route moved too little to reach the spring interaction range (distance={routeDistance:0.00}m)");
                }
                else if (!hadSpringBeforeRoute && !collectedSpringOnRoute && !springFocusReached)
                {
                    Fail($"Continuous Arrival start route moved {routeDistance:0.00}m but did not reach spring interaction focus (position={motor.transform.position:F2}, spring={(springPickup != null ? springPickup.transform.position.ToString("F2") : "missing")}, focus={splatInteractor.CurrentFocus})");
                }
                else if (!hadSpringBeforeRoute && !collectedSpringOnRoute && springFocusReached)
                {
                    Ok($"Continuous Arrival start route traveled {routeDistance:0.00}m and reached spring interaction focus without auto-collecting it");
                }
                else if (hadSpringBeforeRoute)
                {
                    Ok($"Continuous Arrival start route traveled {routeDistance:0.00}m to the first component pad; spring was already owned");
                }
                else
                {
                    Ok($"Continuous Arrival start route traveled {routeDistance:0.00}m and collected the spring pickup");
                }

                var startY = motor.transform.position.y;
                inputReader.DebugPulseJump(held: true);
                var apex = startY;
                var jumpCamera = Camera.main;
                var jumpViewportMinY = 1f;
                var jumpViewportMaxY = 0f;
                var jumpViewportSampleCount = 0;
                var apexViewport = Vector3.zero;
                var jumpWait = 0f;
                while (jumpWait < 1.2f)
                {
                    jumpWait += Time.deltaTime;
                    if (motor.transform.position.y >= apex)
                    {
                        apex = motor.transform.position.y;
                        if (jumpCamera != null)
                        {
                            apexViewport = jumpCamera.WorldToViewportPoint(
                                motor.transform.position + Vector3.up * 0.5f);
                        }
                    }

                    yield return null;

                    // Sample the rendered camera throughout takeoff, apex, and landing,
                    // not just at startup. This uses the same motor and production rig.
                    if (jumpCamera != null)
                    {
                        var viewport = jumpCamera.WorldToViewportPoint(
                            motor.transform.position + Vector3.up * 0.5f);
                        if (viewport.z > 0f)
                        {
                            jumpViewportMinY = Mathf.Min(jumpViewportMinY, viewport.y);
                            jumpViewportMaxY = Mathf.Max(jumpViewportMaxY, viewport.y);
                            jumpViewportSampleCount++;
                        }
                    }
                }

                inputReader.DebugClear();
                var jumpGain = apex - startY;
                var expectedJump = settings.JumpHeight;
                Debug.Log($"[ArrivalSmoke] Jump camera framing: apexViewport={apexViewport:F2}, focusViewportYRange=({jumpViewportMinY:F2}, {jumpViewportMaxY:F2}), visibleSamples={jumpViewportSampleCount}.");
                // After grounded-lockout fix, expect near SO JumpHeight (Euler integration ~slightly under).
                if (jumpGain < expectedJump * 0.85f || jumpGain > expectedJump * 1.25f)
                {
                    Fail($"Jump height out of range (gain={jumpGain:0.00}, expected~{expectedJump:0.00})");
                }
                else
                {
                    Ok($"Jump characterization gain={jumpGain:0.00} (SO JumpHeight={expectedJump:0.00})");
                }

                if (jumpCamera == null || jumpViewportSampleCount < 10
                    || apexViewport.z <= 0f || apexViewport.y < 0.20f || apexViewport.y > 0.80f
                    || jumpViewportMinY < 0.15f || jumpViewportMaxY > 0.85f)
                {
                    Fail($"Arrival camera loses Splat during jump (apex={apexViewport:F2}, focusYRange=({jumpViewportMinY:F2}, {jumpViewportMaxY:F2}), samples={jumpViewportSampleCount})");
                }
                else
                {
                    Ok($"Arrival camera keeps Splat visible through jump arc (apex viewport Y={apexViewport.y:F2}, range={jumpViewportMinY:F2}–{jumpViewportMaxY:F2})");
                }

                Teleport(motor, before + Vector3.up * 0.1f);
                yield return new WaitForSeconds(0.2f);
                inputReader.DebugSetMove(new Vector2(0f, 1f));
                var moveWait = 0f;
                var peakSpeed = 0f;
                while (moveWait < 0.85f)
                {
                    moveWait += Time.deltaTime;
                    peakSpeed = Mathf.Max(peakSpeed, motor.PlanarVelocity.magnitude);
                    yield return null;
                }

                inputReader.DebugClear();
                if (peakSpeed < settings.MaxSpeed * 0.55f)
                {
                    Fail($"Move speed too low (peak={peakSpeed:0.00}, MaxSpeed={settings.MaxSpeed:0.00})");
                }
                else
                {
                    Ok($"Move characterization peakSpeed={peakSpeed:0.00} (SO MaxSpeed={settings.MaxSpeed:0.00})");
                }

                // Camera follow + look orbit (not “trustworthy feel” — proves rig responds).
                var camRig = FindAnyObjectByType<PlatformingCameraRig>();
                var gameCam = UnityEngine.Camera.main;
                if (camRig == null || gameCam == null)
                {
                    Fail("Missing PlatformingCameraRig or Main Camera for characterization");
                }
                else
                {
                    Teleport(motor, before + new Vector3(6f, 0.1f, 4f));
                    var followWait = 0f;
                    while (followWait < 0.9f)
                    {
                        followWait += Time.deltaTime;
                        yield return null;
                    }

                    var followErr = Vector3.Distance(
                        new Vector3(camRig.transform.position.x, 0f, camRig.transform.position.z),
                        new Vector3(motor.transform.position.x, 0f, motor.transform.position.z));
                    if (followErr > 2.5f)
                    {
                        Fail($"Camera follow lag too high (planar err={followErr:0.00})");
                    }
                    else
                    {
                        Ok($"Camera follow planar err={followErr:0.00}");
                    }

                    var yawBefore = gameCam.transform.eulerAngles.y;
                    inputReader.DebugSetLook(new Vector2(2.5f, 0f));
                    var lookWait = 0f;
                    while (lookWait < 0.55f)
                    {
                        lookWait += Time.deltaTime;
                        yield return null;
                    }

                    inputReader.DebugClear();
                    var yawDelta = Mathf.Abs(Mathf.DeltaAngle(yawBefore, gameCam.transform.eulerAngles.y));
                    if (yawDelta < 8f)
                    {
                        Fail($"Camera look orbit too small (yawDelta={yawDelta:0.0})");
                    }
                    else
                    {
                        Ok($"Camera look orbit yawDelta={yawDelta:0.0}");
                    }

                    // Collision pull-in: obstruct follow→camera and expect closer framing.
                    Teleport(motor, before + Vector3.up * 0.1f);
                    yield return new WaitForSeconds(0.35f);
                    var origin = motor.transform.position + Vector3.up * 2.2f;
                    var freeDist = Vector3.Distance(gameCam.transform.position, origin);
                    var camWorld = gameCam.transform.position;
                    var mid = (origin + camWorld) * 0.5f;
                    var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "~SmokeCameraBlocker";
                    wall.transform.position = mid;
                    wall.transform.localScale = new Vector3(3.5f, 3.5f, 0.4f);
                    wall.transform.rotation = Quaternion.LookRotation((camWorld - origin).normalized, Vector3.up);
                    var collideWait = 0f;
                    while (collideWait < 0.6f)
                    {
                        collideWait += Time.deltaTime;
                        yield return null;
                    }

                    var blockedDist = Vector3.Distance(gameCam.transform.position, origin);
                    Object.Destroy(wall);
                    if (blockedDist >= freeDist - 0.15f)
                    {
                        Fail($"Camera collision did not pull in (free={freeDist:0.00}, blocked={blockedDist:0.00})");
                    }
                    else
                    {
                        Ok($"Camera collision pull-in free={freeDist:0.00} → blocked={blockedDist:0.00}");
                    }
                }

                yield return CharacterizeJumpForgiveness(motor, inputReader, before, Ok, Fail);

                RestoreMutedBehaviours(mutedBoings);
            }
            else
            {
                Fail("Missing SplatInputReader or SplatMotorSettings for characterization");
            }

            // Collect via real pickup Interact path (keeps SO refs authoritative).
            var pickups = FindObjectsByType<CollectiblePickup>(FindObjectsInactive.Include)
                .Where(pickup => pickup != null && pickup.Definition != null
                    && (pickup.Definition.Id == "component_spring"
                        || pickup.Definition.Id == "component_metal_dish"
                        || pickup.Definition.Id == "component_power_crystal"
                        || pickup.Definition.Id == "material_rainbow_goo"))
                .OrderBy(pickup => Vector3.Distance(motor.transform.position, pickup.transform.position))
                .ToArray();
            var collected = 0;
            var discoveryEyePeak = 0f;
            var componentDiscoveries = new Dictionary<string, SplatSkeletonAnimator.DiscoveryKind>();
            var smokeInputReader = motor.GetComponent<SplatInputReader>();
            var testedRealPickupInput = false;
            Vector2 ProductionMoveToward(Vector3 target)
            {
                var desired = target - motor.transform.position;
                desired.y = 0f;
                var cameraTransform = Camera.main != null ? Camera.main.transform : null;
                var forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
                var right = cameraTransform != null ? cameraTransform.right : Vector3.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                return desired.sqrMagnitude > 0.001f
                    ? new Vector2(Vector3.Dot(desired.normalized, right), Vector3.Dot(desired.normalized, forward))
                    : Vector2.zero;
            }

            KeyboardState ProductionKeysToward(Vector3 target)
            {
                var input = ProductionMoveToward(target);
                var keys = new List<Key>(2);
                if (input.y >= 0.35f) keys.Add(Key.W);
                else if (input.y <= -0.35f) keys.Add(Key.S);
                if (input.x >= 0.35f) keys.Add(Key.D);
                else if (input.x <= -0.35f) keys.Add(Key.A);
                return new KeyboardState(keys.ToArray());
            }

            // The refused-build reaction check is isolated characterization.
            // Restore the authored start before exercising the gathering route.
            Teleport(motor, buildSite.transform.position + Vector3.up * 1.2f);
            Physics.SyncTransforms();
            var buildFocusWait = 0f;
            while ((!motor.IsGrounded || splatInteractor.CurrentFocus != buildSite) && buildFocusWait < 1.5f)
            {
                buildFocusWait += Time.deltaTime;
                yield return null;
            }
            if (splatInteractor.CurrentFocus != buildSite)
            {
                Fail($"Expected BuildSite focus before refused-build reaction, got {splatInteractor.CurrentFocus}");
            }
            else if (smokeInputReader == null)
            {
                Fail("SplatInputReader missing for real interaction-path reaction smoke");
            }
            else
            {
                smokeInputReader.DebugPulseInteract();
                var oopsWait = 0f;
                while (activeMeshyVisual && !meshyAnimator.IsPlayingActionForDiagnostics("Oops") && oopsWait < 0.4f)
                {
                    oopsWait += Time.deltaTime;
                    yield return null;
                }
                if (activeMeshyVisual && oopsWait == 0f) yield return null;
                if (activeMeshyVisual)
                {
                    if (!meshyAnimator.IsPlayingActionForDiagnostics("Oops"))
                    {
                        Fail($"Active Meshy Splat did not enter the Oops response when a build was refused (controller={meshyAnimator.RuntimeControllerNameForDiagnostics}, clip={meshyAnimator.RuntimeClipNameForDiagnostics}, hasTrigger={meshyAnimator.HasTriggerForDiagnostics("Oops")})");
                    }
                    else
                    {
                        Ok("Active Meshy Splat entered the Oops response on refused build");
                    }
                }
                else if (skeletonAnimator == null || !skeletonAnimator.enabled
                    || skeletonAnimator.CurrentAction != SplatSkeletonAnimator.Action.Oops)
                {
                    Fail($"Refused build did not preserve Splat's sheepish reaction (action={(skeletonAnimator != null ? skeletonAnimator.CurrentAction.ToString() : "missing")})");
                }
                else
                {
                    Ok("Real Splat interaction preserves the refused-build shrug instead of overwriting it with the generic reach");
                }
            }

            smokeInputReader?.DebugClear();
            Teleport(motor, before + Vector3.up * 0.05f);
            motor.HardResetState();
            Physics.SyncTransforms();
            var routeGroundWait = 0f;
            while (!motor.IsGrounded && routeGroundWait < 1f)
            {
                routeGroundWait += Time.deltaTime;
                yield return null;
            }
            var pickupArcChecked = false;
            var pickupArcSpawnCountBeforeCollection = TempCraftSparkBurst.PickupArcSpawnCountForDiagnostics;
            var proximityAssistChecked = false;
            var activeMeshDiscoveries = new HashSet<string>();
            foreach (var pickup in pickups)
            {
                if (pickup == null || pickup.Definition == null || !pickup.CanInteract)
                {
                    continue;
                }

                var id = pickup.Definition.Id;
                var verifyProximityAssist = id == "component_metal_dish" && !proximityAssistChecked;
                // Walk to the authored pickup using production W/A/S/D bindings.
                // Stop at interaction range for ordinary E pickups; close only
                // to the assist sensor for the dedicated metal-dish near miss.
                var pickupDirection = Vector3.ProjectOnPlane(
                    pickup.transform.position - motor.transform.position, Vector3.up).normalized;
                if (pickupDirection.sqrMagnitude < 0.01f) pickupDirection = Vector3.forward;
                var pickupStandOff = verifyProximityAssist ? 0.9f : 1.6f;
                var approach = pickup.transform.position - pickupDirection * pickupStandOff;
                approach.y = motor.transform.position.y;
                var routeStartPosition = motor.transform.position;
                var approachStartedAt = Time.time;
                var approachElapsed = 0f;
                while (pickup.CanInteract && approachElapsed < 12f
                    && (splatInteractor.CurrentFocus != pickup
                        || Vector3.ProjectOnPlane(pickup.transform.position - motor.transform.position, Vector3.up).magnitude > pickupStandOff + 0.08f))
                {
                    pickupDirection = Vector3.ProjectOnPlane(
                        pickup.transform.position - motor.transform.position, Vector3.up).normalized;
                    if (pickupDirection.sqrMagnitude < 0.01f) pickupDirection = Vector3.forward;
                    approach = pickup.transform.position - pickupDirection * pickupStandOff;
                    approach.y = motor.transform.position.y;
                    var remaining = Vector3.ProjectOnPlane(approach - motor.transform.position, Vector3.up).magnitude;
                    if (remaining <= 0.45f)
                    {
                        // If the interaction sphere has not focused the target
                        // at the near-miss point, close the remaining gap on foot.
                        approach = pickup.transform.position;
                        approach.y = motor.transform.position.y;
                    }

                    if (reviewKeyboard == null)
                    {
                        Fail($"No keyboard available for production approach to {id}");
                        break;
                    }

                    InputSystem.QueueStateEvent(reviewKeyboard, ProductionKeysToward(approach));
                    approachElapsed += Time.deltaTime;
                    yield return null;
                }

                if (reviewKeyboard != null)
                {
                    InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
                    yield return null;
                }
                var focusWait = Time.time - approachStartedAt;
                var walkedDistance = Vector3.ProjectOnPlane(
                    motor.transform.position - routeStartPosition, Vector3.up).magnitude;
                Debug.Log($"[ArrivalSmoke] Production approach to {id}: walked={walkedDistance:F2}m, elapsed={focusWait:F2}s, focused={splatInteractor.CurrentFocus == pickup}, collected={!pickup.CanInteract}.");
                if (verifyProximityAssist)
                {
                    var assistWait = 0f;
                    while (pickup.CanInteract && assistWait < 0.25f)
                    {
                        assistWait += Time.deltaTime;
                        yield return null;
                    }

                    proximityAssistChecked = true;
                    if (pickup.CanInteract)
                    {
                        var playerController = motor.GetComponent<CharacterController>();
                        var sensor = pickup.ProximityAssistSensor;
                        var center = sensor != null ? sensor.bounds.center : pickup.transform.position;
                        var closest = playerController != null ? playerController.ClosestPoint(center) : motor.transform.position;
                        Fail($"Splat did not collect a component from the wider near-miss assist radius (assistRadius={pickup.AssistRadiusWorldForDiagnostics:F2}, overlapCount={pickup.LastAssistOverlapCountForDiagnostics}, pickup={pickup.transform.position}, player={motor.transform.position}, closest={closest}, sensor={sensor != null})");
                        if (reviewKeyboard != null && splatInteractor.CurrentFocus == pickup)
                        {
                            InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState(Key.E));
                            yield return null;
                            InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
                            yield return null;
                        }
                    }
                    else
                    {
                        Ok("Near-miss component pickup collects through the proximity-assist radius");
                    }
                }
                else if (id == "component_spring" && !testedRealPickupInput && splatInteractor.CurrentFocus == pickup)
                {
                    var springCollectedByProductionInput = false;
                    if (reviewKeyboard != null)
                    {
                        InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState(Key.E));
                        yield return null;
                        InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
                        yield return null;
                        springCollectedByProductionInput = !pickup.CanInteract;
                        testedRealPickupInput = springCollectedByProductionInput;
                    }

                    if (springCollectedByProductionInput)
                    {
                        Ok("Keyboard E reached the production Interact InputAction on the focused spring pickup");
                    }
                    else
                    {
                        Fail("Keyboard E did not collect the focused spring through the production Interact InputAction");
                    }

                    if (activeMeshyVisual)
                    {
                        var discoverWait = 0f;
                        while (!meshyAnimator.IsPlayingActionForDiagnostics("Discover") && discoverWait < 0.4f)
                        {
                            discoverWait += Time.deltaTime;
                            yield return null;
                        }

                        if (!meshyAnimator.IsPlayingActionForDiagnostics("Discover"))
                        {
                            Fail($"Active Meshy Splat did not enter Discover after real component pickup input (controller={meshyAnimator.RuntimeControllerNameForDiagnostics}, clip={meshyAnimator.RuntimeClipNameForDiagnostics}, hasTrigger={meshyAnimator.HasTriggerForDiagnostics("Discover")})");
                        }
                        else
                        {
                            Ok("Active Meshy Splat entered Discover after real component pickup input");
                        }
                    }
                    else if (skeletonAnimator == null || !skeletonAnimator.enabled
                        || skeletonAnimator.CurrentAction != SplatSkeletonAnimator.Action.Discover
                        || skeletonAnimator.CurrentDiscoveryKind != SplatSkeletonAnimator.DiscoveryKind.Spring)
                    {
                        Fail($"Real Splat input overwrote the spring reaction (action={skeletonAnimator.CurrentAction}, kind={skeletonAnimator.CurrentDiscoveryKind})");
                    }
                    else
                    {
                        Ok("Real Splat input preserves the spring-specific discovery reaction");
                    }
                }
                else
                {
                    if (id == "component_spring" && !testedRealPickupInput)
                    {
                        Fail($"Splat did not focus the expected real-path pickup {id}; focus={splatInteractor.CurrentFocus}, player={motor.transform.position}, pickup={pickup.transform.position}, focusRadius={splatInteractor.Radius:0.00}, focusWait={focusWait:0.00}");
                    }

                    if (pickup.CanInteract && splatInteractor.CurrentFocus == pickup && reviewKeyboard != null)
                    {
                        InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState(Key.E));
                        yield return null;
                        InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
                        yield return null;
                    }

                    if (pickup.CanInteract)
                    {
                        Fail($"Production E did not collect {id} at its authored pickup location (focus={splatInteractor.CurrentFocus})");
                    }
                }
                collected++;
                yield return null;
                if (!pickupArcChecked)
                {
                    pickupArcChecked = true;
                    var arcSpawned = TempCraftSparkBurst.PickupArcSpawnCountForDiagnostics > pickupArcSpawnCountBeforeCollection;
                    if (activeMeshyVisual ? !arcSpawned : FindAnyObjectByType<TempPouchPickupArc>() == null)
                    {
                        Fail($"Collecting a pickup did not create the short sparkle arc toward Splat's visible maker pack (spawnCount={TempCraftSparkBurst.PickupArcSpawnCountForDiagnostics}, before={pickupArcSpawnCountBeforeCollection}, activeMeshy={activeMeshyVisual})");
                    }
                    else
                    {
                        Ok(activeMeshyVisual
                            ? "Pickup spawned a sparkle arc toward Splat's visible maker pack"
                            : "Pickup creates a live sparkle arc toward Splat's visible maker pack");
                    }
                }

                if (pickup.Definition.Category == CollectibleCategory.Component && activeMeshyVisual)
                {
                    if (meshyAnimator.IsPlayingActionForDiagnostics("Discover"))
                        activeMeshDiscoveries.Add(pickup.Definition.Id);
                }
                else if (pickup.Definition.Category == CollectibleCategory.Component &&
                    skeletonAnimator != null && skeletonAnimator.enabled)
                {
                    componentDiscoveries[pickup.Definition.Id] = skeletonAnimator.CurrentAction == SplatSkeletonAnimator.Action.Discover
                        ? skeletonAnimator.CurrentDiscoveryKind
                        : SplatSkeletonAnimator.DiscoveryKind.Other;

                    var eyeRoot = FindTransformByName(skeletonAnimator.transform, "ART_EyeRootL");
                    var expressionWait = 0f;
                    while (eyeRoot != null && expressionWait < 0.2f)
                    {
                        discoveryEyePeak = Mathf.Max(discoveryEyePeak, eyeRoot.localScale.y);
                        expressionWait += Time.deltaTime;
                        yield return null;
                    }
                }
            }

            if (!inventory.HasAll(new[] { "component_spring", "component_metal_dish", "component_power_crystal" }))
            {
                Fail($"Inventory missing craft components after {collected} pickup interacts. Owned=[{string.Join(",", inventory.OwnedIds)}]");
                    ReleaseReviewKeyboard();
                Finish(pass, report.ToString());
                yield break;
            }

            Ok($"Collected craft components via pickups ({collected} interacts)");
            if (!testedRealPickupInput && syntheticKeyboardInputUnavailable)
            {
                Debug.LogWarning("[ArrivalSmoke] No production keyboard pickup was verified because synthetic Batch Mode input was unavailable.");
            }
            else if (!testedRealPickupInput)
            {
                Fail("The real Splat input path never reached a component pickup");
            }
            if (!proximityAssistChecked)
            {
                Fail("The Arrival smoke did not exercise the near-miss pickup assist");
            }
            if (activeMeshyVisual)
            {
                var discoveredRequiredParts = activeMeshDiscoveries.Contains("component_spring")
                    && activeMeshDiscoveries.Contains("component_metal_dish")
                    && activeMeshDiscoveries.Contains("component_power_crystal");
                if (!discoveredRequiredParts)
                {
                    Fail($"Active Meshy Splat missed component discovery reactions; observed=[{string.Join(",", activeMeshDiscoveries)}]");
                }
                else
                {
                    Ok("Active Meshy Splat entered its imported Alert-based Discover state for all three craft components");
                }
                Ok("Eye-scale expression checks are not applicable to the active Meshy face rig");
            }
            else
            {
            var discoveredRequiredParts = componentDiscoveries.TryGetValue("component_spring", out var springReaction)
                && springReaction == SplatSkeletonAnimator.DiscoveryKind.Spring
                && componentDiscoveries.TryGetValue("component_metal_dish", out var dishReaction)
                && dishReaction == SplatSkeletonAnimator.DiscoveryKind.Dish
                && componentDiscoveries.TryGetValue("component_power_crystal", out var crystalReaction)
                && crystalReaction == SplatSkeletonAnimator.DiscoveryKind.Crystal;
            if (!discoveredRequiredParts)
            {
                Fail($"Component-specific discovery reaction missing; observed=[{string.Join(",", componentDiscoveries.Select(pair => $"{pair.Key}:{pair.Value}"))}]");
            }
            else
            {
                Ok("Splat used a different discovery reaction for the spring, dish, and crystal");
            }

            if (discoveryEyePeak <= 1.03f)
            {
                Fail($"Splat's curious eye expression did not widen on discovery (peak Y scale={discoveryEyePeak:0.00})");
            }
            else
            {
                Ok($"Splat's eyes widen with curiosity on discovery (peak Y scale={discoveryEyePeak:0.00})");
            }
            }

            if (activeMeshyVisual)
            {
                var wonderRegistry = SecretWonderRegistry.Ensure();
                var smokeWonderId = $"smoke_wonder_{Time.frameCount}";
                if (!wonderRegistry.TryClaim(smokeWonderId))
                {
                    Fail("SecretWonderRegistry rejected the smoke wonder claim");
                }
                else
                {
                    yield return null;
                    if (!meshyAnimator.IsPlayingActionForDiagnostics("Discover"))
                        Fail("Active Meshy Splat did not enter Discover for the hidden-wonder claim");
                    else
                        Ok("Active Meshy Splat entered Discover for the hidden-wonder claim");
                }
            }
            else if (skeletonAnimator == null || !skeletonAnimator.enabled)
            {
                Fail("SplatSkeletonAnimator missing for hidden-wonder reaction");
            }
            else
            {
                var wonderRegistry = SecretWonderRegistry.Ensure();
                var smokeWonderId = $"smoke_wonder_{Time.frameCount}";
                if (!wonderRegistry.TryClaim(smokeWonderId))
                {
                    Fail("SecretWonderRegistry rejected the smoke wonder claim");
                }
                else
                {
                    yield return null;
                    if (skeletonAnimator.CurrentAction != SplatSkeletonAnimator.Action.Discover
                        || skeletonAnimator.CurrentDiscoveryKind != SplatSkeletonAnimator.DiscoveryKind.Wonder)
                    {
                        Fail($"Splat did not play the hidden-wonder reaction (action={skeletonAnimator.CurrentAction}, kind={skeletonAnimator.CurrentDiscoveryKind})");
                    }
                    else
                    {
                        Ok("Splat plays a joyful discovery reaction when the secret-wonder registry claims a beacon");
                    }
                }
            }

            var built = false;
            void OnBuilt(string id)
            {
                if (id == "boing_o_matic")
                {
                    built = true;
                }
            }

            GameEvents.BuildCompleted += OnBuilt;
            var craftWalkStartedAt = Time.time;
            while (splatInteractor.CurrentFocus != buildSite && Time.time - craftWalkStartedAt < 12f)
            {
                if (reviewKeyboard == null)
                {
                    Fail("No keyboard available for production approach to the BuildSite");
                    break;
                }

                var craftDirection = Vector3.ProjectOnPlane(
                    buildSite.transform.position - motor.transform.position, Vector3.up).normalized;
                var craftApproach = buildSite.transform.position - craftDirection * 1.25f;
                craftApproach.y = motor.transform.position.y;
                var craftDistance = Vector3.ProjectOnPlane(
                    craftApproach - motor.transform.position, Vector3.up).magnitude;
                if (craftDistance <= 0.4f)
                {
                    // The first frame at the nominal stand-off can arrive before
                    // SplatInteractor refreshes its focus. Continue closing gently
                    // instead of ending the route before the interaction becomes
                    // available.
                    var finalDirection = Vector3.ProjectOnPlane(
                        buildSite.transform.position - motor.transform.position, Vector3.up).normalized;
                    craftApproach = buildSite.transform.position - finalDirection * 0.65f;
                    craftApproach.y = motor.transform.position.y;
                }

                InputSystem.QueueStateEvent(reviewKeyboard, ProductionKeysToward(craftApproach));
                yield return null;
            }

            if (reviewKeyboard != null)
            {
                InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
                yield return null;
            }

            if (splatInteractor.CurrentFocus != buildSite)
            {
                Fail($"Splat did not reach BuildSite through the production movement path (focus={splatInteractor.CurrentFocus}, player={motor.transform.position:F2}, site={buildSite.transform.position:F2}, elapsed={Time.time - craftWalkStartedAt:F2}s)");
            }
            else if (reviewKeyboard != null)
            {
                InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState(Key.E));
                yield return null;
                InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
            }

            var craftWait = 0f;
            while (!built && craftWait < 5f)
            {
                craftWait += Time.deltaTime;
                yield return null;
            }

            GameEvents.BuildCompleted -= OnBuilt;

            if (!built || !session.HasInvention("boing_o_matic"))
            {
                Fail("BuildSite did not complete Boing-O-Matic");
                ReleaseReviewKeyboard();
                Finish(pass, report.ToString());
                yield break;
            }

            Ok("Craft completed (Boing-O-Matic)");

            // The crafting interaction can continue while the automated pickup
            // route is settling on the workshop edge. Re-establish ground contact
            // at the same horizontal location before the boss approach so leftover
            // fall velocity cannot turn that route check into an OOB recovery test.
            var postCraftGroundOrigin = motor.transform.position + Vector3.up * 16f;
            if (Physics.Raycast(postCraftGroundOrigin, Vector3.down, out var postCraftGround,
                24f, motor.GroundSupportMask, QueryTriggerInteraction.Ignore))
            {
                Teleport(motor, postCraftGround.point + Vector3.up * 0.05f);
                motor.HardResetState();
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                Ok($"Post-craft smoke route resumed from supported workshop ground ({postCraftGround.collider.name})");
            }
            else
            {
                Fail($"No walkable ground below the workshop after crafting (position={motor.transform.position:F2})");
            }

            var gulchPad = GameObject.Find("TEMP_ART_GulchBoingOMatic");
            var boing = gulchPad != null
                ? gulchPad.GetComponentInChildren<BoingOMatic>(true) : null;
            var playerCollider = motor.GetComponent<CharacterController>();
            var boingTrigger = boing != null ? boing.GetComponent<Collider>() : null;
            var terracePad = GameObject.Find("TEMP_ART_TerraceBoingOMatic");
            var terraceBoing = terracePad != null
                ? terracePad.GetComponentInChildren<BoingOMatic>(true) : null;
            var terraceTrigger = terraceBoing != null ? terraceBoing.GetComponent<Collider>() : null;
            var weakPoint = boss.GetComponentInChildren<BossWeakPoint>(true);
            var weakPointCollider = weakPoint != null ? weakPoint.GetComponent<Collider>() : null;
            var hitsField = typeof(ScrapStormSentinel).GetField("_hits",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var hitsToDefeatField = typeof(ScrapStormSentinel).GetField("hitsToDefeat",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (gulchPad == null || !gulchPad.activeInHierarchy)
            {
                Fail("Gulch Boing pad not active after craft");
            }
            else if (boing == null || playerCollider == null || boingTrigger == null
                || !boingTrigger.isTrigger)
            {
                Fail("Crafted Boing pad is missing its trigger path or player collider");
            }
            else
            {
                Ok("Gulch Boing pad unlocked with trigger and player collider");
            }

            var launchOriginY = motor.transform.position.y;
            var highestLaunchY = launchOriginY;
            var craftedLaunchConfirmed = false;
            var initialHits = hitsField != null ? (int)hitsField.GetValue(boss) : -1;
            var bossHitCountAtStart = initialHits;
            var hitsToDefeat = hitsToDefeatField != null ? (int)hitsToDefeatField.GetValue(boss) : 3;
            var relayHitCueCountAtBossStart = TempSfxPlayer.Instance != null
                ? TempSfxPlayer.Instance.RelayHitCuePlayCountForDiagnostics
                : 0;
            var naturalArcHit = false;
            var closestRelayApproach = float.PositiveInfinity;
            var closestArcPosition = motor.transform.position;
            var closestArcTime = 0f;
            var closestArcCollisionFlags = CollisionFlags.None;
            var closestArcGrounded = false;
            var closestArcHasCraftedProof = false;
            var closestArcBlockers = "none";
            var terraceClosestApproach = float.PositiveInfinity;
            var closestTerraceArcPosition = motor.transform.position;
            var launchedPlanarVelocity = Vector3.zero;
            var launchedVerticalVelocity = 0f;
            var naturalBoingUses = 0;
            var terracePadEntryPosition = motor.transform.position;
            var terracePadEntryDistance = float.PositiveInfinity;
            void OnNaturalBoingUsed(string inventionId)
            {
                if (inventionId != "boing_o_matic") return;
                naturalBoingUses++;
                Debug.Log($"[ArrivalSmoke] Observed natural Boing launch #{naturalBoingUses} at {motor.transform.position:F2}, exposed={(weakPoint != null && weakPoint.IsExposed)}, proof={motor.WasRecentlyLaunchedByCraftedInvention}.");
                if (naturalBoingUses == 2 && terraceTrigger != null)
                {
                    terracePadEntryPosition = motor.transform.position;
                    terracePadEntryDistance = Vector3.Distance(
                        terracePadEntryPosition, terraceTrigger.bounds.center);
                }
            }

            Vector2 MoveInputToward(Vector3 target)
            {
                var desired = target - motor.transform.position;
                desired.y = 0f;
                var cameraTransform = Camera.main != null ? Camera.main.transform : null;
                var forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
                var right = cameraTransform != null ? cameraTransform.right : Vector3.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                var input = desired.sqrMagnitude > 0.001f
                    ? new Vector2(Vector3.Dot(desired.normalized, right),
                        Vector3.Dot(desired.normalized, forward))
                    : Vector2.zero;
                return input;
            }

            KeyboardState KeyboardToward(Vector3 target)
            {
                var input = MoveInputToward(target);
                var keys = new List<Key>(2);
                if (input.y >= 0.35f) keys.Add(Key.W);
                else if (input.y <= -0.35f) keys.Add(Key.S);
                if (input.x >= 0.35f) keys.Add(Key.D);
                else if (input.x <= -0.35f) keys.Add(Key.A);
                return new KeyboardState(keys.ToArray());
            }

            if (boing != null && playerCollider != null && boingTrigger != null
                && weakPoint != null && weakPointCollider != null && hitsField != null)
            {
                // Launch from the authored pad and let Unity physics resolve
                // the flight toward an exposed boss relay without repositioning
                // Splat. This measures the authored route rather than claiming
                // a hit from the later deterministic contact setup.
                var relaySphere = weakPointCollider as SphereCollider;
                var dormantRelayRadius = relaySphere != null ? relaySphere.radius : 0f;
                // Stage outside Rattle's actual slam radius. Waiting beside the
                // spring lets the slam displace Splat before the punish window.
                var slamRadiusField = typeof(ScrapStormSentinel).GetField("slamRadius",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var slamRadius = slamRadiusField != null ? (float)slamRadiusField.GetValue(boss) : 3.4f;
                var awayFromBoss = boingTrigger.bounds.center - boss.transform.position;
                awayFromBoss.y = 0f;
                var bossToPadDistance = awayFromBoss.magnitude;
                awayFromBoss = awayFromBoss.sqrMagnitude > 0.001f ? awayFromBoss.normalized : Vector3.back;
                var stagingOffset = Mathf.Max(1.8f, slamRadius + 1.25f - bossToPadDistance);
                var approachStart = boingTrigger.bounds.center + awayFromBoss * stagingOffset;
                var approachWalkStart = motor.transform.position;
                var approachWalkWait = 0f;
                var nextApproachDiagnostic = 1f;
                var usedSyntheticMoveFallback = false;
                var approachPlanarOffset = motor.transform.position - approachStart;
                approachPlanarOffset.y = 0f;
                while ((!motor.IsGrounded || approachPlanarOffset.magnitude > 0.45f)
                    && approachWalkWait < 3f && motor.transform.position.y > -2f)
                {
                    if (reviewKeyboard != null)
                    {
                        var withinBrakingRadius = approachPlanarOffset.magnitude <= 1.1f;
                        var state = withinBrakingRadius
                            ? new KeyboardState()
                            : KeyboardToward(approachStart);
                        InputSystem.QueueStateEvent(reviewKeyboard, state);
                        if (withinBrakingRadius)
                            inputReader.DebugSetMove(Vector2.zero);
                        if (!usedSyntheticMoveFallback && reviewKeyboardWasAdded
                            && !withinBrakingRadius && approachWalkWait >= 0.35f
                            && inputReader.Move.sqrMagnitude < 0.01f)
                        {
                            inputReader.DebugSetMove(MoveInputToward(approachStart));
                            usedSyntheticMoveFallback = true;
                            Debug.LogWarning("[ArrivalSmoke] Synthetic keyboard produced no production Move value; using explicit motor-input fallback for the remaining approach. This segment is test-assisted, not production keyboard evidence.");
                        }
                        if (approachWalkWait >= nextApproachDiagnostic)
                        {
                            Debug.Log($"[ArrivalSmoke] Workshop-to-pad steering at {approachWalkWait:F1}s: player={motor.transform.position:F2}, target={approachStart:F2}, move={inputReader.Move:F2}, velocity={motor.PlanarVelocity:F2}, grounded={motor.IsGrounded}, cameraYaw={(Camera.main != null ? Camera.main.transform.eulerAngles.y : -1f):F1}.");
                            nextApproachDiagnostic += 1f;
                        }
                    }
                    approachWalkWait += Time.deltaTime;
                    yield return null;
                    approachPlanarOffset = motor.transform.position - approachStart;
                    approachPlanarOffset.y = 0f;
                }
                if (reviewKeyboard != null)
                {
                    InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
                }
                inputReader.DebugClear();
                var approachWalkDistance = Vector3.Distance(motor.transform.position, approachStart);
                if (approachWalkDistance <= 0.8f)
                {
                    Ok(usedSyntheticMoveFallback
                        ? $"Test-assisted motor input traversed from the workshop toward Rattle's approach point ({Vector3.Distance(approachWalkStart, motor.transform.position):F2}m); production keyboard was unavailable"
                        : $"Production keyboard traversed from the workshop toward Rattle's approach point ({Vector3.Distance(approachWalkStart, motor.transform.position):F2}m)");
                }
                else
                {
                    Fail($"Splat could not reach Rattle's safe approach point from the workshop (remaining={approachWalkDistance:F2}m, elapsed={approachWalkWait:F2}s, position={motor.transform.position:F2}, target={approachStart:F2})");
                }
                Physics.SyncTransforms();
                var initialExposureWait = 0f;
                while (!weakPoint.IsExposed && initialExposureWait < 8f)
                {
                    initialExposureWait += Time.deltaTime;
                    yield return null;
                }
                if (!weakPoint.IsExposed)
                {
                    Fail("Rattle did not naturally expose the relay before the first Boing approach");
                }
                if (relaySphere != null && relaySphere.radius > dormantRelayRadius + 0.01f)
                {
                    Ok($"Exposed relay strike radius expands ({dormantRelayRadius:F2}→{relaySphere.radius:F2})");
                }
                else
                {
                    Fail("Exposed relay strike radius did not grow with the crystal");
                }
                GameEvents.InventionUsed += OnNaturalBoingUsed;
                if (reviewKeyboard == null)
                {
                    Fail("Could not drive the first boss Boing approach with the production keyboard input");
                }
                else
                {
                    var launchCountBeforeApproach = naturalBoingUses;
                    var approachWait = 0f;
                    var usedSyntheticPadFallback = false;
                    while (naturalBoingUses == launchCountBeforeApproach && approachWait < 2f)
                    {
                        InputSystem.QueueStateEvent(reviewKeyboard, KeyboardToward(boingTrigger.bounds.center));
                        if (!usedSyntheticPadFallback && reviewKeyboardWasAdded
                            && approachWait >= 0.35f && inputReader.Move.sqrMagnitude < 0.01f)
                        {
                            inputReader.DebugSetMove(MoveInputToward(boingTrigger.bounds.center));
                            usedSyntheticPadFallback = true;
                            Debug.LogWarning("[ArrivalSmoke] Synthetic keyboard still produced no production Move value at the Boing pad; continuing with explicit motor-input fallback. This approach is test-assisted, not production keyboard evidence.");
                        }
                        approachWait += Time.deltaTime;
                        yield return null;
                    }
                    InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
                    inputReader.DebugClear();
                    Debug.Log($"[ArrivalSmoke] First Boing approach: inputMode={(usedSyntheticPadFallback ? "test-assisted motor input (synthetic keyboard unavailable)" : "production keyboard")}, exposureWait={initialExposureWait:F2}s, elapsed={approachWait:F2}s, newCraftedLaunches={naturalBoingUses - launchCountBeforeApproach}, start={approachStart:F2}, pad={boingTrigger.bounds.center:F2}, boss={boss.transform.position:F2}, bossToPad={bossToPadDistance:F2}m, slamRadius={slamRadius:F2}m, end={motor.transform.position:F2}, exposed={weakPoint.IsExposed}, launchProof={motor.WasRecentlyLaunchedByCraftedInvention}.");
                }
                launchOriginY = motor.transform.position.y;
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return null;
                craftedLaunchConfirmed = motor.WasRecentlyLaunchedByCraftedInvention;
                launchedPlanarVelocity = motor.PlanarVelocity;
                launchedVerticalVelocity = motor.VerticalVelocity;
                highestLaunchY = Mathf.Max(highestLaunchY, motor.transform.position.y);
                var arcStartTime = Time.time;
                var farCheckpointObject = GameObject.Find("ART_Checkpoint_GulchFar");
                var farCheckpointVolume = farCheckpointObject != null
                    ? farCheckpointObject.GetComponent<CheckpointVolume>() : null;
                var farCheckpointCollider = farCheckpointObject != null
                    ? farCheckpointObject.GetComponent<Collider>() : null;
                var closestCheckpointSurfaceGap = float.PositiveInfinity;
                var closestCheckpointPlayerPosition = motor.transform.position;
                var checkpointSphere = farCheckpointCollider as SphereCollider;
                var checkpointTriggerRadius = checkpointSphere != null
                    ? checkpointSphere.radius * Mathf.Max(farCheckpointObject.transform.lossyScale.x,
                        farCheckpointObject.transform.lossyScale.y, farCheckpointObject.transform.lossyScale.z)
                    : 0f;

                for (var arcStep = 0; arcStep < 60
                    && (int)hitsField.GetValue(boss) == initialHits; arcStep++)
                {
                    if (reviewKeyboard != null)
                    {
                        var naturalTarget = naturalBoingUses < 2 && terraceTrigger != null
                            ? terraceTrigger.bounds.center : weakPointCollider.bounds.center;
                        InputSystem.QueueStateEvent(reviewKeyboard, KeyboardToward(naturalTarget));
                    }
                    Physics.SyncTransforms();
                    yield return new WaitForFixedUpdate();
                    yield return null;
                    highestLaunchY = Mathf.Max(highestLaunchY, motor.transform.position.y);
                    if (farCheckpointCollider != null)
                    {
                        var checkpointCenter = farCheckpointCollider.bounds.center;
                        var playerSurfacePoint = playerCollider.ClosestPoint(checkpointCenter);
                        var checkpointGap = Vector3.Distance(playerSurfacePoint, checkpointCenter);
                        if (checkpointGap < closestCheckpointSurfaceGap)
                        {
                            closestCheckpointSurfaceGap = checkpointGap;
                            closestCheckpointPlayerPosition = motor.transform.position;
                        }
                    }
                    var closest = weakPointCollider.ClosestPoint(motor.transform.position);
                    var approach = Vector3.Distance(motor.transform.position, closest);
                    if (terraceTrigger != null)
                    {
                        var terraceClosest = terraceTrigger.ClosestPoint(motor.transform.position);
                        var terraceApproach = Vector3.Distance(motor.transform.position, terraceClosest);
                        if (terraceApproach < terraceClosestApproach)
                        {
                            terraceClosestApproach = terraceApproach;
                            closestTerraceArcPosition = motor.transform.position;
                        }
                    }
                    if (approach < closestRelayApproach)
                    {
                        closestRelayApproach = approach;
                        closestArcPosition = motor.transform.position;
                        closestArcTime = Time.time - arcStartTime;
                        closestArcCollisionFlags = playerCollider.collisionFlags;
                        closestArcGrounded = motor.IsGrounded;
                        closestArcHasCraftedProof = motor.WasRecentlyLaunchedByCraftedInvention;
                        var obstructionDirection = motor.PlanarVelocity.sqrMagnitude > 0.01f
                            ? motor.PlanarVelocity.normalized : motor.transform.forward;
                        closestArcBlockers = DescribeForwardObstructions(motor, obstructionDirection, 2f);
                    }
                }

                if (reviewKeyboard != null)
                {
                    InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
                }

                naturalArcHit = (int)hitsField.GetValue(boss) > initialHits;
                if (naturalArcHit)
                {
                    Ok("Two-pad Boing arc reaches the exposed relay through physics with held keyboard input");
                    if (relaySphere != null && Mathf.Approximately(relaySphere.radius, dormantRelayRadius))
                    {
                        Ok("Relay strike radius returns to its dormant size after a hit");
                    }
                    else
                    {
                        Fail("Relay strike radius did not reset after a hit");
                    }
                }
                else
                {
                    Debug.LogWarning("[ArrivalSmoke] Production-input first Boing approach missed the exposed relay; retaining the assisted terrace leg below as the full-combat regression check.");
                }
                var directionField = typeof(BoingOMatic).GetField("launchDirection",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var localLaunchDirection = directionField != null
                    ? (Vector3)directionField.GetValue(boing) : Vector3.zero;
                Debug.Log($"[ArrivalSmoke] Natural Boing chain: launchCount={naturalBoingUses}, pad={boingTrigger.bounds.center:F2}, relay={weakPointCollider.bounds.center:F2}, end={motor.transform.position:F2}, terracePadActive={(terracePad != null && terracePad.activeInHierarchy)}, terracePad={(terraceTrigger != null ? terraceTrigger.bounds.center.ToString("F2") : "missing")}, secondPadEntry={terracePadEntryPosition:F2}, entryToTerrace={terracePadEntryDistance:F2}m, closestTerracePosition={closestTerraceArcPosition:F2}, closestTerraceApproach={terraceClosestApproach:F2}m, checkpoint={(farCheckpointCollider != null ? farCheckpointCollider.bounds.center.ToString("F2") : "missing")}, checkpointTriggerRadius={checkpointTriggerRadius:F2}, closestCheckpointGap={closestCheckpointSurfaceGap:F2}, closestCheckpointAt={closestCheckpointPlayerPosition:F2}, localDirection={localLaunchDirection:F2}, worldDirection={boing.transform.TransformDirection(localLaunchDirection.normalized):F2}, launchVelocity=({launchedPlanarVelocity:F2}, {launchedVerticalVelocity:F2}), closestAt={closestArcTime:F2}s position={closestArcPosition:F2}, collisionFlags={closestArcCollisionFlags}, closestForwardBlockers={closestArcBlockers}, groundedAtClosest={closestArcGrounded}, craftedProofAtClosest={closestArcHasCraftedProof}, groundedAtEnd={motor.IsGrounded}, highestY={highestLaunchY:F2}, initialHitCount={initialHits}, finalHitCount={(int)hitsField.GetValue(boss)}, closestRelayApproach={closestRelayApproach:F2}m, hit={naturalArcHit}.");

                // The first authored Boing flight crosses the high Gulch route.
                // Verify its real checkpoint trigger here, before any later boss
                // recovery or other smoke setup can affect Splat's position.
                var questDirector = FindAnyObjectByType<WorldQuestDirector>();
                if (farCheckpointVolume == null || questDirector == null)
                {
                    Fail("Could not verify the high Rainbow path checkpoint after the first Boing flight");
                }
                else if (!questDirector.HasReachedCheckpoint("Checkpoint_GulchFar"))
                {
                    Fail($"Natural first Boing flight did not activate Checkpoint_GulchFar (Splat ended at {motor.transform.position:F2})");
                }
                else
                {
                    Ok("Natural first Boing flight activates the high Rainbow path checkpoint");
                }

                if (!naturalArcHit && terracePad != null && terracePad.activeInHierarchy
                    && terraceBoing != null && terraceTrigger != null && terraceTrigger.isTrigger)
                {
                    // The first spring should land near the unlocked terrace
                    // spring. Test its authored second leg as a separate real
                    // physics flight so a miss identifies which leg is short.
                    weakPoint.SetExposed(true);
                    Teleport(motor, terraceTrigger.bounds.center);
                    Physics.SyncTransforms();
                    yield return new WaitForFixedUpdate();
                    yield return null;
                    var terraceLaunchConfirmed = motor.WasRecentlyLaunchedByCraftedInvention;
                    var terraceClosest = float.PositiveInfinity;
                    var terraceClosestPosition = motor.transform.position;
                    var terraceHighestY = motor.transform.position.y;
                    var terraceStart = Time.time;
                    for (var arcStep = 0; arcStep < 60
                        && (int)hitsField.GetValue(boss) == initialHits; arcStep++)
                    {
                        var waypoint = weakPointCollider.bounds.center
                            + (motor.transform.position.z < weakPointCollider.bounds.center.z - 1.2f
                                ? Vector3.right * 2.5f : Vector3.zero);
                        var desired = waypoint - motor.transform.position;
                        desired.y = 0f;
                        var captureCamera = Camera.main != null ? Camera.main.transform : null;
                        if (inputReader != null && captureCamera != null && desired.sqrMagnitude > 0.001f)
                        {
                            var forward = captureCamera.forward;
                            var right = captureCamera.right;
                            forward.y = 0f;
                            right.y = 0f;
                            forward.Normalize();
                            right.Normalize();
                            inputReader.DebugSetMove(new Vector2(
                                Vector3.Dot(desired.normalized, right),
                                Vector3.Dot(desired.normalized, forward)));
                        }

                        if (!weakPoint.IsExposed) weakPoint.SetExposed(true);
                        Physics.SyncTransforms();
                        yield return new WaitForFixedUpdate();
                        yield return null;
                        terraceHighestY = Mathf.Max(terraceHighestY, motor.transform.position.y);
                        var closest = weakPointCollider.ClosestPoint(motor.transform.position);
                        var approach = Vector3.Distance(motor.transform.position, closest);
                        if (approach < terraceClosest)
                        {
                            terraceClosest = approach;
                            terraceClosestPosition = motor.transform.position;
                        }
                    }

                    inputReader?.DebugClear();
                    var terraceArcHit = (int)hitsField.GetValue(boss) > initialHits;
                    Debug.Log($"[ArrivalSmoke] Natural Terrace Boing leg with air steering: launchConfirmed={terraceLaunchConfirmed}, pad={terraceTrigger.bounds.center:F2}, relay={weakPointCollider.bounds.center:F2}, closestPosition={terraceClosestPosition:F2}, finalPosition={motor.transform.position:F2}, collisionFlags={playerCollider.collisionFlags}, grounded={motor.IsGrounded}, elapsed={Time.time - terraceStart:F2}s, highestY={terraceHighestY:F2}, closestRelayApproach={terraceClosest:F2}m, hit={terraceArcHit}.");
                }

                GameEvents.InventionUsed -= OnNaturalBoingUsed;
                inputReader?.DebugClear();
            }

            if (boing == null || !craftedLaunchConfirmed || highestLaunchY <= launchOriginY + 0.5f)
            {
                Fail("Crafted Boing trigger did not perform a proven upward launch");
            }
            else
            {
                Ok("Crafted Boing trigger performs a proven upward launch");
            }

            var completed = false;
            var gatewaySignalReceived = false;
            void OnComplete() => completed = true;
            void OnGatewaySignalReceived() => gatewaySignalReceived = true;
            GameEvents.LevelCompleted += OnComplete;
            GameEvents.GatewaySignalReceived += OnGatewaySignalReceived;
            initialHits = hitsField != null ? (int)hitsField.GetValue(boss) : -1;
            if (weakPoint == null || weakPointCollider == null || !weakPointCollider.isTrigger
                || boing == null || boingTrigger == null || playerCollider == null
                || hitsField == null || initialHits < 0)
            {
                Fail("Could not exercise the exposed relay trigger through crafted Boing launches");
            }
            else
            {
                GameEvents.InventionUsed += OnNaturalBoingUsed;
                for (var hitIndex = initialHits; hitIndex < hitsToDefeat; hitIndex++)
                {
                    var hitCountBeforeAttempt = (int)hitsField.GetValue(boss);
                    // Use the nearest active spring and stage just outside its trigger while
                    // Rattle runs the next slam cycle. Waiting idle beside the relay let
                    // Splat fall out of the arena before the next punish window opened.
                    var useTerracePad = terraceTrigger != null && terraceTrigger.enabled
                        && terracePad != null && terracePad.activeInHierarchy
                        && Vector3.Distance(motor.transform.position, terraceTrigger.bounds.center)
                            < Vector3.Distance(motor.transform.position, boingTrigger.bounds.center);
                    var followupTrigger = useTerracePad ? terraceTrigger : boingTrigger;
                    var followupPadName = useTerracePad ? "Terrace" : "Gulch";
                    var followupTarget = followupTrigger.bounds.center;
                    var awayFromBoss = (followupTarget - boss.transform.position);
                    awayFromBoss.y = 0f;
                    if (awayFromBoss.sqrMagnitude > 0.01f)
                    {
                        followupTarget += awayFromBoss.normalized * 2f;
                    }
                    if (Physics.Raycast(followupTarget + Vector3.up * 3f, Vector3.down,
                        out var followupGround, 8f, motor.GroundSupportMask,
                        QueryTriggerInteraction.Ignore))
                    {
                        followupTarget.y = followupGround.point.y + 0.05f;
                    }

                    // This smoke isolates each real pad-to-relay flight. Reset the
                    // test actor onto the nearest safe staging surface before the
                    // boss's next cycle, rather than letting a previous strike's
                    // overshoot/OOB fall decide whether the next flight can start.
                    Teleport(motor, followupTarget);
                    motor.HardResetState();
                    Physics.SyncTransforms();
                    yield return new WaitForFixedUpdate();
                    var launchCountBeforeAttempt = naturalBoingUses;
                    var relayWindowWait = 0f;
                    while (!weakPoint.IsExposed && relayWindowWait < 8f)
                    {
                        if (Vector3.Distance(motor.transform.position, followupTarget) > 1.4f)
                        {
                            if (reviewKeyboard != null)
                            {
                                InputSystem.QueueStateEvent(reviewKeyboard, KeyboardToward(followupTarget));
                            }

                            if (inputReader != null && reviewKeyboardWasAdded
                                && relayWindowWait >= 0.35f && inputReader.Move.sqrMagnitude < 0.01f)
                            {
                                inputReader.DebugSetMove(MoveInputToward(followupTarget));
                            }
                        }
                        else
                        {
                            if (reviewKeyboard != null)
                            {
                                InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
                            }
                            inputReader?.DebugClear();
                        }

                        relayWindowWait += Time.deltaTime;
                        yield return null;
                    }

                    if (!weakPoint.IsExposed)
                    {
                        Fail($"Rattle did not naturally expose the relay before follow-up hit {hitIndex + 1}/{hitsToDefeat}");
                        break;
                    }

                    var followupApproachStart = motor.transform.position;
                    var padApproachWait = 0f;
                    var usedSyntheticFollowupFallback = false;
                    while (naturalBoingUses == launchCountBeforeAttempt && padApproachWait < 3f)
                    {
                        if (reviewKeyboard != null)
                        {
                            InputSystem.QueueStateEvent(reviewKeyboard, KeyboardToward(followupTrigger.bounds.center));
                            if (!usedSyntheticFollowupFallback && reviewKeyboardWasAdded
                                && padApproachWait >= 0.35f && inputReader.Move.sqrMagnitude < 0.01f)
                            {
                                inputReader.DebugSetMove(MoveInputToward(followupTrigger.bounds.center));
                                usedSyntheticFollowupFallback = true;
                                Debug.LogWarning($"[ArrivalSmoke] Synthetic keyboard could not reach the {followupPadName} pad; applying explicit motor-input fallback. This approach is test-assisted, not production keyboard evidence.");
                            }
                        }
                        padApproachWait += Time.deltaTime;
                        yield return null;
                    }
                    if (reviewKeyboard != null)
                    {
                        InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
                    }
                    inputReader?.DebugClear();
                    if (!motor.WasRecentlyLaunchedByCraftedInvention
                        || naturalBoingUses != launchCountBeforeAttempt + 1)
                    {
                        var overlapsPad = followupTrigger.bounds.Intersects(playerCollider.bounds);
                        var launchDelta = naturalBoingUses - launchCountBeforeAttempt;
                        Debug.LogError($"[ArrivalSmoke] Follow-up pad launch diagnostics: hit={hitIndex + 1}/{hitsToDefeat}, pad={followupPadName}, relayWindowWait={relayWindowWait:F2}s, padApproach={padApproachWait:F2}s, start={followupApproachStart:F2}, end={motor.transform.position:F2}, exposed={weakPoint.IsExposed}, padEnabled={(useTerracePad ? terraceBoing.enabled : boing.enabled)}, triggerEnabled={followupTrigger.enabled}, playerColliderEnabled={playerCollider.enabled}, overlapsPad={overlapsPad}, grounded={motor.IsGrounded}, launchProof={motor.WasRecentlyLaunchedByCraftedInvention}, launchDelta={launchDelta}, padBounds={followupTrigger.bounds}.");
                        Fail($"Player did not reach an active Boing pad for relay hit {hitIndex + 1}/{hitsToDefeat}");
                        break;
                    }

                    var arcSteps = 0;
                    var followupClosestRelayApproach = float.PositiveInfinity;
                    var followupClosestRelayPosition = motor.transform.position;
                    var followupCraftedProofAtClosest = false;
                    while (arcSteps < 90
                        && weakPoint.IsExposed
                        && (int)hitsField.GetValue(boss) == hitCountBeforeAttempt)
                    {
                        var launchesThisAttempt = naturalBoingUses - launchCountBeforeAttempt;
                        var target = !useTerracePad && launchesThisAttempt < 2 && terraceTrigger != null
                            ? terraceTrigger.bounds.center
                            : weakPointCollider.bounds.center;
                        if (reviewKeyboard != null)
                        {
                            InputSystem.QueueStateEvent(reviewKeyboard, KeyboardToward(target));
                        }
                        Physics.SyncTransforms();
                        yield return new WaitForFixedUpdate();
                        yield return null;
                        arcSteps++;
                        var relayClosestPoint = weakPointCollider.ClosestPoint(motor.transform.position);
                        var relayApproach = Vector3.Distance(motor.transform.position, relayClosestPoint);
                        if (relayApproach < followupClosestRelayApproach)
                        {
                            followupClosestRelayApproach = relayApproach;
                            followupClosestRelayPosition = motor.transform.position;
                            followupCraftedProofAtClosest = motor.WasRecentlyLaunchedByCraftedInvention;
                        }
                    }

                    inputReader?.DebugClear();
                    var observedHits = (int)hitsField.GetValue(boss);
                    Debug.Log($"[ArrivalSmoke] Natural-window follow-up Boing arc: hit={hitIndex + 1}/{hitsToDefeat}, pad={followupPadName}, relayWindowWait={relayWindowWait:F2}s, padApproach={padApproachWait:F2}s, launchCount={naturalBoingUses - launchCountBeforeAttempt}, physicsSteps={arcSteps}, start={followupApproachStart:F2}, closest={followupClosestRelayApproach:F2}m at {followupClosestRelayPosition:F2}, proofAtClosest={followupCraftedProofAtClosest}, proofAtEnd={motor.WasRecentlyLaunchedByCraftedInvention}, groundedAtEnd={motor.IsGrounded}, end={motor.transform.position:F2}, exposedAtEnd={weakPoint.IsExposed}, hitCount={hitCountBeforeAttempt}->{observedHits}.");
                    if (observedHits != hitCountBeforeAttempt + 1)
                    {
                        Fail($"Two-pad Boing flight did not hit Rattle's natural relay window for boss hit {hitIndex + 1}/{hitsToDefeat}: hits={observedHits}");
                        break;
                    }

                    Ok($"Two-pad Boing flight hit Rattle's natural relay window ({observedHits}/{hitsToDefeat})");
                    yield return null;
                }

                GameEvents.InventionUsed -= OnNaturalBoingUsed;
                inputReader?.DebugClear();
                if (hitsField != null && (int)hitsField.GetValue(boss) == hitsToDefeat)
                {
                    Ok("Follow-up boss relay hits were accepted during natural exposure windows on physics-resolved two-pad Boing flights");
                }

                var relayHitCueCount = TempSfxPlayer.Instance != null
                    ? TempSfxPlayer.Instance.RelayHitCuePlayCountForDiagnostics
                    : 0;
                var expectedRelayHitCues = Mathf.Max(0, hitsToDefeat - bossHitCountAtStart);
                if (relayHitCueCount - relayHitCueCountAtBossStart >= expectedRelayHitCues)
                {
                    Ok($"Relay hit cue played on each registered hit ({relayHitCueCount - relayHitCueCountAtBossStart}/{expectedRelayHitCues})");
                }
                else
                {
                    Fail($"Relay hit cue count did not match registered hits ({relayHitCueCount - relayHitCueCountAtBossStart}/{expectedRelayHitCues})");
                }
            }

            var bossWait = 0f;
            while (!completed && bossWait < 3f)
            {
                bossWait += Time.deltaTime;
                yield return null;
            }

            GameEvents.LevelCompleted -= OnComplete;
            if (!completed)
            {
                Fail("LevelCompleted not raised after boss weak-point hits (with invention)");
            }
            else
            {
                Ok("Level complete after boss exam");
            }

            var worldQuestDirector = FindAnyObjectByType<WorldQuestDirector>();
            if (worldQuestDirector == null)
            {
                Fail("WorldQuestDirector missing after boss completion");
            }
            else if (!worldQuestDirector.IsQuestComplete("arrival_main"))
            {
                Fail($"Arrival main quest still active after complete path (next='{worldQuestDirector.GetActiveBoardLine()}')");
            }
            else
            {
                Ok("Arrival main quest closes after the completed gather/craft/traversal/boss path");
            }

            var objectiveCoach = FindAnyObjectByType<ArrivalObjectiveCoach>();
            if (objectiveCoach == null)
            {
                Fail("ArrivalObjectiveCoach missing after boss completion");
            }
            else if (objectiveCoach.GoalCopyForDiagnostics != "Rainbow gateway open — onward, Splat!")
            {
                Fail($"Arrival victory objective did not replace the boss prompt (goal='{objectiveCoach.GoalCopyForDiagnostics}')");
            }
            else
            {
                Ok("Arrival objective HUD switches to the open-gateway reward after boss completion");
            }

            var isListening = activeMeshyVisual
                ? meshyAnimator.IsPlayingActionForDiagnostics("Listen")
                : skeletonAnimator != null && skeletonAnimator.enabled
                    && skeletonAnimator.CurrentAction == SplatSkeletonAnimator.Action.Listen;
            if (!isListening)
            {
                Fail("Splat did not hold a listening pose for Vesper's gateway reply");
            }
            else
            {
                Ok(activeMeshyVisual
                    ? "Active Meshy Splat entered the held Alert-based Listen state after boss completion"
                    : "Splat listens while the gateway reply arrives");

                var endingWait = 0f;
                var isCelebrating = activeMeshyVisual
                    ? meshyAnimator.IsPlayingActionForDiagnostics("Victory")
                    : skeletonAnimator.CurrentAction == SplatSkeletonAnimator.Action.Celebrate;
                while (!isCelebrating && endingWait < 8f)
                {
                    endingWait += Time.deltaTime;
                    yield return null;
                    isCelebrating = activeMeshyVisual
                        ? meshyAnimator.IsPlayingActionForDiagnostics("Victory")
                        : skeletonAnimator.CurrentAction == SplatSkeletonAnimator.Action.Celebrate;
                }

                if (!isCelebrating)
                {
                    var victoryTriggerPresent = meshyAnimator != null
                        && meshyAnimator.HasTriggerForDiagnostics("Victory");
                    Debug.Log($"[ArrivalSmoke] Ending diagnostics: gatewaySignalReceived={gatewaySignalReceived}, victoryTriggerPresent={victoryTriggerPresent}, controller={(meshyAnimator != null ? meshyAnimator.RuntimeControllerNameForDiagnostics : "missing")}, clip={(meshyAnimator != null ? meshyAnimator.RuntimeClipNameForDiagnostics : "missing")}, pose={(meshyAnimator != null ? meshyAnimator.ReactionPoseKindForDiagnostics : "missing")}");
                    Fail(gatewaySignalReceived
                        ? "Gateway signal arrived, but Splat's active animator did not enter Victory"
                        : "Vesper's gateway reply did not raise GatewaySignalReceived");
                }
                else
                {
                    Ok(activeMeshyVisual
                        ? "Active Meshy Splat celebrates when Vesper's gateway reply begins"
                        : "Splat celebrates when Vesper's three-tap reply begins");
                    if (!activeMeshyVisual)
                    {
                        yield return new WaitForSeconds(0.35f);
                        var eyeRoot = FindTransformByName(skeletonAnimator.transform, "ART_EyeRootL");
                        if (eyeRoot == null || eyeRoot.localScale.y >= 0.9f)
                        {
                            Fail($"Splat's eyes did not soften into a happy squint (Y scale={(eyeRoot != null ? eyeRoot.localScale.y : -1f):0.00})");
                        }
                        else
                        {
                            Ok($"Splat's eyes soften into a happy squint (Y scale={eyeRoot.localScale.y:0.00})");
                        }
                    }

                    const string splatReply = "Splat: “Signal received, map thief. I’m coming.”";
                    endingWait = 0f;
                    while ((GameplayToastHud.Instance == null
                        || GameplayToastHud.Instance.CurrentMessage != splatReply) && endingWait < 2.5f)
                    {
                        endingWait += Time.deltaTime;
                        yield return null;
                    }

                    if (GameplayToastHud.Instance == null || GameplayToastHud.Instance.CurrentMessage != splatReply)
                    {
                        Fail("Splat's written answer did not follow Vesper's three-tap reply");
                    }
                    else
                    {
                        Ok("Splat answers Vesper after the three-tap signal");
                    }
                }
            }

            GameEvents.GatewaySignalReceived -= OnGatewaySignalReceived;

            // Restart reloads Arrival cleanly (R / LevelRestartRequested).
            var sceneName = SceneManager.GetActiveScene().name;
            GameEvents.RaiseLevelRestartRequested();
            var reloadWait = 0f;
            while (reloadWait < 3f)
            {
                reloadWait += Time.unscaledDeltaTime;
                yield return null;
                if (SceneManager.GetActiveScene().isLoaded && FindAnyObjectByType<SplatMotor>() != null)
                {
                    // Give Awake/OnEnable a beat after load.
                    yield return null;
                    break;
                }
            }

            var motorAfter = FindAnyObjectByType<SplatMotor>();
            var sessionAfter = FindAnyObjectByType<LevelSession>();
            if (motorAfter == null || sessionAfter == null || SceneManager.GetActiveScene().name != sceneName)
            {
                Fail("Level restart did not reload Arrival with motor/session");
            }
            else if (sessionAfter.HasInvention("boing_o_matic"))
            {
                Fail("Level restart kept invention state (expected fresh session)");
            }
            else
            {
                Ok("Level restart reloads Arrival with fresh session");
            }

            ReleaseReviewKeyboard();
            Finish(pass, report.ToString());
        }


        private static void Teleport(SplatMotor motor, Vector3 position)
        {
            var controller = motor.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            motor.transform.position = position;
            if (controller != null)
            {
                controller.enabled = true;
            }
        }

        private IEnumerator CharacterizeJumpForgiveness(SplatMotor motor, SplatInputReader inputReader,
            Vector3 originalPosition, System.Action<string> ok, System.Action<string> fail)
        {
            var settings = GetMotorSettings(motor);
            if (settings == null)
            {
                fail("Jump forgiveness characterization is missing SplatMotorSettings");
                yield break;
            }

            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "~SmokeJumpGracePlatform";
            platform.transform.position = originalPosition + new Vector3(35f, 12f, 25f);
            platform.transform.localScale = new Vector3(6f, 0.5f, 6f);
            var platformTop = platform.transform.position.y + platform.transform.localScale.y * 0.5f;
            var groundedPosition = new Vector3(
                platform.transform.position.x,
                platformTop + 0.08f,
                platform.transform.position.z);
            var grounded = false;

            Teleport(motor, groundedPosition);
            motor.HardResetState();
            Physics.SyncTransforms();
            yield return null;
            var settleWait = 0f;
            while (settleWait < 0.3f)
            {
                if (motor.IsGrounded)
                {
                    break;
                }

                settleWait += Time.deltaTime;
                yield return null;
            }
            grounded = motor.IsGrounded;

            if (!grounded)
            {
                fail("Jump grace characterization could not settle Splat on its temporary platform");
                Object.Destroy(platform);
                Teleport(motor, originalPosition);
                motor.HardResetState();
                yield break;
            }

            var coyoteJumped = false;
            void OnCoyoteJumped() => coyoteJumped = true;
            motor.Jumped += OnCoyoteJumped;
            Teleport(motor, groundedPosition + Vector3.right * 4f);
            Physics.SyncTransforms();
            var edgeWait = 0f;
            while (motor.IsGrounded && edgeWait < 0.1f)
            {
                edgeWait += Time.deltaTime;
                yield return null;
            }

            if (motor.IsGrounded)
            {
                fail("Coyote-time characterization did not get Splat clear of the test platform");
            }
            else
            {
                // Pulse early inside the authored 0.15 s coyote window. A
                // longer delay plus a slow editor frame can consume the whole
                // grace period before the production InputAction is sampled.
                yield return new WaitForSeconds(0.03f);
                inputReader.DebugPulseJump(held: true);
                yield return null;
                inputReader.DebugClear();
            }

            motor.Jumped -= OnCoyoteJumped;
            if (coyoteJumped)
            {
                ok("Coyote-time jump succeeds shortly after leaving the platform edge");
            }
            else
            {
                fail("Coyote-time jump did not trigger shortly after leaving the platform edge");
            }

            Teleport(motor, groundedPosition);
            motor.HardResetState();
            Physics.SyncTransforms();
            yield return null;
            var reSettleWait = 0f;
            while (!motor.IsGrounded && reSettleWait < 0.3f)
            {
                reSettleWait += Time.deltaTime;
                yield return null;
            }

            if (!motor.IsGrounded)
            {
                fail("Jump buffer characterization could not resettle Splat on its temporary platform");
                Object.Destroy(platform);
                Teleport(motor, originalPosition);
                motor.HardResetState();
                yield break;
            }

            var bufferedJumped = false;
            void OnBufferedJumped() => bufferedJumped = true;
            motor.Jumped += OnBufferedJumped;
            Teleport(motor, groundedPosition + Vector3.up * 0.72f);
            motor.HardResetState();
            Physics.SyncTransforms();
            var descentWait = 0f;
            while (motor.transform.position.y > platformTop + 0.4f && descentWait < 0.5f)
            {
                descentWait += Time.deltaTime;
                yield return null;
            }

            inputReader.DebugPulseJump(held: true);
            yield return null;
            inputReader.DebugClear();
            var landingJumpWait = 0f;
            while (!bufferedJumped && landingJumpWait < 0.35f)
            {
                landingJumpWait += Time.deltaTime;
                yield return null;
            }

            motor.Jumped -= OnBufferedJumped;
            if (bufferedJumped)
            {
                ok("Jump input pressed shortly before landing is buffered into the next jump");
            }
            else
            {
                fail("Jump input pressed shortly before landing was lost instead of buffered");
            }

            yield return CharacterizePlanarAccelerationAndBraking(motor, inputReader,
                groundedPosition, settings, ok, fail);

            var shortHopGain = 0f;
            var fullJumpGain = 0f;
            yield return MeasureJumpApex(motor, inputReader, groundedPosition, 0.05f,
                gain => shortHopGain = gain);
            yield return MeasureJumpApex(motor, inputReader, groundedPosition, 0.30f,
                gain => fullJumpGain = gain);
            if (shortHopGain > 0.15f && fullJumpGain >= settings.JumpHeight * 0.75f
                && fullJumpGain > shortHopGain + 0.5f)
            {
                ok($"Variable jump height: short={shortHopGain:F2}m, full={fullJumpGain:F2}m");
            }
            else
            {
                fail($"Variable jump height response too small or out of range (short={shortHopGain:F2}m, full={fullJumpGain:F2}m, target={settings.JumpHeight:F2}m)");
            }

            inputReader.DebugClear();
            Object.Destroy(platform);
            Teleport(motor, originalPosition);
            motor.HardResetState();
            Physics.SyncTransforms();
            yield return null;
        }

        private IEnumerator CharacterizePlanarAccelerationAndBraking(SplatMotor motor,
            SplatInputReader inputReader, Vector3 groundedPosition, SplatMotorSettings settings,
            System.Action<string> ok, System.Action<string> fail)
        {
            if (settings == null)
            {
                fail("Ground response characterization is missing SplatMotorSettings");
                yield break;
            }

            var controller = motor.GetComponent<CharacterController>();
            Teleport(motor, groundedPosition);
            motor.HardResetState();
            Physics.SyncTransforms();
            var settle = 0f;
            while (!motor.IsGrounded && settle < 0.3f)
            {
                settle += Time.deltaTime;
                yield return null;
            }

            inputReader.DebugSetMove(Vector2.up);
            yield return null;
            var firstSpeed = motor.PlanarVelocity.magnitude;
            var accelerationElapsed = 0f;
            while (accelerationElapsed < 0.12f)
            {
                accelerationElapsed += Time.deltaTime;
                yield return null;
            }

            var acceleratedSpeed = motor.PlanarVelocity.magnitude;
            inputReader.DebugClear();
            var brakingElapsed = 0f;
            var stopped = motor.PlanarVelocity.magnitude <= 0.5f;
            while (!stopped && brakingElapsed < 0.30f)
            {
                brakingElapsed += Time.deltaTime;
                yield return null;
                stopped = motor.PlanarVelocity.magnitude <= 0.5f;
            }

            if (motor.PlanarVelocity.magnitude <= 0.5f && acceleratedSpeed > firstSpeed + 1f
                && acceleratedSpeed > settings.MaxSpeed * 0.25f && brakingElapsed <= 0.30f)
            {
                ok($"Ground response: speed {firstSpeed:F2}→{acceleratedSpeed:F2}m/s in 0.12s, braked to {motor.PlanarVelocity.magnitude:F2}m/s in {brakingElapsed:F2}s");
            }
            else
            {
                fail($"Ground acceleration/braking response out of range (initial={firstSpeed:F2}m/s, at 0.12s={acceleratedSpeed:F2}m/s, final={motor.PlanarVelocity.magnitude:F2}m/s after {brakingElapsed:F2}s, controller={controller != null})");
            }

            Teleport(motor, groundedPosition);
            motor.HardResetState();
            Physics.SyncTransforms();
            yield return null;
        }

        private IEnumerator MeasureJumpApex(SplatMotor motor, SplatInputReader inputReader,
            Vector3 groundedPosition, float holdSeconds, System.Action<float> reportGain)
        {
            var settings = GetMotorSettings(motor);
            if (settings == null)
            {
                reportGain?.Invoke(0f);
                yield break;
            }

            Teleport(motor, groundedPosition);
            motor.HardResetState();
            Physics.SyncTransforms();
            var settle = 0f;
            while (!motor.IsGrounded && settle < 0.3f)
            {
                settle += Time.deltaTime;
                yield return null;
            }

            var startY = motor.transform.position.y;
            var apexY = startY;
            inputReader.DebugPulseJump(held: true);
            yield return null;
            var held = 0f;
            while (held < holdSeconds)
            {
                held += Time.deltaTime;
                apexY = Mathf.Max(apexY, motor.transform.position.y);
                yield return null;
            }

            inputReader.DebugClear();
            var flight = 0f;
            while (flight < 1.0f && !motor.IsGrounded)
            {
                flight += Time.deltaTime;
                apexY = Mathf.Max(apexY, motor.transform.position.y);
                yield return null;
            }

            apexY = Mathf.Max(apexY, motor.transform.position.y);
            reportGain?.Invoke(apexY - startY);
            Teleport(motor, groundedPosition);
            motor.HardResetState();
            Physics.SyncTransforms();
            yield return null;
        }

        private static Transform FindTransformByName(Transform root, string targetName)
        {
            if (root == null)
            {
                return null;
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == targetName)
                {
                    return transforms[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Meshy showcase FBX×100 SphereColliders (~180m) + Boing/Scrap OnTriggerStay were
        /// ApplyLaunch-storming Splat during jump/camera characterization. Mute only the showcase.
        /// </summary>
        private static System.Collections.Generic.List<(Behaviour behaviour, bool wasEnabled)> MuteMeshyShowcaseLaunchers()
        {
            var muted = new System.Collections.Generic.List<(Behaviour behaviour, bool wasEnabled)>();
            var showcase = GameObject.Find("ART_MeshyCriticalShowcase");
            if (showcase == null)
            {
                return muted;
            }

            void Mute(Behaviour behaviour)
            {
                if (behaviour == null)
                {
                    return;
                }

                muted.Add((behaviour, behaviour.enabled));
                behaviour.enabled = false;
            }

            foreach (var boing in showcase.GetComponentsInChildren<BoingOMatic>(true))
            {
                Mute(boing);
            }

            foreach (var mite in showcase.GetComponentsInChildren<ScrapMite>(true))
            {
                Mute(mite);
            }

            foreach (var floater in showcase.GetComponentsInChildren<ScrapFloater>(true))
            {
                Mute(floater);
            }

            foreach (var roller in showcase.GetComponentsInChildren<ScrapRoller>(true))
            {
                Mute(roller);
            }

            foreach (var col in showcase.GetComponentsInChildren<Collider>(true))
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }

            return muted;
        }

        private static void RestoreMutedBehaviours(
            System.Collections.Generic.List<(Behaviour behaviour, bool wasEnabled)> muted)
        {
            if (muted == null)
            {
                return;
            }

            for (var i = 0; i < muted.Count; i++)
            {
                var (behaviour, wasEnabled) = muted[i];
                if (behaviour != null)
                {
                    behaviour.enabled = wasEnabled;
                }
            }

            // Showcase colliders stay off for the rest of smoke (visual-only; sanitize owns persistence).
        }

        private static SplatMotorSettings GetMotorSettings(SplatMotor motor)
        {
            var field = typeof(SplatMotor).GetField(
                "settings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(motor) as SplatMotorSettings;
        }

        private static string DescribeForwardObstructions(SplatMotor motor, Vector3 direction, float distance)
        {
            var controller = motor != null ? motor.GetComponent<CharacterController>() : null;
            if (controller == null)
            {
                return "character-controller-missing";
            }

            var scale = motor.transform.lossyScale;
            var radius = controller.radius * Mathf.Max(scale.x, scale.z);
            var height = Mathf.Max(controller.height * scale.y, radius * 2f);
            var center = motor.transform.TransformPoint(controller.center);
            var segment = Mathf.Max(0f, height * 0.5f - radius);
            var hits = Physics.CapsuleCastAll(
                    center + Vector3.up * segment,
                    center - Vector3.up * segment,
                    radius,
                    direction,
                    distance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore)
                .Where(hit => hit.collider != null && hit.collider.GetComponentInParent<SplatMotor>() == null)
                .OrderBy(hit => hit.distance)
                .Select(hit => $"{hit.collider.name}@{hit.point:F2}[{hit.distance:F2}m]")
                .Distinct()
                .Take(8);
            return string.Join("; ", hits);
        }

        private void Finish(bool pass, string report)
        {
            var frameTelemetry = BuildFrameTimeTelemetry();
            report += frameTelemetry + "\n" + _focusedFrameReports;
            LastRunPassed = pass;
            LastReport = report;
            Debug.Log("[ArrivalSmoke] Full-run frame time telemetry " + frameTelemetry.Trim());
            if (pass)
            {
                Debug.Log("[ArrivalSmoke] PASSED\n" + report);
            }
            else
            {
                Debug.LogError("[ArrivalSmoke] FAILED\n" + report);
            }

            ArrivalPlayModeSmokeBridge.NotifyFinished(pass, report);
            Destroy(gameObject);
        }

        private string BuildFrameTimeTelemetry()
        {
            return BuildFrameTimeTelemetry(_frameTimeSamplesMs, _frameTimeSampleCount);
        }

        private static string BuildFrameTimeTelemetry(float[] samples, int sampleCount)
        {
            if (sampleCount == 0)
            {
                return "Frame time samples=0 (unavailable)\n";
            }

            var sorted = new float[sampleCount];
            System.Array.Copy(samples, sorted, sampleCount);
            System.Array.Sort(sorted);
            var median = sorted[sampleCount / 2];
            var p95 = sorted[Mathf.Clamp(Mathf.CeilToInt(sampleCount * 0.95f) - 1,
                0, sampleCount - 1)];
            var maximum = sorted[sampleCount - 1];
            return $"samples={sampleCount}, median={median:F2}ms, p95={p95:F2}ms, max={maximum:F2}ms";
        }

        private void StartFocusedFrameTiming(string phase)
        {
            _focusedFramePhase = phase;
            _focusedFrameSampleCount = 0;
            _generationZeroCountAtFocusedStart = System.GC.CollectionCount(0);
            _motorMarkerSampleCount = 0;
            _shadowMarkerSampleCount = 0;
            _mainThreadSampleCount = 0;
            _frameTimingSampleCount = 0;
            for (var markerIndex = 0; markerIndex < _aggregateProfilerCaptures.Count; markerIndex++)
                _aggregateProfilerCaptures[markerIndex].SampleCount = 0;
            for (var counterIndex = 0; counterIndex < _renderCounterCaptures.Count; counterIndex++)
                _renderCounterCaptures[counterIndex].SampleCount = 0;
            _motorMarkerRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts,
                "SplatMotor.Update", 128);
            _shadowMarkerRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts,
                "PlayerLandingShadow.LateUpdate", 128);
            _mainThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal,
                "Main Thread", 128);
            _cameraMarkerRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts,
                "PlatformingCameraRig.LateUpdate", 128);
            _animatorUpdateRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts,
                "SplatSkeletonAnimator.Update", 128);
            _animatorLateUpdateRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts,
                "SplatSkeletonAnimator.LateUpdate", 128);
            _gcAllocatedRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory,
                "GC Allocated In Frame", 128);
            _captureFocusedFrameSamples = true;
        }

        private void StopFocusedFrameTiming()
        {
            _captureFocusedFrameSamples = false;
            var telemetry = BuildFrameTimeTelemetry(_focusedFrameSamplesMs, _focusedFrameSampleCount);
            var line = $"[ArrivalSmoke] Focused frame time ({_focusedFramePhase}): {telemetry}";
            Debug.Log(line);
            _focusedFrameReports.AppendLine(line);
            AppendProfilerTelemetry("SplatMotor.Update", _motorMarkerSamplesMs, _motorMarkerSampleCount);
            AppendProfilerTelemetry("PlayerLandingShadow.LateUpdate", _shadowMarkerSamplesMs, _shadowMarkerSampleCount);
            AppendProfilerTelemetry("Main Thread", _mainThreadSamplesMs, _mainThreadSampleCount);
            AppendProfilerTelemetry("PlatformingCameraRig.LateUpdate", _cameraMarkerSamplesMs, _focusedFrameSampleCount);
            AppendProfilerTelemetry("SplatSkeletonAnimator.Update", _animatorUpdateSamplesMs, _focusedFrameSampleCount);
            AppendProfilerTelemetry("SplatSkeletonAnimator.LateUpdate", _animatorLateUpdateSamplesMs, _focusedFrameSampleCount);
            AppendProfilerTelemetry("FrameTiming CPU Main", _cpuMainFrameSamplesMs, _frameTimingSampleCount);
            AppendProfilerTelemetry("FrameTiming CPU Render", _cpuRenderFrameSamplesMs, _frameTimingSampleCount);
            AppendProfilerTelemetry("FrameTiming GPU", _gpuFrameSamplesMs, _frameTimingSampleCount);
            AppendProfilerTelemetry("FrameTiming Present Wait", _presentWaitSamplesMs, _frameTimingSampleCount);
            AppendProfilerCounterTelemetry("GC Allocated In Frame", _gcAllocatedBytesSamples,
                _gcAllocatedRecorder.Valid ? _focusedFrameSampleCount : 0);
            for (var markerIndex = 0; markerIndex < _aggregateProfilerCaptures.Count; markerIndex++)
            {
                var capture = _aggregateProfilerCaptures[markerIndex];
                AppendProfilerTelemetry(capture.Name, capture.SamplesMs, capture.SampleCount);
            }
            for (var counterIndex = 0; counterIndex < _renderCounterCaptures.Count; counterIndex++)
            {
                var capture = _renderCounterCaptures[counterIndex];
                AppendRenderCounterTelemetry(capture.Name, capture.Samples, capture.SampleCount);
            }
            var previousGenerationZeroCount = _generationZeroCountAtFocusedStart;
            var frameTimingCount = Mathf.Min(_focusedFrameSampleCount, _frameTimingSampleCount);
            for (var i = 0; i < _focusedFrameSampleCount; i++)
            {
                var currentGenerationZeroCount = _gcGenerationZeroSamples[i];
                if (currentGenerationZeroCount > previousGenerationZeroCount)
                {
                    Debug.Log($"[ArrivalSmoke] Focused GC collection during {_focusedFramePhase} sample {i}: gen0={previousGenerationZeroCount}->{currentGenerationZeroCount}");
                }
                previousGenerationZeroCount = currentGenerationZeroCount;
                if (i < frameTimingCount && (_cpuMainFrameSamplesMs[i] >= 25f
                    || _focusedFrameSamplesMs[i] >= 35f))
                {
                    Debug.Log($"[ArrivalSmoke] Focused frame spike ({_focusedFramePhase}, sample {i}): interval={_focusedFrameSamplesMs[i]:F2}ms, cpuMain={_cpuMainFrameSamplesMs[i]:F2}ms, cpuRender={_cpuRenderFrameSamplesMs[i]:F2}ms, gpu={_gpuFrameSamplesMs[i]:F2}ms, gcAllocated={_gcAllocatedBytesSamples[i]:F0}B, gen0={_gcGenerationZeroSamples[i]}");
                }
            }
            _motorMarkerRecorder.Dispose();
            _shadowMarkerRecorder.Dispose();
            _mainThreadRecorder.Dispose();
            _cameraMarkerRecorder.Dispose();
            _animatorUpdateRecorder.Dispose();
            _animatorLateUpdateRecorder.Dispose();
            _gcAllocatedRecorder.Dispose();
        }

        private void OnDestroy()
        {
            for (var markerIndex = 0; markerIndex < _aggregateProfilerCaptures.Count; markerIndex++)
                _aggregateProfilerCaptures[markerIndex].Recorder.Dispose();
            for (var counterIndex = 0; counterIndex < _renderCounterCaptures.Count; counterIndex++)
                _renderCounterCaptures[counterIndex].Recorder.Dispose();
        }

        private void AppendProfilerTelemetry(string markerName, float[] samples, int count)
        {
            var telemetry = BuildFrameTimeTelemetry(samples, count);
            var line = $"[ArrivalSmoke] Focused profiler marker ({_focusedFramePhase}, {markerName}): {telemetry}";
            Debug.Log(line);
            _focusedFrameReports.AppendLine(line);
        }

        private int _generationZeroCountAtFocusedStart;

        private void AppendProfilerCounterTelemetry(string counterName, float[] samples, int count)
        {
            var line = count > 0
                ? $"[ArrivalSmoke] Focused profiler counter ({_focusedFramePhase}, {counterName}): samples={count}, median={Median(samples, count):F0}B, p95={Percentile95(samples, count):F0}B, max={Maximum(samples, count):F0}B"
                : $"[ArrivalSmoke] Focused profiler counter ({_focusedFramePhase}, {counterName}): unavailable";
            Debug.Log(line);
            _focusedFrameReports.AppendLine(line);
        }

        private void AppendRenderCounterTelemetry(string counterName, long[] samples, int count)
        {
            if (count <= 0)
            {
                var unavailable = $"[ArrivalSmoke] Focused render counter ({_focusedFramePhase}, {counterName}): unavailable";
                Debug.Log(unavailable);
                _focusedFrameReports.AppendLine(unavailable);
                return;
            }

            var sorted = new long[count];
            System.Array.Copy(samples, sorted, count);
            System.Array.Sort(sorted);
            var median = sorted[(count - 1) / 2];
            var p95 = sorted[Mathf.Clamp(Mathf.CeilToInt(count * 0.95f) - 1, 0, count - 1)];
            var line = $"[ArrivalSmoke] Focused render counter ({_focusedFramePhase}, {counterName}): samples={count}, median={median}, p95={p95}, max={sorted[count - 1]}";
            Debug.Log(line);
            _focusedFrameReports.AppendLine(line);
        }

        private static float Median(float[] samples, int count)
        {
            var sorted = new float[count];
            System.Array.Copy(samples, sorted, count);
            System.Array.Sort(sorted);
            return sorted[(count - 1) / 2];
        }

        private static float Percentile95(float[] samples, int count)
        {
            var sorted = new float[count];
            System.Array.Copy(samples, sorted, count);
            System.Array.Sort(sorted);
            return sorted[Mathf.Clamp(Mathf.CeilToInt(count * 0.95f) - 1, 0, count - 1)];
        }

        private static float Maximum(float[] samples, int count)
        {
            var maximum = 0f;
            for (var i = 0; i < count; i++) maximum = Mathf.Max(maximum, samples[i]);
            return maximum;
        }
    }

    /// <summary>Editor bridge hooks without putting Editor code in this assembly.</summary>
    public static class ArrivalPlayModeSmokeBridge
    {
        public static System.Action<bool, string> Finished;

        public static void NotifyFinished(bool pass, string report) => Finished?.Invoke(pass, report);
    }
}
