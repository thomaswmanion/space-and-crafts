using System.Collections;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Full-screen new-game story — Canvas overlay, cannot be missed.
    /// </summary>
    public sealed class OpeningStoryPresenter : MonoBehaviour
    {
        [SerializeField] private string arrivalScene = "RainbowWorld_Arrival";
        [SerializeField] private float cardSeconds = 6f;
        [SerializeField] private bool skipWithJump = true;

        private static readonly (string title, string body)[] Cards =
        {
            (
                "THREE TAPS FROM HOME",
                "Splat is a teenage alien inventor who maps rainbow roads with Vesper, his best friend and shortcut rival. His brother Squat is usually Splat’s first test pilot. When a road in Rainbow Kingdom cracks and Vesper vanishes beyond it, Splat follows the signal they promised to answer: three taps mean, “I’m here. Keep looking.” He pats his spring: “Okay, map. Let’s see if you bounce.”"
            ),
            (
                "THE KEEPER IN THE STORM",
                "Rattle, the arch’s old gatekeeper, is sealing the broken road before the crack spreads. To Rattle, it’s protection; to Vesper, it’s a lock. A scrapstorm has jammed the relay. Splat: “I appreciate the safety lecture. I’m still coming through.”"
            ),
            (
                "MAKE A WAY THROUGH",
                "Every good shortcut starts with salvage. Find a spring, a dish, and a charged crystal. Build the Boing-O-Matic, ride Rattle’s storm to the relay, then use your invention to break through the jam."
            ),
            (
                "ANSWER THE SIGNAL",
                "Reset the relay and send Vesper the three-tap signal you promised. The arch lights up; the other rainbows answer. Splat grins: “Vesper, hold that thought. I have a terrible idea.”"
            ),
        };

        private int _card;
        private int _advanceToken;
        private Text _title;
        private Text _body;
        private RectTransform _bodyViewport;
        private Text _hint;
        private Canvas _canvas;
        private RectTransform _splatStage;
        private RectTransform _vesperStage;
        private bool _portraitLayout;
        private bool _reviewOnly;
        private RectTransform _beatStage;
        private Text _beatCaption;

        public static void BeginFromMenu()
        {
            var go = new GameObject("SYS_OpeningStory");
            DontDestroyOnLoad(go);
            var presenter = go.AddComponent<OpeningStoryPresenter>();
            presenter.StartCoroutine(presenter.Run());
        }

        public static void ReviewCard(int index)
        {
            if (index < 0 || index >= Cards.Length)
            {
                Debug.LogWarning($"[Space & Crafts] Story card index {index + 1} is out of range.");
                return;
            }

            var presenter = FindAnyObjectByType<OpeningStoryPresenter>();
            if (presenter == null)
            {
                var go = new GameObject("SYS_OpeningStoryReview");
                DontDestroyOnLoad(go);
                presenter = go.AddComponent<OpeningStoryPresenter>();
                presenter._reviewOnly = true;
                presenter.BuildUi();
            }
            else if (!presenter._reviewOnly)
            {
                Debug.LogWarning("[Space & Crafts] Wait for or stop the timed story before using the held-card review.");
                return;
            }

            presenter.ShowCard(index);
            Debug.Log($"[Space & Crafts] Holding opening story card {index + 1}/{Cards.Length} for layout review.");
        }

        public void ForceAdvanceCard() => _advanceToken++;

        private IEnumerator Run()
        {
            BuildUi();
            WorldMusicDirector.Ensure(1);
            for (_card = 0; _card < Cards.Length; _card++)
            {
                ShowCard(_card);
                var seen = _advanceToken;
                var until = Time.unscaledTime + GetCardDisplaySeconds(Cards[_card].body);
                while (Time.unscaledTime < until)
                {
                    if (skipWithJump && (WasAdvancePressed() || _advanceToken != seen))
                    {
                        break;
                    }

                    yield return null;
                }
            }

            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
            }

            var scene = arrivalScene;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                scene = "RainbowWorld_Arrival";
            }

            if (GameSlotStore.ActiveSlot >= 0)
            {
                GameSlotStore.RefreshMetaFromProgress(GameSlotStore.ActiveSlot);
            }

            SceneManager.LoadScene(scene);
            Destroy(gameObject);
        }

        private float GetCardDisplaySeconds(string body)
        {
            const float readingWordsPerMinute = 180f;
            const float settleSeconds = 1.25f;
            var wordCount = string.IsNullOrWhiteSpace(body) ? 0 : body.Split(' ').Length;
            var readingTime = settleSeconds + wordCount * 60f / readingWordsPerMinute;
            return Mathf.Max(cardSeconds, readingTime);
        }

        private void BuildUi()
        {
            _canvas = RuntimeUiFactory.EnsureOverlayCanvas("UI_OpeningStory", 200);
            var root = _canvas.transform;
            var hasNordicPanorama = BuildNordicPanorama(root);

            RuntimeUiFactory.Panel(
                root,
                "Dim",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(0.06f, 0.12f, 0.28f, hasNordicPanorama ? 0.42f : 0.82f));

            // Directed visual beat behind the card — rainbow gateway + craft storm.
            BuildDirectedBackdrop(root, hasNordicPanorama);

            RuntimeUiFactory.Panel(
                root,
                "Accent",
                new Vector2(0.1f, 0.72f),
                new Vector2(0.9f, 0.735f),
                Vector2.zero,
                Vector2.zero,
                new Color(1f, 0.55f, 0.2f, 1f));

            var card = RuntimeUiFactory.Panel(
                root,
                "Card",
                new Vector2(0.08f, 0.2f),
                new Vector2(0.92f, 0.74f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.1f, 0.12f, 0.28f, 0.985f));

            // Rainbow strip under brand — genre color before the player reads a word.
            for (var i = 0; i < 5; i++)
            {
                var t0 = 0.08f + i * 0.168f;
                RuntimeUiFactory.Panel(
                    root,
                    $"Rainbow_{i}",
                    new Vector2(t0, 0.745f),
                    new Vector2(t0 + 0.16f, 0.762f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.HSVToRGB(i / 5f, 0.75f, 1f));
            }

            RuntimeUiFactory.Label(
                root,
                "BrandChip",
                "SPACE & CRAFTS  ·  OUR WAY GALAXY",
                26,
                FontStyle.Bold,
                new Color(1f, 0.9f, 0.45f),
                TextAnchor.MiddleCenter);
            var brand = root.Find("BrandChip")?.GetComponent<UnityEngine.UI.Text>();
            if (brand != null)
            {
                RuntimeUiFactory.Stretch(brand.rectTransform, 0f, 0f, 0f, 0f);
                brand.rectTransform.anchorMin = new Vector2(0.08f, 0.86f);
                brand.rectTransform.anchorMax = new Vector2(0.92f, 0.94f);
            }

            BuildSplatPortrait(card);
            BuildVesperPortrait(card);

            _title = RuntimeUiFactory.Label(
                card,
                "Title",
                "",
                42,
                FontStyle.Bold,
                new Color(1f, 0.9f, 0.45f),
                TextAnchor.UpperLeft);
            RuntimeUiFactory.Stretch(_title.rectTransform, 48f, 0f, 32f, 36f);
            _title.rectTransform.anchorMin = new Vector2(0f, 0.62f);
            _title.rectTransform.anchorMax = new Vector2(0.68f, 1f);

            _body = RuntimeUiFactory.Label(
                CreateBodyViewport(card),
                "Body",
                "",
                28,
                FontStyle.Bold,
                new Color(0.98f, 0.97f, 1f),
                TextAnchor.UpperLeft);
            RuntimeUiFactory.Stretch(_body.rectTransform, 0f, 0f, 0f, 0f);
            _body.resizeTextForBestFit = true;
            _body.resizeTextMinSize = 20;
            _body.resizeTextMaxSize = 28;

            _hint = RuntimeUiFactory.Label(
                card,
                "Hint",
                "",
                18,
                FontStyle.Normal,
                new Color(0.8f, 0.78f, 1f, 0.9f),
                TextAnchor.MiddleLeft);
            RuntimeUiFactory.Stretch(_hint.rectTransform, 48f, 24f, 220f, 0f);
            _hint.rectTransform.anchorMin = new Vector2(0f, 0f);
            _hint.rectTransform.anchorMax = new Vector2(1f, 0.18f);

            var cont = RuntimeUiFactory.MakeButton(
                card,
                "Continue",
                "Continue →",
                new Vector2(0.72f, 0.06f),
                new Vector2(0.96f, 0.16f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.95f, 0.55f, 0.2f, 1f),
                Color.white,
                22);
            cont.onClick.AddListener(ForceAdvanceCard);

            ApplyResponsiveLayout(card, cont.GetComponent<RectTransform>());
            gameObject.AddComponent<StoryPortraitBob>().Bind(_splatStage, _vesperStage, _beatStage);
        }

        private void ApplyResponsiveLayout(Transform card, RectTransform continueButton)
        {
            // Keep the two-column portrait layout on landscape displays. On a
            // portrait window, the artwork column would leave the story copy
            // too narrow, so give the text the full width instead.
            _portraitLayout = Screen.height > 0 && Screen.width / (float)Screen.height < 1.2f;
            if (!_portraitLayout)
            {
                return;
            }

            var cardRect = card.GetComponent<RectTransform>();
            // Portrait cards use a compact reading panel. Keeping the footer
            // close to the copy makes the story feel like a deliberate card,
            // rather than a full-screen sheet with a detached button.
            cardRect.anchorMin = new Vector2(0.04f, 0.46f);
            cardRect.anchorMax = new Vector2(0.96f, 0.82f);

            if (_splatStage != null) _splatStage.gameObject.SetActive(false);
            if (_vesperStage != null) _vesperStage.gameObject.SetActive(false);

            _title.rectTransform.anchorMin = new Vector2(0f, 0.84f);
            _title.rectTransform.anchorMax = new Vector2(1f, 1f);
            _title.rectTransform.offsetMin = new Vector2(28f, 0f);
            _title.rectTransform.offsetMax = new Vector2(-28f, -12f);

            if (_bodyViewport != null)
            {
                _bodyViewport.anchorMin = new Vector2(0f, 0.16f);
                _bodyViewport.anchorMax = new Vector2(1f, 0.70f);
                _bodyViewport.offsetMin = new Vector2(28f, 0f);
                _bodyViewport.offsetMax = new Vector2(-28f, 0f);
            }

            _body.resizeTextMinSize = 20;

            _hint.rectTransform.anchorMin = new Vector2(0f, 0.06f);
            _hint.rectTransform.anchorMax = new Vector2(0.70f, 0.18f);
            _hint.rectTransform.offsetMin = new Vector2(24f, 12f);
            _hint.rectTransform.offsetMax = new Vector2(-8f, -4f);

            if (continueButton != null)
            {
                continueButton.anchorMin = new Vector2(0.70f, 0.06f);
                continueButton.anchorMax = new Vector2(0.97f, 0.18f);
            }
        }
        private Transform CreateBodyViewport(Transform card)
        {
            var viewportObject = new GameObject("BodyViewport", typeof(RectTransform), typeof(RectMask2D));
            _bodyViewport = viewportObject.GetComponent<RectTransform>();
            _bodyViewport.SetParent(card, false);
            _bodyViewport.anchorMin = new Vector2(0f, 0.22f);
            _bodyViewport.anchorMax = new Vector2(0.64f, 0.62f);
            _bodyViewport.offsetMin = new Vector2(48f, 0f);
            _bodyViewport.offsetMax = new Vector2(-32f, 0f);
            return _bodyViewport;
        }

        private bool BuildNordicPanorama(Transform root)
        {
            var panorama = Resources.Load<Texture2D>("Environment/ArrivalNordicFjordPanoramaPaintedV1")
                ?? Resources.Load<Texture2D>("Environment/ArrivalNordicFjordPanorama");
            if (panorama == null)
            {
                return false;
            }

            var imageObject = new GameObject("ART_NordicFjordBackdrop", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(root, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = imageObject.GetComponent<RawImage>();
            image.texture = panorama;
            image.raycastTarget = false;

            // Crop the wide panorama symmetrically to the current screen aspect so
            // fjord peaks keep their shape on common 16:9 and 4:3 displays.
            var textureAspect = panorama.width / (float)panorama.height;
            var screenAspect = Screen.height > 0 ? Screen.width / (float)Screen.height : textureAspect;
            var uvWidth = Mathf.Clamp(screenAspect / textureAspect, 0.35f, 1f);
            image.uvRect = new Rect((1f - uvWidth) * 0.5f, 0f, uvWidth, 1f);
            return true;
        }

        private void BuildDirectedBackdrop(Transform root, bool hasNordicPanorama)
        {
            // Keep the old color washes as a fallback; on the panorama they become
            // restrained tints so the Norwegian landscape remains visible.
            RuntimeUiFactory.Panel(
                root,
                "SkyWash",
                new Vector2(0f, 0.45f),
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.25f, 0.45f, 0.85f, hasNordicPanorama ? 0.16f : 0.55f));
            RuntimeUiFactory.Panel(
                root,
                "GroundWash",
                new Vector2(0f, 0f),
                new Vector2(1f, 0.32f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.55f, 0.32f, 0.62f, hasNordicPanorama ? 0.12f : 0.55f));

            // The block-built gateway is a landscape composition. On portrait
            // displays it crops into oversized bars, so let the Nordic panorama
            // carry the background without those decorative panels.
            if (Screen.height > 0 && Screen.width / (float)Screen.height < 1.2f)
            {
                return;
            }

            // Tall rainbow gateway — directed craft-platformer beat owning the backdrop.
            for (var i = 0; i < 6; i++)
            {
                var u = i / 6f;
                RuntimeUiFactory.Panel(
                    root,
                    $"GateBand_{i}",
                    new Vector2(0.18f + u * 0.015f, 0.32f + i * 0.055f),
                    new Vector2(0.82f - u * 0.015f, 0.38f + i * 0.055f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.HSVToRGB(u, 0.85f, 1f));
            }

            RuntimeUiFactory.Panel(
                root,
                "GatePillarL",
                new Vector2(0.18f, 0.18f),
                new Vector2(0.235f, 0.62f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.95f, 0.45f, 0.85f, 0.85f));
            RuntimeUiFactory.Panel(
                root,
                "GatePillarR",
                new Vector2(0.765f, 0.18f),
                new Vector2(0.82f, 0.62f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.95f, 0.45f, 0.85f, 0.85f));

            // Craft icons (spring / gear / star as colored panels) along the gate sill.
            for (var i = 0; i < 3; i++)
            {
                RuntimeUiFactory.Panel(
                    root,
                    $"CraftIcon_{i}",
                    new Vector2(0.32f + i * 0.14f, 0.2f),
                    new Vector2(0.4f + i * 0.14f, 0.3f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.HSVToRGB(0.08f + i * 0.28f, 0.8f, 1f));
            }

            _beatStage = RuntimeUiFactory.Panel(
                root,
                "BeatStage",
                new Vector2(0.28f, 0.34f),
                new Vector2(0.72f, 0.58f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.12f, 0.1f, 0.28f, 0.72f));
            RuntimeUiFactory.Panel(
                _beatStage,
                "BeatAccent",
                new Vector2(0f, 0.88f),
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector2.zero,
                new Color(1f, 0.55f, 0.2f, 1f));
            _beatCaption = RuntimeUiFactory.Label(
                _beatStage,
                "BeatCaption",
                "RAINBOW KINGDOM  •  WORLD 1",
                22,
                FontStyle.Bold,
                new Color(1f, 0.92f, 0.45f),
                TextAnchor.MiddleCenter);
            RuntimeUiFactory.Stretch(_beatCaption.rectTransform, 12f, 16f, 12f, 12f);
        }

        /// <summary>UI Splat face so story cards show the hero, not only paragraphs.</summary>
        private void BuildSplatPortrait(Transform card)
        {
            var stage = RuntimeUiFactory.Panel(
                card,
                "SplatStage",
                new Vector2(0.69f, 0.3f),
                new Vector2(0.84f, 0.86f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.55f, 0.35f, 0.95f, 0.35f));
            _splatStage = stage;

            // Prefer Mac's approved white-body/purple-limbs/backwards-cap Splat
            // portrait, and fall back only to the same approved character concept.
            var portrait = Resources.Load<Sprite>("Characters/SplatApprovedCapEyePortrait")
                ?? Resources.Load<Sprite>("Characters/SplatApprovedConceptCapEye");
            if (portrait != null)
            {
                var art = RuntimeUiFactory.Panel(
                    stage,
                    "SplatPortraitArtwork",
                    new Vector2(0.01f, 0.18f),
                    new Vector2(0.99f, 0.98f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.white);
                var image = art.GetComponent<Image>();
                image.sprite = portrait;
                image.type = Image.Type.Simple;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            RuntimeUiFactory.Label(
                stage,
                "SplatCaption",
                "SPLAT",
                18,
                FontStyle.Bold,
                new Color(1f, 0.9f, 0.45f),
                TextAnchor.MiddleCenter);
            var caption = stage.Find("SplatCaption")?.GetComponent<Text>();
            if (caption != null)
            {
                RuntimeUiFactory.Stretch(caption.rectTransform, 0f, 8f, 0f, 0f);
                caption.rectTransform.anchorMin = new Vector2(0f, 0f);
                caption.rectTransform.anchorMax = new Vector2(1f, 0.18f);
            }
        }

        private void BuildVesperPortrait(Transform card)
        {
            var stage = RuntimeUiFactory.Panel(
                card,
                "VesperStage",
                new Vector2(0.85f, 0.3f),
                new Vector2(0.99f, 0.86f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.12f, 0.62f, 0.78f, 0.28f));
            _vesperStage = stage;

            var portrait = Resources.Load<Sprite>("Characters/VesperSignalPortrait");
            if (portrait != null)
            {
                var portraitRect = RuntimeUiFactory.Panel(
                    stage,
                    "VesperPortraitArtwork",
                    new Vector2(0.04f, 0.18f),
                    new Vector2(0.96f, 0.99f),
                    Vector2.zero,
                    Vector2.zero,
                    Color.white);
                var portraitImage = portraitRect.GetComponent<Image>();
                portraitImage.sprite = portrait;
                portraitImage.type = Image.Type.Simple;
                portraitImage.preserveAspect = true;
                portraitImage.raycastTarget = false;
            }
            else
            {
                var body = RuntimeUiFactory.Panel(
                    stage,
                    "VesperBody",
                    new Vector2(0.12f, 0.12f),
                    new Vector2(0.88f, 0.78f),
                    Vector2.zero,
                    Vector2.zero,
                    new Color(0.18f, 0.62f, 0.72f, 1f));

                RuntimeUiFactory.Panel(body, "EyeL", new Vector2(0.18f, 0.48f), new Vector2(0.42f, 0.78f),
                    Vector2.zero, Vector2.zero, Color.white);
                RuntimeUiFactory.Panel(body, "EyeR", new Vector2(0.58f, 0.48f), new Vector2(0.82f, 0.78f),
                    Vector2.zero, Vector2.zero, Color.white);
                RuntimeUiFactory.Panel(body, "PupilL", new Vector2(0.25f, 0.54f), new Vector2(0.36f, 0.7f),
                    Vector2.zero, Vector2.zero, new Color(0.06f, 0.1f, 0.2f));
                RuntimeUiFactory.Panel(body, "PupilR", new Vector2(0.64f, 0.54f), new Vector2(0.75f, 0.7f),
                    Vector2.zero, Vector2.zero, new Color(0.06f, 0.1f, 0.2f));

                // Keep the teal mapmaker design when the optional portrait art is absent.
                RuntimeUiFactory.Panel(stage, "MapmakerHeadband", new Vector2(0.12f, 0.72f), new Vector2(0.88f, 0.9f),
                    Vector2.zero, Vector2.zero, new Color(0.95f, 0.69f, 0.34f, 1f));
                var signalStar = RuntimeUiFactory.Panel(stage, "SignalStar", new Vector2(0.75f, 0.73f), new Vector2(0.88f, 0.86f),
                    Vector2.zero, Vector2.zero, new Color(0.64f, 0.84f, 1f));
                RuntimeUiFactory.SetCircleImage(signalStar.GetComponent<Image>());

                var map = RuntimeUiFactory.Panel(stage, "RouteMap", new Vector2(0.58f, 0.12f), new Vector2(0.96f, 0.39f),
                    Vector2.zero, Vector2.zero, new Color(0.94f, 0.83f, 0.56f, 1f));
                var route = RuntimeUiFactory.Panel(map, "SignalRoute", new Vector2(0.12f, 0.44f), new Vector2(0.88f, 0.55f),
                    Vector2.zero, Vector2.zero, new Color(0.12f, 0.56f, 0.72f, 1f));
                route.localRotation = Quaternion.Euler(0f, 0f, -22f);
                var routeStar = RuntimeUiFactory.Panel(map, "RouteStar", new Vector2(0.72f, 0.62f), new Vector2(0.94f, 0.88f),
                    Vector2.zero, Vector2.zero, new Color(0.82f, 0.34f, 0.58f, 1f));
                RuntimeUiFactory.SetCircleImage(routeStar.GetComponent<Image>());
            }

            var caption = RuntimeUiFactory.Label(stage, "VesperCaption", "VESPER", 18, FontStyle.Bold,
                new Color(1f, 0.9f, 0.45f), TextAnchor.MiddleCenter);
            RuntimeUiFactory.Stretch(caption.rectTransform, 0f, 8f, 0f, 0f);
            caption.rectTransform.anchorMin = new Vector2(0f, 0f);
            caption.rectTransform.anchorMax = new Vector2(1f, 0.18f);
        }

        private void ShowCard(int i)
        {
            if (i < 0 || i >= Cards.Length || _title == null)
            {
                return;
            }

            var card = Cards[i];
            _title.text = card.title;
            _body.text = card.body;
            _hint.text = $"Story {i + 1}/{Cards.Length}  ·  Enter / Space / South button to continue";
            if (_beatCaption != null)
            {
                _beatCaption.text = i switch
                {
                    0 => "A PROMISE TO VESPER",
                    1 => "RATTLE'S BROKEN DUTY",
                    2 => "BUILD · RIDE · REPAIR",
                    _ => "FOLLOW THE SIGNAL",
                };
            }

            if (_vesperStage != null)
            {
                _vesperStage.gameObject.SetActive(!_portraitLayout && (i == 0 || i == Cards.Length - 1));
            }
        }

        private sealed class StoryPortraitBob : MonoBehaviour
        {
            private RectTransform _splat;
            private RectTransform _vesper;
            private RectTransform _beat;
            private Vector2 _splatRest;
            private Vector2 _beatRest;

            public void Bind(RectTransform splat, RectTransform vesper, RectTransform beat)
            {
                _splat = splat;
                _vesper = vesper;
                _beat = beat;
                if (_splat != null)
                {
                    _splatRest = _splat.anchoredPosition;
                }

                if (_beat != null)
                {
                    _beatRest = _beat.anchoredPosition;
                }
            }

            private void Update()
            {
                var t = Time.unscaledTime;
                if (_splat != null)
                {
                    var s = 1f + Mathf.Sin(t * 2.6f) * 0.045f;
                    _splat.localScale = new Vector3(s, s, 1f);
                }

                if (_vesper != null)
                {
                    var s = 1f + Mathf.Sin(t * 2.3f + 0.7f) * 0.04f;
                    _vesper.localScale = new Vector3(s, s, 1f);
                }

                if (_beat != null)
                {
                    var s = 1f + Mathf.Sin(t * 2.1f) * 0.04f;
                    _beat.localScale = new Vector3(s, s, 1f);
                }
            }
        }

        private static bool WasAdvancePressed()
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
    }
}
