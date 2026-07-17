using System;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Combat;
using SpaceAndCrafts.CameraSystem;
using UnityEngine;

namespace SpaceAndCrafts.Player
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 5;
        [SerializeField, Min(0f)] private float hitInvulnerability = 1.2f;
        [SerializeField] private PlayerAbilities abilities;

        private float invulnerableUntil;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsInvulnerable => Time.time < invulnerableUntil || (abilities != null && abilities.IsInvisible);
        public event Action<int, int> HealthChanged;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            if (abilities == null)
            {
                abilities = GetComponent<PlayerAbilities>();
            }
        }

        public void TakeDamage(int amount, Vector3 sourcePosition)
        {
            if (amount <= 0 || IsInvulnerable)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            invulnerableUntil = Time.time + hitInvulnerability;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            ImpactBurst.Spawn(transform.position + Vector3.up * 0.3f, new Color(0.65f, 0.2f, 1f), 1.2f);
            if (PlatformCameraFollow.Instance != null)
            {
                PlatformCameraFollow.Instance.AddImpulse(0.22f, 0.2f);
            }

            PlayerController3D controller = GetComponent<PlayerController3D>();
            if (controller != null)
            {
                controller.KnockBack(transform.position - sourcePosition, 5.5f, 5f);
            }

            if (CurrentHealth == 0 && GameManager.Instance != null)
            {
                GameManager.Instance.RestartCurrentLevel();
            }
        }

        public void Heal(int amount)
        {
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + Mathf.Max(0, amount));
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
