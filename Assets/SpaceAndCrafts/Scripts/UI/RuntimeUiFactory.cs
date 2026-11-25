using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Builds readable Screen Space Overlay UI at runtime (no prefab dependency).
    /// Replaces antique OnGUI / world TextMesh chrome.
    /// </summary>
    public static class RuntimeUiFactory
    {
        private static Sprite _circleSprite;
        private static Sprite _compactHudPanelSprite;
        private static Sprite[] _nordicHudCorners;
        private static Font _arrivalHudRegularFont;
        private static Font _arrivalHudBoldFont;

        public static Canvas EnsureOverlayCanvas(string name, int sortOrder = 100)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                var c = existing.GetComponent<Canvas>();
                if (c != null)
                {
                    return c;
                }
            }

            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            // Screen Space - Camera so GameView / camera captures include the HUD (Overlay is invisible to Camera.Render).
            var cam = Camera.main;
            if (cam != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1.2f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        public static RectTransform Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            return rt;
        }

        public static Text Label(Transform parent, string name, string content, int fontSize, FontStyle style,
            Color color, TextAnchor align, bool wrap = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            var usesBoldFace = style == FontStyle.Bold || style == FontStyle.BoldAndItalic;
            var regularFont = ArrivalHudRegularFont();
            var boldFont = usesBoldFace ? ArrivalHudBoldFont() : null;
            var customFont = usesBoldFace ? boldFont ?? regularFont : regularFont;
            text.font = customFont != null ? customFont : BuiltinFont();
            text.text = content;
            text.fontSize = fontSize;
            // The bundled regular and bold files are separate static faces. Avoid
            // synthetic double-bold while preserving italic when a caller requests it.
            text.fontStyle = boldFont != null
                ? style == FontStyle.BoldAndItalic ? FontStyle.Italic : FontStyle.Normal
                : style;
            text.color = color;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = wrap ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public static Button MakeButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color bg, Color fg, int fontSize = 22)
        {
            var rt = Panel(parent, name, anchorMin, anchorMax, offsetMin, offsetMax, bg);
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bg;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(bg, Color.black, 0.15f);
            colors.selectedColor = colors.highlightedColor;
            btn.colors = colors;
            var text = Label(rt, "Label", label, fontSize, FontStyle.Bold, fg, TextAnchor.MiddleCenter, false);
            var trt = text.rectTransform;
            trt.offsetMin = new Vector2(16f, 4f);
            trt.offsetMax = new Vector2(-16f, -4f);
            text.raycastTarget = false;
            return btn;
        }

        public static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void SetCircleImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            if (_circleSprite == null)
            {
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "RuntimeUi_Circle",
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var dx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                        var dy = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                        var distance = Mathf.Sqrt(dx * dx + dy * dy);
                        var alpha = Mathf.Clamp01((1f - distance) * size * 0.5f);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply(false, true);
                _circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                _circleSprite.name = "RuntimeUi_Circle";
                _circleSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            image.sprite = _circleSprite;
        }

        /// <summary>
        /// Applies the shared illustrated Nordic corner accents over the dark Arrival
        /// surface. The source texture lives in Resources for runtime-created HUDs.
        /// </summary>
        public static void ApplyNordicHudFrame(Image image, float opacity = 0.88f, float cornerScale = 1f)
        {
            if (image == null)
            {
                return;
            }

            if (_nordicHudCorners == null)
            {
                var texture = Resources.Load<Texture2D>("UI/arrival-hud-frame");
                if (texture == null)
                {
                    ApplyCompactHudFrame(image, opacity);
                    return;
                }

                const int cornerWidth = 250;
                const int cornerHeight = 230;
                const float pixelsPerUiUnit = 6f;
                _nordicHudCorners = new[]
                {
                    Sprite.Create(texture, new Rect(0, texture.height - cornerHeight, cornerWidth, cornerHeight),
                        new Vector2(0f, 1f), pixelsPerUiUnit, 0, SpriteMeshType.FullRect),
                    Sprite.Create(texture, new Rect(texture.width - cornerWidth, texture.height - cornerHeight, cornerWidth, cornerHeight),
                        new Vector2(1f, 1f), pixelsPerUiUnit, 0, SpriteMeshType.FullRect),
                    Sprite.Create(texture, new Rect(0, 0, cornerWidth, cornerHeight),
                        new Vector2(0f, 0f), pixelsPerUiUnit, 0, SpriteMeshType.FullRect),
                    Sprite.Create(texture, new Rect(texture.width - cornerWidth, 0, cornerWidth, cornerHeight),
                        new Vector2(1f, 0f), pixelsPerUiUnit, 0, SpriteMeshType.FullRect)
                };

                var names = new[] { "NordicCorner_TopLeft", "NordicCorner_TopRight", "NordicCorner_BottomLeft", "NordicCorner_BottomRight" };
                for (var i = 0; i < _nordicHudCorners.Length; i++)
                {
                    _nordicHudCorners[i].name = names[i] + "Sprite";
                    _nordicHudCorners[i].hideFlags = HideFlags.HideAndDontSave;
                }
            }

            ApplyCompactHudFrame(image, opacity);
            var cyan = new Color(0.42f, 0.86f, 0.98f, 0.36f);
            foreach (var edgeName in new[] { "CompactFrame_Top", "CompactFrame_Bottom", "CompactFrame_Left", "CompactFrame_Right" })
            {
                var edge = image.transform.Find(edgeName);
                if (edge != null)
                {
                    edge.GetComponent<Image>().color = cyan;
                }
            }

            var anchors = new[] { new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(1f, 0f) };
            var insets = new[] { new Vector2(4f, -4f), new Vector2(-4f, -4f), new Vector2(4f, 4f), new Vector2(-4f, 4f) };
            for (var i = 0; i < _nordicHudCorners.Length; i++)
            {
                var cornerName = $"NordicCorner_{i}";
                var corner = image.transform.Find(cornerName);
                if (corner == null)
                {
                    var go = new GameObject(cornerName, typeof(RectTransform), typeof(Image));
                    corner = go.transform;
                    corner.SetParent(image.transform, false);
                }

                var rect = (RectTransform)corner;
                rect.anchorMin = anchors[i];
                rect.anchorMax = anchors[i];
                rect.pivot = anchors[i];
                rect.anchoredPosition = insets[i];
                rect.sizeDelta = new Vector2(42f, 39f) * Mathf.Clamp01(cornerScale);
                var cornerImage = corner.GetComponent<Image>();
                cornerImage.sprite = _nordicHudCorners[i];
                cornerImage.type = Image.Type.Simple;
                cornerImage.preserveAspect = true;
                cornerImage.color = Color.white;
                cornerImage.raycastTarget = false;
                corner.SetAsLastSibling();
            }
        }

        /// <summary>
        /// Draws a compact code-native frame for small in-game HUD panels. The
        /// corner brackets keep their size at every aspect ratio, unlike the
        /// illustrated nine-slice frame which needs taller cards.
        /// </summary>
        public static void ApplyCompactHudFrame(Image image, float opacity = 0.96f)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = null;
            image.type = Image.Type.Simple;
            // Keep the root transparent. Several HUDs tint their root Image while
            // animating feedback, so a dedicated child must own the actual fill.
            image.color = Color.clear;
            image.raycastTarget = false;

            var root = image.transform;
            var cyan = new Color(0.42f, 0.86f, 0.98f, 0.92f);
            var background = root.Find("CompactFrame_Background");
            if (background == null)
            {
                var backplate = new GameObject("CompactFrame_Background", typeof(RectTransform), typeof(Image));
                background = backplate.transform;
                background.SetParent(root, false);
                var rect = (RectTransform)background;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var backgroundImage = background.GetComponent<Image>();
            var panelSprite = CompactHudPanelSprite();
            backgroundImage.sprite = panelSprite;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = panelSprite != null
                ? new Color(1f, 1f, 1f, Mathf.Clamp01(opacity))
                : new Color(0.018f, 0.043f, 0.087f, Mathf.Clamp01(opacity));
            backgroundImage.raycastTarget = false;
            background.SetAsFirstSibling();

            AddHudEdge(root, "CompactFrame_Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), Vector2.zero, cyan);
            AddHudEdge(root, "CompactFrame_Bottom", Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 2f), cyan);
            AddHudEdge(root, "CompactFrame_Left", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(2f, 0f), cyan);
            AddHudEdge(root, "CompactFrame_Right", new Vector2(1f, 0f), Vector2.one, new Vector2(-2f, 0f), Vector2.zero, cyan);
        }

        private static void AddHudEdge(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var edge = parent.Find(name);
            if (edge == null)
            {
                var bar = new GameObject(name, typeof(RectTransform), typeof(Image));
                edge = bar.transform;
                edge.SetParent(parent, false);
            }

            var rect = (RectTransform)edge;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            edge.SetAsLastSibling();
            var image = edge.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static Font BuiltinFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }

        private static Font ArrivalHudRegularFont()
        {
            if (_arrivalHudRegularFont == null)
            {
                _arrivalHudRegularFont = Resources.Load<Font>("UI/Fonts/AtkinsonHyperlegible-Regular");
            }

            return _arrivalHudRegularFont;
        }

        private static Font ArrivalHudBoldFont()
        {
            if (_arrivalHudBoldFont == null)
            {
                _arrivalHudBoldFont = Resources.Load<Font>("UI/Fonts/AtkinsonHyperlegible-Bold");
            }

            return _arrivalHudBoldFont;
        }

        private static Sprite CompactHudPanelSprite()
        {
            if (_compactHudPanelSprite != null)
            {
                return _compactHudPanelSprite;
            }

            var texture = Resources.Load<Texture2D>("UI/arrival-hud-frame");
            if (texture == null || texture.width < 800 || texture.height < 500)
            {
                return null;
            }

            // Reuse the hand-painted fabric center without pulling its ornate corners
            // beneath HUD copy. Each card stretches this quiet region independently.
            const int horizontalInset = 250;
            const int verticalInset = 210;
            var rect = new Rect(
                horizontalInset,
                verticalInset,
                texture.width - horizontalInset * 2,
                texture.height - verticalInset * 2);
            _compactHudPanelSprite = Sprite.Create(
                texture,
                rect,
                new Vector2(0.5f, 0.5f),
                6f,
                0,
                SpriteMeshType.FullRect);
            _compactHudPanelSprite.name = "ArrivalHud_PaintedFabricCenter";
            _compactHudPanelSprite.hideFlags = HideFlags.HideAndDontSave;
            return _compactHudPanelSprite;
        }
    }
}
