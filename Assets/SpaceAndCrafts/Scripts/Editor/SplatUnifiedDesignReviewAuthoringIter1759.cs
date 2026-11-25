#if UNITY_EDITOR
using System;
using System.Linq;
using SpaceAndCrafts.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Authors a separate Splat design-review prefab from the Iter1725 Generic-rig
    /// candidate. It never edits or promotes a production prefab or scene.
    /// </summary>
    public static class SplatUnifiedDesignReviewAuthoringIter1759
    {
        private const string SourcePrefabPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/ReviewOnly_Iter1725/Splat_Iter1725_ModelRoot_ReviewOnly.prefab";
        private const string SourceShaderPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/SplatCandidateVertexColorPreview.shader";
        private const string EyeMaterialPath = "Assets/SpaceAndCrafts/Materials/TempArt/Mat_SplatEye.mat";
        private const string SmileMaterialPath = "Assets/SpaceAndCrafts/Materials/TempArt/Mat_SplatSmile.mat";
        private const string EyeGlintMaterialPath = "Assets/SpaceAndCrafts/Materials/TempArt/Mat_SplatEyeGlint.mat";
        private const string CapMaterialPath = "Assets/SpaceAndCrafts/Materials/TempArt/Mat_SplatBackwardsCap.mat";
        private const string OutputFolder =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/ReviewOnly_Iter1759";
        private const string OutputMaterialPath = OutputFolder + "/Splat_Iter1759_VertexColor_ReviewOnly.mat";
        private const string OutputPrefabPath = OutputFolder + "/Splat_Iter1759_ProportionPalette_ReviewOnly.prefab";

        [MenuItem("Space & Crafts/Author Splat Iter1759 Proportion and Palette Review Prefab")]
        public static void Author()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath) != null ||
                AssetDatabase.LoadAssetAtPath<Material>(OutputMaterialPath) != null)
                throw new InvalidOperationException("Refusing to overwrite existing Iter1759 review assets.");

            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(SourceShaderPath);
            var eye = AssetDatabase.LoadAssetAtPath<Material>(EyeMaterialPath);
            var smile = AssetDatabase.LoadAssetAtPath<Material>(SmileMaterialPath);
            var eyeGlint = AssetDatabase.LoadAssetAtPath<Material>(EyeGlintMaterialPath);
            var cap = AssetDatabase.LoadAssetAtPath<Material>(CapMaterialPath);
            if (sourcePrefab == null || shader == null || eye == null || smile == null || eyeGlint == null || cap == null)
                throw new InvalidOperationException("A source prefab, preview shader, or approved-palette material is missing.");

            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                const string parent = "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717";
                AssetDatabase.CreateFolder(parent, "ReviewOnly_Iter1759");
            }

            var vertexMaterial = new Material(shader)
            {
                name = "Splat Iter1759 Vertex Colors (Review Only)",
            };
            AssetDatabase.CreateAsset(vertexMaterial, OutputMaterialPath);
            AssetDatabase.SaveAssets();

            var previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = PrefabUtility.InstantiatePrefab(sourcePrefab, previewScene) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException("Could not instantiate the existing ReviewOnly Splat rig.");
                instance.name = "Splat_Iter1759_ProportionPalette_ReviewOnly";

                // Small non-destructive corrections; preserve vertical boot height
                // so later support-plane contact work is not undermined.
                ScaleUniqueBone(instance.transform, "Hand_L", new Vector3(0.90f, 0.90f, 0.90f));
                ScaleUniqueBone(instance.transform, "Hand_R", new Vector3(0.90f, 0.90f, 0.90f));
                ScaleUniqueBone(instance.transform, "Foot_L", new Vector3(0.95f, 1f, 0.95f));
                ScaleUniqueBone(instance.transform, "Foot_R", new Vector3(0.95f, 1f, 0.95f));

                SetRendererMaterial(instance, "Splat_UnifiedBodyAndLimbs", vertexMaterial);
                SetRendererMaterial(instance, "Eye_L", eye);
                SetRendererMaterial(instance, "Eye_R", eye);
                SetRendererMaterial(instance, "Brow_L", eye);
                SetRendererMaterial(instance, "Brow_R", eye);
                SetRendererMaterial(instance, "EyeCatch_L", eyeGlint);
                SetRendererMaterial(instance, "EyeCatch_R", eyeGlint);
                SetRendererMaterial(instance, "Splat friendly smile", smile);
                SetRendererMaterial(instance, "Backwards cap crown", cap);
                SetRendererMaterial(instance, "Backwards cap rear brim", cap);
                SetRendererMaterial(instance, "Cap front strap", cap);

                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath);
                if (prefab == null)
                    throw new InvalidOperationException("Could not save the separate Iter1759 review prefab.");
                AssetDatabase.SaveAssets();
                Debug.Log("[Splat Iter1759] Authored a proportion/palette ReviewOnly prefab; production assets untouched: " + OutputPrefabPath);
                EditorApplication.Exit(0);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static void ScaleUniqueBone(Transform root, string boneName, Vector3 factor)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(candidate => candidate.name == boneName)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException($"Expected one {boneName} bone, found {matches.Length}.");
            matches[0].localScale = Vector3.Scale(matches[0].localScale, factor);
        }

        private static void SetRendererMaterial(GameObject root, string rendererName, Material material)
        {
            var matches = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(candidate => candidate.name == rendererName)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException($"Expected one {rendererName} skinned renderer, found {matches.Length}.");
            var renderer = matches[0];
            var materials = renderer.sharedMaterials;
            if (materials.Length == 0) materials = new[] { material };
            else for (var i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
    }
}
#endif
