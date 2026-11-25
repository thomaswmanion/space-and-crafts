using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    [CreateAssetMenu(
        fileName = "CollectibleDefinition",
        menuName = "Space & Crafts/Gameplay/Collectible Definition")]
    public sealed class CollectibleDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private CollectibleCategory category = CollectibleCategory.Component;
        [SerializeField] [TextArea] private string description;

        public string Id => id;
        public string DisplayName => displayName;
        public CollectibleCategory Category => category;
        public string Description => description;
    }
}
