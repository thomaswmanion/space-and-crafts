using System.Collections.Generic;
using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Subdivided, edge-softened walkable slab (handmade shelf read). Not a bare Unity cube.
    /// Still replaceable by commissioned modular floor kits.
    /// </summary>
    public static class OrganicSlabMesh
    {
        public static Mesh Create(float chamfer = 0.1f, int edgeDivs = 4, float lipNoise = 0.012f)
        {
            chamfer = Mathf.Clamp(chamfer, 0.04f, 0.22f);
            edgeDivs = Mathf.Clamp(edgeDivs, 2, 8);
            var baseMesh = TempChamferBoxMesh.Create(chamfer);
            var src = baseMesh.vertices;
            var srcTris = baseMesh.triangles;

            // Subdivide each triangle edgeDivs times for smoother normals & painted lip.
            var verts = new List<Vector3>(src);
            var tris = new List<int>(srcTris);
            for (var pass = 0; pass < Mathf.Min(2, edgeDivs - 1); pass++)
            {
                var next = new List<int>(tris.Count * 4);
                var midCache = new Dictionary<long, int>();
                for (var i = 0; i < tris.Count; i += 3)
                {
                    var a = tris[i];
                    var b = tris[i + 1];
                    var c = tris[i + 2];
                    var ab = Mid(a, b, verts, midCache);
                    var bc = Mid(b, c, verts, midCache);
                    var ca = Mid(c, a, verts, midCache);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }

                tris = next;
            }

            var v = verts.ToArray();
            for (var i = 0; i < v.Length; i++)
            {
                // Soften top lip and add tiny handmade irregularity on upper faces.
                if (v[i].y > 0.15f)
                {
                    var n = Mathf.Sin(v[i].x * 17.3f + v[i].z * 11.1f) * lipNoise;
                    v[i].y += n;
                    var radial = new Vector3(v[i].x, 0f, v[i].z);
                    if (radial.magnitude > 0.01f)
                    {
                        v[i] += radial.normalized * (n * 0.35f);
                    }
                }
            }

            var mesh = new Mesh
            {
                name = "Organic_Slab",
                indexFormat = v.Length > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };
            mesh.SetVertices(v);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static int Mid(int a, int b, List<Vector3> verts, Dictionary<long, int> cache)
        {
            var key = ((long)Mathf.Min(a, b) << 32) + Mathf.Max(a, b);
            if (cache.TryGetValue(key, out var idx))
            {
                return idx;
            }

            idx = verts.Count;
            verts.Add((verts[a] + verts[b]) * 0.5f);
            cache[key] = idx;
            return idx;
        }
    }
}
