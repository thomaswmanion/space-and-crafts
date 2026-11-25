using SpaceAndCrafts.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Short-lived screen-space message for pickup, craft, boss, and rescue feedback.
    /// </summary>
    public sealed class GameplayToastHud : MonoBehaviour
    {
        public static GameplayToastHud Instance { get; private set; }
        public string CurrentMessage => _message;

        [SerializeField] private float defaultDuration = 2.2f;

        private string _message = string.Empty;
        private float _shownAt;
        private float _until;
        private RectTransform _root;
        private CanvasGroup _canvasGroup;
        private Text _label;

        private static readonly Color PanelColor = new(0.08f, 0.1f, 0.22f, 0.96f);

        private void Awake()
        {
            BuildUi();
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (_root != null)
            {
                Destroy(_root.gameObject);
            }
        }

        public void Show(string message, float duration = -1f)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            BuildUi();
            _message = message;
            _shownAt = Time.unscaledTime;
            _until = _shownAt + (duration > 0f ? duration : defaultDuration);
            if (_label != null)
            {
                _label.text = _message;
            }

            if (_root != null)
            {
                _root.gameObject.SetActive(true);
            }
        }

        private void BuildUi()
        {
            if (_root != null)
            {
                return;
            }

            var canvas = RuntimeUiFactory.EnsureOverlayCanvas("UI_GameplayToastHud", 120);
            _root = RuntimeUiFactory.Panel(
                canvas.transform,
                "GameplayToast",
                new Vector2(0.745f, 0.07f),
                new Vector2(0.985f, 0.15f),
                Vector2.zero,
                Vector2.zero,
                PanelColor);
            var panel = _root.GetComponent<Image>();
            RuntimeUiFactory.ApplyCompactHudFrame(panel, 0.96f);
            panel.raycastTarget = false;
            _canvasGroup = _root.gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _label = RuntimeUiFactory.Label(
                _root,
                "ToastMessage",
                _message,
                20,
                FontStyle.Bold,
                new Color(0.98f, 0.97f, 1f),
                TextAnchor.MiddleLeft);
            // Keep momentary feedback off the central landmark and objective
            // column; the opposite lower corner from the controls stays readable.
            RuntimeUiFactory.Stretch(_label.rectTransform, 18f, 6f, 18f, 6f);
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 14;
            _label.resizeTextMaxSize = 20;
            _label.raycastTarget = false;
            _root.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_root == null || !_root.gameObject.activeSelf)
            {
                return;
            }

            var remaining = _until - Time.unscaledTime;
            if (remaining <= 0f)
            {
                _root.gameObject.SetActive(false);
                return;
            }

            const float fadeInSeconds = 0.14f;
            const float fadeOutSeconds = 0.32f;
            var fadeIn = Mathf.Clamp01((Time.unscaledTime - _shownAt) / fadeInSeconds);
            var fadeOut = Mathf.Clamp01(remaining / fadeOutSeconds);
            _canvasGroup.alpha = Mathf.Min(fadeIn, fadeOut);
        }
    }

    public static class GameplayFeedback
    {
        public static void Toast(string message, float duration = -1f, bool playBlip = true)
        {
            if (playBlip)
            {
                TempSfxPlayer.PlayToastBlip();
            }

            if (GameplayToastHud.Instance != null)
            {
                GameplayToastHud.Instance.Show(message, duration);
                return;
            }

            Debug.Log($"[Space & Crafts] {message}");
        }
    }
}
