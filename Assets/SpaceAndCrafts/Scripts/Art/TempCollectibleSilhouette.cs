using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Per-type pickup detail so collectibles read apart at a glance. Organic meshes, replaceable by finals.
    /// </summary>
    public static class TempCollectibleSilhouette
    {
        public static void Decorate(
            Transform root,
            string collectibleId,
            Color baseColor,
            System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null || string.IsNullOrEmpty(collectibleId))
            {
                return;
            }

            switch (collectibleId)
            {
                case "material_rainbow_goo":
                    BuildGoo(root, baseColor, applyMat);
                    break;
                case "component_spring":
                    BuildSpring(root, baseColor, applyMat);
                    break;
                case "component_metal_dish":
                    BuildDish(root, baseColor, applyMat);
                    break;
                case "component_power_crystal":
                    BuildCrystal(root, baseColor, applyMat);
                    break;
                case "discovery_badge_secret_nook":
                    BuildBadge(root, baseColor, applyMat);
                    break;
            }
        }

        private static void BuildGoo(
            Transform root,
            Color color,
            System.Action<GameObject, string, Color, float, float> applyMat)
        {
            AddMesh(root, "TEMP_ART_GooBlobA", OrganicMeshFactory.SoftBlob(2, 0.28f, 0.7f, 0.05f),
                new Vector3(0.2f, -0.1f, 0.08f), new Vector3(1.1f, 0.85f, 1f), Quaternion.identity,
                "TEMP_Mat_GooBlob", color * 1.05f, applyMat);
            AddMesh(root, "TEMP_ART_GooBlobB", OrganicMeshFactory.SoftBlob(2, 0.22f, 0.65f, 0.04f),
                new Vector3(-0.18f, -0.15f, -0.1f), Vector3.one, Quaternion.identity,
                "TEMP_Mat_GooBlob", color * 0.9f, applyMat);
            AddMesh(root, "TEMP_ART_GooShine", OrganicMeshFactory.Icosphere(2, 0.1f),
                new Vector3(0.08f, 0.18f, 0.16f), Vector3.one, Quaternion.identity,
                "TEMP_Mat_GooShine", Color.white, applyMat, 0.1f, 0.05f);
        }

        private static void BuildSpring(
            Transform root,
            Color color,
            System.Action<GameObject, string, Color, float, float> applyMat)
        {
            AddMesh(root, "TEMP_ART_SpringCoil", OrganicMeshFactory.SpringHelix(0.86f, 0.43f, 0.07f, 4, 28),
                Vector3.zero, Vector3.one, Quaternion.identity,
                "TEMP_Mat_SpringRing", color, applyMat, 0.45f, 0.25f);
        }

        private static void BuildDish(
            Transform root,
            Color color,
            System.Action<GameObject, string, Color, float, float> applyMat)
        {
            AddMesh(root, "TEMP_ART_DishBowl", OrganicMeshFactory.DishBowl(0.58f, 0.32f, 12, 40),
                new Vector3(0f, -0.05f, 0f), Vector3.one, Quaternion.identity,
                "TEMP_Mat_DishBowl", color, applyMat, 0.15f, 0.8f);
            AddMesh(root, "TEMP_ART_DishRim", OrganicMeshFactory.Torus(0.58f, 0.06f, 40, 12),
                new Vector3(0f, 0.12f, 0f), Vector3.one, Quaternion.identity,
                "TEMP_Mat_DishRim", color * 0.85f, applyMat, 0.2f, 0.75f);
            AddMesh(root, "TEMP_ART_DishHandle", OrganicMeshFactory.HookCurve(0.42f, 0.24f, 0.05f, 20),
                new Vector3(0.45f, 0.08f, 0f), Vector3.one, Quaternion.Euler(0f, 90f, 15f),
                "TEMP_Mat_DishHandle", color * 0.7f, applyMat, 0.25f, 0.65f);
        }

        private static void BuildCrystal(
            Transform root,
            Color color,
            System.Action<GameObject, string, Color, float, float> applyMat)
        {
            AddMesh(root, "TEMP_ART_CrystalShardA", OrganicMeshFactory.CrystalFacet(0.75f, 0.18f, 6),
                new Vector3(0.1f, 0.04f, 0.05f), Vector3.one, Quaternion.Euler(10f, 18f, -14f),
                "TEMP_Mat_CrystalShard", color * 1.08f, applyMat, 0.2f, 0.55f);
            AddMesh(root, "TEMP_ART_CrystalShardB", OrganicMeshFactory.CrystalFacet(0.55f, 0.12f, 5),
                new Vector3(-0.18f, -0.06f, -0.08f), Vector3.one, Quaternion.Euler(-18f, -28f, 25f),
                "TEMP_Mat_CrystalShard", color * 0.78f, applyMat, 0.2f, 0.55f);
            AddMesh(root, "TEMP_ART_CrystalGlow", OrganicMeshFactory.SoftBlob(2, 0.14f, 1f, 0.02f),
                new Vector3(0f, 0.35f, 0f), Vector3.one, Quaternion.identity,
                "TEMP_Mat_CrystalGlow", new Color(0.42f, 0.96f, 1f), applyMat, 0.1f, 0.65f);
        }

        private static void BuildBadge(
            Transform root,
            Color color,
            System.Action<GameObject, string, Color, float, float> applyMat)
        {
            AddMesh(root, "TEMP_ART_BadgeDisc", OrganicMeshFactory.DishBowl(0.48f, 0.08f, 8, 32),
                Vector3.zero, new Vector3(1f, 0.35f, 1f), Quaternion.identity,
                "TEMP_Mat_BadgeDisc", color, applyMat, 0.35f, 0.4f);
            AddMesh(root, "TEMP_ART_BadgeRing", OrganicMeshFactory.Torus(0.5f, 0.035f, 36, 10),
                new Vector3(0f, 0.05f, 0f), Vector3.one, Quaternion.identity,
                "TEMP_Mat_BadgeRing", new Color(0.86f, 0.49f, 0.14f), applyMat, 0.2f, 0.7f);
            AddMesh(root, "TEMP_ART_BadgeStar", OrganicMeshFactory.FacetedStar(0.28f, 0.11f, 0.08f, 5),
                new Vector3(0f, 0.08f, 0f), Vector3.one, Quaternion.identity,
                "TEMP_Mat_BadgeStar", new Color(0.9f, 0.34f, 0.06f), applyMat, 0.15f, 0.2f);
        }

        private static void AddMesh(
            Transform root,
            string name,
            Mesh mesh,
            Vector3 localPos,
            Vector3 localScale,
            Quaternion localRot,
            string matName,
            Color color,
            System.Action<GameObject, string, Color, float, float> applyMat,
            float smoothness = 0.35f,
            float metallic = 0.25f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            applyMat(go, matName, color, smoothness, metallic);
        }
    }
}
