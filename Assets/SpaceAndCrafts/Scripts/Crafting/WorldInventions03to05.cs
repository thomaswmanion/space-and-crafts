using UnityEngine;

namespace SpaceAndCrafts.Crafting
{
    /// <summary>World 3 invention: Junk Magnet Gauntlet.</summary>
    public sealed class JunkMagnetGauntlet : MonoBehaviour
    {
        [SerializeField] private float pullRadius = 8f;
        [SerializeField] private float pullForce = 28f;
        [SerializeField] private string inventionId = "junk_magnet_gauntlet";
        private bool _active;

        public string InventionId => inventionId;

        public void SetActivePull(bool active) => _active = active;

        private void FixedUpdate()
        {
            if (!_active)
            {
                return;
            }

            var hits = Physics.OverlapSphere(transform.position, pullRadius);
            for (var i = 0; i < hits.Length; i++)
            {
                var rb = hits[i].attachedRigidbody;
                if (rb == null || rb.isKinematic)
                {
                    continue;
                }

                var to = transform.position - rb.position;
                rb.AddForce(to.normalized * pullForce, ForceMode.Acceleration);
            }
        }
    }

    /// <summary>World 4 invention: Paint Bubble Raft.</summary>
    public sealed class PaintBubbleRaft : MonoBehaviour
    {
        [SerializeField] private float floatHeight = 2.5f;
        [SerializeField] private float riseSpeed = 4f;
        [SerializeField] private string inventionId = "paint_bubble_raft";
        private Transform _rider;

        public string InventionId => inventionId;

        public void Mount(Transform rider) => _rider = rider;

        public void Dismount() => _rider = null;

        private void Update()
        {
            if (_rider == null)
            {
                return;
            }

            var target = transform.position + Vector3.up * floatHeight;
            _rider.position = Vector3.MoveTowards(_rider.position, target, riseSpeed * Time.deltaTime);
        }
    }

    /// <summary>World 5 invention: Wind-Up Bridge Bug.</summary>
    public sealed class WindUpBridgeBug : MonoBehaviour
    {
        [SerializeField] private GameObject bridgeRoot;
        [SerializeField] private float deploySeconds = 1.2f;
        [SerializeField] private string inventionId = "windup_bridge_bug";
        private bool _deployed;

        public string InventionId => inventionId;

        public void Deploy()
        {
            if (_deployed)
            {
                return;
            }

            _deployed = true;
            if (bridgeRoot != null)
            {
                bridgeRoot.SetActive(true);
            }

            StartCoroutine(Grow());
        }

        private System.Collections.IEnumerator Grow()
        {
            if (bridgeRoot == null)
            {
                yield break;
            }

            var t = 0f;
            var scale = bridgeRoot.transform.localScale;
            bridgeRoot.transform.localScale = new Vector3(0.05f, scale.y, scale.z);
            while (t < deploySeconds)
            {
                t += Time.deltaTime;
                var u = Mathf.SmoothStep(0f, 1f, t / deploySeconds);
                bridgeRoot.transform.localScale = new Vector3(Mathf.Lerp(0.05f, scale.x, u), scale.y, scale.z);
                yield return null;
            }

            bridgeRoot.transform.localScale = scale;
        }
    }
}
