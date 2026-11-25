using SpaceAndCrafts.Art;
using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Organic hovering scrap silhouette for ScrapFloater.
    /// </summary>
    public static class OrganicScrapFloaterBuilder
    {
        public static void Build(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var body = OrganicMeshFactory.CreateMeshObject(
                "ART_FloaterBody",
                OrganicMeshFactory.SoftBlob(3, 0.55f, 0.88f, 0.05f),
                root,
                new Vector3(0f, 0.2f, 0f),
                Vector3.one,
                null);
            applyMat(body, "Mat_ScrapFloater_Body", new Color(0.45f, 0.85f, 0.95f), 0.45f, 0.2f);

            var ring = OrganicMeshFactory.CreateMeshObject(
                "ART_FloaterRing",
                OrganicMeshFactory.Torus(0.55f, 0.08f, 24, 10),
                root,
                new Vector3(0f, 0.25f, 0f),
                new Vector3(1f, 0.35f, 1f),
                null);
            applyMat(ring, "Mat_ScrapFloater_Ring", new Color(0.95f, 0.7f, 0.35f), 0.55f, 0.35f);

            var eye = OrganicMeshFactory.CreateMeshObject(
                "ART_FloaterEye",
                OrganicMeshFactory.SoftBlob(2, 0.18f, 1f, 0.02f),
                root,
                new Vector3(0f, 0.35f, 0.35f),
                Vector3.one,
                null);
            applyMat(eye, "Mat_ScrapFloater_Eye", new Color(1f, 0.35f, 0.45f), 0.6f, 0.1f);

            var col = root.gameObject.GetComponent<SphereCollider>();
            if (col == null)
            {
                col = root.gameObject.AddComponent<SphereCollider>();
            }

            col.isTrigger = true;
            col.radius = 0.55f;
            col.center = new Vector3(0f, 0.25f, 0f);

            if (root.GetComponent<Rigidbody>() == null)
            {
                var rb = root.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (root.GetComponent<SpaceAndCrafts.AI.ScrapFloater>() == null)
            {
                root.gameObject.AddComponent<SpaceAndCrafts.AI.ScrapFloater>();
            }
        }
    }
}
