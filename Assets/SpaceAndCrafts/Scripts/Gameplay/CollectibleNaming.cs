using System.Collections.Generic;
using System.Text;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Pure helpers for readable collectible ids (HUD / craft refuse messages).
    /// </summary>
    public static class CollectibleNaming
    {
        public static string PrettyId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "?";
            }

            return id
                .Replace("component_", string.Empty)
                .Replace("discovery_badge_", string.Empty)
                .Replace("material_", string.Empty)
                .Replace('_', ' ');
        }

        public static string BuildMissingMessage(IEnumerable<string> requiredIds, System.Func<string, bool> hasId)
        {
            var sb = new StringBuilder("Still need: ");
            var first = true;
            foreach (var id in requiredIds)
            {
                if (hasId(id))
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append(", ");
                }

                sb.Append(PrettyId(id));
                first = false;
            }

            return first ? "Something's missing…" : sb.ToString();
        }
    }
}
