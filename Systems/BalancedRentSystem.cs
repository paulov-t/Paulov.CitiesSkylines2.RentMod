using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PaulovRentMod.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RentAdjustSystem))]
    public partial class BalancedRentSystem : GameSystemBase
    {
        private EntityQuery m_BuildingQuery;
        //private EntityQuery m_PropertyQuery;
        private EntityQuery m_BuildingPropertyQuery;
        private readonly Dictionary<Entity, int> m_PreviousRent = new();


        // Tuning values
        private const float VacancyDiscountStrong = 6.0f;
        private const float VacancyDiscountMedium = 6.0f;
        private const float HighDemandIncrease = 8.0f;

        private const float IncomeRentLimit = 0.5f;

        // smoothing amount
        private const float RentChangeSpeed = 0.5f;

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_BuildingQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<Building>(),
                    ComponentType.ReadOnly<ResidentialProperty>(),
                },
                Options = EntityQueryOptions.IncludePrefab
            });

         
            //m_PropertyQuery = GetEntityQuery(
            //    ComponentType.ReadOnly<PropertyRenter>()
            //);

            m_BuildingPropertyQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadWrite<BuildingPropertyData>() },
                Options = EntityQueryOptions.IncludePrefab
            });

            Mod.log?.Info($"{nameof(BalancedRentSystem)} created.");
        }

        private Unity.Mathematics.Random random = new Unity.Mathematics.Random();
        private static DateTime LastUpdate = DateTime.MinValue;

        private Dictionary<int, float> adjustedBuildingsPreChange = new Dictionary<int, float>();

        protected override void OnUpdate()
        {
            bool isFirstUpdate = LastUpdate == DateTime.MinValue;
            if (LastUpdate > DateTime.Now.AddSeconds(-1))
                return;

            LastUpdate = DateTime.Now;

            EntityManager em = EntityManager;

            using NativeArray<Entity> entities =
                m_BuildingQuery.ToEntityArray(
                    Allocator.Temp);

            //Mod.log?.Info($"Found {entities.Length} buildings.");
            var unresolvedBuildings = new List<Entity>();

            if (isFirstUpdate)
            {
                Mod.log?.Info($"Found {entities.Length} buildings using {nameof(m_BuildingQuery)}.");

                foreach (Entity building in entities)
                {
                    AdjustBuildingRent(building, em, ref unresolvedBuildings);
                }

                entities.Dispose();
                return;
            }

            foreach (Entity building in entities)
            {
                if (adjustedBuildingsPreChange.ContainsKey(building.Index))
                {
                    var bpd = EntityManager.GetComponentData<BuildingPropertyData>(building);
                    bpd.m_SpaceMultiplier = adjustedBuildingsPreChange[building.Index];
                    EntityManager.SetComponentData(building, bpd);
                    adjustedBuildingsPreChange.Remove(building.Index);
                }
            }

            foreach (Entity building in entities)
            {
                AdjustBuildingRent(building, em, ref unresolvedBuildings);
            }

            // The easiest way to resolve the buildings that havent been fixed is by adding a PropertyOnMarket component (that is auto removed by the game after)
            foreach (Entity building in unresolvedBuildings)
            {
                if (!em.HasComponent<PropertyOnMarket>(building))
                {
                    em.AddComponent<PropertyOnMarket>(building);
                }
            }

        }

        private void AdjustBuildingRent(
            Entity building,
            EntityManager em,
            ref List<Entity> unresolvedBuildings
            )
        {
            // Only process residential properties
            if (!IsResidentialOnly(building, em))
            {
                return;
            }

            if (!em.HasComponent<PropertyOnMarket>(building))
            {
                unresolvedBuildings.Add(building);
                return;
            }

            PropertyOnMarket market =
                em.GetComponentData<PropertyOnMarket>(building);
            
            int vanillaRent =
                market.m_AskingRent;

            if (vanillaRent <= 0)
                return;


            float multiplier = 5f * (Mod.Settings?.RentMultiplier ?? 1.0f);

            //
            // Vacancy adjustment
            //
            multiplier *= GetVacancyModifier(
                building,
                em);

            //
            // Demand hook
            // Add your demand API here later
            //
            multiplier *= 7f;

            int targetRent =
                MathfRound(vanillaRent * multiplier);

            //
            // Affordability cap
            //
            targetRent =
                ApplyAffordabilityCap(
                    building,
                    targetRent,
                    em);
            //
            // Smooth changes
            //
            int finalRent =
                SmoothRent(
                    building,
                    targetRent);

            finalRent =
                math.max(1, finalRent);

            //
            // Update market
            //
            market.m_AskingRent =
                finalRent;

            em.SetComponentData(
                building,
                market);

            //
            // Update existing tenants
            //
            UpdateTenantRents(
                building,
                finalRent,
                em);
        }

        private bool IsResidentialOnly(Entity building, EntityManager em)
        {
            return true;
            //if (!em.HasComponent<PrefabRef>(building))
            //    return false;

            //Entity prefab = em.GetComponentData<PrefabRef>(building).m_Prefab;

            //if (!em.HasComponent<BuildingPropertyData>(prefab))
            //    return false;

            //BuildingPropertyData data = em.GetComponentData<BuildingPropertyData>(prefab);

            //// Check if building has residential properties and no other property types
            //return data.m_ResidentialProperties > 0 
            //    //&& 
            //    //   data.m_CommercialProperties == 0 && 
            //    //   data.m_IndustrialProperties == 0 && 
            //    //   data.m_OfficeProperties == 0
            //       ;
        }

        private float GetVacancyModifier(
            Entity building,
            EntityManager em)
        {

            if (!em.HasComponent<PrefabRef>(building))
                return 1f;


            Entity prefab =
                em.GetComponentData<PrefabRef>(building)
                .m_Prefab;


            if (!em.HasComponent<BuildingPropertyData>(prefab))
                return 1f;

            BuildingPropertyData data =
                em.GetComponentData<BuildingPropertyData>(prefab);

            int capacity =
                data.CountProperties();

            if (capacity <= 0)
                return 1f;

            if (!em.HasBuffer<Renter>(building))
                return VacancyDiscountStrong;

            int occupied =
                em.GetBuffer<Renter>(building)
                .Length;

            float vacancy =
                1f -
                ((float)occupied / capacity);

            if (vacancy >= 0.40f)
                return VacancyDiscountStrong;

            if (vacancy >= 0.20f)
                return VacancyDiscountMedium;

            if (vacancy <= 0.02f)
                return HighDemandIncrease;

            return 1f;
        }




        private int ApplyAffordabilityCap(
            Entity building,
            int rent,
            EntityManager em)
        {

            if (!em.HasBuffer<Renter>(building))
                return rent;
            
            DynamicBuffer<Renter> renters =
                em.GetBuffer<Renter>(building);

            if (renters.Length == 0)
                return rent;
            
            int totalIncome = 0;
            int count = 0;

            foreach (Renter renter in renters)
            {
                Entity household =
                    renter.m_Renter;

                if (!em.HasBuffer<HouseholdCitizen>(household))
                    continue;

                // 
                // Conservative estimate:
                // use current money
                //
                if (!em.HasBuffer<Game.Economy.Resources>(household))
                    continue;

                var resources =
                    em.GetBuffer<Game.Economy.Resources>(household);

                int money =
                    EconomyUtils.GetResources(
                        Resource.Money,
                        resources);

                totalIncome += money;
                count++;
            }

            if (count == 0)
                return rent;

            int average =
                totalIncome / count;

            int maxRent =
                (int)
                (average * IncomeRentLimit);

            return
                math.clamp(
                    math.min(
                    rent,
                    math.max(maxRent, 1)
                    )
                    , (76 * 4), 3000);
        }




        private void UpdateTenantRents(
            Entity building,
            int rent,
            EntityManager em)
        {

            if (!em.HasBuffer<Renter>(building))
                return;


            DynamicBuffer<Renter> renters =
                em.GetBuffer<Renter>(building);

            foreach (Renter renter in renters)
            {

                Entity renterEntity =
                    renter.m_Renter;

                if (!em.HasComponent<PropertyRenter>(renterEntity))
                    continue;

                PropertyRenter property =
                    em.GetComponentData<PropertyRenter>(
                        renterEntity);

                property.m_Rent =
                    rent;

                em.SetComponentData(
                    renterEntity,
                    property);
            }
        }

        private int SmoothRent(
            Entity building,
            int target)
        {

            if (!m_PreviousRent.TryGetValue(
                building,
                out int previous))
            {
                m_PreviousRent[building] =
                    target;

                return target;
            }

            int result =
                previous +
                (int)
                ((target - previous)
                * RentChangeSpeed);

            m_PreviousRent[building] =
                result;

            return result;
        }



        private int MathfRound(float value)
        {
            return (int)math.round(value);
        }
    }
}