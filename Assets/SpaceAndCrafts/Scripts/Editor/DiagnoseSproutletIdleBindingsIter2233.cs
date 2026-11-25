#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Read-only report of non-transform curves in the imported Sproutlet animation clips.</summary>
    public static class DiagnoseSproutletIdleBindingsIter2233
    {
        private const string ActionsPath = "Assets/SpaceAndCrafts/Art/Characters/Sproutlet/Meshy_Iter2001/Sproutlet_Actions.fbx";

        [MenuItem("Space & Crafts/Assets/Diagnose Sproutlet Idle Bindings Iter2233")]
        public static void Diagnose()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ActionsPath).OfType<AnimationClip>()
                .Where(clip => clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            if (clips.Length == 0) throw new InvalidOperationException("No imported action clips found.");

            foreach (var clip in clips)
            {
                var curveBindings = AnimationUtility.GetCurveBindings(clip)
                    .Where(binding => binding.type != typeof(Transform))
                    .Select(binding => $"{binding.type.Name}:{binding.path}:{binding.propertyName}")
                    .ToArray();
                var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                var objectSummary = objectBindings.Select(binding =>
                {
                    var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding) ?? Array.Empty<ObjectReferenceKeyframe>();
                    var values = keys.Select(key => key.value ? key.value.name : "null").Distinct().ToArray();
                    return $"{binding.type.Name}:{binding.path}:{binding.propertyName};keys={keys.Length};values={string.Join("|", values)}";
                }).ToArray();
                Debug.Log($"[SproutletIdleBindings2233] clip={clip.name}; nonTransformCurves={(curveBindings.Length == 0 ? "none" : string.Join(" || ", curveBindings))}; objectCurves={(objectSummary.Length == 0 ? "none" : string.Join(" || ", objectSummary))}.");
            }

            Debug.Log($"[SproutletIdleBindings2233] Read-only diagnostic completed; clips={clips.Length}; no assets modified.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
#endif
