using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BaiChuanModdingCore.Features.SeamothModuleCompat
{
	internal static class SeamothModuleCompatFeature
    {
        internal static TechType MK2;
        internal static TechType MK3;
        internal static bool HasMK2;
        internal static bool HasMK3;
        internal static TechType MK4;
        internal static TechType MK5;
        internal static bool HasMK4;
        internal static bool HasMK5;
        internal static ManualLogSource? Log;
        private static MethodInfo? _tryGetModdedTechType;
        private static bool _techHandlerResolved;

        internal static void TryResolveTechTypes()
        {
            if (!HasMK2)
                HasMK2 = TryGetModdedTechType("VehiclePowerUpgradeModuleMK2", out MK2);
            if (!HasMK3)
                HasMK3 = TryGetModdedTechType("VehiclePowerUpgradeModuleMK3", out MK3);
            if (!HasMK4)
                HasMK4 = TryGetModdedTechType("SeamothHullModule4", out MK4);
            if (!HasMK5)
                HasMK5 = TryGetModdedTechType("SeamothHullModule5", out MK5);
        }

        private static bool TryGetModdedTechType(string id, out TechType tech)
        {
            tech = TechType.None;
            Type? handler = AccessTools.TypeByName("SMLHelper.V2.Handlers.TechTypeHandler");
            MethodInfo? method = handler?.GetMethod(
                "TryGetModdedTechType",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(TechType).MakeByRefType() },
                null);
            if (method == null)
            {
                return false;
            }

            object?[] args = { id, null };
            bool ok = method.Invoke(null, args) is bool result && result;
            if (!ok || args[1] is not TechType found)
            {
                return false;
            }

            tech = found;
            return true;
        }
		internal static void Initialize(Harmony harmony, ManualLogSource log)
        {
            Log = log;
            TryResolveTechTypes();
            UpgradedVehiclesCompat.Install(harmony);
            Log.LogInfo("[SeamothModuleCompat] ready  MK2=" + HasMK2 +
                " MK3=" + HasMK3 + " MK4=" + HasMK4 + " MK5=" + HasMK5);
        }
    }

    static class UpgradedVehiclesCompat
    {
        private const string AssemblyName = "UpgradedVehicles";

        private static Type UpgraderType;
        private static PropertyInfo ModulesProperty;
        private static PropertyInfo DepthProperty;
        private static PropertyInfo EfficientyBonusProperty;
        private static PropertyInfo SpeedMultiplierProperty;
        private static MethodInfo CalculateDepthMethod;
        private static MethodInfo CalculateArmorMethod;
        private static MethodInfo UpdateArmorMethod;
        private static MethodInfo UpdatePowerRatingMethod;
        private static MethodInfo SpeedBonusMethod;
        private static MethodInfo UpdateSpeedRatingMethod;
        private static FieldInfo SpeedModuleField;
        private static FieldInfo CommonUpgradeModulesField;
        private static FieldInfo ArmorPlatingModulesField;
        private static FieldInfo DepthUpgradeModulesField;
        private static FieldInfo EnginePowerRatingField;

        private static Harmony _harmony;
        private static bool _patched;
        private static bool _subscribed;

        public static void Install(Harmony harmony)
        {
            _harmony = harmony;
            Resolve();
            TryPatch();

            if (UpgraderType == null && !_subscribed)
            {
                _subscribed = true;
                AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            }
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.GetName().Name != AssemblyName) return;
            Resolve();
            TryPatch();
        }

        private static void Resolve()
        {
            if (UpgraderType != null) return;

            Type type = Type.GetType("UpgradedVehicles.VehicleUpgrader, UpgradedVehicles", false, false);
            if (type == null) return;

            UpgraderType = type;
            ModulesProperty = type.GetProperty(
                "UpgradeModules", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            DepthProperty = type.GetProperty(
                "DepthIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            EfficientyBonusProperty = type.GetProperty(
                "EfficientyBonus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            SpeedMultiplierProperty = type.GetProperty(
                "SpeedMultiplier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            CalculateDepthMethod = type.GetMethod(
                "CalculateDepthModuleIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            CalculateArmorMethod = type.GetMethod(
                "CalculateArmorPlatingAmount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            UpdateArmorMethod = type.GetMethod(
                "UpdateArmorRating", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            UpdatePowerRatingMethod = type.GetMethod(
                "UpdatePowerRating", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            SpeedBonusMethod = type.GetMethod(
                "GetSpeedMultiplierBonus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            UpdateSpeedRatingMethod = type.GetMethod(
                "UpdateSpeedRating", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            SpeedModuleField = type.GetField(
                "SpeedBoostingModule", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            CommonUpgradeModulesField = type.GetField(
                "CommonUpgradeModules", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            ArmorPlatingModulesField = type.GetField(
                "ArmorPlatingModules", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            DepthUpgradeModulesField = type.GetField(
                "DepthUpgradeModules", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            EnginePowerRatingField = AccessTools.Field(typeof(Vehicle), "enginePowerRating");
        }

        private static void TryPatch()
        {
            if (_patched || _harmony == null || UpgraderType == null) return;

            MethodInfo getEfficiency = UpgraderType.GetMethod(
                "GetEfficiencyBonus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getEfficiency == null) return;

            _harmony.Patch(getEfficiency, postfix: new HarmonyMethod(
                typeof(UpgradedVehiclesCompat).GetMethod(
                    "GetEfficiencyBonusPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
            _patched = true;
        }

        private static void GetEfficiencyBonusPostfix(object __instance, ref float __result)
        {
            Equipment modules = GetModules(__instance);
            if (modules == null) return;

            if (SeamothModuleCompatFeature.HasMK2) __result += 1.75f * modules.GetCount(SeamothModuleCompatFeature.MK2);
            if (SeamothModuleCompatFeature.HasMK3) __result += 2.5f * modules.GetCount(SeamothModuleCompatFeature.MK3);

            if (EfficientyBonusProperty != null && EfficientyBonusProperty.CanWrite)
                EfficientyBonusProperty.SetValue(__instance, __result, null);
        }

        public static bool IsLoaded()
        {
            return UpgraderType != null;
        }

        public static bool IsArmorModule(TechType techType)
        {
            IDictionary<TechType, int> armor =
                ArmorPlatingModulesField?.GetValue(null) as IDictionary<TechType, int>;
            return armor != null && armor.ContainsKey(techType);
        }

        public static bool IsSpeedModule(TechType techType)
        {
            if (SpeedModuleField == null) return false;
            object value = SpeedModuleField.GetValue(null);
            return value is TechType && (TechType)value == techType;
        }

        public static void RefreshArmorOnly(Vehicle vehicle)
        {
            Component upgrader = GetUpgrader(vehicle);
            if (upgrader == null) return;

            Equipment modules = GetModules(upgrader);
            if (modules == null) return;

            SetDepthIndex(upgrader, modules);
            if (CalculateArmorMethod != null && UpdateArmorMethod != null)
            {
                int armor = (int)CalculateArmorMethod.Invoke(upgrader, null);
                UpdateArmorMethod.Invoke(upgrader, new object[] { armor });
            }
        }

        public static void RefreshSpeedOnly(Vehicle vehicle)
        {
            Component upgrader = GetUpgrader(vehicle);
            if (upgrader == null) return;

            Equipment modules = GetModules(upgrader);
            if (modules == null) return;

            SetDepthIndex(upgrader, modules);
            int speed = GetSpeedCount(modules);
            if (SpeedBonusMethod != null && SpeedMultiplierProperty != null)
            {
                float multiplier = (float)SpeedBonusMethod.Invoke(upgrader, new object[] { (float)speed });
                SpeedMultiplierProperty.SetValue(upgrader, multiplier, null);
                if (UpdateSpeedRatingMethod != null)
                    UpdateSpeedRatingMethod.Invoke(upgrader, new object[] { (float)speed });
            }
        }

        private static Component GetUpgrader(Vehicle vehicle)
        {
            if (vehicle == null || UpgraderType == null) return null;
            return vehicle.GetComponent(UpgraderType);
        }

        private static int GetSpeedCount(Equipment modules)
        {
            return SpeedModuleField == null
                ? 0
                : modules.GetCount((TechType)SpeedModuleField.GetValue(null));
        }

        private static void SetDepthIndex(Component upgrader, Equipment modules)
        {
            int depth = -1;
            if (SeamothModuleCompatFeature.HasMK5 && modules.GetCount(SeamothModuleCompatFeature.MK5) > 0)
                depth = 5;
            else if (SeamothModuleCompatFeature.HasMK4 && modules.GetCount(SeamothModuleCompatFeature.MK4) > 0)
                depth = 4;
            else if (CalculateDepthMethod != null)
                depth = (int)CalculateDepthMethod.Invoke(upgrader, null);

            if (depth >= 0 && DepthProperty != null)
                DepthProperty.SetValue(upgrader, depth, null);
        }

        public static bool HasDepthModules(Vehicle vehicle)
        {
            if (vehicle == null || vehicle.modules == null) return false;

            Equipment modules = vehicle.modules;
            if (SeamothModuleCompatFeature.HasMK5 && modules.GetCount(SeamothModuleCompatFeature.MK5) > 0) return true;
            if (SeamothModuleCompatFeature.HasMK4 && modules.GetCount(SeamothModuleCompatFeature.MK4) > 0) return true;

            return modules.GetCount(TechType.VehicleHullModule3) > 0 ||
                   modules.GetCount(TechType.VehicleHullModule2) > 0 ||
                   modules.GetCount(TechType.VehicleHullModule1) > 0;
        }

        public static bool IsCommonDepthUpgrade(TechType techType)
        {
            ICollection<TechType> common = CommonUpgradeModulesField?.GetValue(null) as ICollection<TechType>;
            ICollection<TechType> depth = DepthUpgradeModulesField?.GetValue(null) as ICollection<TechType>;
            return common != null && depth != null && common.Contains(techType) && depth.Contains(techType);
        }

        private static float _lastPowerRefresh;

        public static void RefreshStats(Vehicle vehicle)
        {
            if (vehicle == null || UpgraderType == null) return;

            Component upgrader = vehicle.GetComponent(UpgraderType);
            if (upgrader == null) return;

            Equipment modules = GetModules(upgrader);
            if (modules == null) return;

            int depth = -1;
            if (SeamothModuleCompatFeature.HasMK5 && modules.GetCount(SeamothModuleCompatFeature.MK5) > 0)
                depth = 5;
            else if (SeamothModuleCompatFeature.HasMK4 && modules.GetCount(SeamothModuleCompatFeature.MK4) > 0)
                depth = 4;
            else if (CalculateDepthMethod != null)
                depth = (int)CalculateDepthMethod.Invoke(upgrader, null);

            if (depth >= 0 && DepthProperty != null)
                DepthProperty.SetValue(upgrader, depth, null);

            int speed = SpeedModuleField == null
                ? 0
                : modules.GetCount((TechType)SpeedModuleField.GetValue(null));

            if (CalculateArmorMethod != null && UpdateArmorMethod != null)
            {
                int armor = (int)CalculateArmorMethod.Invoke(upgrader, null);
                UpdateArmorMethod.Invoke(upgrader, new object[] { armor });
            }

            if (SpeedBonusMethod != null && SpeedMultiplierProperty != null)
            {
                float multiplier = (float)SpeedBonusMethod.Invoke(upgrader, new object[] { (float)speed });
                SpeedMultiplierProperty.SetValue(upgrader, multiplier, null);
                if (UpdateSpeedRatingMethod != null)
                    UpdateSpeedRatingMethod.Invoke(upgrader, new object[] { (float)speed });
            }

            if (UpdatePowerRatingMethod != null)
            {
                int vanilla = modules.GetCount(TechType.VehiclePowerUpgradeModule);
                UpdatePowerRatingMethod.Invoke(upgrader, new object[] { speed, vanilla });
            }

        }

        public static void RefreshPowerOnly(Vehicle vehicle)
        {
            if (vehicle == null || UpgraderType == null) return;
            if (Time.time - _lastPowerRefresh < 0.15f) return;
            _lastPowerRefresh = Time.time;

            Component upgrader = vehicle.GetComponent(UpgraderType);
            if (upgrader == null) return;

            Equipment modules = GetModules(upgrader);
            if (modules == null) return;

            int speed = SpeedModuleField == null
                ? 0
                : modules.GetCount((TechType)SpeedModuleField.GetValue(null));
            int vanilla = modules.GetCount(TechType.VehiclePowerUpgradeModule);

            if (UpdatePowerRatingMethod != null)
                UpdatePowerRatingMethod.Invoke(upgrader, new object[] { speed, vanilla });
        }

        public static float GetSpeedMultiplier(Vehicle vehicle)
        {
            if (vehicle == null || UpgraderType == null || SpeedMultiplierProperty == null)
                return 1f;

            Component upgrader = vehicle.GetComponent(UpgraderType);
            if (upgrader == null) return 1f;

            object value = SpeedMultiplierProperty.GetValue(upgrader, null);
            return value is float ? (float)value : 1f;
        }

        public static void SetFallbackRating(Vehicle vehicle)
        {
            if (vehicle == null || EnginePowerRatingField == null) return;

            Equipment modules = vehicle.modules;
            if (modules == null) return;

            float rating = 1f + modules.GetCount(TechType.VehiclePowerUpgradeModule);
            if (SeamothModuleCompatFeature.HasMK2) rating += 1.75f * modules.GetCount(SeamothModuleCompatFeature.MK2);
            if (SeamothModuleCompatFeature.HasMK3) rating += 2.5f * modules.GetCount(SeamothModuleCompatFeature.MK3);

            EnginePowerRatingField.SetValue(vehicle, rating);
            ErrorMessage.AddMessage(Language.main.GetFormat("PowerRatingNowFormat", rating));
        }

        private static Equipment GetModules(object upgrader)
        {
            return ModulesProperty?.GetValue(upgrader, null) as Equipment;
        }
    }

    [HarmonyPatch(typeof(SeaMoth), "OnUpgradeModuleChange")]
    static class ModuleChangePatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SeaMoth __instance, int slotID, TechType techType, bool added)
        {
            SeamothModuleCompatFeature.TryResolveTechTypes();
            bool isEfficiency = (SeamothModuleCompatFeature.HasMK2 && techType == SeamothModuleCompatFeature.MK2) ||
                                (SeamothModuleCompatFeature.HasMK3 && techType == SeamothModuleCompatFeature.MK3);
            bool isDepth = (SeamothModuleCompatFeature.HasMK4 && techType == SeamothModuleCompatFeature.MK4) ||
                           (SeamothModuleCompatFeature.HasMK5 && techType == SeamothModuleCompatFeature.MK5) ||
                           UpgradedVehiclesCompat.IsCommonDepthUpgrade(techType);

            if (UpgradedVehiclesCompat.IsLoaded())
            {
                if (isDepth)
                    UpgradedVehiclesCompat.RefreshStats(__instance);
                else if (isEfficiency)
                    UpgradedVehiclesCompat.RefreshPowerOnly(__instance);
                else if (UpgradedVehiclesCompat.IsArmorModule(techType))
                    UpgradedVehiclesCompat.RefreshArmorOnly(__instance);
                else if (UpgradedVehiclesCompat.IsSpeedModule(techType))
                    UpgradedVehiclesCompat.RefreshSpeedOnly(__instance);
                return;
            }

            if (isEfficiency || isDepth || techType == TechType.VehiclePowerUpgradeModule)
                UpgradedVehiclesCompat.SetFallbackRating(__instance);
        }
    }

    [HarmonyPatch(typeof(SeaMoth), "Update")]
    static class SpeedController
    {
        private sealed class State
        {
            public float Forward;
            public float Backward;
            public float Sideward;
            public float Vertical;
            public float UpgradedMultiplier;
            public int Stage;
            public int Gear;
            public bool Green;
            public bool SesActive;
            public bool Valid;
        }

        private const float BaseForward = 11.5f;
        private const float BaseBackward = 5f;
        private const float BaseSideward = 11.5f;
        private const float BaseVertical = 11f;

        private const float SesBaseForward = 13f;
        private const float SesBaseBackward = 5f;
        private const float SesBaseSideward = 11.5f;
        private const float SesBaseVertical = 11f;
        private const float SesLargeFactor = 3.5f;
        private const float SesSmallFactor = 1.5f;

        private static readonly ConditionalWeakTable<Vehicle, State> States =
            new ConditionalWeakTable<Vehicle, State>();

        private static Type _sesInfoType;
        private static Type _sesConfigType;
        private static FieldInfo _sesGearField;
        private static FieldInfo _sesGreenField;
        private static FieldInfo _sesElectronicField;
        private static FieldInfo _sesMechanicalField;
        private static Type _eldritchType;
        private static FieldInfo _eldritchStageField;
        private static bool _typesResolved;
        private static int _lastResolveFrame;

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SeaMoth __instance)
        {
            if (!__instance.GetPilotingMode()) return;

            ResolveTypes();

            float upgradedMultiplier = UpgradedVehiclesCompat.GetSpeedMultiplier(__instance);
            int stage = GetEldritchStage(__instance);

            bool sesActive;
            int gear;
            bool green;
            GetSesState(out sesActive, out gear, out green);

            float forward = BaseForward;
            float backward = BaseBackward;
            float sideward = BaseSideward;
            float vertical = BaseVertical;

            if (sesActive)
            {
                forward = SesBaseForward;
                backward = SesBaseBackward;
                sideward = SesBaseSideward;
                vertical = SesBaseVertical;

                float turbo = green ? 0f : 2f;
                switch (gear)
                {
                    case 1:
                        forward -= SesLargeFactor * 3f;
                        backward -= SesSmallFactor * 3f;
                        sideward -= SesLargeFactor * 3f;
                        vertical -= SesLargeFactor * 2.5f;
                        break;
                    case 2:
                        forward += -SesLargeFactor * 2f + turbo;
                        backward += -SesSmallFactor * 2f + turbo;
                        sideward += -SesLargeFactor * 2f + turbo;
                        vertical += -SesLargeFactor * 2f + turbo;
                        break;
                    case 3:
                        forward += -SesLargeFactor + turbo;
                        backward += -SesSmallFactor + turbo;
                        sideward += -SesLargeFactor + turbo;
                        vertical += -SesLargeFactor + turbo;
                        break;
                    case 4:
                        forward += turbo;
                        backward += turbo;
                        sideward += turbo;
                        vertical += turbo;
                        break;
                    default:
                        forward += SesLargeFactor + turbo * 2f;
                        backward += SesSmallFactor + turbo * 2f;
                        sideward += SesLargeFactor + turbo * 2f;
                        vertical += SesLargeFactor + turbo * 2f;
                        break;
                }
            }

            float stageMultiplier = Mathf.Pow(1.5f, stage);
            forward *= upgradedMultiplier * stageMultiplier;
            backward *= stageMultiplier;
            sideward *= stageMultiplier;
            vertical *= stageMultiplier;

            State state = States.GetOrCreateValue(__instance);
            if (state.Valid &&
                state.Forward == forward &&
                state.Backward == backward &&
                state.Sideward == sideward &&
                state.Vertical == vertical &&
                state.UpgradedMultiplier == upgradedMultiplier &&
                state.Stage == stage &&
                state.Gear == gear &&
                state.Green == green &&
                state.SesActive == sesActive &&
                Mathf.Approximately(__instance.forwardForce, state.Forward) &&
                Mathf.Approximately(__instance.backwardForce, state.Backward) &&
                Mathf.Approximately(__instance.sidewardForce, state.Sideward) &&
                Mathf.Approximately(__instance.verticalForce, state.Vertical))
            {
                return;
            }

            __instance.forwardForce = forward;
            __instance.backwardForce = backward;
            __instance.sidewardForce = sideward;
            __instance.verticalForce = vertical;


            state.Forward = forward;
            state.Backward = backward;
            state.Sideward = sideward;
            state.Vertical = vertical;
            state.UpgradedMultiplier = upgradedMultiplier;
            state.Stage = stage;
            state.Gear = gear;
            state.Green = green;
            state.SesActive = sesActive;
            state.Valid = true;
        }

        private static void GetSesState(out bool active, out int gear, out bool green)
        {
            active = false;
            gear = 1;
            green = false;

            if (_sesGearField == null || _sesGreenField == null ||
                _sesElectronicField == null || _sesMechanicalField == null)
                return;

            try
            {
                active = Convert.ToBoolean(_sesElectronicField.GetValue(null)) ||
                         Convert.ToBoolean(_sesMechanicalField.GetValue(null));
                if (!active) return;

                gear = Mathf.Clamp(Mathf.RoundToInt(Convert.ToSingle(_sesGearField.GetValue(null))), 1, 5);
                green = Convert.ToBoolean(_sesGreenField.GetValue(null));
            }
            catch
            {
                active = false;
            }
        }

        private static int GetEldritchStage(Vehicle vehicle)
        {
            if (_eldritchType == null || _eldritchStageField == null) return 0;

            Component component = vehicle.GetComponent(_eldritchType);
            if (component == null) return 0;

            try
            {
                return Convert.ToInt32(_eldritchStageField.GetValue(component));
            }
            catch
            {
                return 0;
            }
        }

        private static bool IsAssemblyLoaded(string name)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (assemblies[i].GetName().Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        private static void ResolveTypes()
        {
            if (_typesResolved) return;

            int frame = Time.frameCount;
            if (frame - _lastResolveFrame < 30) return;
            _lastResolveFrame = frame;

            _sesConfigType = AccessTools.TypeByName("SeamothEnhancementSystem.Configuration.Config");
            _sesInfoType = AccessTools.TypeByName("SeamothEnhancementSystem.InfoObjects.SeamothInfo");
            if (_sesConfigType != null)
                _sesGearField = _sesConfigType.GetField(
                    "SeamothGearValue", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (_sesInfoType != null)
            {
                _sesGreenField = _sesInfoType.GetField(
                    "ModeGreenOn", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                _sesElectronicField = _sesInfoType.GetField(
                    "electronicModuleIn", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                _sesMechanicalField = _sesInfoType.GetField(
                    "mechanicalModuleIn", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }

            _eldritchType = AccessTools.TypeByName("EldritchMoth.Mono.EldritchMothMono");
            if (_eldritchType != null)
                _eldritchStageField = _eldritchType.GetField(
                    "_currentSpeedSetting", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            bool sesReady = _sesGearField != null &&
                            _sesGreenField != null &&
                            _sesElectronicField != null &&
                            _sesMechanicalField != null;
            bool eldritchReady = _eldritchStageField != null || !IsAssemblyLoaded("EldritchMoth");

            _typesResolved = sesReady && eldritchReady;
        }
    }
}





















