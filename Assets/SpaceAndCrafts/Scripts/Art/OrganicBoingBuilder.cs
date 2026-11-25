using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// High-density Boing-O-Matic silhouette using organic meshes (not Unity primitives).
    /// Still replaceable by commissioned finals.
    /// </summary>
    public static class OrganicBoingBuilder
    {
        public static void Build(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null)
            {
                return;
            }

            ClearChildren(root);

            var dish = Make(root, "ART_BoingDish", OrganicMeshFactory.DishBowl(0.78f, 0.28f, 16, 48));
            dish.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            dish.transform.localScale = Vector3.one;
            applyMat(dish, "Mat_BoingDish", new Color(0.82f, 0.88f, 0.96f), 0.18f, 0.75f);

            var rim = Make(root, "ART_BoingRim", OrganicMeshFactory.Torus(0.72f, 0.05f, 56, 20));
            rim.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            applyMat(rim, "Mat_BoingRim", new Color(0.55f, 0.62f, 0.74f), 0.2f, 0.6f);

            var spring = Make(root, "ART_BoingSpring", OrganicMeshFactory.SpringHelix(0.75f, 0.3f, 0.04f, 7, 32));
            spring.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            applyMat(spring, "Mat_BoingSpring", new Color(1f, 0.78f, 0.22f), 0.35f, 0.25f);

            var crystal = Make(root, "ART_BoingCrystal", OrganicMeshFactory.CrystalFacet(0.7f, 0.2f, 8));
            crystal.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            crystal.transform.localRotation = Quaternion.Euler(8f, 25f, 12f);
            applyMat(crystal, "Mat_BoingCrystal", new Color(0.95f, 0.4f, 0.9f), 0.15f, 0.05f);

            var sparkle = Make(root, "ART_BoingSparkle", OrganicMeshFactory.FacetedStar(0.12f, 0.05f, 0.04f, 5));
            sparkle.transform.localPosition = new Vector3(0.15f, 0.85f, 0.1f);
            applyMat(sparkle, "Mat_BoingSparkle", new Color(1f, 0.95f, 0.55f), 0.1f, 0.2f);

            PlaceFoot(root, new Vector3(-0.42f, -0.62f, 0.32f), applyMat);
            PlaceFoot(root, new Vector3(0.42f, -0.62f, 0.32f), applyMat);
            PlaceFoot(root, new Vector3(0f, -0.62f, -0.4f), applyMat);
        }

        private static void PlaceFoot(Transform root, Vector3 localPos, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var foot = Make(root, "ART_BoingFoot", OrganicMeshFactory.SoftBlob(3, 0.14f, 0.55f, 0.04f));
            foot.transform.localPosition = localPos;
            applyMat(foot, "Mat_BoingFoot", new Color(0.45f, 0.5f, 0.6f), 0.4f, 0.35f);
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

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Object.Destroy(child);
                }
                else
                {
                    Object.DestroyImmediate(child);
                }
            }
        }
    }
}
