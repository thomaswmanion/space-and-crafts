using System.IO;
using System.Text;
using UnityEngine;

namespace SpaceAndCrafts.Core
{
    /// <summary>
    /// Lightweight runtime budget watcher — logs spikes + periodic density snapshots
    /// so Galaxy-scale content passes stay honest.
    /// </summary>
    public sealed class PerformanceBudgetGate : MonoBehaviour
    {
        [SerializeField] private float warnFrameMs = 22f;
        [SerializeField] private int sampleEvery = 30;
        [SerializeField] private int densityEvery = 180;

        private int _frame;
        private float _worstMs;
        private int _spikeCount;
        private int _lastTriEstimate;
        private int _lastRendererCount;

        public static PerformanceBudgetGate Ensure()
        {
            var existing = FindAnyObjectByType<PerformanceBudgetGate>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("SYS_PerformanceBudget");
            Object.DontDestroyOnLoad(go);
            return go.AddComponent<PerformanceBudgetGate>();
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime * 1000f;
            if (dt > _worstMs)
            {
                _worstMs = dt;
            }

            _frame++;
            if (_frame % Mathf.Max(1, sampleEvery) == 0)
            {
                if (_worstMs >= warnFrameMs)
                {
                    _spikeCount++;
                    Debug.LogWarning($"[PerfBudget] spike {_worstMs:0.0}ms (warn≥{warnFrameMs:0.0})");
                }

                _worstMs = 0f;
            }

            if (_frame % Mathf.Max(1, densityEvery) == 0)
            {
                CaptureDensitySnapshot();
            }
        }

        private void CaptureDensitySnapshot()
        {
            var renderers = FindObjectsByType<MeshRenderer>();
            _lastRendererCount = renderers.Length;
            var tris = 0;
            for (var i = 0; i < renderers.Length; i++)
            {
                var mf = renderers[i].GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    tris += mf.sharedMesh.triangles.Length / 3;
                }
            }

            _lastTriEstimate = tris;
            Debug.Log($"[PerfBudget] density renderers={_lastRendererCount} tris~{_lastTriEstimate} spikes={_spikeCount}");
            WriteReport();
        }

        private void WriteReport()
        {
            try
            {
                var dir = Path.Combine(Application.dataPath, "..", "Logs");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "perf-budget-report.json");
                var sb = new StringBuilder(256);
                sb.Append('{');
                sb.Append($"\"at\":\"{System.DateTime.UtcNow:o}\",");
                sb.Append($"\"renderers\":{_lastRendererCount},");
                sb.Append($"\"trisEstimate\":{_lastTriEstimate},");
                sb.Append($"\"spikeCount\":{_spikeCount},");
                sb.Append($"\"warnFrameMs\":{warnFrameMs.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                sb.Append('}');
                File.WriteAllText(path, sb.ToString());
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PerfBudget] report write failed: {ex.Message}");
            }
        }
    }
}
