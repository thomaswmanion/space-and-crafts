using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.UI
{
    public sealed class HomeMenuController : MonoBehaviour
    {
        private enum MenuPage
        {
            Home,
            Worlds,
            Controls,
            Settings
        }

        private MenuPage page;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle panelStyle;
        private Texture2D panelTexture;

        private void Awake()
        {
            Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.height / 900f, 0.72f, 1.25f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            GUI.Label(new Rect(0f, 42f, width, 82f), "SPACE & CRAFTS", titleStyle);
            GUI.Label(new Rect(0f, 118f, width, 42f), "A Rainbow-Sized Adventure", subtitleStyle);

            Rect panel = new Rect(width * 0.5f - 260f, 190f, 520f, 535f);
            GUI.Box(panel, string.Empty, panelStyle);

            switch (page)
            {
                case MenuPage.Home:
                    DrawHome(panel);
                    break;
                case MenuPage.Worlds:
                    DrawWorlds(panel);
                    break;
                case MenuPage.Controls:
                    DrawControls(panel);
                    break;
                case MenuPage.Settings:
                    DrawSettings(panel);
                    break;
            }

            GUI.Label(new Rect(0f, height - 48f, width, 30f), "Created by a father-and-son game team", subtitleStyle);
        }

        private void DrawHome(Rect panel)
        {
            float x = panel.x + 70f;
            float y = panel.y + 65f;
            float width = panel.width - 140f;
            if (MenuButton(new Rect(x, y, width, 66f), "START ADVENTURE"))
            {
                SceneManager.LoadScene("World01_Test");
            }
            if (MenuButton(new Rect(x, y + 88f, width, 58f), "WORLD SELECT")) page = MenuPage.Worlds;
            if (MenuButton(new Rect(x, y + 164f, width, 58f), "CONTROLS")) page = MenuPage.Controls;
            if (MenuButton(new Rect(x, y + 240f, width, 58f), "SETTINGS")) page = MenuPage.Settings;
            if (MenuButton(new Rect(x, y + 330f, width, 52f), "QUIT")) Application.Quit();
        }

        private void DrawWorlds(Rect panel)
        {
            GUI.Label(new Rect(panel.x, panel.y + 25f, panel.width, 42f), "CHOOSE A RAINBOW ROUTE", subtitleStyle);
            float x = panel.x + 48f;
            float y = panel.y + 90f;
            float width = panel.width - 96f;
            if (MenuButton(new Rect(x, y, width, 58f), "WORLD 1  ★  RAINBOW KINGDOM"))
            {
                SceneManager.LoadScene("World01_Test");
            }
            DrawLocked(new Rect(x, y + 72f, width, 52f), "WORLD 2  🔒  UNKNOWN ROUTE");
            DrawLocked(new Rect(x, y + 136f, width, 52f), "WORLD 3  🔒  UNKNOWN ROUTE");
            DrawLocked(new Rect(x, y + 200f, width, 52f), "WORLD 4  🔒  UNKNOWN ROUTE");
            DrawLocked(new Rect(x, y + 264f, width, 52f), "WORLD 5  🔒  CRYSTAL TOWN");
            if (MenuButton(new Rect(x + 60f, y + 350f, width - 120f, 52f), "BACK")) page = MenuPage.Home;
        }

        private void DrawControls(Rect panel)
        {
            GUI.Label(new Rect(panel.x + 35f, panel.y + 38f, panel.width - 70f, 350f),
                "MOVE\nWASD / Arrow Keys / Left Stick\n\nJUMP & STOMP\nSpace / South Button\n\nPUNCH\nF / Left Click / Right Trigger\n\nINVISIBILITY & WALL PHASE\nLeft Shift / Right Shoulder\n\nWATER GUN\nQ / Left Trigger\n\nINTERACT\nE / West Button",
                subtitleStyle);
            if (MenuButton(new Rect(panel.x + 130f, panel.y + 450f, panel.width - 260f, 52f), "BACK")) page = MenuPage.Home;
        }

        private void DrawSettings(Rect panel)
        {
            GUI.Label(new Rect(panel.x, panel.y + 42f, panel.width, 44f), "WEB GRAPHICS QUALITY", subtitleStyle);
            float x = panel.x + 75f;
            float width = panel.width - 150f;
            if (MenuButton(new Rect(x, panel.y + 120f, width, 58f), "PERFORMANCE")) SetRenderScale(0.75f);
            if (MenuButton(new Rect(x, panel.y + 198f, width, 58f), "BALANCED")) SetRenderScale(0.9f);
            if (MenuButton(new Rect(x, panel.y + 276f, width, 58f), "BEAUTIFUL")) SetRenderScale(1f);
            GUI.Label(new Rect(panel.x + 40f, panel.y + 355f, panel.width - 80f, 52f), "Balanced is recommended for most web browsers.", subtitleStyle);
            if (MenuButton(new Rect(panel.x + 130f, panel.y + 450f, panel.width - 260f, 52f), "BACK")) page = MenuPage.Home;
        }

        private static void SetRenderScale(float scale)
        {
            if (UniversalRenderPipeline.asset != null)
            {
                UniversalRenderPipeline.asset.renderScale = scale;
            }
        }

        private bool MenuButton(Rect rect, string label)
        {
            return GUI.Button(rect, label, buttonStyle);
        }

        private void DrawLocked(Rect rect, string label)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.55f, 0.58f, 0.68f, 0.85f);
            GUI.Box(rect, label, buttonStyle);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 54, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(1f, 0.92f, 0.2f);
            subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            subtitleStyle.normal.textColor = Color.white;
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = new Color(1f, 0.92f, 0.25f);
            panelStyle = new GUIStyle(GUI.skin.box);
            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, new Color(0.055f, 0.07f, 0.16f, 0.94f));
            panelTexture.Apply();
            panelStyle.normal.background = panelTexture;
        }
    }
}
