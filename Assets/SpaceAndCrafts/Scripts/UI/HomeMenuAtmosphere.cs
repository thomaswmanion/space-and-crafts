using UnityEngine;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Fills the HomeMenu purple void with a craft-platformer stage: floor, sky band, rim light, floating scraps.
    /// Built at runtime so the scene stays light; reads as a title stage, not empty editor clear-color.
    /// </summary>
    public sealed class HomeMenuAtmosphere : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnHome()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name == null || !scene.name.Contains("HomeMenu"))
            {
                return;
            }

            if (Object.FindAnyObjectByType<HomeMenuAtmosphere>() != null)
            {
                return;
            }

            var root = GameObject.Find("ART_HomeMenu");
            var go = new GameObject("SYS_HomeMenuAtmosphere");
            if (root != null)
            {
                go.transform.SetParent(root.transform, false);
            }

            go.AddComponent<HomeMenuAtmosphere>().Build();
        }

        private void Build()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.45f, 0.72f, 0.95f);
                // Brand-first Home: frame Splat center-stage under the title, dock sits below.
                cam.transform.position = new Vector3(0.55f, 1.55f, -5.35f);
                cam.transform.rotation = Quaternion.Euler(5f, -4f, 0f);
                cam.fieldOfView = 38f;
            }

            // Soft sky / haze bands behind hero (quads face -Z toward camera by default).
            Quad("ART_TitleSky", new Vector3(0.6f, 2.2f, 8f), new Vector3(22f, 14f, 1f),
                new Color(0.55f, 0.82f, 1f, 1f));
            Quad("ART_TitleHaze", new Vector3(0.4f, 0.4f, 7f), new Vector3(18f, 6f, 1f),
                new Color(1f, 0.55f, 0.85f, 0.35f));

            // Stage floor — craft platformer ground, not void.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "ART_TitleFloor";
            floor.transform.SetParent(transform, false);
            floor.transform.localPosition = new Vector3(0.2f, -0.05f, 1.2f);
            floor.transform.localScale = new Vector3(7.5f, 0.08f, 7.5f);
            Object.Destroy(floor.GetComponent<Collider>());
            Tint(floor, new Color(0.72f, 0.38f, 0.62f), 0.18f, 0.04f);

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ART_TitleRing";
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0.2f, 0.02f, 1.2f);
            ring.transform.localScale = new Vector3(3.2f, 0.04f, 3.2f);
            Object.Destroy(ring.GetComponent<Collider>());
            Tint(ring, new Color(0.35f, 0.95f, 1f), 0.35f, 0.4f);

            // Soft key keeps the matte lavender silhouette readable against the stage.
            var key = new GameObject("LIT_TitleKey");
            key.transform.SetParent(transform, false);
            key.transform.localPosition = new Vector3(-2.5f, 4.5f, -2f);
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.92f, 0.94f, 1f);
            light.intensity = 0.38f;
            key.transform.rotation = Quaternion.Euler(35f, 40f, 0f);

            var fill = new GameObject("LIT_TitleFill");
            fill.transform.SetParent(transform, false);
            fill.transform.localPosition = new Vector3(3f, 2f, -1f);
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.65f, 0.7f, 1f);
            fillLight.intensity = 0.55f;
            fillLight.range = 10f;

            // Meshy hero + tree + gateway own the stage (no primitive rainbow cubes).
            PlantTitleMeshyStage();
            PlantTitleMeshyGateway();

            var gate = GameObject.Find("ART_TitleGateway");
            if (gate != null)
            {
                // Hide primitive title gateway — Meshy RainbowGateway replaces it.
                foreach (var r in gate.GetComponentsInChildren<Renderer>(true))
                {
                    if (r != null)
                    {
                        r.enabled = false;
                    }
                }
            }

            var faceLit = new GameObject("LIT_TitleFace");
            faceLit.transform.SetParent(transform, false);
            faceLit.transform.position = new Vector3(-0.2f, 1.55f, -1.15f);
            var faceLight = faceLit.AddComponent<Light>();
            faceLight.type = LightType.Spot;
            faceLight.color = new Color(0.95f, 0.96f, 1f);
            faceLight.intensity = 0.85f;
            faceLight.range = 7f;
            faceLight.spotAngle = 42f;
            faceLit.transform.rotation = Quaternion.LookRotation(
                new Vector3(0.35f, 1.2f, 0.2f) - faceLit.transform.position);

            RenderSettings.ambientIntensity = 0.35f;
            RenderSettings.ambientLight = new Color(0.35f, 0.42f, 0.55f);
            RenderSettings.reflectionIntensity = 0.2f;
        }

        /// <summary>Use the shared soft, expressive mascot at title scale; retain Meshy scenery.</summary>
        private void PlantTitleMeshyStage()
        {
            var soft = GameObject.Find("ART_TitleSplat");
            if (soft != null)
            {
                SpaceAndCrafts.Art.SplatVisualRepair.Repair(soft.transform);
                soft.SetActive(true);
                soft.transform.position = new Vector3(0.35f, 0.05f, 0.35f);
                // The Home camera looks in from -Z; show the full +Z face and keep
                // the title, character, and save panel in distinct visual bands.
                soft.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                soft.transform.localScale = Vector3.one * 0.8f;

                var life = soft.GetComponent<MeshyPropLifeProxy>();
                if (life == null)
                {
                    life = soft.AddComponent<MeshyPropLifeProxy>();
                }

                life.bobMeters = 0.06f;
                life.bobHz = 0.75f;
                life.yawDeg = 0f;
            }

            var treePrefab = LoadCommissionedPrefab("Environments", "RainbowCraftTree");
            if (treePrefab != null)
            {
                var tree = Object.Instantiate(treePrefab);
                tree.name = "ART_TitleMeshyTree";
                tree.transform.SetParent(transform, false);
                StripImportedColliders(tree);
                FitWorldSize(tree, 3.4f);
                tree.transform.position = new Vector3(-2.35f, 0.02f, 2.4f);
                tree.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
                MuteSpecular(tree);
                var life = tree.AddComponent<MeshyPropLifeProxy>();
                life.bobMeters = 0.04f;
                life.bobHz = 0.35f;
                life.yawDeg = 6f;
            }
        }

        /// <summary>Existing Meshy RainbowGateway behind hero — replaces primitive rainbow cubes.</summary>
        private void PlantTitleMeshyGateway()
        {
            var prefab = LoadCommissionedPrefab("Landmarks", "RainbowGateway");
            if (prefab == null)
            {
                return;
            }

            var gate = Object.Instantiate(prefab);
            gate.name = "ART_TitleMeshyGateway";
            gate.transform.SetParent(transform, false);
            StripImportedColliders(gate);
            FitWorldSize(gate, 4.8f);
            gate.transform.position = new Vector3(0.55f, 0.02f, 2.85f);
            gate.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            MuteSpecular(gate);
            var life = gate.AddComponent<MeshyPropLifeProxy>();
            life.bobMeters = 0.03f;
            life.bobHz = 0.25f;
            life.yawDeg = 0f;
        }

        private static void MuteSpecular(GameObject root)
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

                    if (mat.HasProperty("_Smoothness"))
                    {
                        mat.SetFloat("_Smoothness", Mathf.Min(mat.GetFloat("_Smoothness"), 0.22f));
                    }

                    if (mat.HasProperty("_Metallic"))
                    {
                        mat.SetFloat("_Metallic", Mathf.Min(mat.GetFloat("_Metallic"), 0.08f));
                    }

                    if (mat.HasProperty("_EmissionColor"))
                    {
                        var e = mat.GetColor("_EmissionColor");
                        mat.SetColor("_EmissionColor", e * 0.25f);
                    }
                }

                rend.materials = mats;
            }
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

        private static void StripImportedColliders(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>(true))
            {
                Object.Destroy(c);
            }

            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                Object.Destroy(rb);
            }
        }

        private static void FitWorldSize(GameObject go, float targetSize)
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

            var max = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (max < 0.001f)
            {
                return;
            }

            go.transform.localScale *= targetSize / max;

            rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
            {
                return;
            }

            b = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
            {
                if (rends[i] != null)
                {
                    b.Encapsulate(rends[i].bounds);
                }
            }

            go.transform.position += Vector3.up * (go.transform.position.y - b.min.y + 0.02f);
        }

        private GameObject Quad(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            Object.Destroy(go.GetComponent<Collider>());
            Tint(go, color, 0.1f, 0f);
            return go;
        }

        private static void Tint(GameObject go, Color color, float smoothness, float metallic)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                return;
            }

            var mat = new Material(shader) { name = go.name + "_Mat" };
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            else
            {
                mat.color = color;
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }

            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", metallic);
            }

            if (color.a < 0.99f && mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.05f);
            }

            rend.sharedMaterial = mat;
        }

        /// <summary>Tiny local life component so title scraps move without depending on Arrival wiring.</summary>
        private sealed class MeshyPropLifeProxy : MonoBehaviour
        {
            public float bobMeters = 0.1f;
            public float bobHz = 0.5f;
            public float yawDeg = 20f;
            private Vector3 _rest;
            private Quaternion _restRot;
            private float _phase;

            private void Awake()
            {
                _rest = transform.localPosition;
                _restRot = transform.localRotation;
                _phase = Random.Range(0f, Mathf.PI * 2f);
            }

            private void Update()
            {
                var t = Time.time * bobHz * Mathf.PI * 2f + _phase;
                transform.localPosition = _rest + Vector3.up * (Mathf.Sin(t) * bobMeters);
                transform.localRotation = _restRot * Quaternion.Euler(0f, Time.time * yawDeg, Mathf.Sin(t) * 8f);
            }
        }
    }
}
