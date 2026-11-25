using UnityEngine;

namespace SpaceAndCrafts.Cameras
{
    /// <summary>
    /// Persistent player camera preferences. ScriptableObject values remain the defaults.
    /// </summary>
    public static class PlayerCameraPreferences
    {
        private const string MouseSensitivityKey = "SpaceAndCrafts.Camera.MouseSensitivity";
        private const string GamepadSensitivityKey = "SpaceAndCrafts.Camera.GamepadSensitivity";

        public static float GetMouseLookSensitivity(float defaultValue)
        {
            return Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensitivityKey, defaultValue), 0.02f, 0.4f);
        }

        public static float GetGamepadLookMultiplier(float defaultValue)
        {
            return Mathf.Clamp(PlayerPrefs.GetFloat(GamepadSensitivityKey, defaultValue), 0.5f, 2.5f);
        }

        public static void SetMouseLookSensitivity(float value)
        {
            PlayerPrefs.SetFloat(MouseSensitivityKey, Mathf.Clamp(value, 0.02f, 0.4f));
            PlayerPrefs.Save();
        }

        public static void SetGamepadLookMultiplier(float value)
        {
            PlayerPrefs.SetFloat(GamepadSensitivityKey, Mathf.Clamp(value, 0.5f, 2.5f));
            PlayerPrefs.Save();
        }
    }
}
