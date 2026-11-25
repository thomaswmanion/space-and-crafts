using System;

namespace SpaceAndCrafts.Crafting
{
    /// <summary>
    /// Defines when a crafted upgrade also satisfies a level's base invention gate.
    /// </summary>
    public static class InventionCompatibility
    {
        public static bool Satisfies(string requiredInventionId, string completedInventionId)
        {
            if (string.IsNullOrWhiteSpace(requiredInventionId)
                || string.IsNullOrWhiteSpace(completedInventionId))
            {
                return false;
            }

            if (string.Equals(requiredInventionId, completedInventionId, StringComparison.Ordinal))
            {
                return true;
            }

            // Turbo is the optional Arrival recipe upgrade: it must still pass every
            // traversal, safety, boss, and primary-quest gate that accepts Standard Boing.
            return requiredInventionId == "boing_o_matic"
                && completedInventionId == "boing_o_matic_turbo";
        }
    }
}
