using SpaceAndCrafts.Core;
using SpaceAndCrafts.Combat;
using UnityEngine;

namespace SpaceAndCrafts.Collectibles
{
    [RequireComponent(typeof(Collider))]
    public sealed class CrystalCollectible : MonoBehaviour
    {
        [SerializeField, Min(1)] private int value = 1;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.8f;
        [SerializeField, Min(0f)] private float spinSpeed = 90f;

        private bool collected;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || !other.CompareTag("Player"))
            {
                return;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("A GameManager is required to collect crystals.", this);
                return;
            }

            collected = true;
            GameManager.Instance.AddCrystals(value);
            ImpactBurst.Spawn(transform.position, new Color(0.1f, 0.95f, 1f), 0.8f);

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
            }

            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
