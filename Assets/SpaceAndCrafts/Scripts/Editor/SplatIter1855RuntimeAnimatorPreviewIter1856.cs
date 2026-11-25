#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Capture-only Animator.Update preview of the ReviewOnly Splat state machine.</summary>
    public static class SplatIter1855RuntimeAnimatorPreviewIter1856
    {
        private const string PrefabPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1855/ReviewOnly/Splat_Iter1855_Rigged_ReviewOnly.prefab";
        private const string RigPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1855/Splat_Iter1855_Rigged.fbx";
        private const string AlternativeActionsPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1855/AnimationCandidates/JumpLand_CustomPrime_01/Splat_Iter1855_RegularJump_JumpingDown_Bundle.fbx";
        private const string AlternativeControllerPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1855/ReviewOnly/Splat_Iter1855_RegularJump_Land_ReviewOnly.controller";
        private const string OutputPath = "Logs/score-comparison/splat-meshy-iter1855-reviewonly/splat-iter1855-runtime-animator-preview-iter1856.png";
        private const string ReportPath = "Logs/score-comparison/splat-meshy-iter1855-reviewonly/runtime-animator-preview-iter1856.txt";

        private static readonly string[] Labels = { "Idle", "Walk", "Run", "Jump Takeoff", "Jump Mid", "Land Entry", "Land Settle", "Return Idle" };

        [MenuItem("Space & Crafts/Preview Splat Iter1855 ReviewOnly Animator")]
        public static void Capture()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var avatar = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>().FirstOrDefault(a => a != null && a.isValid);
                var actionsImporter = AssetImporter.GetAtPath(AlternativeActionsPath) as ModelImporter;
                if (avatar == null || actionsImporter == null) throw new InvalidOperationException("Missing source Avatar or alternate Meshy action bundle.");
                actionsImporter.animationType = ModelImporterAnimationType.Generic;
                actionsImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                actionsImporter.sourceAvatar = avatar;
                actionsImporter.importAnimation = true;
                actionsImporter.SaveAndReimport();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var alternateClips = AssetDatabase.LoadAllAssetsAtPath(AlternativeActionsPath).OfType<AnimationClip>()
                    .Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).ToArray();
                var regularJump = alternateClips.FirstOrDefault(c => c.name.IndexOf("Regular", StringComparison.OrdinalIgnoreCase) >= 0 && c.name.IndexOf("Jump", StringComparison.OrdinalIgnoreCase) >= 0);
                var jumpDown = alternateClips.FirstOrDefault(c => c.name.IndexOf("Jumping_Down", StringComparison.OrdinalIgnoreCase) >= 0 || c.name.IndexOf("Jumping Down", StringComparison.OrdinalIgnoreCase) >= 0);
                if (regularJump == null || jumpDown == null) throw new InvalidOperationException("Could not identify Regular Jump and Jumping Down in the imported bundle: " + string.Join(", ", alternateClips.Select(c => c.name)));
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null) throw new InvalidOperationException("Missing ReviewOnly candidate prefab: " + PrefabPath);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var splat = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (splat == null) throw new InvalidOperationException("Could not instantiate ReviewOnly Splat prefab.");
                splat.transform.position = new Vector3(0f, .2374f, 0f);
                splat.transform.rotation = Quaternion.identity;
                var animator = splat.GetComponentInChildren<Animator>(true);
                if (animator == null || animator.runtimeAnimatorController == null || animator.avatar == null || !animator.avatar.isValid)
                    throw new InvalidOperationException("ReviewOnly Animator, controller or valid Avatar is missing.");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 1f;
                var controller = animator.runtimeAnimatorController;
                foreach (var p in controller.animationClips) if (p == null) throw new InvalidOperationException("Controller contains a missing clip.");
                var originalControllerPath = AssetDatabase.GetAssetPath(controller);
                if (string.IsNullOrEmpty(originalControllerPath)) throw new InvalidOperationException("ReviewOnly controller has no asset path.");
                if (AssetDatabase.LoadMainAssetAtPath(AlternativeControllerPath) != null) AssetDatabase.DeleteAsset(AlternativeControllerPath);
                if (!AssetDatabase.CopyAsset(originalControllerPath, AlternativeControllerPath)) throw new InvalidOperationException("Could not copy ReviewOnly controller for the alternative action pass.");
                var alternativeController = AssetDatabase.LoadAssetAtPath<AnimatorController>(AlternativeControllerPath);
                if (alternativeController == null) throw new InvalidOperationException("Copied alternative controller could not be loaded.");
                var machine = alternativeController.layers[0].stateMachine;
                var jumpState = machine.states.Select(s => s.state).FirstOrDefault(s => s != null && s.name == "Jump");
                var landState = machine.states.Select(s => s.state).FirstOrDefault(s => s != null && s.name == "Land");
                if (jumpState == null || landState == null) throw new InvalidOperationException("Copied controller is missing Jump or Land states.");
                jumpState.motion = regularJump;
                landState.motion = jumpDown;
                EditorUtility.SetDirty(alternativeController);
                AssetDatabase.SaveAssets();
                animator.runtimeAnimatorController = alternativeController;
                animator.avatar = avatar;
                animator.Rebind();
                animator.Update(0f);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "AnimatorPreviewFloor_NotSaved";
                floor.transform.localScale = Vector3.one * 4f;
                var floorMat = MakeMaterial(new Color(.26f, .29f, .34f));
                floor.GetComponent<Renderer>().sharedMaterial = floorMat;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.20f, .22f, .26f);
                AddLight(new Vector3(25f, 125f, -18f), .75f);
                AddLight(new Vector3(-30f, 18f, 48f), .26f);
                var camera = new GameObject("AnimatorPreviewCamera_NotSaved").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.08f, .10f, .14f, 1f);
                camera.orthographic = true;
                camera.allowHDR = true;
                camera.nearClipPlane = .02f;
                camera.farClipPlane = 80f;
                camera.orthographicSize = 1.10f;
                camera.transform.position = new Vector3(0f, .76f, 4.6f);
                camera.transform.LookAt(new Vector3(0f, .76f, 0f));

                animator.SetFloat("MoveSpeed", 0f);
                animator.Play("Locomotion", 0, 0f);
                animator.Update(0f);
                var notes = new StringBuilder();
                notes.AppendLine("ReviewOnly runtime Animator.Update sequence; not game Play Mode, player input or human play.");
                notes.AppendLine("Animator enabled=" + animator.enabled + ", Avatar valid=" + animator.avatar.isValid + ", type=" + animator.avatar.GetType().Name + ", controller=" + alternativeController.name + ", applyRootMotion=" + animator.applyRootMotion + ".");
                notes.AppendLine("Controller clips: " + string.Join(", ", alternativeController.animationClips.Where(c => c != null).Select(c => c.name + "(" + c.length.ToString("F2") + "s)")));
                notes.AppendLine("Jump/Land states replaced only in a copied ReviewOnly controller with same-rig action clips: Jump=" + regularJump.name + "(" + regularJump.length.ToString("F2") + "s), Land=" + jumpDown.name + "(" + jumpDown.length.ToString("F2") + "s). Bundle clips=[" + string.Join(", ", alternateClips.Select(c => c.name)) + "].");
                var frames = new Texture2D(1536, 1536, TextureFormat.RGB24, false);
                int captureIndex = 0;
                Action<string> capture = label =>
                {
                    var info = animator.GetCurrentAnimatorStateInfo(0);
                    var clips = animator.GetCurrentAnimatorClipInfo(0);
                    var renderers = splat.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
                    var b = BoundsOf(renderers);
                    var tileTarget = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
                    var tile = new Texture2D(512, 512, TextureFormat.RGB24, false);
                    camera.targetTexture = tileTarget;
                    camera.Render();
                    RenderTexture.active = tileTarget;
                    tile.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                    tile.Apply();
                    frames.SetPixels((captureIndex % 3) * 512, (2 - captureIndex / 3) * 512, 512, 512, tile.GetPixels());
                    camera.targetTexture = null;
                    RenderTexture.active = null;
                    tileTarget.Release();
                    UnityEngine.Object.DestroyImmediate(tileTarget);
                    UnityEngine.Object.DestroyImmediate(tile);
                    notes.AppendLine(label + ": state=" + StateName(animator)
                        + ", normalized=" + info.normalizedTime.ToString("F2") + ", rootY=" + splat.transform.position.y.ToString("F3")
                        + "m, boundsMinY=" + b.min.y.ToString("F3") + "m, clips=[" + string.Join(", ", clips.Select(c => c.clip.name + "@" + c.weight.ToString("F2"))) + "]");
                    captureIndex++;
                };
                Action<float> advance = seconds =>
                {
                    var ticks = Mathf.Max(1, Mathf.CeilToInt(seconds * 60f));
                    for (var i = 0; i < ticks; i++) animator.Update(seconds / ticks);
                };
                advance(.55f); capture(Labels[0]);
                animator.SetFloat("MoveSpeed", .75f); advance(.70f); capture(Labels[1]);
                animator.SetFloat("MoveSpeed", 1.8f); advance(.70f); capture(Labels[2]);
                animator.SetFloat("MoveSpeed", 0f); advance(.50f);
                animator.SetTrigger("Jump"); advance(.12f); capture(Labels[3]);
                advance(.34f); capture(Labels[4]);
                animator.SetTrigger("Land"); advance(.10f); capture(Labels[5]);
                advance(.34f); capture(Labels[6]);
                advance(1.05f); capture(Labels[7]);
                frames.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);
                File.WriteAllBytes(OutputPath, frames.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(frames);
                notes.AppendLine("Eight-frame staged Editor capture: " + OutputPath + ". It uses the ReviewOnly prefab/controller and Animator.Update; this is not an Arrival scene, SplatMotor integration, build, formal test or human-play check.");
                notes.AppendLine("No production prefab, scene or controller changed. Character height/contact is measured against a temporary flat floor only; varied surfaces and foot IK remain unverified.");
                File.WriteAllText(ReportPath, notes.ToString());
                Debug.Log(notes.ToString());
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static Bounds BoundsOf(Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0) throw new InvalidOperationException("No visible Splat renderers.");
            var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        private static Material MakeMaterial(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }

        private static string StateName(Animator animator)
        {
            var hash = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            foreach (var name in new[] { "Locomotion", "Jump", "Land", "Victory", "Personality" })
                if (Animator.StringToHash(name) == hash) return name;
            return "unknown(" + hash + ")";
        }

        private static void AddLight(Vector3 direction, float intensity)
        {
            var go = new GameObject("AnimatorPreviewLight_NotSaved");
            go.transform.rotation = Quaternion.LookRotation(-direction.normalized);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = new Color(1f, .94f, .87f);
        }
    }
}
#endif
