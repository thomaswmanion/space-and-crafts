using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Organic rolling scrap silhouette with readable charge telegraph stripe.
    /// </summary>
    public static class OrganicScrapRollerBuilder
    {
        public static void Build(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var body = OrganicMeshFactory.CreateMeshObject(
                "ART_RollerBody",
                OrganicMeshFactory.SoftBlob(3, 0.55f, 0.95f, 0.04f),
                root,
                new Vector3(0f, 0.45f, 0f),
                Vector3.one,
                null);
            applyMat(body, "Mat_ScrapRoller_Body", new Color(0.55f, 0.72f, 0.35f), 0.4f, 0.25f);

            var stripe = OrganicMeshFactory.CreateMeshObject(
                "ART_RollerStripe",
                OrganicMeshFactory.Torus(0.42f, 0.07f, 20, 8),
                root,
                new Vector3(0f, 0.45f, 0f),
                new Vector3(1f, 0.4f, 1f),
                null);
            applyMat(stripe, "Mat_ScrapRoller_Stripe", new Color(1f, 0.55f, 0.2f), 0.55f, 0.3f);

            var stud = OrganicMeshFactory.CreateMeshObject(
                "ART_RollerStud",
                OrganicMeshFactory.SoftBlob(2, 0.16f, 1f, 0.02f),
                root,
                new Vector3(0.35f, 0.55f, 0.15f),
                Vector3.one,
                null);
            applyMat(stud, "Mat_ScrapRoller_Stud", new Color(0.9f, 0.9f, 0.35f), 0.5f, 0.15f);

            var col = root.gameObject.GetComponent<SphereCollider>();
            if (col == null)
            {
                col = root.gameObject.AddComponent<SphereCollider>();
            }

            col.isTrigger = true;
            col.radius = 0.55f;
            col.center = new Vector3(0f, 0.45f, 0f);

            if (root.gameObject.GetComponent<SpaceAndCrafts.AI.ScrapRoller>() == null)
            {
                root.gameObject.AddComponent<SpaceAndCrafts.AI.ScrapRoller>();
            }
        }
    }
}
