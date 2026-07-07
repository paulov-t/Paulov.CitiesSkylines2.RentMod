using Colossal.Logging;
using Game;
using Game.Agents;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Prefabs.Modes;
using Game.Simulation;
using Game.Tools;
using System;
using Colossal.Serialization.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PaulovRentMod.Systems
{
    public partial class TaxIncomeReductionSystem : GameSystemBase
    {
        private static ILog log = Mod.log;

        private EntityQuery m_EconomyParameterQuery;
        private CityStatisticsSystem m_CityStatisticsSystem;
        private SimulationSystem m_SimulationSystem;
        private ResourceSystem m_ResourceSystem;
        private EntityQuery m_ResidentialTaxPayerGroup;
        private EntityQuery m_CommercialTaxPayerGroup;
        private EntityQuery m_IndustrialTaxPayerGroup;
        private EntityQuery m_TaxParameterGroup;
        private EntityQuery m_GameModeSettingQuery;
        private NativeArray<int> m_TaxRates;
        private float3 m_TaxPaidMultiplier;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
            m_ResidentialTaxPayerGroup = GetEntityQuery(ComponentType.ReadWrite<TaxPayer>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Resources>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.ReadOnly<Household>());
            m_CommercialTaxPayerGroup = GetEntityQuery(ComponentType.ReadWrite<TaxPayer>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Resources>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.ReadOnly<ServiceAvailable>());
            m_IndustrialTaxPayerGroup = GetEntityQuery(ComponentType.ReadWrite<TaxPayer>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Resources>(), ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Game.Companies.StorageCompany>(), ComponentType.Exclude<ServiceAvailable>());
            m_TaxParameterGroup = GetEntityQuery(ComponentType.ReadOnly<TaxParameterData>());
            m_GameModeSettingQuery = GetEntityQuery(ComponentType.ReadOnly<ModeSettingData>());
            m_TaxRates = new NativeArray<int>(92, Allocator.Persistent);
            m_TaxRates[0] = 10;
            m_TaxPaidMultiplier = new float3(1f, 1f, 1f);

            log?.Info("TaxReductionSystem Created.");
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode != GameMode.Game || (purpose != Colossal.Serialization.Entities.Purpose.LoadGame && purpose != Colossal.Serialization.Entities.Purpose.NewGame))
            {
                log?.Debug(string.Format("{0}: Not in game mode or not loading/starting a game. Disabling.", nameof(TaxIncomeReductionSystem)));
                base.Enabled = false;
                return;
            }

            if (m_EconomyParameterQuery.IsEmpty)
            {
                log?.Warn("EconomyParameterData not found. Cannot adjust rent parameters. Disabling system.");
                base.Enabled = false;
                return;
            }

            log?.Info($"Applying {nameof(TaxIncomeReductionSystem)} parameter adjustments...");
            ModeSettingData singleton = m_GameModeSettingQuery.GetSingleton<ModeSettingData>();
            try
            {
                if (singleton.m_Enable)
                {
                    log?.Info($"Enabling {nameof(TaxIncomeReductionSystem)}. Reducing tax paid by 50% for all types.");
                    singleton.m_TaxPaidMultiplier = new float3(0.1f, 1.0f, 1.0f); // Reduce tax paid by 50% for all types
                }
                else
                {
                    m_TaxPaidMultiplier = new float3(0.1f, 1f, 1f);
                }
            }
            catch (Exception exception)
            {
                log?.Error(exception, $"Failed to get/set {nameof(TaxIncomeReductionSystem)} parameters in OnGameLoadingComplete!");
            }
            finally
            {
                base.Enabled = false;
                log?.Info(string.Format("{0}: disabled after execution.", nameof(TaxIncomeReductionSystem)));
            }
        }

        protected override void OnUpdate()
        {
        }
    }

}