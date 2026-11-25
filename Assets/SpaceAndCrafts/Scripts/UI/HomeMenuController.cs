using SpaceAndCrafts.Art;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Title screen: Canvas brand panel + 3 save slots. New Game → full-screen story → Rainbow Kingdom.
    /// </summary>
    public sealed class HomeMenuController : MonoBehaviour
    {
        [SerializeField] private Transform titleRoot;
        [SerializeField] private Transform heroRoot;
        [SerializeField] private Transform ctaRoot;
        [SerializeField] private string playSceneName = "RainbowWorld_Arrival";
        [SerializeField] private WorldSelectBoard worldSelect;
        [SerializeField] private SaveSlotBoard saveSlots;
        [SerializeField] private bool openWithStoryOnNewGame = true;

        private Vector3 _heroRest;
        private Canvas _canvas;
        private Text[] _slotLabels = new Text[GameSlotStore.SlotCount];
        private Image[] _slotBgs = new Image[GameSlotStore.SlotCount];
        private Text _ctaLabel;
        private Button _ctaButton;
        private GameObject _optionsPanel;
        private Button _settingsButton;
        private Button _optionsBackButton;
        private Text _mouseSensitivityValue;
        private Text _gamepadSensitivityValue;
        private float _mouseSensitivity;
        private float _gamepadSensitivity;

        private static readonly Color PanelBg = new(0.1f, 0.08f, 0.22f, 0.92f);
        private static readonly Color SlotIdle = new(0.18f, 0.16f, 0.34f, 0.95f);
        private static readonly Color SlotHot = new(1f, 0.72f, 0.28f, 1f);
        private static readonly Color Brand = new(1f, 0.9f, 0.42f, 1f);
        private static readonly Color Body = new(0.96f, 0.95f, 1f, 0.96f);

        private void Awake()
        {
            if (heroRoot != null) _heroRest = heroRoot.localPosition;
            if (worldSelect == null)
            {
                worldSelect = GetComponentInChildren<WorldSelectBoard>();
            }

            if (saveSlots == null)
            {
                saveSlots = GetComponentInChildren<SaveSlotBoard>();
            }

            if (worldSelect != null)
            {
                Destroy(worldSelect);
                worldSelect = null;
            }

            var worldGo = transform.Find("WorldSelect");
            if (worldGo != null)
            {
                Destroy(worldGo.gameObject);
            }

            HideWorldText("Title");
            HideWorldText("Tagline");
            HideWorldText("CTA");
            HideWorldText("Watermark");
            HideWorldText("SaveSlot");
            HideWorldText("WorldSelect");

            if (heroRoot != null)
            {
                heroRoot.localScale = Vector3.one * 1.85f;
                SplatVisualRepair.Repair(heroRoot);
            }

            GameSlotStore.LoadIndex();
            saveSlots?.Refresh();
            _mouseSensitivity = PlayerCameraPreferences.GetMouseLookSensitivity(0.11f);
            _gamepadSensitivity = PlayerCameraPreferences.GetGamepadLookMultiplier(1.25f);
            BuildCanvas();
            RefreshSlotUi();
            RefreshOptionsUi();
        }

        private void HideWorldText(string childName)
        {
            var t = transform.Find(childName);
            if (t != null)
            {
                t.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_optionsPanel != null && _optionsPanel.activeSelf)
            {
                if (WasCancelPressed())
                {
                    CloseOptions();
                }

                return;
            }

            if (heroRoot != null)
            {
                heroRoot.localPosition = _heroRest + Vector3.up * (0.28f + Mathf.Sin(Time.time * 2.1f) * 0.12f);
                // Keep Splat facing the menu camera; use a small curious sway
                // instead of a continuous turn that hides the character's face.
                heroRoot.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.55f) * 7f, 0f);
            }

            if (WasOptionsPressed())
            {
                OpenOptions();
                return;
            }

            if (WasStartPressed())
            {
                ConfirmSlotAndPlay();
            }

            if (WasLeft())
            {
                NudgeSlot(-1);
            }
            else if (WasRight())
            {
                NudgeSlot(1);
            }
        }

        private void BuildCanvas()
        {
            _canvas = RuntimeUiFactory.EnsureOverlayCanvas("UI_HomeMenu", 50);
            var root = _canvas.transform;
            BuildApprovedSplatPortrait(root);

            // Brand-first rainbow crown — genre color before any dock chrome.
            for (var i = 0; i < 6; i++)
            {
                var t0 = 0.12f + i * 0.128f;
                RuntimeUiFactory.Panel(
                    root,
                    $"Rainbow_{i}",
                    new Vector2(t0, 0.905f),
                    new Vector2(t0 + 0.118f, 0.935f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.HSVToRGB(i / 6f, 0.85f, 1f));
            }

            var brand = RuntimeUiFactory.Label(
                root,
                "Brand",
                "SPACE & CRAFTS",
                78,
                FontStyle.Bold,
                Brand,
                TextAnchor.MiddleCenter);
            RuntimeUiFactory.Stretch(brand.rectTransform, 0f, 0f, 0f, 0f);
            brand.rectTransform.anchorMin = new Vector2(0.06f, 0.8f);
            brand.rectTransform.anchorMax = new Vector2(0.94f, 0.9f);

            var galaxy = RuntimeUiFactory.Label(
                root,
                "Galaxy",
                "OUR WAY GALAXY",
                34,
                FontStyle.Bold,
                new Color(0.55f, 0.95f, 1f),
                TextAnchor.MiddleCenter);
            RuntimeUiFactory.Stretch(galaxy.rectTransform, 0f, 0f, 0f, 0f);
            galaxy.rectTransform.anchorMin = new Vector2(0.1f, 0.74f);
            galaxy.rectTransform.anchorMax = new Vector2(0.9f, 0.8f);

            var genre = RuntimeUiFactory.Label(
                root,
                "Genre",
                "3D craft-platformer  ·  invent · bounce · build",
                22,
                FontStyle.Bold,
                new Color(1f, 0.85f, 0.45f),
                TextAnchor.MiddleCenter);
            RuntimeUiFactory.Stretch(genre.rectTransform, 0f, 0f, 0f, 0f);
            genre.rectTransform.anchorMin = new Vector2(0.1f, 0.69f);
            genre.rectTransform.anchorMax = new Vector2(0.9f, 0.74f);

            var blurb = RuntimeUiFactory.Label(
                root,
                "Blurb",
                "Your friend Vesper is beyond a broken rainbow road. Build a way to reach her.",
                20,
                FontStyle.Normal,
                Body,
                TextAnchor.MiddleCenter);
            RuntimeUiFactory.Stretch(blurb.rectTransform, 0f, 0f, 0f, 0f);
            blurb.rectTransform.anchorMin = new Vector2(0.15f, 0.64f);
            blurb.rectTransform.anchorMax = new Vector2(0.85f, 0.69f);

            // Bottom dock for saves — interaction chrome, not the hero composition.
            var panel = RuntimeUiFactory.Panel(
                root,
                "BrandPanel",
                new Vector2(0.12f, 0.04f),
                new Vector2(0.88f, 0.34f),
                Vector2.zero,
                Vector2.zero,
                PanelBg);

            RuntimeUiFactory.Panel(
                panel,
                "AccentBar",
                new Vector2(0f, 0.92f),
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector2.zero,
                new Color(1f, 0.55f, 0.22f, 1f));

            var choose = RuntimeUiFactory.Label(
                panel,
                "Choose",
                "CHOOSE A SAVE",
                18,
                FontStyle.Bold,
                new Color(0.75f, 0.88f, 1f),
                TextAnchor.MiddleCenter);
            RuntimeUiFactory.Stretch(choose.rectTransform, 0f, 0f, 0f, 0f);
            choose.rectTransform.anchorMin = new Vector2(0f, 0.78f);
            choose.rectTransform.anchorMax = new Vector2(1f, 0.92f);

            for (var i = 0; i < GameSlotStore.SlotCount; i++)
            {
                var idx = i;
                var x0 = 0.04f + i * 0.32f;
                var btn = RuntimeUiFactory.MakeButton(
                    panel,
                    $"Slot{i}",
                    $"Slot {i + 1}",
                    new Vector2(x0, 0.38f),
                    new Vector2(x0 + 0.28f, 0.74f),
                    Vector2.zero,
                    Vector2.zero,
                    SlotIdle,
                    Color.white,
                    16);
                _slotBgs[i] = btn.GetComponent<Image>();
                _slotLabels[i] = btn.GetComponentInChildren<Text>();
                if (_slotLabels[i] != null)
                {
                    _slotLabels[i].fontSize = 15;
                }

                btn.onClick.AddListener(() => SelectSlot(idx));
            }

            _ctaButton = RuntimeUiFactory.MakeButton(
                panel,
                "CTA",
                "▶  NEW GAME  →  Rainbow Kingdom",
                new Vector2(0.2f, 0.08f),
                new Vector2(0.8f, 0.32f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.95f, 0.55f, 0.2f, 1f),
                Color.white,
                22);
            _ctaLabel = _ctaButton.GetComponentInChildren<Text>();
            _ctaButton.onClick.AddListener(ConfirmSlotAndPlay);

            var hint = RuntimeUiFactory.Label(
                root,
                "Hint",
                "← → switch slot  ·  Enter / Space confirm  ·  O / TOP BUTTON settings",
                15,
                FontStyle.Normal,
                new Color(0.85f, 0.82f, 1f, 0.8f),
                TextAnchor.MiddleCenter);
            RuntimeUiFactory.Stretch(hint.rectTransform, 0f, 0f, 0f, 0f);
            hint.rectTransform.anchorMin = new Vector2(0.2f, 0f);
            hint.rectTransform.anchorMax = new Vector2(0.78f, 0.04f);

            _settingsButton = RuntimeUiFactory.MakeButton(
                root,
                "Settings",
                "SETTINGS",
                new Vector2(0.82f, 0.002f),
                new Vector2(0.98f, 0.045f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.16f, 0.14f, 0.3f, 0.95f),
                Color.white,
                14);
            _settingsButton.onClick.AddListener(OpenOptions);
            BuildOptionsPanel(root);
        }

        private void BuildApprovedSplatPortrait(Transform canvasRoot)
        {
            var portrait = Resources.Load<Sprite>("Characters/SplatApprovedCapEyePortrait")
                ?? Resources.Load<Sprite>("Characters/SplatApprovedConceptCapEye");
            if (portrait == null)
            {
                return;
            }

            // The Home Menu's old 3D placeholder predates Mac's approved design.
            // Use the cohesive portrait until a production 3D rig passes review;
            // don't leave the old silhouette visible behind it.
            if (heroRoot != null)
            {
                var legacyRenderers = heroRoot.GetComponentsInChildren<Renderer>(true);
                for (var i = 0; i < legacyRenderers.Length; i++)
                {
                    if (legacyRenderers[i] != null)
                    {
                        legacyRenderers[i].enabled = false;
                    }
                }
            }

            var artwork = RuntimeUiFactory.Panel(
                canvasRoot,
                "HomeSplatApprovedPortrait",
                new Vector2(0.30f, 0.34f),
                new Vector2(0.56f, 0.63f),
                Vector2.zero,
                Vector2.zero,
                Color.white);
            var image = artwork.GetComponent<Image>();
            image.sprite = portrait;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void BuildOptionsPanel(Transform root)
        {
            _optionsPanel = RuntimeUiFactory.Panel(
                root,
                "CameraOptionsOverlay",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(0.015f, 0.02f, 0.07f, 0.72f)).gameObject;

            var card = RuntimeUiFactory.Panel(
                _optionsPanel.transform,
                "CameraOptionsPanel",
                new Vector2(0.2f, 0.16f),
                new Vector2(0.8f, 0.84f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.08f, 0.075f, 0.18f, 0.99f));

            var title = RuntimeUiFactory.Label(
                card,
                "OptionsTitle",
                "CAMERA CONTROLS",
                30,
                FontStyle.Bold,
                Brand,
                TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.78f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.94f);

            var mouseLabel = RuntimeUiFactory.Label(
                card,
                "MouseSensitivityLabel",
                "MOUSE LOOK  ·  DEGREES PER PIXEL",
                18,
                FontStyle.Bold,
                Body,
                TextAnchor.MiddleCenter);
            mouseLabel.rectTransform.anchorMin = new Vector2(0.12f, 0.66f);
            mouseLabel.rectTransform.anchorMax = new Vector2(0.88f, 0.76f);

            var gamepadLabel = RuntimeUiFactory.Label(
                card,
                "GamepadSensitivityLabel",
                "CONTROLLER LOOK",
                18,
                FontStyle.Bold,
                Body,
                TextAnchor.MiddleCenter);
            gamepadLabel.rectTransform.anchorMin = new Vector2(0.12f, 0.4f);
            gamepadLabel.rectTransform.anchorMax = new Vector2(0.88f, 0.5f);

            CreateOptionStepper(card, "MouseSensitivity", 0.13f, 0.49f,
                out _mouseSensitivityValue, delta => AdjustMouseSensitivity(delta));
            CreateOptionStepper(card, "GamepadSensitivity", 0.13f, 0.23f,
                out _gamepadSensitivityValue, delta => AdjustGamepadSensitivity(delta));

            var saveHint = RuntimeUiFactory.Label(
                card,
                "OptionsSaveHint",
                "Changes save automatically",
                14,
                FontStyle.Normal,
                new Color(0.8f, 0.82f, 0.94f, 0.8f),
                TextAnchor.MiddleCenter);
            saveHint.rectTransform.anchorMin = new Vector2(0.12f, 0.12f);
            saveHint.rectTransform.anchorMax = new Vector2(0.88f, 0.2f);

            _optionsBackButton = RuntimeUiFactory.MakeButton(
                card,
                "OptionsBack",
                "BACK",
                new Vector2(0.34f, 0.025f),
                new Vector2(0.66f, 0.12f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.22f, 0.2f, 0.38f, 1f),
                Color.white,
                18);
            _optionsBackButton.onClick.AddListener(CloseOptions);
            _optionsPanel.SetActive(false);
        }

        private void CreateOptionStepper(Transform parent, string name, float left, float bottom,
            out Text valueLabel, System.Action<float> adjust)
        {
            var minus = RuntimeUiFactory.MakeButton(
                parent,
                name + "Minus",
                "−",
                new Vector2(left, bottom),
                new Vector2(left + 0.17f, bottom + 0.15f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.22f, 0.2f, 0.38f, 1f),
                Color.white,
                24);
            minus.onClick.AddListener(() => adjust(-1f));

            var value = RuntimeUiFactory.Label(parent, name + "Value", "", 21, FontStyle.Bold,
                new Color(0.55f, 0.95f, 1f), TextAnchor.MiddleCenter);
            value.rectTransform.anchorMin = new Vector2(left + 0.18f, bottom);
            value.rectTransform.anchorMax = new Vector2(left + 0.57f, bottom + 0.15f);
            valueLabel = value;

            var plus = RuntimeUiFactory.MakeButton(
                parent,
                name + "Plus",
                "+",
                new Vector2(left + 0.58f, bottom),
                new Vector2(left + 0.75f, bottom + 0.15f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.22f, 0.2f, 0.38f, 1f),
                Color.white,
                24);
            plus.onClick.AddListener(() => adjust(1f));
        }

        private void RefreshOptionsUi()
        {
            if (_mouseSensitivityValue != null)
            {
                _mouseSensitivityValue.text = $"{_mouseSensitivity:0.00}° / px";
            }

            if (_gamepadSensitivityValue != null)
            {
                _gamepadSensitivityValue.text = $"{_gamepadSensitivity:0.0}×";
            }
        }

        private void AdjustMouseSensitivity(float direction)
        {
            _mouseSensitivity = Mathf.Clamp(_mouseSensitivity + direction * 0.01f, 0.02f, 0.4f);
            PlayerCameraPreferences.SetMouseLookSensitivity(_mouseSensitivity);
            RefreshOptionsUi();
        }

        private void AdjustGamepadSensitivity(float direction)
        {
            _gamepadSensitivity = Mathf.Clamp(_gamepadSensitivity + direction * 0.1f, 0.5f, 2.5f);
            PlayerCameraPreferences.SetGamepadLookMultiplier(_gamepadSensitivity);
            RefreshOptionsUi();
        }

        private void OpenOptions()
        {
            if (_optionsPanel == null)
            {
                return;
            }

            _optionsPanel.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(_optionsBackButton != null ? _optionsBackButton.gameObject : null);
        }

        private void CloseOptions()
        {
            if (_optionsPanel != null)
            {
                _optionsPanel.SetActive(false);
            }

            EventSystem.current?.SetSelectedGameObject(_settingsButton != null ? _settingsButton.gameObject : null);
        }

        private void SelectSlot(int idx)
        {
            while (saveSlots != null && saveSlots.Selected != idx)
            {
                NudgeSlot(idx > saveSlots.Selected ? 1 : -1);
            }

            RefreshSlotUi();
        }

        private void RefreshSlotUi()
        {
            var selected = saveSlots != null ? saveSlots.Selected : 0;
            for (var i = 0; i < GameSlotStore.SlotCount; i++)
            {
                if (_slotLabels[i] == null)
                {
                    continue;
                }

                var occupied = GameSlotStore.Index.slots[i].occupied;
                _slotLabels[i].text = occupied
                    ? GameSlotStore.DescribeSlot(i)
                    : $"Slot {i + 1}  ·  Empty — New Game";
                if (_slotBgs[i] != null)
                {
                    _slotBgs[i].color = i == selected ? SlotHot : SlotIdle;
                    _slotLabels[i].color = i == selected ? new Color(0.12f, 0.08f, 0.05f) : Color.white;
                }
            }

            var empty = saveSlots == null || saveSlots.SelectedIsEmpty;
            if (_ctaLabel != null)
            {
                _ctaLabel.text = empty ? "▶  NEW GAME  →  Rainbow Kingdom" : "▶  CONTINUE";
            }
        }

        private void NudgeSlot(int delta)
        {
            saveSlots?.Nudge(delta);
            RefreshSlotUi();
        }

        public void ConfirmSlotAndPlay()
        {
            var slot = saveSlots != null ? saveSlots.Selected : 0;
            if (saveSlots == null || saveSlots.SelectedIsEmpty)
            {
                GameSlotStore.StartNewGame(slot, $"Splat {slot + 1}");
                if (openWithStoryOnNewGame)
                {
                    BeginOpeningStory();
                    return;
                }

                LoadArrival();
                return;
            }

            if (!GameSlotStore.ContinueSlot(slot))
            {
                GameSlotStore.StartNewGame(slot, $"Splat {slot + 1}");
                BeginOpeningStory();
                return;
            }

            var progress = PlayerProgressSave.Current;
            var resumeScene = progress.continueSceneId;
            if (!string.IsNullOrWhiteSpace(resumeScene)
                && Application.CanStreamedLevelBeLoaded(resumeScene))
            {
                SceneManager.LoadScene(resumeScene);
                return;
            }

            var world = progress.lastPlayedWorldId;
            if (!string.IsNullOrWhiteSpace(world)
                && progress.completedWorldIds != null
                && progress.completedWorldIds.Contains(world)
                && Application.CanStreamedLevelBeLoaded("WorkshopHub"))
            {
                SceneManager.LoadScene("WorkshopHub");
                return;
            }

            if (!string.IsNullOrEmpty(world) && Application.CanStreamedLevelBeLoaded(world))
            {
                SceneManager.LoadScene(world);
                return;
            }

            LoadArrival();
        }

        public void PlayArrival() => ConfirmSlotAndPlay();

        private void BeginOpeningStory()
        {
            HideHomeMenuForStory();
            OpeningStoryPresenter.BeginFromMenu();
        }

        /// <summary>Open the story presentation without selecting or creating a save slot.</summary>
        public void PreviewOpeningStoryWithoutSave()
        {
            HideHomeMenuForStory();
            OpeningStoryPresenter.BeginFromMenu();
        }

        private void HideHomeMenuForStory()
        {
            if (titleRoot != null) titleRoot.gameObject.SetActive(false);
            if (heroRoot != null) heroRoot.gameObject.SetActive(false);
            if (ctaRoot != null) ctaRoot.gameObject.SetActive(false);
            var watermark = transform.Find("Watermark");
            if (watermark != null) watermark.gameObject.SetActive(false);
            var saveLabel = transform.Find("SaveSlot");
            if (saveLabel != null) saveLabel.gameObject.SetActive(false);
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
            }

            enabled = false;
        }

        private void LoadArrival()
        {
            if (!string.IsNullOrEmpty(playSceneName) && Application.CanStreamedLevelBeLoaded(playSceneName))
            {
                SceneManager.LoadScene(playSceneName);
                return;
            }

            if (worldSelect != null)
            {
                worldSelect.ConfirmSelection();
                return;
            }

            SceneManager.LoadScene("RainbowWorld_Arrival");
        }

        private static bool WasStartPressed()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0))
            {
                return true;
            }

            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            {
                return true;
            }

            var pad = Gamepad.current;
            return pad != null && pad.buttonSouth.wasPressedThisFrame;
        }

        private static bool WasCancelPressed()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                return true;
            }

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            var pad = Gamepad.current;
            return pad != null && pad.buttonEast.wasPressedThisFrame;
        }

        private static bool WasOptionsPressed()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                return true;
            }

            var kb = Keyboard.current;
            if (kb != null && kb.oKey.wasPressedThisFrame)
            {
                return true;
            }

            var pad = Gamepad.current;
            return pad != null && pad.buttonNorth.wasPressedThisFrame;
        }

        private static bool WasLeft()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                return true;
            }

            var kb = Keyboard.current;
            return kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame);
        }

        private static bool WasRight()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                return true;
            }

            var kb = Keyboard.current;
            return kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame);
        }
    }
}
