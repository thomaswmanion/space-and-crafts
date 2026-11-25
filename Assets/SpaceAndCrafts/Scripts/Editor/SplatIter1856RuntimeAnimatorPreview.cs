#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Eight live Animator.Update frames from the isolated Iter1856 ReviewOnly controller.</summary>
    public static class SplatIter1856RuntimeAnimatorPreview
    {
        private const string PrefabPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1856/ReviewOnly/Splat_Iter1856_Rigged_ReviewOnly.prefab";
        private const string ControllerPath = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1856/Splat_Iter1856_Playable.controller";
        private const string TexturePath = "Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1856/Splat_Iter1856_GameReady_base_color.png";
        private const string OutputPath = "Logs/score-comparison/splat-meshy-iter1856-reviewonly/runtime-animator-sequence.png";
        private const string ReportPath = "Logs/score-comparison/splat-meshy-iter1856-reviewonly/runtime-animator-sequence.txt";
        private static readonly string[] Labels = { "Idle", "Walk", "Run", "Jump Takeoff", "Jump Mid", "Land Entry", "Land Settle", "Return Idle", "Pickup", "Dodge", "Air Attack", "Run-Jump-Roll", "Hit Reaction", "Victory" };

        [MenuItem("Space & Crafts/Preview Splat Iter1856 ReviewOnly Animator")]
        public static void Capture()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
                if (prefab == null || texture == null || controller == null) throw new InvalidOperationException("Missing ReviewOnly prefab, playable controller, or base color texture.");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var splat = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (splat == null) throw new InvalidOperationException("Could not instantiate Iter1856 Splat prefab.");
                splat.transform.position = new Vector3(0f, .2374f, 0f);
                splat.transform.rotation = Quaternion.identity;
                var animator = splat.GetComponentInChildren<Animator>(true);
                if (animator == null || animator.runtimeAnimatorController == null || animator.avatar == null || !animator.avatar.isValid)
                    throw new InvalidOperationException("ReviewOnly Animator, controller, or Generic Avatar is invalid.");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 1f;
                animator.runtimeAnimatorController = controller;

                var artMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture"))
                { name = "Splat_Iter1856_CaptureAlbedo_NotSaved" };
                if (artMaterial.HasProperty("_BaseMap")) artMaterial.SetTexture("_BaseMap", texture);
                if (artMaterial.HasProperty("_MainTex")) artMaterial.SetTexture("_MainTex", texture);
                foreach (var r in splat.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = artMaterial;

                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "RuntimePreviewFloor_NotSaved";
                floor.transform.localScale = Vector3.one * 4f;
                var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                SetColor(floorMat, new Color(.31f, .34f, .39f));
                floor.GetComponent<Renderer>().sharedMaterial = floorMat;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.25f, .27f, .30f);
                AddLight(new Vector3(25f, 125f, -18f), .85f);
                AddLight(new Vector3(-30f, 18f, 48f), .32f);
                var camera = new GameObject("RuntimePreviewCamera_NotSaved").AddComponent<Camera>();
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
                notes.AppendLine("ReviewOnly live Animator.Update sequence, isolated from game movement/controller input.");
                notes.AppendLine("Animator enabled=" + animator.enabled + ", Avatar valid=" + animator.avatar.isValid + ", controller=" + animator.runtimeAnimatorController.name + ", applyRootMotion=" + animator.applyRootMotion + ".");
                notes.AppendLine("Controller clips: " + string.Join(", ", animator.runtimeAnimatorController.animationClips.Where(c => c != null).Select(c => c.name + "(" + c.length.ToString("F2") + "s)")));
                var sheet = new Texture2D(1536, 2560, TextureFormat.RGB24, false);
                var captureIndex = 0;
                Action<string> capture = label =>
                {
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    var clips = animator.GetCurrentAnimatorClipInfo(0);
                    var b = BoundsOf(splat.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray());
                    var target = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
                    var tile = new Texture2D(512, 512, TextureFormat.RGB24, false);
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    tile.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                    tile.Apply();
                    sheet.SetPixels((captureIndex % 3) * 512, (4 - captureIndex / 3) * 512, 512, 512, tile.GetPixels());
                    camera.targetTexture = null;
                    RenderTexture.active = null;
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                    UnityEngine.Object.DestroyImmediate(tile);
                    notes.AppendLine(label + ": state=" + StateName(animator) + ", normalized=" + state.normalizedTime.ToString("F2")
                        + ", rootY=" + splat.transform.position.y.ToString("F3") + "m, rendererMinY=" + b.min.y.ToString("F3") + "m, clips=["
                        + string.Join(", ", clips.Select(c => c.clip.name + "@" + c.weight.ToString("F2"))) + "]");
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
                animator.SetTrigger("Pickup"); advance(.35f); capture(Labels[8]);
                animator.Play("Locomotion", 0, 0f); animator.Update(0f);
                animator.SetTrigger("Dodge"); advance(.35f); capture(Labels[9]);
                animator.Play("Locomotion", 0, 0f); animator.Update(0f);
                animator.SetTrigger("AirAttack"); advance(.35f); capture(Labels[10]);
                animator.Play("Locomotion", 0, 0f); animator.Update(0f);
                animator.SetTrigger("RunJumpRoll"); advance(.35f); capture(Labels[11]);
                animator.Play("Locomotion", 0, 0f); animator.Update(0f);
                animator.SetTrigger("Hit"); advance(.30f); capture(Labels[12]);
                animator.Play("Locomotion", 0, 0f); animator.Update(0f);
                animator.SetTrigger("Victory"); advance(.50f); capture(Labels[13]);

                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);
                File.WriteAllBytes(OutputPath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
                notes.AppendLine("Capture: " + OutputPath + ". The mesh is shaded with its albedo in an unlit review material to avoid the overly dark comparison render; it is not a production lighting result.");
                notes.AppendLine("The controller used here is also assigned by the Iter1856 production player prefab. This remains an isolated Unity Animator.Update preview, not game Play Mode, SplatMotor contact, a build, formal test, or human play. Flat-floor measurements do not prove varied-platform contact or IK; Dodge/AirAttack/RunJumpRoll triggers are authored hooks, not mapped to player inputs.");
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

        private static string StateName(Animator animator)
        {
            var hash = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            foreach (var n in new[] { "Locomotion", "Jump", "Land", "Victory", "Personality", "Pickup", "Dodge", "AirAttack", "RunJumpRoll", "Hit" })
                if (Animator.StringToHash(n) == hash) return n;
            return "unknown(" + hash + ")";
        }

        private static Bounds BoundsOf(Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0) throw new InvalidOperationException("No visible character renderers.");
            var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        private static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        private static void AddLight(Vector3 direction, float intensity)
        {
            var go = new GameObject("RuntimePreviewLight_NotSaved");
            go.transform.rotation = Quaternion.LookRotation(-direction.normalized);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = new Color(1f, .94f, .87f);
        }
    }
}
#endif
