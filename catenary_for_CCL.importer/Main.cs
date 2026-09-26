using System;
using System.Reflection;

using HarmonyLib;
using UnityModManagerNet;

using DV.Simulation.Cars;
using DV.Simulation.Controllers;

namespace catenary_for_CCL.importer;

[HarmonyPatch(typeof(SimController), "Initialize")]
public static class Main
{
    private static UnityModManager.ModEntry? _mod;
    
    public static void log(string message)
    {
        _mod?.Logger.Log(message);
    }
    
    private static bool Load(UnityModManager.ModEntry mod)
    {
        _mod = mod;
        Harmony? injector = null;

        try
        {
            injector = new(mod.Info.Id);
            injector.PatchAll(Assembly.GetExecutingAssembly());

            mapper.map_new_vehicles();
        }
        catch (Exception ex)
        {
            mod.Logger.LogException($"Failed to load {mod.Info.DisplayName}:", ex);
            injector?.UnpatchAll(mod.Info.Id);
            return false;
        }

        return true;
    }

    private static void Prefix(SimController __instance)
    {
        var all_controllers = __instance.GetComponentsInChildren<ASimInitializedController>();
        if (all_controllers != null && (__instance.otherSimControllers == null 
            || __instance.otherSimControllers.Length != all_controllers.Length))
        {
            __instance.otherSimControllers = all_controllers;
        }
    }
}
