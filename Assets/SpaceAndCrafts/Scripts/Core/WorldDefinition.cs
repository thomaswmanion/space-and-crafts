using UnityEngine;

namespace SpaceAndCrafts.Core
{
    /// <summary>
    /// Catalog entry for one of the first ten worlds.
    /// </summary>
    [CreateAssetMenu(menuName = "Space & Crafts/World Definition", fileName = "WorldDefinition")]
    public sealed class WorldDefinition : ScriptableObject
    {
        public string worldId = "rainbow_arrival";
        public string displayName = "Rainbow Kingdom — The Arrival";
        [TextArea(2, 5)] public string fantasyBlurb;
        public string inventionId = "boing_o_matic";
        public string inventionDisplayName = "Boing-O-Matic";
        [TextArea(2, 4)] public string inventionBlurb;
        public string scenePath = "Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity";
        public int worldIndex = 1;
        public Color accentColor = new Color(0.7f, 0.45f, 0.95f);
        public bool systemsPlayable;
        public bool visualsMeetBar;
    }
}
