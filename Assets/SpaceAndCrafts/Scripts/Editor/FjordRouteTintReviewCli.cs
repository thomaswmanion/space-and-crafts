#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Temporary Play Mode-only material study for the existing Rainbow Kingdom fjord.
    /// It leaves scene assets untouched and writes a separate review screenshot.
    /// </summary>
    [InitializeOnLoad]
    public static class FjordRouteTintReviewCli
    {
        private const string PendingKey = "SpaceAndCrafts.FjordTintReview.Pending";
        private const string AppliedKey = "SpaceAndCrafts.FjordTintReview.Applied";
        private const string OutputKey = "SpaceAndCrafts.ArrivalCapture.Output";
        private const int ApplyAfterFrames = 24;

        static FjordRouteTintReviewCli()
        {
            EditorApplication.update -= ApplyTemporaryTint;
            EditorApplication.update += ApplyTemporaryTint;
        }

        public static void CaptureSofterLavenderFjordReviewIter1775()
        {
            const string output = "Logs/score-comparison/fjord-route-tint-review-iter1775/preview.png";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(AppliedKey, false);

            // Reuse the exact established scene and camera framing. Redirect output only
            // after the capture method initializes its Play Mode session.
            ArrivalVisualCaptureCli.CaptureWorld01SoftLavenderRouteIter1758();
            SessionState.SetString(OutputKey, output);
        }

        public static void CaptureProductionSlateLavenderFjordIter1776()
        {
            const string output = "Logs/score-comparison/fjord-route-production-tint-iter1776/frame.png";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
            SessionState.SetBool(PendingKey, false);

            // Reuse the established camera/scene capture, this time letting the
            // edited production builder supply the tint without a runtime override.
            ArrivalVisualCaptureCli.CaptureWorld01SoftLavenderRouteIter1758();
            SessionState.SetString(OutputKey, output);
        }

        private static void ApplyTemporaryTint()
        {
            if (!SessionState.GetBool(PendingKey, false)
                || SessionState.GetBool(AppliedKey, false)
                || !EditorApplication.isPlaying
                || Time.frameCount < ApplyAfterFrames)
            {
                return;
            }

            var landform = GameObject.Find("ART_FjordLandform");
            var renderer = landform != null ? landform.GetComponent<Renderer>() : null;
            if (renderer == null)
            {
                return;
            }

            var materials = renderer.materials;
            if (materials == null || materials.Length == 0 || materials[0] == null)
            {
                return;
            }

            var top = materials[0];
            if (top.HasProperty("_BaseColor"))
            {
                top.SetColor("_BaseColor", new Color(0.98f, 0.94f, 1.08f, 1f));
            }
            else if (top.HasProperty("_Color"))
            {
                top.SetColor("_Color", new Color(0.98f, 0.94f, 1.08f, 1f));
            }

            renderer.materials = materials;
            SessionState.SetBool(AppliedKey, true);
            Debug.Log("[FjordTintReview] Applied a temporary softer slate-lavender tint to ART_FjordLandform only; no saved material or scene asset changed.");
        }
    }
}
#endif
