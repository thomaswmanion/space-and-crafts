#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Read-only Unity compatibility audit for the existing Perforation Warden asset set.</summary>
    public static class PerforationWardenAssetAuditIter2235
    {
        private const string Root = "Assets/SpaceAndCrafts/Art/Enemies/PerforationWarden/Meshy_Iter1951/";
        private static readonly string[] Paths =
        {
            Root + "PerforationWarden_Rigged.fbx",
            Root + "PerforationWarden_Actions.fbx",
            Root + "PerforationWarden_Walking.fbx",
            Root + "PerforationWarden_Running.fbx",
        };

        [MenuItem("Space & Crafts/Assets/Audit Perforation Warden Iter2235")]
        public static void Audit()
        {
            var riggedRoot = AssetDatabase.LoadAssetAtPath<GameObject>(Paths[0]);
            if (riggedRoot == null) throw new InvalidOperationException($"Missing rigged root: {Paths[0]}");
            var rigPaths = riggedRoot.GetComponentsInChildren<Transform>(true)
                .Select(item => AnimationUtility.CalculateTransformPath(item, riggedRoot.transform))
                .ToHashSet(StringComparer.Ordinal);
            var skins = riggedRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var vertices = skins.Sum(item => item.sharedMesh ? item.sharedMesh.vertexCount : 0);
            var bones = skins.Sum(item => item.bones == null ? 0 : item.bones.Length);
            var rigImporter = AssetImporter.GetAtPath(Paths[0]) as ModelImporter;
            var skinLocations = string.Join("|", skins.Select(item => AnimationUtility.CalculateTransformPath(item.transform, riggedRoot.transform)));
            var animators = riggedRoot.GetComponentsInChildren<Animator>(true);
            var animatorLocations = string.Join("|", animators.Select(item => AnimationUtility.CalculateTransformPath(item.transform, riggedRoot.transform)));
            var rootComponents = string.Join(",", riggedRoot.GetComponents<Component>().Where(item => item != null).Select(item => item.GetType().Name));
            Debug.Log($"[PerforationWardenAudit2235] rigType={rigImporter?.animationType.ToString() ?? "unknown"}; rootComponents={rootComponents}; renderers={skins.Length}; skinPaths={skinLocations}; animators={animators.Length}; animatorPaths={animatorLocations}; bones={bones}; vertices={vertices}; hierarchyPaths={rigPaths.Count}.");
            foreach (var transform in riggedRoot.GetComponentsInChildren<Transform>(true))
                Debug.Log($"[PerforationWardenAudit2235] node={AnimationUtility.CalculateTransformPath(transform, riggedRoot.transform)}; name={transform.name}; localPosition={transform.localPosition}; localRotation={transform.localRotation.eulerAngles}.");

            var failed = 0;
            foreach (var path in Paths)
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null)
                {
                    Debug.LogError($"[PerforationWardenAudit2235] Missing asset: {path}");
                    failed++;
                    continue;
                }
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .Where(clip => clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
                Debug.Log($"[PerforationWardenAudit2235] asset={path}; rigType={importer?.animationType.ToString() ?? "unknown"}; clips={clips.Length}.");
                foreach (var clip in clips)
                {
                    var paths = AnimationUtility.GetCurveBindings(clip)
                        .Where(binding => binding.type == typeof(Transform))
                        .Select(binding => binding.path).Distinct(StringComparer.Ordinal).ToArray();
                    var matched = paths.Count(rigPaths.Contains);
                    var unmatched = paths.Where(item => !rigPaths.Contains(item)).Take(6).ToArray();
                    Debug.Log($"[PerforationWardenAudit2235] clip={clip.name}; seconds={clip.length:F2}; transformPaths={matched}/{paths.Length}; unmatched={(unmatched.Length == 0 ? "none" : string.Join("|", unmatched))}.");
                    if (paths.Length == 0 || matched != paths.Length) failed++;
                }
                if (clips.Length == 0) failed++;
            }
            Debug.Log($"[PerforationWardenAudit2235] Completed; failures={failed}; no assets or scenes modified.");
            if (Application.isBatchMode) EditorApplication.Exit(failed == 0 ? 0 : 3);
        }
    }
}
#endif
