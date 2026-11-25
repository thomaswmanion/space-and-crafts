#if UNITY_EDITOR
using System.Text;
using SpaceAndCrafts.AI;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Evidence check that Arrival critical wiring is present (not a feel playtest).
    /// </summary>
    public static class ArrivalSceneValidator
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity";

        [MenuItem("Space & Crafts/Validate Arrival Scene Wiring")]
        public static void ValidateMenu() => ValidateAndLog();

        public static void ValidateAndLog()
        {
            var ok = Validate(out var report);
            if (ok)
            {
                Debug.Log("[Space & Crafts] Arrival wiring VALID.\n" + report);
            }
            else
            {
                Debug.LogError("[Space & Crafts] Arrival wiring FAILED.\n" + report);
            }
        }

        /// <summary>Batchmode entry: exits 0 if valid, 1 if not (via exception).</summary>
        public static void ValidateOrThrow()
        {
            if (!Validate(out var report))
            {
                throw new System.Exception("Arrival wiring validation failed:\n" + report);
            }

            Debug.Log("[Space & Crafts] Arrival wiring VALID.\n" + report);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        public static bool Validate(out string report)
        {
            var sb = new StringBuilder();
            var pass = true;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                report = "Could not open Arrival scene.";
                return false;
            }

            void Require<T>(string label) where T : Object
            {
                var found = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
                if (found == null)
                {
                    pass = false;
                    sb.AppendLine($"FAIL: missing {label} ({typeof(T).Name})");
                }
                else
                {
                    sb.AppendLine($"OK: {label}");
                }
            }

            Require<SplatMotor>("Splat motor");
            Require<SplatInputReader>("Input reader");
            Require<PlayerRespawn>("Player respawn");
            Require<PlatformingCameraRig>("Camera rig");
            Require<BuildSite>("Build site");
            Require<BoingOMatic>("Boing-O-Matic");
            Require<ScrapMite>("ScrapMite");
            Require<ScrapStormSentinel>("Scrap-Storm Sentinel");
            Require<BossWeakPoint>("Boss weak point");
            Require<FallVolume>("Fall volume");
            Require<CheckpointVolume>("Checkpoint");
            Require<LevelFlowController>("Level flow / restart");
            Require<LevelSession>("Level session");
            Require<GameplayToastHud>("Toast HUD");
            Require<CollectibleHud>("Collectible HUD");
            Require<GatewayRevealPresenter>("Gateway reveal");
            Require<GatewayCompleteVista>("Gateway complete vista");
            Require<ArrivalOpeningVista>("Arrival opening vista");
            Require<CraftAdmireBeat>("Craft admire beat");
            Require<TempSfxPlayer>("TEMP SFX player");
            Require<InventionRequiredHint>("Gulch invention hint");
            Require<TempArtBanner>("TEMP ART banner");

            var motor = Object.FindAnyObjectByType<SplatMotor>();
            if (motor != null)
            {
                var so = new SerializedObject(motor);
                if (so.FindProperty("settings").objectReferenceValue == null)
                {
                    pass = false;
                    sb.AppendLine("FAIL: SplatMotor.settings is null");
                }
                else
                {
                    sb.AppendLine("OK: SplatMotor.settings assigned");
                }

                if (so.FindProperty("cameraTransform").objectReferenceValue == null)
                {
                    pass = false;
                    sb.AppendLine("FAIL: SplatMotor.cameraTransform is null");
                }
                else
                {
                    sb.AppendLine("OK: SplatMotor.cameraTransform assigned");
                }
            }

            var cam = Object.FindAnyObjectByType<PlatformingCameraRig>();
            if (cam != null)
            {
                var so = new SerializedObject(cam);
                if (so.FindProperty("settings").objectReferenceValue == null)
                {
                    pass = false;
                    sb.AppendLine("FAIL: PlatformingCameraRig.settings is null");
                }
                else
                {
                    sb.AppendLine("OK: PlatformingCameraRig.settings assigned");
                }
            }

            var pickups = Object.FindObjectsByType<CollectiblePickup>(FindObjectsInactive.Include);
            sb.AppendLine($"OK: CollectiblePickup count = {pickups.Length}");
            if (pickups.Length < 4)
            {
                pass = false;
                sb.AppendLine("FAIL: expected >= 4 pickups (goo + 3 components + optional badge)");
            }

            var nullDefs = 0;
            foreach (var pickup in pickups)
            {
                if (pickup == null || pickup.Definition == null || string.IsNullOrWhiteSpace(pickup.Definition.Id))
                {
                    nullDefs++;
                    pass = false;
                    sb.AppendLine($"FAIL: pickup '{pickup?.name}' has null/empty CollectibleDefinition");
                }
            }

            if (nullDefs == 0)
            {
                sb.AppendLine("OK: all pickup definitions assigned");
            }

            var banners = Object.FindObjectsByType<TempArtBanner>(FindObjectsInactive.Include);
            sb.AppendLine($"OK: TempArtBanner count = {banners.Length}");
            if (banners.Length < 1)
            {
                pass = false;
                sb.AppendLine("FAIL: no visible TEMP ART banners");
            }

            pass &= ValidateCollidersAndCoverage(sb);

            report = sb.ToString();
            return pass;
        }

        /// <summary>
        /// Sample critical-path ground + fall/checkpoint trigger sanity (not a feel playtest).
        /// </summary>
        private static bool ValidateCollidersAndCoverage(StringBuilder sb)
        {
            var pass = true;
            Physics.SyncTransforms();

            var fall = Object.FindAnyObjectByType<FallVolume>(FindObjectsInactive.Include);
            if (fall != null)
            {
                var col = fall.GetComponent<Collider>();
                if (col == null || !col.isTrigger)
                {
                    pass = false;
                    sb.AppendLine("FAIL: FallVolume collider missing or not trigger");
                }
                else if (fall.transform.position.y > -2f)
                {
                    pass = false;
                    sb.AppendLine($"FAIL: FallVolume too high (y={fall.transform.position.y:0.0}); expected under level");
                }
                else
                {
                    sb.AppendLine("OK: FallVolume trigger under level");
                }
            }

            var checkpoints = Object.FindObjectsByType<CheckpointVolume>(FindObjectsInactive.Include);
            var triggerCheckpoints = 0;
            foreach (var cp in checkpoints)
            {
                var col = cp.GetComponent<Collider>();
                if (col != null && col.isTrigger)
                {
                    triggerCheckpoints++;
                }
            }

            sb.AppendLine($"OK: CheckpointVolume triggers = {triggerCheckpoints}/{checkpoints.Length}");
            if (triggerCheckpoints < 4)
            {
                pass = false;
                sb.AppendLine("FAIL: expected >= 4 checkpoint triggers along Arrival path");
            }

            // Critical-path sample points in the Arrival root's authored space.
            // The level is presented at a reduced root scale, so raycast probes
            // must follow that transform instead of treating authored coordinates
            // as world coordinates. Offset X on spawn to avoid Splat's controller.
            var arrivalRoot = GameObject.Find("TEMP_ART_ArrivalWorld")?.transform;
            Vector3 ToWorld(Vector3 authoredPoint) => arrivalRoot != null
                ? arrivalRoot.TransformPoint(authoredPoint)
                : authoredPoint;

            var groundedSamples = new[]
            {
                ("Shelf spawn", new Vector3(1.5f, 8f, -2f)),
                ("Paintfall Glade", new Vector3(0f, 8f, 18f)),
                ("Junk Arch Workshop", new Vector3(0f, 8f, 34f)),
                ("Launch Gulch near", new Vector3(0f, 8f, 48f)),
                ("Launch Gulch far", new Vector3(0f, 12f, 60f)),
                ("Gateway Terrace", new Vector3(0f, 12f, 74f)),
            };

            foreach (var (label, origin) in groundedSamples)
            {
                var worldOrigin = ToWorld(origin);
                if (TryRaycastSolidGround(worldOrigin, out var hit))
                {
                    sb.AppendLine($"OK: ground under {label} ({hit.collider.name} @ y={hit.point.y:0.00})");
                }
                else
                {
                    pass = false;
                    sb.AppendLine($"FAIL: no solid ground under {label} at {worldOrigin}");
                }
            }

            // Intentional gulch gap — must NOT have walkable ground in the middle.
            var gapOrigin = ToWorld(new Vector3(0f, 8f, 54f));
            if (Physics.Raycast(gapOrigin, Vector3.down, out var gapHit, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                pass = false;
                sb.AppendLine($"FAIL: gulch gap blocked by {gapHit.collider.name} (Boing traversal foreshadow broken)");
            }
            else
            {
                sb.AppendLine("OK: gulch gap open (no near ground in middle)");
            }

            return pass;
        }

        private static bool TryRaycastSolidGround(Vector3 origin, out RaycastHit hit)
        {
            var hits = Physics.RaycastAll(origin, Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (var i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider != null && hits[i].collider.GetComponentInParent<SplatMotor>() != null)
                {
                    continue;
                }

                hit = hits[i];
                return true;
            }

            hit = default;
            return false;
        }
    }
}
#endif
