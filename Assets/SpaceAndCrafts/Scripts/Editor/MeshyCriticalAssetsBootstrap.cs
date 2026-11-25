#if UNITY_EDITOR
using System.IO;
using SpaceAndCrafts.AI;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Wires Meshy-generated critical FBX/GLB into Arrival for visual Play Mode testing.
    /// </summary>
    public static class MeshyCriticalAssetsBootstrap
    {
        private const string ArrivalScene = "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity";
        private const string Commissioned = "Assets/SpaceAndCrafts/Art/Commissioned";

        private static readonly (string id, string folder)[] Assets =
        {
            ("Splat_Hero", "Hero"),
            ("BoingOMatic", "Inventions"),
            ("ScrapMite", "Enemies"),
            ("ScrapFloater", "Enemies"),
            ("ScrapRoller", "Enemies"),
            ("ScrapStormSentinel", "Enemies"),
            ("RainbowGateway", "Landmarks"),
            ("RainbowCraftTree", "Environments"),
            ("ScrapRockCluster", "Environments"),
            ("PowerCrystalPickup", "Inventions"),
        };

        [MenuItem("Space & Crafts/Sanitize Meshy Showcase Colliders")]
        public static void SanitizeShowcaseColliders()
        {
            var scene = EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            var fixedCount = SanitizeShowcaseRoot(GameObject.Find("ART_MeshyCriticalShowcase"));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Space & Crafts] Meshy showcase sanitize: {fixedCount} objects corrected.");
        }

        /// <summary>
        /// Strips showcase Boing launch storms and rescales FBX×100 SphereColliders to playable world size.
        /// Safe to re-run; does not add Meshy assets.
        /// </summary>
        public static int SanitizeShowcaseRoot(GameObject root)
        {
            if (root == null)
            {
                return 0;
            }

            var fixedCount = 0;
            foreach (var boing in root.GetComponentsInChildren<BoingOMatic>(true))
            {
                if (boing == null)
                {
                    continue;
                }

                StripImportedColliders(boing.gameObject);
                Object.DestroyImmediate(boing);
                fixedCount++;
            }

            foreach (var col in root.GetComponentsInChildren<SphereCollider>(true))
            {
                if (col == null)
                {
                    continue;
                }

                var lossy = col.transform.lossyScale;
                var avg = (Mathf.Abs(lossy.x) + Mathf.Abs(lossy.y) + Mathf.Abs(lossy.z)) / 3f;
                var worldRadius = col.radius * avg;
                if (worldRadius <= 8f)
                {
                    continue;
                }

                // Keep a small playable bump/pickup radius; never leave ~180m triggers.
                var targetWorld = col.isTrigger ? 0.7f : 1.1f;
                col.radius = targetWorld / Mathf.Max(0.01f, avg);
                fixedCount++;
            }

            return fixedCount;
        }

        [MenuItem("Space & Crafts/Bootstrap Meshy Critical Assets Into Arrival")]
        public static void Bootstrap()
        {
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            var world = GameObject.Find("TEMP_ART_ArrivalWorld");
            var parent = world != null ? world.transform : null;

            var rootName = "ART_MeshyCriticalShowcase";
            var old = GameObject.Find(rootName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            var root = new GameObject(rootName);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            // Right of spawn so the path stays clear and giant props can't pin Splat at start.
            root.transform.position = new Vector3(6f, 0f, 2f);

            HideTempArtNoise();
            EnsureShowcasePad(root.transform);

            var x = 0f;
            foreach (var (id, folder) in Assets)
            {
                var model = LoadModel(folder, id);
                if (model == null)
                {
                    Debug.LogWarning($"[Space & Crafts] Meshy asset missing: {folder}/{id}");
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                if (instance == null)
                {
                    instance = Object.Instantiate(model);
                }

                instance.name = $"ART_Meshy_{id}";
                instance.transform.SetParent(root.transform, false);
                instance.transform.localPosition = new Vector3(x, 0.05f, 0f);
                // Normalize FBX ×100 roots, then fit to a playable showcase size.
                instance.transform.localScale = Vector3.one;
                StripImportedColliders(instance);
                FitToMaxSize(instance, 2.2f);
                WireGameplay(instance, id);
                x += 3.2f;
            }

            // Do NOT swap player Splat to Meshy — first-shot hero reads creepy/wrong; keep organic Splat.
            RestoreOrganicSplatVisual();
            TrySwapBoingVisual();
            TrySwapGatewayVisual();
            TrySwapExistingEnemyVisuals();
            TrySwapBossVisual();
            TrySwapCrystalPickupVisual();
            SanitizeShowcaseRoot(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Space & Crafts] Meshy critical assets bootstrapped into Arrival showcase + key swaps.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void HideTempArtNoise()
        {
            foreach (var banner in Object.FindObjectsByType<TempArtBanner>(FindObjectsInactive.Include))
            {
                if (banner != null)
                {
                    banner.gameObject.SetActive(false);
                }
            }
        }

        private static void EnsureShowcasePad(Transform root)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "ART_MeshyShowcasePad";
            pad.transform.SetParent(root, false);
            pad.transform.localPosition = new Vector3(14f, -0.05f, 0f);
            pad.transform.localScale = new Vector3(34f, 0.1f, 6f);
            var rend = pad.GetComponent<Renderer>();
            if (rend != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    rend.sharedMaterial = new Material(shader) { color = new Color(0.15f, 0.18f, 0.28f) };
                }
            }

            var col = pad.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }

            var labelGo = new GameObject("ART_MeshyShowcaseLabel");
            labelGo.transform.SetParent(root, false);
            labelGo.transform.localPosition = new Vector3(14f, 3.2f, -2.2f);
            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = "Craft curios — bounce, scrap, and shine";
            tm.fontSize = 42;
            tm.characterSize = 0.07f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.92f, 0.55f, 0.85f);
        }

        private static GameObject LoadModel(string folder, string id)
        {
            // Prefer URP-materialized prefab (textures wired) over raw FBX/GLB.
            var urp = $"{Commissioned}/{folder}/{id}_URP.prefab";
            var fbx = $"{Commissioned}/{folder}/{id}.fbx";
            var glb = $"{Commissioned}/{folder}/{id}.glb";
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(urp);
            if (go == null)
            {
                go = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            }

            if (go == null)
            {
                go = AssetDatabase.LoadAssetAtPath<GameObject>(glb);
            }

            return go;
        }

        /// <summary>
        /// Meshy FBX roots often ship at localScale 100 (cm). Always multiply the current scale —
        /// replacing with an absolute 1.x value collapses models into near-invisible dots.
        /// Fit by longest axis so flat props (rocks) don't explode to scale 600+.
        /// </summary>
        private static void FitToMaxSize(GameObject instance, float targetSize)
        {
            var rends = instance.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
            {
                return;
            }

            var bounds = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
            {
                bounds.Encapsulate(rends[i].bounds);
            }

            var max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (max > 1e-4f)
            {
                instance.transform.localScale *= targetSize / max;
            }

            // Sit on ground after scale.
            rends = instance.GetComponentsInChildren<Renderer>();
            bounds = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
            {
                bounds.Encapsulate(rends[i].bounds);
            }

            var deltaY = instance.transform.position.y - bounds.min.y;
            instance.transform.position += Vector3.up * (deltaY + 0.02f);
        }

        private static void WireGameplay(GameObject instance, string id)
        {
            switch (id)
            {
                case "ScrapMite":
                    EnsureTrigger(instance, 0.55f);
                    if (instance.GetComponent<ScrapMite>() == null)
                    {
                        instance.AddComponent<ScrapMite>();
                    }

                    break;
                case "ScrapFloater":
                    EnsureTrigger(instance, 0.55f);
                    var floater = instance.GetComponent<ScrapFloater>() ?? instance.AddComponent<ScrapFloater>();
                    floater.Configure(instance.transform.position, 1.4f, 1.1f);
                    break;
                case "ScrapRoller":
                    EnsureTrigger(instance, 0.6f);
                    var roller = instance.GetComponent<ScrapRoller>() ?? instance.AddComponent<ScrapRoller>();
                    roller.Configure(instance.transform.position);
                    break;
                case "ScrapStormSentinel":
                    EnsureTrigger(instance, 1.2f);
                    // Showcase: patrol-tier scrap AI so you can bump it without full boss wiring.
                    if (instance.GetComponent<ScrapMite>() == null)
                    {
                        instance.AddComponent<ScrapMite>();
                    }

                    break;
                case "BoingOMatic":
                    // Showcase row is visual only — never attach BoingOMatic at FBX×100 lossy scale
                    // (SphereCollider radius 0.9 * 200 ≈ 180m OnTriggerStay launch storm).
                    foreach (var boing in instance.GetComponentsInChildren<BoingOMatic>(true))
                    {
                        Object.DestroyImmediate(boing);
                    }

                    StripImportedColliders(instance);
                    break;
                case "PowerCrystalPickup":
                    WireCrystalPickup(instance);
                    break;
                case "RainbowCraftTree":
                case "ScrapRockCluster":
                    // Visual-only in showcase — solid colliders at bad fits were pinning Splat.
                    break;
            }
        }

        private static void StripImportedColliders(GameObject root)
        {
            foreach (var col in root.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(col);
            }

            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                Object.DestroyImmediate(rb);
            }
        }

        private static void WireCrystalPickup(GameObject instance)
        {
            EnsureTrigger(instance, 0.7f);
            var def = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>(
                "Assets/SpaceAndCrafts/ScriptableObjects/Collectibles/component_power_crystal.asset");
            var pickup = instance.GetComponent<CollectiblePickup>() ?? instance.AddComponent<CollectiblePickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("definition").objectReferenceValue = def;
            so.FindProperty("collectOnTriggerEnter").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (instance.GetComponent<PickupMotion>() == null)
            {
                instance.AddComponent<PickupMotion>();
            }
        }

        private static void EnsureSolidCollider(GameObject go)
        {
            if (go.GetComponent<Collider>() != null)
            {
                return;
            }

            var box = go.AddComponent<BoxCollider>();
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
            {
                return;
            }

            var bounds = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
            {
                bounds.Encapsulate(rends[i].bounds);
            }

            box.center = go.transform.InverseTransformPoint(bounds.center);
            var lossy = go.transform.lossyScale;
            box.size = new Vector3(
                bounds.size.x / Mathf.Max(0.01f, Mathf.Abs(lossy.x)),
                bounds.size.y / Mathf.Max(0.01f, Mathf.Abs(lossy.y)),
                bounds.size.z / Mathf.Max(0.01f, Mathf.Abs(lossy.z)));
        }

        private static void EnsureTrigger(GameObject go, float worldRadius)
        {
            var col = go.GetComponent<SphereCollider>();
            if (col == null)
            {
                col = go.AddComponent<SphereCollider>();
            }

            col.isTrigger = true;
            // SphereCollider.radius is local — compensate Meshy FBX lossy scale so world size stays playable.
            var lossy = go.transform.lossyScale;
            var avg = (Mathf.Abs(lossy.x) + Mathf.Abs(lossy.y) + Mathf.Abs(lossy.z)) / 3f;
            col.radius = worldRadius / Mathf.Max(0.01f, avg);
            if (go.GetComponent<Rigidbody>() == null)
            {
                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private static void TrySwapBoingVisual()
        {
            var boingModel = LoadModel("Inventions", "BoingOMatic");
            if (boingModel == null)
            {
                return;
            }

            foreach (var boing in Object.FindObjectsByType<BoingOMatic>())
            {
                if (boing == null)
                {
                    continue;
                }

                // Skip showcase instances we just created.
                if (boing.transform.root.name == "ART_MeshyCriticalShowcase")
                {
                    continue;
                }

                var holder = boing.transform.Find("ART_MeshyBoingVisual");
                if (holder != null)
                {
                    Object.DestroyImmediate(holder.gameObject);
                }

                foreach (Transform child in boing.transform)
                {
                    if (child.name.StartsWith("ART_") || child.name.StartsWith("TEMP_"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }

                var visual = (GameObject)PrefabUtility.InstantiatePrefab(boingModel);
                visual.name = "ART_MeshyBoingVisual";
                visual.transform.SetParent(boing.transform, false);
                visual.transform.localPosition = Vector3.zero;
                FitUnderParent(visual, 1.5f);
            }
        }

        private static void TrySwapGatewayVisual()
        {
            var gateModel = LoadModel("Landmarks", "RainbowGateway");
            if (gateModel == null)
            {
                return;
            }

            var gate = GameObject.Find("TEMP_ART_MultiGatewayReveal");
            if (gate == null)
            {
                return;
            }

            var holder = gate.transform.Find("ART_MeshyGatewayVisual");
            if (holder != null)
            {
                Object.DestroyImmediate(holder.gameObject);
            }

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(gateModel);
            visual.name = "ART_MeshyGatewayVisual";
            visual.transform.SetParent(gate.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0f, 0f);
            FitUnderParent(visual, 6f);
        }

        private static void FitUnderParent(GameObject instance, float targetHeight)
        {
            // Normalize FBX root scale before measuring — compounding ×100 with Fit blew Meshy to 100m+ colliders.
            instance.transform.localScale = Vector3.one;
            StripImportedColliders(instance);
            FitToMaxSize(instance, targetHeight);
        }

        private static void RestoreOrganicSplatVisual()
        {
            var splat = GameObject.Find("Splat_TEMP");
            if (splat == null)
            {
                return;
            }

            if (SplatMeshyVisualMode.IsEnabledOn(splat.transform))
            {
                // Respect the explicit player-character choice. This bootstrap
                // still restores Organic Splat for all unmarked legacy scenes.
                SplatVisualRepair.Repair(splat.transform);
                return;
            }

            var meshy = splat.transform.Find("ART_MeshySplatVisual");
            if (meshy != null)
            {
                Object.DestroyImmediate(meshy.gameObject);
            }

            var organic = splat.transform.Find(OrganicSplatBuilder.VisualRootName);
            if (organic != null)
            {
                organic.gameObject.SetActive(true);
            }
        }

        private static void AttachMeshyChild(Transform parent, GameObject model, string holderName, float targetHeight)
        {
            if (parent == null || model == null)
            {
                return;
            }

            var old = parent.Find(holderName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            foreach (Transform child in parent)
            {
                if (child.name.StartsWith("ART_") || child.name.StartsWith("TEMP_"))
                {
                    // Keep labels / weak points / rings; hide organic mesh shells.
                    if (child.name.Contains("LABEL") || child.name.Contains("Weak") || child.name.Contains("Danger") ||
                        child.name.Contains("Ring"))
                    {
                        continue;
                    }

                    child.gameObject.SetActive(false);
                }
            }

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = holderName;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            StripImportedColliders(visual);
            FitUnderParent(visual, targetHeight);
        }

        private static void TrySwapExistingEnemyVisuals()
        {
            // Prefer the existing commissioned mesh islands with their authored
            // local pivots. The raw Meshy source has no clips, but this rig lets
            // ScrapMiteCommissionedAnimator pose the visible feet and limbs.
            // Fall back to the static Meshy model when the rig prefab is absent.
            var miteRig = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/SpaceAndCrafts/Enemies/ScrapMite_Rigged.prefab");
            var miteModel = miteRig != null ? miteRig : LoadModel("Enemies", "ScrapMite");
            SwapTypeVisuals<ScrapMite>(miteModel, "ART_MeshyEnemyVisual", 1.1f);
            SwapTypeVisuals<ScrapFloater>(LoadModel("Enemies", "ScrapFloater"), "ART_MeshyEnemyVisual", 1.2f);
            SwapTypeVisuals<ScrapRoller>(LoadModel("Enemies", "ScrapRoller"), "ART_MeshyEnemyVisual", 1.2f);
        }

        [MenuItem("Space & Crafts/Install Articulated Mite Visual In Arrival (Iter1129)")]
        public static void InstallArticulatedMiteVisualInArrivalIter1129()
        {
            AssetDatabase.Refresh();
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/SpaceAndCrafts/Enemies/ScrapMite_Rigged.prefab");
            if (rig == null)
            {
                Debug.LogError("[Space & Crafts] Iter1129 could not find the existing ScrapMite_Rigged prefab.");
                if (Application.isBatchMode) EditorApplication.Exit(2);
                return;
            }

            var scene = EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            var mites = Object.FindObjectsByType<ScrapMite>();
            SwapTypeVisuals<ScrapMite>(rig, "ART_MeshyEnemyVisual", 1.1f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Iter1129 installed the authored-pivot Meshy Mite rig on {mites.Length} Arrival Mite instances; each scene visual now has ScrapMiteCommissionedAnimator when the rig prefab is available.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void SwapTypeVisuals<T>(GameObject model, string holderName, float height) where T : Component
        {
            if (model == null)
            {
                return;
            }

            foreach (var enemy in Object.FindObjectsByType<T>())
            {
                if (enemy == null || enemy.transform.root.name == "ART_MeshyCriticalShowcase")
                {
                    continue;
                }

                AttachMeshyChild(enemy.transform, model, holderName, height);
            }
        }

        private static void TrySwapBossVisual()
        {
            // Prefer the commissioned Rainbow Kingdom Regent with its imported
            // Generic rig/controller. The older ScrapStormSentinel delivery is
            // a single unrigged mesh; keeping it as fallback avoids blocking
            // the Arrival scene if the animation candidate is unavailable.
            const string regentCandidatePath =
                "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1408/UnityCandidate/ChromaticRegent_AnimatedCandidate.prefab";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(regentCandidatePath)
                ?? LoadModel("Enemies", "ScrapStormSentinel");
            var boss = Object.FindAnyObjectByType<ScrapStormSentinel>();
            if (model == null || boss == null || boss.transform.root.name == "ART_MeshyCriticalShowcase")
            {
                return;
            }

            // The arrival-context capture showed the Regent reading too small
            // against the broad terrace. Give the existing rig a clearer boss
            // silhouette while retaining its authored proportions.
            AttachMeshyChild(boss.transform, model, "ART_MeshyBossVisual", 3.8f);
            if (model.name == "ChromaticRegent_AnimatedCandidate")
            {
                var visual = boss.transform.Find("ART_MeshyBossVisual");
                ApplyRegentRuntimeMaterial(visual);
                AlignRegentFeetToSupport(visual, boss.transform);
                AlignRegentWeakPointToChest(visual, boss);
                HideMisformedRegentCrackTell(boss);
                // This is a hold for the old floating crystal prop. The
                // animated Regent uses its own chest/crown silhouette; keeping
                // the orphan perch above it reads as a black spike through the
                // new character.
                var oldWeakPerch = boss.transform.Find("ART_ScrapStormWeakPerch");
                if (oldWeakPerch != null) oldWeakPerch.gameObject.SetActive(false);
            }
        }

        private static void AlignRegentFeetToSupport(Transform visualRoot, Transform bossRoot)
        {
            if (visualRoot == null || bossRoot == null)
            {
                return;
            }

            var renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            var visibleBottom = float.PositiveInfinity;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled)
                {
                    visibleBottom = Mathf.Min(visibleBottom, renderers[i].bounds.min.y);
                }
            }

            if (float.IsInfinity(visibleBottom))
            {
                return;
            }

            var supportY = bossRoot.position.y;
            var hits = Physics.RaycastAll(bossRoot.position + Vector3.up * 5f, Vector3.down,
                15f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var nearest = float.PositiveInfinity;
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.transform == bossRoot
                    || hit.collider.transform.IsChildOf(bossRoot))
                {
                    continue;
                }

                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    supportY = hit.point.y;
                }
            }

            var correction = supportY - visibleBottom;
            if (Mathf.Abs(correction) > 0.015f && Mathf.Abs(correction) < 2.5f)
            {
                visualRoot.position += Vector3.up * correction;
            }

            Debug.Log($"[Space & Crafts] Regent support alignment: supportY={supportY:F3}, visibleBottom={visibleBottom:F3}, offset={correction:F3}, applied={Mathf.Abs(correction) > 0.015f && Mathf.Abs(correction) < 2.5f}.");
        }

        private static void AlignRegentWeakPointToChest(Transform visualRoot, ScrapStormSentinel boss)
        {
            if (visualRoot == null || boss == null)
            {
                return;
            }

            var weakPoint = boss.GetComponentInChildren<BossWeakPoint>(true);
            if (weakPoint == null)
            {
                return;
            }

            var renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            var bounds = default(Bounds);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds || bounds.size.y < 0.5f)
            {
                return;
            }

            // The Regent has a large chibi head; a conventional 57% center
            // lands the crystal across its face. Place the target on the upper
            // torso so the exposed marker stays distinct without masking it.
            var chestHeight = bounds.min.y + bounds.size.y * 0.43f;
            var frontOffset = Mathf.Min(0.24f, bounds.extents.z * 0.12f);
            weakPoint.transform.position = new Vector3(bounds.center.x, chestHeight, bounds.center.z)
                + visualRoot.forward * frontOffset;
            weakPoint.transform.rotation = visualRoot.rotation;

            // The Chromatic Regent already has a visible chest ornament. Reuse
            // that as the target cue and keep the old oversized crystal trigger
            // invisible, while its collider and boss-hit event remain active.
            foreach (var renderer in weakPoint.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                }
            }

            EditorUtility.SetDirty(weakPoint.transform);
            Debug.Log($"[Space & Crafts] Regent weak-point trigger seated at chest: position={weakPoint.transform.position:F3}, visualHidden=true, collider={weakPoint.GetComponent<Collider>() != null}.");
        }

        private static void HideMisformedRegentCrackTell(ScrapStormSentinel boss)
        {
            if (boss == null)
            {
                return;
            }

            var descendants = boss.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < descendants.Length; i++)
            {
                var tell = descendants[i];
                if (tell == null || tell.name != "ART_ScrapStormCrackTell")
                {
                    continue;
                }

                // In the Arrival capture this GearRing becomes a 5.6 m orange
                // stripe through the boss's feet, while the separate danger
                // ring and impact disc already communicate the slam radius.
                tell.gameObject.SetActive(false);
                EditorUtility.SetDirty(tell.gameObject);
                Debug.Log("[Space & Crafts] Hid the misformed Regent crack telegraph stripe; the boss danger ring and impact disc remain active.");
                return;
            }
        }

        private static void ApplyRegentRuntimeMaterial(Transform visualRoot)
        {
            if (visualRoot == null)
            {
                return;
            }

            const string texturePath =
                "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1408/UnityCandidate/ChromaticRegent_BaseColor.jpg";
            const string materialPath =
                "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1408/UnityCandidate/ChromaticRegent_Rigged_URP.mat";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null || texture == null)
                {
                    Debug.LogWarning($"[Space & Crafts] Regent material not assigned: shader={shader != null}, albedo={texture != null}.");
                    return;
                }

                material = new Material(shader) { name = "ChromaticRegent_Rigged_URP" };
                material.SetTexture(material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex", texture);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.08f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.34f);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var assignedRenderers = 0;
            foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                if (slots.Length == 0) slots = new Material[1];
                for (var i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots;
                EditorUtility.SetDirty(renderer);
                assignedRenderers++;
            }
            Debug.Log($"[Space & Crafts] Regent UnityCandidate material: albedo={texture != null}; renderers={assignedRenderers}; material={material.name}.");
        }

        [MenuItem("Space & Crafts/Install Rigged Chromatic Regent On Arrival Boss (Iter1472)")]
        public static void InstallRiggedRegentOnArrivalBossIter1472()
        {
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            TrySwapBossVisual();
            var boss = Object.FindAnyObjectByType<ScrapStormSentinel>();
            var visual = boss != null ? boss.transform.Find("ART_MeshyBossVisual") : null;
            var animator = visual != null ? visual.GetComponentInChildren<Animator>(true) : null;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Iter1472 installed Rainbow boss visual: {visual != null}; animator={(animator != null)}; controller={(animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "none")}; avatar={(animator != null && animator.avatar != null && animator.avatar.isValid)}.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("Space & Crafts/Refresh Arrival Regent Scale and Targets (Iter1575)")]
        public static void RefreshArrivalRegentScaleAndTargetsIter1575()
        {
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ArrivalScene, OpenSceneMode.Single);
            TrySwapBossVisual();
            var boss = Object.FindAnyObjectByType<ScrapStormSentinel>();
            var visual = boss != null ? boss.transform.Find("ART_MeshyBossVisual") : null;
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Space & Crafts] Iter1575 enlarged Arrival Regent visual and recalculated chest/feet targets: visual={visual != null}; scale={(visual != null ? visual.localScale.ToString("F3") : "missing")}; sceneSaved={saved}.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(saved && visual != null ? 0 : 1);
            }
        }

        private static void TrySwapCrystalPickupVisual()
        {
            var model = LoadModel("Inventions", "PowerCrystalPickup");
            if (model == null)
            {
                return;
            }

            foreach (var pickup in Object.FindObjectsByType<CollectiblePickup>())
            {
                if (pickup == null || pickup.transform.root.name == "ART_MeshyCriticalShowcase")
                {
                    continue;
                }

                var so = new SerializedObject(pickup);
                var def = so.FindProperty("definition")?.objectReferenceValue as CollectibleDefinition;
                if (def == null || def.Id != "component_power_crystal")
                {
                    continue;
                }

                AttachMeshyChild(pickup.transform, model, "ART_MeshyCrystalVisual", 0.9f);
            }
        }
    }
}
#endif
