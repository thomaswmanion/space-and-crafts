using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Quiet, non-colliding fjord scenery that gives Arrival a continuous landscape below
    /// its stepping-stone route. Built from code so the playable route remains untouched.
    /// </summary>
    public static class ArrivalFjordBackdrop
    {
        // Give the broad catch volume a continuous fjord surface and enclosing ridge
        // silhouettes beneath it, including the outer edge of a wayward fall.
        public const float FjordSpan = 460f;
        public const float FjordCatchSpan = FjordSpan + 60f;
        private const float FjordWaterSpan = FjordCatchSpan + 8f;
        private const int NearWallSegments = 39;
        private const int FarRidgeSegments = 31;
        private const int HorizonRidgeSegments = 96;
        private const float HorizonRidgeInnerRadius = 212f;
        private const float HorizonRidgeOuterRadius = 244f;
        private const float CliffTextureWorldRepeat = 14f;
        private const float WaterTextureWorldRepeat = 40f;

        public static void Build(Transform parent, Vector3 start, Vector3 end)
        {
            if (parent == null) return;
            var old = parent.Find("ART_ArrivalFjordBackdrop");
            if (old != null) Object.Destroy(old.gameObject);

            var root = new GameObject("ART_ArrivalFjordBackdrop");
            root.transform.SetParent(parent, false);
            root.AddComponent<ArrivalFjordResourceOwner>();
            var axis = end - start;
            axis.y = 0f;
            if (axis.sqrMagnitude < 0.01f) axis = Vector3.forward;
            axis.Normalize();
            var across = Vector3.Cross(Vector3.up, axis).normalized;
            var midpoint = (start + end) * 0.5f;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;

            var calmWaterAlbedo = Resources.Load<Texture2D>("Environment/ArrivalFjordWaterCalmV2");
            var waterAlbedo = calmWaterAlbedo != null
                ? calmWaterAlbedo
                : Resources.Load<Texture2D>("Environment/ArrivalFjordWaterAlbedo");
            var waterTint = calmWaterAlbedo != null
                ? new Color(0.88f, 0.98f, 1.02f)
                : waterAlbedo != null
                    ? new Color(1.08f, 1.17f, 1.2f)
                    : new Color(0.075f, 0.30f, 0.39f);
            var water = MakeMaterial(shader, waterTint,
                waterAlbedo != null ? (calmWaterAlbedo != null ? 0.42f : 0.62f) : 0.42f, 0.02f);
            ApplyWaterAlbedo(water, waterAlbedo);
            var waterGo = MakeObject(root.transform, "ART_FjordWater",
                MakeWaterMesh(FjordWaterSpan, FjordWaterSpan, 54), water);
            waterGo.transform.position = new Vector3(midpoint.x, -6.4f, midpoint.z);
            waterGo.AddComponent<ArrivalFjordWaterAnimator>();
            var waterRenderer = waterGo.GetComponent<MeshRenderer>();
            if (waterRenderer != null)
            {
                waterRenderer.shadowCastingMode = ShadowCastingMode.Off;
                waterRenderer.receiveShadows = false;
            }

            var slateAlbedo = Resources.Load<Texture2D>("Environment/ArrivalNordicSlateAlbedoV2")
                ?? Resources.Load<Texture2D>("Environment/ArrivalSlateCliffAlbedo");
            var nearCliff = MakeMaterial(shader,
                slateAlbedo != null ? new Color(0.48f, 0.68f, 0.75f) : new Color(0.22f, 0.36f, 0.41f),
                0.12f, 0.02f);
            var farCliff = MakeMaterial(shader,
                slateAlbedo != null ? new Color(0.7f, 0.92f, 0.98f) : new Color(0.39f, 0.54f, 0.61f),
                0.18f, 0.01f);
            var distantCliff = MakeMaterial(shader,
                slateAlbedo != null ? new Color(0.78f, 0.98f, 1.08f) : new Color(0.32f, 0.47f, 0.55f),
                0.2f, 0.01f);
            ApplyCliffAlbedo(nearCliff, slateAlbedo);
            ApplyCliffAlbedo(farCliff, slateAlbedo);
            ApplyCliffAlbedo(distantCliff, slateAlbedo);
            var snow = MakeMaterial(shader, new Color(0.72f, 0.84f, 0.87f), 0.24f, 0.01f);
            var falls = MakeMaterial(shader, new Color(0.48f, 0.78f, 0.82f), 0.34f, 0.01f);
            var auroraAqua = MakeAuroraMaterial(new Color(0.28f, 0.82f, 0.72f, 0.14f));
            var auroraLilac = MakeAuroraMaterial(new Color(0.58f, 0.62f, 0.96f, 0.085f));
            if (falls.HasProperty("_EmissionColor"))
            {
                falls.EnableKeyword("_EMISSION");
                falls.SetColor("_EmissionColor", new Color(0.19f, 0.52f, 0.6f, 1f) * 0.45f);
            }
            // Keep the close bank low enough for the panorama and aurora to frame it;
            // taller snow-tipped ridges sit farther away so the view reads as a broad fjord.
            for (var side = -1; side <= 1; side += 2)
            {
                CreateRidge(root.transform, $"ART_FjordWall_{side}", midpoint, axis, across * side,
                    42f, 70f, -6.3f, 5f, 12f, NearWallSegments, nearCliff, side * 19, side < 0, snow);
                CreateRidge(root.transform, $"ART_FjordPeaks_{side}", midpoint, axis, across * side,
                    82f, 120f, -6.3f, 11f, 28f, FarRidgeSegments, farCliff, side * 37 + 5, side < 0, snow);
                // A softer third layer creates distant mountain depth beyond the fjord walls.
                CreateRidge(root.transform, $"ART_FjordDistantPeaks_{side}", midpoint, axis, across * side,
                    132f, 174f, -6.3f, 18f, 38f, FarRidgeSegments, distantCliff,
                    side * 53 + 11, side < 0, snow);
                for (var i = 0; i < 3; i++)
                {
                    var t = 0.18f + i * 0.31f + (side < 0 ? 0.12f : 0f);
                    CreateWaterfall(root.transform, $"ART_FjordWaterfall_{side}_{i}", midpoint, axis,
                        across * side, 42f, -6.3f, 5f, 12f, NearWallSegments, side * 19, t, falls, side > 0);
                }

                var far = side * 146f;
                CreateAuroraBand(root.transform, $"ART_ArrivalAurora_Aqua_{side}", midpoint, axis, across,
                    far, 306f, 37f, 7.5f, side * 0.7f, auroraAqua);
                CreateAuroraBand(root.transform, $"ART_ArrivalAurora_Lilac_{side}", midpoint, axis, across,
                    far + side * 7f, 284f, 44f, 6f, side * 1.2f + 1.1f, auroraLilac);
            }

            CreateShelfFoundation(root.transform, nearCliff, waterGo.transform.position.y);
            // Close the distant view in every direction. The side ridges alone leave the
            // fjord open at both ends, where a wide camera orbit can reveal empty sky.
            CreateHorizonRidge(root.transform, midpoint, farCliff);
        }

        /// <summary>
        /// Visually roots the Arrival spawn shelf into the fjord, replacing the exposed
        /// sides of its graybox slab with a low-poly slate outcrop that sinks into water.
        /// It has no collider, so the authored route and forgiving fall catch stay intact.
        /// </summary>
        private static void CreateShelfFoundation(Transform parent, Material material, float waterY)
        {
            var shelf = GameObject.Find("ART_ShelfFloor");
            var shelfRenderer = shelf != null ? shelf.GetComponentInChildren<Renderer>() : null;
            if (shelfRenderer == null || material == null)
            {
                return;
            }

            var bounds = shelfRenderer.bounds;
            const int segmentsPerSide = 18;
            var topY = bounds.max.y + 0.04f;
            var bottomY = waterY - 0.85f;
            var vertices = new List<Vector3>((segmentsPerSide + 1) * 12);
            var uvs = new List<Vector2>((segmentsPerSide + 1) * 12);
            var triangles = new List<int>(segmentsPerSide * 4 * 12);

            AddShelfCliffSide(parent, vertices, uvs, triangles,
                new Vector3(bounds.max.x, 0f, bounds.min.z),
                new Vector3(bounds.max.x, 0f, bounds.max.z), Vector3.right,
                topY, bottomY, segmentsPerSide);
            AddShelfCliffSide(parent, vertices, uvs, triangles,
                new Vector3(bounds.max.x, 0f, bounds.max.z),
                new Vector3(bounds.min.x, 0f, bounds.max.z), Vector3.forward,
                topY, bottomY, segmentsPerSide);
            AddShelfCliffSide(parent, vertices, uvs, triangles,
                new Vector3(bounds.min.x, 0f, bounds.max.z),
                new Vector3(bounds.min.x, 0f, bounds.min.z), Vector3.left,
                topY, bottomY, segmentsPerSide);
            AddShelfCliffSide(parent, vertices, uvs, triangles,
                new Vector3(bounds.min.x, 0f, bounds.min.z),
                new Vector3(bounds.max.x, 0f, bounds.min.z), Vector3.back,
                topY, bottomY, segmentsPerSide);

            if (vertices.Count == 0)
            {
                return;
            }

            var mesh = new Mesh { name = "ART_FjordShelfFoundationMesh" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            MakeObject(parent, "ART_FjordShelfFoundation", mesh, material);
        }

        private static void AddShelfCliffSide(Transform parent, List<Vector3> vertices, List<Vector2> uvs,
            List<int> triangles, Vector3 start, Vector3 end, Vector3 outward, float topY, float bottomY,
            int segments)
        {
            var direction = (end - start).normalized;
            var length = Vector3.Distance(start, end);
            var winding = Vector3.Dot(Vector3.Cross(direction, Vector3.down), outward) >= 0f;
            var firstVertex = vertices.Count;
            var height = topY - bottomY;
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var edge = Vector3.Lerp(start, end, t);
                var broad = Mathf.Sin(t * Mathf.PI * 5f + Vector3.Dot(outward, Vector3.right) * 1.7f) * 0.22f;
                var fine = Mathf.Sin(t * Mathf.PI * 13f + Vector3.Dot(outward, Vector3.forward) * 2.3f) * 0.12f;
                var waviness = broad + fine;
                var u = t * length / CliffTextureWorldRepeat;

                vertices.Add(parent.InverseTransformPoint(edge + outward * 0.08f + Vector3.up * topY));
                vertices.Add(parent.InverseTransformPoint(edge + outward * (0.5f + waviness)
                    + Vector3.up * (Mathf.Lerp(topY, bottomY, 0.48f) + broad)));
                vertices.Add(parent.InverseTransformPoint(edge + outward * (1.2f + waviness)
                    + Vector3.up * bottomY));
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, height * 0.48f / CliffTextureWorldRepeat));
                uvs.Add(new Vector2(u, height / CliffTextureWorldRepeat));
            }

            for (var i = 0; i < segments; i++)
            {
                for (var row = 0; row < 2; row++)
                {
                    var a = firstVertex + i * 3 + row;
                    var b = a + 3;
                    if (winding)
                    {
                        AddTriangle(triangles, a, b, a + 1);
                        AddTriangle(triangles, b, b + 1, a + 1);
                    }
                    else
                    {
                        AddTriangle(triangles, a, a + 1, b);
                        AddTriangle(triangles, b, a + 1, b + 1);
                    }
                }
            }
        }

        private static void CreateHorizonRidge(Transform parent, Vector3 center, Material material)
        {
            const int rows = 3;
            const float baseY = -6.3f;
            var vertices = new Vector3[(HorizonRidgeSegments + 1) * rows];
            var uvs = new Vector2[vertices.Length];
            var triangles = new List<int>(HorizonRidgeSegments * (rows - 1) * 6);
            var peakHeights = new float[HorizonRidgeSegments + 1];

            for (var i = 0; i <= HorizonRidgeSegments; i++)
            {
                var t = i / (float)HorizonRidgeSegments;
                var angle = t * Mathf.PI * 2f;
                var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var wave = Mathf.Abs(Mathf.Sin(angle * 4.5f + 0.4f)) * 0.68f
                    + Mathf.Abs(Mathf.Sin(angle * 8.5f - 0.7f)) * 0.32f;
                var peak = Mathf.Lerp(21f, 36f, wave);
                peakHeights[i] = peak;
                var row = i * rows;
                var u = t * 100f;
                vertices[row] = center + radial * HorizonRidgeInnerRadius + Vector3.up * baseY;
                vertices[row + 1] = center + radial * (HorizonRidgeInnerRadius + 10f) + Vector3.up * peak;
                vertices[row + 2] = center + radial * HorizonRidgeOuterRadius + Vector3.up * baseY;
                uvs[row] = new Vector2(u, 0f);
                uvs[row + 1] = new Vector2(u, (peak - baseY) / CliffTextureWorldRepeat);
                uvs[row + 2] = new Vector2(u, (HorizonRidgeOuterRadius - HorizonRidgeInnerRadius) / CliffTextureWorldRepeat);
            }

            for (var i = 0; i < HorizonRidgeSegments; i++)
            {
                for (var row = 0; row < rows - 1; row++)
                {
                    var a = i * rows + row;
                    var b = (i + 1) * rows + row;
                    AddTriangle(triangles, a, b, a + 1);
                    AddTriangle(triangles, a + 1, b, b + 1);
                }
            }

            var mesh = new Mesh { name = "ART_FjordHorizonRidgeMesh" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            MakeObject(parent, "ART_FjordHorizonRidge", mesh, material);
            CreateHorizonSnowcaps(parent, center, peakHeights, baseY);
        }

        private static void CreateHorizonSnowcaps(Transform parent, Vector3 center,
            float[] peakHeights, float baseY)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var step = Mathf.PI * 2f / HorizonRidgeSegments;
            var snowline = 34f;

            for (var i = 0; i < HorizonRidgeSegments; i++)
            {
                var peak = peakHeights[i];
                var previous = (i + HorizonRidgeSegments - 1) % HorizonRidgeSegments;
                var next = (i + 1) % HorizonRidgeSegments;
                if (peak < snowline || peak < peakHeights[previous] || peak < peakHeights[next])
                {
                    continue;
                }

                var angle = i * step;
                var capRadius = HorizonRidgeInnerRadius + 7f;
                var leftAngle = angle - step * 0.62f;
                var rightAngle = angle + step * 0.62f;
                var left = center + new Vector3(Mathf.Cos(leftAngle), 0f, Mathf.Sin(leftAngle)) * capRadius
                    + Vector3.up * (Mathf.Lerp(baseY, peakHeights[previous], 0.7f) + 0.08f);
                var right = center + new Vector3(Mathf.Cos(rightAngle), 0f, Mathf.Sin(rightAngle)) * capRadius
                    + Vector3.up * (Mathf.Lerp(baseY, peakHeights[next], 0.7f) + 0.08f);
                var tip = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))
                    * (HorizonRidgeInnerRadius + 10f) + Vector3.up * (peak + 0.08f);
                var first = vertices.Count;
                vertices.Add(left);
                vertices.Add(right);
                vertices.Add(tip);
                AddTriangle(triangles, first, first + 1, first + 2);
            }

            if (vertices.Count == 0)
            {
                return;
            }

            var mesh = new Mesh { name = "ART_FjordHorizonSnowcapsMesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            MakeObject(parent, "ART_FjordHorizonSnowcaps", mesh,
                MakeMaterial(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"),
                    new Color(0.72f, 0.84f, 0.87f), 0.24f, 0.01f));
        }

        private static Mesh MakeWaterMesh(float width, float length, int segments)
        {
            var mesh = new Mesh { name = "ArrivalFjordWaterMesh" };
            var vertices = new Vector3[(segments + 1) * (segments + 1)];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * segments * 6];
            for (var z = 0; z <= segments; z++)
            {
                for (var x = 0; x <= segments; x++)
                {
                    var u = x / (float)segments;
                    var v = z / (float)segments;
                    var ripple = Mathf.Sin(u * 68f + v * 13f) * Mathf.Sin(v * 51f - u * 17f) * 0.055f;
                    var vertexIndex = z * (segments + 1) + x;
                    vertices[vertexIndex] = new Vector3((u - 0.5f) * width, ripple, (v - 0.5f) * length);
                    uvs[vertexIndex] = new Vector2(u * width / WaterTextureWorldRepeat,
                        v * length / WaterTextureWorldRepeat);
                }
            }

            var index = 0;
            for (var z = 0; z < segments; z++)
            {
                for (var x = 0; x < segments; x++)
                {
                    var a = z * (segments + 1) + x;
                    var b = a + segments + 1;
                    triangles[index++] = a; triangles[index++] = b; triangles[index++] = b + 1;
                    triangles[index++] = a; triangles[index++] = b + 1; triangles[index++] = a + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreateRidge(Transform parent, string name, Vector3 center, Vector3 axis,
            Vector3 outward, float innerDistance, float outerDistance, float baseY, float minimumPeak,
            float maximumPeak, int segments, Material material, int seed, bool reverseWinding,
            Material snowMaterial)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var peakHeights = new float[segments + 1];
            var rows = 3;
            var columns = segments + 1;
            for (var z = 0; z < columns; z++)
            {
                var t = z / (float)segments;
                var peak = GetPeakHeight(t, segments, seed, minimumPeak, maximumPeak);
                peakHeights[z] = peak;
                var along = axis * ((t - 0.5f) * FjordSpan);
                vertices.Add(center + along + outward * innerDistance + Vector3.up * baseY);
                vertices.Add(center + along + outward * (innerDistance + 10f) + Vector3.up * peak);
                vertices.Add(center + along + outward * outerDistance + Vector3.up * baseY);
                var u = (t - 0.5f) * FjordSpan / CliffTextureWorldRepeat + seed * 0.013f;
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, (peak - baseY) / CliffTextureWorldRepeat));
                uvs.Add(new Vector2(u, (outerDistance - innerDistance) / CliffTextureWorldRepeat));
            }

            for (var z = 0; z < segments; z++)
            {
                for (var x = 0; x < rows - 1; x++)
                {
                    var a = z * rows + x; var b = a + rows;
                    if (reverseWinding)
                    {
                        AddTriangle(triangles, a, a + 1, b);
                        AddTriangle(triangles, a + 1, b + 1, b);
                    }
                    else
                    {
                        AddTriangle(triangles, a, b, a + 1);
                        AddTriangle(triangles, a + 1, b, b + 1);
                    }
                }
            }

            var mesh = new Mesh { name = name + "Mesh" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            MakeObject(parent, name, mesh, material);

            if (snowMaterial != null)
            {
                CreateSnowCaps(parent, name + "_SnowCaps", center, axis, outward, innerDistance,
                    baseY, minimumPeak, maximumPeak, segments, peakHeights, reverseWinding, snowMaterial);
            }
        }

        private static float GetPeakHeight(float t, int segments, int seed, float minimum, float maximum)
        {
            var wave = Mathf.Abs(Mathf.Sin((t * segments + seed * 0.173f) * 1.19f)) * 0.7f
                + Mathf.Abs(Mathf.Sin((t * segments + seed * 0.071f) * 0.53f)) * 0.3f;
            return Mathf.Lerp(minimum, maximum, wave);
        }

        private static void CreateSnowCaps(Transform parent, string name, Vector3 center, Vector3 axis,
            Vector3 outward, float innerDistance, float baseY, float minimumPeak, float maximumPeak,
            int segments, float[] peakHeights, bool reverseWinding, Material material)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var threshold = Mathf.Lerp(minimumPeak, maximumPeak, 0.82f);
            var halfWidth = FjordSpan / segments * 0.22f;
            for (var z = 1; z < segments; z++)
            {
                var peak = peakHeights[z];
                if (peak < threshold || peak < peakHeights[z - 1] || peak < peakHeights[z + 1])
                {
                    continue;
                }

                var t = z / (float)segments;
                var along = (t - 0.5f) * FjordSpan;
                var fraction = halfWidth / (FjordSpan / segments);
                var leftPeak = Mathf.Lerp(peakHeights[z - 1], peak, 1f - fraction);
                var rightPeak = Mathf.Lerp(peak, peakHeights[z + 1], fraction);
                var lowerFraction = 0.78f;
                var tipFraction = 0.96f;
                var left = center + axis * (along - halfWidth)
                    + outward * (innerDistance + 10f * lowerFraction + 0.1f)
                    + Vector3.up * (baseY + (leftPeak - baseY) * lowerFraction);
                var right = center + axis * (along + halfWidth)
                    + outward * (innerDistance + 10f * lowerFraction + 0.1f)
                    + Vector3.up * (baseY + (rightPeak - baseY) * lowerFraction);
                var tip = center + axis * along
                    + outward * (innerDistance + 10f * tipFraction + 0.1f)
                    + Vector3.up * (baseY + (peak - baseY) * tipFraction);

                var start = vertices.Count;
                vertices.Add(left); vertices.Add(right); vertices.Add(tip);
                if (reverseWinding)
                {
                    triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                }
                else
                {
                    triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                }
            }

            if (vertices.Count == 0) return;
            var mesh = new Mesh { name = name + "Mesh" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            MakeObject(parent, name, mesh, material);
        }

        private static void CreateAuroraBand(Transform parent, string name, Vector3 center, Vector3 axis,
            Vector3 across, float axisOffset, float width, float baseHeight, float bandHeight,
            float phase, Material material)
        {
            if (material == null) return;

            const int segments = 36;
            var vertices = new Vector3[(segments + 1) * 2];
            var triangles = new int[segments * 6];
            for (var i = 0; i <= segments; i++)
            {
                var u = i / (float)segments;
                // Mathf.Sin(pi) can be a tiny negative float. Clamp before fractional
                // exponentiation so the closing vertex stays finite on every backend.
                var envelope = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(Mathf.PI * u)), 0.72f);
                var fold = Mathf.Sin(u * 5.2f * Mathf.PI + phase) * 1.3f
                    + Mathf.Sin(u * 2.1f * Mathf.PI - phase * 0.65f) * 0.8f;
                var depth = Mathf.Sin(u * 4.6f * Mathf.PI + phase) * 2.2f;
                var position = center + across * ((u - 0.5f) * width) + axis * (axisOffset + depth);
                var bottom = baseHeight + fold - bandHeight * 0.34f * envelope;
                var top = baseHeight + fold + bandHeight * 0.66f * envelope;
                vertices[i * 2] = position + Vector3.up * bottom;
                vertices[i * 2 + 1] = position + Vector3.up * top;
            }

            var index = 0;
            var reverseWinding = axisOffset < 0f;
            for (var i = 0; i < segments; i++)
            {
                var bottom = i * 2;
                var top = bottom + 1;
                var nextBottom = bottom + 2;
                var nextTop = bottom + 3;
                if (reverseWinding)
                {
                    triangles[index++] = bottom; triangles[index++] = nextBottom; triangles[index++] = top;
                    triangles[index++] = top; triangles[index++] = nextBottom; triangles[index++] = nextTop;
                }
                else
                {
                    triangles[index++] = bottom; triangles[index++] = top; triangles[index++] = nextBottom;
                    triangles[index++] = top; triangles[index++] = nextTop; triangles[index++] = nextBottom;
                }
            }

            var mesh = new Mesh { name = name + "Mesh" };
            mesh.vertices = vertices; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = MakeObject(parent, name, mesh, material);
            go.AddComponent<ArrivalFjordAuroraAnimator>().Configure(axis, phase, segments);
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static void CreateWaterfall(Transform parent, string name, Vector3 center, Vector3 axis,
            Vector3 outward, float innerDistance, float baseY, float minimumPeak, float maximumPeak,
            int segments, int seed, float t, Material material, bool reverseWinding)
        {
            var wave = Mathf.Abs(Mathf.Sin((t * segments + seed * 0.173f) * 1.19f)) * 0.7f
                + Mathf.Abs(Mathf.Sin((t * segments + seed * 0.071f) * 0.53f)) * 0.3f;
            var peak = Mathf.Lerp(minimumPeak, maximumPeak, wave) * 0.82f;
            var along = axis * ((t - 0.5f) * FjordSpan);
            var halfWidth = 0.62f;
            var top = center + along + outward * (innerDistance + 9.85f) + Vector3.up * (peak - 0.12f);
            var foot = center + along + outward * (innerDistance + 0.45f) + Vector3.up * (baseY + 0.16f);
            var vertices = new[]
            {
                top - axis * halfWidth, top + axis * halfWidth,
                foot - axis * halfWidth, foot + axis * halfWidth,
            };
            var mesh = new Mesh { name = name + "Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = reverseWinding
                ? new[] { 0, 2, 1, 1, 2, 3 }
                : new[] { 0, 1, 2, 1, 3, 2 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            MakeObject(parent, name, mesh, material);
        }

        private static void AddTriangle(List<int> triangles, int a, int b, int c)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
        }

        private static GameObject MakeObject(Transform parent, string name, Mesh mesh, Material material)
        {
            var owner = parent.GetComponent<ArrivalFjordResourceOwner>();
            owner?.Register(mesh, material);
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static Material MakeMaterial(Shader shader, Color color, float smoothness, float metallic)
        {
            var material = new Material(shader) { name = "Mat_ArrivalFjord_" + ColorUtility.ToHtmlStringRGB(color) };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            return material;
        }

        private static Material MakeAuroraMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
            if (shader == null) return null;
            var material = new Material(shader) { name = "Mat_ArrivalAurora_" + ColorUtility.ToHtmlStringRGB(color) };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetFloat("_Blend", 0f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            return material;
        }

        private static void ApplyCliffAlbedo(Material material, Texture2D albedo)
        {
            if (material == null || albedo == null)
            {
                return;
            }

            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
            else if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
        }

        private static void ApplyWaterAlbedo(Material material, Texture2D albedo)
        {
            if (material == null || albedo == null)
            {
                return;
            }

            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
            else if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
        }
    }
}
