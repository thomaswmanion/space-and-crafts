using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// TEMP craft spark pops (organic SoftBlob shards). Replace with authored VFX later.
    /// </summary>
    public static class TempCraftSparkBurst
    {
        private static Material _sharedSparkMat;
        private static Mesh _sharedSparkMesh;
        private static Mesh _pickupStarMesh;
        public static int PickupArcSpawnCountForDiagnostics { get; private set; }

        public static void Spawn(Vector3 worldPosition, int count = 10, float lifetime = 0.45f)
        {
            var root = new GameObject("TEMP_ART_CraftSparks");
            root.transform.position = worldPosition;
            var driver = root.AddComponent<TempCraftSparkDriver>();
            driver.Begin(count, lifetime, SharedSparkMaterial(), SharedSparkMesh());
        }

        public static void SpawnPickupArc(Vector3 worldPosition, Transform target, int count = 3)
        {
            if (target == null)
            {
                return;
            }

            if (_pickupStarMesh == null)
            {
                _pickupStarMesh = OrganicMeshFactory.FacetedStar(0.11f, 0.045f, 0.025f, 4);
                _pickupStarMesh.name = "TEMP_Organic_PouchPickupStar";
            }

            var root = new GameObject("TEMP_ART_PouchPickupArc");
            var driver = root.AddComponent<TempPouchPickupArc>();
            driver.Begin(worldPosition, target, count, SharedSparkMaterial(), _pickupStarMesh);
            PickupArcSpawnCountForDiagnostics++;
        }

        private static Mesh SharedSparkMesh()
        {
            if (_sharedSparkMesh != null)
            {
                return _sharedSparkMesh;
            }

            _sharedSparkMesh = OrganicMeshFactory.SoftBlob(1, 0.5f, 0.85f, 0.08f);
            _sharedSparkMesh.name = "TEMP_Organic_CraftSpark";
            return _sharedSparkMesh;
        }

        private static Material SharedSparkMaterial()
        {
            if (_sharedSparkMat != null)
            {
                return _sharedSparkMat;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            _sharedSparkMat = new Material(shader)
            {
                name = "TEMP_Mat_CraftSparkShared",
                color = new Color(1f, 0.8f, 0.45f)
            };
            return _sharedSparkMat;
        }
    }

    public sealed class TempCraftSparkDriver : MonoBehaviour
    {
        private Transform[] _sparks;
        private Vector3[] _velocities;
        private float _life;
        private float _elapsed;

        public void Begin(int count, float lifetime, Material sharedMat, Mesh sharedMesh)
        {
            count = Mathf.Clamp(count, 4, 24);
            _life = Mathf.Max(0.15f, lifetime);
            _sparks = new Transform[count];
            _velocities = new Vector3[count];

            for (var i = 0; i < count; i++)
            {
                var spark = new GameObject($"TEMP_ART_Spark_{i}");
                spark.transform.SetParent(transform, false);
                spark.transform.localScale = Vector3.one * Random.Range(0.06f, 0.14f);
                var filter = spark.AddComponent<MeshFilter>();
                filter.sharedMesh = sharedMesh;
                var renderer = spark.AddComponent<MeshRenderer>();
                if (sharedMat != null)
                {
                    renderer.sharedMaterial = sharedMat;
                }

                _sparks[i] = spark.transform;
                _velocities[i] = Random.onUnitSphere * Random.Range(2.5f, 5.5f);
                _velocities[i].y = Mathf.Abs(_velocities[i].y) + 1.5f;
            }
        }

        private void Update()
        {
            if (_sparks == null)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / _life);
            for (var i = 0; i < _sparks.Length; i++)
            {
                if (_sparks[i] == null)
                {
                    continue;
                }

                _velocities[i] += Physics.gravity * Time.deltaTime * 0.35f;
                _sparks[i].position += _velocities[i] * Time.deltaTime;
                var size = Mathf.Lerp(0.12f, 0.02f, t);
                _sparks[i].localScale = Vector3.one * size;
            }

            if (_elapsed >= _life)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class TempPouchPickupArc : MonoBehaviour
    {
        private Transform[] _sparks;
        private Vector3[] _startOffsets;
        private float _elapsed;
        private const float Duration = 0.34f;

        public void Begin(Vector3 origin, Transform target, int count, Material material, Mesh mesh)
        {
            count = Mathf.Clamp(count, 1, 6);
            transform.position = origin;
            _sparks = new Transform[count];
            _startOffsets = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                var sparkle = new GameObject($"TEMP_ART_PouchStar_{i}");
                sparkle.transform.SetParent(transform, false);
                var filter = sparkle.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = sparkle.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                _sparks[i] = sparkle.transform;
                _startOffsets[i] = Random.insideUnitSphere * 0.12f;
                sparkle.transform.position = origin + _startOffsets[i];
                sparkle.transform.localScale = Vector3.one * Random.Range(0.72f, 1.08f);
            }

            _target = target;
            _origin = origin;
        }

        private Transform _target;
        private Vector3 _origin;

        private void Update()
        {
            if (_target == null || _sparks == null)
            {
                Destroy(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / Duration);
            var eased = t * t * (3f - 2f * t);
            var destination = _target.position;
            for (var i = 0; i < _sparks.Length; i++)
            {
                var sparkle = _sparks[i];
                if (sparkle == null)
                {
                    continue;
                }

                var arc = Vector3.up * (0.22f + i * 0.035f) * Mathf.Sin(t * Mathf.PI);
                sparkle.position = Vector3.Lerp(_origin + _startOffsets[i], destination, eased) + arc;
                sparkle.rotation = Quaternion.Euler(0f, 0f, _elapsed * 540f + i * 90f);
                sparkle.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, t);
            }

            if (_elapsed >= Duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
