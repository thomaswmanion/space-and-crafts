#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>Creates a non-destructive review prefab with the existing GLB base-color atlas.</summary>
    public static class CreateSproutletTexturedReviewIter2231
    {
        private const string Root = "Assets/SpaceAndCrafts/Art/Characters/Sproutlet/Meshy_Iter2001/Review";
        private const string SourcePrefab = Root + "/Sproutlet_Review_Iter2228.prefab";
        private const string TexturePath = Root + "/Sproutlet_BaseColor_Iter2231.png";
        private const string MaterialPath = Root + "/Sproutlet_ReviewMaterial_Iter2231.mat";
        private const string PrefabPath = Root + "/Sproutlet_Review_Textured_Iter2231.prefab";

        [MenuItem("Space & Crafts/Assets/Create Textured Sproutlet Review Iter2231")]
        public static void Create()
        {
            if (AssetDatabase.LoadMainAssetAtPath(MaterialPath) != null
                || AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null)
                throw new InvalidOperationException("Refusing to overwrite Sproutlet Iter2231 review assets.");

            AssetDatabase.Refresh();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (source == null || texture == null)
                throw new InvalidOperationException($"Missing source prefab/atlas: {SourcePrefab}; {TexturePath}");

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No supported Lit shader found.");

            var material = new Material(shader) { name = "Sproutlet_ReviewMaterial_Iter2231" };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            else if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            AssetDatabase.CreateAsset(material, MaterialPath);

            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate source review prefab.");
            instance.name = "Sproutlet_Review_Textured_Iter2231";
            var skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(skin => skin.sharedMesh != null).ToArray();
            if (skins.Length != 1)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                throw new InvalidOperationException($"Expected one skinned mesh; found {skins.Length}.");
            }
            var rendererName = skins[0].name;
            skins[0].sharedMaterial = material;

            var result = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath, out var saved);
            UnityEngine.Object.DestroyImmediate(instance);
            if (!saved || result == null) throw new InvalidOperationException("Failed to save textured review prefab.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SproutletReview2231] Created textured review variant {PrefabPath}; renderer={rendererName}; texture={texture.width}x{texture.height}; material={shader.name}; source prefab and scenes unchanged.");
        }
    }
}
#endif
