using System;
using static Character_Stats.Character_Stats;
using HarmonyLib;
using UnityEngine;

namespace Armor.Patches;

[HarmonyPatch]
internal static class DamageReductionPatch
{
    /// <summary>
    /// Prefix on PlayerHealth.Hurt — reduces incoming damage based on
    /// the player's combined Health + Strength upgrade levels.
    /// 
    /// Formula: taken = baseDamage - (combinedLevel * reductionPerLevel * baseDamage)
    ///        = baseDamage * (1 - combinedLevel * reductionPerLevel)
    /// 
    /// Example: Health 5, Strength 5, reductionPerLevel 0.02
    ///   → reduction = 10 * 0.02 = 0.20 (20%)
    ///   → taken = baseDamage * 0.80
    /// </summary>
    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Hurt))]
    [HarmonyPrefix]
    private static void Hurt_Prefix(PlayerHealth __instance, ref int _damage)
    {
        if (!Armor.EnableArmor.Value)
            return;

        if (_damage <= 0)
            return;

        if (!AreStatsReady)
            return;

        try
        {
            // Get the player avatar that owns this PlayerHealth
            var playerAvatar = __instance.playerAvatar;
            if (playerAvatar == null)
                return;

            // Only apply to the local player
            string steamId = SemiFunc.PlayerGetSteamID(playerAvatar);
            string? localId = GetLocalSteamId();
            if (localId == null || steamId != localId)
                return;

            // Read stat levels from Character Stats
            int healthLevel = GetUpgradeLevel(steamId, "Health");
            int strengthLevel = GetUpgradeLevel(steamId, "Strength");
            int combinedLevel = healthLevel + strengthLevel;

            if (combinedLevel <= 0)
                return;

            // Calculate reduction
            float reductionPerLevel = Armor.ReductionPerLevel.Value;
            float maxReduction = Armor.MaxReduction.Value;

            float reductionPercent = combinedLevel * reductionPerLevel;
            reductionPercent = Math.Min(reductionPercent, maxReduction);

            // Apply reduction
            int originalDamage = _damage;
            float damageMultiplier = 1f - reductionPercent;
            _damage = Math.Max(Mathf.RoundToInt(originalDamage * damageMultiplier), 1);

            Armor.Logger.LogDebug(
                $"Armor: {originalDamage} -> {_damage} dmg " +
                $"({reductionPercent:P0} reduction, " +
                $"Health: {healthLevel}, Strength: {strengthLevel}, combined: {combinedLevel})");
        }
        catch (Exception ex)
        {
            Armor.Logger.LogError($"DamageReduction exception: {ex.Message}");
        }
    }
}
