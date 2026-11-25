#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Capture-only proportion experiment on the Iter1836 candidate. It never
    /// writes the candidate prefab, controller, player prefab, or scene.
    /// </summary>
    public static class SplatMeshySilhouettePrototypeIter1838
    {
        private const string CandidatePrefab = "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyImageCandidate_Iter1836_ReviewOnly/Splat_Iter1836_ReviewOnly.prefab";
        private const string IdlePath = "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyImageCandidate_Iter1836_ReviewOnly/Splat_Idle7_FaceForward_Iter1836_ReviewOnly.anim";
        private const string OutputDirectory = "Logs/score-comparison/splat-silhouette-pose-iter1838-r4";

        [MenuItem("Space & Crafts/Capture Splat Iter1838 Silhouette Prototype")]
        public static void Capture()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePrefab);
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdlePath);
            if (prefab == null || idle == null)
                throw new InvalidOperationException("Missing Iter1836 candidate prefab or face-forward idle clip.");

            var output = Path.GetFullPath(OutputDirectory);
            Directory.CreateDirectory(output);
            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (go == null) throw new InvalidOperationException("Could not instantiate the Iter1836 review prefab.");
            var animator = go.GetComponent<Animator>() ?? go.GetComponentInChildren<Animator>(true);
            if (animator != null) animator.enabled = false;

            var transforms = go.GetComponentsInChildren<Transform>(true);
            var basePose = transforms.ToDictionary(t => t,
                t => (position: t.localPosition, rotation: t.localRotation, scale: t.localScale));
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            var cameraObject = new GameObject("Iter1838 silhouette review camera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.82f, 0.88f, 0.96f);
            camera.orthographic = true;
            camera.orthographicSize = 0.79f;
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 12f;
            var lightObject = new GameObject("Iter1838 silhouette review key light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.98f, 0.94f);
            lightObject.transform.rotation = Quaternion.Euler(30f, -25f, 0f);
            var oldAmbient = RenderSettings.ambientLight;
            var oldAmbientMode = RenderSettings.ambientMode;
            var oldActive = RenderTexture.active;
            var image = new Texture2D(768, 768, TextureFormat.RGB24, false);
            try
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.60f, 0.63f, 0.68f);
                idle.SampleAnimation(go, idle.length * 0.90f);
                foreach (var t in transforms)
                    if (basePose.TryGetValue(t, out var pose)) t.localScale = pose.scale;

                var report = new List<string>
                {
                    "Iter1838 capture-only pose prototype; no character geometry, prefab, controller, production scene, or live-game asset is edited.",
                    "Existing Iter1836 skinned mesh and bones only. Body, face, and cap bind scales are preserved.",
                    "Prototype v4: arm stems lengthened 20% and widened 10%, forearms lengthened 16% and widened 8%, hands 14% larger, arms posed outward; feet moved further apart and boots widened/deepened about 10%.",
                };

                ApplyBoneScale(go, basePose, "LeftArm", new Vector3(1.10f, 1.20f, 1.10f));
                ApplyBoneScale(go, basePose, "RightArm", new Vector3(1.10f, 1.20f, 1.10f));
                ApplyBoneScale(go, basePose, "LeftForeArm", new Vector3(1.08f, 1.16f, 1.08f));
                ApplyBoneScale(go, basePose, "RightForeArm", new Vector3(1.08f, 1.16f, 1.08f));
                ApplyBoneScale(go, basePose, "LeftHand", new Vector3(1.14f, 1.14f, 1.10f));
                ApplyBoneScale(go, basePose, "RightHand", new Vector3(1.14f, 1.14f, 1.10f));
                RotateBoneOutward(go, "LeftArm", -18f);
                RotateBoneOutward(go, "RightArm", 18f);
                RotateBoneOutward(go, "LeftForeArm", -8f);
                RotateBoneOutward(go, "RightForeArm", 8f);

                var leftLeg = FindBone(go, "LeftUpLeg");
                var rightLeg = FindBone(go, "RightUpLeg");
                if (leftLeg != null) leftLeg.localPosition += new Vector3(-0.085f, 0f, 0f);
                if (rightLeg != null) rightLeg.localPosition += new Vector3(0.085f, 0f, 0f);
                ApplyBoneScale(go, basePose, "LeftFoot", new Vector3(1.12f, 1.00f, 1.10f));
                ApplyBoneScale(go, basePose, "RightFoot", new Vector3(1.12f, 1.00f, 1.10f));

                var leftHand = FindBone(go, "LeftHand");
                var rightHand = FindBone(go, "RightHand");
                var leftFoot = FindBone(go, "LeftFoot");
                var rightFoot = FindBone(go, "RightFoot");
                report.Add($"bones: armL={Describe(FindBone(go, "LeftArm"))}; handL={Describe(leftHand)}; armR={Describe(FindBone(go, "RightArm"))}; handR={Describe(rightHand)}; legL={Describe(leftLeg)}; footL={Describe(leftFoot)}; legR={Describe(rightLeg)}; footR={Describe(rightFoot)}");
                report.Add($"edited local scales: body/head/cap unchanged; hands=1.14x; boots=1.12x width, 1.10x depth; added leg spacing=0.17m total before model scale.");

                var center = new Vector3(0f, 0.575f, 0f);
                var views = new[]
                {
                    (name: "front", offset: new Vector3(0.03f, 0.03f, 3.8f)),
                    (name: "threequarter", offset: new Vector3(-1.45f, 0.10f, 3.55f)),
                };
                for (var i = 0; i < views.Length; i++)
                {
                    camera.transform.position = center + views[i].offset;
                    camera.transform.LookAt(center);
                    camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 768, 768), 0, 0);
                    image.Apply(false, false);
                    File.WriteAllBytes(Path.Combine(output, "prototype_" + views[i].name + ".png"), image.EncodeToPNG());
                }
                File.WriteAllLines(Path.Combine(output, "review.txt"), report);
                Debug.Log("[Splat Iter1838] Saved review-only arm/boot silhouette prototype under " + output);
            }
            finally
            {
                RenderTexture.active = oldActive;
                RenderSettings.ambientLight = oldAmbient;
                RenderSettings.ambientMode = oldAmbientMode;
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void ApplyBoneScale(GameObject root,
            Dictionary<Transform, (Vector3 position, Quaternion rotation, Vector3 scale)> basePose,
            string boneName, Vector3 multiplier)
        {
            var bone = FindBone(root, boneName);
            if (bone != null && basePose.TryGetValue(bone, out var pose))
                bone.localScale = Vector3.Scale(pose.scale, multiplier);
        }

        private static void RotateBoneOutward(GameObject root, string boneName, float degrees)
        {
            var bone = FindBone(root, boneName);
            if (bone != null)
                bone.localRotation = bone.localRotation * Quaternion.AngleAxis(degrees, Vector3.forward);
        }

        private static Transform FindBone(GameObject root, string boneName) => root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name.Equals(boneName, StringComparison.OrdinalIgnoreCase));

        private static string Describe(Transform t) => t == null ? "missing" :
            $"{t.name}@{t.localPosition.ToString("F3")} scale={t.localScale.ToString("F2")}";
    }
}
#endif
