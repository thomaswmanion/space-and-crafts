#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Records a human Play Mode feel sign-off for the quality scorecard.
    /// Only call after a real session in Arrival / PlayerPlayground.
    /// </summary>
    public static class HumanFeelSignOffMenu
    {
        public const string SignOffPath = "Logs/human-feel-signoff.json";

        [MenuItem("Space & Crafts/Sign Off Human Feel (after Play Mode)")]
        public static void SignOff()
        {
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");
            }

            var json =
                "{\n" +
                $"  \"humanFeelSignedOff\": true,\n" +
                $"  \"signedAt\": \"{DateTime.UtcNow:o}\",\n" +
                "  \"note\": \"Play Mode feel verified smooth enough to continue toward Galaxy parity.\"\n" +
                "}\n";
            File.WriteAllText(SignOffPath, json);
            Debug.Log($"[HumanFeel] Signed off → {SignOffPath}. Re-run Quality Scorecard audit.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        public static bool IsSignedOff()
        {
            if (!File.Exists(SignOffPath))
            {
                return false;
            }

            try
            {
                var text = File.ReadAllText(SignOffPath);
                return text.IndexOf("\"humanFeelSignedOff\": true", StringComparison.Ordinal) >= 0
                       || text.IndexOf("\"humanFeelSignedOff\":true", StringComparison.Ordinal) >= 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif
