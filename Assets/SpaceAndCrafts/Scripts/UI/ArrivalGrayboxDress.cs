using System.Collections.Generic;
using UnityEngine;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Player-visible craft dress on Arrival graybox slabs — saturated craft mats + soft lip edges.
    /// Runtime-only; does not rewrite authored meshes.
    /// </summary>
    public sealed class ArrivalGrayboxDress : MonoBehaviour
    {
        private readonly List<Material> _ownedMaterials = new();
        private readonly List<Mesh> _ownedMeshes = new();

        private static readonly Color[] CraftPalette =
        {
            new(0.95f, 0.45f, 0.78f),
            new(0.45f, 0.85f, 1f),
            new(1f, 0.85f, 0.35f),
            new(0.62f, 0.42f, 0.95f),
            new(0.4f, 0.95f, 0.65f),
            new(1f, 0.55f, 0.35f),
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

            if (Object.FindAnyObjectByType<ArrivalGrayboxDress>() != null)
            {
                return;
            }

            var go = new GameObject("SYS_ArrivalGrayboxDress");
            go.AddComponent<ArrivalGrayboxDress>().Dress();
        }

        private void OnDestroy()
        {
            for (var i = 0; i < _ownedMaterials.Count; i++)
            {
                if (_ownedMaterials[i] != null) Destroy(_ownedMaterials[i]);
            }
            _ownedMaterials.Clear();

            for (var i = 0; i < _ownedMeshes.Count; i++)
            {
                if (_ownedMeshes[i] != null) Destroy(_ownedMeshes[i]);
            }
            _ownedMeshes.Clear();
        }

        private void Dress()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                return;
            }

            SoftenGatewayThresholdVisual();
            AlignGatewayThresholdToTerrace();

            var dressed = 0;
            var customShelfFloor = GameObject.Find("ART_ShelfFloor");
            foreach (var rend in Object.FindObjectsByType<MeshRenderer>())
            {
                if (rend == null || !rend.enabled)
                {
                    continue;
                }

                var go = rend.gameObject;
                if (customShelfFloor != null
                    && (go.transform == customShelfFloor.transform || go.transform.IsChildOf(customShelfFloor.transform)))
                {
                    continue;
                }

                var n = go.name;
                if (n.StartsWith("ART_Meshy") || n.Contains("Showcase") || n.Contains("Splat")
                    || n.Contains("Crumb") || n.Contains("Beacon") || n.Contains("Enemy")
                    || n.Contains("Runway") || n.Contains("CraftIcon") || n.Contains("SkyBanner")
                    || n.Contains("Scrap") || n.Contains("Mite") || n.Contains("WarnHalo")
                    || n.Contains("CosmicBackdrop") || n.Contains("VistaStar") || n.Contains("Path"))
                {
                    continue;
                }

                // Don't paint tiny round discs under foes — that recreates figurine pads.
                var parent = go.transform.parent;
                if (parent != null)
                {
                    var pn = parent.name;
                    if (pn.Contains("Scrap") || pn.Contains("Mite") || pn.Contains("Roller")
                        || pn.Contains("Floater") || pn.Contains("Spitter") || pn.Contains("Sentinel"))
                    {
                        continue;
                    }
                }

                if (go.GetComponentInParent<Camera>() != null)
                {
                    continue;
                }

                var filter = go.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                var bounds = rend.bounds;
                var size = bounds.size;
                var isSlab = size.y < 1.4f && size.x > 1.6f && size.z > 1.6f;
                var isBlock = size.y >= 1.2f && size.x > 1f && size.z > 1f && size.magnitude > 3.2f;
                var isWall = size.y > 2.5f && (size.x > 3f || size.z > 3f) && Mathf.Min(size.x, size.z) < 2.2f;
                if (!isSlab && !isBlock && !isWall)
                {
                    continue;
                }

                if (size.magnitude < 2.4f)
                {
                    continue;
                }

                // Floors only — tall walls/blocks are hidden by ArrivalMeshyPathDress.
                // Desaturate + nearly zero emission so pastel cubes stop washing Meshy.
                if (!isSlab || isWall || isBlock)
                {
                    continue;
                }

                var color = Color.Lerp(CraftPalette[dressed % CraftPalette.Length], new Color(0.55f, 0.58f, 0.62f), 0.45f);
                color *= 0.72f;
                var isRegentTerrace = n == "ART_TerraceFloor";
                var terraceSlate = isRegentTerrace
                    ? Resources.Load<Texture2D>("Environment/ArrivalNordicSlateAlbedoV4")
                    : null;
                if (isRegentTerrace && terraceSlate != null && filter.sharedMesh != null
                    && filter.sharedMesh.uv.Length != filter.sharedMesh.vertexCount)
                {
                    // ART_Mesh_OrganicSlab has no UV0 channel; give this terrace
                    // its own planar XZ UVs so the slate texture actually appears.
                    var sourceMesh = filter.sharedMesh;
                    var uvMesh = Object.Instantiate(sourceMesh);
                    _ownedMeshes.Add(uvMesh);
                    uvMesh.name = sourceMesh.name + "_TerraceUV";
                    var vertices = uvMesh.vertices;
                    var uvBounds = uvMesh.bounds;
                    var uv = new Vector2[vertices.Length];
                    for (var i = 0; i < vertices.Length; i++)
                    {
                        uv[i] = new Vector2(
                            Mathf.InverseLerp(uvBounds.min.x, uvBounds.max.x, vertices[i].x),
                            Mathf.InverseLerp(uvBounds.min.z, uvBounds.max.z, vertices[i].z));
                    }
                    uvMesh.uv = uv;
                    filter.sharedMesh = uvMesh;
                }
                if (isRegentTerrace)
                {
                    // Keep the boss deck's mauve Rainbow identity while adding the
                    // same real slate grain used by the continuous Arrival landform.
                    color = new Color(0.98f, 0.78f, 1.05f, 1f);
                }
                var mat = new Material(shader) { name = go.name + "_CraftDress" };
                _ownedMaterials.Add(mat);
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }
                else
                {
                    mat.color = color;
                }

                if (terraceSlate != null)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", terraceSlate);
                    else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", terraceSlate);
                    mat.mainTextureScale = new Vector2(3f, 3f);
                }

                if (mat.HasProperty("_Smoothness"))
                {
                    mat.SetFloat("_Smoothness", 0.28f);
                }

                if (mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", 0.04f);
                }

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 0.04f);
                }

                rend.sharedMaterial = mat;

                dressed++;
                if (dressed >= 48)
                {
                    break;
                }
            }
        }

        private static void AlignGatewayThresholdToTerrace()
        {
            var threshold = GameObject.Find("ART_GatewayThreshold");
            var terrace = GameObject.Find("ART_TerraceFloor");
            var thresholdRenderer = threshold != null ? threshold.GetComponent<Renderer>() : null;
            var terraceRenderer = terrace != null ? terrace.GetComponent<Renderer>() : null;
            if (thresholdRenderer == null || terraceRenderer == null) return;

            var gap = thresholdRenderer.bounds.min.y - terraceRenderer.bounds.max.y;
            if (gap <= 0.02f || gap >= 2f) return;

            threshold.transform.position += Vector3.down * gap;
        }

        private void SoftenGatewayThresholdVisual()
        {
            var threshold = GameObject.Find("ART_GatewayThreshold");
            var renderer = threshold != null ? threshold.GetComponent<Renderer>() : null;
            if (threshold == null || renderer == null) return;

            // This scene object is render-only (no collider). Reduce its silhouette
            // slightly and harmonize the blue-gray sill with the mauve terrace so it
            // reads as a deck seam instead of a heavy bar across the approach view.
            var scale = threshold.transform.localScale;
            // Authoring already sets this target. Clamp rather than multiply so
            // runtime dressing is idempotent for newly-authored and serialized scenes.
            if (scale.y > 0.72f)
            {
                scale.y = 0.72f;
                threshold.transform.localScale = scale;
            }

            var material = renderer.material;
            _ownedMaterials.Add(material);
            var terraceMauve = new Color(0.56f, 0.45f, 0.68f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", terraceMauve);
            else material.color = terraceMauve;
        }

    }
}
