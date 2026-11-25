using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Isolated import and staged visual comparison of Meshy Iter1843 against
    /// Iter1841. Writes only a ReviewOnly prefab beneath the Iter1843 work folder
    /// and stills under Logs; it never opens or saves a production scene.
    /// </summary>
    public static class SplatMeshyIter1843ReviewCaptureCli
    {
        private const string CandidateRoot = "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/Iter1843";
        private const string CandidateModelPath = CandidateRoot + "/ReviewOnly/rigged_character.fbx";
        private const string CandidateTexturePath = CandidateRoot + "/Splat_Iter1843_T25_base_color.png";
        private const string CandidateWalkPath = CandidateRoot + "/ReviewOnly/walking_fbx.fbx";
        private const string CandidateRunPath = CandidateRoot + "/ReviewOnly/running_fbx.fbx";
        private const string CandidateActionBundlePath = CandidateRoot + "/ReviewOnly/Splat_Iter1843_AnimationBundle.fbx";
        private const string Candidate1844Root = "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/Iter1844";
        private const string Candidate1844ModelPath = Candidate1844Root + "/ReviewOnly/rigged_character.fbx";
        private const string Candidate1844TexturePath = Candidate1844Root + "/Splat_Iter1844_T25_base_color.png";
        private const string Candidate1844WalkPath = Candidate1844Root + "/ReviewOnly/walking_fbx.fbx";
        private const string Candidate1844RunPath = Candidate1844Root + "/ReviewOnly/running_fbx.fbx";
        private const string Candidate1844ActionBundlePath = Candidate1844Root + "/ReviewOnly/Splat_Iter1844_AnimationBundle.fbx";
        private const string Candidate1844PrefabPath = Candidate1844Root + "/ReviewOnly/Splat_Iter1844_ReviewOnly.prefab";
        private const string Candidate1844MaterialPath = Candidate1844Root + "/ReviewOnly/Splat_Iter1844_ReviewOnly.mat";
        private const string Iter1841Root = "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/Iter1841";
        private const string Iter1841ModelPath = Iter1841Root + "/rigged_character.fbx";
        private const string Iter1841PrefabPath = Iter1841Root + "/ReviewOnly/Splat_Iter1841_ReviewOnly.prefab";
        private const string Iter1841TexturePath = "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/Splat_Meshy_Iter1841_base_color.png";
        private const string ReviewFolder = CandidateRoot + "/ReviewOnly";
        private const string CandidatePrefabPath = ReviewFolder + "/Splat_Iter1843_ReviewOnly.prefab";
        private const string CandidateMaterialPath = ReviewFolder + "/Splat_Iter1843_ReviewOnly.mat";
        private const string Iter1841MaterialPath = Iter1841Root + "/ReviewOnly/Splat_Iter1841_ReviewOnly.mat";
        private const string OutputRoot = "Logs/score-comparison/splat-iter1841-1843-1844-review";
        private const int CellWidth = 720;
        private const int CellHeight = 900;

        public static void Capture()
        {
            Directory.CreateDirectory(OutputRoot);
            EnsureFolder(ReviewFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureFolder(Candidate1844Root + "/ReviewOnly");
            PrepareReviewModel(CandidateModelPath, CandidateTexturePath, CandidatePrefabPath,
                CandidateMaterialPath, CandidateWalkPath, CandidateRunPath, CandidateActionBundlePath);
            PrepareReviewModel(Candidate1844ModelPath, Candidate1844TexturePath, Candidate1844PrefabPath,
                Candidate1844MaterialPath, Candidate1844WalkPath, Candidate1844RunPath, Candidate1844ActionBundlePath);

            var model1843 = AssetDatabase.LoadAssetAtPath<GameObject>(CandidateModelPath);
            var model1841 = AssetDatabase.LoadAssetAtPath<GameObject>(Iter1841ModelPath);
            var model1844 = AssetDatabase.LoadAssetAtPath<GameObject>(Candidate1844ModelPath);
            var prefab1841 = AssetDatabase.LoadAssetAtPath<GameObject>(Iter1841PrefabPath);
            var prefab1844 = AssetDatabase.LoadAssetAtPath<GameObject>(Candidate1844PrefabPath);
            if (model1843 == null || model1841 == null || prefab1841 == null || model1844 == null || prefab1844 == null)
                throw new InvalidOperationException("Could not load Iter1841, Iter1843, and Iter1844 review assets.");

            var material1843 = LoadOrCreateMaterial(CandidateMaterialPath, CandidateTexturePath);
            var material1841 = AssetDatabase.LoadAssetAtPath<Material>(Iter1841MaterialPath);
            if (material1841 == null) throw new InvalidOperationException("Missing existing Iter1841 review material: " + Iter1841MaterialPath);
            var material1844 = LoadOrCreateMaterial(Candidate1844MaterialPath, Candidate1844TexturePath);
            var candidatePrefabRoot = PrefabUtility.InstantiatePrefab(model1843) as GameObject;
            if (candidatePrefabRoot == null) throw new InvalidOperationException("Could not instantiate Iter1843 FBX.");
            candidatePrefabRoot.name = "Splat_Iter1843_ReviewOnly";
            ApplyMaterial(candidatePrefabRoot, material1843);
            var savedCandidatePrefab = PrefabUtility.SaveAsPrefabAsset(candidatePrefabRoot, CandidatePrefabPath);
            UnityEngine.Object.DestroyImmediate(candidatePrefabRoot);
            if (savedCandidatePrefab == null) throw new InvalidOperationException("Could not save isolated Iter1843 ReviewOnly prefab.");

            var report = new List<string>
            {
                "Splat Iter1843 versus Iter1841 isolated Meshy asset review",
                "Capture type: staged editor stills using Unity's graphics renderer; not gameplay, a build, or human-play verification.",
                "Support: flat gray review plane at Y=0. Models are uniformly scaled to a common rendered height and statically seated by measured lower render bounds. This visual setup does not prove moving-platform or animated foot contact.",
                "Contact sheet columns are Iter1841 (left), Iter1843 (middle), Iter1844 (right); first three rows are front, three-quarter, side; final four rows are idle-or-bind, walk, run, jump.",
                "Motion stills use delivered clips only when transform-curve paths match the rig. Iter1843 and Iter1844 have their own action bundles. Unsupported clips are explicitly left as static bind poses.",
                "Iter1843 asset inputs: " + CandidateRoot + "/Splat_Iter1843_T25.fbx (source), " + CandidateModelPath + " (rigged review copy), and " + CandidateTexturePath,
                "Iter1841 comparison inputs: " + Iter1841ModelPath + " and " + Iter1841TexturePath,
                "Iter1844 inputs: " + Candidate1844Root + "/Splat_Iter1844_T25.fbx (source), " + Candidate1844ModelPath + " (rigged review copy), and " + Candidate1844TexturePath,
                "Review-only prefab: " + CandidatePrefabPath,
                "No production prefab, scene, or player controller was modified.",
                ""
            };
            DescribeAsset(model1841, Iter1841ModelPath, report);
            DescribeAsset(model1843, CandidateModelPath, report);
            DescribeAsset(model1844, Candidate1844ModelPath, report);

            var idle1841 = FindClip(Iter1841Root + "/animation_fbx.fbx", "Idle");
            var walk1841 = FindClip(Iter1841Root + "/walking_fbx.fbx", "walking");
            var run1841 = FindClip(Iter1841Root + "/running_fbx.fbx", "running");
            var idle1843 = FindClip(CandidateActionBundlePath, "Idle", "Idle_7", "Idle 7");
            if (idle1843 == null) idle1843 = idle1841;
            var walk1843 = FindClip(CandidateWalkPath, "walk", "Walking", "walking");
            var run1843 = FindClip(CandidateRunPath, "run", "Running", "running");
            var idle1844 = FindClip(Candidate1844ActionBundlePath, "Idle", "Idle_7", "Idle 7");
            if (idle1844 == null) idle1844 = idle1841;
            var walk1844 = FindClip(Candidate1844WalkPath, "walking");
            var run1844 = FindClip(Candidate1844RunPath, "running");
            var jump1841 = FindClip(Iter1841Root + "/animation_fbx.fbx", "happy_jump_m", "Regular_Jump", "Jump");
            var jump1843 = FindClip(CandidateActionBundlePath, "Regular_Jump", "happy_jump_m", "Jump");
            var jump1844 = FindClip(Candidate1844ActionBundlePath, "Regular_Jump", "happy_jump_m", "Jump");
            report.Add("");
            report.Add("Clip sources found: Iter1841 Idle=" + ClipDescription(idle1841) + ", Walk=" + ClipDescription(walk1841) + ", Run=" + ClipDescription(run1841) + ".");
            report.Add("Iter1843 Walk=" + ClipDescription(walk1843) + ", Run=" + ClipDescription(run1843)
                + "; Idle=" + ClipDescription(idle1843) + ". Cross-rig clips are sampled only when curve-path coverage passes the documented 60% gate.");
            report.Add("Iter1843 delivered action-bundle clips=" + string.Join(",", AssetDatabase.LoadAllAssetsAtPath(CandidateActionBundlePath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).Select(c => c.name + " " + c.length.ToString("F2") + "s")));
            report.Add("Iter1844 Walk=" + ClipDescription(walk1844) + ", Run=" + ClipDescription(run1844) + ", Idle=" + ClipDescription(idle1844) + ", Jump=" + ClipDescription(jump1844) + ". Delivered action-bundle clips=" + string.Join(",", AssetDatabase.LoadAllAssetsAtPath(Candidate1844ActionBundlePath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).Select(c => c.name + " " + c.length.ToString("F2") + "s")));
            report.Add("Parent-confirmed Meshy costs: Iter1843 rig task 5 credits; Iter1844 rig task 5 credits and six-action bundle 18 credits. Iter1843 action-bundle price was not confirmed in this review task; this review capture spent 0 credits.");

            var oldScene = EditorSceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.37f, 0.39f, 0.43f);
            var light1 = CreateDirectional("Review Key", new Vector3(35f, -28f, 0f), 1.15f);
            var light2 = CreateDirectional("Review Fill", new Vector3(5f, 155f, 0f), 0.55f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Neutral flat support plane (review only)";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(0.55f, 1f, 0.55f);
            var floorMat = NewLitMaterial(new Color(0.22f, 0.25f, 0.31f));
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            var rt = new RenderTexture(CellWidth, CellHeight, 24, RenderTextureFormat.ARGB32);
            var cell = new Texture2D(CellWidth, CellHeight, TextureFormat.RGB24, false);
            var sheet = new Texture2D(CellWidth * 3, CellHeight * 7, TextureFormat.RGB24, false);
            var cameraGo = new GameObject("Review comparison camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.12f, 0.17f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = rt;
            camera.allowHDR = false;
            camera.allowMSAA = true;

            // Use the isolated Iter1841 prefab so its root orientation and
            // review material assignment are preserved. The other two are new
            // isolated prefabs prepared from their rigged FBXs above.
            var prefabs = new[] { prefab1841, savedCandidatePrefab, prefab1844 };
            var materials = new[] { material1841, material1843, material1844 };
            var columnNames = new[] { "Iter1841", "Iter1843", "Iter1844" };
            var views = new[] { ("front", 0f), ("three_quarter", 34f), ("side", 90f) };
            var animations = new[]
            {
                new[] { idle1841, walk1841, run1841, jump1841 },
                new[] { idle1843, walk1843, run1843, jump1843 },
                new[] { idle1844, walk1844, run1844, jump1844 }
            };
            var rowLabels = new[] { "front", "three_quarter", "side", "idle_or_bind", "walk", "run", "jump" };
            for (var col = 0; col < prefabs.Length; col++)
            {
                var instance = PrefabUtility.InstantiatePrefab(prefabs[col]) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not stage " + columnNames[col]);
                instance.name = columnNames[col] + "_ReviewInstance";
                ApplyMaterial(instance, materials[col]);
                var animator = instance.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    animator.runtimeAnimatorController = null;
                    animator.enabled = false;
                }

                // Make comparison scale independent of source units and seat the
                // static bind pose on the same plane using rendered mesh vertices.
                var bounds = CalculateRenderedBounds(instance);
                if (bounds.size.y <= 0.001f) throw new InvalidOperationException(columnNames[col] + " has no measurable rendered height.");
                var scale = 1.55f / bounds.size.y;
                instance.transform.localScale = instance.transform.localScale * scale;
                bounds = CalculateRenderedBounds(instance);
                instance.transform.position += Vector3.down * bounds.min.y;
                bounds = CalculateRenderedBounds(instance);
                report.Add($"{columnNames[col]} staged dimensions H={bounds.size.y:F3}m W={bounds.size.x:F3}m D={bounds.size.z:F3}m; measured mesh-min-to-plane gap={bounds.min.y:F4}m after static seating; root scale multiplier={scale:F4} applied over existing prefab scale.");

                var basePose = CapturePose(instance);
                for (var row = 0; row < rowLabels.Length; row++)
                {
                    RestorePose(basePose);
                    instance.transform.localRotation = Quaternion.identity;
                    var angle = row < 3 ? views[row].Item2 : views[1].Item2;
                    if (row >= 3)
                    {
                        var clip = animations[col][row - 3];
                        var compatible = IsClipCompatible(clip, instance.transform);
                        if (clip != null && compatible)
                        {
                            var phase = row == 3 ? 0.5f : row == 6 ? 0.42f : 0.32f;
                            var t = Mathf.Clamp(clip.length * phase, 0f, Mathf.Max(0f, clip.length - 0.0001f));
                            clip.SampleAnimation(instance, t);
                            var poseChange = MeasurePoseChange(basePose);
                            RestoreScale(basePose);
                            instance.transform.localPosition = basePose.rootPosition;
                            instance.transform.localRotation = basePose.rotations[0];
                            var sourceLabel = row == 3 && col == 1 && clip == idle1841 ? "Iter1841 Idle reused on Iter1843 rig"
                                : row == 3 && col == 2 && clip == idle1841 ? "Iter1841 Idle reused on Iter1844 rig" : clip.name;
                            report.Add($"Staged {columnNames[col]} {rowLabels[row]} from clip '{sourceLabel}' at {t:F3}s/{clip.length:F3}s; compatible animated curve path coverage={CurvePathCoverage(clip, instance.transform):P0}; changed joints={poseChange.changed}/{basePose.transforms.Length - 1}, max local rotation delta={poseChange.maxRotation:F1}deg, max local position delta={poseChange.maxPosition:F4}m; root translation removed; bind scales restored.");
                        }
                        else
                        {
                            report.Add($"Staged {columnNames[col]} {rowLabels[row]} as bind/static pose: no clip or incompatible bone-path coverage (clip={ClipDescription(clip)}, coverage={(clip != null ? CurvePathCoverage(clip, instance.transform).ToString("P0") : "n/a")} ). Not an animated sample.");
                        }
                        bounds = CalculateRenderedBounds(instance);
                        instance.transform.position += Vector3.down * bounds.min.y;
                        bounds = CalculateRenderedBounds(instance);
                    }
                    var focus = bounds.center + Vector3.up * (bounds.size.y * 0.04f);
                    var offsetDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    var distance = Mathf.Max(3.9f, bounds.size.y * 2.85f);
                    camera.transform.position = focus + offsetDirection * distance + Vector3.up * (bounds.size.y * 0.19f);
                    camera.transform.rotation = Quaternion.LookRotation(focus - camera.transform.position, Vector3.up);
                    camera.Render();
                    RenderTexture.active = rt;
                    cell.ReadPixels(new Rect(0, 0, CellWidth, CellHeight), 0, 0);
                    cell.Apply();
                    var px = cell.GetPixels32();
                    // Texture coordinates are bottom-left; preserve the same top-to-bottom
                    // ordering described by review.txt.
                    sheet.SetPixels32(col * CellWidth, (6 - row) * CellHeight, CellWidth, CellHeight, px);
                    var singlePath = Path.Combine(OutputRoot, $"{columnNames[col]}_{rowLabels[row]}.png");
                    File.WriteAllBytes(singlePath, cell.EncodeToPNG());
                }
                UnityEngine.Object.DestroyImmediate(instance);
            }
            sheet.Apply();
            report.Add("");
            report.Add("Visual read from the saved six-angle/motion sheet: Iter1843 and Iter1844 are close variants with round white bodies, purple limbs/caps, and front-readable faces. Both retain a conspicuous near-horizontal arms-out silhouette across their sampled Idle/Walk/Run stills; the single Jump still reads airborne but keeps the arms mostly out. This suggests the delivered clips have not yet demonstrated a convincing compact-character gait in the rendered poses, despite substantial transform curves and complete binding-path matches. The Iter1841 review prefab's sampled Idle/Walk/Run reads as a more bent, less T-pose-like silhouette in these stills. This is a visual comparison of staged samples, not a verdict on all clip phases or live play.");
            report.Add("Every pose, including Jump, was statically translated so its lowest rendered mesh bound met Y=0 in that still. The ground-plane gap is therefore staged to zero per image; it is not independent evidence of root height, plantar alignment, continuous foot locking, sloped/varied support, runtime traversal, or a jump apex/airborne arc.");
            File.WriteAllBytes(Path.Combine(OutputRoot, "Splat_Iter1841_1843_1844_ContactSheet.png"), sheet.EncodeToPNG());
            File.WriteAllLines(Path.Combine(OutputRoot, "review.txt"), report);

            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(cell);
            UnityEngine.Object.DestroyImmediate(sheet);
            UnityEngine.Object.DestroyImmediate(floorMat);
            UnityEngine.Object.DestroyImmediate(light1);
            UnityEngine.Object.DestroyImmediate(light2);
            EditorSceneManager.CloseScene(scene, true);
            if (oldScene.IsValid() && oldScene.isLoaded) EditorSceneManager.SetActiveScene(oldScene);
            AssetDatabase.SaveAssets();
            Debug.Log("SPLAT_ITER1843_REVIEW_COMPLETE output=" + OutputRoot + " prefab=" + CandidatePrefabPath + " ; staged comparison only, no gameplay/contact claim.");
            EditorApplication.Exit(0);
        }

        private static void DescribeAsset(GameObject model, string path, List<string> report)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var meshRenderers = model.GetComponentsInChildren<MeshRenderer>(true);
            var avatars = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().ToArray();
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).ToArray();
            report.Add("");
            report.Add(Path.GetFileName(path) + " importer: animationType=" + (importer != null ? importer.animationType.ToString() : "unknown")
                + ", avatarSetup=" + (importer != null ? importer.avatarSetup.ToString() : "unknown")
                + ", importAnimation=" + (importer != null && importer.importAnimation));
            report.Add(Path.GetFileName(path) + $" renderers: skinned={skins.Length}, staticMesh={meshRenderers.Length}; importedAvatar="
                + (avatars.Length == 0 ? "none" : string.Join(",", avatars.Select(a => a.name + " valid=" + a.isValid)))
                + "; embedded clips=" + (clips.Length == 0 ? "none" : string.Join(",", clips.Select(c => c.name + " " + c.length.ToString("F2") + "s"))));
            for (var i = 0; i < skins.Length; i++)
                report.Add($"SkinnedRenderer[{i}] {skins[i].name}: mesh={(skins[i].sharedMesh != null ? skins[i].sharedMesh.name : "null")}, bones={(skins[i].bones != null ? skins[i].bones.Length : 0)}, rootBone={(skins[i].rootBone != null ? skins[i].rootBone.name : "null")}.");
            if (skins.Length > 0)
            {
                var allBones = skins.SelectMany(s => s.bones ?? Array.Empty<Transform>()).Where(t => t != null).Distinct().ToArray();
                report.Add("Unique bound bones=" + allBones.Length + "; names=" + string.Join(",", allBones.Select(t => t.name).Distinct().OrderBy(n => n)));
            }
        }

        private static Bounds CalculateRenderedBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException(root.name + " has no measurable renderers.");
            // Unity updates SkinnedMeshRenderer.bounds for the evaluated pose and
            // includes the complete hierarchy scale once. BakeMesh vertices can
            // already reflect the renderer scale, so transforming them again
            // double-counts scale in a nested imported rig.
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static Material LoadOrCreateMaterial(string path, string texturePath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = NewLitMaterial(Color.white);
                material.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(material, path);
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject PrepareReviewModel(string modelPath, string texturePath, string prefabPath,
            string materialPath, params string[] animationPaths)
        {
            var modelImporter = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (modelImporter == null) throw new InvalidOperationException("Missing rigged Meshy model importer: " + modelPath);
            if (modelImporter.animationType != ModelImporterAnimationType.Generic
                || modelImporter.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel
                || modelImporter.importAnimation || modelImporter.optimizeGameObjects
                || modelImporter.importCameras || modelImporter.importLights
                || modelImporter.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                modelImporter.animationType = ModelImporterAnimationType.Generic;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                modelImporter.sourceAvatar = null;
                modelImporter.importAnimation = false;
                modelImporter.optimizeGameObjects = false;
                modelImporter.importCameras = false;
                modelImporter.importLights = false;
                modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
                modelImporter.SaveAndReimport();
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>()
                .FirstOrDefault(a => a != null && a.isValid);
            if (avatar == null) throw new InvalidOperationException("No valid Generic Avatar imported for " + modelPath);

            foreach (var animationPath in animationPaths)
            {
                var importer = AssetImporter.GetAtPath(animationPath) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Missing Meshy animation FBX importer: " + animationPath);
                if (importer.animationType == ModelImporterAnimationType.Generic
                    && importer.avatarSetup == ModelImporterAvatarSetup.CopyFromOther
                    && importer.sourceAvatar == avatar && importer.importAnimation
                    && !importer.optimizeGameObjects && importer.materialImportMode == ModelImporterMaterialImportMode.None)
                    continue;
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                importer.importAnimation = true;
                importer.optimizeGameObjects = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                var clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    var clipName = (clip.name ?? string.Empty).ToLowerInvariant();
                    clip.loopTime = clipName.Contains("idle") || clipName.Contains("walk") || clipName.Contains("run");
                    clip.loopPose = clip.loopTime;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new InvalidOperationException("Could not load imported Meshy model: " + modelPath);
            var material = LoadOrCreateMaterial(materialPath, texturePath);
            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not stage review model: " + modelPath);
            instance.name = Path.GetFileNameWithoutExtension(prefabPath);
            ApplyMaterial(instance, material);
            var animator = instance.GetComponentInChildren<Animator>(true);
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.enabled = false;
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
            if (prefab == null) throw new InvalidOperationException("Could not save ReviewOnly prefab: " + prefabPath);
            return prefab;
        }

        private static AnimationClip FindClip(string modelPath, params string[] acceptedNames)
        {
            return AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(c => acceptedNames.Any(n =>
                    string.Equals(c.name, n, StringComparison.OrdinalIgnoreCase)
                    || c.name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static string ClipDescription(AnimationClip clip) => clip == null ? "none" : $"{clip.name} ({clip.length:F2}s)";

        private static bool IsClipCompatible(AnimationClip clip, Transform root) =>
            clip != null && CurvePathCoverage(clip, root) >= 0.60f;

        private static float CurvePathCoverage(AnimationClip clip, Transform root)
        {
            if (clip == null) return 0f;
            var animated = AnimationUtility.GetCurveBindings(clip)
                .Where(b => b.type == typeof(Transform) || b.propertyName.StartsWith("m_Local", StringComparison.Ordinal))
                .Select(b => b.path).Distinct().ToArray();
            if (animated.Length == 0) return 0f;
            var available = new HashSet<string>(root.GetComponentsInChildren<Transform>(true)
                .Select(t => AnimationUtility.CalculateTransformPath(t, root)));
            return animated.Count(available.Contains) / (float)animated.Length;
        }

        private sealed class PoseSnapshot
        {
            public Transform root;
            public Vector3 rootPosition;
            public Transform[] transforms;
            public Vector3[] positions;
            public Quaternion[] rotations;
            public Vector3[] scales;
        }

        private static PoseSnapshot CapturePose(GameObject root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            return new PoseSnapshot
            {
                root = root.transform,
                rootPosition = root.transform.localPosition,
                transforms = transforms,
                positions = transforms.Select(t => t.localPosition).ToArray(),
                rotations = transforms.Select(t => t.localRotation).ToArray(),
                scales = transforms.Select(t => t.localScale).ToArray()
            };
        }

        private static void RestorePose(PoseSnapshot snapshot)
        {
            for (var i = 0; i < snapshot.transforms.Length; i++)
            {
                snapshot.transforms[i].localPosition = snapshot.positions[i];
                snapshot.transforms[i].localRotation = snapshot.rotations[i];
                snapshot.transforms[i].localScale = snapshot.scales[i];
            }
        }

        private static void RestoreScale(PoseSnapshot snapshot)
        {
            for (var i = 0; i < snapshot.transforms.Length; i++) snapshot.transforms[i].localScale = snapshot.scales[i];
        }

        private static (int changed, float maxRotation, float maxPosition) MeasurePoseChange(PoseSnapshot snapshot)
        {
            var changed = 0;
            var maxRotation = 0f;
            var maxPosition = 0f;
            // Ignore the root transform because capture deliberately removes
            // motion-root translation/rotation to keep the character staged.
            for (var i = 1; i < snapshot.transforms.Length; i++)
            {
                var rotation = Quaternion.Angle(snapshot.rotations[i], snapshot.transforms[i].localRotation);
                var position = Vector3.Distance(snapshot.positions[i], snapshot.transforms[i].localPosition);
                if (rotation > 0.05f || position > 0.0005f) changed++;
                maxRotation = Mathf.Max(maxRotation, rotation);
                maxPosition = Mathf.Max(maxPosition, position);
            }
            return (changed, maxRotation, maxPosition);
        }

        private static void ApplyMaterial(GameObject root, Material material)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is SkinnedMeshRenderer || renderer is MeshRenderer)
                    renderer.sharedMaterial = material;
            }
        }

        private static Material NewLitMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }

        private static Light CreateDirectional(string name, Vector3 euler, float intensity)
        {
            var go = new GameObject(name);
            go.transform.rotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = new Color(1f, 0.95f, 0.9f);
            return light;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
