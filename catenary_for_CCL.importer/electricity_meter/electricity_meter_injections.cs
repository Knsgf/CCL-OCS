using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using DV.Damage;
using DV.ServicePenalty;
using DV.Simulation.Cars;
using DV.ThingTypes;
using LocoSim.Implementations;

namespace catenary_for_CCL.importer;

#region Ownership tracking

[HarmonyPatch(typeof(OwnedCarsStateController), "RegisterCarStateTracker")]
internal static class private_vehicle_registar
{
    private static void Postfix(TrainCar? car, LocoDebtTrackerBase? carDebtTracker)
    {
        if (car != null)
            electricity_meter.replace_tracker_on_ownership_change(car, carDebtTracker as SimulatedCarDebtTracker); 
    }
}

[HarmonyPatch(typeof(LocoDebtController), "RegisterLocoDebtTracker")]
internal static class company_vehicle_registar
{
    private static void Postfix(TrainCar? car, LocoDebtTrackerBase? locoDebtTracker)
    {
        if (car != null)
            electricity_meter.replace_tracker_on_ownership_change(car, locoDebtTracker as SimulatedCarDebtTracker); 
    }
}

#endregion

[HarmonyPatch(typeof(SimController), "OnLogicCarInitialized")]
internal static class logic_car_initialiser
{
    private static void Postfix(TrainCar? ___train, SimulatedCarDebtTracker ___debt)
    {
        if (___train != null)
            electricity_meter.assign_new_tracker(___train, ___debt);
    }
}
        
[HarmonyPatch(typeof(SimulatedCarDebtTracker))]
internal static class electricity_meter_injections
{
    [HarmonyPatch("InitializeDebtComponents"), HarmonyPrefix]
    private static void InitializeDebtComponentsPrefix(DamageController? ___dmgController, 
        Dictionary<ResourceType, List<ResourceContainer>>? ___resourceToResourceContainers, out bool __state)
    {
        __state = false;
        if (___dmgController == null || ___resourceToResourceContainers == null)
            return;
        var unit = TrainCar.Resolve(___dmgController.gameObject);
        if (unit == null || unit.gameObject.GetComponentInChildren<electricity_meter_definition>() == null)
            return; 
            
        __state = true;
        bool has_electric_charge_container = false;
        foreach (KeyValuePair<ResourceType, List<ResourceContainer>> tracked_resource in ___resourceToResourceContainers)
        {
            if (tracked_resource.Key == ResourceType.ElectricCharge)
            {
                has_electric_charge_container = true;
                break;
            }
        }
        if (!has_electric_charge_container)
            ___resourceToResourceContainers[ResourceType.ElectricCharge] = []; 
    }

    [HarmonyPatch("InitializeDebtComponents"), HarmonyPostfix]
    private static void InitializeDebtComponentsPostfix(SimulatedCarDebtTracker? __instance,
        Dictionary<ResourceType, List<ResourceContainer>>? ___resourceToResourceContainers, bool __state)
    {
        if (__state && __instance != null && ___resourceToResourceContainers != null)
        {
            electricity_meter.initial_electric_charge[__instance] = 0.0f;
            foreach (ResourceContainer electric_charge_container in ___resourceToResourceContainers[ResourceType.ElectricCharge])
                electricity_meter.initial_electric_charge[__instance] += electric_charge_container.amountReadOut.Value; 
        }
    }

    [HarmonyPatch("UpdateDebtValues"), HarmonyPrefix]
    private static void UpdateDebtValuesPrefix(SimulatedCarDebtTracker? __instance, out float? __state)
    {
        __state = null;
        if (__instance == null || !electricity_meter.initial_electric_charge.TryGetValue(__instance, out float initial_charge))
            return; 

        // Reset the start and snapshot values of the debt component to vanilla settings for consistency.
        // The snapshot value might become negative temporarily, but doing it is safe as UpdateDebtValues()
        // doesn't deal with snapshots.
        foreach (DebtComponent current_fee in __instance.GetTrackedDebts())
        {
            if (current_fee.Type == ResourceType.ElectricCharge && electricity_meter.fee_trackers.ContainsKey(__instance))
            { 
                if (current_fee.HasSnapshot)
                    __state = current_fee.StartValue - current_fee.SnapshotValue; 
                current_fee.UpdateStartValue(initial_charge);
                if (__state != null)
                    current_fee.SetSnapshot(current_fee.StartValue - (float) __state); 
                break;
            }
        }
    }

    [HarmonyPatch("UpdateDebtValues"), HarmonyPostfix]
    private static void UpdateDebtValuesPostfix(SimulatedCarDebtTracker? __instance, float? __state)
    {
        if (__instance == null || !electricity_meter.initial_electric_charge.TryGetValue(__instance, out float initialCharge))
            return; 
        foreach (DebtComponent current_fee in __instance.GetTrackedDebts())
        {
            if (current_fee.Type == ResourceType.ElectricCharge && electricity_meter.fee_trackers.TryGetValue(__instance, out electricity_meter meter))
            { 
                float new_end_value = current_fee.EndValue - Mathf.Max((float) meter.energyConsumed, 0.0f);
                float minimum       = (__state != null) ? Mathf.Min(new_end_value, current_fee.SnapshotValue) : new_end_value;
                if (minimum >= 0.0f)
                    current_fee.UpdateEndValue(new_end_value); 
                else
                {
                    current_fee.UpdateEndValue(0.0f);
                    current_fee.UpdateStartValue(initialCharge - minimum);
                    if (__state != null)
                        current_fee.SetSnapshot(current_fee.StartValue - (float) __state); 
                }
                break;
            }
        }
    }

    [HarmonyPatch("ResetState"), HarmonyPostfix]
    private static void ResetStatePostfix(SimulatedCarDebtTracker? __instance)
    {
        if (__instance != null && electricity_meter.fee_trackers.TryGetValue(__instance, out electricity_meter meter))
            meter.Reset();
    }
}
