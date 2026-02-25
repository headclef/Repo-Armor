using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Armor;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("headclef.CharacterStats", BepInDependency.DependencyFlags.HardDependency)]
public class Armor : BaseUnityPlugin
{
    private const string PluginGuid = "headclef.Armor";
    private const string PluginName = "Armor";
    private const string PluginVersion = "1.0.0";

    internal static Armor Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    // ── Config ──
    internal static ConfigEntry<bool> EnableArmor = null!;
    internal static ConfigEntry<float> ReductionPerLevel = null!;
    internal static ConfigEntry<float> MaxReduction = null!;

    private void Awake()
    {
        Instance = this;
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        BindConfiguration();
        Harmony ??= new Harmony(Info.Metadata.GUID);
        Harmony.PatchAll();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    private void OnDestroy()
    {
        Harmony?.UnpatchSelf();
    }

    private void BindConfiguration()
    {
        const string section = "Damage Reduction";

        EnableArmor = Config.Bind(section, "Enable", true,
            "Enable damage reduction based on Health + Strength stats.");

        ReductionPerLevel = Config.Bind(section, "Reduction Per Combined Level", 0.02f,
            new ConfigDescription(
                "Damage reduction percentage per combined level of Health + Strength. " +
                "E.g. 0.02 = 2% per level. At combined 10: 20% reduction.",
                new AcceptableValueRange<float>(0.005f, 0.1f)));

        MaxReduction = Config.Bind(section, "Max Reduction", 0.50f,
            new ConfigDescription(
                "Maximum damage reduction cap (percentage). 0.50 = 50% max reduction.",
                new AcceptableValueRange<float>(0.1f, 0.9f)));
    }
}
