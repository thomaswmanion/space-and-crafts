using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Compatibility wrapper — Boing silhouette now builds organically (no Unity primitives).
    /// </summary>
    public static class TempBoingSilhouette
    {
        public static void Build(Transform root, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            OrganicBoingBuilder.Build(root, applyMat);
        }
    }
}
