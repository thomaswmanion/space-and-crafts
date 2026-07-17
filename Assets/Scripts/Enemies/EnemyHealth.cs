using SpaceAndCrafts.Combat;
using SpaceAndCrafts.CameraSystem;
using UnityEngine;

namespace SpaceAndCrafts.Enemies
{
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 2;
        [SerializeField, Min(0f)] private float defeatDelay = 0.15f;

        private int health;
        private bool alive = true;
        private float flashUntil;
        private Renderer[] renderers;
        private Vector3 baseScale;
        private MaterialPropertyBlock propertyBlock;

        public bool IsAlive => alive;
        public int CurrentHealth => Mathf.Max(0, health);
        public int MaxHealth => maxHealth;

        public void Configure(int hitPoints)
        {
            maxHealth = Mathf.Max(1, hitPoints);
            health = maxHealth;
        }

        private void Awake()
        {
            health = maxHealth;
            renderers = GetComponentsInChildren<Renderer>();
            baseScale = transform.localScale;
            propertyBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            bool flashing = Time.time < flashUntil;
            foreach (Renderer enemyRenderer in renderers)
            {
                enemyRenderer.GetPropertyBlock(propertyBlock);
                if (flashing)
                {
                    propertyBlock.SetColor("_BaseColor", Color.white * 2f);
                }
                else
                {
                    propertyBlock.Clear();
                }
                enemyRenderer.SetPropertyBlock(propertyBlock);
            }
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, 12f * Time.deltaTime);
        }

        public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (!alive || amount <= 0)
            {
                return;
            }

            health -= amount;
            flashUntil = Time.time + 0.11f;
            transform.localScale = new Vector3(baseScale.x * 1.12f, baseScale.y * 0.82f, baseScale.z * 1.12f);
            transform.position += hitDirection.normalized * 0.25f;
            ImpactBurst.Spawn(hitPoint, new Color(1f, 0.2f, 0.12f), maxHealth >= 10 ? 1.8f : 1f);
            if (PlatformCameraFollow.Instance != null)
            {
                PlatformCameraFollow.Instance.AddImpulse(maxHealth >= 10 ? 0.18f : 0.08f);
            }
            if (health <= 0)
            {
                alive = false;
                EnemyBrain brain = GetComponent<EnemyBrain>();
                if (brain != null)
                {
                    brain.enabled = false;
                }
                Destroy(gameObject, defeatDelay);
            }
        }
    }
}
