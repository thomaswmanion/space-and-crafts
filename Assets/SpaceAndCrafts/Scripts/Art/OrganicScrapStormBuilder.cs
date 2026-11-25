using System.Collections.Generic;
using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Hulking Scrap-Storm sentinel silhouette (organic meshes, not Unity primitives).
    /// Wire into ScrapStormSentinel: body = body out, weakPoint on weakGo, dangerRing = dangerRing out.
    /// dangerRing is a telegraph root (ring + impact disc). Claw arms are named for wind-up pose.
    /// </summary>
    public static class OrganicScrapStormBuilder
    {
        public static void Build(
            Transform root,
            out Transform body,
            out GameObject weakGo,
            out Transform dangerRing,
            System.Action<GameObject, string, Color, float, float> applyMat)
        {
            body = null;
            weakGo = null;
            dangerRing = null;
            if (root == null || applyMat == null)
            {
                return;
            }

            ClearChildren(root);

            var bodyGo = Make(root, "ART_ScrapStormBody", OrganicMeshFactory.SoftBlob(4, 1.2f, 0.94f, 0.08f));
            bodyGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            bodyGo.transform.localScale = new Vector3(1.2f, 1.08f, 1.2f);
            applyMat(bodyGo, "Mat_ScrapStormBody", new Color(0.85f, 0.3f, 0.45f), 0.35f, 0.25f);
            body = bodyGo.transform;

            var belly = Make(body, "ART_ScrapStormBelly", OrganicMeshFactory.SoftBlob(3, 0.55f, 0.7f, 0.06f));
            belly.transform.localPosition = new Vector3(0f, -0.35f, 0.25f);
            belly.transform.localScale = new Vector3(1.35f, 0.85f, 1.1f);
            applyMat(belly, "Mat_ScrapStormBelly", new Color(0.7f, 0.28f, 0.4f), 0.4f, 0.2f);

            var shoulder = Make(body, "ART_ScrapStormShoulder", OrganicMeshFactory.SoftBlob(3, 0.58f, 0.42f, 0.05f));
            shoulder.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            shoulder.transform.localScale = new Vector3(1.95f, 0.55f, 0.9f);
            applyMat(shoulder, "Mat_ScrapStormShoulder", new Color(0.65f, 0.55f, 0.5f), 0.4f, 0.5f);

            var chimney = Make(body, "ART_ScrapStormChimney", OrganicMeshFactory.CrystalFacet(1.55f, 0.24f, 8));
            chimney.transform.localPosition = new Vector3(-0.45f, 1.15f, -0.25f);
            chimney.transform.localRotation = Quaternion.Euler(8f, 15f, -6f);
            applyMat(chimney, "Mat_ScrapStormChimney", new Color(0.55f, 0.58f, 0.65f), 0.25f, 0.7f);

            var stackCap = Make(chimney.transform, "ART_ScrapStormStackCap", OrganicMeshFactory.Torus(0.2f, 0.045f, 40, 14));
            stackCap.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            applyMat(stackCap, "Mat_ScrapStormStackCap", new Color(0.5f, 0.52f, 0.58f), 0.3f, 0.75f);

            var ventSmoke = Make(stackCap.transform, "ART_ScrapStormVentPuff", OrganicMeshFactory.SoftBlob(2, 0.18f, 0.85f, 0.04f));
            ventSmoke.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            applyMat(ventSmoke, "Mat_ScrapStormVent", new Color(0.75f, 0.7f, 0.72f, 0.55f), 0.15f, 0.05f);

            PlaceClaw(body, new Vector3(-1.4f, 0.15f, 0.35f), -1, applyMat);
            PlaceClaw(body, new Vector3(1.4f, 0.15f, 0.35f), 1, applyMat);

            var scrapA = Make(body, "ART_ScrapPlateA", OrganicMeshFactory.RibbonPlane(0.85f, 0.4f, 0.06f, 18, 5, 0.25f));
            scrapA.transform.localPosition = new Vector3(0.55f, 0.2f, -0.55f);
            scrapA.transform.localRotation = Quaternion.Euler(25f, -30f, 15f);
            applyMat(scrapA, "Mat_ScrapPlate", new Color(0.6f, 0.45f, 0.42f), 0.4f, 0.45f);
            var scrapB = Make(body, "ART_ScrapPlateB", OrganicMeshFactory.GearRing(0.32f, 0.055f, 10, 0.08f, 48, 12));
            scrapB.transform.localPosition = new Vector3(0.7f, -0.15f, 0.4f);
            scrapB.transform.localRotation = Quaternion.Euler(60f, 20f, 0f);
            applyMat(scrapB, "Mat_ScrapGear", new Color(0.55f, 0.58f, 0.6f), 0.35f, 0.7f);
            var scrapC = Make(body, "ART_ScrapPlateC", OrganicMeshFactory.HookCurve(0.55f, 0.4f, 0.04f, 24));
            scrapC.transform.localPosition = new Vector3(-0.65f, -0.1f, -0.35f);
            scrapC.transform.localRotation = Quaternion.Euler(15f, 40f, -25f);
            applyMat(scrapC, "Mat_ScrapHook", new Color(0.58f, 0.5f, 0.48f), 0.35f, 0.55f);

            var perch = Make(root, "ART_ScrapStormWeakPerch", OrganicMeshFactory.RibbedCylinder(0.55f, 0.12f, 6, 0.02f, 28, 8));
            perch.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            applyMat(perch, "Mat_ScrapStormPerch", new Color(0.45f, 0.42f, 0.4f), 0.3f, 0.55f);

            weakGo = Make(root, "ART_ScrapStormWeakPoint", OrganicMeshFactory.CrystalFacet(0.95f, 0.3f, 7));
            weakGo.transform.localPosition = new Vector3(0f, 3.45f, 0f);
            weakGo.transform.localRotation = Quaternion.Euler(12f, 0f, 8f);
            applyMat(weakGo, "Mat_ScrapStormWeakPoint", new Color(1f, 0.95f, 0.35f), 0.15f, 0.35f);
            var weakCol = weakGo.AddComponent<SphereCollider>();
            weakCol.isTrigger = true;
            // The Arrival root is scaled to fit the terrace, so preserve enough
            // world-space forgiveness for the crafted airborne strike.
            weakCol.radius = 1.25f;
            weakCol.center = Vector3.zero;

            // Telegraph root: expanding danger ring + ground impact disc (scaled together by sentinel).
            var telegraph = new GameObject("ART_ScrapStormTelegraph");
            telegraph.transform.SetParent(root, false);
            telegraph.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            telegraph.SetActive(false);
            dangerRing = telegraph.transform;

            var ring = Make(dangerRing, "ART_ScrapStormDangerRing", OrganicMeshFactory.Torus(0.5f, 0.05f, 64, 16));
            ring.transform.localPosition = Vector3.zero;
            applyMat(ring, "Mat_ScrapStormDangerRing", new Color(1f, 0.25f, 0.2f, 0.7f), 0.2f, 0.05f);

            var impact = Make(dangerRing, "ART_ScrapStormImpactDisc", OrganicMeshFactory.SoftBlob(3, 0.48f, 0.12f, 0.02f));
            impact.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            impact.transform.localScale = new Vector3(1f, 0.35f, 1f);
            applyMat(impact, "Mat_ScrapStormImpactDisc", new Color(1f, 0.45f, 0.15f, 0.45f), 0.25f, 0.1f);

            var tellCracks = Make(dangerRing, "ART_ScrapStormCrackTell", OrganicMeshFactory.GearRing(0.42f, 0.03f, 6, 0.12f, 36, 8));
            tellCracks.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            tellCracks.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            applyMat(tellCracks, "Mat_ScrapStormCrackTell", new Color(1f, 0.55f, 0.2f, 0.55f), 0.2f, 0.15f);
        }

        private static void PlaceClaw(Transform body, Vector3 localPos, int side, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var spring = Make(body, side < 0 ? "ART_ScrapStormClawArmL" : "ART_ScrapStormClawArmR",
                OrganicMeshFactory.SpringHelix(1.05f, 0.18f, 0.045f, 5, 28));
            spring.transform.localPosition = localPos;
            spring.transform.localRotation = Quaternion.Euler(25f, side * 15f, side * -40f);
            applyMat(spring, "Mat_ScrapStormClawArm", new Color(0.7f, 0.35f, 0.4f), 0.35f, 0.4f);

            var path = new List<Vector3>();
            for (var i = 0; i <= 20; i++)
            {
                var t = i / 20f;
                var ang = t * Mathf.PI * 0.9f;
                path.Add(new Vector3(side * Mathf.Sin(ang) * 0.4f, -0.15f - t * 0.4f, 0.22f + Mathf.Cos(ang) * 0.28f));
            }

            var claw = Make(spring.transform, side < 0 ? "ART_ScrapStormClawL" : "ART_ScrapStormClawR",
                OrganicMeshFactory.TubeAlongPath(path, 0.06f, 10, "Organic_ScrapClaw"));
            claw.transform.localPosition = new Vector3(0f, -0.45f, 0.1f);
            applyMat(claw, "Mat_ScrapStormClaw", new Color(0.75f, 0.35f, 0.4f), 0.35f, 0.4f);

            var tip = Make(claw.transform, "ART_ScrapStormClawTip", OrganicMeshFactory.CrystalFacet(0.4f, 0.09f, 5));
            tip.transform.localPosition = path[path.Count - 1];
            tip.transform.localRotation = Quaternion.Euler(40f, 0f, side * 20f);
            applyMat(tip, "Mat_ScrapStormClawTip", new Color(0.85f, 0.45f, 0.4f), 0.3f, 0.5f);

            // Wind-up blade: reads during telegraph rise (sentinel tilts claw arms).
            var blade = Make(spring.transform, "ART_ScrapStormWindUpBlade", OrganicMeshFactory.ClawBlade(0.55f, 0.18f, 0.06f));
            blade.transform.localPosition = new Vector3(side * 0.15f, -0.15f, 0.35f);
            blade.transform.localRotation = Quaternion.Euler(20f, side * 25f, side * -35f);
            applyMat(blade, "Mat_ScrapStormWindUp", new Color(0.95f, 0.4f, 0.35f), 0.3f, 0.45f);
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
