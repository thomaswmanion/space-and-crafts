#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Ensures commissioned FBX/glTF drop folders + README exist so art can land without inventing tooling mid-delivery.
    /// Does not import models — only scaffolds the swap pipeline.
    /// </summary>
    public static class CommissionedArtPipelineBootstrap
    {
        private const string Root = "Assets/SpaceAndCrafts/Art/Commissioned";
        private static readonly string[] Slots =
        {
            "Hero",
            "Inventions",
            "Enemies",
            "Landmarks",
            "Environments",
            "VFX",
        };

        [MenuItem("Space & Crafts/Scaffold Commissioned Art Pipeline")]
        public static void Bootstrap()
        {
            EnsureFolder(Root);
            for (var i = 0; i < Slots.Length; i++)
            {
                EnsureFolder($"{Root}/{Slots[i]}");
            }

            var readmePath = $"{Root}/README.md";
            if (!File.Exists(readmePath))
            {
                File.WriteAllText(readmePath, ReadmeBody);
                AssetDatabase.ImportAsset(readmePath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Commissioned art pipeline scaffolded under {Root}.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>Returns true when any commissioned model file is present (pipeline no longer empty).</summary>
        public static bool HasAnyCommissionedModel()
        {
            if (!Directory.Exists(Root))
            {
                return false;
            }

            foreach (var pattern in new[] { "*.fbx", "*.FBX", "*.gltf", "*.glb" })
            {
                var hits = Directory.GetFiles(Root, pattern, SearchOption.AllDirectories);
                if (hits.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var leaf = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }

        private const string ReadmeBody =
@"# Commissioned art drop folder

Drop authored FBX/glTF here. OrganicMeshFactory stand-ins stay until a file lands in the matching slot.

## Naming

| Slot | Example file | Replaces |
|---|---|---|
| Hero/ | `Splat_Hero.fbx` | `OrganicSplatBuilder` / `Splat_TEMP` |
| Inventions/ | `BoingOMatic.fbx`, `VineSpooler.fbx`, … | `Prefabs/Inventions/*_Organic.prefab` |
| Enemies/ | `ScrapMite.fbx`, `ScrapStormSentinel.fbx` | `Prefabs/Enemies/*_Organic.prefab` |
| Landmarks/ | `World02_CanopyLandmark.fbx` | `OrganicLandmarkBuilders` scene roots |
| Environments/ | modular kit pieces | Arrival / world graybox slabs |
| VFX/ | optional mesh accents | TEMP spark/dust stand-ins |

## Swap steps

1. Import FBX (ModelImporter: Read/Write off unless needed; generate colliders off).
2. Create or update prefab under `Prefabs/` with `_Authored` suffix.
3. Point world bootstraps / catalog prefab refs at the authored prefab.
4. Keep organic bake menus for fallback until all 10 inventions + Arrival heroes are swapped.

See `docs/MISSING_ASSETS.md` and `docs/TEN_WORLDS_PLAN.md`.
";
    }
}
#endif
