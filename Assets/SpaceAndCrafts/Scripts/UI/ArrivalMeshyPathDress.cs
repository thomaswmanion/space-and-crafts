using System.Collections.Generic;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using UnityEngine;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Player-visible craft fantasy on Arrival: clone EXISTING showcase Meshy onto the
    /// spawn→parts→Build Site path + OrganicMeshFactory silhouettes that mask flat slabs.
    /// No new Meshy generation. Showcase shelf stays; playable path gets the dress.
    /// Iteration 128 keeps the playable hero on the shared, original soft-blob design;
    /// commissioned Meshy props remain useful for scenery, but the old humanoid hero is retired.
    /// </summary>
    public sealed class ArrivalMeshyPathDress : MonoBehaviour
    {
        private static Texture2D _shelfSlateGrain;
        private Mesh _shelfSlatePlateMesh;
        private Material _shelfSlateMaterial;
        private Mesh _fjordLandformMesh;
        private Material _fjordLandTopMaterial;
        private Material _fjordLandShoulderMaterial;
        private Material _fjordLandCliffMaterial;
        private readonly List<Mesh> _runtimeUvMeshes = new();
        private readonly List<Mesh> _runtimeFjordSaddleMeshes = new();
        private readonly HashSet<Material> _runtimeOwnedMaterials = new();
        private static readonly float[] RockScaleVariants = { 0.86f, 1.12f, 0.96f, 1.18f, 0.82f, 1.04f };
        private static readonly float[] RockYawVariants = { 17f, 83f, 149f, 221f, 287f, 337f };
        private static readonly Color[] RockTintVariants =
        {
            new(0.62f, 0.72f, 0.79f, 1f),
            new(0.7f, 0.78f, 0.84f, 1f),
            new(0.58f, 0.68f, 0.76f, 1f),
            new(0.76f, 0.82f, 0.86f, 1f),
        };

        private static readonly string[] PropNames =
        {
            "ART_Meshy_RainbowCraftTree",
            "ART_Meshy_ScrapRockCluster",
            "ART_Meshy_RainbowGateway",
            "ART_Meshy_PowerCrystalPickup",
            "ART_Meshy_BoingOMatic",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Try(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode _)
        {
            Try(scene);
        }

        private static void Try(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || scene.name == null || !scene.name.Contains("Arrival"))
            {
                return;
            }

            if (Object.FindAnyObjectByType<ArrivalMeshyPathDress>() != null)
            {
                return;
            }

            var go = new GameObject("SYS_ArrivalMeshyPathDress");
            go.AddComponent<ArrivalMeshyPathDress>().Build();
        }

        private void Build()
        {
            HideTemporaryArtLabels();

            // Park the museum shelf off-path so first viewport reads playable craft dress, not a prop aisle.
            var showcase = GameObject.Find("ART_MeshyCriticalShowcase");
            if (showcase != null)
            {
                showcase.transform.position = new Vector3(80f, 0f, 40f);
                // This shelf is decorative. Its imported crystal scale can make a
                // pickup trigger span the spawn even after the shelf is parked off-path.
                // Preserve the models, but remove their interactions, physics, and
                // rendering: a few imported Showcase bounds still reached back into
                // the play camera from 80 m away and blocked the fjord panorama.
                StripGameplay(showcase);
                foreach (var renderer in showcase.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer != null) renderer.enabled = false;
                }
            }

            var splat = GameObject.Find("Splat_TEMP");
            var start = splat != null ? splat.transform.position : Vector3.zero;
            start.y = 0f;

            var end = start + Vector3.forward * 22f;
            var site = Object.FindAnyObjectByType<BuildSite>();
            if (site != null)
            {
                end = site.transform.position;
                end.y = 0f;
            }

            CollectiblePickup nearest = null;
            var best = float.MaxValue;
            foreach (var pickup in Object.FindObjectsByType<CollectiblePickup>())
            {
                if (pickup?.Definition == null || pickup.Definition.Category != CollectibleCategory.Component)
                {
                    continue;
                }

                if (pickup.transform.root.name.Contains("Showcase"))
                {
                    continue;
                }

                var d = (pickup.transform.position - start).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    nearest = pickup;
                }
            }

            var mid = nearest != null ? nearest.transform.position : Vector3.Lerp(start, end, 0.45f);
            mid.y = 0f;

            NormalizeSpurGrounds();
            UseOriginalSplatVisual();
            PlantMeshyCopies(start, mid, end);
            PlantGatewayLandmarks(start, mid, end);
            PlantDistantMeshyGatewayTease(start, end);
            ArrivalFjordBackdrop.Build(transform, start, end);
            DressMidPathIslands(start, mid, end);
            DressSpawnShelfFloor(start);
            SoftenFloatingPickupShadows();
            HideJunkSpheresAndWash(start, end);
            HideWorstGrayboxSlabs(start, end);
            BuildContinuousFjordLandform(start, mid, end);
            ToneArrivalExposure();
            // StyleAtmosphere / GrayboxDress may spawn after us — second pass next frame.
            StartCoroutine(HideAgainNextFrame(start, end));
        }

        /// <summary>
        /// Make the visible optional spur islands solid across their full soft-blob surface.
        /// The serialized BoxCollider was only 8 m wide while each rendered mesh was about 19 m.
        /// </summary>
        private static void NormalizeSpurGrounds()
        {
            foreach (var ground in Object.FindObjectsByType<Transform>())
            {
                if (ground == null || ground.name != "ART_SpurGround")
                {
                    continue;
                }

                var filter = ground.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                var scale = ground.localScale;
                if (scale.y < 1.2f)
                {
                    ground.localScale = new Vector3(scale.x, 1.25f, scale.z);
                }

                var meshCollider = ground.GetComponent<MeshCollider>();
                if (meshCollider == null)
                {
                    meshCollider = ground.gameObject.AddComponent<MeshCollider>();
                }
                if (meshCollider == null)
                {
                    continue;
                }
                meshCollider.convex = false;
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = filter.sharedMesh;
                foreach (var box in ground.GetComponents<BoxCollider>())
                {
                    box.enabled = false;
                }
            }
        }

        /// <summary>
        /// Iter15: ShelfFloor magenta slab must not own the frame — mute material + Meshy Rock dress.
        /// Collider stays; MeshRenderer kept but recolored so walkable ground still reads.
        /// </summary>
        private void DressSpawnShelfFloor(Vector3 start)
        {
            var shelf = GameObject.Find("ART_ShelfFloor");
            if (shelf != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                foreach (var rend in shelf.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (rend == null || shader == null)
                    {
                        continue;
                    }

                    // Muted craft slate — not magenta billboard.
                    var mat = OwnRuntimeMaterial(new Material(shader) { name = "Mat_ShelfFloor_CraftMute" });
                    // Reuse the same slate as the fjord cliffs so the playable shelf
                    // reads as part of the Nordic landscape instead of a bare gray slab.
                    // Keep the small procedural grain as a resource-missing fallback.
                    var shelfAlbedo = Resources.Load<Texture2D>("Environment/ArrivalNordicSlateAlbedoV4")
                        ?? Resources.Load<Texture2D>("Environment/ArrivalNordicSlateAlbedoV3")
                        ?? Resources.Load<Texture2D>("Environment/ArrivalNordicSlateAlbedoV2")
                        ?? Resources.Load<Texture2D>("Environment/ArrivalShelfSlatePainted");
                    // Keep the new hand-painted fjord slate consistent across the spawn shelf,
                    // side trails, route crest, and cliff backdrop; preserve the old shelf tile as fallback.
                    var c = shelfAlbedo != null
                        ? new Color(0.9f, 0.96f, 1f, 1f)
                        : new Color(0.58f, 0.69f, 0.75f, 1f);
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", c);
                    }
                    else
                    {
                        mat.color = c;
                    }

                    if (mat.HasProperty("_Smoothness"))
                    {
                        mat.SetFloat("_Smoothness", 0.12f);
                    }

                    if (mat.HasProperty("_Metallic"))
                    {
                        mat.SetFloat("_Metallic", 0.02f);
                    }

                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", Color.black);
                    }

                    var cliffAlbedo = Resources.Load<Texture2D>("Environment/ArrivalNordicSlateAlbedoV2")
                        ?? Resources.Load<Texture2D>("Environment/ArrivalSlateCliffAlbedo");
                    var grain = shelfAlbedo ?? cliffAlbedo ?? GetShelfSlateGrain();
                    if (shelfAlbedo != null)
                    {
                        // Mirroring joins opposing tile edges while keeping the broad flagstones intact.
                        shelfAlbedo.wrapMode = TextureWrapMode.Mirror;
                        shelfAlbedo.anisoLevel = 4;

                        // Organic slab meshes have no authored UV0. Project the slate
                        // across this renderer so the texture does not collapse to one texel.
                        ApplyPlanarSlateUv(rend, "ART_ShelfFloor_PaintedSlateUV");
                    }
                    // The cliff texture is already fractured at a fine scale. Fewer, larger
                    // repeats let the spawn shelf read as broad natural slate plates instead
                    // of a tiny tiled pattern; keep fallback grain denser because it is softer.
                    var textureScale = shelfAlbedo != null
                        ? new Vector2(2.5f, 2f)
                        : cliffAlbedo != null
                        ? new Vector2(4f, 3f)
                        : new Vector2(8f, 8f);
                    if (mat.HasProperty("_BaseMap"))
                    {
                        mat.SetTexture("_BaseMap", grain);
                        mat.SetTextureScale("_BaseMap", textureScale);
                    }
                    else if (mat.HasProperty("_MainTex"))
                    {
                        mat.SetTexture("_MainTex", grain);
                        mat.SetTextureScale("_MainTex", textureScale);
                    }

                    rend.sharedMaterial = mat;
                    _shelfSlateMaterial = mat;
                }

                if (shader != null)
                {
                    var floorRenderer = shelf.GetComponentInChildren<MeshRenderer>(true);
                    if (floorRenderer != null)
                    {
                        // The continuous fjord landform replaces these scattered top overlays.
                    }
                }
            }

            Transform rockSrc = null;
            Transform treeSrc = null;
            foreach (var name in new[] { "ART_Meshy_ScrapRockCluster", "ART_Meshy_RainbowCraftTree" })
            {
                var go = GameObject.Find(name);
                if (go == null || !IsUnderShowcase(go.transform))
                {
                    continue;
                }

                if (name.Contains("Rock"))
                {
                    rockSrc = go.transform;
                }
                else
                {
                    treeSrc = go.transform;
                }
            }

            if (rockSrc == null && treeSrc == null)
            {
                return;
            }

            // One off-center rock cluster gives the spawn shelf a natural edge cue.
            // The route already has a gateway and slate inlays; mirrored rubble piles
            // crowded the player's first view and made the floor feel like a prop dump.
            var pads = new[]
            {
                start + new Vector3(-6.2f, 0f, 2.8f),
            };

            for (var i = 0; i < pads.Length; i++)
            {
                // Keep the Norway-inspired first vista open; use low rock clusters here.
                var src = rockSrc ?? treeSrc;
                if (src == null)
                {
                    continue;
                }

                var clone = Object.Instantiate(src.gameObject);
                clone.name = $"ART_ShelfMeshy_{src.name}_{i}";
                clone.transform.SetParent(transform, false);
                StripGameplay(clone, enableRenderers: true);
                FitWorldSize(clone, src.name.Contains("Tree") ? 2.8f : 1.85f);
                var p = pads[i];
                p.y = GroundY(p);
                clone.transform.position = p;
                clone.transform.rotation = Quaternion.Euler(0f, 50f * i, 0f);
                if (src.name.Contains("Rock")) ApplyRockVariation(clone, i + 3);
                SitOnGround(clone);
                var life = clone.GetComponent<MeshyPropLife>() ?? clone.AddComponent<MeshyPropLife>();
                life.ConfigureForArrival(clone.transform);
            }
        }

        private static Texture2D GetShelfSlateGrain()
        {
            if (_shelfSlateGrain != null)
            {
                return _shelfSlateGrain;
            }

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, true)
            {
                name = "ART_ShelfSlate_ProceduralGrain",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 2,
                hideFlags = HideFlags.DontSave,
            };

            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                var v = y / (float)size;
                for (var x = 0; x < size; x++)
                {
                    var u = x / (float)size;
                    var broad = Mathf.Sin(2f * Mathf.PI * (2f * u + v)) * 0.035f
                              + Mathf.Cos(2f * Mathf.PI * (3f * u - 2f * v)) * 0.024f;
                    var middle = Mathf.Sin(2f * Mathf.PI * (11f * u + 7f * v)
                                           + 0.7f * Mathf.Sin(2f * Mathf.PI * 3f * v)) * 0.018f;
                    var fine = Mathf.Sin(2f * Mathf.PI * (29f * u + 17f * v)
                                         + 0.4f * Mathf.Cos(2f * Mathf.PI * 13f * u)) * 0.009f;
                    var value = Mathf.Clamp(0.9f + broad + middle + fine, 0.8f, 1f);
                    pixels[y * size + x] = new Color(value * 0.99f, value, value * 1.02f, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: true, makeNoLongerReadable: true);
            _shelfSlateGrain = texture;
            return texture;
        }

        private void ApplyPlanarSlateUv(MeshRenderer renderer, string meshName)
        {
            var filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null)
            {
                return;
            }

            if (filter.sharedMesh.name == meshName)
            {
                return;
            }

            var texturedMesh = Object.Instantiate(filter.sharedMesh);
            texturedMesh.name = meshName;
            texturedMesh.hideFlags = HideFlags.DontSave;
            var vertices = texturedMesh.vertices;
            var bounds = texturedMesh.bounds;
            var width = Mathf.Max(bounds.size.x, 0.0001f);
            var depth = Mathf.Max(bounds.size.z, 0.0001f);
            var uvs = new Vector2[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                uvs[i] = new Vector2(
                    (vertices[i].x - bounds.min.x) / width,
                    (vertices[i].z - bounds.min.z) / depth);
            }

            texturedMesh.uv = uvs;
            filter.sharedMesh = texturedMesh;
            _runtimeUvMeshes.Add(texturedMesh);
        }

        private void PlantShelfFloorInlays(Vector3 start, Shader shader, float floorY)
        {
            var outerMesh = CreateEllipseMesh(0.42f, 0.58f, "ShelfFloor_GlazeTile");
            var innerMesh = CreateEllipseMesh(0.19f, 0.32f, "ShelfFloor_GlazeCore");
            var outerMats = new[]
            {
                MakeFloorInlayMaterial(shader, "Mat_ShelfFloor_Glaze_Teal", new Color(0.24f, 0.51f, 0.6f, 1f)),
                MakeFloorInlayMaterial(shader, "Mat_ShelfFloor_Glaze_Blue", new Color(0.29f, 0.47f, 0.65f, 1f)),
                MakeFloorInlayMaterial(shader, "Mat_ShelfFloor_Glaze_Mint", new Color(0.28f, 0.57f, 0.57f, 1f)),
            };
            var innerMat = MakeFloorInlayMaterial(shader, "Mat_ShelfFloor_GlazeCore", new Color(0.5f, 0.7f, 0.73f, 1f));

            // A restrained, color-shifted handmade trail carries the gateway motif across the shelf.
            for (var i = 0; i < 9; i++)
            {
                var p = start + new Vector3(Mathf.Sin(i * 0.55f) * 0.24f, 0f, 2.2f + i * 1.35f);
                p.y = floorY + 0.015f;
                var rotation = Quaternion.Euler(0f, (i % 2 == 0 ? -7f : 7f), 0f);
                CreateFloorInlay($"ART_ShelfFloor_Glaze_{i}", outerMesh, outerMats[i % outerMats.Length], p, rotation);
                CreateFloorInlay($"ART_ShelfFloor_GlazeCore_{i}", innerMesh, innerMat,
                    p + Vector3.up * 0.004f, rotation);
            }
        }

        /// <summary>
        /// Breaks up the broad spawn shelf with low, faceted slate outcrops. These are
        /// render-only overlays: the continuous floor collider remains the sole surface.
        /// </summary>
        private void PlantShelfSlatePlates(Vector3 start, MeshRenderer floorRenderer)
        {
            if (floorRenderer == null || floorRenderer.sharedMaterial == null)
            {
                return;
            }

            _shelfSlatePlateMesh = CreateShelfSlatePlateMesh();
            var positions = new[]
            {
                (x: -6.25f, z: -5.05f, width: 2.15f, length: 1.45f, yaw: -13f, tint: new Color(0.68f, 0.77f, 0.82f, 1f)),
                (x: 6.1f, z: -5.2f, width: 1.9f, length: 1.35f, yaw: 16f, tint: new Color(0.53f, 0.65f, 0.71f, 1f)),
                (x: -6.55f, z: -2.25f, width: 1.75f, length: 1.3f, yaw: 9f, tint: new Color(0.62f, 0.72f, 0.78f, 1f)),
                (x: 6.35f, z: -1.65f, width: 2.05f, length: 1.25f, yaw: -18f, tint: new Color(0.7f, 0.78f, 0.82f, 1f)),
                (x: -6.15f, z: 1.0f, width: 1.8f, length: 1.2f, yaw: 17f, tint: new Color(0.56f, 0.67f, 0.73f, 1f)),
                (x: 6.45f, z: 1.7f, width: 2.0f, length: 1.45f, yaw: -9f, tint: new Color(0.64f, 0.73f, 0.79f, 1f)),
                (x: -6.0f, z: 4.55f, width: 2.2f, length: 1.25f, yaw: -7f, tint: new Color(0.7f, 0.78f, 0.82f, 1f)),
                (x: 6.25f, z: 4.8f, width: 1.85f, length: 1.3f, yaw: 14f, tint: new Color(0.58f, 0.68f, 0.76f, 1f)),
            };

            var top = floorRenderer.bounds.max.y + 0.012f;
            var tintId = Shader.PropertyToID("_BaseColor");
            var tintFallbackId = Shader.PropertyToID("_Color");
            var block = new MaterialPropertyBlock();
            for (var i = 0; i < positions.Length; i++)
            {
                var plate = positions[i];
                var go = new GameObject($"ART_ShelfSlatePlate_{i}");
                go.transform.SetParent(transform, false);
                go.transform.SetPositionAndRotation(
                    new Vector3(start.x + plate.x, top, start.z + plate.z),
                    Quaternion.Euler(0f, plate.yaw, 0f));
                go.transform.localScale = new Vector3(plate.width, 1f, plate.length);
                go.AddComponent<MeshFilter>().sharedMesh = _shelfSlatePlateMesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = floorRenderer.sharedMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                block.Clear();
                if (renderer.sharedMaterial.HasProperty(tintId))
                {
                    block.SetColor(tintId, plate.tint);
                }
                else if (renderer.sharedMaterial.HasProperty(tintFallbackId))
                {
                    block.SetColor(tintFallbackId, plate.tint);
                }

                renderer.SetPropertyBlock(block);
            }
        }

        private void OnDestroy()
        {
            if (_fjordLandformMesh != null)
            {
                if (Application.isPlaying) Destroy(_fjordLandformMesh);
                else DestroyImmediate(_fjordLandformMesh);
                _fjordLandformMesh = null;
            }
            if (_fjordLandTopMaterial != null)
            {
                if (Application.isPlaying) Destroy(_fjordLandTopMaterial);
                else DestroyImmediate(_fjordLandTopMaterial);
                _fjordLandTopMaterial = null;
            }
            if (_fjordLandShoulderMaterial != null)
            {
                if (Application.isPlaying) Destroy(_fjordLandShoulderMaterial);
                else DestroyImmediate(_fjordLandShoulderMaterial);
                _fjordLandShoulderMaterial = null;
            }
            if (_fjordLandCliffMaterial != null)
            {
                if (Application.isPlaying) Destroy(_fjordLandCliffMaterial);
                else DestroyImmediate(_fjordLandCliffMaterial);
                _fjordLandCliffMaterial = null;
            }

            if (_shelfSlatePlateMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_shelfSlatePlateMesh);
                }
                else
                {
                    DestroyImmediate(_shelfSlatePlateMesh);
                }

                _shelfSlatePlateMesh = null;
            }

            for (var i = 0; i < _runtimeUvMeshes.Count; i++)
            {
                var mesh = _runtimeUvMeshes[i];
                if (mesh == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(mesh);
                }
                else
                {
                    DestroyImmediate(mesh);
                }
            }

            _runtimeUvMeshes.Clear();
            for (var i = 0; i < _runtimeFjordSaddleMeshes.Count; i++)
            {
                var mesh = _runtimeFjordSaddleMeshes[i];
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }
            _runtimeFjordSaddleMeshes.Clear();

            foreach (var material in _runtimeOwnedMaterials)
            {
                if (material == null) continue;
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
            _runtimeOwnedMaterials.Clear();
        }

        private Material OwnRuntimeMaterial(Material material)
        {
            if (material != null) _runtimeOwnedMaterials.Add(material);
            return material;
        }

        private static Mesh CreateShelfSlatePlateMesh()
        {
            const int segments = 9;
            var vertices = new Vector3[1 + segments * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 9];
            vertices[0] = new Vector3(0f, 0.17f, 0f);
            uvs[0] = new Vector2(0.5f, 0.5f);
            for (var i = 0; i < segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var irregularity = Mathf.Sin(i * 1.73f) * 0.055f;
                var topRadius = 0.69f + (i % 2 == 0 ? 0.12f : -0.035f) + irregularity;
                var baseRadius = 0.92f + (i % 2 == 0 ? 0.045f : -0.025f) + irregularity;
                var topY = 0.075f + (i % 3) * 0.024f + Mathf.Cos(i * 2.1f) * 0.012f;
                var topIndex = i + 1;
                var baseIndex = segments + i + 1;
                vertices[topIndex] = new Vector3(direction.x * topRadius, topY, direction.y * topRadius);
                vertices[baseIndex] = new Vector3(direction.x * baseRadius, 0.006f, direction.y * baseRadius);
                uvs[topIndex] = new Vector2(0.5f + direction.x * topRadius * 0.5f,
                    0.5f + direction.y * topRadius * 0.5f);
                uvs[baseIndex] = new Vector2(0.5f + direction.x * baseRadius * 0.5f,
                    0.5f + direction.y * baseRadius * 0.5f);

                var nextTop = (i + 1) % segments + 1;
                var nextBase = (i + 1) % segments + segments + 1;
                var topTri = i * 9;
                triangles[topTri] = 0;
                triangles[topTri + 1] = nextTop;
                triangles[topTri + 2] = topIndex;
                triangles[topTri + 3] = topIndex;
                triangles[topTri + 4] = nextTop;
                triangles[topTri + 5] = baseIndex;
                triangles[topTri + 6] = nextTop;
                triangles[topTri + 7] = nextBase;
                triangles[topTri + 8] = baseIndex;
            }

            var mesh = new Mesh { name = "ART_ShelfSlatePlateMesh", hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Replaces square, disconnected graybox floor tops with a narrower winding fjord ridge.
        /// A unified rock material and uneven crown follow the gather route; sloped skirts descend
        /// into the fjord. Existing floor colliders and fall rescue remain gameplay safeguards.
        /// </summary>
        private void BuildContinuousFjordLandform(Vector3 start, Vector3 mid, Vector3 end)
        {
            var shelf = GameObject.Find("ART_ShelfFloor");
            var shelfRenderer = shelf != null ? shelf.GetComponentInChildren<MeshRenderer>(true) : null;
            if (shelfRenderer == null)
            {
                return;
            }

            const int longitudinalSteps = 144;
            const float topY = 0.22f;
            const float waterY = -6.4f;
            var lateral = new[] { -2.4f, -2.2f, -1.95f, -1.72f, -1.5f, -1.32f, -1.16f, -1.07f,
                -1.015f, -0.97f, -0.86f, -0.62f, -0.32f, 0f, 0.32f, 0.62f, 0.86f, 0.97f,
                1.015f, 1.07f, 1.16f, 1.32f, 1.5f, 1.72f, 1.95f, 2.2f, 2.4f };
            var vertices = new List<Vector3>((longitudinalSteps + 1) * lateral.Length);
            var uvs = new List<Vector2>((longitudinalSteps + 1) * lateral.Length);
            var topTriangles = new List<int>(longitudinalSteps * 12 * 6);
            var shoulderTriangles = new List<int>(longitudinalSteps * 4 * 6);
            var cliffTriangles = new List<int>(longitudinalSteps * 8 * 6);
            var originalStart = start;
            var axis = end - start;
            axis.y = 0f;
            if (axis.sqrMagnitude < 0.01f)
            {
                axis = Vector3.forward;
            }
            axis.Normalize();
            var across = Vector3.Cross(Vector3.up, axis).normalized;
            start -= axis * 7.5f;
            end += axis * 4.5f;
            var midT = Mathf.Clamp01((mid.z - start.z) / Mathf.Max(0.01f, end.z - start.z));
            var midDeviation = mid.x - Mathf.Lerp(start.x, end.x, midT);
            var midSine = Mathf.Max(0.35f, Mathf.Sin(Mathf.PI * midT));

            for (var i = 0; i <= longitudinalSteps; i++)
            {
                var t = i / (float)longitudinalSteps;
                var center = Vector3.Lerp(start, end, t);
                var routeWeave = Mathf.Sin(Mathf.PI * t) * Mathf.Sin(Mathf.PI * 2f * t) * 0.5f;
                center += across * (midDeviation * Mathf.Sin(Mathf.PI * t) / midSine * 0.96f
                    + routeWeave);
                var before = Mathf.Max(0f, t - 0.004f);
                var after = Mathf.Min(1f, t + 0.004f);
                var tangent = (RouteCenter(after) - RouteCenter(before)).normalized;
                if (tangent.sqrMagnitude < 0.01f)
                {
                    tangent = axis;
                }
                var rowAcross = Vector3.Cross(Vector3.up, tangent).normalized;
                var gapA = Mathf.Exp(-Mathf.Pow((center.z - (originalStart.z + 11.5f)) / 2.7f, 2f));
                var gapB = Mathf.Exp(-Mathf.Pow((center.z - (originalStart.z
                    + (end.z - start.z - 12f) * 0.705f)) / 1.9f, 2f));
                // Vary only the outer shoulder/cliff skirt so the safe crown stays
                // generous while the exposed fjord edge avoids a uniform cutout.
                var edgeRipple = Mathf.Sin(t * 31f + 0.4f) * 0.34f
                    + Mathf.Sin(t * 13f + 1.7f) * 0.24f
                    + Mathf.Sin(t * 59f + 0.9f) * 0.07f;
                // Keep the route generous at the two pinch points while showing more
                // fjord at ordinary stretches; the previous 7.6 m base read as a plaza.
                var halfWidth = Mathf.Clamp(3.15f - gapA * 0.45f - gapB * 0.35f
                    + Mathf.Sin(t * 17f) * 0.18f, 2.35f, 3.55f);
                var topNoise = Mathf.Sin(t * 11f + 0.5f) * 0.14f
                    + Mathf.Sin(t * 29f + 1.2f) * 0.052f + Mathf.Sin(t * 53f) * 0.022f;
                for (var j = 0; j < lateral.Length; j++)
                {
                    var c = lateral[j];
                    var absolute = Mathf.Abs(c);
                    var offset = Mathf.Sign(c) * (Mathf.Min(absolute, 1f) * halfWidth
                        + Mathf.Max(0f, absolute - 1f) * (3f + edgeRipple));
                    var point = center + rowAcross * offset;
                    if (absolute <= 1f)
                    {
                        var shoulder = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.82f, 1f, absolute));
                        var crown = (1f - absolute * absolute) * 0.07f;
                        point.y = topY + topNoise * (1f - absolute * 0.28f) + crown - shoulder * 0.2f;
                    }
                    else
                    {
                        var cliffT = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, 2.4f, absolute));
                        point.y = Mathf.Lerp(topY - 0.2f + topNoise * 0.7f, waterY - 0.3f, cliffT);
                        if (absolute > 1.08f)
                        {
                            var faceNoise = Mathf.Sin(t * 37f + j * 1.15f) * 0.085f
                                + Mathf.Sin(t * 71f + j * 2.4f) * 0.04f;
                            point.y += faceNoise * Mathf.Sin(cliffT * Mathf.PI);
                        }
                    }

                    vertices.Add(transform.InverseTransformPoint(point));
                    uvs.Add(new Vector2(point.x / 7f, point.z / 7f));
                }
            }

            var columns = lateral.Length;
            for (var i = 0; i < longitudinalSteps; i++)
            {
                for (var j = 0; j < columns - 1; j++)
                {
                    var a = i * columns + j;
                    var b = a + columns;
                    var c = b + 1;
                    var d = a + 1;
                    var middleC = (lateral[j] + lateral[j + 1]) * 0.5f;
                    var absLateral = Mathf.Abs(middleC);
                    // Give the path edge a broader slate shoulder. The walkable
                    // geometry/collider is unchanged; this material band bridges
                    // the lavender crown into the descending cliff face.
                    var destination = absLateral <= 0.70f
                        ? topTriangles
                        : absLateral <= 1.08f ? shoulderTriangles : cliffTriangles;
                    // Both lateral skirts use the same index winding. The row direction
                    // follows the route, while lateral indices always progress from left
                    // to right; reversing the left skirt made its normals face inward.
                    destination.Add(a); destination.Add(b); destination.Add(d);
                    destination.Add(b); destination.Add(c); destination.Add(d);
                }
            }

            _fjordLandformMesh = new Mesh { name = "ART_FjordLandformMesh" };
            _fjordLandformMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _fjordLandformMesh.SetVertices(vertices);
            _fjordLandformMesh.SetUVs(0, uvs);
            _fjordLandformMesh.subMeshCount = 3;
            _fjordLandformMesh.SetTriangles(topTriangles, 0);
            _fjordLandformMesh.SetTriangles(shoulderTriangles, 1);
            _fjordLandformMesh.SetTriangles(cliffTriangles, 2);
            _fjordLandformMesh.RecalculateNormals();
            _fjordLandformMesh.RecalculateBounds();

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                return;
            }
            var cliffTexture = Resources.Load<Texture2D>("Environment/ArrivalNordicSlateAlbedoV2")
                ?? Resources.Load<Texture2D>("Environment/ArrivalSlateCliffAlbedo");
            var topTexture = Resources.Load<Texture2D>("Environment/ArrivalNordicSlateAlbedoV4")
                ?? Resources.Load<Texture2D>("Environment/ArrivalNordicSlateAlbedoV3")
                ?? cliffTexture ?? Resources.Load<Texture2D>("Environment/ArrivalShelfSlatePainted");
            // Keep the fjord's slate grain and cool Rainbow Kingdom identity,
            // while preventing the broad route from reading as saturated magenta.
            // Iter1775's matched preview tested this material-only correction;
            // terrain silhouette and walkable collider are unchanged.
            _fjordLandTopMaterial = MakeLandformMaterial(shader, "Mat_FjordLandTop",
                topTexture, new Color(0.98f, 0.94f, 1.08f, 1f));
            _fjordLandShoulderMaterial = MakeLandformMaterial(shader, "Mat_FjordLandShoulder",
                topTexture, new Color(0.80f, 0.91f, 1.02f, 1f));
            _fjordLandCliffMaterial = MakeLandformMaterial(shader, "Mat_FjordLandCliff",
                cliffTexture, new Color(0.80f, 0.90f, 0.94f, 1f));

            var terrain = new GameObject("ART_FjordLandform");
            terrain.transform.SetParent(transform, false);
            var filter = terrain.AddComponent<MeshFilter>();
            filter.sharedMesh = _fjordLandformMesh;
            var renderer = terrain.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[]
            {
                _fjordLandTopMaterial, _fjordLandShoulderMaterial, _fjordLandCliffMaterial
            };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            var collider = terrain.AddComponent<MeshCollider>();
            collider.sharedMesh = _fjordLandformMesh;

            // The continuous fjord mesh now supplies both the walkable land surface and
            // the visible edge into the fjord. Hide and disable the legacy floor blocks;
            // leaving their BoxColliders active creates invisible walls across this route.
            foreach (var floorName in new[] { "ART_ShelfFloor", "ART_GladeFloor", "ART_WorkshopFloor",
                         "ART_PaintPool", "ART_NurseryA", "ART_NurseryB", "ART_NurseryC", "ART_VistaLedge" })
            {
                var floor = GameObject.Find(floorName);
                if (floor == null) continue;
                foreach (var floorRenderer in floor.GetComponentsInChildren<Renderer>(true))
                {
                    if (floorRenderer != null) floorRenderer.enabled = false;
                }
                foreach (var legacyCollider in floor.GetComponentsInChildren<BoxCollider>(true))
                {
                    if (legacyCollider != null) legacyCollider.enabled = false;
                }
            }
            var foundation = GameObject.Find("ART_FjordShelfFoundation");
            if (foundation != null) foundation.SetActive(false);
            foreach (var rendererToMute in Object.FindObjectsByType<MeshRenderer>())
            {
                var name = rendererToMute != null ? rendererToMute.gameObject.name : string.Empty;
                if (name.StartsWith("ART_ShelfSlatePlate_") || name.StartsWith("ART_ShelfFloorInlay_"))
                {
                    rendererToMute.enabled = false;
                }
            }

            BuildFjordSpurSaddles(RouteCenter, originalStart, start, end, axis, topY, waterY);
            PlantFjordRouteStitches(RouteCenter, topY);

            Vector3 RouteCenter(float routeT)
            {
                var p = Vector3.Lerp(start, end, routeT);
                var routeWeave = Mathf.Sin(Mathf.PI * routeT) * Mathf.Sin(Mathf.PI * 2f * routeT) * 0.5f;
                p += across * (midDeviation * Mathf.Sin(Mathf.PI * routeT) / midSine * 0.96f
                    + routeWeave);
                return p;
            }
        }

        /// <summary>
        /// A restrained glazed stitch follows the real ridge centerline from the
        /// arrival shelf to BuildSite. It breaks up the broad slate top and gives
        /// players a quiet forward cue without adding collision or HUD-like arrows.
        /// </summary>
        private void PlantFjordRouteStitches(System.Func<float, Vector3> routeCenter, float routeTopY)
        {
            if (routeCenter == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                return;
            }

            const int stitchCount = 18;
            var outerMesh = CreateEllipseMesh(0.13f, 0.32f, "FjordRouteStitchOuter");
            var coreMesh = CreateEllipseMesh(0.052f, 0.15f, "FjordRouteStitchCore");
            var outerInstances = new CombineInstance[stitchCount];
            var coreInstances = new CombineInstance[stitchCount];
            for (var i = 0; i < stitchCount; i++)
            {
                var spacing = i / (float)(stitchCount - 1);
                var t = Mathf.Lerp(0.035f, 0.965f, spacing);
                var beforeT = Mathf.Max(0f, t - 0.004f);
                var afterT = Mathf.Min(1f, t + 0.004f);
                var tangent = routeCenter(afterT) - routeCenter(beforeT);
                tangent.y = 0f;
                if (tangent.sqrMagnitude < 0.001f)
                {
                    tangent = Vector3.forward;
                }

                var point = routeCenter(t);
                var topNoise = Mathf.Sin(t * 11f + 0.5f) * 0.14f
                    + Mathf.Sin(t * 29f + 1.2f) * 0.052f + Mathf.Sin(t * 53f) * 0.022f;
                point.y = routeTopY + topNoise + 0.07f + 0.012f;
                var yaw = (i % 2 == 0 ? -9f : 9f);
                var size = 0.88f + Mathf.Sin(i * 1.71f) * 0.12f;
                var worldRotation = Quaternion.LookRotation(tangent.normalized, Vector3.up)
                    * Quaternion.Euler(0f, yaw, 0f);
                var matrix = transform.worldToLocalMatrix
                    * Matrix4x4.TRS(point, worldRotation, Vector3.one * size);
                outerInstances[i] = new CombineInstance { mesh = outerMesh, transform = matrix };
                coreInstances[i] = new CombineInstance { mesh = coreMesh, transform = matrix };
            }

            var combinedOuter = new Mesh { name = "ART_FjordRouteStitchMesh" };
            combinedOuter.CombineMeshes(outerInstances, mergeSubMeshes: true, useMatrices: true);
            combinedOuter.RecalculateBounds();
            var combinedCore = new Mesh { name = "ART_FjordRouteStitchCoreMesh" };
            combinedCore.CombineMeshes(coreInstances, mergeSubMeshes: true, useMatrices: true);
            combinedCore.RecalculateBounds();
            _runtimeFjordSaddleMeshes.Add(combinedOuter);
            _runtimeFjordSaddleMeshes.Add(combinedCore);

            var outerMaterial = MakeFloorInlayMaterial(shader, "Mat_FjordRouteStitch_Teal",
                new Color(0.30f, 0.58f, 0.64f, 1f));
            var coreMaterial = MakeFloorInlayMaterial(shader, "Mat_FjordRouteStitch_Honey",
                new Color(0.82f, 0.69f, 0.44f, 1f));
            var stitchRoot = new GameObject("ART_FjordRouteStitches");
            stitchRoot.transform.SetParent(transform, false);
            var outer = new GameObject("ART_FjordRouteStitch_Teal");
            outer.transform.SetParent(stitchRoot.transform, false);
            outer.AddComponent<MeshFilter>().sharedMesh = combinedOuter;
            var outerRenderer = outer.AddComponent<MeshRenderer>();
            outerRenderer.sharedMaterial = outerMaterial;
            outerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outerRenderer.receiveShadows = false;

            var core = new GameObject("ART_FjordRouteStitch_Honey");
            core.transform.SetParent(stitchRoot.transform, false);
            core.AddComponent<MeshFilter>().sharedMesh = combinedCore;
            var coreRenderer = core.AddComponent<MeshRenderer>();
            coreRenderer.sharedMaterial = coreMaterial;
            coreRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coreRenderer.receiveShadows = false;

            if (Application.isPlaying)
            {
                Destroy(outerMesh);
                Destroy(coreMesh);
            }
            else
            {
                DestroyImmediate(outerMesh);
                DestroyImmediate(coreMesh);
            }
        }

        /// <summary>
        /// Join each optional slate shelf to the main ridge with one tapered landform mesh.
        /// The top and collision share the same mesh; skirts descend into the fjord catch.
        /// </summary>
        private void BuildFjordSpurSaddles(System.Func<float, Vector3> routeCenter,
            Vector3 originalStart, Vector3 routeStart, Vector3 routeEnd, Vector3 routeAxis,
            float routeTopY, float waterY)
        {
            if (routeCenter == null || _fjordLandTopMaterial == null || _fjordLandCliffMaterial == null)
            {
                return;
            }

            var routeSpan = Vector3.Dot(routeEnd - routeStart, routeAxis);
            if (routeSpan <= 0.01f)
            {
                return;
            }

            var zones = new[] { "Zone_WestSpur", "Zone_EastSpur" };
            foreach (var zoneName in zones)
            {
                var zone = GameObject.Find(zoneName);
                var ground = zone != null ? zone.transform.Find("ART_SpurGround") : null;
                var groundRenderer = ground != null ? ground.GetComponent<Renderer>() : null;
                if (ground == null || groundRenderer == null)
                {
                    continue;
                }

                var bounds = groundRenderer.bounds;
                var routeT = Mathf.Clamp01(Vector3.Dot(ground.position - routeStart, routeAxis) / routeSpan);
                var center = routeCenter(routeT);
                center.y = routeTopY;
                var before = routeCenter(Mathf.Max(0f, routeT - 0.02f));
                var after = routeCenter(Mathf.Min(1f, routeT + 0.02f));
                var tangent = after - before;
                tangent.y = 0f;
                if (tangent.sqrMagnitude < 0.01f) tangent = routeAxis;
                tangent.Normalize();

                var toward = bounds.center - center;
                toward.y = 0f;
                toward -= tangent * Vector3.Dot(toward, tangent);
                if (toward.sqrMagnitude < 0.01f)
                {
                    toward = Vector3.Cross(Vector3.up, tangent);
                }
                toward.Normalize();

                var gapA = Mathf.Exp(-Mathf.Pow((center.z - (originalStart.z + 11.5f)) / 2.7f, 2f));
                var gapB = Mathf.Exp(-Mathf.Pow((center.z - (originalStart.z
                    + (routeEnd.z - routeStart.z - 12f) * 0.705f)) / 1.9f, 2f));
                var halfWidth = Mathf.Clamp(3.15f - gapA * 0.45f - gapB * 0.35f
                    + Mathf.Sin(routeT * 17f) * 0.18f, 2.35f, 3.55f);
                var topNoise = Mathf.Sin(routeT * 11f + 0.5f) * 0.14f
                    + Mathf.Sin(routeT * 29f + 1.2f) * 0.052f + Mathf.Sin(routeT * 53f) * 0.022f;
                var routeEdge = center + toward * (halfWidth - 0.12f);
                routeEdge.y = routeTopY + topNoise;
                var padEdge = bounds.ClosestPoint(new Vector3(routeEdge.x, bounds.center.y, routeEdge.z));
                padEdge.y = 0f;
                var branchDirection = padEdge - routeEdge;
                branchDirection.y = 0f;
                var gapLength = branchDirection.magnitude;
                if (gapLength < 0.45f || gapLength > 10f)
                {
                    continue;
                }

                branchDirection /= gapLength;
                var branchEnd = padEdge + branchDirection * 1.15f;
                branchEnd.y = Mathf.Max(routeTopY + topNoise, bounds.max.y + 0.025f);
                CreateFjordSaddleMesh(zoneName.Replace("Zone_", "") , routeEdge, branchEnd,
                    tangent, routeTopY + topNoise, branchEnd.y, waterY);
            }
        }

        private void CreateFjordSaddleMesh(string suffix, Vector3 start, Vector3 end,
            Vector3 routeTangent, float startY, float endY, float waterY)
        {
            var existing = transform.Find("ART_FjordSpurSaddle_" + suffix);
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            var direction = end - start;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.25f) return;
            direction.Normalize();
            var across = Vector3.Cross(Vector3.up, direction).normalized;
            const int longitudinalSteps = 20;
            const int columns = 5;
            var lateral = new[] { -1f, -0.5f, 0f, 0.5f, 1f };
            var vertices = new List<Vector3>((longitudinalSteps + 1) * (columns + 2));
            var uvs = new List<Vector2>((longitudinalSteps + 1) * (columns + 2));
            var topTriangles = new List<int>(longitudinalSteps * (columns - 1) * 6);
            var cliffTriangles = new List<int>(longitudinalSteps * 12);
            var skirtBottomY = waterY - 0.28f;

            for (var i = 0; i <= longitudinalSteps; i++)
            {
                var t = i / (float)longitudinalSteps;
                var eased = Mathf.SmoothStep(0f, 1f, t);
                var center = Vector3.Lerp(start, end, t)
                    + routeTangent * (Mathf.Sin(t * Mathf.PI) * 0.32f);
                var halfWidth = Mathf.Lerp(1.85f, 3.0f, eased)
                    + Mathf.Sin(t * Mathf.PI) * 0.12f;
                var rowTopY = Mathf.Lerp(startY, endY, eased) + Mathf.Sin(t * Mathf.PI) * 0.055f;
                for (var j = 0; j < columns; j++)
                {
                    var amount = lateral[j];
                    var point = center + across * (amount * halfWidth);
                    point.y = rowTopY + (1f - Mathf.Abs(amount)) * 0.045f
                        - Mathf.Abs(amount) * 0.06f + Mathf.Sin(t * 17f + j * 0.72f) * 0.012f;
                    vertices.Add(transform.InverseTransformPoint(point));
                    uvs.Add(new Vector2(point.x / 7f, point.z / 7f));
                }
            }

            var leftBottomStart = vertices.Count;
            for (var i = 0; i <= longitudinalSteps; i++)
            {
                var topIndex = i * columns;
                var left = transform.TransformPoint(vertices[topIndex]);
                left.y = skirtBottomY;
                vertices.Add(transform.InverseTransformPoint(left));
                uvs.Add(new Vector2(left.x / 7f, left.y / 7f));
            }
            var rightBottomStart = vertices.Count;
            for (var i = 0; i <= longitudinalSteps; i++)
            {
                var topIndex = i * columns + columns - 1;
                var right = transform.TransformPoint(vertices[topIndex]);
                right.y = skirtBottomY;
                vertices.Add(transform.InverseTransformPoint(right));
                uvs.Add(new Vector2(right.x / 7f, right.y / 7f));
            }

            for (var i = 0; i < longitudinalSteps; i++)
            {
                for (var j = 0; j < columns - 1; j++)
                {
                    var a = i * columns + j;
                    var b = a + columns;
                    var c = b + 1;
                    var d = a + 1;
                    topTriangles.Add(a); topTriangles.Add(b); topTriangles.Add(c);
                    topTriangles.Add(a); topTriangles.Add(c); topTriangles.Add(d);
                }

                var leftTop = i * columns;
                var leftTopNext = leftTop + columns;
                var leftBottom = leftBottomStart + i;
                var leftBottomNext = leftBottom + 1;
                cliffTriangles.Add(leftTop); cliffTriangles.Add(leftBottom); cliffTriangles.Add(leftTopNext);
                cliffTriangles.Add(leftTopNext); cliffTriangles.Add(leftBottom); cliffTriangles.Add(leftBottomNext);

                var rightTop = i * columns + columns - 1;
                var rightTopNext = rightTop + columns;
                var rightBottom = rightBottomStart + i;
                var rightBottomNext = rightBottom + 1;
                cliffTriangles.Add(rightTop); cliffTriangles.Add(rightTopNext); cliffTriangles.Add(rightBottom);
                cliffTriangles.Add(rightTopNext); cliffTriangles.Add(rightBottomNext); cliffTriangles.Add(rightBottom);
            }

            var mesh = new Mesh { name = "ART_FjordSpurSaddleMesh_" + suffix };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(topTriangles, 0);
            mesh.SetTriangles(cliffTriangles, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var saddle = new GameObject("ART_FjordSpurSaddle_" + suffix);
            saddle.transform.SetParent(transform, false);
            saddle.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = saddle.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { _fjordLandTopMaterial, _fjordLandCliffMaterial };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            saddle.AddComponent<MeshCollider>().sharedMesh = mesh;
            _runtimeFjordSaddleMeshes.Add(mesh);
        }

        private static Material MakeLandformMaterial(Shader shader, string name, Texture2D texture, Color tint)
        {
            var material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            else material.color = tint;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.01f);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                texture.anisoLevel = 4;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                else if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            }
            return material;
        }

        private Material MakeFloorInlayMaterial(Shader shader, string name, Color color)
        {
            var mat = OwnRuntimeMaterial(new Material(shader) { name = name });
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.18f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            return mat;
        }

        private void CreateFloorInlay(string name, Mesh mesh, Material material, Vector3 position, Quaternion rotation)
        {
            var tile = new GameObject(name);
            tile.transform.SetParent(transform, false);
            tile.transform.SetPositionAndRotation(position, rotation);
            tile.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = tile.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Mesh CreateEllipseMesh(float halfWidth, float halfLength, string name)
        {
            const int segments = 24;
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            for (var i = 0; i < segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * halfWidth, 0f, Mathf.Sin(angle) * halfLength);
                var t = i * 3;
                triangles[t] = 0;
                triangles[t + 1] = (i + 1) % segments + 1;
                triangles[t + 2] = i + 1;
            }

            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Hide far magenta/yellow junk spheres and emissive wash props that blow out Meshy.
        /// </summary>
        private void HideJunkSpheresAndWash(Vector3 start, Vector3 end)
        {
            foreach (var rend in Object.FindObjectsByType<MeshRenderer>())
            {
                if (rend == null)
                {
                    continue;
                }

                var go = rend.gameObject;
                if (IsCommissionedFjordLedge(go.transform))
                {
                    continue;
                }
                var n = go.name ?? "";
                if (n == "ART_SpurGround")
                {
                    if (_shelfSlateMaterial != null)
                    {
                        ApplyPlanarSlateUv(rend, "ART_SpurGround_PaintedSlateUV");
                        rend.sharedMaterial = _shelfSlateMaterial;
                        var block = new MaterialPropertyBlock();
                        rend.GetPropertyBlock(block);
                        if (_shelfSlateMaterial.HasProperty("_BaseColor"))
                        {
                            block.SetColor("_BaseColor", new Color(0.78f, 0.86f, 0.93f, 1f));
                        }
                        else
                        {
                            block.SetColor("_Color", new Color(0.78f, 0.86f, 0.93f, 1f));
                        }

                        rend.SetPropertyBlock(block);
                    }
                    else
                    {
                        var mat = OwnRuntimeMaterial(rend.material);
                        var mutedSlate = new Color(0.36f, 0.45f, 0.54f, 1f);
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mutedSlate);
                        else mat.color = mutedSlate;
                        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
                    }

                    continue;
                }

                if (n.Contains("CraftPartBeacon") && rend.bounds.size.magnitude > 20f)
                {
                    // A few Meshy pickup prefabs nest their beacon under an extreme parent
                    // transform. Disable only the pathological decorative copy; real level
                    // beacons stay small and readable.
                    rend.enabled = false;
                    continue;
                }

                if (n.StartsWith("ART_DistantMeshyGateway")
                    || n.StartsWith("ART_Meshy") || n.StartsWith("ART_Path") || n.StartsWith("ART_Island")
                    || n.StartsWith("ART_ShelfMeshy") || n.Contains("Splat") || n.Contains("Showcase")
                    || n.Contains("Pickup") || n.Contains("Enemy") || n.Contains("Crumb")
                    || n.Contains("Boing") || n.Contains("Spring") || n.Contains("BuildSite")
                    || n.Contains("GatewayLandmark") || go.GetComponentInParent<Camera>() != null)
                {
                    continue;
                }

                var b = rend.bounds;
                var nearPath = NearPathSegment(b.center, start, end, 32f)
                    || (b.center.z > start.z - 6f && b.center.z < end.z + 40f);
                if (!nearPath)
                {
                    continue;
                }

                // Named wash / junk (horizon ribbon + far rainbow beads were the yellow blowout).
                if (n.Contains("VistaStar") || n.Contains("GatewayBeacon") || n.Contains("PaintfallCloud")
                    || n.Contains("CosmicBackdrop") || n.Contains("SkyBanner") || n.Contains("ArrivalCraftIcon")
                    || n.Contains("HorizonRibbon") || n.Contains("AmbientHorizon") || n.Contains("RainbowBead")
                    || n.Contains("RainbowGateway_Primary") || n.Contains("RainbowGateway_Dormant")
                    || n.Contains("CuriositySpur") || n.Contains("NookStar"))
                {
                    rend.enabled = false;
                    continue;
                }

                // Large near-spherical junk (magenta spheres, yellow sun blobs).
                var size = b.size;
                var maxAxis = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
                var minAxis = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
                var spherical = maxAxis > 1.8f && minAxis > maxAxis * 0.55f;
                if (!spherical)
                {
                    continue;
                }

                // Don't kill floor slabs or tall thin props.
                if (size.y < 0.8f || size.y > maxAxis * 1.4f)
                {
                    continue;
                }

                var filter = go.GetComponent<MeshFilter>();
                var meshName = filter != null && filter.sharedMesh != null ? filter.sharedMesh.name : "";
                if (meshName.Contains("Sphere") || meshName.Contains("sphere") || spherical)
                {
                    // Far junk only — keep close gameplay pickups (already skipped by name).
                    if (b.center.z > start.z + 4f || Mathf.Abs(b.center.x - start.x) > 3.5f)
                    {
                        rend.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Floating collectibles should not paint large, moving silhouettes over the route.
        /// Their glow and motion remain visible; the player and landmarks retain their shadows.
        /// </summary>
        private static void SoftenFloatingPickupShadows()
        {
            foreach (var pickup in Object.FindObjectsByType<CollectiblePickup>())
            {
                if (pickup == null || IsUnderShowcase(pickup.transform))
                {
                    continue;
                }

                foreach (var renderer in pickup.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
        }

        /// <summary>
        /// Keep the motor and collider on Splat_TEMP and use the shared original mascot visual.
        /// The commissioned hero's realistic face and lanky proportions clash with the game.
        /// </summary>
        private void UseOriginalSplatVisual()
        {
            var splat = GameObject.Find("Splat_TEMP");
            if (splat == null)
            {
                return;
            }

            if (SplatMeshyVisualMode.IsEnabledOn(splat.transform))
            {
                // This dresser is also responsible for hiding the older static
                // commissioned hero. The explicit Meshy player mode opts out of
                // that destructive fallback while retaining the same motor root.
                SplatVisualRepair.Repair(splat.transform);
                return;
            }

            var importedVisual = splat.transform.Find("ART_MeshySplatVisual");
            if (importedVisual != null)
            {
                importedVisual.gameObject.SetActive(false);
                Object.Destroy(importedVisual.gameObject);
            }

            SplatVisualRepair.Repair(splat.transform);
        }

        private static bool MeshyReads(GameObject visual)
        {
            if (visual == null || !visual.activeInHierarchy)
            {
                return false;
            }

            foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null && r.enabled)
                {
                    return true;
                }
            }

            return false;
        }

        private static void HideSoftBlob(Transform splat)
        {
            var organic = splat.Find(OrganicSplatBuilder.VisualRootName);
            if (organic == null)
            {
                return;
            }

            foreach (var r in organic.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null)
                {
                    r.enabled = false;
                }
            }
        }

        private System.Collections.IEnumerator HideAgainNextFrame(Vector3 start, Vector3 end)
        {
            yield return null;
            // Late SplatVisualRepair / style dressers may reappear after first Build.
            HideTemporaryArtLabels();
            var splat = GameObject.Find("Splat_TEMP");
            if (splat != null)
            {
                var meshy = splat.transform.Find("ART_MeshySplatVisual");
                if (meshy != null && MeshyReads(meshy.gameObject))
                {
                    HideSoftBlob(splat.transform);
                }
            }

            HideJunkSpheresAndWash(start, end);
            HideWorstGrayboxSlabs(start, end);
            ToneArrivalExposure();
            var cosmic = GameObject.Find("ART_CosmicBackdrop");
            if (cosmic != null)
            {
                foreach (var r in cosmic.GetComponentsInChildren<Renderer>(true))
                {
                    if (r != null)
                    {
                        r.enabled = false;
                    }
                }
            }

            var banner = GameObject.Find("ART_ArrivalSkyBanner");
            if (banner != null)
            {
                foreach (var r in banner.GetComponentsInChildren<Renderer>(true))
                {
                    if (r != null)
                    {
                        r.enabled = false;
                    }
                }
            }

            // Second settle — catch deferred organic arch / clay spawns.
            yield return null;
            if (splat != null)
            {
                var meshy = splat.transform.Find("ART_MeshySplatVisual");
                if (meshy != null && MeshyReads(meshy.gameObject))
                {
                    HideSoftBlob(splat.transform);
                }
            }

            HideJunkSpheresAndWash(start, end);
            HideWorstGrayboxSlabs(start, end);
            ToneArrivalExposure();
        }

        private static void HideTemporaryArtLabels()
        {
            // Graybox annotations are authoring aids, not player-facing guidance.
            // ArrivalObjectiveCoach and interaction prompts carry the useful instructions.
            foreach (var banner in Object.FindObjectsByType<TempArtBanner>(FindObjectsInactive.Include))
            {
                if (banner != null)
                {
                    banner.gameObject.SetActive(false);
                }
            }
        }

        private void PlantMeshyCopies(Vector3 start, Vector3 mid, Vector3 end)
        {
            var sources = new System.Collections.Generic.List<Transform>();
            foreach (var name in PropNames)
            {
                var go = GameObject.Find(name);
                if (go != null && IsUnderShowcase(go.transform))
                {
                    sources.Add(go.transform);
                }
            }

            if (sources.Count == 0)
            {
                // Fallback: any Meshy prop in scene (still no new generation).
                foreach (var t in Object.FindObjectsByType<Transform>())
                {
                    if (t == null || t.parent == null)
                    {
                        continue;
                    }

                    var n = t.name;
                    if (n != null && n.StartsWith("ART_Meshy_") && !n.Contains("Splat_Hero")
                        && (n.Contains("Tree") || n.Contains("Rock") || n.Contains("Gateway")
                            || n.Contains("Crystal") || n.Contains("Boing")))
                    {
                        sources.Add(t);
                    }
                }
            }

            if (sources.Count == 0)
            {
                return;
            }

            // Use one authored environmental landmark beyond the opening. Four repeated
            // shoulder clones made the route look like scattered rubble instead of a place.

            // Prefer non-gateway sources for shoulder dress — gateways become path landmarks.
            var shoulder = new System.Collections.Generic.List<Transform>();
            foreach (var s in sources)
            {
                if (s != null && s.name != null && !s.name.Contains("Gateway"))
                {
                    shoulder.Add(s);
                }
            }

            if (shoulder.Count == 0)
            {
                shoulder.AddRange(sources);
            }

            var rockSource = shoulder.Find(source => source != null && source.name.Contains("Rock"));
            var treeSource = shoulder.Find(source => source != null && source.name.Contains("Tree"));
            var src = treeSource ?? rockSource ?? shoulder[0];
            var along = (end - start).normalized;
            var right = Vector3.Cross(Vector3.up, along).normalized;
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;
            var clone = Object.Instantiate(src.gameObject);
            clone.name = $"ART_PathLandmark_{src.name}";
            clone.transform.SetParent(transform, false);
            StripGameplay(clone, enableRenderers: true);
            FitWorldSize(clone, src.name.Contains("Tree") ? 3.8f : 1.9f);

            var p = Vector3.Lerp(mid, end, 0.6f) + right * 6.2f;
            p.y = GroundY(p);
            clone.transform.position = p;
            clone.transform.rotation = Quaternion.LookRotation(along, Vector3.up);
            if (src.name.Contains("Rock")) ApplyRockVariation(clone, 1);
            SitOnGround(clone);

            // Personality via MeshyPropLife only (no second transform driver).
            var life = clone.GetComponent<MeshyPropLife>() ?? clone.AddComponent<MeshyPropLife>();
            life.ConfigureForArrival(clone.transform);
            if (clone.GetComponent<MeshyIdleAnim>() == null)
            {
                clone.AddComponent<MeshyIdleAnim>();
            }
        }

        /// <summary>
        /// Walk-through RainbowGateway landmarks ON the path — own the first viewport (no pink arch competition).
        /// Colliders stripped so the player can walk through.
        /// </summary>
        private void PlantGatewayLandmarks(Vector3 start, Vector3 mid, Vector3 end)
        {
            Transform src = null;
            var named = GameObject.Find("ART_Meshy_RainbowGateway");
            if (named != null && IsUnderShowcase(named.transform))
            {
                src = named.transform;
            }

            if (src == null)
            {
                foreach (var t in Object.FindObjectsByType<Transform>())
                {
                    if (t != null && t.name != null && t.name.Contains("RainbowGateway")
                        && t.name.StartsWith("ART_Meshy_"))
                    {
                        src = t;
                        break;
                    }
                }
            }

            if (src == null)
            {
                return;
            }

            var along = (end - start).normalized;
            if (along.sqrMagnitude < 0.01f)
            {
                along = Vector3.forward;
            }

            // Keep one smaller gateway marker beyond the first route section. Repeating
            // the same large arch twice near the entrance stacks silhouettes and hides
            // the destination; the scene's authored gateway remains the primary landmark.
            var landmarkSlots = new[]
            {
                (pos: Vector3.Lerp(mid, end, 0.62f), size: 6.2f),
            };

            for (var i = 0; i < landmarkSlots.Length; i++)
            {
                var slot = landmarkSlots[i];
                var clone = Object.Instantiate(src.gameObject);
                clone.name = $"ART_PathGatewayLandmark_{i}";
                clone.transform.SetParent(transform, false);
                StripGameplay(clone, enableRenderers: true);
                FitWorldSize(clone, slot.size);

                var p = slot.pos;
                p.y = GroundY(p);
                clone.transform.position = p;
                // Opening faces along travel so Splat walks through the portal.
                clone.transform.rotation = Quaternion.LookRotation(along, Vector3.up);
                SitOnGround(clone);
                MuteMeshyBlowout(clone);

                if (clone.GetComponent<MeshyIdleAnim>() == null)
                {
                    clone.AddComponent<MeshyIdleAnim>();
                }
            }

            // The fjord panorama and enclosing ridges now provide the opening view.
            // A former 54×30 m flat blue Quad hid the distant clutter through the arch,
            // but from the boss approach it covered most of the sky and broke the vista.
        }

        /// <summary>
        /// Replace the distant graybox portal cluster with copies of the commissioned
        /// Rainbow Gateway asset. Keep its three progression slots and hand those
        /// renderers to the post-boss reveal presenter.
        /// </summary>
        private void PlantDistantMeshyGatewayTease(Vector3 start, Vector3 end)
        {
            var revealRoot = GameObject.Find("TEMP_ART_MultiGatewayReveal");
            if (revealRoot == null) return;

            var showcaseGateway = GameObject.Find("ART_Meshy_RainbowGateway");
            if (showcaseGateway == null || !IsUnderShowcase(showcaseGateway.transform)) return;

            var slotNames = new[]
            {
                "TEMP_ART_RainbowGateway_DormantA",
                "TEMP_ART_RainbowGateway_DormantB",
                "TEMP_ART_RainbowGateway_DormantC",
            };
            var fallbackOffsets = new[]
            {
                new Vector3(-14f, 0f, 0f),
                new Vector3(14f, 2f, 4f),
                new Vector3(0f, 5f, 10f),
            };
            // These are skyline landmarks viewed across the fjord from the boss
            // terrace. The smaller graybox scale read as narrow slivers at that
            // distance, so give the commissioned arches a clear portal silhouette.
            // Keep each destination arch readable without letting the three
            // overlapping silhouettes swallow Splat on the victory view.
            var targetSizes = new[] { 5f, 5.6f, 6.2f };
            var along = (end - start).normalized;
            if (along.sqrMagnitude < 0.01f) along = Vector3.forward;
            var revealRenderers = new List<Renderer>(24);

            for (var i = 0; i < slotNames.Length; i++)
            {
                var anchor = revealRoot.transform.Find(slotNames[i]);
                var targetPosition = anchor != null
                    ? anchor.position : revealRoot.transform.TransformPoint(fallbackOffsets[i]);
                if (anchor != null) anchor.gameObject.SetActive(false);
                var clone = Object.Instantiate(showcaseGateway);
                clone.name = $"ART_DistantMeshyGateway_{i}";
                clone.transform.SetParent(transform, false);
                StripGameplay(clone, enableRenderers: true);
                FitWorldSize(clone, targetSizes[i]);
                clone.transform.SetPositionAndRotation(targetPosition,
                    Quaternion.LookRotation(along, Vector3.up));
                // FitWorldSize seats the source at y=0, but setting the anchor
                // pose afterward cancels that offset. Re-seat against the real
                // terrace surface so the arch feet do not disappear below it.
                var gatewayGroundY = GroundY(targetPosition);
                SitOnGroundAtY(clone, gatewayGroundY);
                MuteMeshyBlowout(clone);

                foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null) continue;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    revealRenderers.Add(renderer);
                }

                var renderers = clone.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    for (var r = 1; r < renderers.Length; r++)
                    {
                        if (renderers[r] != null) bounds.Encapsulate(renderers[r].bounds);
                    }

                    var camera = Camera.main;
                    var viewport = camera != null ? camera.WorldToViewportPoint(bounds.center) : Vector3.zero;
                    Debug.Log($"[Space & Crafts] Distant gateway {i}: center={bounds.center:F2}, size={bounds.size:F2}, groundY={gatewayGroundY:F2}, bottomY={bounds.min.y:F2}, viewport={viewport:F2}, camera={(camera != null ? camera.transform.position.ToString("F2") : "missing")}.");
                }
            }

            // Keep the empty marker root active for GatewayCompleteVista's focus,
            // while the old child assemblies stay inactive behind the Meshy models.
            var reveal = Object.FindAnyObjectByType<GatewayRevealPresenter>();
            if (reveal != null) reveal.SetDormantGatewayRenderers(revealRenderers.ToArray());
            Debug.Log($"[Space & Crafts] Replaced three distant graybox arches with commissioned Rainbow Gateway copies; revealRenderers={revealRenderers.Count}, revealPresenter={(reveal != null)}.");
        }

        private void MuteMeshyBlowout(GameObject root)
        {
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                if (rend == null)
                {
                    continue;
                }

                var mats = rend.materials;
                for (var i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null)
                    {
                        continue;
                    }

                    OwnRuntimeMaterial(mat);

                    if (mat.HasProperty("_Smoothness"))
                    {
                        mat.SetFloat("_Smoothness", Mathf.Min(mat.GetFloat("_Smoothness"), 0.28f));
                    }

                    if (mat.HasProperty("_Metallic"))
                    {
                        mat.SetFloat("_Metallic", Mathf.Min(mat.GetFloat("_Metallic"), 0.15f));
                    }

                    if (mat.HasProperty("_EmissionColor"))
                    {
                        var e = mat.GetColor("_EmissionColor");
                        // Kill yellow/white emission that washes the first viewport.
                        if (e.maxColorComponent > 0.35f)
                        {
                            mat.SetColor("_EmissionColor", e * 0.12f);
                        }
                    }

                    if (mat.HasProperty("_BaseColor"))
                    {
                        var c = mat.GetColor("_BaseColor");
                        if (c.r > 0.85f && c.g > 0.75f && c.b < 0.45f)
                        {
                            mat.SetColor("_BaseColor", Color.Lerp(c, new Color(0.55f, 0.5f, 0.45f), 0.55f));
                        }
                    }
                }

                rend.materials = mats;
            }
        }

        /// <summary>
        /// Hide mid-path graybox island renderers and plant existing Tree/Rock Meshy on them.
        /// </summary>
        private void DressMidPathIslands(Vector3 start, Vector3 mid, Vector3 end)
        {
            Transform treeSrc = null;
            Transform rockSrc = null;
            foreach (var name in new[] { "ART_Meshy_RainbowCraftTree", "ART_Meshy_ScrapRockCluster" })
            {
                var go = GameObject.Find(name);
                if (go == null || !IsUnderShowcase(go.transform))
                {
                    continue;
                }

                if (name.Contains("Tree"))
                {
                    treeSrc = go.transform;
                }
                else
                {
                    rockSrc = go.transform;
                }
            }

            if (treeSrc == null && rockSrc == null)
            {
                return;
            }

            var islandNames = new[]
            {
                "ART_GladeFloor", "ART_WorkshopFloor", "ART_PaintPool",
                "ART_NurseryA", "ART_NurseryB", "ART_NurseryC", "ART_VistaLedge",
            };

            var planted = 0;
            var routeDirection = (end - start).normalized;
            var routeRight = Vector3.Cross(Vector3.up, routeDirection).normalized;
            if (routeRight.sqrMagnitude < 0.01f)
            {
                routeRight = Vector3.right;
            }

            foreach (var islandName in islandNames)
            {
                var island = GameObject.Find(islandName);
                if (island == null)
                {
                    continue;
                }

                foreach (var r in island.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r != null && !r.name.StartsWith("ART_Meshy") && !r.name.StartsWith("ART_Path"))
                    {
                        r.enabled = false;
                    }
                }

                var c = island.transform.position;
                c.y = GroundY(c);
                // Preserve the first gateway sightline; bring trees in only after the
                // arrival shelf and opening vista, with rocks framing the near platforms.
                var isNearOpening = Mathf.Abs(c.z - start.z) < 55f;
                var pick = (!isNearOpening && planted % 2 == 0 ? treeSrc : rockSrc)
                    ?? treeSrc ?? rockSrc;
                if (pick == null)
                {
                    continue;
                }

                var clone = Object.Instantiate(pick.gameObject);
                clone.name = $"ART_IslandMeshy_{pick.name}_{islandName}";
                clone.transform.SetParent(transform, false);
                StripGameplay(clone, enableRenderers: true);
                FitWorldSize(clone, pick.name.Contains("Tree") ? 2.8f : 2.5f);
                // Keep each decorative cluster outside the central running lane. Offset
                // from the nearest route point so a floor already left/right of the path
                // is pushed outward instead of sometimes being pushed back into the lane.
                var routeSpan = end - start;
                var routeLengthSquared = routeSpan.sqrMagnitude;
                var nearestRoutePoint = routeLengthSquared > 0.01f
                    ? start + routeSpan * Mathf.Clamp01(Vector3.Dot(c - start, routeSpan) / routeLengthSquared)
                    : start;
                var outward = c - nearestRoutePoint;
                outward.y = 0f;
                var lateralDistance = outward.magnitude;
                if (lateralDistance < 0.1f)
                {
                    outward = routeRight * (planted % 2 == 0 ? -1f : 1f);
                    lateralDistance = 0f;
                }
                else
                {
                    outward /= lateralDistance;
                }

                var p = c + outward * Mathf.Max(0f, 5.5f - lateralDistance) + routeDirection * 0.15f;
                p.y = GroundY(p);
                clone.transform.position = p;
                clone.transform.rotation = Quaternion.Euler(0f, 40f * planted, 0f);
                if (pick.name.Contains("Rock")) ApplyRockVariation(clone, planted + 2);
                SitOnGround(clone);
                foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                var life = clone.GetComponent<MeshyPropLife>() ?? clone.AddComponent<MeshyPropLife>();
                life.ConfigureForArrival(clone.transform);
                if (clone.GetComponent<MeshyIdleAnim>() == null)
                {
                    clone.AddComponent<MeshyIdleAnim>();
                }

                planted++;
            }

            // Extra Tree/Rock pair at mid-path shoulders if islands were sparse.
            if (planted < 2 && treeSrc != null)
            {
                var along = (end - start).normalized;
                var right = Vector3.Cross(Vector3.up, along).normalized;
                for (var i = 0; i < 4; i++)
                {
                    var src = i % 2 == 0 ? treeSrc : (rockSrc ?? treeSrc);
                    var clone = Object.Instantiate(src.gameObject);
                    clone.name = $"ART_IslandMeshy_Fill_{i}";
                    clone.transform.SetParent(transform, false);
                    StripGameplay(clone, enableRenderers: true);
                    FitWorldSize(clone, src.name.Contains("Tree") ? 3.4f : 2.5f);
                    if (src.name.Contains("Rock"))
                    {
                        clone.transform.rotation = Quaternion.Euler(0f, 37f * i, 0f);
                        ApplyRockVariation(clone, i + 5);
                    }

                    var p = Vector3.Lerp(mid, end, 0.2f + i * 0.15f) + right * ((i % 2 == 0 ? -1f : 1f) * 3.8f);
                    p.y = GroundY(p);
                    clone.transform.position = p;
                    SitOnGround(clone);
                }
            }
        }

        /// <summary>
        /// Kill the worst pastel cube pixels on spawn→Build Site: hide tall walls / vista rails /
        /// craft lips / sky banner so Meshy + arches own the frame. Keep walkable floors.
        /// </summary>
        private void HideWorstGrayboxSlabs(Vector3 start, Vector3 end)
        {
            foreach (var rend in Object.FindObjectsByType<MeshRenderer>())
            {
                if (rend == null)
                {
                    continue;
                }

                var go = rend.gameObject;
                if (IsCommissionedFjordLedge(go.transform))
                {
                    continue;
                }
                var n = go.name ?? "";
                if (n.StartsWith("ART_CraftLip") || n.StartsWith("ART_ArrivalSkyBanner")
                    || n.StartsWith("ART_ArrivalRainbowRunway") || n.StartsWith("ART_ClayBlob")
                    || n.Contains("GatewayArchTube"))
                {
                    rend.enabled = false;
                    continue;
                }

                if (n.StartsWith("ART_DistantMeshyGateway")
                    || n.StartsWith("ART_Meshy") || n.StartsWith("ART_Path") || n.StartsWith("ART_Island")
                    || n.StartsWith("ART_Fjord")
                    || n.StartsWith("ART_ArrivalAurora")
                    || n.Contains("Splat")
                    || n.Contains("GatewaySkyPlug")
                    || n.Contains("Showcase") || n.Contains("Floor") || n.Contains("Pickup")
                    || n.Contains("Enemy") || n.Contains("Crumb") || n.Contains("Beacon")
                    || n.Contains("Boing") || n.Contains("Spring") || n.Contains("BuildSite")
                    || n.Contains("WarnHalo") || n.Contains("CraftSilhouette") || n.Contains("PathArch")
                    || n.Contains("GatewayLandmark") || n.Contains("Title")
                    || go.GetComponentInParent<Camera>() != null)
                {
                    continue;
                }

                var b = rend.bounds;
                // First-viewport + path corridor: kill huge pastel masses that steal the frame.
                var inFrontOfSpawn = b.center.z > start.z - 4f && b.center.z < end.z + 28f
                    && Mathf.Abs(b.center.x - start.x) < 22f;
                if (!inFrontOfSpawn && !NearPathSegment(b.center, start, end, 18f))
                {
                    continue;
                }

                var size = b.size;
                var maxAxis = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
                var isTallWall = size.y > 2.2f && Mathf.Min(size.x, size.z) < 2.6f;
                var isHugePastel = maxAxis > 8f && size.y > 1.6f && size.y < maxAxis * 0.85f;
                var namedBad = n.Contains("VistaRail") || n.Contains("ArchLeft") || n.Contains("ArchRight")
                    || n.Contains("ArchTop") || n.Contains("Nursery") || n.Contains("VistaLedge")
                    || n.Contains("VistaStar") || n.Contains("CosmicBackdrop") || n.Contains("ArenaRing")
                    || n.Contains("GatewayBeacon") || n.Contains("GulchFar") || n.Contains("GulchNear")
                    || n.Contains("RainbowGateway_Primary") || n.Contains("RainbowGateway_Dormant")
                    || n.Contains("GatewayArch") || n.Contains("Prop_Clay");
                if (isTallWall || isHugePastel || namedBad)
                {
                    rend.enabled = false;
                }
            }
        }

        private static bool NearPathSegment(Vector3 point, Vector3 start, Vector3 end, float radius)
        {
            var ab = end - start;
            var len2 = ab.sqrMagnitude;
            if (len2 < 0.01f)
            {
                return (point - start).sqrMagnitude <= radius * radius;
            }

            var t = Mathf.Clamp01(Vector3.Dot(point - start, ab) / len2);
            var closest = start + ab * t;
            closest.y = point.y;
            return (point - closest).sqrMagnitude <= radius * radius;
        }

        private static bool IsCommissionedFjordLedge(Transform t)
        {
            while (t != null)
            {
                if (t.name.StartsWith("ART_FjordCliffLedge_")
                    || t.name == "ART_FjordLedgeModules_Iter2148")
                {
                    return true;
                }
                t = t.parent;
            }
            return false;
        }

        private static void ToneArrivalExposure()
        {
            // Iter15: kill yellow wash that blows Meshy albedo.
            RenderSettings.ambientIntensity = 0.55f;
            RenderSettings.ambientLight = new Color(0.42f, 0.48f, 0.6f);
            RenderSettings.ambientSkyColor = new Color(0.4f, 0.55f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.5f, 0.57f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.3f, 0.36f);
            RenderSettings.reflectionIntensity = 0.25f;
            RenderSettings.subtractiveShadowColor = new Color(0.2f, 0.22f, 0.28f);

            if (Camera.main != null)
            {
                Camera.main.backgroundColor = new Color(0.28f, 0.48f, 0.72f);
                Camera.main.allowHDR = false;
            }

            foreach (var light in Object.FindObjectsByType<Light>())
            {
                if (light == null)
                {
                    continue;
                }

                var n = light.gameObject.name ?? "";
                // Cap warm/yellow lights hard — they were the yellow wash.
                var c = light.color;
                if (c.r > 0.85f && c.g > 0.7f && c.b < 0.55f)
                {
                    light.color = new Color(0.85f, 0.88f, 1f);
                    light.intensity = Mathf.Min(light.intensity, 0.25f);
                }

                if (n.Contains("VistaFill") || n.Contains("VistaStar"))
                {
                    light.intensity = Mathf.Min(light.intensity, 0.08f);
                    light.enabled = false;
                }
                else if (n.Contains("KeyLight") || n.Contains("ZoneLighting") || light.type == LightType.Directional)
                {
                    light.intensity = Mathf.Min(light.intensity, 0.85f);
                    if (light.color.r > light.color.b)
                    {
                        light.color = Color.Lerp(light.color, new Color(0.9f, 0.92f, 1f), 0.55f);
                    }
                }
                else if (n.Contains("SplatRim") || n.Contains("Face"))
                {
                    light.intensity = Mathf.Min(light.intensity, 0.85f);
                }
                else if (light.type == LightType.Point || light.type == LightType.Spot)
                {
                    light.intensity = Mathf.Min(light.intensity, 0.9f);
                }
            }
        }

        private static bool IsUnderShowcase(Transform t)
        {
            while (t != null)
            {
                var n = t.name;
                if (n != null && (n.Contains("Showcase") || n.Contains("MeshyCritical")))
                {
                    return true;
                }

                t = t.parent;
            }

            return false;
        }

        private static void StripGameplay(GameObject go, bool enableRenderers = false)
        {
            // The showcase source is deliberately hidden before its art is cloned
            // onto the Arrival route. Unity copies Renderer.enabled from that hidden
            // instance, so explicitly restore visual clones while keeping the
            // showcase itself hidden.
            if (enableRenderers)
            {
                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer != null) renderer.enabled = true;
                }
            }

            // Pickup art may carry a gameplay beacon child. On Meshy crystal clones its
            // transformed bounds can span hundreds of units and paint the spawn sky yellow.
            foreach (var child in go.GetComponentsInChildren<Transform>(true))
            {
                if (child != go.transform && child.name.Contains("CraftPartBeacon"))
                {
                    child.gameObject.SetActive(false);
                    Object.Destroy(child.gameObject);
                }
            }

            foreach (var c in go.GetComponentsInChildren<Collider>(true))
            {
                c.enabled = false;
                Object.Destroy(c);
            }

            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.detectCollisions = false;
                Object.Destroy(rb);
            }

            foreach (var b in go.GetComponentsInChildren<BoingOMatic>(true))
            {
                Object.Destroy(b);
            }

            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null)
                {
                    continue;
                }

                var ns = mb.GetType().Namespace ?? "";
                if (ns.Contains(".AI") || ns.Contains(".Gameplay") || mb is CollectiblePickup)
                {
                    Object.Destroy(mb);
                }
            }
        }

        private static void FitWorldSize(GameObject go, float targetSize)
        {
            // Measure as-cloned (keeps FBX child scales), then multiply root so longest axis = target.
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
            {
                return;
            }

            var b = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
            {
                if (rends[i] != null)
                {
                    b.Encapsulate(rends[i].bounds);
                }
            }

            var max = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (max < 0.001f)
            {
                return;
            }

            go.transform.localScale *= targetSize / max;
            SitOnGround(go);
        }

        private void ApplyRockVariation(GameObject root, int variationIndex)
        {
            if (root == null)
            {
                return;
            }

            var index = Mathf.Abs(variationIndex);
            root.transform.localScale *= RockScaleVariants[index % RockScaleVariants.Length];
            root.transform.rotation *= Quaternion.Euler(0f, RockYawVariants[index % RockYawVariants.Length], 0f);

            var tint = RockTintVariants[index % RockTintVariants.Length];
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                var materials = renderer.materials;
                for (var i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null)
                    {
                        continue;
                    }

                    OwnRuntimeMaterial(material);

                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", material.GetColor("_BaseColor") * tint);
                    }
                    else if (material.HasProperty("_Color"))
                    {
                        material.SetColor("_Color", material.GetColor("_Color") * tint);
                    }
                }

                renderer.materials = materials;
            }
        }

        private static void FitByHeight(GameObject go, float targetHeight)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
            {
                return;
            }

            var b = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
            {
                if (rends[i] != null)
                {
                    b.Encapsulate(rends[i].bounds);
                }
            }

            if (b.size.y < 0.001f)
            {
                return;
            }

            go.transform.localScale *= targetHeight / b.size.y;

            rends = go.GetComponentsInChildren<Renderer>();
            b = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
            {
                if (rends[i] != null)
                {
                    b.Encapsulate(rends[i].bounds);
                }
            }

            var groundY = go.transform.parent != null ? go.transform.parent.position.y : 0f;
            go.transform.position += Vector3.up * (groundY - b.min.y);
        }

        private static GameObject LoadCommissionedPrefab(string folder, string id)
        {
#if UNITY_EDITOR
            var urp = $"Assets/SpaceAndCrafts/Art/Commissioned/{folder}/{id}_URP.prefab";
            var fbx = $"Assets/SpaceAndCrafts/Art/Commissioned/{folder}/{id}.fbx";
            var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(urp);
            if (go == null)
            {
                go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            }

            return go;
#else
            return Resources.Load<GameObject>($"Commissioned/{folder}/{id}_URP");
#endif
        }

        private static void SitOnGround(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
            {
                return;
            }

            var b = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
            {
                if (rends[i] != null)
                {
                    b.Encapsulate(rends[i].bounds);
                }
            }

            go.transform.position += Vector3.up * (go.transform.position.y - b.min.y + 0.02f);
        }

        private static void SitOnGroundAtY(GameObject go, float groundY)
        {
            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0)
            {
                return;
            }

            var minY = float.PositiveInfinity;
            foreach (var renderer in rends)
            {
                if (renderer != null)
                {
                    minY = Mathf.Min(minY, renderer.bounds.min.y);
                }
            }

            if (!float.IsInfinity(minY))
            {
                go.transform.position += Vector3.up * (groundY - minY + 0.02f);
            }
        }

        private static float GroundY(Vector3 p)
        {
            if (Physics.Raycast(p + Vector3.up * 12f, Vector3.down, out var hit, 30f)
                && hit.point.y > -1.5f && hit.point.y < p.y + 6f)
            {
                return hit.point.y;
            }

            return Mathf.Max(0f, p.y);
        }

        private static Material MakeMat(Shader shader, Color color, float emission)
        {
            if (shader == null)
            {
                return null;
            }

            var mat = new Material(shader) { name = "Mat_PathCraft" };
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emission);
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0.5f);
            }

            return mat;
        }

    }
}
