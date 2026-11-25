using SpaceAndCrafts.Art;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Brief, non-colliding surface ripple used when the fjord catches a missed jump.
    /// </summary>
    public sealed class ArrivalFjordSplashEffect : MonoBehaviour
    {
        private const float Duration = 0.9f;
        private static readonly Color SplashColor = new(0.34f, 0.88f, 0.96f, 0.78f);
        private static readonly Color FoamColor = new(0.76f, 0.96f, 1f, 0.66f);

        private readonly System.Collections.Generic.List<Mesh> _ownedMeshes = new();
        private readonly System.Collections.Generic.List<Material> _ownedMaterials = new();
        private Transform[] _ripples;
        private Renderer[] _rippleRenderers;
        private Transform[] _spray;
        private Material _sprayMaterial;
        private float _startedAt;

        public static void Spawn(Vector3 position)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
            if (shader == null)
            {
                return;
            }

            var go = new GameObject("ART_ArrivalFjordSplash");
            go.transform.position = position;
            var effect = go.AddComponent<ArrivalFjordSplashEffect>();
            effect.BuildVisuals(shader);
        }

        private void Awake()
        {
            _startedAt = Time.time;
        }

        private void BuildVisuals(Shader shader)
        {
            var rippleMesh = Own(OrganicMeshFactory.Torus(0.32f, 0.024f, 40, 10));
            var dropletMesh = Own(OrganicMeshFactory.SoftBlob(2, 0.12f, 1f, 0f));
            var rippleRoot = transform;
            var rippleCount = 3;
            _ripples = new Transform[rippleCount];
            _rippleRenderers = new Renderer[rippleCount];
            var rippleColors = new[] { SplashColor, FoamColor, SplashColor };

            for (var i = 0; i < rippleCount; i++)
            {
                var holder = i == 0 ? gameObject : new GameObject($"ART_FjordRipple_{i}");
                if (i > 0)
                {
                    holder.transform.SetParent(rippleRoot, false);
                }

                MeshFilter filter;
                MeshRenderer renderer;
                if (i == 0)
                {
                    filter = GetComponent<MeshFilter>();
                    if (filter == null) filter = gameObject.AddComponent<MeshFilter>();
                    renderer = GetComponent<MeshRenderer>();
                    if (renderer == null) renderer = gameObject.AddComponent<MeshRenderer>();
                }
                else
                {
                    filter = holder.AddComponent<MeshFilter>();
                    renderer = holder.AddComponent<MeshRenderer>();
                }

                filter.sharedMesh = rippleMesh;
                Configure(renderer, shader, rippleColors[i], $"Mat_ArrivalFjordRipple_{i}");
                _ripples[i] = holder.transform;
                _rippleRenderers[i] = renderer;
            }

            const int sprayCount = 8;
            _spray = new Transform[sprayCount];
            for (var i = 0; i < sprayCount; i++)
            {
                var drop = new GameObject($"ART_FjordDroplet_{i}");
                drop.transform.SetParent(rippleRoot, false);
                drop.AddComponent<MeshFilter>().sharedMesh = dropletMesh;
                var renderer = drop.AddComponent<MeshRenderer>();
                if (_sprayMaterial == null)
                {
                    _sprayMaterial = Configure(renderer, shader, FoamColor, "Mat_ArrivalFjordSpray");
                }
                else
                {
                    Configure(renderer, _sprayMaterial);
                }
                var angle = i * (Mathf.PI * 2f / sprayCount) + 0.22f;
                _spray[i] = drop.transform;
            }
        }

        private Mesh Own(Mesh mesh)
        {
            if (mesh != null)
            {
                _ownedMeshes.Add(mesh);
            }

            return mesh;
        }

        private Material Configure(Renderer renderer, Shader shader, Color color, string name)
        {
            var material = new Material(shader) { name = name };
            SetColor(material, color);
            MakeTransparent(material);
            _ownedMaterials.Add(material);
            Configure(renderer, material);
            return material;
        }

        private static void Configure(Renderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void Update()
        {
            var age = Time.time - _startedAt;
            for (var i = 0; i < _ripples.Length; i++)
            {
                var delay = i * 0.11f;
                var t = Mathf.Clamp01((age - delay) / (Duration - delay));
                var eased = Mathf.SmoothStep(0f, 1f, t);
                _ripples[i].localScale = Vector3.one * Mathf.Lerp(0.2f, 4.5f, eased);
                var baseColor = i == 1 ? FoamColor : SplashColor;
                SetColor(_rippleRenderers[i].sharedMaterial,
                    new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (age < delay ? 0f : 1f - t)));
            }

            var sprayT = Mathf.Clamp01(age / 0.58f);
            var sprayAlpha = age > 0.58f ? 0f : FoamColor.a * (1f - sprayT);
            SetColor(_sprayMaterial, new Color(FoamColor.r, FoamColor.g, FoamColor.b, sprayAlpha));
            for (var i = 0; i < _spray.Length; i++)
            {
                var angle = i * (Mathf.PI * 2f / _spray.Length) + 0.22f;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var radial = Mathf.Lerp(0.12f, 0.78f, sprayT);
                var arc = Mathf.Sin(sprayT * Mathf.PI) * 0.72f + 0.08f;
                _spray[i].localPosition = direction * radial + Vector3.up * arc;
                var size = Mathf.Lerp(0.9f, 0.45f, sprayT);
                _spray[i].localScale = new Vector3(size, size * 1.25f, size);
            }

            if (age >= Duration)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            foreach (var mesh in _ownedMeshes)
            {
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }

            foreach (var material in _ownedMaterials)
            {
                if (material == null) continue;
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
        }

        private static void SetColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        private static void MakeTransparent(Material material)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetFloat("_Blend", 0f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
            }
            else if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_ALPHABLEND_ON");
            }

            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            material.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
