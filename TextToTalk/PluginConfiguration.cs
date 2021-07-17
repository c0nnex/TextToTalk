﻿using Amazon.Polly;
using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using TextToTalk.Backends;
using TextToTalk.GameEnums;
using TextToTalk.Migrations;

// ReSharper disable InconsistentNaming

namespace TextToTalk
{
    public class PluginConfiguration : IPluginConfiguration
    {
        private const string DefaultPreset = "Default";

        #region Obsolete Members
        [Obsolete("Use EnabledChatTypesPresets.")]
        public bool EnableAllChatTypes { get; set; }

        [Obsolete("Use EnabledChatTypesPresets.")]
        // ReSharper disable once CollectionNeverUpdated.Global
        public IList<int> EnabledChatTypes { get; set; }

        [Obsolete("Use VoicePresets.")]
        public int Rate { get; set; }

        [Obsolete("Use VoicePresets.")]
        public int Volume { get; set; }

        [Obsolete("Use VoicePresets.")]
        public string VoiceName { get; set; }

        [Obsolete("Use Backend.")]
        public bool UseWebsocket { get; set; }

        /// <summary>
        /// <c>true</c> if it is not the first time, <c>false</c> if the first time handler has not run before. This was named horribly.
        /// </summary>
        [Obsolete("Use InitializedEver.")]
        public bool FirstTime { get; set; }
        #endregion

        public int Version { get; set; }

        public bool Enabled { get; set; }

        public bool UseKeybind { get; set; }
        public VirtualKey.Enum ModifierKey { get; set; }
        public VirtualKey.Enum MajorKey { get; set; }

        public bool MigratedTo1_5 { get; set; }

        public bool MigratedTo1_6 { get; set; }

        public IList<Trigger> Bad { get; set; }
        public IList<Trigger> Good { get; set; }

        public int CurrentPresetId { get; set; }
        public IList<EnabledChatTypesPreset> EnabledChatTypesPresets { get; set; }

        public int CurrentVoicePresetId { get; set; }
        public IList<VoicePreset> VoicePresets { get; set; }

        public TTSBackend Backend { get; set; }

        public int WebsocketPort { get; set; }

        public bool NameNpcWithSay { get; set; } = true;

        public bool EnableNameWithSay { get; set; } = true;

        public bool DisallowMultipleSay { get; set; }

        public bool ReadFromQuestTalkAddon { get; set; } = true;

        public bool UseGenderedVoicePresets { get; set; }

        public int UngenderedVoicePresetId { get; set; }

        public int MaleVoicePresetId { get; set; }

        public int FemaleVoicePresetId { get; set; }

        public string PollyVoice { get; set; }

        public string PollyVoiceUngendered { get; set; }

        public string PollyVoiceMale { get; set; }

        public string PollyVoiceFemale { get; set; }

        public string PollyEngine { get; set; } = Engine.Neural;

        public string PollyRegion { get; set; }

        public int PollySampleRate { get; set; } = 22050;

        public float PollyVolume { get; set; } = 1.0f;

        public int PollyPlaybackRate { get; set; } = 100;

        public bool CancelQueueOnToggle { get; set; } = false;
        public string SaysPostfix { get; set; } = "says";

        public List<TextReplacer> Replacers { get; set; }


        [JsonIgnore]
        public bool InitializedEver
        {
#pragma warning disable 618
            get => FirstTime;
            set => FirstTime = value;
#pragma warning restore 618
        }
        [JsonIgnore]
        public bool Debug { get; set; } = false;

        [JsonIgnore] private DalamudPluginInterface pluginInterface;
        
        public PluginConfiguration()
        {
            Enabled = true;

            Bad = new List<Trigger>();
            Good = new List<Trigger>();

            ModifierKey = VirtualKey.Enum.VkControl;
            MajorKey = VirtualKey.Enum.VkN;
        }

        public void Initialize(DalamudPluginInterface pi)
        {
            this.pluginInterface = pi;

            EnabledChatTypesPresets ??= new List<EnabledChatTypesPreset>();
            VoicePresets ??= new List<VoicePreset>();
            Replacers ??= new List<TextReplacer>() { new TextReplacer() { ChatText = "ul'dah", ReplaceWith = "uldaahr" } };
            if (!InitializedEver)
            {
                EnabledChatTypesPresets.Add(new EnabledChatTypesPreset
                {
                    Id = 0,
                    EnabledChatTypes = new List<int>
                    {
                        (int) XivChatType.Say,
                        (int) XivChatType.Shout,
                        (int) XivChatType.Party,
                        (int) AdditionalChatType.BeneficialEffectOnYou,
                        (int) AdditionalChatType.BeneficialEffectOnYouEnded,
                        (int) AdditionalChatType.BeneficialEffectOnOtherPlayer,
                    },
                    Name = DefaultPreset,
                    UseKeybind = false,
                    ModifierKey = VirtualKey.Enum.VkShift,
                    MajorKey = VirtualKey.Enum.Vk0,
                });

                using var ss = new SpeechSynthesizer();
                VoicePresets.Add(new VoicePreset
                {
                    Id = 0,
                    Rate = ss.Rate,
                    Volume = ss.Volume,
                    VoiceName = ss.GetInstalledVoices().First().VoiceInfo.Name,
                    Name = DefaultPreset,
                });
                InitializedEver = true;
                MigratedTo1_5 = true;
                MigratedTo1_6 = true;
            }

            if (InitializedEver)
            {
                var migrations = new IConfigurationMigration[] { new Migration1_5(), new Migration1_6() };
                foreach (var migration in migrations)
                {
                    if (migration.ShouldMigrate(this))
                    {
                        migration.Migrate(this);
                    }
                }
            }
            Replacers.RemoveAll(vr => String.IsNullOrEmpty(vr.ChatText));
            Save();
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }

        public EnabledChatTypesPreset GetCurrentEnabledChatTypesPreset()
        {
            return EnabledChatTypesPresets.First(p => p.Id == CurrentPresetId);
        }

        public EnabledChatTypesPreset NewChatTypesPreset()
        {
            var highestId = EnabledChatTypesPresets.Select(p => p.Id).Max();
            var preset = new EnabledChatTypesPreset
            {
                Id = highestId + 1,
                EnabledChatTypes = new List<int>(),
                Name = "New preset",
                UseKeybind = false,
                ModifierKey = VirtualKey.Enum.VkShift,
                MajorKey = VirtualKey.Enum.Vk0,
            };

            EnabledChatTypesPresets.Add(preset);

            return preset;
        }

        public void SetCurrentEnabledChatTypesPreset(int presetId)
        {
            CurrentPresetId = presetId;
        }

        public VoicePreset GetCurrentVoicePreset()
        {
            return VoicePresets.First(p => p.Id == CurrentVoicePresetId);
        }

        public VoicePreset GetCurrentUngenderedVoicePreset()
        {
            return VoicePresets.FirstOrDefault(p => p.Id == UngenderedVoicePresetId);
        }

        public VoicePreset GetCurrentMaleVoicePreset()
        {
            return VoicePresets.FirstOrDefault(p => p.Id == MaleVoicePresetId);
        }

        public VoicePreset GetCurrentFemaleVoicePreset()
        {
            return VoicePresets.FirstOrDefault(p => p.Id == FemaleVoicePresetId);
        }

        public VoicePreset NewVoicePreset()
        {
            using var ss = new SpeechSynthesizer();

            var highestId = VoicePresets.Select(p => p.Id).Max();
            var preset = new VoicePreset
            {
                Id = highestId + 1,
                Rate = ss.Rate,
                Volume = ss.Volume,
                VoiceName = ss.GetInstalledVoices().First().VoiceInfo.Name,
                Name = "New preset",
            };

            VoicePresets.Add(preset);

            return preset;
        }

        public void SetCurrentVoicePreset(int presetId)
        {
            CurrentVoicePresetId = presetId;
        }
    }
}
