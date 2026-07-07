//using Colossal.Logging;
//using Colossal.Serialization.Entities;
//using Game;
//using Game.Prefabs;
//using Game.Prefabs.Modes;
//using Unity.Entities;
//using Unity.Mathematics;

//namespace PaulovRentMod.Systems
//{
//    public partial class TaxIncomeReductionSystem : GameSystemBase
//    {
//        private static ILog log = Mod.log;

//        private EntityQuery m_GameModeSettingQuery;

//        protected override void OnCreate()
//        {
//            base.OnCreate();
//            m_GameModeSettingQuery = GetEntityQuery(ComponentType.ReadWrite<ModeSettingData>());
//            log?.Info("TaxReductionSystem Created.");
//        }

//        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
//        {
//            base.OnGameLoadingComplete(purpose, mode);
//            if (mode != GameMode.Game || (purpose != Purpose.LoadGame && purpose != Purpose.NewGame))
//            {
//                log?.Debug($"{nameof(TaxIncomeReductionSystem)}: Not in game mode or not loading/starting a game. Disabling.");
//                base.Enabled = false;
//                return;
//            }

//            if (m_GameModeSettingQuery.IsEmpty)
//            {
//                log?.Warn("ModeSettingData not found. Cannot adjust tax parameters. Disabling system.");
//                base.Enabled = false;
//                return;
//            }

//            TaxIncomeUpdate();
           
//        }

//        protected override void OnUpdate()
//        {
//            TaxIncomeUpdate();
//        }

//        void TaxIncomeUpdate()
//        {
//            log?.Info($"Applying {nameof(TaxIncomeReductionSystem)} parameter adjustments...");
//            try
//            {
//                Entity entity = m_GameModeSettingQuery.GetSingletonEntity();
//                ModeSettingData data = base.EntityManager.GetComponentData<ModeSettingData>(entity);
//                data.m_TaxPaidMultiplier = new float3(0f, 0f, 0f);
//                data.m_SupportPoorCitizens = false;
//                data.m_EnableGovernmentSubsidies = false;
//                data.m_MinimumWealth = 0;
//                base.EntityManager.SetComponentData(entity, data);
//                //log?.Info($"Tax paid multiplier set to {data.m_TaxPaidMultiplier} (0.1% - ~12.5% of normal tax).");
//            }
//            catch (System.Exception exception)
//            {
//                log?.Error(exception, $"Failed to set tax parameters in {nameof(TaxIncomeReductionSystem)}!");
//            }
//            finally
//            {
//                //base.Enabled = false;
//                //log?.Info($"{nameof(TaxIncomeReductionSystem)}: disabled after execution.");
//            }
//        }
//    }
//}