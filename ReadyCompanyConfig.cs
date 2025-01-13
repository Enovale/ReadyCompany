using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
using ReadyCompany.Patches;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ReadyCompany
{
    public class ReadyCompanyConfig: SyncedConfig2<ReadyCompanyConfig>
    {
        [field: SyncedEntryField] public SyncedEntry<bool> RequireReadyToStart { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<bool> AutoStartWhenReady { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<int> PercentageForReady { get; private set; }
        
        //public ConfigEntry<InteractionType> ReadyUpInteraction { get; private set; } 
        //public ConfigEntry<InteractionType> UnreadyInteraction { get; private set; } 
        //public ConfigEntry<float> HoldTimeNeeded { get; private set; } 
        //public ConfigEntry<int> MultiTapsNeeded { get; private set; }
        
        public ConfigEntry<Color> ReadyBarColor { get; private set; }
        public ConfigEntry<string> CustomReadyInteractionString { get; private set; }
        public ConfigEntry<Color> UnreadyBarColor { get; private set; }
        public ConfigEntry<string> CustomUnreadyInteractionString { get; private set; }

        private const string FEATURES_STRING = "Features";
        private const string TUNING_STRING = "Tuning";
        private const string INTERACTION_STRING = "Interaction";

        public ReadyCompanyConfig(ConfigFile cfg) : base(MyPluginInfo.PLUGIN_GUID)
        {
            RequireReadyToStart = cfg.BindSyncedEntry(FEATURES_STRING, nameof(RequireReadyToStart), false, "Require the lobby to be Ready to pull the ship lever.");
            AutoStartWhenReady = cfg.BindSyncedEntry(FEATURES_STRING, nameof(AutoStartWhenReady), false, "Automatically pull the ship lever when the lobby is Ready.");
            PercentageForReady = cfg.BindSyncedEntry(TUNING_STRING, nameof(PercentageForReady), 100, "What percentage of ready players is needed for the lobby to be considered \"Ready\".");
            RequireReadyToStart.Changed += (_, _) => ReadyHandler.UpdateReadyMap();
            AutoStartWhenReady.Changed += (_, _) => ReadyHandler.UpdateReadyMap();
            RequireReadyToStart.Changed += (_, _) => ReadyHandler.UpdateReadyMap();

            ReadyBarColor = cfg.Bind(INTERACTION_STRING, nameof(ReadyBarColor), Color.white, "What color the bar should be when holding the bind.");
            UnreadyBarColor = cfg.Bind(INTERACTION_STRING, nameof(UnreadyBarColor), Color.red, "What color the bar should be when holding the bind.");
            CustomReadyInteractionString = cfg.Bind(INTERACTION_STRING, nameof(CustomReadyInteractionString), "hold(duration = 1.5)", "Specify a custom interaction type for this input.");
            CustomUnreadyInteractionString = cfg.Bind(INTERACTION_STRING, nameof(CustomUnreadyInteractionString), "multiTap(tapTime = 0.2, tapCount = 3)", "Specify a custom interaction type for this input.");
            CustomReadyInteractionString.SettingChanged += (_, _) => UpdateBindingsBasedOnConfig();
            CustomUnreadyInteractionString.SettingChanged += (_, _) => UpdateBindingsBasedOnConfig();

            ConfigManager.Register(this);
        }

        internal void UpdateBindingsBasedOnConfig()
        {
            if (ReadyCompany.InputActions == null) return;

            var kbmBindingReady = ReadyCompany.InputActions.ReadyInput.bindings[0];
            kbmBindingReady.overrideInteractions = CustomReadyInteractionString.Value;
            ReadyCompany.InputActions.ReadyInput.ApplyBindingOverride(0, kbmBindingReady);

            var gmpBindingReady = ReadyCompany.InputActions.ReadyInput.bindings[1];
            gmpBindingReady.overrideInteractions = CustomReadyInteractionString.Value;
            ReadyCompany.InputActions.ReadyInput.ApplyBindingOverride(1, gmpBindingReady);

            var kbmBindingUnready = ReadyCompany.InputActions.UnreadyInput.bindings[0];
            kbmBindingUnready.overrideInteractions = CustomUnreadyInteractionString.Value;
            ReadyCompany.InputActions.UnreadyInput.ApplyBindingOverride(0, kbmBindingUnready);

            var gmpBindingUnready = ReadyCompany.InputActions.UnreadyInput.bindings[1];
            gmpBindingUnready.overrideInteractions = CustomUnreadyInteractionString.Value;
            ReadyCompany.InputActions.UnreadyInput.ApplyBindingOverride(1, gmpBindingUnready);
        }
    }
}