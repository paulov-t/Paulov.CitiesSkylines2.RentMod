using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
using Game.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;

namespace PaulovRentMod.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RentAdjustSystem))]
    public partial class EconomySystem : GameSystemBase
    {
        private static ILog log = Mod.log;

        private EntityQuery m_EconomyParameterQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadWrite<EconomyParameterData>());

            log?.Info($"{nameof(EconomySystem)} created.");
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode != GameMode.Game || (purpose != Purpose.LoadGame && purpose != Purpose.NewGame))
            {
                log?.Debug($"{nameof(EconomySystem)}: Not in game mode. Disabling.");
                Enabled = false;
                return;
            }

            ApplyBaseParameters();
        }

        private void ApplyBaseParameters()
        {
            log?.Info("Applying base economy parameters...");
            var entity = m_EconomyParameterQuery.GetSingletonEntity();
            var economyParams = EntityManager.GetComponentData<EconomyParameterData>(entity);
            economyParams.m_MixedBuildingCompanyRentPercentage = 0;

            EntityManager.SetComponentData(entity, economyParams);

            log?.Info("Base economy parameters applied.");
        }

        protected override void OnUpdate()
        {
        }
    }
}
