using SpaceAndCrafts.Art;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using UnityEngine;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Spatial “what to do” — glowing columns on craft parts + Build Site so the loop is readable in-world.
    /// </summary>
    public sealed class CraftObjectiveBeacons : MonoBehaviour
    {
        private static Mesh _pickupBeaconRingMesh;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            TryWire(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode _)
        {
            TryWire(scene);
        }

        private static void TryWire(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || scene.name == null || !scene.name.Contains("Arrival"))
            {
                return;
            }

            if (Object.FindAnyObjectByType<CraftObjectiveBeacons>() != null)
            {
                return;
            }

            var go = new GameObject("SYS_CraftObjectiveBeacons");
            go.AddComponent<CraftObjectiveBeacons>().Wire();
        }

        private void Wire()
        {
            foreach (var pickup in Object.FindObjectsByType<CollectiblePickup>())
            {
                if (pickup == null || pickup.Definition == null)
                {
                    continue;
                }

                if (pickup.Definition.Category != CollectibleCategory.Component)
                {
                    continue;
                }

                // Showcase Meshy pickups can sit at FBX×100 — never beacon those.
                if (pickup.transform.root.name.Contains("MeshyCriticalShowcase") ||
                    pickup.transform.root.name.Contains("Showcase"))
                {
                    continue;
                }

                var beaconColor = pickup.Definition.Id switch
                {
                    "component_spring" => new Color(1f, 0.68f, 0.2f),
                    "component_metal_dish" => new Color(0.3f, 0.88f, 0.95f),
                    "component_power_crystal" => new Color(0.45f, 0.78f, 1f),
                    _ => new Color(1f, 0.85f, 0.25f)
                };
                AttachBeacon(pickup.transform, "ART_CraftPartBeacon", beaconColor, 0.22f, true);
            }

            var site = Object.FindAnyObjectByType<BuildSite>();
            if (site != null && !site.transform.root.name.Contains("Showcase"))
            {
                AttachBeacon(site.transform, "ART_BuildSiteBeacon", new Color(1f, 0.35f, 0.85f), 0.7f, false);
            }
        }

        private static void AttachBeacon(Transform host, string name, Color color, float height, bool spin)
        {
            if (host == null || host.Find(name) != null)
            {
                return;
            }

            _pickupBeaconRingMesh ??= OrganicMeshFactory.Torus(0.24f, 0.04f, 36, 10);
            var beacon = new GameObject(name);
            beacon.name = name;
            beacon.transform.SetParent(host, false);
            var renderers = host.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            var hostBounds = new Bounds(host.position, Vector3.zero);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled || !renderers[i].gameObject.activeInHierarchy)
                    continue;
                if (!hasBounds)
                {
                    hostBounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    hostBounds.Encapsulate(renderers[i].bounds);
                }
            }
            var top = hasBounds ? hostBounds.max.y : host.position.y;
            beacon.transform.position = new Vector3(host.position.x, top + height, host.position.z);
            beacon.transform.rotation = Quaternion.identity;
            var inheritedScale = host.lossyScale;
            beacon.transform.localScale = new Vector3(
                1f / Mathf.Max(Mathf.Abs(inheritedScale.x), 0.001f),
                1f / Mathf.Max(Mathf.Abs(inheritedScale.y), 0.001f),
                1f / Mathf.Max(Mathf.Abs(inheritedScale.z), 0.001f));
            beacon.AddComponent<MeshFilter>().sharedMesh = _pickupBeaconRingMesh;
            beacon.AddComponent<MeshRenderer>();

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader) { name = name + "_Mat" };
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }
                else
                {
                    mat.color = color;
                }

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.42f);
                }

                beacon.GetComponent<Renderer>().sharedMaterial = mat;
            }

            var lightGo = new GameObject("LIT_Beacon");
            lightGo.transform.SetParent(beacon.transform, false);
            lightGo.transform.localPosition = Vector3.up;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
                light.intensity = 0.42f;
                light.range = 1.5f;

            var pulse = beacon.AddComponent<BeaconPulse>();
            pulse.spin = spin;
            pulse.baseScale = beacon.transform.localScale;
        }

        private sealed class BeaconPulse : MonoBehaviour
        {
            public bool spin;
            public Vector3 baseScale = Vector3.one;
            private float _phase;

            private void Awake()
            {
                _phase = Random.Range(0f, Mathf.PI * 2f);
            }

            private void Update()
            {
                var t = Time.time * 2.2f + _phase;
                var s = 1f + Mathf.Sin(t) * 0.12f;
                transform.localScale = new Vector3(baseScale.x * s, baseScale.y, baseScale.z * s);
                if (spin)
                {
                    transform.Rotate(0f, 70f * Time.deltaTime, 0f, Space.World);
                }
            }
        }
    }
}
