using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.Player;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Compact screen-space prompt driven by the same proximity focus as interaction.
    /// </summary>
    public sealed class InteractPromptHud : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new("InteractPromptHud.Update");
        [SerializeField] private SplatInteractor interactor;

        private GameObject _promptRoot;
        private Text _promptText;
        private Text _recipeHintText;
        private SplatInputReader _input;
        private bool _showingRecipeHint;

        public void Bind(SplatInteractor target)
        {
            interactor = target;
            _input = interactor != null ? interactor.GetComponent<SplatInputReader>() : null;
        }

        private void Awake()
        {
            if (interactor == null)
            {
                interactor = GetComponent<SplatInteractor>()
                    ?? UnityEngine.Object.FindAnyObjectByType<SplatInteractor>();
            }

            if (_input == null && interactor != null)
            {
                _input = interactor.GetComponent<SplatInputReader>();
            }

            BuildUi();
        }

        private void Update()
        {
            using (UpdateMarker.Auto()) UpdateCore();
        }

        private void UpdateCore()
        {
            if (_promptRoot == null)
            {
                return;
            }

            var focus = interactor != null ? interactor.CurrentFocus : null;
            if (focus == null || !focus.CanInteract || focus is CollectiblePickup)
            {
                _promptRoot.SetActive(false);
                return;
            }

            var action = focus is BuildSite buildSite ? buildSite.PromptLabel : "Interact";
            var showRecipeHint = focus is BuildSite recipeSite && recipeSite.CanCycleRecipes;
            _promptText.text = $"<color=#FFD47A>{GetInteractBindings()}</color>   <color=#EAF1FF>{action}</color>";
            if (showRecipeHint && _recipeHintText != null)
            {
                _recipeHintText.text = $"<color=#FFD47A>{GetCycleRecipeBindings()}</color>   <color=#B9C5D8>Change recipe</color>";
            }
            if (_showingRecipeHint != showRecipeHint)
            {
                _showingRecipeHint = showRecipeHint;
                if (_recipeHintText != null) _recipeHintText.gameObject.SetActive(showRecipeHint);
                var primaryRect = _promptText.rectTransform;
                primaryRect.anchorMin = showRecipeHint ? new Vector2(0.04f, 0.47f) : new Vector2(0.04f, 0.08f);
                primaryRect.anchorMax = showRecipeHint ? new Vector2(0.96f, 0.97f) : new Vector2(0.96f, 0.92f);
                primaryRect.offsetMin = new Vector2(0f, 2f);
                primaryRect.offsetMax = new Vector2(0f, -2f);
            }
            _promptRoot.SetActive(true);
        }

        private void BuildUi()
        {
            var canvas = RuntimeUiFactory.EnsureOverlayCanvas("UI_InteractPromptHud", 90);
            var panel = RuntimeUiFactory.Panel(
                canvas.transform,
                "InteractPrompt",
                new Vector2(0.28f, 0.105f),
                new Vector2(0.72f, 0.17f),
                Vector2.zero,
                Vector2.zero,
                Color.white);
            RuntimeUiFactory.ApplyCompactHudFrame(panel.GetComponent<Image>(), 0.97f);
            _promptRoot = panel.gameObject;

            _promptText = RuntimeUiFactory.Label(
                panel,
                "PromptText",
                "",
                18,
                FontStyle.Bold,
                new Color(0.98f, 0.97f, 1f),
                TextAnchor.MiddleCenter,
                wrap: false);
            RuntimeUiFactory.Stretch(_promptText.rectTransform, 18f, 4f, 18f, 4f);
            _promptText.resizeTextForBestFit = true;
            _promptText.resizeTextMinSize = 15;
            _promptText.resizeTextMaxSize = 18;
            _recipeHintText = RuntimeUiFactory.Label(
                panel,
                "RecipeHintText",
                "",
                14,
                FontStyle.Normal,
                new Color(0.77f, 0.82f, 0.89f),
                TextAnchor.MiddleCenter,
                wrap: false);
            _recipeHintText.rectTransform.anchorMin = new Vector2(0.04f, 0.03f);
            _recipeHintText.rectTransform.anchorMax = new Vector2(0.96f, 0.48f);
            _recipeHintText.rectTransform.offsetMin = Vector2.zero;
            _recipeHintText.rectTransform.offsetMax = Vector2.zero;
            _recipeHintText.resizeTextForBestFit = true;
            _recipeHintText.resizeTextMinSize = 12;
            _recipeHintText.resizeTextMaxSize = 14;
            _recipeHintText.raycastTarget = false;
            _recipeHintText.gameObject.SetActive(false);
            _promptRoot.SetActive(false);
        }

        private string GetInteractBindings()
        {
            return GetActiveBindingHint("Interact", "E", "West");
        }

        private string GetCycleRecipeBindings()
        {
            return GetActiveBindingHint("CycleRecipe", "Q", "RB");
        }

        private string GetActiveBindingHint(string actionName, string keyboardFallback, string gamepadFallback)
        {
            var bindingGroup = _input != null ? _input.ActiveBindingGroup : "Keyboard&Mouse";
            var hint = _input != null ? _input.GetBindingHint(actionName, bindingGroup) : string.Empty;
            if (!string.IsNullOrWhiteSpace(hint))
            {
                return hint;
            }

            return bindingGroup == "Gamepad" ? gamepadFallback : keyboardFallback;
        }
    }
}
