#if UNITY_EDITOR
using System;
using System.Linq;
using SpaceAndCrafts.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Replaces the three opening Rainbow Kingdom ScrapMite visuals with the
    /// existing commissioned, skinned Meshy spider. Original objects and AI
    /// remain in place; their old renderers are disabled for easy rollback.
    /// </summary>
    public static class PromoteRainbowSpiderArrivalCli
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity";
        private const string PrefabPath =
            "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1461/UnityCandidate/RainbowKingdomSpider_AnimatedCandidate.prefab";
        private const string VisualName = "ART_MeshyEnemyVisual";

        [MenuItem("Space & Crafts/Promote Existing Meshy Spider to World 01 Mites")]
        public static void Promote()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var mites = UnityEngine.Object.FindObjectsByType<ScrapMite>()
                .OrderBy(mite => mite.transform.position.z)
                .ThenBy(mite => mite.transform.position.x)
                .ToArray();
            if (prefab == null || mites.Length == 0)
                throw new InvalidOperationException($"Missing spider prefab or World 01 mites (prefab={prefab != null}, mites={mites.Length}).");

            var promoted = 0;
            foreach (var mite in mites)
            {
                if (mite == null || mite.GetComponentInChildren<RainbowSpiderAnimatorDriver>(true) != null)
                    continue;

                foreach (var renderer in mite.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null) continue;
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                }
                foreach (var legacyDriver in mite.GetComponentsInChildren<ScrapMiteCommissionedAnimator>(true))
                {
                    if (legacyDriver == null) continue;
                    legacyDriver.enabled = false;
                    EditorUtility.SetDirty(legacyDriver);
                }

                var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException("Could not instantiate the existing Meshy spider candidate.");
                instance.name = VisualName;
                instance.transform.SetParent(mite.transform, false);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                var skinned = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (renderers.Length == 0 || skinned.Length == 0
                    || instance.GetComponentInChildren<Animator>(true) == null
                    || instance.GetComponentInChildren<RainbowSpiderAnimatorDriver>(true) == null)
                    throw new InvalidOperationException($"Promoted visual under {mite.name} is missing its skinned renderer, Animator, or AI driver.");
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                    EditorUtility.SetDirty(renderer);
                }
                EditorUtility.SetDirty(instance);
                promoted++;
            }

            if (promoted == 0)
                throw new InvalidOperationException("World 01 already has Meshy spider visuals; no promotion was performed.");
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[World 01 Meshy spider] promoted={promoted}, totalMites={mites.Length}, saved={saved}, prefab={PrefabPath}; original visuals retained with renderers disabled.");
            if (Application.isBatchMode) EditorApplication.Exit(saved ? 0 : 1);
        }
    }
}
#endif