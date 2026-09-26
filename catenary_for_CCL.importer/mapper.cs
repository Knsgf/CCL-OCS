using System;
using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;
using UnityEngine;

using DV;
using DV.ThingTypes;
using LocoSim.Definitions;

using proxies = CCL.Types.Proxies.Ports;

namespace catenary_for_CCL.importer;

internal interface electric_component_defition
{
    void map_from(proxies.SimComponentDefinitionProxy proxy);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal class editor_proxy(Type proxy_type): Attribute
{
    public Type proxy_type { get; private set; } = proxy_type;
}

[HarmonyPatch(typeof(CCL.Importer.CarManager), "LoadCarDefinitions")]
internal static class mapper
{
    private static readonly HashSet<TrainCarLivery> _scanned_liveries = [];
    private static readonly Dictionary<Type, Type>  _type_mapping     = [];

    static mapper()
    {
        foreach (Type current_type in Assembly.GetExecutingAssembly().GetTypes())
        {
            Main.log($"ITS {current_type}");
            if (Attribute.GetCustomAttribute(current_type, typeof(editor_proxy)) is editor_proxy proxy_info)
            { 
                Main.log($"ITSP {proxy_info.proxy_type}"); 
                _type_mapping[proxy_info.proxy_type] = current_type;
            }
        }
    }

    private static void map_component(GameObject prefab, Type component_proxy_type, SimComponentDefinition[] execution_order,
        Dictionary<proxies.SimComponentDefinitionProxy, int> execution_indices)
    {
        Component[]? mapped_component_proxies = prefab.GetComponentsInChildren(component_proxy_type, includeInactive: false);
        Main.log($"LVTST '{prefab.name}' {mapped_component_proxies?.Length.ToString() ?? "<null>"}");
        if (mapped_component_proxies == null || mapped_component_proxies.Length == 0)
            return;

        Type component_type = _type_mapping[component_proxy_type];
        foreach (Component current_proxy in mapped_component_proxies)
        {
            var real_proxy = (proxies.SimComponentDefinitionProxy) current_proxy;
            if (!execution_indices.TryGetValue(real_proxy, out int executiuon_index))
                continue;
            GameObject entity = current_proxy.gameObject;
            var replacement = (electric_component_defition) entity.AddComponent(component_type);
            assert.test(replacement is SimComponentDefinition);
            replacement.map_from(real_proxy);
            Main.log($"LVTSM '{real_proxy}' {executiuon_index}");
            assert.test(execution_order[executiuon_index] == null);
            execution_order[executiuon_index] = (SimComponentDefinition) replacement;
            //GameObject.Destroy(current_proxy);
        }
    }
    
    internal static void map_new_vehicles()
    {
        List<TrainCarLivery> all_vehicle_types = Globals.G.Types.Liveries;
        Main.log($"LVTST {all_vehicle_types.Count} {_scanned_liveries.Count}");
        foreach (TrainCarLivery current_type in all_vehicle_types)
        { 
            if (_scanned_liveries.Contains(current_type))
                continue;
            var component_connections       = current_type.prefab.GetComponentInChildren<        SimConnectionDefinition      >();
            var component_connections_proxy = current_type.prefab.GetComponentInChildren<proxies.SimConnectionsDefinitionProxy>();
            List<proxies.SimComponentDefinitionProxy>? execution_order_proxy = component_connections_proxy?.executionOrder;
            if (component_connections?.executionOrder == null || execution_order_proxy == null)
                continue;
            
            Main.log($"LVTSTP '{current_type.id}' {execution_order_proxy.Count}");
            Dictionary<proxies.SimComponentDefinitionProxy, int> execution_indices = [];
            Dictionary<Type, Type> type_mapping = _type_mapping;
            for (int proxy_index = execution_order_proxy.Count - 1; proxy_index >= 0; --proxy_index)
            {
                proxies.SimComponentDefinitionProxy proxy = execution_order_proxy[proxy_index];
                Main.log($"LVTSTP {proxy.GetType()}");
                if (type_mapping.ContainsKey(proxy.GetType()))
                    execution_indices[proxy] = proxy_index;
            }
            Main.log($"LVTSTP {execution_indices.Count}");
            if (execution_indices.Count > 0)
            {
                foreach (Type current_component_type in _type_mapping.Keys)
                    map_component(current_type.prefab, current_component_type, component_connections.executionOrder, execution_indices);
                foreach (SimComponentDefinition current_component in component_connections.executionOrder)
                    Main.log($"LVTSTC2 {current_component?.ToString() ?? "<null>"}");
            }
        }
        _scanned_liveries.Clear();
        _scanned_liveries.UnionWith(all_vehicle_types);
    }

    private static void Postfix()
    {
        map_new_vehicles();
    }
}
