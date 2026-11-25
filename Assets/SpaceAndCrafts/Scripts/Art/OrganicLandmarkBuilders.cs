using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// World-specific organic landmark compositions (high-poly silhouettes, not stacked primitives).
    /// Parent should be a fresh ART_WorldLandmark root; children are the composition.
    /// </summary>
    public static class OrganicLandmarkBuilders
    {
        public static void BuildForWorld(int worldIndex, Transform root, Color accent, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null)
            {
                return;
            }

            ClearChildren(root);
            switch (worldIndex)
            {
                case 2: BuildResonantCave(root, accent, applyMat); break;
                case 3: BuildMagnetFoilArches(root, accent, applyMat); break;
                case 4: BuildBubbleClusters(root, accent, applyMat); break;
                case 5: BuildGearRings(root, accent, applyMat); break;
                case 6: BuildIceFilaments(root, accent, applyMat); break;
                case 7: BuildBoomDishes(root, accent, applyMat); break;
                case 8: BuildTideHulls(root, accent, applyMat); break;
                case 9: BuildStarNurseries(root, accent, applyMat); break;
                case 10: BuildStainedGlassCathedral(root, accent, applyMat); break;
                default: break;
            }
        }

        public static string FantasyLabel(int worldIndex)
        {
            switch (worldIndex)
            {
                case 2: return "resonant cave arches and chime crystals";
                case 3: return "magnet foil arches";
                case 4: return "bubble clusters";
                case 5: return "gear rings";
                case 6: return "ice filaments";
                case 7: return "boom dishes";
                case 8: return "tide hulls";
                case 9: return "star nurseries";
                case 10: return "stained-glass cathedral arches";
                default: return "organic landmark";
            }
        }

        private static void BuildResonantCave(Transform root, Color accent, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            // Broad organic arches read as a continuous, supported cave route. The
            // prismatic growths mark resonant anchor points without making platforms.
            for (var i = 0; i < 3; i++)
            {
                var rib = Make(root, $"ART_ResonantCaveArch_{i}", OrganicMeshFactory.ArchGateway(5.8f, 4.7f, 0.48f, 32));
                rib.transform.localPosition = new Vector3(0f, 0.05f, -1f + i * 3.7f);
                rib.transform.localRotation = Quaternion.Euler(0f, (i - 1) * 4f, (i - 1) * 1.5f);
                applyMat(rib, "Mat_Landmark_ResonantBasalt", Color.Lerp(new Color(0.19f, 0.22f, 0.34f), accent, 0.18f), 0.62f, 0.16f);
            }

            for (var i = 0; i < 6; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var facet = Make(root, $"ART_ChimeCrystal_{i}", OrganicMeshFactory.CrystalFacet(1.45f + (i % 3) * 0.28f, 0.4f, 7));
                facet.transform.localPosition = new Vector3(side * (2.2f + (i % 3) * 0.25f), 0.25f + (i % 2) * 0.18f, -0.6f + i * 1.35f);
                facet.transform.localRotation = Quaternion.Euler(0f, i * 37f, side * 9f);
                facet.transform.localScale = new Vector3(0.8f, 1.2f + (i % 3) * 0.2f, 0.75f);
                applyMat(facet, $"Mat_Landmark_ChimeCrystal_{i % 3}", Color.Lerp(accent, new Color(0.34f, 0.93f, 1f), (i % 3) * 0.16f), 0.18f, 0.22f);
            }
        }

        private static void BuildMagnetFoilArches(Transform root, Color accent, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            for (var i = 0; i < 3; i++)
            {
                var arch = Make(root, $"ART_FoilArch_{i}", OrganicMeshFactory.ArchGateway(2.2f + i * 0.4f, 2.8f + i * 0.5f, 0.28f, 32));
                arch.transform.localPosition = new Vector3((i - 1) * 2.2f, 0.1f, i * 1.2f);
                arch.transform.localRotation = Quaternion.Euler(0f, i * 18f - 18f, 0f);
                applyMat(arch, "Mat_Landmark_FoilArch", Color.Lerp(accent, new Color(0.7f, 0.85f, 1f), 0.35f), 0.25f, 0.65f);
            }

            var scrap = Make(root, "ART_ScrapTide", OrganicMeshFactory.Torus(1.6f, 0.12f, 48, 16));
            scrap.transform.localPosition = new Vector3(0f, 0.35f, 1f);
            applyMat(scrap, "Mat_Landmark_ScrapTide", Color.Lerp(accent, new Color(0.5f, 0.55f, 0.65f), 0.4f), 0.3f, 0.7f);
        }

        private static void BuildBubbleClusters(Transform root, Color accent, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var offsets = new[]
            {
                new Vector3(0f, 1.2f, 0f),
                new Vector3(-1.4f, 0.8f, 0.6f),
                new Vector3(1.5f, 1.5f, -0.4f),
                new Vector3(-0.6f, 2.2f, 0.8f),
                new Vector3(0.9f, 2.6f, 0.3f),
                new Vector3(0.2f, 0.5f, 1.2f)
            };
            for (var i = 0; i < offsets.Length; i++)
            {
                var r = 0.45f + (i % 3) * 0.18f;
                var bubble = Make(root, $"ART_Bubble_{i}", OrganicMeshFactory.SoftBlob(3, r, 0.95f, 0.05f));
                bubble.transform.localPosition = offsets[i];
                var tint = Color.Lerp(accent, new Color(0.6f, 0.9f, 1f), (i % 4) * 0.15f);
                applyMat(bubble, $"Mat_Landmark_Bubble_{i % 3}", tint, 0.12f, 0.05f);
            }

            var raft = Make(root, "ART_SoapRoad", OrganicMeshFactory.Torus(1.8f, 0.08f, 40, 12));
            raft.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            applyMat(raft, "Mat_Landmark_SoapRoad", Color.Lerp(accent, Color.white, 0.4f), 0.2f, 0.1f);
        }

        private static void BuildGearRings(Transform root, Color accent, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var sizes = new[] { 1.8f, 1.2f, 0.75f };
            for (var i = 0; i < sizes.Length; i++)
            {
                var gear = Make(root, $"ART_GearRing_{i}", OrganicMeshFactory.GearRing(sizes[i], 0.1f + i * 0.02f, 10 + i * 2, 0.12f, 56, 14));
                gear.transform.localPosition = new Vector3(0f, 0.8f + i * 0.9f, 0f);
                gear.transform.localRotation = Quaternion.Euler(15f + i * 25f, i * 40f, 10f);
                applyMat(gear, "Mat_Landmark_GearRing", Color.Lerp(accent, new Color(0.7f, 0.55f, 0.3f), 0.3f), 0.3f, 0.55f);
            }

            for (var i = 0; i < 4; i++)
            {
                var peg = Make(root, $"ART_CraterPeg_{i}", OrganicMeshFactory.CrystalFacet(0.9f, 0.18f, 6));
                var ang = (i / 4f) * Mathf.PI * 2f;
                peg.transform.localPosition = new Vector3(Mathf.Cos(ang) * 1.6f, 0.5f, Mathf.Sin(ang) * 1.6f);
                applyMat(peg, "Mat_Landmark_CraterPeg", Color.Lerp(accent, new Color(0.5f, 0.4f, 0.25f), 0.4f), 0.35f, 0.3f);
            }
        }

        private static void BuildIceFilaments(Transform root, Color accent, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            for (var i = 0; i < 6; i++)
            {
                var filament = Make(root, $"ART_IceFilament_{i}", OrganicMeshFactory.SpringHelix(3.2f + i * 0.3f, 0.15f + (i % 3) * 0.05f, 0.035f, 4 + i % 3, 24));
                filament.transform.localPosition = new Vector3(-2.5f + i * 1f, 1.5f, (i % 2) * 0.8f);
                filament.transform.localRotation = Quaternion.Euler(8f, i * 12f, (i - 2.5f) * 8f);
                applyMat(filament, "Mat_Landmark_IceFilament", Color.Lerp(accent, Color.white, 0.35f), 0.15f, 0.2f);
            }

            var loom = Make(root, "ART_FrostLoom", OrganicMeshFactory.ArchGateway(3.2f, 2.6f, 0.2f, 28));
            loom.transform.localPosition = new Vector3(0f, 0.1f, 1.5f);
            applyMat(loom, "Mat_Landmark_FrostLoom", Color.Lerp(accent, new Color(0.75f, 0.9f, 1f), 0.25f), 0.2f, 0.35f);
        }

        private static void BuildBoomDishes(Transform root, Color accent, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var poses = new[]
            {
                (new Vector3(-1.8f, 1.2f, 0f), new Vector3(-25f, 30f, 0f), 0.9f),
                (new Vector3(1.6f, 1.8f, 0.8f), new Vector3(-40f, -20f, 10f), 0.7f),
                (new Vector3(0.2f, 2.6f, -0.6f), new Vector3(-15f, 0f, 0f), 1.1f)
            };
            for (var i = 0; i < poses.Length; i++)
            {
                var dish = Make(root, $"ART_BoomDish_{i}", OrganicMeshFactory.DishBowl(poses[i].Item3, 0.28f, 12, 40));
                dish.transform.localPosition = poses[i].Item1;
                dish.transform.localRotation = Quaternion.Euler(poses[i].Item2);
                applyMat(dish, "Mat_Landmark_BoomDish", Color.Lerp(accent, new Color(0.7f, 0.5f, 0.95f), 0.3f), 0.2f, 0.35f);

                var stand = Make(root, $"ART_DishStand_{i}", OrganicMeshFactory.SpringHelix(poses[i].Item1.y, 0.12f, 0.04f, 3, 16));
                stand.transform.localPosition = new Vector3(poses[i].Item1.x, poses[i].Item1.y * 0.45f, poses[i].Item1.z);
                applyMat(stand, "Mat_Landmark_DishStand", Color.Lerp(accent, new Color(0.45f, 0.35f, 0.6f), 0.4f), 0.35f, 0.4f);
            }
        }

        private static void BuildTideHulls(Transform root, Color accent, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            for (var i = 0; i < 3; i++)
            {
                var hull = Make(root, $"ART_TideHull_{i}", OrganicMeshFactory.SoftBlob(3, 0.7f, 0.4f, 0.05f));
                hull.transform.localPosition = new Vector3(-2f + i * 2f, 0.55f + (i % 2) * 0.25f, i * 0.6f);
                hull.transform.localScale = new Vector3(2.2f - i * 0.3f, 0.55f, 1.1f);
                hull.transform.localRotation = Quaternion.Euler(0f, -20f + i * 25f, 8f * (i - 1));
                applyMat(hull, "Mat_Landmark_TideHull", Color.Lerp(accent, new Color(0.25f, 0.45f, 0.5f), 0.35f), 0.35f, 0.3f);
            }

            var mast = Make(root, "ART_WreckMast", OrganicMeshFactory.SpringHelix(3.5f, 0.1f, 0.04f, 2, 18));
            mast.transform.localPosition = new Vector3(0f, 1.8f, 0.5f);
            applyMat(mast, "Mat_Landmark_WreckMast", Color.Lerp(accent, new Color(0.55f, 0.45f, 0.35f), 0.4f), 0.4f, 0.2f);

            var buoy = Make(root, "ART_TideBuoy", OrganicMeshFactory.Torus(1.4f, 0.1f, 36, 14));
            buoy.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            applyMat(buoy, "Mat_Landmark_TideBuoy", Color.Lerp(accent, Color.white, 0.25f), 0.25f, 0.25f);
        }

        private static void BuildStarNurseries(Transform root, Color accent, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            for (var i = 0; i < 7; i++)
            {
                var seed = Make(root, $"ART_NurserySeed_{i}", OrganicMeshFactory.SoftBlob(3, 0.2f + (i % 3) * 0.08f, 1f, 0.04f));
                var ang = (i / 7f) * Mathf.PI * 2f;
                var rad = 0.8f + (i % 3) * 0.45f;
                seed.transform.localPosition = new Vector3(Mathf.Cos(ang) * rad, 1f + (i % 4) * 0.45f, Mathf.Sin(ang) * rad);
                applyMat(seed, "Mat_Landmark_NurserySeed", Color.Lerp(accent, new Color(0.9f, 0.85f, 1f), 0.4f), 0.15f, 0.05f);
            }

            var cradle = Make(root, "ART_StarCradle", OrganicMeshFactory.DishBowl(1.6f, 0.35f, 12, 40));
            cradle.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            applyMat(cradle, "Mat_Landmark_StarCradle", Color.Lerp(accent, new Color(0.4f, 0.35f, 0.75f), 0.3f), 0.25f, 0.2f);

            var lamp = Make(root, "ART_NurseryLamp", OrganicMeshFactory.CrystalFacet(1.2f, 0.25f, 8));
            lamp.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            applyMat(lamp, "Mat_Landmark_NurseryLamp", Color.Lerp(accent, new Color(0.7f, 0.95f, 1f), 0.35f), 0.12f, 0.08f);
        }

        private static void BuildStainedGlassCathedral(Transform root, Color accent, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            for (var i = 0; i < 3; i++)
            {
                var arch = Make(root, $"ART_CathedralArch_{i}", OrganicMeshFactory.ArchGateway(2.4f + i * 0.35f, 3.2f + i * 0.4f, 0.22f, 30));
                arch.transform.localPosition = new Vector3(0f, 0.1f, i * 1.1f);
                applyMat(arch, "Mat_Landmark_CathedralArch", Color.Lerp(accent, new Color(0.85f, 0.75f, 0.4f), 0.35f), 0.3f, 0.55f);
            }

            var paneColors = new[]
            {
                new Color(1f, 0.35f, 0.4f),
                new Color(0.35f, 0.7f, 1f),
                new Color(0.45f, 0.95f, 0.5f),
                new Color(0.95f, 0.8f, 0.3f),
                new Color(0.85f, 0.45f, 0.95f)
            };
            for (var i = 0; i < paneColors.Length; i++)
            {
                var pane = Make(root, $"ART_GlassPane_{i}", OrganicMeshFactory.SoftBlob(3, 0.35f, 0.4f, 0.02f));
                var t = i / (float)(paneColors.Length - 1);
                pane.transform.localPosition = new Vector3(Mathf.Lerp(-1.2f, 1.2f, t), 1.6f + Mathf.Sin(t * Mathf.PI) * 0.8f, 0.4f);
                pane.transform.localScale = new Vector3(0.8f, 1.5f, 0.2f);
                applyMat(pane, $"Mat_Landmark_GlassPane_{i}", paneColors[i], 0.12f, 0.05f);
            }

            var hub = Make(root, "ART_RainbowHub", OrganicMeshFactory.CrystalFacet(1.4f, 0.3f, 8));
            hub.transform.localPosition = new Vector3(0f, 2.2f, 1.5f);
            applyMat(hub, "Mat_Landmark_RainbowHub", Color.Lerp(accent, Color.white, 0.3f), 0.15f, 0.1f);
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
