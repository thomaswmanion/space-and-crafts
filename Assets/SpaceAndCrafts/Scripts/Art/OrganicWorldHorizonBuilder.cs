using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Distant horizon ring + silhouette mounds so each world reads beyond fog tint alone.
    /// Procedural organic meshes — swap for commissioned sky/horizon later.
    /// </summary>
    public static class OrganicWorldHorizonBuilder
    {
        public const string RootName = "ART_WorldHorizon";

        public static void Build(
            Transform parent,
            int worldIndex,
            Color fogTint,
            System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (parent == null || applyMat == null)
            {
                return;
            }

            ClearNamedChild(parent, RootName);
            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;

            var skyTint = Color.Lerp(fogTint, Color.white, 0.35f);
            skyTint.a = 1f;
            var groundTint = Color.Lerp(fogTint, new Color(0.2f, 0.18f, 0.22f), 0.55f);

            var bowl = Make(root.transform, "ART_HorizonBowl", OrganicMeshFactory.DishBowl(48f, 6f, 14, 48));
            bowl.transform.localPosition = new Vector3(0f, -2.5f, 0f);
            bowl.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            bowl.transform.localScale = new Vector3(1f, 0.55f, 1f);
            applyMat(bowl, $"Mat_HorizonBowl_W{worldIndex:00}", groundTint, 0.08f, 0.05f);

            var ring = Make(root.transform, "ART_HorizonRing", OrganicMeshFactory.Torus(42f, 1.6f, 72, 12));
            ring.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            applyMat(ring, $"Mat_HorizonRing_W{worldIndex:00}", skyTint, 0.12f, 0.05f);

            PlaceSilhouette(root.transform, worldIndex, 0, new Vector3(0f, 0.5f, 38f), fogTint, applyMat);
            PlaceSilhouette(root.transform, worldIndex, 1, new Vector3(-28f, 0.4f, 22f), fogTint, applyMat);
            PlaceSilhouette(root.transform, worldIndex, 2, new Vector3(26f, 0.35f, 18f), fogTint, applyMat);
            PlaceSilhouette(root.transform, worldIndex, 3, new Vector3(12f, 0.6f, -34f), fogTint, applyMat);
        }

        private static void PlaceSilhouette(
            Transform root,
            int worldIndex,
            int slot,
            Vector3 localPos,
            Color fogTint,
            System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var tint = Color.Lerp(fogTint, new Color(0.15f, 0.14f, 0.18f), 0.65f);
            Mesh mesh;
            Vector3 scale;
            switch ((worldIndex + slot) % 5)
            {
                case 0:
                    mesh = OrganicMeshFactory.SoftBlob(3, 2.2f, 1.4f, 0.12f);
                    scale = new Vector3(4.5f, 3.2f, 3.5f);
                    break;
                case 1:
                    mesh = OrganicMeshFactory.SpiralShell(2.2f, 0.4f, 2.4f, 0.35f, 2.8f, 48);
                    scale = new Vector3(2.8f, 2.8f, 2.8f);
                    break;
                case 2:
                    mesh = OrganicMeshFactory.CrystalFacet(4.5f, 1.4f, 6);
                    scale = new Vector3(2.2f, 2.6f, 2.2f);
                    break;
                case 3:
                    mesh = OrganicMeshFactory.ArchGateway(5.5f, 6.5f, 0.9f, 20);
                    scale = Vector3.one * 1.4f;
                    break;
                default:
                    mesh = OrganicMeshFactory.RibbedCylinder(5f, 1.6f, 10, 0.12f, 36, 10);
                    scale = new Vector3(2.4f, 2.2f, 2.4f);
                    break;
            }

            var go = Make(root, $"ART_HorizonSilhouette_{slot}", mesh);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, slot * 55f + worldIndex * 17f, 0f);
            go.transform.localScale = scale;
            applyMat(go, $"Mat_HorizonSilhouette_W{worldIndex:00}_{slot}", tint, 0.1f, 0.08f);
        }

        private static GameObject Make(Transform parent, string name, Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            return go;
        }

        private static void ClearNamedChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing == null)
            {
                var sceneRoot = GameObject.Find(name);
                if (sceneRoot != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(sceneRoot);
                    }
                    else
                    {
                        Object.DestroyImmediate(sceneRoot);
                    }
                }

                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(existing.gameObject);
            }
            else
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }
    }
}
