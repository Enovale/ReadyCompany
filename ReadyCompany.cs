using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using ReadyCompany.Patches;

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
    internal static ReadyInputs? InputActions { get; set; }
    internal new static ReadyCompanyConfig Config = null!; 

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        Config = new ReadyCompanyConfig(base.Config);
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
        var showPopupCheckbox = new BoolCheckBoxConfigItem(Config.ShowPopup, false);
        LethalConfigManager.AddConfigItem(showPopupCheckbox);
        var playSoundCheckbox = new BoolCheckBoxConfigItem(Config.PlaySound, false);
        LethalConfigManager.AddConfigItem(playSoundCheckbox);
        var soundVolumeSlider = new IntSliderConfigItem(Config.SoundVolume, new IntSliderOptions 
        {
            RequiresRestart = false,
            Min = 0,
            Max = 100
        });
        LethalConfigManager.AddConfigItem(soundVolumeSlider);
        var reloadCustomSoundsButton = new GenericButtonConfigItem(ReadyCompanyConfig.TUNING_STRING,
            "Reload Custom Sounds", "Reloads any custom sounds from disk.", "Reload", () => Config.LoadCustomSounds());
        LethalConfigManager.AddConfigItem(reloadCustomSoundsButton);
        var readyInteractionInput = new TextInputFieldConfigItem(Config.CustomReadyInteractionString, false);
        LethalConfigManager.AddConfigItem(readyInteractionInput);
        var unreadyInteractionInput = new TextInputFieldConfigItem(Config.CustomUnreadyInteractionString, false);
        LethalConfigManager.AddConfigItem(unreadyInteractionInput);

        if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"))
            InitializeLobbyCompatibility();
        
        ReadyHandler.InitializeEvents();

        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void InitializeLobbyCompatibility() => LobbyCompatibility.Features.PluginHelper.RegisterPlugin(MyPluginInfo.PLUGIN_GUID, new(MyPluginInfo.PLUGIN_VERSION), LobbyCompatibility.Enums.CompatibilityLevel.Everyone, LobbyCompatibility.Enums.VersionStrictness.Major);

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();
        JoinPatches.Init();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
}