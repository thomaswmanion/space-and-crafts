using SpaceAndCrafts.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceAndCrafts.Player
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("Punch")]
        [SerializeField, Min(0.1f)] private float punchRange = 1.45f;
        [SerializeField, Min(1)] private int punchDamage = 1;
        [SerializeField, Min(0.05f)] private float punchCooldown = 0.38f;

        [Header("Water Gun")]
        [SerializeField, Min(0.05f)] private float waterFireInterval = 0.14f;
        [SerializeField, Min(1f)] private float waterSpeed = 18f;
        [SerializeField, Min(1f)] private float waterLifetime = 2.2f;

        private float nextPunchTime;
        private float nextWaterTime;
        private static Material waterMaterial;
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject waterGunVisual;

        public bool HasWaterGun { get; private set; }
        public float PunchFlashUntil { get; private set; }

        public void SetMuzzle(Transform value)
        {
            muzzle = value;
        }

        public void SetWaterGunVisual(GameObject value)
        {
            waterGunVisual = value;
            if (waterGunVisual != null)
            {
                waterGunVisual.SetActive(HasWaterGun);
            }
        }

        public void GiveWaterGun()
        {
            HasWaterGun = true;
            if (waterGunVisual != null)
            {
                waterGunVisual.SetActive(true);
            }
        }

        private void Update()
        {
            if (WasPunchPressed() && Time.time >= nextPunchTime)
            {
                Punch();
            }

            if (HasWaterGun && IsWaterPressed() && Time.time >= nextWaterTime)
            {
                FireWater();
            }
        }

        private void Punch()
        {
            nextPunchTime = Time.time + punchCooldown;
            PunchFlashUntil = Time.time + 0.14f;
            Vector3 center = transform.position + Vector3.up * 0.2f + transform.forward * punchRange * 0.75f;
            Collider[] hits = Physics.OverlapSphere(center, punchRange * 0.7f, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider hit in hits)
            {
                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    damageable.TakeDamage(punchDamage, hit.ClosestPoint(center), transform.forward);
                }
            }
        }

        private void FireWater()
        {
            nextWaterTime = Time.time + waterFireInterval;
            Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 0.2f + transform.forward * 0.8f;
            GameObject droplet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            droplet.name = "Water Shot";
            droplet.transform.position = origin;
            droplet.transform.localScale = new Vector3(0.28f, 0.28f, 0.55f);
            droplet.transform.rotation = transform.rotation;
            Collider collider = droplet.GetComponent<Collider>();
            collider.isTrigger = true;
            Rigidbody body = droplet.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.linearVelocity = transform.forward * waterSpeed;
            WaterProjectile projectile = droplet.AddComponent<WaterProjectile>();
            projectile.Initialize(gameObject, waterLifetime);

            if (waterMaterial == null)
            {
                Shader waterShader = Shader.Find("Universal Render Pipeline/Lit");
                if (waterShader != null)
                {
                    waterMaterial = new Material(waterShader);
                    waterMaterial.SetColor("_BaseColor", new Color(0.05f, 0.65f, 1f));
                    waterMaterial.SetFloat("_Smoothness", 0.8f);
                }
            }
            if (waterMaterial != null)
            {
                droplet.GetComponent<Renderer>().sharedMaterial = waterMaterial;
            }
        }

        private static bool WasPunchPressed()
        {
            bool keyboard = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
            bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool gamepad = Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame;
            return keyboard || mouse || gamepad;
        }

        private static bool IsWaterPressed()
        {
            bool keyboard = Keyboard.current != null && Keyboard.current.qKey.isPressed;
            bool gamepad = Gamepad.current != null && Gamepad.current.leftTrigger.isPressed;
            return keyboard || gamepad;
        }
    }
}
