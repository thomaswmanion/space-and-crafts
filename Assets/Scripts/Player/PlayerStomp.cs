using SpaceAndCrafts.Combat;
using SpaceAndCrafts.Enemies;
using UnityEngine;

namespace SpaceAndCrafts.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerStomp : MonoBehaviour
    {
        [SerializeField, Min(1)] private int stompDamage = 2;
        [SerializeField, Min(1f)] private float bounceSpeed = 9f;

        private PlayerController3D controller;

        private void Awake()
        {
            controller = GetComponent<PlayerController3D>();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (controller.Velocity.y > 0.5f || hit.normal.y < 0.35f)
            {
                return;
            }

            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy == null || !enemy.IsAlive || transform.position.y <= enemy.transform.position.y + 0.25f)
            {
                return;
            }

            enemy.TakeDamage(stompDamage, hit.point, Vector3.down);
            controller.LaunchUpward(bounceSpeed);
        }
    }
}
