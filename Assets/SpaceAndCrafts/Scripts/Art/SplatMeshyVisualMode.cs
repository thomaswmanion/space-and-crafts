using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Explicit opt-in for the Meshy Splat child visual. With this component enabled on
    /// the player root, visual repair preserves ART_MeshySplatVisual and leaves the
    /// Organic child available but inactive as a rollback option. Absence keeps the
    /// existing Organic Splat behavior unchanged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SplatMeshyVisualMode : MonoBehaviour
    {
        public const string MeshyVisualRootName = "ART_MeshySplatVisual";

        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int GlossinessId = Shader.PropertyToID("_Glossiness");
        private static readonly int BumpScaleId = Shader.PropertyToID("_BumpScale");

        [Header("Soft toy finish")]
        [SerializeField, Range(0f, 1f)] private float surfaceSmoothness = 0.08f;
        [SerializeField, Range(0f, 1f)] private float surfaceNormalStrength = 0.10f;
        [SerializeField] private string preferredAnimatorControllerName = "Splat_Iter1858_Playable";

        private MaterialPropertyBlock _surfaceBlock;
        private Transform _styledVisual;
        private bool _surfaceStyleApplied;

        public static bool IsEnabledOn(Transform playerRoot)
        {
            if (playerRoot == null)
            {
                return false;
            }

            var mode = playerRoot.GetComponent<SplatMeshyVisualMode>();
            return mode != null && mode.enabled;
        }

        /// <summary>
        /// Resolve the promoted rig even when a serialized prefab instance kept
        /// its source asset name. The explicit mode marker is the opt-in; a
        /// skinned, animated non-organic child is the safe fallback.
        /// </summary>
        public static Transform ResolveVisual(Transform playerRoot)
        {
            if (playerRoot == null) return null;
            var mode = playerRoot.GetComponent<SplatMeshyVisualMode>();
            var preferredName = mode != null ? mode.preferredAnimatorControllerName : string.Empty;
            Transform fallback = null;
            for (var i = 0; i < playerRoot.childCount; i++)
            {
                var child = playerRoot.GetChild(i);
                if (child.name == OrganicSplatBuilder.VisualRootName) continue;
                var animator = child.GetComponent<Animator>() ?? child.GetComponentInChildren<Animator>(true);
                if (animator == null || animator.runtimeAnimatorController == null) continue;
                var hasSkinnedMesh = false;
                foreach (var skin in child.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (skin != null && skin.sharedMesh != null && skin.bones != null && skin.bones.Length > 0)
                    {
                        hasSkinnedMesh = true;
                        break;
                    }
                if (!hasSkinnedMesh) continue;
                if (!string.IsNullOrWhiteSpace(preferredName)
                    && animator.runtimeAnimatorController.name == preferredName)
                {
                    if (child.name != MeshyVisualRootName) child.name = MeshyVisualRootName;
                    return child;
                }

                var rollback = child.name.IndexOf("Rollback", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || child.name.IndexOf("ReviewOnly", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (!rollback && fallback == null)
                    fallback = child;
            }
            if (fallback != null && fallback.name != MeshyVisualRootName)
                fallback.name = MeshyVisualRootName;
            return fallback;
        }

        private void LateUpdate()
        {
            var visual = ResolveVisual(transform);
            if (visual != _styledVisual)
            {
                _styledVisual = visual;
                _surfaceStyleApplied = false;
            }

            if (!_surfaceStyleApplied)
            {
                ApplySoftToyFinish(visual);
            }
        }

        private void ApplySoftToyFinish(Transform visual)
        {
            if (visual == null)
            {
                return;
            }

            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            _surfaceBlock ??= new MaterialPropertyBlock();
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (renderer == null)
                {
                    continue;
                }

                var materials = renderer.sharedMaterials;
                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    var material = materials[materialIndex];
                    if (material == null)
                    {
                        continue;
                    }

                    _surfaceBlock.Clear();
                    renderer.GetPropertyBlock(_surfaceBlock, materialIndex);
                    if (material.HasProperty(SmoothnessId))
                    {
                        _surfaceBlock.SetFloat(SmoothnessId, surfaceSmoothness);
                    }

                    // Unity's legacy Standard shader uses Glossiness for the
                    // same finish control; set it only when the imported shader
                    // exposes that property.
                    if (material.HasProperty(GlossinessId))
                    {
                        _surfaceBlock.SetFloat(GlossinessId, surfaceSmoothness);
                    }

                    if (material.HasProperty(BumpScaleId))
                    {
                        _surfaceBlock.SetFloat(BumpScaleId, surfaceNormalStrength);
                    }

                    renderer.SetPropertyBlock(_surfaceBlock, materialIndex);
                }
            }

            _surfaceStyleApplied = true;
        }
    }
}
