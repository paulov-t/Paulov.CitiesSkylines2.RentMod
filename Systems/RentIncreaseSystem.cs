using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PaulovRentMod.Systems
{
    public partial class RentIncreaseSystem : GameSystemBase
    {
        private static ILog log = Mod.log;

        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_BuildingPropertyQuery;
        private EntityQuery m_BuildingQuery;

        private int m_UpdateCounter;
        private const int kUpdateInterval = 512;
        private bool m_BaseParamsApplied;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadWrite<EconomyParameterData>());

            m_BuildingPropertyQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadWrite<BuildingPropertyData>() },
                Options = EntityQueryOptions.IncludePrefab
            });

            m_BuildingQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Transform>(),
                    ComponentType.ReadOnly<Building>(),
                },
                None = new[] { ComponentType.ReadOnly<Deleted>() },
            });

            log?.Info("RentIncreaseSystem created.");
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode != GameMode.Game || (purpose != Purpose.LoadGame && purpose != Purpose.NewGame))
            {
                log?.Debug($"{nameof(RentIncreaseSystem)}: Not in game mode. Disabling.");
                Enabled = false;
                return;
            }

            OnUpdate();
        }

        protected override void OnUpdate()
        {
            if (m_EconomyParameterQuery.IsEmpty)
                return;

            if (!m_BaseParamsApplied)
            {
                ApplyBaseParameters();
                m_BaseParamsApplied = true;
            }

            if (++m_UpdateCounter < kUpdateInterval)
                return;
            m_UpdateCounter = 0;

            UpdateContextAwareLandValue();
        }

        private void ApplyBaseParameters()
        {
            log?.Info("Applying base economy parameters...");
            var entity = m_EconomyParameterQuery.GetSingletonEntity();
            var economyParams = EntityManager.GetComponentData<EconomyParameterData>(entity);

            economyParams.m_RentPriceBuildingZoneTypeBase = new float3(105f, 85f, 95f);
            economyParams.m_MixedBuildingCompanyRentPercentage = 0;
            economyParams.m_UnemploymentBenefit = 76 * 4;
            economyParams.m_Pension = 241 * 4;

            EntityManager.SetComponentData(entity, economyParams);

            ApplySpaceMultipliers();

            log?.Info("Base economy parameters applied.");
        }

        private void ApplySpaceMultipliers()
        {
            if (m_BuildingPropertyQuery.IsEmpty)
                return;

            using var buildingEntities = m_BuildingPropertyQuery.ToEntityArray(Allocator.Temp);
            foreach (var buildingEntity in buildingEntities)
            {
                var bpd = EntityManager.GetComponentData<BuildingPropertyData>(buildingEntity);
                bpd.m_SpaceMultiplier *= 25f;
                if (bpd.m_ResidentialProperties > 1)
                    bpd.m_SpaceMultiplier *= 1.25f;
                if (bpd.m_ResidentialProperties > 250)
                    bpd.m_SpaceMultiplier *= 1.15f;
                if (bpd.m_ResidentialProperties > 500)
                    bpd.m_SpaceMultiplier *= 1.07f;
                EntityManager.SetComponentData(buildingEntity, bpd);
            }
        }

        private void UpdateContextAwareLandValue()
        {
            if (m_BuildingQuery.IsEmpty)
                return;

            using var entities = m_BuildingQuery.ToEntityArray(Allocator.Temp);
            using var transforms = m_BuildingQuery.ToComponentDataArray<Transform>(Allocator.Temp);

            float resiAccum = 0f, commAccum = 0f, indusAccum = 0f;
            int resiCount = 0, commCount = 0, indusCount = 0;

            for (int i = 0; i < entities.Length; i++)
            {
                var zoneType = GetZoneType(entities[i]);
                float3 pos = transforms[i].m_Position;
                int cellIndex = LandValueSystem.GetCellIndex(pos);
                float3 cellCenter = LandValueSystem.GetCellCenter(cellIndex);
                float offset = math.distance(pos, cellCenter);

                switch (zoneType)
                {
                    case ZoneType.Residential:
                        resiAccum += offset;
                        resiCount++;
                        break;
                    case ZoneType.Commercial:
                        commAccum += offset;
                        commCount++;
                        break;
                    case ZoneType.Industrial:
                        indusAccum += offset;
                        indusCount++;
                        break;
                }
            }

            float resiAdj = GetOffsetAdjustment(resiCount, resiAccum);
            float commAdj = GetOffsetAdjustment(commCount, commAccum);
            float indusAdj = GetOffsetAdjustment(indusCount, indusAccum);

            var entity = m_EconomyParameterQuery.GetSingletonEntity();
            var economyParams = EntityManager.GetComponentData<EconomyParameterData>(entity);

            float baseResi = 1.15f, baseComm = 1.1f, baseIndus = 3.5f;
            economyParams.m_LandValueModifier = new float3(
                math.clamp(baseResi + resiAdj, 0.85f, 1.45f),
                math.clamp(baseComm + commAdj, 0.85f, 1.4f),
                math.clamp(baseIndus + indusAdj, 2.5f, 4.5f)
            );

            EntityManager.SetComponentData(entity, economyParams);
        }

        private static float GetOffsetAdjustment(int count, float totalOffset)
        {
            if (count < 5)
                return 0f;
            float avgOffset = totalOffset / count;
            return math.clamp((15f - avgOffset) * 0.003f, -0.05f, 0.05f);
        }

        private enum ZoneType { Unknown, Residential, Commercial, Industrial }

        private ZoneType GetZoneType(Entity buildingEntity)
        {
            if (EntityManager.HasComponent<ResidentialProperty>(buildingEntity))
                return ZoneType.Residential;
            if (EntityManager.HasComponent<CommercialProperty>(buildingEntity))
                return ZoneType.Commercial;
            if (EntityManager.HasComponent<IndustrialProperty>(buildingEntity))
                return ZoneType.Industrial;
            return ZoneType.Unknown;
        }
    }
}