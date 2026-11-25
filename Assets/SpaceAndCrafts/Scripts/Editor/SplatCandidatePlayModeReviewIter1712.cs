#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SpaceAndCrafts.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Creates an unsaved, disposable scene and records a visual-review pass
    /// through the Iter1711 candidate using the real player motor and driver.
    /// No production prefab, player, or scene is modified.
    /// </summary>
    public static class SplatCandidatePlayModeReviewIter1712
    {
        private const string ModelPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1711/Splat_UnifiedRig_Candidate_Iter1711.fbx";
        private const string ControllerPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1711/Splat_UnifiedRig_Candidate_Iter1711.controller";
        private const string InputPath = "Assets/SpaceAndCrafts/Input/SplatInputActions.inputactions";
        private const string SettingsPath = "Assets/SpaceAndCrafts/ScriptableObjects/SplatMotorSettings.asset";
        private const string ShaderPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/SplatCandidateVertexColorPreview.shader";
        private const string CaptureRootName = "Splat Iter1712 Isolated Play Mode Review";
        [MenuItem("Space & Crafts/Capture Iter1711 Splat Play Mode Review")]
        public static void Run()
        {
            PrepareCandidateAssets();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var settings = AssetDatabase.LoadAssetAtPath<SplatMotorSettings>(SettingsPath);
            if (model == null || controller == null || avatar == null || !avatar.isValid || inputActions == null || settings == null)
                throw new InvalidOperationException("Iter1711 candidate model/controller/avatar, input actions, or motor settings are unavailable.");
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Refusing to start a candidate review while another Play Mode session is active.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(CaptureRootName);

            var support = GameObject.CreatePrimitive(PrimitiveType.Plane);
            support.name = "Temporary visual-review support";
            support.layer = 0;
            support.transform.position = Vector3.zero;
            support.transform.localScale = new Vector3(5f, 1f, 5f);
            SetMaterialColor(support.GetComponent<Renderer>(), new Color(0.18f, 0.25f, 0.34f));

            // Keep the root inactive while adding/configuring components so
            // OnEnable does not see an unassigned input-action asset.
            var player = new GameObject("SplatMotor review root");
            player.SetActive(false);
            var controllerCapsule = player.AddComponent<CharacterController>();
            controllerCapsule.skinWidth = SplatMotor.PlatformContactSkinWidth;
            controllerCapsule.stepOffset = 0.22f;
            controllerCapsule.slopeLimit = 50f;
            var input = player.AddComponent<SplatInputReader>();
            SetSerializedReference(input, "inputActions", inputActions);
            var motor = player.AddComponent<SplatMotor>();
            SetSerializedReference(motor, "settings", settings);
            SetSerializedReference(motor, "input", input);

            var visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (visual == null)
                throw new InvalidOperationException("Could not instantiate Iter1711 FBX in the disposable review scene.");
            visual.name = "Splat Iter1711 review candidate";
            visual.transform.SetParent(player.transform, false);
            var animator = visual.GetComponent<Animator>() ?? visual.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = visual.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            ApplyPreviewMaterials(visual);

            var renderBounds = CalculateRendererBounds(visual);
            controllerCapsule.radius = 0.48f;
            controllerCapsule.height = Mathf.Max(1.45f, renderBounds.size.y * 0.86f);
            var footOffset = -renderBounds.min.y;
            controllerCapsule.center = new Vector3(0f, controllerCapsule.height * 0.5f - footOffset, 0f);
            player.transform.position = Vector3.up * footOffset;

            // Input is camera-relative in production. An identity basis keeps
            // the scripted forward run facing the +Z camera used in this review.
            var movementBasis = new GameObject("Review movement basis");
            var serializedMotorWithBasis = new SerializedObject(motor);
            serializedMotorWithBasis.FindProperty("cameraTransform").objectReferenceValue = movementBasis.transform;
            serializedMotorWithBasis.ApplyModifiedPropertiesWithoutUndo();
            player.transform.SetParent(root.transform, true);

            var lightObject = new GameObject("Temporary review key light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.color = new Color(0.92f, 0.94f, 1f);
            lightObject.transform.rotation = Quaternion.Euler(34f, -28f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.50f, 0.57f);

            var cameraObject = new GameObject("Temporary review camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.068f, 0.12f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(1.15f, renderBounds.size.y * 0.86f);
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 100f;
            var renderTexture = new RenderTexture(960, 960, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            camera.targetTexture = renderTexture;

            // The references above are scene-only transient object overrides. Mark
            // the disposable scene dirty so Unity includes them in its Play Mode
            // backup/restore serialization before reloading the domain.
            EditorSceneManager.MarkSceneDirty(player.scene);
            player.SetActive(true);
            EditorPrefs.SetBool(SplatCandidatePlayModeReviewSequencerIter1712.ArmedKey, true);
            EditorApplication.delayCall += EditorApplication.EnterPlaymode;
        }

        private static void SetSerializedReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            var property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Could not find serialized field '{propertyName}' on {target.GetType().Name}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Read the component back before activation: if Unity rejected the transient
            // reference, abort instead of capturing a misleading stationary preview.
            serialized.Update();
            property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue != value)
                throw new InvalidOperationException($"Serialized field '{propertyName}' was not retained on {target.GetType().Name}.");
        }

        private static void PrepareCandidateAssets()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException($"Unity did not import {ModelPath} as an FBX model.");

            var changed = false;
            if (!importer.importAnimation || importer.animationType != ModelImporterAnimationType.Generic ||
                importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.importAnimation = true;
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }
            var clipSettings = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            foreach (var clip in clipSettings)
            {
                var name = clip.name ?? string.Empty;
                var shouldLoop = name.EndsWith("Idle", StringComparison.OrdinalIgnoreCase) || name.EndsWith("Run", StringComparison.OrdinalIgnoreCase);
                if (clip.loopTime != shouldLoop || clip.loopPose != shouldLoop)
                {
                    clip.loopTime = shouldLoop;
                    clip.loopPose = shouldLoop;
                    changed = true;
                }
            }
            if (changed)
            {
                importer.clipAnimations = clipSettings;
                importer.SaveAndReimport();
            }

            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).ToArray();
            var idle = FindClip(clips, "Idle");
            var run = FindClip(clips, "Run");
            var jump = FindClip(clips, "Jump");
            var land = FindClip(clips, "Land");
            if (idle == null || run == null || jump == null || land == null)
                throw new InvalidOperationException("Iter1711 FBX did not import distinct Idle/Run/Jump/Land clips.");
            if (!idle.isLooping || !run.isLooping || jump.isLooping || land.isLooping)
                throw new InvalidOperationException("Imported loop settings are wrong; expected looping Idle/Run only.");

            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            machine.states = Array.Empty<ChildAnimatorState>();
            var locomotion = new BlendTree
            {
                name = "Splat Iter1711 Idle Run",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "MoveSpeed",
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(locomotion, controller);
            locomotion.AddChild(idle, 0f);
            locomotion.AddChild(run, 2f);
            var locomotionState = machine.AddState("Locomotion", new Vector3(180f, 0f));
            locomotionState.motion = locomotion;
            var jumpState = machine.AddState("Jump", new Vector3(420f, 100f));
            jumpState.motion = jump;
            var landState = machine.AddState("Land", new Vector3(680f, 100f));
            landState.motion = land;
            machine.defaultState = locomotionState;
            var takeoff = machine.AddAnyStateTransition(jumpState);
            takeoff.hasExitTime = false;
            takeoff.duration = 0.08f;
            takeoff.canTransitionToSelf = false;
            takeoff.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            var touchdown = jumpState.AddTransition(landState);
            touchdown.hasExitTime = false;
            touchdown.duration = 0.07f;
            touchdown.AddCondition(AnimatorConditionMode.If, 0f, "Land");
            var recover = landState.AddTransition(locomotionState);
            recover.hasExitTime = true;
            recover.exitTime = 0.90f;
            recover.duration = 0.10f;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static AnimationClip FindClip(AnimationClip[] clips, string suffix) =>
            clips.FirstOrDefault(clip => clip.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Iter1711 model has no renderers.");
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void ApplyPreviewMaterials(GameObject root)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
                throw new InvalidOperationException("The existing preview vertex-color shader is unavailable.");
            var material = new Material(shader) { name = "Iter1712 Review Vertex Color Material" };
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
        }

        private static void SetMaterialColor(Renderer renderer, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            renderer.sharedMaterial = material;
        }
    }

    [InitializeOnLoad]
    internal static class SplatCandidatePlayModeReviewSequencerIter1712
    {
        internal const string ArmedKey = "SpaceCrafts.SplatCandidateIter1712.Armed";
        private const string CaptureRootName = "Splat Iter1712 Isolated Play Mode Review";
        private const string OutputDirectory = "Logs/score-comparison/splat-candidate-playmode-review-iter1712";
        private const string InputPath = "Assets/SpaceAndCrafts/Input/SplatInputActions.inputactions";
        private const string SettingsPath = "Assets/SpaceAndCrafts/ScriptableObjects/SplatMotorSettings.asset";
        private static bool _active;
        private static double _startedAt;
        private static double _landingAt = -1d;
        private static bool _idleCaptured;
        private static bool _runCaptured;
        private static bool _jumpCaptured;
        private static bool _jumpPulsed;
        private static bool _sawAirborne;
        private static bool _landCaptured;
        private static bool _settleCaptured;
        private static bool _visualAlignmentApplied;
        private static float _visualAlignmentOffset;
        private static Transform _player;
        private static GameObject _visual;
        private static SplatInputReader _input;
        private static SplatMotor _motor;
        private static Animator _animator;
        private static Camera _camera;
        private static RenderTexture _renderTexture;
        private static float _supportTop;
        private static readonly StringBuilder Report = new();

        static SplatCandidatePlayModeReviewSequencerIter1712()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += TryStartSequence;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                EditorApplication.delayCall += TryStartSequence;
            else if (state == PlayModeStateChange.EnteredEditMode && EditorPrefs.GetBool(ArmedKey))
            {
                EditorPrefs.DeleteKey(ArmedKey);
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }

        private static void TryStartSequence()
        {
            if (!EditorApplication.isPlaying || !EditorPrefs.GetBool(ArmedKey) || _active)
                return;
            var root = GameObject.Find(CaptureRootName);
            _player = root != null ? root.transform.Find("SplatMotor review root") : null;
            _visual = _player != null ? _player.Find("Splat Iter1711 review candidate")?.gameObject : null;
            if (_player == null || _visual == null)
                return;

            _input = _player.GetComponent<SplatInputReader>();
            _motor = _player.GetComponent<SplatMotor>();
            _animator = _visual.GetComponent<Animator>() ?? _visual.GetComponentInChildren<Animator>(true);
            var cameraObject = GameObject.Find("Temporary review camera");
            _camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
            _renderTexture = _camera != null ? _camera.targetTexture : null;
            var support = GameObject.Find("Temporary visual-review support");
            _supportTop = support != null ? support.transform.position.y : 0f;
            if (_input == null || _motor == null || _animator == null || _camera == null || _renderTexture == null)
            {
                Report.AppendLine("error=review scene is missing the player motor/input/animator/camera/render target");
                Finish();
                return;
            }

            // New, unsaved scenes can discard SerializedObject component
            // references during the Editor-to-PlayMode scene backup. Rebind
            // these three references after domain reload, in this disposable
            // scene only, before collecting any transition evidence.
            SetRuntimeField(_input, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath));
            SetRuntimeField(_motor, "settings", AssetDatabase.LoadAssetAtPath<SplatMotorSettings>(SettingsPath));
            SetRuntimeField(_motor, "input", _input);

            Directory.CreateDirectory(OutputDirectory);
            Report.Clear();
            Report.AppendLine("scope=isolated Editor Play Mode visual review only; no production scenes/prefabs touched");
            Report.AppendLine("candidate=Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1711/Splat_UnifiedRig_Candidate_Iter1711.fbx");
            Report.AppendLine($"reviewPipeline={(GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "Built-in")}; supportTopY={_supportTop:F5}; initialRootY={_player.position.y:F5}; initialGrounded={_motor.IsGrounded}");
            var motorSettings = typeof(SplatMotor).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_motor);
            var motorInput = typeof(SplatMotor).GetField("input", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_motor);
            Report.AppendLine($"runtimeWiring=settingsAssigned={motorSettings != null} inputAssigned={motorInput != null} inputEnabled={_input.enabled} inputAssetAssigned={typeof(SplatInputReader).GetField("inputActions", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_input) != null}");
            _startedAt = EditorApplication.timeSinceStartup;
            _landingAt = -1d;
            _idleCaptured = _runCaptured = _jumpCaptured = _jumpPulsed = _sawAirborne = _landCaptured = _settleCaptured = false;
            _visualAlignmentApplied = false;
            _visualAlignmentOffset = 0f;
            _input.DebugSetMove(Vector2.zero);
            _active = true;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!_active || !EditorApplication.isPlaying)
                return;
            var elapsed = EditorApplication.timeSinceStartup - _startedAt;
            if (!_visualAlignmentApplied && elapsed >= 0.25d)
            {
                var soleMinY = MeasureBakedMeshMinimumY();
                if (!float.IsNaN(soleMinY) && !float.IsInfinity(soleMinY))
                {
                    _visualAlignmentOffset = _supportTop - soleMinY;
                    _visual.transform.position += Vector3.up * _visualAlignmentOffset;
                    Report.AppendLine($"previewOnlyVisualAlignment=offsetY:{_visualAlignmentOffset:F5} preOffsetSoleMinY:{soleMinY:F5}; imported FBX asset left unchanged");
                }
                else
                {
                    Report.AppendLine("previewOnlyVisualAlignment=unavailable; no skinned sole vertices found");
                }
                _visualAlignmentApplied = true;
            }
            if (elapsed < 0.55d)
            {
                _input.DebugSetMove(Vector2.zero);
            }
            else if (elapsed < 1.10d)
            {
                if (!_idleCaptured)
                {
                    Capture("idle");
                    _idleCaptured = true;
                }
                _input.DebugSetMove(Vector2.up);
            }
            else
            {
                if (!_runCaptured)
                {
                    Capture("run");
                    _runCaptured = true;
                    _input.DebugSetMove(Vector2.zero);
                }
                if (elapsed > 1.36d && !_jumpPulsed)
                {
                    _input.DebugSetMove(Vector2.zero);
                    _input.DebugPulseJump();
                    _jumpPulsed = true;
                    Report.AppendLine($"jumpPulseAt={elapsed:F3}s");
                }
            }

            if (_jumpPulsed && !_motor.IsGrounded)
                _sawAirborne = true;
            if (_jumpPulsed && elapsed > 1.64d && !_jumpCaptured)
            {
                Capture("jump");
                _jumpCaptured = true;
            }
            if (_sawAirborne && _motor.IsGrounded && _landingAt < 0d)
                _landingAt = EditorApplication.timeSinceStartup;
            if (_landingAt >= 0d && !_landCaptured && EditorApplication.timeSinceStartup - _landingAt > 0.10d)
            {
                Capture("land-impact");
                _landCaptured = true;
            }
            if (_landingAt >= 0d && !_settleCaptured && EditorApplication.timeSinceStartup - _landingAt > 1.08d)
            {
                Capture("land-settle");
                _settleCaptured = true;
                Finish();
            }
            else if (_jumpPulsed && elapsed > 4.0d && !_settleCaptured)
            {
                Report.AppendLine($"landingTimeout=true sawAirborne={_sawAirborne} grounded={_motor.IsGrounded}; captures are not a completed transition sequence");
                Finish();
            }
        }

        private static void SetRuntimeField(UnityEngine.Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || value == null)
                throw new InvalidOperationException($"Cannot bind isolated review field {target.GetType().Name}.{fieldName}; target asset or field is missing.");
            field.SetValue(target, value);
        }

        private static void Capture(string label)
        {
            FollowCamera();
            _camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            var image = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
            image.Apply(false, false);
            var imagePath = Path.Combine(OutputDirectory, label + ".png");
            File.WriteAllBytes(imagePath, image.EncodeToPNG());
            RenderTexture.active = previous;
            UnityEngine.Object.Destroy(image);

            var state = _animator.GetCurrentAnimatorStateInfo(0);
            var clipInfo = _animator.GetCurrentAnimatorClipInfo(0).FirstOrDefault();
            var meshMin = MeasureBakedMeshMinimumY();
            var cc = _motor.GetComponent<CharacterController>();
            Report.AppendLine($"frame={label} animatorState={StateName(state)} transition={_animator.IsInTransition(0)} clip={(clipInfo.clip != null ? clipInfo.clip.name : "none")} speed={_motor.PlanarVelocity.magnitude:F3} grounded={_motor.IsGrounded} verticalVelocity={_motor.VerticalVelocity:F3} rootY={_player.position.y:F4} capsuleMinY={cc.bounds.min.y:F4} skinnedSoleMinY={meshMin:F5} soleGapToSupport={meshMin - _supportTop:F5} file={imagePath}");
        }

        private static void FollowCamera()
        {
            var target = _player.position + Vector3.up * 0.82f;
            _camera.transform.position = target + new Vector3(2.25f, 1.35f, 5.8f);
            _camera.transform.LookAt(target);
        }

        private static float MeasureBakedMeshMinimumY()
        {
            var minimum = float.PositiveInfinity;
            var found = false;
            foreach (var renderer in _visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh == null)
                    continue;
                var baked = new Mesh { name = "Iter1712 transient sole measurement" };
                renderer.BakeMesh(baked);
                var vertices = baked.vertices;
                for (var i = 0; i < vertices.Length; i++)
                    minimum = Mathf.Min(minimum, renderer.transform.TransformPoint(vertices[i]).y);
                found |= vertices.Length > 0;
                UnityEngine.Object.Destroy(baked);
            }
            return found ? minimum : float.NaN;
        }

        private static string StateName(AnimatorStateInfo state)
        {
            if (state.IsName("Land")) return "Land";
            if (state.IsName("Jump")) return "Jump";
            if (state.IsName("Locomotion")) return "Locomotion";
            return state.shortNameHash.ToString();
        }

        private static void Finish()
        {
            if (!_active)
                return;
            _active = false;
            EditorApplication.update -= Tick;
            File.WriteAllText(Path.Combine(OutputDirectory, "playmode-review.txt"), Report.ToString());
            if (_camera != null)
                _camera.targetTexture = null;
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(_renderTexture);
            }
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
