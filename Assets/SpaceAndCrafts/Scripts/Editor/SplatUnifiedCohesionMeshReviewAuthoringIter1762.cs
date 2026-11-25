#if UNITY_EDITOR
using System;
using System.Linq;
using SpaceAndCrafts.Player;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Creates an isolated, mesh-level Splat silhouette review candidate. The
    /// source FBX, its imported mesh, and all production assets remain untouched.
    /// </summary>
    public static class SplatUnifiedCohesionMeshReviewAuthoringIter1762
    {
        private const string ModelPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/Splat_UnifiedRig_Candidate_Iter1717.fbx";
        private const string SourcePrefabPath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/ReviewOnly_Iter1725/Splat_Iter1725_ModelRoot_ReviewOnly.prefab";
        private const string BodyPalettePath =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/ReviewOnly_Iter1759/Splat_Iter1759_VertexColor_ReviewOnly.mat";
        private const string EyeMaterialPath = "Assets/SpaceAndCrafts/Materials/TempArt/Mat_SplatEye.mat";
        private const string SmileMaterialPath = "Assets/SpaceAndCrafts/Materials/TempArt/Mat_SplatSmile.mat";
        private const string EyeGlintMaterialPath = "Assets/SpaceAndCrafts/Materials/TempArt/Mat_SplatEyeGlint.mat";
        private const string CapMaterialPath = "Assets/SpaceAndCrafts/Materials/TempArt/Mat_SplatBackwardsCap.mat";
        private const string OutputFolder =
            "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717/ReviewOnly_Iter1762";
        private const string OutputMeshPath = OutputFolder + "/Splat_Iter1762_CohesionMesh_ReviewOnly.asset";
        private const string OutputPrefabPath = OutputFolder + "/Splat_Iter1762_CohesionMesh_ReviewOnly.prefab";
        private const float BodyDepthExpansion = 1.20f;

        [MenuItem("Space & Crafts/Author Splat Iter1762 Cohesion Mesh Review Prefab")]
        public static void Author()
        {
            try
            {
                AuthorCore();
                Debug.Log("[Splat Iter1762] Authored a ReviewOnly mesh-cohesion variant; production assets untouched: " + OutputPrefabPath);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void AuthorCore()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.LoadAssetAtPath<Mesh>(OutputMeshPath) != null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath) != null)
                throw new InvalidOperationException("Refusing to overwrite an existing Iter1762 ReviewOnly asset.");
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1717", "ReviewOnly_Iter1762");

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            var vertexColorMaterial = AssetDatabase.LoadAssetAtPath<Material>(BodyPalettePath);
            var eye = AssetDatabase.LoadAssetAtPath<Material>(EyeMaterialPath);
            var smile = AssetDatabase.LoadAssetAtPath<Material>(SmileMaterialPath);
            var glint = AssetDatabase.LoadAssetAtPath<Material>(EyeGlintMaterialPath);
            var cap = AssetDatabase.LoadAssetAtPath<Material>(CapMaterialPath);
            if (model == null || sourcePrefab == null || vertexColorMaterial == null || eye == null || smile == null || glint == null || cap == null)
                throw new InvalidOperationException("A source model, Iter1725 prefab, existing palette material, or facial/cap material is missing.");

            var sourceSkins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var sourceRenderer = ExactlyOne(sourceSkins.Where(r => r.name == "Splat_UnifiedBodyAndLimbs"), "source core renderer");
            var sourceMesh = sourceRenderer.sharedMesh;
            if (sourceMesh == null || sourceMesh.vertexCount != 99428 || sourceMesh.subMeshCount != 1 ||
                !sourceMesh.HasVertexAttribute(VertexAttribute.Color))
                throw new InvalidOperationException("Refusing mesh edit: source core mesh layout changed from the audited one-submesh vertex-colored skin.");

            var bones = sourceRenderer.bones;
            var bindposes = sourceMesh.bindposes;
            if (bones == null || bindposes.Length != bones.Length)
                throw new InvalidOperationException("Core mesh bindposes no longer match its bone array.");
            var bodyIndex = BoneIndex(bones, "Body");
            var bodyWeights = GetBoneWeights(sourceMesh, bodyIndex);
            var sourceVertices = sourceMesh.vertices;
            var colors = sourceMesh.colors;
            if (sourceVertices.Length != sourceMesh.vertexCount || colors.Length != sourceVertices.Length)
                throw new InvalidOperationException("Core mesh vertices or vertex colors are unavailable.");

            var bodyMasks = new float[sourceVertices.Length];
            var bodyCentroid = Vector3.zero;
            var bodyMaskSum = 0f;
            var bodyAffected = 0;
            for (var i = 0; i < sourceVertices.Length; i++)
            {
                var whiteMask = WhiteBodyColorMask(colors[i]);
                var mask = Mathf.Clamp01(bodyWeights[i]) * whiteMask;
                bodyMasks[i] = mask;
                if (mask <= 0.001f) continue;
                bodyCentroid += sourceVertices[i] * mask;
                bodyMaskSum += mask;
                if (mask >= 0.05f) bodyAffected++;
            }
            if (bodyMaskSum < 1f)
                throw new InvalidOperationException("Could not isolate a weighted white body region from the source vertex colors.");
            bodyCentroid /= bodyMaskSum;

            var meshCopy = UnityEngine.Object.Instantiate(sourceMesh);
            meshCopy.name = "Splat_Iter1762_CohesionMesh_ReviewOnly";
            var editedVertices = new Vector3[sourceVertices.Length];
            for (var i = 0; i < sourceVertices.Length; i++)
            {
                var point = sourceVertices[i];

                // Expand only the off-white vertices weighted to Body, centered
                // on the weighted white-core centroid. The multiplier reaches
                // 1.20 only at full Body weight and fully neutral white color.
                var bodyInfluence = bodyMasks[i];
                if (bodyInfluence > 0.001f)
                {
                    point.z = bodyCentroid.z + (point.z - bodyCentroid.z)
                        * (1f + (BodyDepthExpansion - 1f) * bodyInfluence);
                }

                editedVertices[i] = point;
            }

            meshCopy.vertices = editedVertices;
            meshCopy.RecalculateNormals();
            if (meshCopy.HasVertexAttribute(VertexAttribute.TexCoord0)) meshCopy.RecalculateTangents();
            meshCopy.RecalculateBounds();
            AssetDatabase.CreateAsset(meshCopy, OutputMeshPath);
            AssetDatabase.SaveAssets();

            var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(OutputMeshPath);
            if (savedMesh == null) throw new InvalidOperationException("Cloned ReviewOnly mesh asset failed to save.");

            var previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = PrefabUtility.InstantiatePrefab(sourcePrefab, previewScene) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate the Iter1725 ReviewOnly prefab.");
                instance.name = "Splat_Iter1762_CohesionMesh_ReviewOnly";
                var coreRenderer = ExactlyOne(instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(r => r.name == "Splat_UnifiedBodyAndLimbs"), "ReviewOnly core renderer");
                coreRenderer.sharedMesh = savedMesh;

                SetRendererMaterial(instance, "Splat_UnifiedBodyAndLimbs", vertexColorMaterial);
                SetRendererMaterial(instance, "Eye_L", eye);
                SetRendererMaterial(instance, "Eye_R", eye);
                SetRendererMaterial(instance, "Brow_L", eye);
                SetRendererMaterial(instance, "Brow_R", eye);
                SetRendererMaterial(instance, "EyeCatch_L", glint);
                SetRendererMaterial(instance, "EyeCatch_R", glint);
                SetRendererMaterial(instance, "Splat friendly smile", smile);
                SetRendererMaterial(instance, "Backwards cap crown", cap);
                SetRendererMaterial(instance, "Backwards cap rear brim", cap);
                SetRendererMaterial(instance, "Cap front strap", cap);

                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath);
                if (prefab == null) throw new InvalidOperationException("Could not save the Iter1762 ReviewOnly prefab variant.");
                AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.ClosePreviewScene(previewScene); }

        }

        private static float[] GetBoneWeights(Mesh mesh, int boneIndex)
        {
            var results = new float[mesh.vertexCount];
            using NativeArray<byte> perVertex = mesh.GetBonesPerVertex();
            using NativeArray<BoneWeight1> weights = mesh.GetAllBoneWeights();
            var cursor = 0;
            for (var vertex = 0; vertex < perVertex.Length; vertex++)
            {
                for (var influence = 0; influence < perVertex[vertex] && cursor < weights.Length; influence++, cursor++)
                    if (weights[cursor].boneIndex == boneIndex) results[vertex] += weights[cursor].weight;
            }
            return results;
        }

        private static float WhiteBodyColorMask(Color color)
        {
            var minimum = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            var maximum = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var neutral = 1f - SmoothStep(0.10f, 0.28f, maximum - minimum);
            return SmoothStep(0.70f, 0.92f, minimum) * neutral;
        }

        private static float SmoothStep(float edge0, float edge1, float value)
        {
            var t = Mathf.Clamp01((value - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static int BoneIndex(Transform[] bones, string name)
        {
            var matches = bones.Select((bone, index) => (bone, index)).Where(entry => entry.bone != null && entry.bone.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException($"Expected one {name} in the core mesh bone list, found {matches.Length}.");
            return matches[0].index;
        }

        private static T ExactlyOne<T>(System.Collections.Generic.IEnumerable<T> values, string description)
        {
            var matches = values.ToArray();
            if (matches.Length != 1) throw new InvalidOperationException($"Expected one {description}, found {matches.Length}.");
            return matches[0];
        }

        private static void SetRendererMaterial(GameObject root, string rendererName, Material material)
        {
            var renderer = ExactlyOne(root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(candidate => candidate.name == rendererName), rendererName + " skinned renderer");
            var materials = renderer.sharedMaterials;
            if (materials.Length == 0) materials = new[] { material };
            else for (var i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
    }
}
#endif
