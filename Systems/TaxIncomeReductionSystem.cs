using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
using Game.Prefabs.Modes;
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace PaulovRentMod.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BalancedRentSystem))]
    public partial class TaxIncomeReductionSystem : GameSystemBase
    {
        private static ILog log = Mod.log;

        private EntityQuery m_GameModeSettingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_GameModeSettingQuery = GetEntityQuery(ComponentType.ReadWrite<ModeSettingData>());
            log?.Info("TaxReductionSystem Created.");
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode != GameMode.Game || (purpose != Purpose.LoadGame && purpose != Purpose.NewGame))
            {
                log?.Debug($"{nameof(TaxIncomeReductionSystem)}: Not in game mode or not loading/starting a game. Disabling.");
                base.Enabled = false;
                return;
            }

            if (m_GameModeSettingQuery.IsEmpty)
            {
                log?.Warn("ModeSettingData not found. Cannot adjust tax parameters. Disabling system.");
                base.Enabled = false;
                return;
            }

            TaxIncomeUpdate();
            
        }

        //private static DateTime LastUpdate = DateTime.MinValue;

        protected override void OnUpdate()
        {
            //bool isFirstUpdate = LastUpdate == DateTime.MinValue;
            //if (LastUpdate > DateTime.Now.AddSeconds(-1))
            //    return;

            //LastUpdate = DateTime.Now;

            //TaxIncomeUpdate();
        }

        void TaxIncomeUpdate()
        {
            log?.Info($"Applying {nameof(TaxIncomeReductionSystem)} parameter adjustments...");
            try
            {
                Entity entity = m_GameModeSettingQuery.GetSingletonEntity();
                ModeSettingData data = base.EntityManager.GetComponentData<ModeSettingData>(entity);
                data.m_TaxPaidMultiplier = new float3(0.33f, 0.33f, 0.33f);
                data.m_SupportPoorCitizens = false;
                data.m_EnableGovernmentSubsidies = false;
                data.m_MinimumWealth = 0;
                base.EntityManager.SetComponentData(entity, data);
            }
            catch (System.Exception exception)
            {
                log?.Error(exception, $"Failed to set tax parameters in {nameof(TaxIncomeReductionSystem)}!");
            }
            finally
            {
            }
        }
    }
}
