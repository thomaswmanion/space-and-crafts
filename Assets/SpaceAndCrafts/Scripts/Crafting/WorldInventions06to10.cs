using System.Collections;
using UnityEngine;

namespace SpaceAndCrafts.Crafting
{
    /// <summary>World 6 invention: Heat-Tape Torch.</summary>
    public sealed class HeatTapeTorch : MonoBehaviour
    {
        [SerializeField] private float meltRange = 6f;
        [SerializeField] private string freezeTag = "FreezeLock";
        [SerializeField] private string inventionId = "heat_tape_torch";

        public string InventionId => inventionId;

        public void Fire()
        {
            var hits = Physics.OverlapSphere(transform.position + transform.forward * 2f, meltRange);
            for (var i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag(freezeTag) || hits[i].name.Contains("Freeze"))
                {
                    hits[i].gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>World 7 invention: Boom Dish Redirector.</summary>
    public sealed class BoomDishRedirector : MonoBehaviour
    {
        [SerializeField] private float blastForce = 30f;
        [SerializeField] private float blastRadius = 5f;
        [SerializeField] private string inventionId = "boom_dish_redirector";

        public string InventionId => inventionId;

        public void Redirect(Vector3 from, Vector3 toDirection)
        {
            var origin = transform.position;
            var hits = Physics.OverlapSphere(origin + toDirection.normalized * 2f, blastRadius);
            for (var i = 0; i < hits.Length; i++)
            {
                var rb = hits[i].attachedRigidbody;
                if (rb != null)
                {
                    rb.AddForce(toDirection.normalized * blastForce, ForceMode.Impulse);
                }
            }

            _ = from;
        }
    }

    /// <summary>World 8 invention: Vacuum Skimmer.</summary>
    public sealed class VacuumSkimmer : MonoBehaviour
    {
        [SerializeField] private float suckRadius = 5f;
        [SerializeField] private float spitForce = 24f;
        [SerializeField] private string inventionId = "vacuum_skimmer";
        private Rigidbody _held;

        public string InventionId => inventionId;

        public void Suck()
        {
            var hits = Physics.OverlapSphere(transform.position, suckRadius);
            for (var i = 0; i < hits.Length; i++)
            {
                var rb = hits[i].attachedRigidbody;
                if (rb == null || rb.isKinematic)
                {
                    continue;
                }

                _held = rb;
                rb.isKinematic = true;
                rb.transform.SetParent(transform, true);
                rb.transform.localPosition = Vector3.forward * 1.2f;
                return;
            }
        }

        public void Spit(Vector3 direction)
        {
            if (_held == null)
            {
                return;
            }

            _held.transform.SetParent(null, true);
            _held.isKinematic = false;
            _held.AddForce(direction.normalized * spitForce, ForceMode.VelocityChange);
            _held = null;
        }
    }

    /// <summary>World 9 invention: Star Seed Planter.</summary>
    public sealed class StarSeedPlanter : MonoBehaviour
    {
        [SerializeField] private GameObject platformPrefab;
        [SerializeField] private float platformLife = 8f;
        [SerializeField] private string inventionId = "star_seed_planter";

        public string InventionId => inventionId;

        public void Plant(Vector3 worldPoint)
        {
            GameObject go;
            if (platformPrefab != null)
            {
                go = Instantiate(platformPrefab, worldPoint, Quaternion.identity);
            }
            else
            {
                go = new GameObject("ART_StarPlatform");
                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = Art.OrganicMeshFactory.SoftBlob(2, 0.8f, 0.25f, 0.02f);
                go.AddComponent<MeshRenderer>();
                go.AddComponent<BoxCollider>();
                go.transform.position = worldPoint;
            }

            StartCoroutine(Despawn(go));
        }

        private IEnumerator Despawn(GameObject go)
        {
            yield return new WaitForSeconds(platformLife);
            if (go != null)
            {
                Destroy(go);
            }
        }
    }

    /// <summary>World 10 invention: Glass Sail Shield.</summary>
    public sealed class GlassSailShield : MonoBehaviour
    {
        [SerializeField] private float parryWindow = 0.35f;
        [SerializeField] private string inventionId = "glass_sail_shield";
        private float _parryUntil;

        public string InventionId => inventionId;
        public bool IsParrying => Time.time <= _parryUntil;

        public void Raise()
        {
            _parryUntil = Time.time + parryWindow;
        }

        public bool TryReflect(Vector3 incoming, out Vector3 reflected)
        {
            if (!IsParrying)
            {
                reflected = Vector3.zero;
                return false;
            }

            reflected = Vector3.Reflect(incoming.normalized, transform.forward);
            return true;
        }
    }
}
