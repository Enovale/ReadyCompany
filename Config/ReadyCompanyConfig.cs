using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
using ReadyCompany.Patches;
using ReadyCompany.Util;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ReadyCompany.Config
{
    public class ReadyCompanyConfig : SyncedConfig2<ReadyCompanyConfig>
    {
        [field: SyncedEntryField]
        public SyncedEntry<bool> RequireReadyToStart { get; private set; }

        [field: SyncedEntryField]
        public SyncedEntry<bool> AutoStartWhenReady { get; private set; }

        [field: SyncedEntryField]
        public SyncedEntry<int> PercentageForReady { get; private set; }

        [field: SyncedEntryField]
        public SyncedEntry<bool> DeadPlayersCanVote { get; private set; }

        [field: SyncedEntryField]
        public SyncedEntry<bool> ReadyAllowedWhenNotInShip { get; private set; }

        public ConfigEntry<Color> StatusColor { get; private set; }
        public ConfigEntry<StatusPlacement> StatusPlacement { get; private set; }
        public ConfigEntry<StatusStyle> StatusStyle { get; private set; }
        public bool HudTextEnabled => StatusStyle.Value is not Config.StatusStyle.Popup;
        public bool PopupEnabled => StatusStyle.Value is Config.StatusStyle.Popup or Config.StatusStyle.Both;
        public ConfigEntry<bool> PlaySound { get; private set; }
        public ConfigEntry<int> SoundVolume { get; private set; }

        public ConfigEntry<Color> ReadyBarColor { get; private set; }
        public ConfigEntry<InteractionPreset> ReadyInteractionPreset { get; private set; }
        public ConfigEntry<string> CustomReadyInteractionString { get; private set; }
        public ConfigEntry<Color> UnreadyBarColor { get; private set; }
        public ConfigEntry<InteractionPreset> UnreadyInteractionPreset { get; private set; }
        public ConfigEntry<string> CustomUnreadyInteractionString { get; private set; }

        internal List<AudioClip> CustomPopupSounds { get; private set; } = [];
        internal List<AudioClip> CustomLobbyReadySounds { get; private set; } = [];

        internal const string FEATURES_STRING = "Features";
        internal const string CUSTOMIZATION_STRING = "Customization";
        internal const string INTERACTION_STRING = "Interaction";

        public ReadyCompanyConfig(ConfigFile cfg) : base(MyPluginInfo.PLUGIN_GUID)
        {
            RequireReadyToStart = cfg.BindSyncedEntry(FEATURES_STRING, nameof(RequireReadyToStart), false,
                "Require the lobby to be Ready to pull the ship lever.");
            AutoStartWhenReady = cfg.BindSyncedEntry(FEATURES_STRING, nameof(AutoStartWhenReady), false,
                "Automatically pull the ship lever when the lobby is Ready.");
            PercentageForReady = cfg.BindSyncedEntry(FEATURES_STRING, nameof(PercentageForReady), 100,
                "What percentage of ready players is needed for the lobby to be considered \"Ready\".");
            DeadPlayersCanVote = cfg.BindSyncedEntry(FEATURES_STRING, nameof(DeadPlayersCanVote), true,
                "Whether or not dead players are allowed to participate in the ready check or are forced to be ready. (During Company visits)");
            ReadyAllowedWhenNotInShip = cfg.BindSyncedEntry(FEATURES_STRING, nameof(ReadyAllowedWhenNotInShip), true,
                "Whether or not people who are outside the ship are forced to not be ready. (During Company visits)");
            RequireReadyToStart.Changed += (_, _) => ReadyHandler.UpdateReadyMap();
            AutoStartWhenReady.Changed += (_, _) => ReadyHandler.UpdateReadyMap();
            RequireReadyToStart.Changed += (_, _) => ReadyHandler.UpdateReadyMap();
            DeadPlayersCanVote.Changed += (_, _) => ReadyHandler.UpdateReadyMap();
            ReadyAllowedWhenNotInShip.Changed += (_, _) => ReadyHandler.UpdateReadyMap();

            StatusColor = cfg.Bind(CUSTOMIZATION_STRING, nameof(StatusColor), new Color(0.9528f, 0.3941f, 0, 1),
                "The color of the ready status text.");
            StatusPlacement = cfg.Bind(CUSTOMIZATION_STRING, nameof(StatusPlacement),
                Config.StatusPlacement.AboveHotbar, "Where to place the text showing the ready status.");
            StatusStyle = cfg.Bind(CUSTOMIZATION_STRING, nameof(StatusStyle), Config.StatusStyle.HudText,
                "How to show the current ready status. HudText is a persistent bit of text placed according to StatusPlacement, " +
                "Popup is the vanilla hint popup with the ready status inside whenever it changes..");
            PlaySound = cfg.Bind(CUSTOMIZATION_STRING, nameof(PlaySound), true,
                "Whether or not to play a sound when the ready status changes.");
            SoundVolume = cfg.Bind(CUSTOMIZATION_STRING, nameof(SoundVolume), 100, "The volume of the popup sound.");
            StatusPlacement.SettingChanged += (_, _) => HUDPatches.UpdatePlacementBasedOnConfig(StatusPlacement.Value);
            StatusStyle.SettingChanged += (_, _) => HUDPatches.SetTextActive(HudTextEnabled);

            ReadyBarColor = cfg.Bind(INTERACTION_STRING, nameof(ReadyBarColor), Color.white,
                "What color the bar should be when holding the bind.");
            UnreadyBarColor = cfg.Bind(INTERACTION_STRING, nameof(UnreadyBarColor), Color.red,
                "What color the bar should be when holding the bind.");
            ReadyInteractionPreset = cfg.Bind(INTERACTION_STRING, nameof(ReadyInteractionPreset),
                InteractionPreset.LongHold, "How you want to press the ready bind to ready up.");
            UnreadyInteractionPreset = cfg.Bind(INTERACTION_STRING, nameof(UnreadyInteractionPreset),
                InteractionPreset.TripleTap, "How you want to press the unready bind to unready.");
            CustomReadyInteractionString = cfg.Bind(INTERACTION_STRING, nameof(CustomReadyInteractionString),
                "hold(duration = 1.5)", "Specify a custom interaction type for this input.");
            CustomUnreadyInteractionString = cfg.Bind(INTERACTION_STRING, nameof(CustomUnreadyInteractionString),
                "multiTap(tapTime = 0.2, tapCount = 3)", "Specify a custom interaction type for this input.");
            ReadyInteractionPreset.SettingChanged += (_, _) =>
                SetInteractionStringBasedOnPreset(CustomReadyInteractionString, ReadyInteractionPreset.Value);
            UnreadyInteractionPreset.SettingChanged += (_, _) =>
                SetInteractionStringBasedOnPreset(CustomUnreadyInteractionString, UnreadyInteractionPreset.Value);
            CustomReadyInteractionString.SettingChanged += (_, _) => UpdateBindingsInteractions();
            CustomUnreadyInteractionString.SettingChanged += (_, _) => UpdateBindingsInteractions();

            LoadCustomSounds();

            ConfigManager.Register(this);
        }

        internal void LoadCustomSounds()
        {
            CustomPopupSounds.Clear();
            CustomLobbyReadySounds.Clear();
            var soundPath =
                Directory.CreateDirectory(Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_GUID, "CustomSounds"));
            var lobbyReadyPath = soundPath.CreateSubdirectory("LobbyReady");

            foreach (var soundFile in soundPath.EnumerateFiles("*.*", SearchOption.AllDirectories))
            {
                if (!new[] { ".wav", ".mp3", ".ogg" }.Contains(soundFile.Extension))
                    continue;

                var soundClip = AudioUtility.GetAudioClip(soundFile.FullName);

                if (soundClip == null)
                {
                    ReadyCompany.Logger.LogError($"Failed to load audio clip: {soundFile}");
                    continue;
                }

                if (soundFile.IsInside(lobbyReadyPath))
                {
                    CustomLobbyReadySounds.Add(soundClip);
                    continue;
                }

                ReadyCompany.Logger.LogDebug($"Loading audioclip {soundFile}");
                CustomPopupSounds.Add(soundClip);
            }
        }

        internal void UpdateCustomInteractionStringsBasedOnPresets()
        {
            SetInteractionStringBasedOnPreset(CustomReadyInteractionString, ReadyInteractionPreset.Value);
            SetInteractionStringBasedOnPreset(CustomUnreadyInteractionString, UnreadyInteractionPreset.Value);
        }

        private void SetInteractionStringBasedOnPreset(ConfigEntry<string> config, InteractionPreset value) =>
            config.Value = GetInteractionStringBasedOnPreset(value) ?? config.Value;

        private string? GetInteractionStringBasedOnPreset(InteractionPreset value) =>
            value switch
            {
                InteractionPreset.Hold => "hold(duration = 0.75)",
                InteractionPreset.LongHold => "hold(duration = 1.5)",
                InteractionPreset.Press => "press",
                InteractionPreset.DoubleTap => "multiTap(tapTime = 0.2, tapCount = 2)",
                InteractionPreset.TripleTap => "multiTap(tapTime = 0.2, tapCount = 3)",
                InteractionPreset.Custom => null,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };

        internal void UpdateBindingsInteractions()
        {
            if (ReadyCompany.InputActions == null)
                return;
            
            ReadyCompany.Logger.LogDebug("Update bindings!");
            UpdateBindingInteraction(ReadyCompany.InputActions.ReadyInput, CustomReadyInteractionString.Value);
            UpdateBindingInteraction(ReadyCompany.InputActions.UnreadyInput, CustomUnreadyInteractionString.Value);
        }

        internal void UpdateBindingInteraction(InputAction action, string interactions)
        {
            foreach (var binding in action.bindings)
            {
                action.ChangeBinding(binding).To(binding with {interactions = interactions});
            }
        }
    }
}