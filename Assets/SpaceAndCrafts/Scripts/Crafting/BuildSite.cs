using System.Collections;
using System.Collections.Generic;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.UI;
using UnityEngine;
using UnityEngine.Events;

namespace SpaceAndCrafts.Crafting
{
    /// <summary>
    /// Adventure crafting: choose recipe variants and assemble each invention once its parts are ready.
    /// </summary>
    public sealed class BuildSite : Interactable
    {
        [SerializeField] private string inventionId = "boing_o_matic";
        [SerializeField] private List<string> requiredCollectibleIds = new()
        {
            "component_spring",
            "component_metal_dish",
            "component_power_crystal"
        };
        [SerializeField] private List<BuildRecipe> recipes = new();
        [SerializeField] private int selectedRecipeIndex;
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private GameObject assembledInventionRoot;
        [SerializeField] private BuildAssemblyPresenter assemblyPresenter;
        [SerializeField] private float assemblyDuration = 2.1f;
        [SerializeField] private float refuseShakeDuration = 0.35f;
        [SerializeField] private float refuseShakeStrength = 0.12f;
        [SerializeField] private UnityEvent assemblyStarted;
        [SerializeField] private UnityEvent assemblyFinished;
        [SerializeField] private UnityEvent assemblyRefused;

        private readonly HashSet<string> _builtInventionIds = new();
        private bool _building;
        private bool _shaking;
        private Vector3 _restLocalPos;

        public override bool CanInteract => base.CanInteract && !_building && HasUnbuiltRecipe();

        public string PromptLabel
        {
            get
            {
                var recipe = GetSelectedRecipe();
                if (recipe == null)
                {
                    return "Build Boing-O-Matic";
                }

                if (IsBuilt(recipe))
                {
                    return $"{recipe.displayName} already built";
                }

                if (inventory == null)
                {
                    return $"Build {recipe.displayName} — collect its parts first";
                }

                if (inventory.HasAll(recipe.requiredCollectibleIds))
                {
                    return $"Build {recipe.displayName} · Ready";
                }

                var missing = CollectibleNaming.BuildMissingMessage(recipe.requiredCollectibleIds, inventory.Has);
                return $"Build {recipe.displayName} — {missing}";
            }
        }

        public bool CanCycleRecipes => CanInteract && recipes != null && recipes.Count > 1;

        public BuildRecipe GetSelectedRecipe()
        {
            EnsureLegacyRecipe();
            if (recipes.Count == 0)
            {
                return null;
            }

            selectedRecipeIndex = Mathf.Clamp(selectedRecipeIndex, 0, recipes.Count - 1);
            return recipes[selectedRecipeIndex];
        }

        public void ConfigureRecipes(List<BuildRecipe> nextRecipes, int selectIndex = 0)
        {
            recipes = nextRecipes ?? new List<BuildRecipe>();
            EnsureLegacyRecipe();
            selectedRecipeIndex = Mathf.Clamp(selectIndex, 0, Mathf.Max(0, recipes.Count - 1));
        }

        public void CycleRecipe(GameObject interactor)
        {
            if (!CanCycleRecipes)
            {
                return;
            }

            if (inventory == null && interactor != null)
            {
                inventory = interactor.GetComponentInParent<InventoryComponent>();
            }

            selectedRecipeIndex = (selectedRecipeIndex + 1) % recipes.Count;
            var recipe = GetSelectedRecipe();
            if (recipe == null)
            {
                return;
            }

            var readiness = IsBuilt(recipe)
                ? " — already built"
                : inventory == null
                ? string.Empty
                : inventory.HasAll(recipe.requiredCollectibleIds)
                    ? " — Ready to build!"
                    : $" — {CollectibleNaming.BuildMissingMessage(recipe.requiredCollectibleIds, inventory.Has)}";
            GameplayFeedback.Toast($"Selected {recipe.displayName}{readiness}");
        }

        private void Awake()
        {
            _restLocalPos = transform.localPosition;
            if (inventory == null)
            {
                inventory = UnityEngine.Object.FindAnyObjectByType<InventoryComponent>();
            }

            if (assemblyPresenter == null)
            {
                assemblyPresenter = GetComponentInChildren<BuildAssemblyPresenter>(true);
            }

            EnsureLegacyRecipe();
            RestoreBuiltRecipes();
        }

        private void RestoreBuiltRecipes()
        {
            if (!PlayerProgressSave.ShouldRestoreSessionProgress)
            {
                return;
            }

            var savedInventions = PlayerProgressSave.Current?.unlockedInventionIds;
            if (savedInventions == null || savedInventions.Count == 0)
            {
                return;
            }

            BuildRecipe restoredFlavor = null;
            for (var savedIndex = 0; savedIndex < savedInventions.Count; savedIndex++)
            {
                var savedId = savedInventions[savedIndex];
                if (string.IsNullOrWhiteSpace(savedId))
                {
                    continue;
                }

                for (var recipeIndex = 0; recipeIndex < recipes.Count; recipeIndex++)
                {
                    var recipe = recipes[recipeIndex];
                    if (recipe == null || string.IsNullOrWhiteSpace(recipe.inventionId))
                    {
                        continue;
                    }

                    if (string.Equals(recipe.inventionId, savedId, System.StringComparison.Ordinal))
                    {
                        _builtInventionIds.Add(savedId);
                        restoredFlavor = recipe;
                    }
                }
            }

            if (restoredFlavor == null)
            {
                return;
            }

            ApplyRecipeToInvention(restoredFlavor);
            if (assembledInventionRoot != null && !assembledInventionRoot.activeSelf)
            {
                assembledInventionRoot.SetActive(true);
            }

            for (var i = 0; i < recipes.Count; i++)
            {
                if (recipes[i] != null && !IsBuilt(recipes[i]))
                {
                    selectedRecipeIndex = i;
                    break;
                }
            }

            SetCanInteract(HasUnbuiltRecipe());
        }

        private void EnsureLegacyRecipe()
        {
            if (recipes.Count > 0)
            {
                return;
            }

            recipes.Add(new BuildRecipe
            {
                displayName = "Standard Boing",
                inventionId = inventionId,
                requiredCollectibleIds = new List<string>(requiredCollectibleIds),
                launchToast = "BOING!"
            });
        }

        protected override void HandleInteract(GameObject interactor)
        {
            if (inventory == null)
            {
                inventory = interactor.GetComponentInParent<InventoryComponent>();
            }

            if (inventory == null)
            {
                GameplayFeedback.Toast("Hmm… no craft pouch?");
                RefuseFeedback();
                return;
            }

            EnsureLegacyRecipe();
            var recipe = GetSelectedRecipe();
            if (recipe == null)
            {
                RefuseFeedback();
                return;
            }

            if (!inventory.HasAll(recipe.requiredCollectibleIds))
            {
                GameplayFeedback.Toast(
                    $"{recipe.displayName}: {CollectibleNaming.BuildMissingMessage(recipe.requiredCollectibleIds, inventory.Has)}");
                RefuseFeedback();
                return;
            }

            if (IsBuilt(recipe))
            {
                GameplayFeedback.Toast($"{recipe.displayName} is already built. Choose another recipe.");
                return;
            }

            StartCoroutine(AssembleRoutine(recipe));
        }

        private void RefuseFeedback()
        {
            GameEvents.RaiseBuildRefused();
            assemblyRefused?.Invoke();
            TempCraftSparkBurst.Spawn(transform.position + Vector3.up * 0.8f, 5, 0.28f);
            if (!_shaking)
            {
                StartCoroutine(ShakeRoutine());
            }
        }

        private IEnumerator ShakeRoutine()
        {
            _shaking = true;
            var elapsed = 0f;
            while (elapsed < refuseShakeDuration)
            {
                elapsed += Time.deltaTime;
                var offset = Random.insideUnitSphere * refuseShakeStrength;
                offset.y = 0f;
                transform.localPosition = _restLocalPos + offset;
                yield return null;
            }

            transform.localPosition = _restLocalPos;
            _shaking = false;
        }

        private IEnumerator AssembleRoutine(BuildRecipe recipe)
        {
            _building = true;
            // Set recipe flavor before the presenter reveals its trigger, so Turbo is
            // already configured on the first frame the invention becomes visible.
            ApplyRecipeToInvention(recipe);
            assemblyStarted?.Invoke();

            if (assemblyPresenter != null && assemblyPresenter.isActiveAndEnabled)
            {
                // UnityEvent wiring remains authorable, while this fallback makes the
                // build site robust if the presenter listener was omitted from a scene.
                if (!assemblyPresenter.IsAssembling)
                {
                    assemblyPresenter.PlayAssembly();
                }

                while (assemblyPresenter.IsAssembling)
                {
                    yield return null;
                }
            }
            else if (assemblyDuration > 0f)
            {
                yield return new WaitForSeconds(assemblyDuration);
            }

            if (assembledInventionRoot != null && !assembledInventionRoot.activeSelf)
            {
                assembledInventionRoot.SetActive(true);
            }

            _builtInventionIds.Add(recipe.inventionId);
            _building = false;
            SetCanInteract(HasUnbuiltRecipe());
            assemblyFinished?.Invoke();
            GameEvents.RaiseBuildCompleted(recipe.inventionId);
            GameplayFeedback.Toast(
                $"{recipe.displayName} assembled!",
                playBlip: TempSfxPlayer.Instance == null);
        }

        private void ApplyRecipeToInvention(BuildRecipe recipe)
        {
            if (assembledInventionRoot == null || recipe == null)
            {
                return;
            }

            var boing = assembledInventionRoot.GetComponentInChildren<BoingOMatic>(true);
            if (boing != null)
            {
                boing.ApplyRecipeFlavor(recipe);
            }
        }

        private bool HasUnbuiltRecipe()
        {
            if (recipes == null || recipes.Count == 0)
            {
                return true;
            }

            for (var i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                if (recipe != null && !IsBuilt(recipe))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsBuilt(BuildRecipe recipe)
        {
            if (recipe == null || string.IsNullOrWhiteSpace(recipe.inventionId))
            {
                return false;
            }

            foreach (var builtId in _builtInventionIds)
            {
                if (InventionCompatibility.Satisfies(recipe.inventionId, builtId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
