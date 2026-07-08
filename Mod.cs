using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using PaulovRentMod.Systems;

namespace PaulovRentMod
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(PaulovRentMod)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));

            AssetDatabase.global.LoadSettings(nameof(PaulovRentMod), m_Setting, new Setting(this));

            log.Info("Localization source added.");
            log.Info("Registering systems...");
            //updateSystem.UpdateAt<RentIncreaseSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<BalancedRentSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<TaxIncomeReductionSystem>(SystemUpdatePhase.GameSimulation);          
            //updateSystem.UpdateAt<PropertyDialogSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EconomySystem>(SystemUpdatePhase.GameSimulation);
            log.Info("System registration complete.");

        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }
    }
}
