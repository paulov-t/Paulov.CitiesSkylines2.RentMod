using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace PaulovRentMod.Systems
{
    public partial class RentIncreaseSystem : GameSystemBase
    {
        private static ILog log = Mod.log;

        private EntityQuery m_EconomyParameterQuery;

        private readonly float3 CustomLandValueModifierDefaults = new float3(0.5f, 0.7f, 0.5f);

        private readonly float3 CustomRentPriceBaseDefaults = new float3(1f, 3f, 0.8f);

        private readonly float3 VanillaLandValueModifierDefaults = new float3(0.35f, 0.7f, 0.5f);

        private readonly float3 VanillaRentPriceBaseDefaults = new float3(0.5f, 3f, 0.8f);

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadWrite<EconomyParameterData>());
            log?.Info("RentIncreaseSystem Created.");
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode != GameMode.Game || (purpose != Purpose.LoadGame && purpose != Purpose.NewGame))
            {
                log?.Debug(string.Format("{0}: Not in game mode or not loading/starting a game. Disabling.", nameof(RentIncreaseSystem)));
                base.Enabled = false;
                return;
            }
          
            if (m_EconomyParameterQuery.IsEmpty)
            {
                log?.Warn("EconomyParameterData not found. Cannot adjust rent parameters. Disabling system.");
                base.Enabled = false;
                return;
            }

            log?.Info("Applying RentIncreaseSystem parameter adjustments...");
            Entity singletonEntity = m_EconomyParameterQuery.GetSingletonEntity();
            try
            {
                EconomyParameterData economyParams = base.EntityManager.GetComponentData<EconomyParameterData>(singletonEntity);
                log?.Info($"Initial values read from EconomyParameterData: LandValue={economyParams.m_LandValueModifier}, BaseRent={economyParams.m_RentPriceBuildingZoneTypeBase}");
                //if (Mod.Settings.UseCustomDefaults)
                //{
                //    economyParams.m_LandValueModifier = CustomLandValueModifierDefaults;
                //    economyParams.m_RentPriceBuildingZoneTypeBase = CustomRentPriceBaseDefaults;
                //    log?.Info($"Checkbox enabled: Parameters Reset to Custom Defaults: LV={economyParams.m_LandValueModifier}, Base={economyParams.m_RentPriceBuildingZoneTypeBase}");
                //}
                //else
                //{
                    //economyParams.m_LandValueModifier = VanillaLandValueModifierDefaults;
                    //economyParams.m_RentPriceBuildingZoneTypeBase = VanillaRentPriceBaseDefaults;
                //    log?.Info($"Checkbox disabled: Parameters Reset to DEFINED Vanilla Defaults: LV={economyParams.m_LandValueModifier}, Base={economyParams.m_RentPriceBuildingZoneTypeBase}");
                //}
                //float landValueFactor = 2f;// Mod.Settings.LandValueRentFactor;
                //float baseRentFactor = 100f;// Mod.Settings.BaseRentFactor;
                //log?.Info($"Read Multipliers: LandValueFactor={landValueFactor}, BaseRentFactor={baseRentFactor}");
                economyParams.m_LandValueModifier = new float3(0.95f, 1.05f, 1f);

                // What I have learnt from decompilation is that the m_RentPriceBuildingZoneTypeBase is a float3 where:
                // x = Residential, y = Commercial, z = Industrial
                var residential = 95f; // Mod.Settings.ResidentialRentFactor;
                var commercial = 100f; // Mod.Settings.CommercialRentFactor;
                var industrial = 125f; // Mod.Settings.IndustrialRentFactor;
                economyParams.m_RentPriceBuildingZoneTypeBase = new float3(residential, commercial, industrial);

                // Set the rent percentage for mixed-use buildings to a low value to reduce their rent impact
                //  Mathf.RoundToInt((float)buildingPropertyData.m_ResidentialProperties / (1f - economyParameterData.m_MixedBuildingCompanyRentPercentage)));
                // I don't believe that these should be much less than others. I understand the point is that rent in large mixed-use buildings is calculated differently, but I don't think it should be much less than the other types. I will set it to 1% for now, but this may need to be adjusted in the future.
                economyParams.m_MixedBuildingCompanyRentPercentage = -0.01f;

                // This is based on UK unemployment benefits, which are currently £74.35 per week for those over 25. This is a reasonable value to use in the game, as it is not too high or too low, and it is based on real-world data. The value is multiplied by 7 to get the weekly amount, and then multiplied by the number of updates per day in the simulation to get the final value.
                // Set the unemployment benefit to a reasonable value based on the current wage system. This is calculated as 75.65 currency units per week, multiplied by 7 days, and then adjusted for the number of updates per day in the simulation.
                //economyParams.m_UnemploymentBenefit = (int)Math.Round(75.65f * Game.Simulation.PayWageSystem.kUpdatesPerDay);
                economyParams.m_UnemploymentBenefit = 0; // Set to 0 to disable unemployment benefits

                log?.Info($"Final LandValueModifier Applied: {economyParams.m_LandValueModifier}");
                log?.Info($"Final RentPriceBuildingZoneTypeBase Applied: {economyParams.m_RentPriceBuildingZoneTypeBase}");
                base.EntityManager.SetComponentData(singletonEntity, economyParams);
                log?.Info("Rent parameters adjusted successfully for this session.");
            }
            catch (Exception exception)
            {
                log?.Error(exception, "Failed to get/set EconomyParameterData in OnGameLoadingComplete!");
            }
            finally
            {
                base.Enabled = false;
                log?.Info(string.Format("{0}: disabled after execution.", nameof(RentIncreaseSystem)));
            }
        }

        protected override void OnUpdate()
        {
        }
    }

}