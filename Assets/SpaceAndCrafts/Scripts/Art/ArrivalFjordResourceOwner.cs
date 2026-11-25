using System.Collections.Generic;
using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Owns the runtime-only meshes and materials created for Arrival's fjord backdrop.
    /// </summary>
    public sealed class ArrivalFjordResourceOwner : MonoBehaviour
    {
        private readonly HashSet<Mesh> _meshes = new();
        private readonly HashSet<Material> _materials = new();

        public void Register(Mesh mesh, Material material)
        {
            if (mesh != null)
            {
                _meshes.Add(mesh);
            }

            if (material != null)
            {
                _materials.Add(material);
            }
        }

        private void OnDestroy()
        {
            foreach (var mesh in _meshes)
            {
                DestroyOwned(mesh);
            }

            foreach (var material in _materials)
            {
                DestroyOwned(material);
            }

            _meshes.Clear();
            _materials.Clear();
        }

        private static void DestroyOwned(Object resource)
        {
            if (resource == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(resource);
            }
            else
            {
                DestroyImmediate(resource);
            }
        }
    }
}
