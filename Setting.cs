using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;

namespace PaulovRentMod
{
    [FileLocation(nameof(PaulovRentMod))]
    [SettingsUIGroupOrder(kToggleGroup, kSliderGroup)]
    [SettingsUIShowGroupName(kToggleGroup, kSliderGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kToggleGroup = "Rent";
        public const string kSliderGroup = "Multiplier";

        public Setting(IMod mod) : base(mod)
        {
        }

        [SettingsUISection(kSection, kToggleGroup)]
        public bool ModEnabled { get; set; } = true;

        [SettingsUISlider(min = 0.1f, max = 10, step = 0.1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(kSection, kSliderGroup)]
        public float RentMultiplier { get; set; } = 1.0f;

        public override void SetDefaults()
        {
            ModEnabled = true;
            RentMultiplier = 1.0f;
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Paulov's Rent Mod" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kToggleGroup), "Rent Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSliderGroup), "Multiplier" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModEnabled)), "Enable Rent Mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModEnabled)), "Enables or disables the rent mod." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RentMultiplier)), "Rent Multiplier" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RentMultiplier)), "Multiplier on top of the system applied. 1.0 = default, 2.0 = double." },
            };
        }

        public void Unload()
        {
        }
    }
}
