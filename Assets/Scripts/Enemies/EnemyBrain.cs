using SpaceAndCrafts.Player;
using UnityEngine;

namespace SpaceAndCrafts.Enemies
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField] private bool flying;
        [SerializeField, Min(0.5f)] private float moveSpeed = 2.2f;
        [SerializeField, Min(1f)] private float detectionRange = 9f;
        [SerializeField, Min(0.5f)] private float attackRange = 1.25f;
        [SerializeField, Min(0.1f)] private float attackCooldown = 1.1f;
        [SerializeField, Min(0f)] private float patrolRadius = 3.5f;

        private CharacterController controller;
        private Transform player;
        private Vector3 spawnPoint;
        private float gravityVelocity;
        private float nextAttackTime;
        private float patrolOffset;

        public void Configure(bool isFlying, float speed, float range)
        {
            flying = isFlying;
            moveSpeed = speed;
            detectionRange = range;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            spawnPoint = transform.position;
            patrolOffset = Random.Range(0f, 10f);
        }

        private void Start()
        {
            PlayerController3D found = FindAnyObjectByType<PlayerController3D>();
            if (found != null)
            {
                player = found.transform;
            }
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            Vector3 toPlayer = player.position - transform.position;
            float distance = new Vector2(toPlayer.x, toPlayer.z).magnitude;
            Vector3 target = distance <= detectionRange ? player.position : PatrolTarget();
            Vector3 direction = target - transform.position;
            if (!flying)
            {
                direction.y = 0f;
            }

            if (direction.sqrMagnitude > 0.1f)
            {
                direction.Normalize();
                Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z);
                if (flatDirection.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatDirection), 8f * Time.deltaTime);
                }
            }

            Vector3 motion = direction * moveSpeed;
            if (flying)
            {
                motion.y += Mathf.Sin(Time.time * 3f + patrolOffset) * 0.65f;
            }
            else
            {
                if (controller.isGrounded && gravityVelocity < 0f)
                {
                    gravityVelocity = -1f;
                }
                gravityVelocity -= 22f * Time.deltaTime;
                motion.y = gravityVelocity;
            }

            controller.Move(motion * Time.deltaTime);

            PlayerAbilities abilities = player.GetComponent<PlayerAbilities>();
            if (distance <= attackRange && Time.time >= nextAttackTime && (abilities == null || !abilities.IsInvisible))
            {
                nextAttackTime = Time.time + attackCooldown;
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(1, transform.position);
                }
            }
        }

        private Vector3 PatrolTarget()
        {
            float time = Time.time * 0.45f + patrolOffset;
            return spawnPoint + new Vector3(Mathf.Sin(time), 0f, Mathf.Cos(time)) * patrolRadius;
        }
    }
}
