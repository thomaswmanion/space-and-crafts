#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Read-only Unity FBX/avatar/clip binding preflight for the unintegrated Pulpal candidate.</summary>
    public static class PulpalUnityImportAuditIter2181
    {
        private const string Root = "Assets/SpaceAndCrafts/Art/Enemies/Pulpal/Meshy_Iter1994/";
        private const string RigPath = Root + "Pulpal_Rigged.fbx";
        private const string Output = "Logs/score-comparison/pulpal-meshy-animation-review-iter2180/unity-import-report.txt";
        private static readonly string[] ClipPaths =
        {
            Root + "Pulpal_IdleAttackHitDefeatRecover.fbx",
            Root + "Pulpal_PaperPressAttack.fbx",
            Root + "Pulpal_Walk.fbx",
            Root + "Pulpal_Run.fbx",
        };

        [MenuItem("Space & Crafts/Audit Pulpal Unity Import (Iter 2181)")]
        public static void Run()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            var report = new StringBuilder();
            report.AppendLine("Pulpal Unity import/rig-binding audit; read-only, isolated, no scene or prefab changes.");
            report.AppendLine("This checks Unity asset import and exact Transform binding paths only; it does not render or verify motion, transitions, collision, gameplay or human play.");
            try
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
                if (model == null) throw new FileNotFoundException("Rigged FBX prefab did not load in Unity", RigPath);
                var importer = AssetImporter.GetAtPath(RigPath) as ModelImporter;
                var avatars = AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>().ToArray();
                var validAvatars = avatars.Where(avatar => avatar != null && avatar.isValid).ToArray();
                var animators = model.GetComponentsInChildren<Animator>(true);
                var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var hierarchy = model.GetComponentsInChildren<Transform>(true)
                    .Select(transform => AnimationUtility.CalculateTransformPath(transform, model.transform))
                    .ToHashSet(StringComparer.Ordinal);
                report.AppendLine($"Rig FBX loaded=true; importerAnimationType={importer?.animationType}; avatarSetup={importer?.avatarSetup}; importAnimation={importer?.importAnimation}; AvatarAssets={avatars.Length}; validAvatars={validAvatars.Length}; embeddedAnimators={animators.Length}; skins={skins.Length}; rigTransforms={hierarchy.Count}");
                foreach (var avatar in avatars)
                    report.AppendLine($"Avatar name={avatar.name}; isValid={avatar.isValid}; isHuman={avatar.isHuman}");
                foreach (var skin in skins)
                    report.AppendLine($"Skin name={skin.name}; mesh={(skin.sharedMesh != null ? skin.sharedMesh.name : "null")}; vertices={(skin.sharedMesh != null ? skin.sharedMesh.vertexCount : 0)}; materials={skin.sharedMaterials.Length}; bones={skin.bones.Length}");

                foreach (var clipPath in ClipPaths)
                {
                    var clips = AssetDatabase.LoadAllAssetsAtPath(clipPath).OfType<AnimationClip>()
                        .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    report.AppendLine($"CLIP_SOURCE {clipPath} loadedClips={clips.Length}");
                    if (clips.Length == 0) report.AppendLine("  STOP: Unity did not expose any AnimationClip at this path.");
                    foreach (var clip in clips)
                    {
                        var bindings = AnimationUtility.GetCurveBindings(clip)
                            .Where(binding => binding.type == typeof(Transform)).ToArray();
                        var paths = bindings.Select(binding => binding.path).Distinct(StringComparer.Ordinal).ToArray();
                        var missing = paths.Where(path => !hierarchy.Contains(path)).ToArray();
                        report.AppendLine($"  Clip={clip.name}; length={clip.length:F3}s; legacy={clip.legacy}; transformBindings={bindings.Length}; distinctPaths={paths.Length}; missingPaths={missing.Length}; pathCoverage={(paths.Length == 0 ? 0f : (paths.Length - missing.Length) / (float)paths.Length):P1}");
                        foreach (var path in missing.Take(20)) report.AppendLine("    missing=" + path);
                    }
                }
                File.WriteAllText(Output, report.ToString(), new UTF8Encoding(false));
                AssetDatabase.Refresh();
                Debug.Log("[Pulpal Iter2181] read-only Unity import audit written: " + Output);
            }
            catch (Exception exception)
            {
                report.AppendLine("AUDIT FAILURE: " + exception);
                File.WriteAllText(Output, report.ToString(), new UTF8Encoding(false));
                Debug.LogException(exception);
                throw;
            }
        }
    }
}
#endif
