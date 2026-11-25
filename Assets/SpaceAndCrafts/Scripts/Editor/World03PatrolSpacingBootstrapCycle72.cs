#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Stages the World 03 traversal encounter later in the pad sequence.</summary>
    public static class World03PatrolSpacingBootstrapCycle72
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity";
        private const float EncounterPackRouteZ = 6f;

        [MenuItem("Space & Crafts/Apply World 03 Staggered Encounter Cycle 72")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var encounterPack = GameObject.Find("ART_EncounterPack");
            if (encounterPack == null)
            {
                Debug.LogError("[World 03 spacing C72] ART_EncounterPack is missing.");
                EditorApplication.Exit(1);
                return;
            }

            var position = encounterPack.transform.position;
            position.z = EncounterPackRouteZ;
            encounterPack.transform.position = position;
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[World 03 spacing C72] moved ART_EncounterPack route center to z={EncounterPackRouteZ:F2}m; saved={saved}; normal mite count retained.");
            EditorApplication.Exit(saved ? 0 : 1);
        }
    }
}
#endif
