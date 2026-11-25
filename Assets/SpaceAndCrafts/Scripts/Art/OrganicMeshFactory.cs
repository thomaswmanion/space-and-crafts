using System.Collections.Generic;
using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Procedural high-density meshes for hero/invention/landmark silhouettes.
    /// Not a substitute for commissioned finals, but must never be a bare Unity primitive.
    /// </summary>
    public static class OrganicMeshFactory
    {
        private static readonly Mesh[] MiteSupportLegCache = new Mesh[6];

        public static Mesh Icosphere(int subdivisions = 3, float radius = 0.5f)
        {
            subdivisions = Mathf.Clamp(subdivisions, 0, 5);
            var t = (1f + Mathf.Sqrt(5f)) / 2f;
            var verts = new List<Vector3>
            {
                new Vector3(-1, t, 0).normalized * radius,
                new Vector3(1, t, 0).normalized * radius,
                new Vector3(-1, -t, 0).normalized * radius,
                new Vector3(1, -t, 0).normalized * radius,
                new Vector3(0, -1, t).normalized * radius,
                new Vector3(0, 1, t).normalized * radius,
                new Vector3(0, -1, -t).normalized * radius,
                new Vector3(0, 1, -t).normalized * radius,
                new Vector3(t, 0, -1).normalized * radius,
                new Vector3(t, 0, 1).normalized * radius,
                new Vector3(-t, 0, -1).normalized * radius,
                new Vector3(-t, 0, 1).normalized * radius
            };

            var faces = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };

            var midCache = new Dictionary<long, int>();
            for (var s = 0; s < subdivisions; s++)
            {
                var next = new List<int>(faces.Count * 4);
                for (var i = 0; i < faces.Count; i += 3)
                {
                    var a = faces[i];
                    var b = faces[i + 1];
                    var c = faces[i + 2];
                    var ab = MidpointIndex(a, b, verts, midCache, radius);
                    var bc = MidpointIndex(b, c, verts, midCache, radius);
                    var ca = MidpointIndex(c, a, verts, midCache, radius);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }

                faces = next;
                midCache.Clear();
            }

            return Finish($"Organic_Icosphere_s{subdivisions}", verts, faces);
        }

        public static Mesh SoftBlob(int subdivisions = 3, float radius = 0.5f, float squashY = 0.92f, float noise = 0.06f)
        {
            var mesh = Icosphere(subdivisions, radius);
            var v = mesh.vertices;
            for (var i = 0; i < v.Length; i++)
            {
                var n = v[i].normalized;
                var warp = 1f + noise * Mathf.Sin(n.x * 7.1f + n.y * 5.3f + n.z * 3.7f);
                v[i] = new Vector3(n.x * radius * warp, n.y * radius * squashY * warp, n.z * radius * warp);
            }

            mesh.vertices = v;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = "Organic_SoftBlob";
            return mesh;
        }

        /// <summary>
        /// Projects local XZ coordinates into normalized UVs for top-down textures
        /// on round walkable meshes. Optional rotation gives repeated pads varied
        /// vein directions without changing their geometry or support surface.
        /// </summary>
        public static void SetPlanarTopDownUVs(Mesh mesh, float rotationDegrees = 0f)
        {
            if (mesh == null) return;
            var vertices = mesh.vertices;
            if (vertices == null || vertices.Length == 0) return;

            var bounds = mesh.bounds;
            var width = Mathf.Max(0.001f, bounds.size.x);
            var depth = Mathf.Max(0.001f, bounds.size.z);
            var uv = new Vector2[vertices.Length];
            var angle = rotationDegrees * Mathf.Deg2Rad;
            var sin = Mathf.Sin(angle);
            var cos = Mathf.Cos(angle);
            for (var i = 0; i < vertices.Length; i++)
            {
                var point = new Vector2(
                    (vertices[i].x - bounds.min.x) / width - 0.5f,
                    (vertices[i].z - bounds.min.z) / depth - 0.5f);
                uv[i] = new Vector2(
                    point.x * cos - point.y * sin + 0.5f,
                    point.x * sin + point.y * cos + 0.5f);
            }

            mesh.uv = uv;
        }

        public static Mesh SplatFoot(int subdivisions = 4, float radius = 0.21f,
            float squashY = 0.78f, float noise = 0.012f)
        {
            // Keep the familiar soft upper silhouette, but give the single
            // continuous foot mesh a broad planar sole. A fully ellipsoidal
            // underside catches light as a hovering bead even when its bounds
            // reach the platform. Clamping only the lower cap leaves the toe,
            // heel and top rounded while providing a readable contact surface.
            var mesh = SoftBlob(subdivisions, radius, squashY, noise);
            var vertices = mesh.vertices;
            var soleY = -radius * squashY * 0.78f;
            for (var i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].y < soleY)
                    vertices[i].y = soleY;
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = "Splat_Continuous_FlatSoleFoot";
            return mesh;
        }

        public static Mesh SkinnedPawArm(int side, float length = 0.5f, int radialSegments = 18,
            int lengthSegments = 20, float radiusScale = 1f)
        {
            side = side < 0 ? -1 : 1;
            length = Mathf.Max(0.3f, length);
            radiusScale = Mathf.Clamp(radiusScale, 0.7f, 1.3f);
            radialSegments = Mathf.Max(8, radialSegments);
            lengthSegments = Mathf.Max(8, lengthSegments);

            // Build the arm and mitten as one closed, smoothly varying surface.
            // The mitten swells into the final third instead of intersecting an
            // independently shaded sphere at the wrist.
            var vertices = new List<Vector3>((lengthSegments + 1) * radialSegments + 2);
            var triangles = new List<int>(lengthSegments * radialSegments * 6 + radialSegments * 6);
            var weights = new BoneWeight[(lengthSegments + 1) * radialSegments + 2];
            var xKeys = new[] { 0f, 0.2f, 0.43f, 0.64f, 0.82f, 0.93f, 0.98f, 1f };
            var radiusKeys = new[] { 0.135f, 0.12f, 0.13f, 0.155f, 0.18f, 0.16f, 0.075f, 0.005f };
            var wristT = Mathf.Clamp01(0.275f / length);

            for (var along = 0; along <= lengthSegments; along++)
            {
                var t = along / (float)lengthSegments;
                var radius = EvaluateProfile(t, xKeys, radiusKeys) * radiusScale;
                var blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(wristT * 0.55f, Mathf.Min(1f, wristT * 1.55f), t));
                for (var radial = 0; radial < radialSegments; radial++)
                {
                    var angle = radial * Mathf.PI * 2f / radialSegments;
                    var index = vertices.Count;
                    vertices.Add(new Vector3(side * length * t,
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius * 0.82f));
                    weights[index] = new BoneWeight
                    {
                        boneIndex0 = 0,
                        boneIndex1 = 1,
                        weight0 = 1f - blend,
                        weight1 = blend
                    };
                }
            }

            var startCap = vertices.Count;
            vertices.Add(Vector3.zero);
            weights[startCap] = new BoneWeight { boneIndex0 = 0, weight0 = 1f };
            var endCap = vertices.Count;
            vertices.Add(new Vector3(side * length, 0f, 0f));
            weights[endCap] = new BoneWeight { boneIndex0 = 0, boneIndex1 = 1, weight0 = 0.05f, weight1 = 0.95f };

            for (var along = 0; along < lengthSegments; along++)
            {
                for (var radial = 0; radial < radialSegments; radial++)
                {
                    var nextRadial = (radial + 1) % radialSegments;
                    var a = along * radialSegments + radial;
                    var b = along * radialSegments + nextRadial;
                    var c = (along + 1) * radialSegments + nextRadial;
                    var d = (along + 1) * radialSegments + radial;
                    if (side > 0) triangles.AddRange(new[] { a, b, c, a, c, d });
                    else triangles.AddRange(new[] { a, c, b, a, d, c });
                }
            }

            for (var radial = 0; radial < radialSegments; radial++)
            {
                var nextRadial = (radial + 1) % radialSegments;
                if (side > 0)
                {
                    triangles.AddRange(new[] { startCap, nextRadial, radial });
                    var last = lengthSegments * radialSegments;
                    triangles.AddRange(new[] { endCap, last + radial, last + nextRadial });
                }
                else
                {
                    triangles.AddRange(new[] { startCap, radial, nextRadial });
                    var last = lengthSegments * radialSegments;
                    triangles.AddRange(new[] { endCap, last + nextRadial, last + radial });
                }
            }

            var mesh = Finish("Splat_SkinnedPawArm", vertices, triangles);
            mesh.boneWeights = weights;
            return mesh;
        }

        private static float EvaluateProfile(float t, float[] xKeys, float[] valueKeys)
        {
            for (var i = 1; i < xKeys.Length; i++)
            {
                if (t > xKeys[i]) continue;
                var blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(xKeys[i - 1], xKeys[i], t));
                return Mathf.Lerp(valueKeys[i - 1], valueKeys[i], blend);
            }

            return valueKeys[valueKeys.Length - 1];
        }

        public static Mesh Torus(float major = 0.45f, float minor = 0.12f, int segments = 48, int tubes = 24)
        {
            segments = Mathf.Max(8, segments);
            tubes = Mathf.Max(6, tubes);
            var verts = new List<Vector3>(segments * tubes);
            var tris = new List<int>(segments * tubes * 6);
            for (var i = 0; i < segments; i++)
            {
                var u = (i / (float)segments) * Mathf.PI * 2f;
                var center = new Vector3(Mathf.Cos(u) * major, 0f, Mathf.Sin(u) * major);
                for (var j = 0; j < tubes; j++)
                {
                    var v = (j / (float)tubes) * Mathf.PI * 2f;
                    var normal = new Vector3(Mathf.Cos(u) * Mathf.Cos(v), Mathf.Sin(v), Mathf.Sin(u) * Mathf.Cos(v));
                    verts.Add(center + normal * minor);
                }
            }

            for (var i = 0; i < segments; i++)
            {
                var i2 = (i + 1) % segments;
                for (var j = 0; j < tubes; j++)
                {
                    var j2 = (j + 1) % tubes;
                    var a = i * tubes + j;
                    var b = i2 * tubes + j;
                    var c = i2 * tubes + j2;
                    var d = i * tubes + j2;
                    tris.AddRange(new[] { a, b, c, a, c, d });
                }
            }

            return Finish("Organic_Torus", verts, tris);
        }

        public static Mesh SpringHelix(float height = 0.7f, float radius = 0.28f, float tube = 0.045f, int coils = 5, int segmentsPerCoil = 24)
        {
            coils = Mathf.Max(2, coils);
            segmentsPerCoil = Mathf.Max(8, segmentsPerCoil);
            var path = new List<Vector3>();
            var total = coils * segmentsPerCoil;
            for (var i = 0; i <= total; i++)
            {
                var t = i / (float)total;
                var ang = t * coils * Mathf.PI * 2f;
                path.Add(new Vector3(Mathf.Cos(ang) * radius, Mathf.Lerp(-height * 0.5f, height * 0.5f, t), Mathf.Sin(ang) * radius));
            }

            return TubeAlongPath(path, tube, 10, "Organic_SpringHelix");
        }

        public static Mesh MiteSupportLeg(float side, float foreAft, float reach = 0.31f, float drop = 0.29f)
        {
            side = Mathf.Sign(side);
            foreAft = Mathf.Clamp(foreAft, -1f, 1f);
            var cacheIndex = (side > 0f ? 3 : 0) + Mathf.RoundToInt(foreAft + 1f);
            var useDefaultDimensions = Mathf.Approximately(reach, 0.31f) && Mathf.Approximately(drop, 0.29f);
            if (useDefaultDimensions && MiteSupportLegCache[cacheIndex] != null)
            {
                return MiteSupportLegCache[cacheIndex];
            }

            var start = Vector3.zero;
            var controlA = new Vector3(side * reach * 0.22f, 0.015f, foreAft * 0.025f);
            var controlB = new Vector3(side * reach * 0.86f, -drop * 0.28f, foreAft * 0.10f);
            var end = new Vector3(side * reach, -drop, foreAft * 0.15f);
            const int segments = 12;
            var path = new List<Vector3>(segments + 1);
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var oneMinusT = 1f - t;
                path.Add(oneMinusT * oneMinusT * oneMinusT * start
                    + 3f * oneMinusT * oneMinusT * t * controlA
                    + 3f * oneMinusT * t * t * controlB
                    + t * t * t * end);
            }

            var mesh = TubeAlongPathVariable(path, 0.052f, 0.018f, 10, "Organic_MiteSupportLeg");
            if (useDefaultDimensions)
            {
                MiteSupportLegCache[cacheIndex] = mesh;
            }
            return mesh;
        }

        public static Mesh LathedProfile(Vector2[] profile, int segments = 32, string name = "Organic_Lathe")
        {
            if (profile == null || profile.Length < 2)
            {
                return SoftBlob();
            }

            segments = Mathf.Max(8, segments);
            var verts = new List<Vector3>(profile.Length * segments);
            var tris = new List<int>();
            for (var s = 0; s < segments; s++)
            {
                var ang = (s / (float)segments) * Mathf.PI * 2f;
                var cos = Mathf.Cos(ang);
                var sin = Mathf.Sin(ang);
                for (var p = 0; p < profile.Length; p++)
                {
                    verts.Add(new Vector3(profile[p].x * cos, profile[p].y, profile[p].x * sin));
                }
            }

            for (var s = 0; s < segments; s++)
            {
                var s2 = (s + 1) % segments;
                for (var p = 0; p < profile.Length - 1; p++)
                {
                    var a = s * profile.Length + p;
                    var b = s2 * profile.Length + p;
                    var c = s2 * profile.Length + p + 1;
                    var d = s * profile.Length + p + 1;
                    tris.AddRange(new[] { a, b, c, a, c, d });
                }
            }

            return Finish(name, verts, tris);
        }

        /// <summary>
        /// One continuous crown-and-bill mesh for Splat's backwards baseball cap.
        /// Local forward is +Z; the bill extends toward -Z so it reads backwards
        /// from the face while remaining a single cohesive accessory mesh.
        /// </summary>
        public static Mesh BackwardsBaseballCap(float radius = 0.40f, int segments = 32)
        {
            segments = Mathf.Max(16, segments);
            var profile = new[]
            {
                new Vector2(radius, 0f),
                new Vector2(radius * 0.98f, 0.065f),
                new Vector2(radius * 0.86f, 0.15f),
                new Vector2(radius * 0.64f, 0.235f),
                new Vector2(radius * 0.37f, 0.292f),
                new Vector2(radius * 0.10f, 0.315f),
                new Vector2(0f, 0.318f)
            };
            var crown = LathedProfile(profile, segments, "Splat_BackwardsCap_Crown");
            var vertices = new List<Vector3>(crown.vertexCount + 9 * 17 * 2);
            var triangles = new List<int>(crown.triangles.Length + 8 * 16 * 12);
            vertices.AddRange(crown.vertices);
            var crownTriangles = crown.triangles;
            for (var i = 0; i + 2 < crownTriangles.Length; i += 3)
            {
                triangles.Add(crownTriangles[i]);
                triangles.Add(crownTriangles[i + 2]);
                triangles.Add(crownTriangles[i + 1]);
            }

            const int rows = 9;
            const int columns = 17;
            var topOffset = vertices.Count;
            for (var row = 0; row < rows; row++)
            {
                var t = row / (float)(rows - 1);
                // A backwards cap has a compact, blunt rear bill. The older long
                // taper projected as a wire-like spike in side views.
                // The tab should emerge as one centered rear flap. Keeping it
                // inside the crown's side silhouette prevents two wing-like
                // corners from appearing under rear gameplay cameras.
                var halfWidth = radius * Mathf.Lerp(0.60f, 0.46f, t);
                var z = -radius * (0.30f + 0.98f * t);
                for (var column = 0; column < columns; column++)
                {
                    var u = column / (float)(columns - 1) * 2f - 1f;
                    var x = u * halfWidth;
                    // A shallow crown curve and a gently cupped edge keep the
                    // bill from reading like a flat card stuck on the cap.
                    var y = radius * (0.025f - 0.09f * t + 0.045f * u * u);
                    vertices.Add(new Vector3(x, y, z));
                }
            }

            for (var row = 0; row < rows - 1; row++)
            {
                for (var column = 0; column < columns - 1; column++)
                {
                    var a = topOffset + row * columns + column;
                    var b = a + 1;
                    var d = a + columns;
                    var c = d + 1;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    triangles.Add(a); triangles.Add(c); triangles.Add(d);
                }
            }

            // Give the curved bill a fine edge instead of a zero-thickness card.
            var undersideOffset = vertices.Count;
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var top = vertices[topOffset + row * columns + column];
                    vertices.Add(top - Vector3.up * radius * 0.045f);
                }
            }
            for (var row = 0; row < rows - 1; row++)
            {
                for (var column = 0; column < columns - 1; column++)
                {
                    var a = undersideOffset + row * columns + column;
                    var b = a + 1;
                    var d = a + columns;
                    var c = d + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(a); triangles.Add(d); triangles.Add(c);
                }
            }

            var mesh = Finish("Splat_BackwardsBaseballCap", vertices, triangles);
            if (Application.isPlaying) Object.Destroy(crown);
            else Object.DestroyImmediate(crown);
            return mesh;
        }

        /// <summary>Curved forehead strap for the backwards cap, following its front crown.</summary>
        public static Mesh BackwardsCapFrontStrap(float radius = 0.40f, int columns = 21, int rows = 5)
        {
            columns = Mathf.Max(9, columns);
            rows = Mathf.Max(3, rows);
            var vertices = new List<Vector3>(columns * rows);
            var triangles = new List<int>((columns - 1) * (rows - 1) * 6);
            var halfWidth = radius * 0.60f;
            const float bottom = 0.022f;
            const float top = 0.092f;
            for (var row = 0; row < rows; row++)
            {
                var v = row / (float)(rows - 1);
                var y = Mathf.Lerp(bottom, top, v);
                // Sample the same lathed crown profile so the strap hugs the
                // front hemisphere instead of floating as a flat rectangle.
                var crownRadius = CapCrownRadiusAtHeight(y, radius);
                for (var column = 0; column < columns; column++)
                {
                    var u = column / (float)(columns - 1) * 2f - 1f;
                    var x = u * halfWidth;
                    var z = Mathf.Sqrt(Mathf.Max(0.001f, crownRadius * crownRadius - x * x)) + 0.006f;
                    vertices.Add(new Vector3(x, y, z));
                }
            }

            for (var row = 0; row < rows - 1; row++)
            {
                for (var column = 0; column < columns - 1; column++)
                {
                    var a = row * columns + column;
                    var b = a + 1;
                    var d = a + columns;
                    var c = d + 1;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    triangles.Add(a); triangles.Add(c); triangles.Add(d);
                }
            }

            return Finish("Splat_BackwardsCap_FrontStrap", vertices, triangles);
        }

        private static float CapCrownRadiusAtHeight(float height, float radius)
        {
            var profile = new[]
            {
                new Vector2(0f, radius),
                new Vector2(0.065f, radius * 0.98f),
                new Vector2(0.15f, radius * 0.86f),
                new Vector2(0.235f, radius * 0.64f),
            };
            for (var i = 0; i < profile.Length - 1; i++)
            {
                if (height <= profile[i + 1].x)
                {
                    var t = Mathf.InverseLerp(profile[i].x, profile[i + 1].x, height);
                    return Mathf.Lerp(profile[i].y, profile[i + 1].y, t);
                }
            }
            return profile[profile.Length - 1].y;
        }

        public static Mesh CrystalFacet(float height = 1f, float radius = 0.28f, int sides = 6)
        {
            sides = Mathf.Clamp(sides, 4, 12);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            verts.Add(new Vector3(0f, height * 0.5f, 0f)); // tip
            verts.Add(new Vector3(0f, -height * 0.5f, 0f)); // base tip
            for (var i = 0; i < sides; i++)
            {
                var a0 = (i / (float)sides) * Mathf.PI * 2f;
                var a1 = ((i + 0.5f) / sides) * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a0) * radius, height * 0.12f, Mathf.Sin(a0) * radius));
                verts.Add(new Vector3(Mathf.Cos(a1) * radius * 0.85f, -height * 0.12f, Mathf.Sin(a1) * radius * 0.85f));
            }

            for (var i = 0; i < sides; i++)
            {
                var u = 2 + i * 2;
                var uNext = 2 + ((i + 1) % sides) * 2;
                var l = u + 1;
                var lNext = uNext + 1;
                tris.AddRange(new[] { 0, u, uNext });
                tris.AddRange(new[] { u, l, lNext, u, lNext, uNext });
                tris.AddRange(new[] { 1, lNext, l });
            }

            return Finish("Organic_CrystalFacet", verts, tris);
        }

        public static Mesh DishBowl(float radius = 0.7f, float depth = 0.22f, int rings = 10, int segments = 36)
        {
            var profile = new Vector2[rings + 1];
            for (var i = 0; i <= rings; i++)
            {
                var t = i / (float)rings;
                var r = Mathf.Lerp(0.05f, radius, t);
                var y = Mathf.Sin(t * Mathf.PI) * depth - depth * 0.35f;
                profile[i] = new Vector2(r, y);
            }

            return LathedProfile(profile, segments, "Organic_DishBowl");
        }

        public static Mesh ArchGateway(float width = 2.4f, float height = 3.2f, float thickness = 0.35f, int segments = 28)
        {
            var path = new List<Vector3>();
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var ang = Mathf.PI * t;
                path.Add(new Vector3(Mathf.Cos(ang) * width * 0.5f, Mathf.Sin(ang) * height, 0f));
            }

            // Legs
            path.Insert(0, new Vector3(width * 0.5f, -0.2f, 0f));
            path.Add(new Vector3(-width * 0.5f, -0.2f, 0f));
            return TubeAlongPath(path, thickness * 0.5f, 12, "Organic_ArchGateway");
        }

        public static Mesh StarBurst(float outer = 0.55f, float inner = 0.22f, float thickness = 0.14f, int points = 5)
        {
            return FacetedStar(outer, inner, thickness, points);
        }

        public static Mesh FacetedStar(float outer = 0.55f, float inner = 0.22f, float thickness = 0.14f, int points = 5)
        {
            points = Mathf.Clamp(points, 3, 12);
            var half = thickness * 0.5f;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            verts.Add(new Vector3(0f, half, 0f));
            verts.Add(new Vector3(0f, -half, 0f));

            var ringCount = points * 2;
            for (var i = 0; i < ringCount; i++)
            {
                var ang = (i / (float)ringCount) * Mathf.PI * 2f - Mathf.PI * 0.5f;
                var r = (i % 2 == 0) ? outer : inner;
                var x = Mathf.Cos(ang) * r;
                var z = Mathf.Sin(ang) * r;
                verts.Add(new Vector3(x, half * 0.55f, z));
                verts.Add(new Vector3(x, -half * 0.55f, z));
            }

            for (var i = 0; i < ringCount; i++)
            {
                var u = 2 + i * 2;
                var uNext = 2 + ((i + 1) % ringCount) * 2;
                var l = u + 1;
                var lNext = uNext + 1;
                tris.AddRange(new[] { 0, u, uNext });
                tris.AddRange(new[] { u, l, lNext, u, lNext, uNext });
                tris.AddRange(new[] { 1, lNext, l });
            }

            return Finish("Organic_FacetedStar", verts, tris);
        }

        public static Mesh RibbedCylinder(float height = 1f, float radius = 0.28f, int ribs = 8, float ribDepth = 0.045f, int segments = 40, int profileSteps = 14, bool capEnds = false)
        {
            ribs = Mathf.Max(3, ribs);
            segments = Mathf.Max(12, segments);
            profileSteps = Mathf.Max(4, profileSteps);
            var profile = new Vector2[profileSteps + 1];
            for (var i = 0; i <= profileSteps; i++)
            {
                var t = i / (float)profileSteps;
                var y = Mathf.Lerp(-height * 0.5f, height * 0.5f, t);
                var flare = 1f - 0.08f * Mathf.Sin(t * Mathf.PI);
                profile[i] = new Vector2(radius * flare, y);
            }

            var verts = new List<Vector3>(profile.Length * segments);
            var tris = new List<int>();
            for (var s = 0; s < segments; s++)
            {
                var ang = (s / (float)segments) * Mathf.PI * 2f;
                var rib = 1f + ribDepth / Mathf.Max(0.01f, radius) * Mathf.Sin(ang * ribs);
                var cos = Mathf.Cos(ang);
                var sin = Mathf.Sin(ang);
                for (var p = 0; p < profile.Length; p++)
                {
                    var r = profile[p].x * rib;
                    verts.Add(new Vector3(r * cos, profile[p].y, r * sin));
                }
            }

            for (var s = 0; s < segments; s++)
            {
                var s2 = (s + 1) % segments;
                for (var p = 0; p < profile.Length - 1; p++)
                {
                    var a = s * profile.Length + p;
                    var b = s2 * profile.Length + p;
                    var c = s2 * profile.Length + p + 1;
                    var d = s * profile.Length + p + 1;
                    tris.AddRange(new[] { a, b, c, a, c, d });
                }
            }

            if (capEnds)
            {
                // Duplicate the end rings so their recalculated normals stay flat
                // and do not blend into the ribbed side wall. Traversal decks need
                // a visible, continuous top beneath their flat walkable collider.
                var topCenter = verts.Count;
                verts.Add(new Vector3(0f, height * 0.5f, 0f));
                var topRingStart = verts.Count;
                for (var s = 0; s < segments; s++)
                {
                    verts.Add(verts[s * profile.Length + profileSteps]);
                }

                var bottomCenter = verts.Count;
                verts.Add(new Vector3(0f, -height * 0.5f, 0f));
                var bottomRingStart = verts.Count;
                for (var s = 0; s < segments; s++)
                {
                    verts.Add(verts[s * profile.Length]);
                }

                for (var s = 0; s < segments; s++)
                {
                    var next = (s + 1) % segments;
                    tris.AddRange(new[] { topCenter, topRingStart + next, topRingStart + s });
                    tris.AddRange(new[] { bottomCenter, bottomRingStart + s, bottomRingStart + next });
                }
            }

            return Finish("Organic_RibbedCylinder", verts, tris);
        }

        public static Mesh SpiralShell(float turns = 2.4f, float startRadius = 0.08f, float endRadius = 0.55f, float tube = 0.07f, float height = 0.55f, int segments = 72)
        {
            turns = Mathf.Max(1f, turns);
            segments = Mathf.Max(24, segments);
            var path = new List<Vector3>(segments + 1);
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var ang = t * turns * Mathf.PI * 2f;
                var r = Mathf.Lerp(startRadius, endRadius, t * t);
                var y = Mathf.Lerp(-height * 0.35f, height * 0.55f, t);
                path.Add(new Vector3(Mathf.Cos(ang) * r, y, Mathf.Sin(ang) * r));
            }

            var tubeStart = tube * 0.55f;
            var tubeEnd = tube * 1.35f;
            return TubeAlongPathVariable(path, tubeStart, tubeEnd, 12, "Organic_SpiralShell");
        }

        public static Mesh GearRing(float major = 0.5f, float minor = 0.09f, int teeth = 12, float toothLength = 0.12f, int segments = 64, int tubes = 14)
        {
            teeth = Mathf.Max(6, teeth);
            segments = Mathf.Max(teeth * 4, segments);
            tubes = Mathf.Max(6, tubes);
            var verts = new List<Vector3>(segments * tubes);
            var tris = new List<int>(segments * tubes * 6);
            for (var i = 0; i < segments; i++)
            {
                var u = (i / (float)segments) * Mathf.PI * 2f;
                var toothPhase = (u / (Mathf.PI * 2f)) * teeth;
                var tooth = Mathf.Abs(toothPhase - Mathf.Floor(toothPhase) - 0.5f) * 2f;
                var bump = tooth < 0.35f ? toothLength * (1f - tooth / 0.35f) : 0f;
                var ringR = major + bump;
                var center = new Vector3(Mathf.Cos(u) * ringR, 0f, Mathf.Sin(u) * ringR);
                for (var j = 0; j < tubes; j++)
                {
                    var v = (j / (float)tubes) * Mathf.PI * 2f;
                    var normal = new Vector3(Mathf.Cos(u) * Mathf.Cos(v), Mathf.Sin(v), Mathf.Sin(u) * Mathf.Cos(v));
                    verts.Add(center + normal * minor);
                }
            }

            for (var i = 0; i < segments; i++)
            {
                var i2 = (i + 1) % segments;
                for (var j = 0; j < tubes; j++)
                {
                    var j2 = (j + 1) % tubes;
                    var a = i * tubes + j;
                    var b = i2 * tubes + j;
                    var c = i2 * tubes + j2;
                    var d = i * tubes + j2;
                    tris.AddRange(new[] { a, b, c, a, c, d });
                }
            }

            return Finish("Organic_GearRing", verts, tris);
        }

        public static Mesh RibbonPlane(float length = 1.2f, float width = 0.45f, float wave = 0.18f, int lengthSegs = 24, int widthSegs = 6, float curl = 0.65f)
        {
            lengthSegs = Mathf.Max(4, lengthSegs);
            widthSegs = Mathf.Max(2, widthSegs);
            var verts = new List<Vector3>((lengthSegs + 1) * (widthSegs + 1));
            var tris = new List<int>(lengthSegs * widthSegs * 6);
            for (var i = 0; i <= lengthSegs; i++)
            {
                var u = i / (float)lengthSegs;
                var x = Mathf.Lerp(-length * 0.5f, length * 0.5f, u);
                var bend = Mathf.Sin(u * Mathf.PI) * curl;
                var lift = Mathf.Sin(u * Mathf.PI * 2f) * wave;
                for (var j = 0; j <= widthSegs; j++)
                {
                    var v = j / (float)widthSegs;
                    var z = Mathf.Lerp(-width * 0.5f, width * 0.5f, v);
                    var edgeCup = (v - 0.5f) * (v - 0.5f) * 4f * wave * 0.35f;
                    verts.Add(new Vector3(x, lift + edgeCup + bend * 0.15f, z + bend));
                }
            }

            var stride = widthSegs + 1;
            for (var i = 0; i < lengthSegs; i++)
            {
                for (var j = 0; j < widthSegs; j++)
                {
                    var a = i * stride + j;
                    var b = a + 1;
                    var c = a + stride + 1;
                    var d = a + stride;
                    tris.AddRange(new[] { a, d, c, a, c, b });
                    tris.AddRange(new[] { a, c, d, a, b, c });
                }
            }

            return Finish("Organic_RibbonPlane", verts, tris);
        }

        public static Mesh HookCurve(float reach = 0.7f, float drop = 0.55f, float tube = 0.045f, int segments = 28)
        {
            segments = Mathf.Max(12, segments);
            var path = new List<Vector3>(segments + 1);
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                if (t < 0.45f)
                {
                    var u = t / 0.45f;
                    path.Add(new Vector3(0f, Mathf.Lerp(drop * 0.5f, drop, u), Mathf.Lerp(0f, reach * 0.15f, u)));
                }
                else
                {
                    var u = (t - 0.45f) / 0.55f;
                    var ang = Mathf.Lerp(0f, Mathf.PI * 1.15f, u);
                    var r = reach * 0.55f;
                    path.Add(new Vector3(Mathf.Sin(ang) * r, drop - (1f - Mathf.Cos(ang)) * r, reach * 0.15f + Mathf.Cos(ang) * r * 0.35f));
                }
            }

            return TubeAlongPath(path, tube, 10, "Organic_HookCurve");
        }

        /// <summary>Curved scrap blade for boss wind-up / claw accents.</summary>
        public static Mesh ClawBlade(float length = 0.55f, float width = 0.18f, float thickness = 0.06f, int segments = 16)
        {
            segments = Mathf.Max(8, segments);
            var path = new List<Vector3>(segments + 1);
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var flare = Mathf.Sin(t * Mathf.PI) * width;
                path.Add(new Vector3(flare * 0.15f, Mathf.Lerp(0f, -length * 0.35f, t), Mathf.Lerp(0f, length, t)));
            }

            var tipR = Mathf.Max(0.012f, thickness * 0.35f);
            return TubeAlongPathVariable(path, thickness, tipR, 8, "Organic_ClawBlade");
        }

        public static GameObject CreateMeshObject(string name, Mesh mesh, Transform parent, Vector3 localPos, Vector3 localScale, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            return go;
        }

        public static Mesh TubeAlongPath(IList<Vector3> path, float radius = 0.05f, int radial = 10, string name = "Organic_TubePath")
        {
            if (path == null || path.Count < 2)
            {
                return SoftBlob();
            }

            radial = Mathf.Max(5, radial);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (var i = 0; i < path.Count; i++)
            {
                var tangent = (i < path.Count - 1 ? path[i + 1] - path[i] : path[i] - path[i - 1]).normalized;
                var up = Vector3.up;
                if (Mathf.Abs(Vector3.Dot(tangent, up)) > 0.95f)
                {
                    up = Vector3.right;
                }

                var normal = Vector3.Cross(tangent, up).normalized;
                var binormal = Vector3.Cross(tangent, normal).normalized;
                for (var r = 0; r < radial; r++)
                {
                    var ang = (r / (float)radial) * Mathf.PI * 2f;
                    var offset = normal * Mathf.Cos(ang) * radius + binormal * Mathf.Sin(ang) * radius;
                    verts.Add(path[i] + offset);
                }
            }

            for (var i = 0; i < path.Count - 1; i++)
            {
                for (var r = 0; r < radial; r++)
                {
                    var r2 = (r + 1) % radial;
                    var a = i * radial + r;
                    var b = i * radial + r2;
                    var c = (i + 1) * radial + r2;
                    var d = (i + 1) * radial + r;
                    tris.AddRange(new[] { a, b, c, a, c, d });
                }
            }

            return Finish(name, verts, tris);
        }

        /// <summary>
        /// A thin brow ribbon that follows the white shell instead of standing
        /// proud as a cylindrical rod. Coordinates use the chest-local shell
        /// center/radius used by OrganicSplatBuilder.
        /// </summary>
        public static Mesh SplatFaceBrow(float side = 1f, int segments = 12)
        {
            segments = Mathf.Max(6, segments);
            const float shellRadius = 0.59f;
            const float shellCenterY = -0.05f;
            const float halfWidth = 0.013f;
            var vertices = new List<Vector3>((segments + 1) * 2);
            var triangles = new List<int>(segments * 6);

            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var x = side * Mathf.Lerp(0.285f, 0.12f, t);
                var y = 0.385f + Mathf.Sin(t * Mathf.PI) * 0.02f;
                var t0 = Mathf.Max(0f, t - 0.01f);
                var t1 = Mathf.Min(1f, t + 0.01f);
                var x0 = side * Mathf.Lerp(0.285f, 0.12f, t0);
                var x1 = side * Mathf.Lerp(0.285f, 0.12f, t1);
                var y0 = 0.385f + Mathf.Sin(t0 * Mathf.PI) * 0.02f;
                var y1 = 0.385f + Mathf.Sin(t1 * Mathf.PI) * 0.02f;
                var tangent = new Vector2(x1 - x0, y1 - y0).normalized;
                var normal = new Vector2(-tangent.y, tangent.x);
                var taper = Mathf.Sin(t * Mathf.PI);
                for (var edge = -1; edge <= 1; edge += 2)
                {
                    var offset = normal * (halfWidth * taper * edge);
                    var px = x + offset.x;
                    var py = y + offset.y;
                    var z2 = shellRadius * shellRadius - px * px - (py - shellCenterY) * (py - shellCenterY);
                    // The soft blob has a small procedural radius warp; leave a
                    // 26 mm relief so both mirrored arcs stay visible on it.
                    var pz = Mathf.Sqrt(Mathf.Max(0.001f, z2)) + 0.026f;
                    vertices.Add(new Vector3(px, py, pz));
                }
            }

            for (var i = 0; i < segments; i++)
            {
                var a = i * 2;
                var b = a + 1;
                var c = a + 3;
                var d = a + 2;
                // The path runs from the outer brow toward the inner brow, so
                // reverse each strip quad to face the camera-side (+Z) shell.
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(a); triangles.Add(d); triangles.Add(c);
            }

            return Finish(side < 0f ? "Splat_FaceBrow_Left" : "Splat_FaceBrow_Right", vertices, triangles);
        }

        private static Mesh TubeAlongPathVariable(List<Vector3> path, float radiusStart, float radiusEnd, int radial, string name)
        {
            if (path == null || path.Count < 2)
            {
                return SoftBlob();
            }

            radial = Mathf.Max(5, radial);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (var i = 0; i < path.Count; i++)
            {
                var t = i / (float)(path.Count - 1);
                var radius = Mathf.Lerp(radiusStart, radiusEnd, t);
                var tangent = (i < path.Count - 1 ? path[i + 1] - path[i] : path[i] - path[i - 1]).normalized;
                var up = Vector3.up;
                if (Mathf.Abs(Vector3.Dot(tangent, up)) > 0.95f)
                {
                    up = Vector3.right;
                }

                var normal = Vector3.Cross(tangent, up).normalized;
                var binormal = Vector3.Cross(tangent, normal).normalized;
                for (var r = 0; r < radial; r++)
                {
                    var ang = (r / (float)radial) * Mathf.PI * 2f;
                    var offset = normal * Mathf.Cos(ang) * radius + binormal * Mathf.Sin(ang) * radius;
                    verts.Add(path[i] + offset);
                }
            }

            for (var i = 0; i < path.Count - 1; i++)
            {
                for (var r = 0; r < radial; r++)
                {
                    var r2 = (r + 1) % radial;
                    var a = i * radial + r;
                    var b = i * radial + r2;
                    var c = (i + 1) * radial + r2;
                    var d = (i + 1) * radial + r;
                    tris.AddRange(new[] { a, b, c, a, c, d });
                }
            }

            return Finish(name, verts, tris);
        }

        private static int MidpointIndex(int a, int b, List<Vector3> verts, Dictionary<long, int> cache, float radius)
        {
            var key = ((long)Mathf.Min(a, b) << 32) + Mathf.Max(a, b);
            if (cache.TryGetValue(key, out var index))
            {
                return index;
            }

            var mid = ((verts[a] + verts[b]) * 0.5f).normalized * radius;
            index = verts.Count;
            verts.Add(mid);
            cache[key] = index;
            return index;
        }

        private static Mesh Finish(string name, List<Vector3> verts, List<int> tris)
        {
            var mesh = new Mesh { name = name, indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
