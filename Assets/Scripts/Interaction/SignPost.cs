using UnityEngine;

namespace SpaceAndCrafts.Interaction
{
    public sealed class SignPost : MonoBehaviour, IInteractable
    {
        [SerializeField] private string message = "Welcome to the Rainbow Kingdom!";
        [SerializeField, Min(1f)] private float displaySeconds = 4f;

        private float visibleUntil;

        public string Prompt => "Read sign";

        public void SetMessage(string value)
        {
            message = value;
        }

        public void Interact()
        {
            visibleUntil = Time.time + displaySeconds;
        }

        private void OnGUI()
        {
            if (Time.time >= visibleUntil)
            {
                return;
            }

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 22,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Box(new Rect(Screen.width * 0.5f - 280f, 35f, 560f, 90f), message, style);
        }
    }
}
