using System;
using System.Collections.Generic;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Compact screen-space inventory ribbon. Stays out of the Arrival view until the
    /// player has at least two items worth tracking.
    /// </summary>
    public sealed class CollectibleHud : MonoBehaviour
    {
        [SerializeField] private InventoryComponent inventory;

        private readonly List<string> _lines = new();
        private RectTransform _root;
        private RectTransform _itemsRoot;
        private Image _panel;
        private Text _summary;
        private float _flashUntil;

        private static readonly Color PanelColor = new(1f, 1f, 1f, 0.88f);

        private void OnEnable()
        {
            GameEvents.CollectibleAcquired += HandleAcquired;
            BuildUi();
            RefreshFromInventory();
        }

        private void OnDisable()
        {
            GameEvents.CollectibleAcquired -= HandleAcquired;
            if (_root != null)
            {
                Destroy(_root.gameObject);
                _root = null;
                _itemsRoot = null;
                _panel = null;
                _summary = null;
            }
        }

        public void BindInventory(InventoryComponent component)
        {
            inventory = component;
            RefreshFromInventory();
        }

        private void HandleAcquired(CollectibleDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            var label = string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.Id : definition.DisplayName;
            if (!_lines.Contains(label))
            {
                _lines.Add(label);
                _lines.Sort(StringComparer.OrdinalIgnoreCase);
                RenderItems();
            }

            _flashUntil = Time.unscaledTime + 0.75f;
            var description = definition.Description?.Trim();
            var hasUsefulDescription = !string.IsNullOrWhiteSpace(description)
                && !description.Equals($"{label} craft component.", StringComparison.OrdinalIgnoreCase)
                && !description.Equals($"{label} component for adventure craft.", StringComparison.OrdinalIgnoreCase);
            GameplayFeedback.Toast(
                hasUsefulDescription ? $"{label} — {description}" : $"{label} found",
                hasUsefulDescription ? 2.8f : -1f,
                playBlip: TempSfxPlayer.Instance == null);
        }

        private void RefreshFromInventory()
        {
            _lines.Clear();
            if (inventory != null)
            {
                foreach (var id in inventory.OwnedIds)
                {
                    _lines.Add(CollectibleNaming.PrettyId(id));
                }
            }

            _lines.Sort(StringComparer.OrdinalIgnoreCase);
            RenderItems();
        }

        private void BuildUi()
        {
            if (_root != null)
            {
                return;
            }

            var canvas = RuntimeUiFactory.EnsureOverlayCanvas("UI_CollectibleHud", 82);
            _root = RuntimeUiFactory.Panel(
                canvas.transform,
                "CollectibleRibbon",
                new Vector2(0.69f, 0.20f),
                new Vector2(0.98f, 0.275f),
                Vector2.zero,
                Vector2.zero,
                PanelColor);
            _panel = _root.GetComponent<Image>();
            RuntimeUiFactory.ApplyNordicHudFrame(_panel, 0.95f);
            _panel.raycastTarget = false;

            _summary = RuntimeUiFactory.Label(
                _root,
                "CollectibleSummary",
                "PACK",
                18,
                FontStyle.Bold,
                new Color(0.9f, 0.94f, 1f, 0.96f),
                TextAnchor.MiddleLeft,
                false);
            RuntimeUiFactory.Stretch(_summary.rectTransform, 42f, 0f, 42f, 0f);
            _summary.rectTransform.anchorMin = new Vector2(0f, 0.58f);
            _summary.rectTransform.anchorMax = new Vector2(1f, 1f);
            _summary.raycastTarget = false;

            _itemsRoot = new GameObject("CollectibleItems", typeof(RectTransform)).GetComponent<RectTransform>();
            _itemsRoot.SetParent(_root, false);
            _itemsRoot.anchorMin = new Vector2(0.075f, 0.07f);
            _itemsRoot.anchorMax = new Vector2(0.925f, 0.59f);
            _itemsRoot.offsetMin = Vector2.zero;
            _itemsRoot.offsetMax = Vector2.zero;
            RenderItems();
        }

        private void RenderItems()
        {
            if (_itemsRoot == null)
            {
                return;
            }

            for (var i = _itemsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_itemsRoot.GetChild(i).gameObject);
            }

            if (_summary != null)
            {
                _summary.text = "PACK";
            }

            if (_lines.Count < 2)
            {
                if (_root != null)
                {
                    _root.gameObject.SetActive(false);
                }

                return;
            }

            if (_root != null)
            {
                _root.gameObject.SetActive(true);
            }

            var slotCount = _lines.Count;
            for (var i = 0; i < slotCount; i++)
            {
                var minX = i / (float)slotCount;
                var maxX = (i + 1) / (float)slotCount;
                var slot = RuntimeUiFactory.Panel(
                    _itemsRoot,
                    $"CollectibleSlot_{i}",
                    new Vector2(minX, 0f),
                    new Vector2(maxX, 1f),
                    new Vector2(2f, 1f),
                    new Vector2(-2f, -1f),
                    new Color(0.16f, 0.2f, 0.33f, 0.76f));
                slot.GetComponent<Image>().raycastTarget = false;

                var marker = RuntimeUiFactory.Panel(
                    slot,
                    "Marker",
                    new Vector2(0.035f, 0.2f),
                    new Vector2(0.14f, 0.8f),
                    Vector2.zero,
                    Vector2.zero,
                    ColorFor(_lines[i])).GetComponent<Image>();
                RuntimeUiFactory.SetCircleImage(marker);
                marker.raycastTarget = false;

                var label = RuntimeUiFactory.Label(
                    slot,
                    "Name",
                    _lines[i],
                    16,
                    FontStyle.Bold,
                    new Color(1f, 0.97f, 0.88f, 0.98f),
                    TextAnchor.MiddleLeft,
                    false);
                RuntimeUiFactory.Stretch(label.rectTransform, 42f, 0f, 5f, 0f);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 11;
                label.resizeTextMaxSize = 14;
            }
        }

        private static Color ColorFor(string name)
        {
            var id = name != null ? name.ToLowerInvariant() : string.Empty;
            if (id.Contains("spring")) return new Color(1f, 0.76f, 0.28f, 1f);
            if (id.Contains("dish")) return new Color(0.69f, 0.83f, 0.94f, 1f);
            if (id.Contains("crystal")) return new Color(0.81f, 0.55f, 0.95f, 1f);
            if (id.Contains("badge")) return new Color(1f, 0.84f, 0.36f, 1f);
            return new Color(0.43f, 0.85f, 0.82f, 1f);
        }

        private void Update()
        {
            if (_panel == null)
            {
                return;
            }

            var remaining = _flashUntil - Time.unscaledTime;
            var pulse = remaining > 0f ? Mathf.Sin((0.75f - remaining) * 18f) * 0.5f + 0.5f : 0f;
            _panel.color = Color.Lerp(PanelColor, new Color(0.82f, 0.93f, 1f, 0.96f), pulse * 0.48f);
        }
    }
}
