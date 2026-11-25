#if UNITY_EDITOR
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Copies the promoted, imported Splat rig from the reviewed character prefab
    /// into the Arrival scene's standalone player object when that scene lacks it.
    /// </summary>
    public static class SplatArrivalRigIntegration
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity";
        private const string PrefabPath = "Assets/SpaceAndCrafts/Prefabs/Characters/Splat_TEMP.prefab";

        public static void Integrate()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || !SplatMeshyVisualMode.IsEnabledOn(player.transform))
                throw new System.InvalidOperationException("Arrival scene player or Meshy visual mode marker is missing.");

            var existing = SplatMeshyVisualMode.ResolveVisual(player.transform);
            if (existing == null)
            {
                var prefabContents = PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    var source = SplatMeshyVisualMode.ResolveVisual(prefabContents.transform);
                    if (source == null)
                        throw new System.InvalidOperationException("Splat_TEMP prefab has no resolvable skinned rig with the preferred controller.");

                    var rig = Object.Instantiate(source.gameObject);
                    rig.name = SplatMeshyVisualMode.MeshyVisualRootName;
                    rig.transform.SetParent(player.transform, false);
                    rig.transform.localPosition = source.localPosition;
                    rig.transform.localRotation = source.localRotation;
                    rig.transform.localScale = source.localScale;
                    Debug.Log($"[SplatArrivalRigIntegration] Added {source.name} ({source.GetComponent<Animator>()?.runtimeAnimatorController?.name}) to Arrival player.");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }
            }

            SplatVisualRepair.Repair(player.transform);
            var selected = SplatMeshyVisualMode.ResolveVisual(player.transform);
            if (selected == null || SplatMeshyAnimatorDriver.FindForPlayer(player.transform) == null)
                throw new System.InvalidOperationException("Arrival rig integration did not produce an active Meshy animator driver.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.InvalidOperationException("Could not save Arrival scene after rig integration.");

            Debug.Log($"[SplatArrivalRigIntegration] Saved Arrival with {selected.name} using {selected.GetComponent<Animator>()?.runtimeAnimatorController?.name}.");
        }
    }
}
#endif
