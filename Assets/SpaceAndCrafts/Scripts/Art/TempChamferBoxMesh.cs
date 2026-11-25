using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// TEMP handmade slab mesh (unit box with top edge chamfer). Not final art.
    /// </summary>
    public static class TempChamferBoxMesh
    {
        public static Mesh Create(float chamfer = 0.08f)
        {
            chamfer = Mathf.Clamp(chamfer, 0.02f, 0.2f);
            var c = chamfer;
            var mesh = new Mesh { name = "TEMP_ChamferBox" };

            // Unit cube centered at origin, extents 0.5. Top rim inset by chamfer for a cut-slab look.
            var v = new Vector3[]
            {
                // Bottom
                new(-0.5f, -0.5f, -0.5f),
                new(0.5f, -0.5f, -0.5f),
                new(0.5f, -0.5f, 0.5f),
                new(-0.5f, -0.5f, 0.5f),
                // Outer top before chamfer ring
                new(-0.5f, 0.5f - c, -0.5f),
                new(0.5f, 0.5f - c, -0.5f),
                new(0.5f, 0.5f - c, 0.5f),
                new(-0.5f, 0.5f - c, 0.5f),
                // Inset top
                new(-0.5f + c, 0.5f, -0.5f + c),
                new(0.5f - c, 0.5f, -0.5f + c),
                new(0.5f - c, 0.5f, 0.5f - c),
                new(-0.5f + c, 0.5f, 0.5f - c),
            };

            var tris = new int[]
            {
                // Bottom
                0, 2, 1, 0, 3, 2,
                // Sides
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7,
                // Chamfer ring
                4, 5, 9, 4, 9, 8,
                5, 6, 10, 5, 10, 9,
                6, 7, 11, 6, 11, 10,
                7, 4, 8, 7, 8, 11,
                // Top
                8, 9, 10, 8, 10, 11,
            };

            mesh.vertices = v;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
