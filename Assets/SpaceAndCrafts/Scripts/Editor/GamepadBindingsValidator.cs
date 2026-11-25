#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Evidence that controller bindings exist on SplatInputActions (not a live pad playtest).
    /// </summary>
    public static class GamepadBindingsValidator
    {
        private const string AssetPath = "Assets/SpaceAndCrafts/Input/SplatInputActions.inputactions";

        [MenuItem("Space & Crafts/Validate Gamepad Bindings")]
        public static void ValidateMenu()
        {
            if (Validate(out var report))
            {
                Debug.Log("[Space & Crafts] Gamepad bindings VALID.\n" + report);
            }
            else
            {
                Debug.LogError("[Space & Crafts] Gamepad bindings FAILED.\n" + report);
            }
        }

        public static void ValidateOrThrow()
        {
            if (!Validate(out var report))
            {
                throw new System.Exception("Gamepad bindings validation failed:\n" + report);
            }

            Debug.Log("[Space & Crafts] Gamepad bindings VALID.\n" + report);
        }

        public static bool Validate(out string report)
        {
            var sb = new StringBuilder();
            var pass = true;
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            if (asset == null)
            {
                report = $"Missing InputActionAsset at {AssetPath}";
                return false;
            }

            var map = asset.FindActionMap("Player", throwIfNotFound: false);
            if (map == null)
            {
                report = "Missing Player action map";
                return false;
            }

            pass &= RequireGamepad(map, "Move", sb);
            pass &= RequireGamepad(map, "Look", sb);
            pass &= RequireGamepad(map, "Jump", sb);
            pass &= RequireGamepad(map, "Interact", sb);
            report = sb.ToString();
            return pass;
        }

        private static bool RequireGamepad(InputActionMap map, string actionName, StringBuilder sb)
        {
            var action = map.FindAction(actionName, throwIfNotFound: false);
            if (action == null)
            {
                sb.AppendLine($"FAIL: missing action '{actionName}'");
                return false;
            }

            var hasGamepad = false;
            foreach (var binding in action.bindings)
            {
                var groups = binding.groups ?? string.Empty;
                var path = binding.path ?? string.Empty;
                if (groups.IndexOf("Gamepad", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("<Gamepad>", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hasGamepad = true;
                    sb.AppendLine($"OK: {actionName} ← {path} [{groups}]");
                    break;
                }
            }

            if (!hasGamepad)
            {
                sb.AppendLine($"FAIL: {actionName} has no Gamepad binding");
            }

            return hasGamepad;
        }
    }
}
#endif
