using System.Collections.Generic;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.Player;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Persistent “what do I do?” coach for Arrival — genre + next step always on screen.
    /// </summary>
    public sealed class ArrivalObjectiveCoach : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new("ArrivalObjectiveCoach.Update");
        private Text _goal;
        private Text _tip;
        private Text _sideQuestLine;
        private CanvasGroup _tipCanvasGroup;
        private SplatMotor _splatMotor;
        private SplatInputReader _inputReader;
        private string _lastControlHintGroup;
        private RectTransform _uiRoot;
        private Image[] _partPips = new Image[3];
        private static readonly string[] RequiredPartIds =
        {
            "component_spring",
            "component_metal_dish",
            "component_power_crystal"
        };
        private static readonly string[] RequiredPartIconResources =
        {
            "UI/arrival-part-spring",
            "UI/arrival-part-dish",
            "UI/arrival-part-crystal"
        };
        private static Sprite[] _requiredPartSprites;
        private readonly HashSet<string> _collectedPartIds = new();
        private int _parts;
        private bool _hasSecretBadge;
        private bool _crafted;
        private bool _craftedTurbo;
        private WorldQuestDirector _questDirector;
        private bool _tipDismissalStarted;
        private float _tipMovementDuration;
        private float _tipDismissDelay;

        public string GoalCopyForDiagnostics => _goal != null ? _goal.text : string.Empty;

        private void OnEnable()
        {
            GameEvents.CollectibleAcquired += OnPickup;
            GameEvents.BuildCompleted += OnCrafted;
            GameEvents.QuestBoardChanged += RefreshCopy;
            SyncInventory();
            BuildUi();
            RefreshCopy();
        }

        private void OnDisable()
        {
            GameEvents.CollectibleAcquired -= OnPickup;
            GameEvents.BuildCompleted -= OnCrafted;
            GameEvents.QuestBoardChanged -= RefreshCopy;
            if (_uiRoot != null)
            {
                Object.Destroy(_uiRoot.gameObject);
                _uiRoot = null;
            }
        }

        private void OnPickup(CollectibleDefinition def)
        {
            if (def == null)
            {
                return;
            }

            if (def.Id == "discovery_badge_secret_nook")
            {
                if (_hasSecretBadge)
                {
                    return;
                }

                _hasSecretBadge = true;
                GameplayFeedback.Toast("Secret badge found — it unlocks Turbo Boing at the Workshop!", 2.8f);
                RefreshCopy();
                return;
            }

            if (def.Category != CollectibleCategory.Component)
            {
                return;
            }

            // Arrival has four component pickups but only three required recipe IDs;
            // the extra power crystal must not make the coach claim the set is complete.
            if (!IsRequiredArrivalPart(def.Id) || !_collectedPartIds.Add(def.Id))
            {
                return;
            }

            _parts = _collectedPartIds.Count;
            RefreshCopy();
        }

        private void SyncInventory()
        {
            var session = Object.FindAnyObjectByType<LevelSession>();
            if (session != null)
            {
                _crafted = session.HasInvention("boing_o_matic");
                _craftedTurbo = session.HasInvention("boing_o_matic_turbo");
            }

            var inventory = Object.FindAnyObjectByType<InventoryComponent>();
            if (inventory == null)
            {
                return;
            }

            foreach (var id in inventory.OwnedIds)
            {
                if (id == "discovery_badge_secret_nook")
                {
                    _hasSecretBadge = true;
                }
                else if (IsRequiredArrivalPart(id))
                {
                    _collectedPartIds.Add(id);
                }
            }

            _parts = _collectedPartIds.Count;
        }

        private static bool IsRequiredArrivalPart(string id)
        {
            return id == "component_spring"
                || id == "component_metal_dish"
                || id == "component_power_crystal";
        }

        private void OnCrafted(string inventionId)
        {
            // Arrival's next beat depends on building the launcher, not any unrelated
            // recipe or future craft event raised in the same scene.
            if (!InventionCompatibility.Satisfies("boing_o_matic", inventionId))
            {
                return;
            }

            _crafted = true;
            if (inventionId == "boing_o_matic_turbo")
            {
                _craftedTurbo = true;
            }

            RefreshCopy();
        }

        private void BuildUi()
        {
            var canvas = RuntimeUiFactory.EnsureOverlayCanvas("UI_ArrivalObjectiveCoach", 80);
            var rootObject = new GameObject("ArrivalObjectiveCoachRoot", typeof(RectTransform));
            _uiRoot = rootObject.GetComponent<RectTransform>();
            _uiRoot.SetParent(canvas.transform, false);
            _uiRoot.anchorMin = Vector2.zero;
            _uiRoot.anchorMax = Vector2.one;
            _uiRoot.offsetMin = Vector2.zero;
            _uiRoot.offsetMax = Vector2.zero;
            var root = _uiRoot.transform;

            var card = RuntimeUiFactory.Panel(
                root,
                "ObjectiveCard",
                new Vector2(0.02f, 0.82f),
                new Vector2(0.255f, 0.95f),
                Vector2.zero,
                Vector2.zero,
                Color.white);
            // One shared field note replaces the separate mission and side-task cards.
            // Keep the card compact so it reads as a note on the world, not another panel.
            RuntimeUiFactory.ApplyNordicHudFrame(card.GetComponent<Image>(), 0.84f, 0.54f);
            AddSideAccent(card, new Color(0.96f, 0.67f, 0.3f, 0.98f));
            var header = RuntimeUiFactory.Label(
                card,
                "Header",
                "RAINBOW KINGDOM",
                15,
                FontStyle.Bold,
                new Color(1f, 0.86f, 0.56f),
                TextAnchor.MiddleLeft);
            // Keep the part pips in their own right-side pocket.
            RuntimeUiFactory.Stretch(header.rectTransform, 28f, 0f, 126f, 0f);
            header.rectTransform.anchorMin = new Vector2(0f, 0.78f);
            header.rectTransform.anchorMax = new Vector2(1f, 1f);

            _goal = RuntimeUiFactory.Label(
                card,
                "Goal",
                "",
                18,
                FontStyle.Bold,
                new Color(0.98f, 0.98f, 1f),
                TextAnchor.UpperLeft);
            RuntimeUiFactory.Stretch(_goal.rectTransform, 28f, 4f, 28f, 4f);
            _goal.rectTransform.anchorMin = new Vector2(0f, 0.36f);
            _goal.rectTransform.anchorMax = new Vector2(1f, 0.77f);
            _goal.resizeTextForBestFit = true;
            _goal.resizeTextMinSize = 16;
            _goal.resizeTextMaxSize = 19;

            // Ingredient marks make the gathering goal readable without another count label.
            for (var i = 0; i < 3; i++)
            {
                var pip = RuntimeUiFactory.Panel(
                    card,
                    $"PartIcon_{i}",
                    new Vector2(1f, 0.89f),
                    new Vector2(1f, 0.89f),
                    Vector2.zero,
                    Vector2.zero,
                    new Color(0.35f, 0.4f, 0.55f, 0.85f));
                pip.GetComponent<RectTransform>().sizeDelta = new Vector2(28f, 28f);
                pip.GetComponent<RectTransform>().anchoredPosition = new Vector2(-112f + i * 30f, 0f);
                _partPips[i] = pip.GetComponent<Image>();
                _partPips[i].sprite = LoadRequiredPartSprite(i);
                _partPips[i].preserveAspect = true;
                _partPips[i].raycastTarget = false;
            }

            // Keep optional discoveries as a single quiet footer line. A separate
            // "TRAILS" caption and divider repeated the same hierarchy in two labels.
            _sideQuestLine = RuntimeUiFactory.Label(
                card,
                "SideTrails",
                string.Empty,
                15,
                FontStyle.Bold,
                new Color(0.91f, 0.96f, 1f, 0.98f),
                TextAnchor.MiddleLeft);
            RuntimeUiFactory.Stretch(_sideQuestLine.rectTransform, 28f, 2f, 24f, 3f);
            _sideQuestLine.rectTransform.anchorMin = new Vector2(0f, 0.06f);
            _sideQuestLine.rectTransform.anchorMax = new Vector2(1f, 0.32f);
            _sideQuestLine.resizeTextForBestFit = true;
            _sideQuestLine.resizeTextMinSize = 14;
            _sideQuestLine.resizeTextMaxSize = 16;
            _sideQuestLine.verticalOverflow = VerticalWrapMode.Truncate;
            RefreshSideQuestCopy();

            // Keep only the two core movement hints; interaction appears contextually.
            var tipPanel = RuntimeUiFactory.Panel(
                root,
                "TipStrip",
                new Vector2(0.02f, 0.015f),
                new Vector2(0.32f, 0.062f),
                Vector2.zero,
                Vector2.zero,
                Color.white);
            _tipCanvasGroup = tipPanel.gameObject.AddComponent<CanvasGroup>();
            _tipCanvasGroup.interactable = false;
            _tipCanvasGroup.blocksRaycasts = false;
            _splatMotor = Object.FindAnyObjectByType<SplatMotor>();
            RuntimeUiFactory.ApplyCompactHudFrame(tipPanel.GetComponent<Image>(), 0.96f);
            RuntimeUiFactory.Panel(
                tipPanel,
                "TipAccent",
                new Vector2(0f, 0f),
                new Vector2(0.012f, 1f),
                Vector2.zero,
                Vector2.zero,
                new Color(1f, 0.72f, 0.28f, 1f));
            _tip = RuntimeUiFactory.Label(
                tipPanel,
                "Tip",
                ControlHintCopy(),
                18,
                FontStyle.Bold,
                new Color(0.95f, 0.92f, 1f),
                TextAnchor.MiddleLeft);
            RuntimeUiFactory.Stretch(_tip.rectTransform, 16f, 2f, 10f, 2f);
            _tip.resizeTextForBestFit = true;
            _tip.resizeTextMinSize = 16;
            _tip.resizeTextMaxSize = 18;
        }

        private static void AddSideAccent(RectTransform parent, Color color)
        {
            var accent = RuntimeUiFactory.Panel(
                parent,
                "HudAccentRail",
                new Vector2(0f, 0.18f),
                new Vector2(0f, 0.82f),
                new Vector2(9f, 0f),
                new Vector2(13f, 0f),
                color);
            accent.GetComponent<Image>().raycastTarget = false;
        }

        private string ControlHintCopy()
        {
            _inputReader = Object.FindAnyObjectByType<SplatInputReader>();
            var group = _inputReader != null ? _inputReader.ActiveBindingGroup : "Keyboard&Mouse";
            var gamepad = group == "Gamepad";
            var move = _inputReader != null
                ? _inputReader.GetBindingHint("Move", group)
                : string.Empty;
            var jump = _inputReader != null
                ? _inputReader.GetBindingHint("Jump", group)
                : string.Empty;
            if (string.IsNullOrWhiteSpace(move)) move = gamepad ? "Left Stick" : "WASD";
            if (string.IsNullOrWhiteSpace(jump)) jump = gamepad ? "South Button" : "Space";
            _lastControlHintGroup = group;
            return $"MOVE  {move}     JUMP  {jump}";
        }

        private void RefreshCopy()
        {
            if (_goal != null)
            {
                _goal.text = NextGoal();
            }

            if (_tip != null)
            {
                _tip.text = ControlHintCopy();
            }

            RefreshSideQuestCopy();

            for (var i = 0; i < _partPips.Length; i++)
            {
                if (_partPips[i] == null)
                {
                    continue;
                }

                var collected = _collectedPartIds.Contains(RequiredPartIds[i]);
                _partPips[i].color = collected
                    ? Color.white
                    // Keep each dark-blue/amber ingredient badge recognizable,
                    // but lower pending-icon brightness so a collected white
                    // badge reads as progress at the compact HUD scale.
                    : new Color(0.54f, 0.63f, 0.76f, 0.68f);
            }
        }

        private void RefreshSideQuestCopy()
        {
            if (_questDirector == null)
            {
                _questDirector = Object.FindAnyObjectByType<WorldQuestDirector>();
            }

            if (_sideQuestLine == null)
            {
                return;
            }

            var lines = _questDirector != null ? _questDirector.GetActiveSideQuestLines() : string.Empty;
            _sideQuestLine.text = lines
                .Replace("Find two hidden rainbow wonders", "Wonders")
                .Replace("Collect the Secret Nook badge too", "Nook badge")
                .Replace("Secret Nook badge", "Nook badge")
                .Replace("\n", "   •   ");
            var hasSideTrails = !string.IsNullOrWhiteSpace(_sideQuestLine.text);
            _sideQuestLine.gameObject.SetActive(hasSideTrails);
        }

        private static Sprite LoadRequiredPartSprite(int index)
        {
            if (_requiredPartSprites == null)
            {
                _requiredPartSprites = new Sprite[RequiredPartIconResources.Length];
                for (var i = 0; i < RequiredPartIconResources.Length; i++)
                {
                    var texture = Resources.Load<Texture2D>(RequiredPartIconResources[i]);
                    if (texture != null)
                    {
                        _requiredPartSprites[i] = Sprite.Create(
                            texture,
                            new Rect(0f, 0f, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f),
                            100f);
                    }
                }
            }

            return index >= 0 && index < _requiredPartSprites.Length ? _requiredPartSprites[index] : null;
        }

        private void Update()
        {
            using (UpdateMarker.Auto()) UpdateCore();
        }

        private void UpdateCore()
        {
            var foundInputReader = false;
            if (_inputReader == null)
            {
                _inputReader = Object.FindAnyObjectByType<SplatInputReader>();
                foundInputReader = _inputReader != null;
            }

            var activeGroup = _inputReader != null ? _inputReader.ActiveBindingGroup : "Keyboard&Mouse";
            if (_tip != null && (foundInputReader || activeGroup != _lastControlHintGroup))
            {
                _tip.text = ControlHintCopy();
            }

            if (_tipCanvasGroup == null || _tipCanvasGroup.alpha <= 0f)
            {
                return;
            }

            if (!_tipDismissalStarted)
            {
                if (_splatMotor == null || !_splatMotor.IsGrounded || _splatMotor.PlanarVelocity.magnitude < 1.4f)
                {
                    _tipMovementDuration = 0f;
                    return;
                }

                _tipMovementDuration += Time.unscaledDeltaTime;
                if (_tipMovementDuration < 0.8f)
                {
                    return;
                }

                _tipDismissalStarted = true;
                _tipDismissDelay = 2f;
            }

            if (_tipDismissDelay > 0f)
            {
                _tipDismissDelay -= Time.unscaledDeltaTime;
                return;
            }

            _tipCanvasGroup.alpha = Mathf.MoveTowards(_tipCanvasGroup.alpha, 0f, Time.unscaledDeltaTime * 1.8f);
            if (_tipCanvasGroup.alpha <= 0f)
            {
                _tipCanvasGroup.gameObject.SetActive(false);
            }
        }

        private string NextGoal()
        {
            if (_questDirector == null)
            {
                _questDirector = Object.FindAnyObjectByType<WorldQuestDirector>();
            }

            if (_questDirector != null && _questDirector.IsQuestComplete("arrival_main"))
            {
                return "Rainbow gateway open — onward, Splat!";
            }

            // Only advance to "ride" after the player actually crafts — ignore showcase Boings.
            if (_crafted)
            {
                if (_questDirector != null && _questDirector.HasReachedCheckpoint("Checkpoint_GulchFar"))
                {
                    return "Use Boing-O-Matic on Rattle's exposed relay.";
                }

                if (HasUsedArrivalInvention())
                {
                    return "Reach the high Rainbow path checkpoint.";
                }

                if (_craftedTurbo)
                {
                    return "Launch Turbo Boing to reach the high Rainbow path.";
                }

                if (_hasSecretBadge)
                {
                    return "Launch Boing-O-Matic to reach the high Rainbow path.";
                }

                return "Launch Boing-O-Matic to reach the high Rainbow path.";
            }

            if (_parts >= 3)
            {
                if (_hasSecretBadge)
                {
                    return "Build Turbo Boing to reach Vesper faster.";
                }

                return "Build Boing-O-Matic to reach Vesper.";
            }

            return $"Find {MissingArrivalParts()} to build Splat's first invention.";
        }

        private bool HasUsedArrivalInvention()
        {
            if (_questDirector == null)
            {
                _questDirector = Object.FindAnyObjectByType<WorldQuestDirector>();
            }

            if (_questDirector == null)
            {
                return false;
            }

            foreach (var quest in _questDirector.Quests)
            {
                if (quest == null || quest.id != "arrival_main" || quest.steps == null)
                {
                    continue;
                }

                for (var i = 0; i < quest.steps.Count; i++)
                {
                    var step = quest.steps[i];
                    if (step != null && step.kind == QuestStepKind.UseInvention
                        && InventionCompatibility.Satisfies("boing_o_matic", step.targetId))
                    {
                        return _questDirector.IsStepComplete(quest, i);
                    }
                }
            }

            return false;
        }

        private string MissingArrivalParts()
        {
            var missing = new List<string>(3);
            if (!_collectedPartIds.Contains("component_spring"))
            {
                missing.Add("spring");
            }

            if (!_collectedPartIds.Contains("component_metal_dish"))
            {
                missing.Add("metal dish");
            }

            if (!_collectedPartIds.Contains("component_power_crystal"))
            {
                missing.Add("power crystal");
            }

            if (missing.Count == 1)
            {
                return missing[0];
            }

            if (missing.Count == 2)
            {
                return $"{missing[0]} and {missing[1]}";
            }

            return $"{missing[0]}, {missing[1]}, and {missing[2]}";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void HookSceneLoaded()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureInArrival(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode _)
        {
            EnsureInArrival(scene);
        }

        private static void EnsureInArrival(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || scene.name == null || !scene.name.Contains("Arrival"))
            {
                return;
            }

            if (Object.FindAnyObjectByType<ArrivalObjectiveCoach>() != null)
            {
                return;
            }

            var go = new GameObject("SYS_ArrivalObjectiveCoach");
            go.AddComponent<ArrivalObjectiveCoach>();
        }
    }
}
