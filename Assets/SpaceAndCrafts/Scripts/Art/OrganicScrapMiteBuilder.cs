using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Dense ScrapMite silhouette (organic meshes). Replaceable by commissioned finals.
    /// </summary>
    public static class OrganicScrapMiteBuilder
    {
        public static void Build(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null)
            {
                return;
            }

            ClearChildren(root);

            var body = Make(root, "ART_MiteBody", OrganicMeshFactory.SoftBlob(3, 0.42f, 0.78f, 0.06f));
            body.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            applyMat(body, "Mat_MiteBody", new Color(0.75f, 0.45f, 0.55f), 0.4f, 0.2f);

            var shell = Make(root, "ART_MiteShell", OrganicMeshFactory.SoftBlob(2, 0.28f, 0.55f, 0.04f));
            shell.transform.localPosition = new Vector3(0f, 0.55f, -0.08f);
            shell.transform.localScale = new Vector3(1.15f, 0.7f, 1f);
            applyMat(shell, "Mat_MiteShell", new Color(0.55f, 0.5f, 0.58f), 0.35f, 0.55f);
            // Keep the cap attached to the body through the organic idle, brace,
            // attack, and hit poses. As a root sibling it stayed behind whenever
            // the body leaned, opening a visible gap instead of moving as one
            // character silhouette.
            shell.transform.SetParent(body.transform, true);

            for (var i = 0; i < 6; i++)
            {
                var side = i < 3 ? -1f : 1f;
                var row = i % 3;
                var z = -0.2f + row * 0.2f;
                var foreAft = row - 1f;
                var leg = Make(root, $"ART_MiteLeg_{i}", OrganicMeshFactory.MiteSupportLeg(side, foreAft));
                leg.transform.localPosition = new Vector3(side * 0.26f, 0.14f, z);
                leg.transform.localRotation = Quaternion.Euler(15f, 0f, side * 40f);
                applyMat(leg, "Mat_MiteLeg", new Color(0.45f, 0.35f, 0.4f), 0.45f, 0.25f);
            }

            PlaceEye(root, body.transform, new Vector3(-0.14f, 0.48f, 0.32f), applyMat);
            PlaceEye(root, body.transform, new Vector3(0.14f, 0.48f, 0.32f), applyMat);

            var antenna = Make(root, "ART_MiteAntenna", OrganicMeshFactory.HookCurve(0.28f, 0.2f, 0.02f, 16));
            antenna.transform.localPosition = new Vector3(0.05f, 0.62f, 0.1f);
            antenna.transform.SetParent(body.transform, true);
            applyMat(antenna, "Mat_MiteAntenna", new Color(0.85f, 0.55f, 0.35f), 0.35f, 0.3f);
        }

        private static void PlaceEye(Transform root, Transform body,
            Vector3 localPos, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var eye = Make(root, "ART_MiteEye", OrganicMeshFactory.SoftBlob(2, 0.08f, 1f, 0.01f));
            eye.transform.localPosition = localPos;
            eye.transform.SetParent(body, true);
            applyMat(eye, "Mat_MiteEye", Color.white, 0.2f, 0.05f);
            var pupil = Make(eye.transform, "ART_MitePupil", OrganicMeshFactory.SoftBlob(1, 0.035f, 1f, 0f));
            pupil.transform.localPosition = new Vector3(0f, 0f, 0.06f);
            applyMat(pupil, "Mat_MitePupil", new Color(0.12f, 0.1f, 0.18f), 0.5f, 0.05f);
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
