using BepInEx;
using BepInEx.Logging;
using CSync.Lib;
using HarmonyLib;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using LobbyCompatibility.Enums;
using LobbyCompatibility.Features;

namespace ReadyCompany;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("LethalNetworkAPI", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.sigurd.csync", "5.0.0")] 
public class ReadyCompany : BaseUnityPlugin
{
    public static ReadyCompany Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }
    internal static ReadyInputs InputActions { get; set; } = null!;
    internal new static ReadyCompanyConfig Config = null!; 

    private void Awake()
    {
        Logger = base.Logger;
        Config = new ReadyCompanyConfig(base.Config);
        Instance = this;
        var requireReadyToStartButton = new BoolCheckBoxConfigItem(Config.RequireReadyToStart.Entry, false);
        LethalConfigManager.AddConfigItem(requireReadyToStartButton);
        var autoStartWhenReadyButton = new BoolCheckBoxConfigItem(Config.AutoStartWhenReady.Entry, false);
        LethalConfigManager.AddConfigItem(autoStartWhenReadyButton);
        var percentageForReadySlider = new IntSliderConfigItem(Config.PercentageForReady.Entry, new IntSliderOptions 
        {
            RequiresRestart = false,
            Min = 0,
            Max = 100
        });
        LethalConfigManager.AddConfigItem(percentageForReadySlider);

        if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"))
            PluginHelper.RegisterPlugin(MyPluginInfo.PLUGIN_GUID, new(MyPluginInfo.PLUGIN_VERSION), CompatibilityLevel.ServerOnly, VersionStrictness.Major);
        
        ReadyHandler.InitializeEvents();

        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
}