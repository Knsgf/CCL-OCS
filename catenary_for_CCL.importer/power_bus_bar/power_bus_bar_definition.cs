using UnityEngine;

using LocoSim.Definitions;
using LocoSim.Implementations;

using catenary_for_CCL.types;

namespace catenary_for_CCL.importer;

[editor_proxy(typeof(power_bus_bar_definition_proxy))]
public class power_bus_bar_definition: SimComponentDefinition, electric_component_defition
{
    public float nominal_voltage  = 1500.0f;
    public int   pantograph_count = 1;

    public readonly PortDefinition supply_voltage            = new(PortType.READONLY_OUT, PortValueType.VOLTS  , "SUPPLY_VOLTAGE"           );
    public readonly PortDefinition supply_voltage_normalised = new(PortType.READONLY_OUT, PortValueType.VOLTS  , "SUPPLY_VOLTAGE_NORMALIZED");
    public readonly PortDefinition pantograph_input_current  = new(PortType.READONLY_OUT, PortValueType.AMPS   , "PANTOGRAPH_INPUT_CURRENT" );
    public readonly PortDefinition raised_pantographs_count  = new(PortType.READONLY_OUT, PortValueType.GENERIC, "PANTOGRAPHS_RAISED_COUNT" );
        
    public readonly PortReferenceDefinition current_draw = new(PortValueType.AMPS, "CURRENT_DRAW");
    public PortReferenceDefinition[] pantograph_in_contact = [];
    public PortReferenceDefinition[] pantograph_voltage    = [];

    public void map_from(MonoBehaviour proxy)
    {
        var real_proxy = (power_bus_bar_definition_proxy) proxy;
        ID               = real_proxy.ID;
        nominal_voltage  = real_proxy.NominalVoltage;
        pantograph_count = real_proxy.PantographCount;

        pantograph_in_contact = new PortReferenceDefinition[pantograph_count];
        pantograph_voltage    = new PortReferenceDefinition[pantograph_count];
        CCL.Types.Proxies.Ports.PortReferenceDefinition[] all_inputs = real_proxy.all_inputs;
        assert.test(all_inputs.Length == pantograph_count * 2 + 1);
        for (int pantograph_index = 0; pantograph_index < pantograph_count; ++pantograph_index)
        {
            int input_index = pantograph_index << 1;
            pantograph_in_contact[pantograph_index] = new(PortValueType.STATE, all_inputs[input_index    ].ID);
            pantograph_voltage   [pantograph_index] = new(PortValueType.VOLTS, all_inputs[input_index + 1].ID);
        }
    }

    public override SimComponent InstantiateImplementation() => new power_bus_bar(this);
}
