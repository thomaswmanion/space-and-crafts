#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    public static class AnimationAssetAuditIter974
    {
        public static void AuditSplatRuntimeClipScaleCurvesIter1248()
        {
            var paths = new[]
            {
                "Assets/SpaceAndCrafts/Resources/Characters/SplatAnimationIter1161/Idle/Splat_Idle.fbx",
                "Assets/SpaceAndCrafts/Resources/Characters/SplatAnimationIter1161/Run/Splat_Run.fbx",
                "Assets/SpaceAndCrafts/Resources/Characters/SplatAnimationIter1161/Jump/Splat_Jump.fbx",
                "Assets/SpaceAndCrafts/Resources/Characters/SplatAnimationIter1161/Land/Splat_Land.fbx",
            };

            foreach (var path in paths)
            {
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .Where(clip => clip != null && !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                    .ToArray();
                if (clips.Length == 0)
                {
                    Debug.LogWarning($"[AnimationAssetAuditIter974] Iter1248 no runtime clips imported from {path}.");
                    continue;
                }

                foreach (var clip in clips)
                {
                    var scaleBindings = AnimationUtility.GetCurveBindings(clip)
                        .Where(binding => binding.type == typeof(Transform)
                            && binding.propertyName.IndexOf("Scale", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToArray();
                    var nonUnitSummaries = scaleBindings.Select(binding =>
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        var keys = curve != null ? curve.keys : System.Array.Empty<Keyframe>();
                        var min = keys.Length == 0 ? float.NaN : keys.Min(key => key.value);
                        var max = keys.Length == 0 ? float.NaN : keys.Max(key => key.value);
                        return new { binding, min, max };
                    }).Where(summary => !float.IsNaN(summary.min)
                        && (Mathf.Abs(summary.min - 1f) > 0.001f || Mathf.Abs(summary.max - 1f) > 0.001f))
                        .Select(summary => $"{summary.binding.path}:{summary.binding.propertyName}[{summary.min:F4},{summary.max:F4}]")
                        .ToArray();
                    Debug.Log($"[AnimationAssetAuditIter974] Iter1248 runtime clip={clip.name}, asset={path}, bindings={AnimationUtility.GetCurveBindings(clip).Length}, scaleBindings={scaleBindings.Length}, nonUnitScale={(nonUnitSummaries.Length == 0 ? "none" : string.Join(";", nonUnitSummaries))}.");
                }
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void AuditSplatArmTipSkinWeightsIter1110()
        {
            const string path = "Assets/SpaceAndCrafts/Resources/Characters/Splat_Rigged_Candidate_Iter378.fbx";
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                Debug.LogError($"[AnimationAssetAuditIter974] Iter1110 missing rig: {path}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = skin.sharedMesh;
                if (mesh == null || skin.bones == null) continue;
                var weights = mesh.boneWeights;
                foreach (var targetBoneName in new[] { "Bone_ArmTipL", "Bone_ArmTipR", "Bone_ArmL", "Bone_ArmR" })
                {
                    var boneIndex = System.Array.FindIndex(skin.bones,
                        bone => bone != null && bone.name == targetBoneName);
                    var influencedVertices = 0;
                    var stronglyInfluencedVertices = 0;
                    var totalWeight = 0f;
                    var maxWeight = 0f;
                    for (var vertexIndex = 0; vertexIndex < weights.Length; vertexIndex++)
                    {
                        var weight = weights[vertexIndex];
                        var influence = BoneWeightForIndex(weight, boneIndex);
                        if (influence <= 0f) continue;
                        influencedVertices++;
                        if (influence >= 0.25f) stronglyInfluencedVertices++;
                        totalWeight += influence;
                        maxWeight = Mathf.Max(maxWeight, influence);
                    }

                    Debug.Log($"[AnimationAssetAuditIter974] Iter1110 skin={skin.name}, mesh={mesh.name}, bone={targetBoneName}, boneIndex={boneIndex}, vertices={mesh.vertexCount}, influencedVertices={influencedVertices}, verticesAtLeast25Percent={stronglyInfluencedVertices}, totalVertexWeight={totalWeight:F2}, maxWeight={maxWeight:F2}.");
                }
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static float BoneWeightForIndex(BoneWeight weight, int boneIndex)
        {
            if (boneIndex < 0) return 0f;
            var influence = 0f;
            if (weight.boneIndex0 == boneIndex) influence += weight.weight0;
            if (weight.boneIndex1 == boneIndex) influence += weight.weight1;
            if (weight.boneIndex2 == boneIndex) influence += weight.weight2;
            if (weight.boneIndex3 == boneIndex) influence += weight.weight3;
            return influence;
        }

        public static void AuditMeshyHeroRigAndClipAvailabilityIter1088()
        {
            const string heroPath = "Assets/SpaceAndCrafts/Art/Commissioned/Hero/Splat_Hero.fbx";
            const string actionPath = "Assets/SpaceAndCrafts/Resources/Characters/Splat_AnimationActions_Iter911.fbx";
            var hero = AssetDatabase.LoadAssetAtPath<GameObject>(heroPath);
            var heroImporter = AssetImporter.GetAtPath(heroPath) as ModelImporter;
            var heroAvatars = AssetDatabase.LoadAllAssetsAtPath(heroPath).OfType<Avatar>().ToArray();
            var heroSkins = hero != null ? hero.GetComponentsInChildren<SkinnedMeshRenderer>(true) : new SkinnedMeshRenderer[0];
            var heroAnimators = hero != null ? hero.GetComponentsInChildren<Animator>(true) : new Animator[0];
            Debug.Log($"[AnimationAssetAuditIter974] Iter1088 Meshy Splat Hero: assetLoaded={hero != null}, animationType={heroImporter?.animationType.ToString() ?? "missing"}, importAnimation={heroImporter?.importAnimation.ToString() ?? "missing"}, avatarSetup={heroImporter?.avatarSetup.ToString() ?? "missing"}, avatars={heroAvatars.Length}, skins={heroSkins.Length}, animators={heroAnimators.Length}.");
            foreach (var avatar in heroAvatars)
                Debug.Log($"[AnimationAssetAuditIter974] Iter1088 Meshy hero Avatar {avatar.name}: valid={avatar.isValid}, human={avatar.isHuman}.");
            foreach (var skin in heroSkins)
                Debug.Log($"[AnimationAssetAuditIter974] Iter1088 Meshy hero skin {skin.name}: mesh={skin.sharedMesh?.name ?? "missing"}, bones={skin.bones.Length}, rootBone={skin.rootBone?.name ?? "missing"}.");

            var actionImporter = AssetImporter.GetAtPath(actionPath) as ModelImporter;
            var actionAvatars = AssetDatabase.LoadAllAssetsAtPath(actionPath).OfType<Avatar>().ToArray();
            var actionClips = AssetDatabase.LoadAllAssetsAtPath(actionPath).OfType<AnimationClip>()
                .Where(clip => clip != null && !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal)).ToArray();
            Debug.Log($"[AnimationAssetAuditIter974] Iter1088 action source: animationType={actionImporter?.animationType.ToString() ?? "missing"}, importAnimation={actionImporter?.importAnimation.ToString() ?? "missing"}, avatarSetup={actionImporter?.avatarSetup.ToString() ?? "missing"}, avatars={actionAvatars.Length}, clips={actionClips.Length}.");
            foreach (var avatar in actionAvatars)
                Debug.Log($"[AnimationAssetAuditIter974] Iter1088 action Avatar {avatar.name}: valid={avatar.isValid}, human={avatar.isHuman}.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void AuditCommissionedEnemyPartHierarchyIter1089()
        {
            const string path = "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/ScrapMite.fbx";
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                Debug.LogError("[AnimationAssetAuditIter974] Iter1089 commissioned ScrapMite FBX did not load.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            var skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            Debug.Log($"[AnimationAssetAuditIter974] Iter1089 commissioned ScrapMite hierarchy: transforms={transforms.Length}, meshFilters={filters.Length}, skinnedRenderers={skins.Length}, renderers={renderers.Length}.");
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                var pathName = AnimationUtility.CalculateTransformPath(transform, root.transform);
                var filter = transform.GetComponent<MeshFilter>();
                var meshName = filter != null ? filter.sharedMesh?.name ?? "missing" : "none";
                Debug.Log($"[AnimationAssetAuditIter974] Iter1089 node={pathName}, childCount={transform.childCount}, mesh={meshName}.");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void AuditRetargetRigPoseIter1062()
        {
            const string animationPath = "Assets/SpaceAndCrafts/Resources/Characters/Splat_AnimationActions_Iter911.fbx";
            const string sourceRigPath = "Assets/SpaceAndCrafts/Resources/Characters/Splat_AnimationActions_Iter911.fbx";
            const string targetRigPath = "Assets/SpaceAndCrafts/Resources/Characters/Splat_Rigged_Candidate_Iter378.fbx";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourceRigPath);
            var target = AssetDatabase.LoadAssetAtPath<GameObject>(targetRigPath);
            if (source == null || target == null)
            {
                Debug.LogError($"[AnimationAssetAuditIter974] Iter1062 missing rig: source={(source != null)}, target={(target != null)}.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var sourceByName = BuildNameMap(source.transform);
            var targetByName = BuildNameMap(target.transform);
            var shared = sourceByName.Keys.Intersect(targetByName.Keys).ToArray();
            var maxPositionDelta = 0f;
            var maxRotationDelta = 0f;
            var maxScaleDelta = 0f;
            var worstPoseName = string.Empty;
            for (var i = 0; i < shared.Length; i++)
            {
                if (!shared[i].StartsWith("Bone_", System.StringComparison.Ordinal)) continue;
                if (sourceByName[shared[i]].Count != 1 || targetByName[shared[i]].Count != 1) continue;
                var a = sourceByName[shared[i]][0];
                var b = targetByName[shared[i]][0];
                var positionDelta = Vector3.Distance(a.localPosition, b.localPosition);
                var rotationDelta = Quaternion.Angle(a.localRotation, b.localRotation);
                var scaleDelta = Vector3.Distance(a.localScale, b.localScale);
                Debug.Log($"[AnimationAssetAuditIter974] Iter1062 bone {shared[i]}: localPosSource={a.localPosition:F4}, localPosTarget={b.localPosition:F4}, posDelta={positionDelta:F4}m, rotationDelta={rotationDelta:F2}deg, scaleDelta={scaleDelta:F4}.");
                var severity = positionDelta + rotationDelta * 0.01f + scaleDelta;
                var previousSeverity = maxPositionDelta + maxRotationDelta * 0.01f + maxScaleDelta;
                if (severity > previousSeverity)
                {
                    maxPositionDelta = positionDelta;
                    maxRotationDelta = rotationDelta;
                    maxScaleDelta = scaleDelta;
                    worstPoseName = shared[i];
                }
            }

            Debug.Log($"[AnimationAssetAuditIter974] Iter1062 rig-local pose comparison: sharedNames={shared.Length}, sourceUnique={sourceByName.Count(pair => pair.Value.Count == 1)}, targetUnique={targetByName.Count(pair => pair.Value.Count == 1)}, maxLocalPositionDelta={maxPositionDelta:F4}m, maxLocalRotationDelta={maxRotationDelta:F2}deg, maxLocalScaleDelta={maxScaleDelta:F4}, worstBone={worstPoseName}.");
            var clips = AssetDatabase.LoadAllAssetsAtPath(animationPath).OfType<AnimationClip>()
                .Where(clip => clip != null && !clip.name.StartsWith("__preview__")).ToArray();
            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                var unique = 0;
                var ambiguous = 0;
                var missing = 0;
                foreach (var binding in bindings)
                {
                    var parts = binding.path.Split('/');
                    var terminal = parts[parts.Length - 1];
                    if (!targetByName.TryGetValue(terminal, out var matches)) missing++;
                    else if (matches.Count != 1) ambiguous++;
                    else unique++;
                }

                Debug.Log($"[AnimationAssetAuditIter974] Iter1062 {clip.name}: bindings={bindings.Length}, uniqueTerminalTargets={unique}, ambiguousTerminalTargets={ambiguous}, missingTerminalTargets={missing}.");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Transform>> BuildNameMap(Transform root)
        {
            var map = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Transform>>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (!map.TryGetValue(transform.name, out var entries))
                {
                    entries = new System.Collections.Generic.List<Transform>();
                    map.Add(transform.name, entries);
                }
                entries.Add(transform);
            }
            return map;
        }

        public static void Run()
        {
            var paths = new[]
            {
                "Assets/SpaceAndCrafts/Art/Commissioned/Hero/Splat_Hero.fbx",
                "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/ScrapMite.fbx",
                "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/ScrapRoller.fbx",
                "Assets/SpaceAndCrafts/Resources/Characters/Splat_AnimationActions_Iter911.fbx",
                "Assets/SpaceAndCrafts/Art/Animations/Splat_Idle.anim",
                "Assets/SpaceAndCrafts/Art/Animations/Splat_Run.anim",
                "Assets/SpaceAndCrafts/Art/Animations/Splat_Jump.anim",
                "Assets/SpaceAndCrafts/Art/Animations/Splat_Land.anim",
                "Assets/SpaceAndCrafts/Art/Animations/Splat_Interact.anim",
                "Assets/SpaceAndCrafts/Art/Animations/Splat_Craft.anim"
            };

            foreach (var path in paths)
            {
                var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .Where(clip => clip != null && !clip.name.StartsWith("__preview__"))
                    .ToArray();
                var names = clips.Length == 0 ? "none" : string.Join(", ", clips.Select(clip =>
                    $"{clip.name}[{clip.length:F2}s, bindings={AnimationUtility.GetCurveBindings(clip).Length}]"));
                Debug.Log($"[AnimationAssetAuditIter974] {path}: imported clips={clips.Length}; {names}.");
            }

            var animModel = AssetDatabase.LoadAssetAtPath<GameObject>(paths[3]);
            var candidate = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SpaceAndCrafts/Resources/Characters/Splat_Rigged_Candidate_Iter378.fbx");
            AuditBindingCompatibility(paths[3], animModel, candidate, "candidate Iter378");
            AuditBindingCompatibility(paths[3], animModel,
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SpaceAndCrafts/Prefabs/Characters/Splat_TEMP.prefab"),
                "active Splat prefab");

            var prefabPath = "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_TEMP.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                var animatorCount = prefab.GetComponentsInChildren<Animator>(true).Length;
                var animationCount = prefab.GetComponentsInChildren<Animation>(true).Length;
                Debug.Log($"[AnimationAssetAuditIter974] active Splat prefab {prefabPath}: Animator={animatorCount}, legacy Animation={animationCount}, procedural SplatSkeletonAnimator={prefab.GetComponentInChildren<SpaceAndCrafts.Player.SplatSkeletonAnimator>(true) != null}.");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void AuditBindingCompatibility(string animationPath, GameObject animationModel,
            GameObject target, string targetName)
        {
            if (animationModel == null || target == null)
            {
                Debug.Log($"[AnimationAssetAuditIter974] Compatibility {targetName}: missing model.");
                return;
            }

            var clips = AssetDatabase.LoadAllAssetsAtPath(animationPath).OfType<AnimationClip>()
                .Where(clip => clip != null && !clip.name.StartsWith("__preview__")).ToArray();
            var targetTransforms = target.GetComponentsInChildren<Transform>(true);
            var targetPaths = new System.Collections.Generic.HashSet<string>();
            var targetNames = new System.Collections.Generic.HashSet<string>();
            for (var i = 0; i < targetTransforms.Length; i++)
            {
                targetPaths.Add(AnimationUtility.CalculateTransformPath(targetTransforms[i], target.transform));
                targetNames.Add(targetTransforms[i].name);
            }

            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                var exact = 0;
                var nameMatches = 0;
                foreach (var binding in bindings)
                {
                    if (targetPaths.Contains(binding.path)) exact++;
                    if (targetNames.Contains(binding.path.Split('/').Length > 0
                            ? binding.path.Split('/')[binding.path.Split('/').Length - 1]
                            : binding.path)) nameMatches++;
                }

                Debug.Log($"[AnimationAssetAuditIter974] Compatibility {clip.name} -> {targetName}: paths={bindings.Length}, exactPathMatches={exact}, sameBoneNameMatches={nameMatches}, animationModelSkins={animationModel.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length}, targetSkins={target.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length}.");
            }
        }
    }
}
#endif
