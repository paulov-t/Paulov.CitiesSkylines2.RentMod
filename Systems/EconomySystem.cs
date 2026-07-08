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

            var rentPriceBase = economyParams.m_RentPriceBuildingZoneTypeBase;
            economyParams.m_RentPriceBuildingZoneTypeBase = new float3(rentPriceBase.x * 3, rentPriceBase.x * 2, rentPriceBase.x * 2);
            economyParams.m_MixedBuildingCompanyRentPercentage = 0;
            // UK based unemployment benefit, 76 GBP per week, multiplied by 4 to match the game's time scale (4 weeks per month)
            economyParams.m_UnemploymentBenefit = 76 * 4;
            // UK based pension, 241 GBP per week, multiplied by 4 to match the game's time scale (4 weeks per month)
            economyParams.m_Pension = 241 * 4;

            EntityManager.SetComponentData(entity, economyParams);

            log?.Info("Base economy parameters applied.");
        }

        protected override void OnUpdate()
        {
        }
    }
}
