using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.Art
{
    /// <summary>Soft, short-lived particle dust for grounded impacts.</summary>
    public static class TempLandDustBurst
    {
        private static Material _sharedDustMaterial;
        private static Material _sharedRingMaterial;
        private static Mesh _sharedRingMesh;
        private static Texture2D _sharedSoftTexture;

        public static GameObject Spawn(Vector3 worldPosition)
        {
            return Spawn(worldPosition, worldPosition.y);
        }

        public static GameObject Spawn(Vector3 worldPosition, float surfaceY)
            => Spawn(worldPosition, surfaceY, 0.83f, false);

        public static GameObject Spawn(Vector3 worldPosition, float surfaceY,
            float impactWeight, bool groundPound)
        {
            impactWeight = Mathf.Clamp01(impactWeight);
            var root = new GameObject("TEMP_ART_LandDust");
            worldPosition.y = surfaceY + 0.025f;
            root.transform.position = worldPosition;
            var particles = root.AddComponent<ParticleSystem>();
            // Unity may start a newly added ParticleSystem before its modules are
            // configured. Stop and clear it first so changing duration/curves is
            // applied cleanly instead of logging a live-system configuration warning.
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Configure(particles, impactWeight);
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = SharedDustMaterial();
            CreateContactRing(root.transform, impactWeight, groundPound);
            particles.Play(true);
            var count = Mathf.RoundToInt(Mathf.Lerp(3f, 9f, impactWeight) + (groundPound ? 2f : 0f));
            particles.Emit(Mathf.Clamp(count, 3, 11));
            Object.Destroy(root, 0.65f);
            return root;
        }

        private static void CreateContactRing(Transform root, float impactWeight, bool groundPound)
        {
            var ringObject = new GameObject("TEMP_ART_LandDustRing");
            ringObject.transform.SetParent(root, false);
            ringObject.transform.localPosition = Vector3.up * 0.02f;
            ringObject.transform.localRotation = Quaternion.identity;
            var radius = Mathf.Lerp(0.70f, 0.92f, impactWeight)
                + (groundPound ? 0.22f : 0f);
            var maximumScale = new Vector3(radius, 1f, radius);
            const float initialScale = 0.86f;
            ringObject.transform.localScale = maximumScale * initialScale;
            var filter = ringObject.AddComponent<MeshFilter>();
            filter.sharedMesh = SharedRingMesh();
            var renderer = ringObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = SharedRingMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var animator = ringObject.AddComponent<TempLandDustRingAnimator>();
            animator.Initialize(renderer, maximumScale, initialScale, 0.64f,
                new Color(0.60f, 0.32f, 0.92f, 0.88f));
        }

        private static Material SharedRingMaterial()
        {
            if (_sharedRingMaterial != null) return _sharedRingMaterial;
            var shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color");
            if (shader == null) return null;

            _sharedRingMaterial = new Material(shader) { name = "TEMP_Mat_LandDustRing" };
            var tint = new Color(0.60f, 0.32f, 0.92f, 0.88f);
            if (_sharedRingMaterial.HasProperty("_Color"))
                _sharedRingMaterial.SetColor("_Color", tint);
            if (_sharedRingMaterial.HasProperty("_BaseColor"))
                _sharedRingMaterial.SetColor("_BaseColor", tint);
            if (_sharedRingMaterial.HasProperty("_Cull"))
                _sharedRingMaterial.SetInt("_Cull", (int)CullMode.Off);
            if (_sharedRingMaterial.HasProperty("_Surface"))
            {
                _sharedRingMaterial.SetFloat("_Surface", 1f);
                _sharedRingMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _sharedRingMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                _sharedRingMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                _sharedRingMaterial.SetInt("_ZWrite", 0);
            }
            _sharedRingMaterial.renderQueue = (int)RenderQueue.Transparent;
            return _sharedRingMaterial;
        }

        private static Mesh SharedRingMesh()
        {
            if (_sharedRingMesh != null) return _sharedRingMesh;
            const int segments = 48;
            const float innerRadius = 0.84f;
            const float outerRadius = 1f;
            var vertices = new Vector3[(segments + 1) * 2];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 12];
            for (var i = 0; i <= segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * innerRadius;
                vertices[i * 2 + 1] = direction * outerRadius;
                normals[i * 2] = Vector3.up;
                normals[i * 2 + 1] = Vector3.up;
                uvs[i * 2] = new Vector2(i / (float)segments, 0f);
                uvs[i * 2 + 1] = new Vector2(i / (float)segments, 1f);
                if (i == segments) continue;
                var triangle = i * 12;
                var inner = i * 2;
                var outer = inner + 1;
                // Wind both faces upward; otherwise the platform camera sees
                // only the back of this single-sided annulus and culls it.
                triangles[triangle] = inner;
                triangles[triangle + 1] = inner + 2;
                triangles[triangle + 2] = outer + 2;
                triangles[triangle + 3] = inner;
                triangles[triangle + 4] = outer + 2;
                triangles[triangle + 5] = outer;
                // Duplicate reversed faces as a fallback for render pipelines
                // that flip front-face winding when submitting the capture camera.
                triangles[triangle + 6] = outer + 2;
                triangles[triangle + 7] = inner + 2;
                triangles[triangle + 8] = inner;
                triangles[triangle + 9] = outer;
                triangles[triangle + 10] = outer + 2;
                triangles[triangle + 11] = inner;
            }

            _sharedRingMesh = new Mesh { name = "TEMP_Mesh_LandDustRing" };
            _sharedRingMesh.vertices = vertices;
            _sharedRingMesh.normals = normals;
            _sharedRingMesh.uv = uvs;
            _sharedRingMesh.triangles = triangles;
            _sharedRingMesh.RecalculateBounds();
            return _sharedRingMesh;
        }

        private static void Configure(ParticleSystem particles, float impactWeight)
        {
            var main = particles.main;
            main.duration = 0.35f;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = 12;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.44f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.70f, 0.90f, impactWeight),
                Mathf.Lerp(1.00f, 1.40f, impactWeight));
            main.startSize = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.24f, 0.32f, impactWeight),
                Mathf.Lerp(0.34f, 0.46f, impactWeight));
            // Keep the contact puff visible on the bright traversal leaves.
            // The previous main alpha and color-over-lifetime alpha multiplied
            // together, leaving this already short-lived cue too faint at play
            // distance. Use one clear alpha envelope and a deeper lavender tint.
            main.startColor = new Color(0.86f, 0.72f, 0.98f, 0.88f);
            main.gravityModifier = 0.24f;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            var emission = particles.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            // Push the burst outward around the player's feet so the body does
            // not hide the entire short-lived contact cue from the follow camera.
            shape.angle = 76f;
            shape.radius = 0.34f;
            shape.radiusThickness = 1f;
            // Particle System cones emit along local +Y; keep the cone upright so
            // impact dust lifts from the platform instead of being pushed into it.
            shape.rotation = Vector3.zero;

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.72f), new Keyframe(0.48f, 1.0f), new Keyframe(1f, 1.18f)));

            var color = particles.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.80f, 0.68f, 0.96f), 0f),
                    new GradientColorKey(new Color(0.66f, 0.56f, 0.82f), 1f) },
                new[] { new GradientAlphaKey(0.84f, 0f), new GradientAlphaKey(0.56f, 0.45f),
                    new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
        }

        private static Material SharedDustMaterial()
        {
            if (_sharedDustMaterial != null) return _sharedDustMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Unlit/Transparent");
            if (shader == null) return null;

            _sharedDustMaterial = new Material(shader) { name = "TEMP_Mat_LandDustShared" };
            if (_sharedDustMaterial.HasProperty("_BaseMap"))
                _sharedDustMaterial.SetTexture("_BaseMap", SharedSoftTexture());
            if (_sharedDustMaterial.HasProperty("_MainTex"))
                _sharedDustMaterial.SetTexture("_MainTex", SharedSoftTexture());
            if (_sharedDustMaterial.HasProperty("_BaseColor"))
                _sharedDustMaterial.SetColor("_BaseColor", Color.white);
            if (_sharedDustMaterial.HasProperty("_Color"))
                _sharedDustMaterial.SetColor("_Color", Color.white);
            if (_sharedDustMaterial.HasProperty("_Surface"))
            {
                _sharedDustMaterial.SetFloat("_Surface", 1f);
                _sharedDustMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _sharedDustMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                _sharedDustMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                _sharedDustMaterial.SetInt("_ZWrite", 0);
            }
            _sharedDustMaterial.renderQueue = (int)RenderQueue.Transparent;
            return _sharedDustMaterial;
        }

        private static Texture2D SharedSoftTexture()
        {
            if (_sharedSoftTexture != null) return _sharedSoftTexture;
            const int size = 32;
            _sharedSoftTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "TEMP_Tex_LandDustSoft",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var uv = new Vector2((x + 0.5f) / size, (y + 0.5f) / size) * 2f - Vector2.one;
                var radial = Mathf.Clamp01(1f - uv.magnitude);
                var alpha = radial * radial * (3f - 2f * radial);
                _sharedSoftTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }

            _sharedSoftTexture.Apply(false, true);
            return _sharedSoftTexture;
        }
    }
}
