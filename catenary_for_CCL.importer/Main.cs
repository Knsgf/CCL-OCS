using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using HarmonyLib;
using UnityModManagerNet;

using LocoSim.Definitions;
using LocoSim.Implementations;

using proxies = CCL.Types.Proxies.Ports;

using catenary_for_CCL.types;

namespace catenary_for_CCL.importer;

public static class Main
{
    private static UnityModManager.ModEntry? _mod;
    
    public static void log(string message)
    {
        _mod?.Logger.Log(message);
    }
    
    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        _mod = modEntry;
        Harmony? patcher = null;

        try
        {
            patcher = new(modEntry.Info.Id);
            patcher.PatchAll(Assembly.GetExecutingAssembly());

            mapper.map_new_vehicles();
        }
        catch (Exception ex)
        {
            modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
            patcher?.UnpatchAll(modEntry.Info.Id);
            return false;
        }

        return true;
    }
}

[editor_proxy(typeof(test_component_definition_proxy))]
internal class test_component_definition: SimComponentDefinition, electric_component_defition
{
    public int val;
    public readonly PortDefinition          counter = new(PortType.READONLY_OUT, PortValueType.GENERIC, "COUNTER");
    public readonly PortReferenceDefinition handle  = new(PortValueType.CONTROL, "HANDLE");

    public void map_from(proxies.SimComponentDefinitionProxy proxy)
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

internal static class assert
{
    [Conditional("DEBUG")]
    public static void test([DoesNotReturnIf(false)] bool passed)
    {
        if (!passed)
            throw new assertion_failed_exception();
    }
}

internal class assertion_failed_exception: Exception
{
    public assertion_failed_exception(): base()
    {}

    public assertion_failed_exception(string message): base(message)
    {}
}
