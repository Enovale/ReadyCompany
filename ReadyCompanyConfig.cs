using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;

namespace ReadyCompany
{
    public class ReadyCompanyConfig: SyncedConfig2<ReadyCompanyConfig>
    {
        [field: SyncedEntryField] public SyncedEntry<bool> RequireReadyToStart { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<bool> AutoStartWhenReady { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<int> PercentageForReady { get; private set; }

        public ReadyCompanyConfig(ConfigFile cfg) : base(MyPluginInfo.PLUGIN_GUID)
        {
            RequireReadyToStart = cfg.BindSyncedEntry("Features", "RequireReadyToStart", false, "Require the lobby to be Ready to pull the ship lever.");
            AutoStartWhenReady = cfg.BindSyncedEntry("Features", "AutoStartWhenReady", false, "Automatically pull the ship lever when the lobby is Ready.");
            PercentageForReady = cfg.BindSyncedEntry("Tuning", "PercentageForReady", 100, "What percentage of ready players is needed for the lobby to be considered \"Ready\".");
            RequireReadyToStart.Changed += (_, _) => ReadyHandler.UpdateReadyMap();
            AutoStartWhenReady.Changed += (_, _) => ReadyHandler.UpdateReadyMap();
            RequireReadyToStart.Changed += (_, _) => ReadyHandler.UpdateReadyMap();
            ConfigManager.Register(this); 
        }
    }
}