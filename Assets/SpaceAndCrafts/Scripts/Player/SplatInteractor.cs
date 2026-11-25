using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using Unity.Profiling;
using UnityEngine;

namespace SpaceAndCrafts.Player
{
    /// <summary>
    /// Proximity interaction entry point for collect / activate / carry.
    /// Owns the shared focus target so HUD and press use the same pick.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class SplatInteractor : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new("SplatInteractor.Update");
        [SerializeField] private SplatInputReader input;
        [SerializeField] private Transform overlapOrigin;
        [SerializeField] private float radius = 1.4f;
        [SerializeField] private LayerMask interactableMask = ~0;

        private readonly Collider[] _hits = new Collider[32];
        private SplatSkeletonAnimator _skeleton;
        private SplatMeshyAnimatorDriver _meshyAnimator;

        public Interactable CurrentFocus { get; private set; }
        public float Radius => radius;
        public LayerMask InteractableMask => interactableMask;

        private void Awake()
        {
            _skeleton = GetComponent<SplatSkeletonAnimator>();
            _meshyAnimator = SplatMeshyAnimatorDriver.FindForPlayer(transform);
        }

        private void Update()
        {
            using (UpdateMarker.Auto()) UpdateCore();
        }

        private void UpdateCore()
        {
            if (_meshyAnimator == null || !_meshyAnimator.isActiveAndEnabled)
                _meshyAnimator = SplatMeshyAnimatorDriver.FindForPlayer(transform);

            RefreshFocus();

            // Recipe selection is a separate input so a ready Standard recipe never
            // builds before the player can intentionally choose an optional upgrade.
            if (input != null && input.CycleRecipePressedThisFrame
                && CurrentFocus is BuildSite buildSite && buildSite.CanCycleRecipes)
            {
                buildSite.CycleRecipe(gameObject);
                return;
            }

            if (input == null || !input.InteractPressedThisFrame || CurrentFocus == null)
            {
                return;
            }

            if (_skeleton != null && _skeleton.enabled)
            {
                _skeleton.PlayAction(SplatSkeletonAnimator.Action.Interact, 0.4f);
            }
            else
            {
                _meshyAnimator?.TriggerInteraction();
            }

            // Start the generic reach first so a pickup, craft site, or other target
            // can replace it with a more specific reaction during its interaction.
            CurrentFocus.Interact(gameObject);
        }

        private void RefreshFocus()
        {
            CurrentFocus = null;
            var origin = overlapOrigin != null ? overlapOrigin.position : transform.position;
            var count = Physics.OverlapSphereNonAlloc(
                origin, radius, _hits, interactableMask, QueryTriggerInteraction.Collide);
            var bestDist = float.MaxValue;
            var bestCenterDist = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit == null)
                {
                    continue;
                }

                // Authored props often put a collider on a child while the shared
                // interaction component lives on the visible root object.
                var interactable = hit.GetComponentInParent<Interactable>();
                if (interactable == null || !interactable.CanInteract)
                {
                    continue;
                }

                // Rank by the actual collider surface so a large nearby prop does not
                // lose focus just because its pivot sits farther from Splat.
                var surfaceDist = Vector3.Distance(hit.ClosestPoint(origin), origin);
                var centerDist = Vector3.Distance(interactable.transform.position, origin);
                // Once the player is inside a large trigger, ClosestPoint returns
                // the player's position and several unrelated pickups all score 0.
                // A small center-distance term gives the actual nearby item focus
                // without letting an oversized collider dominate the interaction.
                var dist = surfaceDist + centerDist * 0.2f;
                if (dist < bestDist || (Mathf.Approximately(dist, bestDist) && centerDist < bestCenterDist))
                {
                    bestDist = dist;
                    bestCenterDist = centerDist;
                    CurrentFocus = interactable;
                }
            }
        }
    }
}
