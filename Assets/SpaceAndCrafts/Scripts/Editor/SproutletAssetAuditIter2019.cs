#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Read-only structural audit of the Meshy Sproutlet candidate assets.</summary>
    public static class SproutletAssetAuditIter2019
    {
        private static readonly string[] Paths =
        {
            "Assets/SpaceAndCrafts/Art/Characters/Sproutlet/Meshy_Iter2001/Sproutlet.fbx",
            "Assets/SpaceAndCrafts/Art/Characters/Sproutlet/Meshy_Iter2001/Sproutlet_Rigged.fbx",
            "Assets/SpaceAndCrafts/Art/Characters/Sproutlet/Meshy_Iter2001/Sproutlet_Actions.fbx",
            "Assets/SpaceAndCrafts/Art/Characters/Sproutlet/Meshy_Iter2001/Sproutlet_Walk.fbx",
            "Assets/SpaceAndCrafts/Art/Characters/Sproutlet/Meshy_Iter2001/Sproutlet_Run.fbx",
        };

        [MenuItem("Space & Crafts/Assets/Audit Sproutlet Meshy Candidate")]
        public static void Audit()
        {
            var failures = 0;
            var riggedRoot = AssetDatabase.LoadAssetAtPath<GameObject>(Paths[1]);
            var rigTransformPaths = riggedRoot
                ? riggedRoot.GetComponentsInChildren<Transform>(true)
                    .Select(transform => AnimationUtility.CalculateTransformPath(transform, riggedRoot.transform))
                    .ToHashSet(StringComparer.Ordinal)
                : new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var path in Paths)
            {
                if (!AssetDatabase.LoadMainAssetAtPath(path))
                {
                    Debug.LogError($"[SproutletAssetAudit2019] Missing asset: {path}");
                    failures++;
                    continue;
                }

                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                var rigType = importer == null ? "unknown" : importer.animationType.ToString();
                var all = AssetDatabase.LoadAllAssetsAtPath(path);
                var avatars = all.OfType<Avatar>().ToArray();
                var clips = all.OfType<AnimationClip>()
                    .Where(clip => clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    .ToArray();
                var skins = root ? root.GetComponentsInChildren<SkinnedMeshRenderer>(true) : Array.Empty<SkinnedMeshRenderer>();
                var boneCount = skins.Sum(skin => skin.bones == null ? 0 : skin.bones.Length);
                var vertexCount = skins.Sum(skin => skin.sharedMesh ? skin.sharedMesh.vertexCount : 0);
                var materialCount = skins.Sum(skin => skin.sharedMaterials == null ? 0 : skin.sharedMaterials.Length);
                var validAvatarCount = avatars.Count(avatar => avatar.isValid);
                var humanAvatarCount = avatars.Count(avatar => avatar.isHuman && avatar.isValid);

                Debug.Log($"[SproutletAssetAudit2019] asset={path}; root={(root ? root.name : "missing")}; rigType={rigType}; skins={skins.Length}; bones={boneCount}; vertices={vertexCount}; materials={materialCount}; avatars={avatars.Length}; validAvatars={validAvatarCount}; validHumanoidAvatars={humanAvatarCount}; clips={clips.Length}.");
                foreach (var avatar in avatars)
                    Debug.Log($"[SproutletAssetAudit2019] avatar={avatar.name}; valid={avatar.isValid}; human={avatar.isHuman}.");
                foreach (var clip in clips)
                {
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    var scale = bindings.Where(binding => binding.type == typeof(Transform) && binding.propertyName.Contains("Scale"))
                        .Select(binding => AnimationUtility.GetEditorCurve(clip, binding))
                        .Where(curve => curve != null)
                        .SelectMany(curve => curve.keys)
                        .Select(key => key.value)
                        .ToArray();
                    var scaleSummary = scale.Length == 0 ? "none" : $"[{scale.Min():F3},{scale.Max():F3}]";
                    Debug.Log($"[SproutletAssetAudit2019] clip={clip.name}; seconds={clip.length:F2}; bindings={bindings.Length}; scaleRange={scaleSummary}.");
                    if (path.Contains("Actions") || path.Contains("Walk") || path.Contains("Run"))
                    {
                        var transformPaths = bindings
                            .Where(binding => binding.type == typeof(Transform))
                            .Select(binding => binding.path)
                            .Distinct(StringComparer.Ordinal)
                            .ToArray();
                        var matched = transformPaths.Count(bindingPath => rigTransformPaths.Contains(bindingPath));
                        var missing = transformPaths.Where(bindingPath => !rigTransformPaths.Contains(bindingPath)).Take(8).ToArray();
                        Debug.Log($"[SproutletAssetAudit2019] clipRigBindingMatch={matched}/{transformPaths.Length}; unmatchedSample={(missing.Length == 0 ? "none" : string.Join("|", missing))}.");
                    }
                }

                // These FBXs are imported as Generic rigs: Unity does not require
                // a Humanoid Avatar for same-hierarchy Generic clip playback.
                // Only demand an Avatar when the model is actually imported as Human.
                var requiredAvatarMissing = importer != null
                    && importer.animationType == ModelImporterAnimationType.Human
                    && humanAvatarCount == 0;
                if (path.Contains("Rigged") && (skins.Length == 0 || boneCount == 0 || requiredAvatarMissing))
                {
                    Debug.LogWarning($"[SproutletAssetAudit2019] Rigged candidate lacks a skinned mesh/bone hierarchy or required Human Avatar: {path}");
                }
            }

            Debug.Log($"[SproutletAssetAudit2019] Completed read-only audit: {Paths.Length} FBX assets; missingAssets={failures}.");
        }
    }
}
#endif
