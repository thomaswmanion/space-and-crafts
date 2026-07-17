using UnityEngine;

namespace SpaceAndCrafts.Combat
{
    public static class ImpactBurst
    {
        private static Material particleMaterial;

        public static void Spawn(Vector3 position, Color color, float size = 1f)
        {
            GameObject effect = new GameObject("Impact Burst");
            effect.transform.position = position;
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.18f;
            main.loop = false;
            main.startLifetime = 0.38f;
            main.startSpeed = 4.2f * size;
            main.startSize = 0.22f * size;
            main.startColor = color;
            main.maxParticles = 12;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 9) });
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f;

            if (particleMaterial == null)
            {
                Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (particleShader == null)
                {
                    particleShader = Shader.Find("Universal Render Pipeline/Lit");
                }
                if (particleShader != null)
                {
                    particleMaterial = new Material(particleShader);
                }
            }
            if (particleMaterial != null)
            {
                effect.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
            }
            particles.Play();
            Object.Destroy(effect, 0.8f);
        }
    }
}
