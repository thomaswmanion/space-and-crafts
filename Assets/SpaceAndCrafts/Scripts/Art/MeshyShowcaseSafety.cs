using SpaceAndCrafts.Crafting;
using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Meshy showcase props must never carry scaled Boing triggers — FBX×100 lossy scale
    /// turns a 0.9 radius into a ~180m OnTriggerStay launch storm.
    /// </summary>
    public static class MeshyShowcaseSafety
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Sanitize()
        {
            var root = GameObject.Find("ART_MeshyCriticalShowcase");
            if (root == null)
            {
                return;
            }

            foreach (var boing in root.GetComponentsInChildren<BoingOMatic>(true))
            {
                if (boing != null)
                {
                    Object.Destroy(boing);
                }
            }

            foreach (var col in root.GetComponentsInChildren<Collider>(true))
            {
                if (col != null && col.isTrigger)
                {
                    Object.Destroy(col);
                }
            }

            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                if (rb != null)
                {
                    Object.Destroy(rb);
                }
            }
        }
    }
}
