using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceAndCrafts.Crafting
{
    /// <summary>
    /// One craft outcome at a BuildSite — parts list + invention id + optional launch flavor.
    /// </summary>
    [Serializable]
    public sealed class BuildRecipe
    {
        public string displayName = "Standard Boing";
        public string inventionId = "boing_o_matic";
        public List<string> requiredCollectibleIds = new();
        [Tooltip("If > 0, applied to BoingOMatic after assemble.")]
        public float launchSpeedOverride = -1f;
        public string launchToast = "BOING!";
        public Color accentTint = Color.white;
    }
}
