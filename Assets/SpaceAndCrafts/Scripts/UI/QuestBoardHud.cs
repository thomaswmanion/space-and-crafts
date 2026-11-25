using SpaceAndCrafts.Core;
using SpaceAndCrafts.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Secondary field-note card for Arrival's stacked objective column.
    /// </summary>
    public sealed class QuestBoardHud : MonoBehaviour
    {
        [SerializeField] private WorldQuestDirector director;

        private string _line = string.Empty;
        private RectTransform _root;
        private Image _panel;
        private Text _heading;
        private Text _label;
        private float _pulseUntil;
        private static readonly Color PanelColor = new(1f, 1f, 1f, 0.88f);
        private static readonly Color TextColor = new(0.92f, 0.97f, 1f, 0.96f);
        private static readonly Color CompleteColor = new(1f, 0.82f, 0.54f, 0.94f);

        private void OnEnable()
        {
            GameEvents.QuestBoardChanged += Refresh;
            GameEvents.QuestCompleted += HandleCompleted;
            BuildUi();
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.QuestBoardChanged -= Refresh;
            GameEvents.QuestCompleted -= HandleCompleted;
            if (_root != null)
            {
                Destroy(_root.gameObject);
                _root = null;
            }
        }

        public void Bind(WorldQuestDirector target)
        {
            director = target;
            Refresh();
        }

        private void HandleCompleted(string _)
        {
            _pulseUntil = Time.unscaledTime + 1.2f;
            Refresh();
        }

        private void BuildUi()
        {
            // Optional goals are presented in the main Arrival field note by
            // ArrivalObjectiveCoach; retain this component for scene compatibility.
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

        private void Refresh()
        {
            if (director == null)
            {
                director = FindAnyObjectByType<WorldQuestDirector>();
            }

            // The field note already carries the main objective. Keep this board focused
            // on optional goals so side quests remain visible while the main quest is active.
            _line = director != null ? director.GetActiveSideQuestLines() : string.Empty;
            _line = _line
                .Replace("Find two hidden rainbow wonders", "Rainbow wonders")
                .Replace("Collect the Secret Nook badge too", "Secret Nook badge");
            if (_label != null)
            {
                _label.text = _line;
            }

            if (_root != null)
            {
                _root.gameObject.SetActive(!string.IsNullOrEmpty(_line));
            }
        }

        private void Update()
        {
            if (_panel == null || string.IsNullOrEmpty(_line))
            {
                return;
            }

            var pulse = Mathf.Clamp01((_pulseUntil - Time.unscaledTime) / 1.2f);
            _panel.color = Color.Lerp(PanelColor, CompleteColor, pulse);
            _label.color = Color.Lerp(TextColor, Color.white, pulse);
        }
    }
}
