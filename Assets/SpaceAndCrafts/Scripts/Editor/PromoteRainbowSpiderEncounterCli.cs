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
    /// Promotes the existing skinned Meshy spider to one World 02 Mite encounter.
    /// The original hierarchy remains in the scene, disabled, so the change is reversible.
    /// </summary>
    public static class PromoteRainbowSpiderEncounterCli
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World02_CorkscrewCanopy.unity";
        private const string EnemyName = "ART_ScrapMite_6";
        private const string VisualName = "ART_MeshyEnemyVisual";
        private const string PrefabPath =
            "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1461/UnityCandidate/RainbowKingdomSpider_AnimatedCandidate.prefab";

        [MenuItem("Space & Crafts/Promote Meshy Spider to World 02 Mite 6")]
        public static void Promote()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var miteObject = GameObject.Find(EnemyName);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (miteObject == null || prefab == null)
                throw new InvalidOperationException($"Missing target or saved spider prefab (target={miteObject != null}, prefab={prefab != null}).");

            var mite = miteObject.GetComponent<ScrapMite>();
            if (mite == null)
                throw new InvalidOperationException($"{EnemyName} does not have its ScrapMite encounter controller.");

            var existingSpider = miteObject.GetComponentsInChildren<RainbowSpiderAnimatorDriver>(true)
                .FirstOrDefault(driver => driver != null && driver.transform.IsChildOf(miteObject.transform));
            if (existingSpider != null)
            {
                Debug.Log($"[Rainbow spider promotion] Already present under {EnemyName}; no changes made.");
                EditorApplication.Exit(0);
                return;
            }

            // Retain all original meshes and scripts in the scene, but remove their
            // renderers and authoring pose driver while the Meshy rig owns this enemy.
            var oldRenderers = miteObject.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < oldRenderers.Length; i++)
            {
                if (oldRenderers[i] == null) continue;
                oldRenderers[i].enabled = false;
                EditorUtility.SetDirty(oldRenderers[i]);
            }
            var oldPivotDrivers = miteObject.GetComponentsInChildren<ScrapMiteCommissionedAnimator>(true);
            for (var i = 0; i < oldPivotDrivers.Length; i++)
            {
                if (oldPivotDrivers[i] == null) continue;
                oldPivotDrivers[i].enabled = false;
                EditorUtility.SetDirty(oldPivotDrivers[i]);
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Could not instantiate the saved skinned Meshy spider candidate.");
            instance.name = VisualName;
            instance.transform.SetParent(miteObject.transform, false);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0 || instance.GetComponentInChildren<Animator>(true) == null
                || instance.GetComponentInChildren<RainbowSpiderAnimatorDriver>(true) == null)
                throw new InvalidOperationException("Promoted spider is missing its renderer, Animator, or ScrapMite animation driver.");

            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = true;
                EditorUtility.SetDirty(renderers[i]);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Rainbow spider promotion] target={EnemyName}, skinnedRenderers={instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length}, animator={instance.GetComponentInChildren<Animator>(true).runtimeAnimatorController?.name}, driver=serialized, originalVisuals=retained-disabled, sceneSaved={saved}.");
            EditorApplication.Exit(saved ? 0 : 1);
        }
    }
}
#endif
