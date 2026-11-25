using System.Collections.Generic;
using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Distinct organic silhouettes for world inventions 2–10 (not Unity primitives).
    /// </summary>
    public static class OrganicInventionBuilders
    {
        public static void Build(string inventionId, Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null || string.IsNullOrEmpty(inventionId))
            {
                return;
            }

            switch (inventionId)
            {
                case "vine_spooler":
                    BuildPinglineSpooler(root, applyMat);
                    break;
                case "junk_magnet_gauntlet":
                    BuildJunkMagnetGauntlet(root, applyMat);
                    break;
                case "paint_bubble_raft":
                    BuildPaintBubbleRaft(root, applyMat);
                    break;
                case "windup_bridge_bug":
                    BuildWindUpBridgeBug(root, applyMat);
                    break;
                case "heat_tape_torch":
                    BuildHeatTapeTorch(root, applyMat);
                    break;
                case "boom_dish_redirector":
                    BuildBoomDishRedirector(root, applyMat);
                    break;
                case "vacuum_skimmer":
                    BuildVacuumSkimmer(root, applyMat);
                    break;
                case "star_seed_planter":
                    BuildStarSeedPlanter(root, applyMat);
                    break;
                case "glass_sail_shield":
                    BuildGlassSailShield(root, applyMat);
                    break;
                case "boing_o_matic":
                    OrganicBoingBuilder.Build(root, applyMat);
                    break;
                default:
                    OrganicBoingBuilder.Build(root, applyMat);
                    break;
            }
        }

        public static void BuildForWorld(int worldIndex, Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            switch (worldIndex)
            {
                case 2: BuildPinglineSpooler(root, applyMat); break;
                case 3: BuildJunkMagnetGauntlet(root, applyMat); break;
                case 4: BuildPaintBubbleRaft(root, applyMat); break;
                case 5: BuildWindUpBridgeBug(root, applyMat); break;
                case 6: BuildHeatTapeTorch(root, applyMat); break;
                case 7: BuildBoomDishRedirector(root, applyMat); break;
                case 8: BuildVacuumSkimmer(root, applyMat); break;
                case 9: BuildStarSeedPlanter(root, applyMat); break;
                case 10: BuildGlassSailShield(root, applyMat); break;
                default: OrganicBoingBuilder.Build(root, applyMat); break;
            }
        }

        public static void BuildJunkMagnet(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            BuildJunkMagnetGauntlet(root, applyMat);
        }

        /// <summary>Legacy entry point retained for serialized editor tools.</summary>
        public static void BuildVineSpooler(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            BuildPinglineSpooler(root, applyMat);
        }

        /// <summary>Compact tuning-fork reel + resonator crystal for Beeping Caves Kingdom.</summary>
        public static void BuildPinglineSpooler(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null)
            {
                return;
            }

            ClearChildren(root);

            var drum = Make(root, "ART_PinglineDrum", OrganicMeshFactory.RibbedCylinder(0.55f, 0.38f, 10, 0.05f, 44, 12));
            drum.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            drum.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            applyMat(drum, "Mat_PinglineDrum", new Color(0.20f, 0.24f, 0.37f), 0.58f, 0.24f);

            var hubL = Make(root, "ART_PinglineHubL", OrganicMeshFactory.SoftBlob(2, 0.16f, 0.7f, 0.03f));
            hubL.transform.localPosition = new Vector3(-0.42f, 0.35f, 0f);
            applyMat(hubL, "Mat_PinglineHub", new Color(0.72f, 0.47f, 0.82f), 0.42f, 0.18f);
            var hubR = Make(root, "ART_PinglineHubR", OrganicMeshFactory.SoftBlob(2, 0.16f, 0.7f, 0.03f));
            hubR.transform.localPosition = new Vector3(0.42f, 0.35f, 0f);
            applyMat(hubR, "Mat_PinglineHub", new Color(0.72f, 0.47f, 0.82f), 0.42f, 0.18f);

            var coil = Make(root, "ART_PinglineCoil", OrganicMeshFactory.SpringHelix(0.85f, 0.42f, 0.035f, 7, 26));
            coil.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            coil.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            applyMat(coil, "Mat_PinglineCoil", new Color(0.39f, 0.73f, 0.92f), 0.32f, 0.18f);

            var crank = Make(root, "ART_PinglineCrank", OrganicMeshFactory.HookCurve(0.45f, 0.35f, 0.04f, 24));
            crank.transform.localPosition = new Vector3(0.55f, 0.35f, 0f);
            crank.transform.localRotation = Quaternion.Euler(0f, 90f, 20f);
            applyMat(crank, "Mat_PinglineCrank", new Color(0.94f, 0.61f, 0.31f), 0.36f, 0.4f);

            var fork = Make(root, "ART_PinglineTuningFork", OrganicMeshFactory.ArchGateway(0.8f, 0.78f, 0.1f, 18));
            fork.transform.localPosition = new Vector3(0f, 0.78f, 0.21f);
            fork.transform.localScale = new Vector3(0.42f, 0.72f, 0.32f);
            applyMat(fork, "Mat_PinglineTuningFork", new Color(0.86f, 0.71f, 0.96f), 0.22f, 0.25f);

            var pingCrystal = Make(root, "ART_PinglineCrystal", OrganicMeshFactory.CrystalFacet(0.31f, 0.12f, 7));
            pingCrystal.transform.localPosition = new Vector3(0f, 0.77f, 0.35f);
            pingCrystal.transform.localScale = new Vector3(0.58f, 0.78f, 0.45f);
            applyMat(pingCrystal, "Mat_PinglineCrystal", new Color(0.38f, 0.95f, 1f), 0.16f, 0.14f);

            var stand = Make(root, "ART_VineStand", OrganicMeshFactory.LathedProfile(new[]
            {
                new Vector2(0.22f, 0f), new Vector2(0.18f, 0.08f), new Vector2(0.08f, 0.2f), new Vector2(0.06f, 0.28f)
            }, 28, "Organic_PinglineHandle"));
            stand.transform.localPosition = new Vector3(0f, 0f, 0f);
            applyMat(stand, "Mat_PinglineHandle", new Color(0.44f, 0.30f, 0.55f), 0.45f, 0.2f);
        }

        /// <summary>Gauntlet cuff + magnet dome + claw hooks — marsh salvage silhouette.</summary>
        public static void BuildJunkMagnetGauntlet(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null)
            {
                return;
            }

            ClearChildren(root);

            var cuff = Make(root, "ART_MagnetCuff", OrganicMeshFactory.RibbedCylinder(0.5f, 0.32f, 6, 0.035f, 36, 10));
            cuff.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            applyMat(cuff, "Mat_MagnetCuff", new Color(0.55f, 0.58f, 0.62f), 0.35f, 0.7f);

            var palm = Make(root, "ART_MagnetPalm", OrganicMeshFactory.SoftBlob(3, 0.38f, 0.65f, 0.04f));
            palm.transform.localPosition = new Vector3(0f, 0.55f, 0.1f);
            applyMat(palm, "Mat_MagnetPalm", new Color(0.45f, 0.5f, 0.58f), 0.3f, 0.55f);

            var dome = Make(root, "ART_MagnetDome", OrganicMeshFactory.DishBowl(0.42f, 0.2f, 10, 36));
            dome.transform.localPosition = new Vector3(0f, 0.72f, 0.15f);
            dome.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            applyMat(dome, "Mat_MagnetDome", new Color(0.85f, 0.25f, 0.28f), 0.25f, 0.65f);

            var core = Make(root, "ART_MagnetCore", OrganicMeshFactory.Icosphere(2, 0.14f));
            core.transform.localPosition = new Vector3(0f, 0.78f, 0.18f);
            applyMat(core, "Mat_MagnetCore", new Color(0.35f, 0.75f, 1f), 0.15f, 0.4f);

            PlaceHook(root, new Vector3(-0.28f, 0.45f, 0.35f), new Vector3(10f, -25f, 35f), applyMat);
            PlaceHook(root, new Vector3(0.28f, 0.45f, 0.35f), new Vector3(10f, 25f, -35f), applyMat);
            PlaceHook(root, new Vector3(0f, 0.35f, 0.42f), new Vector3(25f, 0f, 0f), applyMat);

            var ring = Make(root, "ART_MagnetRing", OrganicMeshFactory.Torus(0.34f, 0.035f, 40, 12));
            ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            applyMat(ring, "Mat_MagnetRing", new Color(0.7f, 0.55f, 0.2f), 0.3f, 0.55f);
        }

        /// <summary>Cluster of paint bubbles on a raft hull — bazaar floater silhouette.</summary>
        public static void BuildPaintBubbleRaft(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null)
            {
                return;
            }

            ClearChildren(root);

            var hull = Make(root, "ART_RaftHull", OrganicMeshFactory.SoftBlob(3, 0.7f, 0.35f, 0.05f));
            hull.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            hull.transform.localScale = new Vector3(1.35f, 1f, 0.95f);
            applyMat(hull, "Mat_RaftHull", new Color(0.75f, 0.55f, 0.35f), 0.45f, 0.12f);

            var rim = Make(root, "ART_RaftRim", OrganicMeshFactory.Torus(0.72f, 0.06f, 48, 14));
            rim.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            rim.transform.localScale = new Vector3(1.2f, 1f, 0.85f);
            applyMat(rim, "Mat_RaftRim", new Color(0.9f, 0.7f, 0.4f), 0.4f, 0.2f);

            PlaceBubble(root, "ART_PaintBubbleA", new Vector3(-0.25f, 0.55f, 0.1f), 0.28f, new Color(1f, 0.35f, 0.55f), applyMat);
            PlaceBubble(root, "ART_PaintBubbleB", new Vector3(0.3f, 0.62f, -0.05f), 0.32f, new Color(0.35f, 0.65f, 1f), applyMat);
            PlaceBubble(root, "ART_PaintBubbleC", new Vector3(0.05f, 0.75f, 0.2f), 0.22f, new Color(0.95f, 0.9f, 0.25f), applyMat);
            PlaceBubble(root, "ART_PaintBubbleD", new Vector3(-0.05f, 0.48f, -0.28f), 0.2f, new Color(0.45f, 0.9f, 0.55f), applyMat);

            var paddle = Make(root, "ART_RaftPaddle", OrganicMeshFactory.RibbonPlane(0.9f, 0.22f, 0.05f, 18, 4, 0.2f));
            paddle.transform.localPosition = new Vector3(0.85f, 0.35f, 0f);
            paddle.transform.localRotation = Quaternion.Euler(0f, 0f, 70f);
            applyMat(paddle, "Mat_RaftPaddle", new Color(0.6f, 0.4f, 0.25f), 0.4f, 0.15f);
        }

        /// <summary>Clockwork beetle with gear body and bridge-plate wings.</summary>
        public static void BuildWindUpBridgeBug(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null)
            {
                return;
            }

            ClearChildren(root);

            var abdomen = Make(root, "ART_BugAbdomen", OrganicMeshFactory.SoftBlob(3, 0.42f, 0.75f, 0.04f));
            abdomen.transform.localPosition = new Vector3(0f, 0.35f, -0.15f);
            abdomen.transform.localScale = new Vector3(1.1f, 0.85f, 1.35f);
            applyMat(abdomen, "Mat_BugAbdomen", new Color(0.7f, 0.55f, 0.3f), 0.35f, 0.45f);

            var gear = Make(root, "ART_BugGear", OrganicMeshFactory.GearRing(0.38f, 0.07f, 10, 0.1f, 56, 12));
            gear.transform.localPosition = new Vector3(0f, 0.4f, 0.15f);
            gear.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            applyMat(gear, "Mat_BugGear", new Color(0.55f, 0.58f, 0.62f), 0.3f, 0.75f);

            var head = Make(root, "ART_BugHead", OrganicMeshFactory.SoftBlob(2, 0.22f, 0.9f, 0.03f));
            head.transform.localPosition = new Vector3(0f, 0.42f, 0.45f);
            applyMat(head, "Mat_BugHead", new Color(0.8f, 0.6f, 0.35f), 0.35f, 0.35f);

            var key = Make(root, "ART_BugWindKey", OrganicMeshFactory.SpringHelix(0.35f, 0.1f, 0.025f, 3, 18));
            key.transform.localPosition = new Vector3(0f, 0.7f, -0.1f);
            key.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
            applyMat(key, "Mat_BugWindKey", new Color(0.9f, 0.75f, 0.25f), 0.3f, 0.5f);

            var wingL = Make(root, "ART_BugWingL", OrganicMeshFactory.RibbonPlane(0.85f, 0.35f, 0.04f, 20, 5, 0.15f));
            wingL.transform.localPosition = new Vector3(-0.45f, 0.48f, 0f);
            wingL.transform.localRotation = Quaternion.Euler(-10f, 15f, 25f);
            applyMat(wingL, "Mat_BugWing", new Color(0.65f, 0.7f, 0.78f), 0.2f, 0.65f);
            var wingR = Make(root, "ART_BugWingR", OrganicMeshFactory.RibbonPlane(0.85f, 0.35f, 0.04f, 20, 5, 0.15f));
            wingR.transform.localPosition = new Vector3(0.45f, 0.48f, 0f);
            wingR.transform.localRotation = Quaternion.Euler(-10f, -15f, -25f);
            applyMat(wingR, "Mat_BugWing", new Color(0.65f, 0.7f, 0.78f), 0.2f, 0.65f);

            PlaceBugLeg(root, new Vector3(-0.3f, 0.1f, 0.25f), -20f, applyMat);
            PlaceBugLeg(root, new Vector3(0.3f, 0.1f, 0.25f), 20f, applyMat);
            PlaceBugLeg(root, new Vector3(-0.35f, 0.08f, -0.2f), -35f, applyMat);
            PlaceBugLeg(root, new Vector3(0.35f, 0.08f, -0.2f), 35f, applyMat);
        }

        /// <summary>Torch nozzle + heat-tape spiral + warm cell — frost thawer silhouette.</summary>
        public static void BuildHeatTapeTorch(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null)
            {
                return;
            }

            ClearChildren(root);

            var grip = Make(root, "ART_TorchGrip", OrganicMeshFactory.RibbedCylinder(0.65f, 0.12f, 5, 0.02f, 28, 10));
            grip.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            applyMat(grip, "Mat_TorchGrip", new Color(0.35f, 0.38f, 0.42f), 0.4f, 0.55f);

            var tape = Make(root, "ART_HeatTape", OrganicMeshFactory.SpringHelix(0.55f, 0.18f, 0.03f, 5, 22));
            tape.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            applyMat(tape, "Mat_HeatTape", new Color(1f, 0.45f, 0.15f), 0.35f, 0.2f);

            var nozzle = Make(root, "ART_TorchNozzle", OrganicMeshFactory.LathedProfile(new[]
            {
                new Vector2(0.08f, 0f), new Vector2(0.14f, 0.08f), new Vector2(0.18f, 0.2f), new Vector2(0.1f, 0.32f), new Vector2(0.06f, 0.4f)
            }, 28, "Organic_TorchNozzle"));
            nozzle.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            applyMat(nozzle, "Mat_TorchNozzle", new Color(0.6f, 0.62f, 0.68f), 0.25f, 0.7f);

            var flame = Make(root, "ART_TorchFlame", OrganicMeshFactory.SoftBlob(2, 0.16f, 1.4f, 0.06f));
            flame.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            applyMat(flame, "Mat_TorchFlame", new Color(1f, 0.75f, 0.25f), 0.2f, 0.05f);

            var cell = Make(root, "ART_WarmCell", OrganicMeshFactory.CrystalFacet(0.35f, 0.1f, 6));
            cell.transform.localPosition = new Vector3(0.22f, 0.25f, 0f);
            cell.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            applyMat(cell, "Mat_WarmCell", new Color(1f, 0.55f, 0.2f), 0.15f, 0.25f);

            var guard = Make(root, "ART_TorchGuard", OrganicMeshFactory.Torus(0.2f, 0.03f, 32, 10));
            guard.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            applyMat(guard, "Mat_TorchGuard", new Color(0.5f, 0.52f, 0.55f), 0.3f, 0.6f);
        }

        /// <summary>Boom dish + redirector stalk + crystal emitter — echo atelier silhouette.</summary>
        public static void BuildBoomDishRedirector(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null)
            {
                return;
            }

            ClearChildren(root);

            var dish = Make(root, "ART_BoomDish", OrganicMeshFactory.DishBowl(0.85f, 0.32f, 14, 44));
            dish.transform.localPosition = new Vector3(0f, 0.55f, 0.15f);
            dish.transform.localRotation = Quaternion.Euler(-55f, 0f, 0f);
            applyMat(dish, "Mat_BoomDish", new Color(0.75f, 0.8f, 0.9f), 0.18f, 0.7f);

            var rim = Make(root, "ART_BoomRim", OrganicMeshFactory.Torus(0.82f, 0.045f, 48, 14));
            rim.transform.localPosition = new Vector3(0f, 0.72f, 0.4f);
            rim.transform.localRotation = Quaternion.Euler(-55f, 0f, 0f);
            applyMat(rim, "Mat_BoomRim", new Color(0.55f, 0.6f, 0.72f), 0.25f, 0.55f);

            var stalk = Make(root, "ART_BoomStalk", OrganicMeshFactory.RibbedCylinder(0.7f, 0.1f, 4, 0.015f, 24, 10));
            stalk.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            applyMat(stalk, "Mat_BoomStalk", new Color(0.45f, 0.48f, 0.55f), 0.35f, 0.5f);

            var baseFoot = Make(root, "ART_BoomBase", OrganicMeshFactory.SoftBlob(2, 0.28f, 0.45f, 0.03f));
            baseFoot.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            applyMat(baseFoot, "Mat_BoomBase", new Color(0.4f, 0.42f, 0.5f), 0.4f, 0.35f);

            var emitter = Make(root, "ART_BoomEmitter", OrganicMeshFactory.CrystalFacet(0.45f, 0.12f, 5));
            emitter.transform.localPosition = new Vector3(0f, 0.55f, 0.55f);
            emitter.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            applyMat(emitter, "Mat_BoomEmitter", new Color(0.55f, 0.85f, 1f), 0.12f, 0.15f);

            var redirect = Make(root, "ART_BoomRedirect", OrganicMeshFactory.HookCurve(0.5f, 0.4f, 0.035f, 22));
            redirect.transform.localPosition = new Vector3(0.35f, 0.6f, 0.2f);
            redirect.transform.localRotation = Quaternion.Euler(0f, -40f, 10f);
            applyMat(redirect, "Mat_BoomRedirect", new Color(0.9f, 0.7f, 0.3f), 0.3f, 0.45f);
        }

        /// <summary>Hover disc + vacuum spiral intake + skim fins — tide yard silhouette.</summary>
        public static void BuildVacuumSkimmer(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null)
            {
                return;
            }

            ClearChildren(root);

            var deck = Make(root, "ART_SkimmerDeck", OrganicMeshFactory.SoftBlob(3, 0.55f, 0.4f, 0.04f));
            deck.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            deck.transform.localScale = new Vector3(1.4f, 1f, 1f);
            applyMat(deck, "Mat_SkimmerDeck", new Color(0.35f, 0.55f, 0.65f), 0.35f, 0.4f);

            var intake = Make(root, "ART_SkimmerIntake", OrganicMeshFactory.SpiralShell(2.2f, 0.06f, 0.4f, 0.055f, 0.35f, 64));
            intake.transform.localPosition = new Vector3(0f, 0.35f, 0.15f);
            intake.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            applyMat(intake, "Mat_SkimmerIntake", new Color(0.5f, 0.7f, 0.8f), 0.25f, 0.55f);

            var rim = Make(root, "ART_SkimmerRim", OrganicMeshFactory.Torus(0.58f, 0.05f, 44, 12));
            rim.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            rim.transform.localScale = new Vector3(1.25f, 1f, 0.95f);
            applyMat(rim, "Mat_SkimmerRim", new Color(0.7f, 0.75f, 0.8f), 0.3f, 0.6f);

            var finL = Make(root, "ART_SkimmerFinL", OrganicMeshFactory.RibbonPlane(0.7f, 0.2f, 0.06f, 16, 4, 0.25f));
            finL.transform.localPosition = new Vector3(-0.55f, 0.2f, -0.15f);
            finL.transform.localRotation = Quaternion.Euler(15f, 20f, 55f);
            applyMat(finL, "Mat_SkimmerFin", new Color(0.3f, 0.75f, 0.7f), 0.4f, 0.25f);
            var finR = Make(root, "ART_SkimmerFinR", OrganicMeshFactory.RibbonPlane(0.7f, 0.2f, 0.06f, 16, 4, 0.25f));
            finR.transform.localPosition = new Vector3(0.55f, 0.2f, -0.15f);
            finR.transform.localRotation = Quaternion.Euler(15f, -20f, -55f);
            applyMat(finR, "Mat_SkimmerFin", new Color(0.3f, 0.75f, 0.7f), 0.4f, 0.25f);

            var spout = Make(root, "ART_SkimmerSpout", OrganicMeshFactory.HookCurve(0.4f, 0.35f, 0.04f, 20));
            spout.transform.localPosition = new Vector3(0f, 0.4f, -0.35f);
            spout.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            applyMat(spout, "Mat_SkimmerSpout", new Color(0.55f, 0.6f, 0.68f), 0.3f, 0.5f);

            var glow = Make(root, "ART_SkimmerGlow", OrganicMeshFactory.Icosphere(2, 0.1f));
            glow.transform.localPosition = new Vector3(0f, 0.42f, 0.05f);
            applyMat(glow, "Mat_SkimmerGlow", new Color(0.4f, 1f, 0.9f), 0.15f, 0.1f);
        }

        /// <summary>Planter cup + star-burst seeds + grow lamp — nursery silhouette.</summary>
        public static void BuildStarSeedPlanter(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null)
            {
                return;
            }

            ClearChildren(root);

            var cup = Make(root, "ART_PlanterCup", OrganicMeshFactory.DishBowl(0.55f, 0.35f, 12, 36));
            cup.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            applyMat(cup, "Mat_PlanterCup", new Color(0.55f, 0.4f, 0.55f), 0.4f, 0.2f);

            var soil = Make(root, "ART_PlanterSoil", OrganicMeshFactory.SoftBlob(2, 0.4f, 0.35f, 0.05f));
            soil.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            applyMat(soil, "Mat_PlanterSoil", new Color(0.35f, 0.22f, 0.18f), 0.55f, 0.05f);

            var starA = Make(root, "ART_StarSeedA", OrganicMeshFactory.FacetedStar(0.22f, 0.09f, 0.08f, 5));
            starA.transform.localPosition = new Vector3(-0.12f, 0.55f, 0.08f);
            starA.transform.localRotation = Quaternion.Euler(15f, 20f, 10f);
            applyMat(starA, "Mat_StarSeed", new Color(1f, 0.9f, 0.4f), 0.15f, 0.35f);
            var starB = Make(root, "ART_StarSeedB", OrganicMeshFactory.StarBurst(0.18f, 0.07f, 0.07f, 6));
            starB.transform.localPosition = new Vector3(0.15f, 0.6f, -0.05f);
            starB.transform.localRotation = Quaternion.Euler(-10f, -30f, 25f);
            applyMat(starB, "Mat_StarSeed", new Color(0.85f, 0.7f, 1f), 0.15f, 0.3f);
            var starC = Make(root, "ART_StarSeedC", OrganicMeshFactory.FacetedStar(0.14f, 0.055f, 0.06f, 5));
            starC.transform.localPosition = new Vector3(0.02f, 0.7f, 0.12f);
            applyMat(starC, "Mat_StarSeed", new Color(0.6f, 0.95f, 1f), 0.12f, 0.25f);

            var stem = Make(root, "ART_GrowLampStem", OrganicMeshFactory.RibbedCylinder(0.55f, 0.05f, 3, 0.01f, 18, 8));
            stem.transform.localPosition = new Vector3(0.35f, 0.45f, -0.2f);
            stem.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            applyMat(stem, "Mat_GrowLampStem", new Color(0.5f, 0.52f, 0.58f), 0.35f, 0.5f);

            var lamp = Make(root, "ART_GrowLamp", OrganicMeshFactory.SoftBlob(2, 0.16f, 0.7f, 0.03f));
            lamp.transform.localPosition = new Vector3(0.48f, 0.85f, -0.25f);
            applyMat(lamp, "Mat_GrowLamp", new Color(1f, 0.95f, 0.7f), 0.2f, 0.15f);

            var shell = Make(root, "ART_PlanterShellAccent", OrganicMeshFactory.SpiralShell(1.6f, 0.04f, 0.22f, 0.03f, 0.25f, 48));
            shell.transform.localPosition = new Vector3(-0.35f, 0.2f, 0.15f);
            shell.transform.localScale = Vector3.one * 0.7f;
            applyMat(shell, "Mat_PlanterShell", new Color(0.7f, 0.5f, 0.75f), 0.35f, 0.25f);
        }

        /// <summary>Curved glass sail + shield boss + crystal spine — cathedral silhouette.</summary>
        public static void BuildGlassSailShield(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            if (root == null || applyMat == null)
            {
                return;
            }

            ClearChildren(root);

            var sail = Make(root, "ART_GlassSail", OrganicMeshFactory.RibbonPlane(1.4f, 0.95f, 0.12f, 28, 10, 0.85f));
            sail.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            sail.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            applyMat(sail, "Mat_GlassSail", new Color(0.65f, 0.85f, 1f, 0.85f), 0.1f, 0.55f);

            var rim = Make(root, "ART_SailRim", OrganicMeshFactory.ArchGateway(1.1f, 1.3f, 0.12f, 24));
            rim.transform.localPosition = new Vector3(0f, 0.15f, 0.05f);
            rim.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            rim.transform.localScale = new Vector3(0.85f, 0.9f, 0.85f);
            applyMat(rim, "Mat_SailRim", new Color(0.75f, 0.78f, 0.85f), 0.2f, 0.65f);

            var boss = Make(root, "ART_ShieldBoss", OrganicMeshFactory.GearRing(0.22f, 0.05f, 8, 0.06f, 40, 10));
            boss.transform.localPosition = new Vector3(0f, 0.55f, 0.12f);
            boss.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            applyMat(boss, "Mat_ShieldBoss", new Color(0.85f, 0.7f, 0.35f), 0.25f, 0.7f);

            var spine = Make(root, "ART_SailSpine", OrganicMeshFactory.CrystalFacet(0.9f, 0.1f, 6));
            spine.transform.localPosition = new Vector3(0f, 0.65f, -0.05f);
            applyMat(spine, "Mat_SailSpine", new Color(0.9f, 0.95f, 1f), 0.12f, 0.2f);

            var grip = Make(root, "ART_ShieldGrip", OrganicMeshFactory.RibbedCylinder(0.45f, 0.07f, 4, 0.012f, 20, 8));
            grip.transform.localPosition = new Vector3(0f, 0.35f, -0.2f);
            grip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            applyMat(grip, "Mat_ShieldGrip", new Color(0.5f, 0.45f, 0.4f), 0.4f, 0.35f);

            var star = Make(root, "ART_SailEmblem", OrganicMeshFactory.FacetedStar(0.16f, 0.06f, 0.05f, 5));
            star.transform.localPosition = new Vector3(0f, 0.55f, 0.18f);
            applyMat(star, "Mat_SailEmblem", new Color(1f, 0.85f, 0.4f), 0.15f, 0.4f);
        }

        private static void PlaceHook(Transform root, Vector3 pos, Vector3 euler, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var hook = Make(root, "ART_MagnetHook", OrganicMeshFactory.HookCurve(0.35f, 0.28f, 0.03f, 18));
            hook.transform.localPosition = pos;
            hook.transform.localRotation = Quaternion.Euler(euler);
            applyMat(hook, "Mat_MagnetHook", new Color(0.65f, 0.68f, 0.72f), 0.3f, 0.65f);
        }

        private static void PlaceBubble(Transform root, string name, Vector3 pos, float radius, Color color, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var bubble = Make(root, name, OrganicMeshFactory.SoftBlob(3, radius, 0.95f, 0.03f));
            bubble.transform.localPosition = pos;
            applyMat(bubble, "Mat_PaintBubble", color, 0.15f, 0.05f);
        }

        private static void PlaceBugLeg(Transform root, Vector3 pos, float zRot, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var path = new List<Vector3>
            {
                Vector3.zero,
                new Vector3(0f, -0.12f, 0.05f),
                new Vector3(0f, -0.28f, 0.02f),
                new Vector3(0f, -0.38f, 0.08f)
            };
            var leg = Make(root, "ART_BugLeg", OrganicMeshFactory.TubeAlongPath(path, 0.03f, 8, "Organic_BugLeg"));
            leg.transform.localPosition = pos;
            leg.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
            applyMat(leg, "Mat_BugLeg", new Color(0.5f, 0.42f, 0.28f), 0.4f, 0.3f);
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
