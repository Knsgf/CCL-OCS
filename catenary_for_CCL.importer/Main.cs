using System;
using System.Reflection;

using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using LocoSim.Definitions;
using LocoSim.Implementations;

using catenary_for_CCL.types;

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

[editor_proxy(typeof(test_component_definition_proxy))]
internal class test_component_definition: SimComponentDefinition, electric_component_defition
{
    public int val;
    public readonly PortDefinition          counter = new(PortType.READONLY_OUT, PortValueType.GENERIC, "COUNTER");
    public readonly PortReferenceDefinition handle  = new(PortValueType.CONTROL, "HANDLE");

    public void map_from(MonoBehaviour proxy)
    {
        var real_proxy = (test_component_definition_proxy) proxy;
        ID  = real_proxy.ID;
        val = real_proxy.val;
    }
    
    public override SimComponent InstantiateImplementation() => new test_component(this);
}

internal class test_component: SimComponent
{
    private float _last_handle = float.NaN, _time_left = 1.0f;
    private readonly Port _counter;
    private readonly PortReference _handle;
    
    public test_component(test_component_definition def): base(def.ID)
    { 
        _counter = AddPort(def.counter);
        _counter.Value = def.val;
        _handle = AddPortReference(def.handle);
    }
    
    public override void Tick(float delta)
    {
        if (_last_handle != _handle.Value)
        {
            _last_handle = _handle.Value;
            Main.log($"test_component.val={_counter.Value} handle={_last_handle}");
        }
        if (_last_handle is > -0.5f and < 0.5f)
            _time_left = 1.0f;
        else
        {
            _time_left -= delta;
            if (_time_left <= 0.0f)
            {
                _time_left = 1.0f;
                _counter.Value += (_last_handle > 0.0f) ? 1.0f : -1.0f;
            }
        }
    }
}
