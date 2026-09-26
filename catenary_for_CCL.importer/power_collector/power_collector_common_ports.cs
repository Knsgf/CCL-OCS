using LocoSim.Implementations;

namespace catenary_for_CCL.importer;

internal abstract class power_collector_common_ports: SimComponent
{
    protected readonly Port _wire_height,  _initial_head_height, _head_height;
    protected readonly Port _wire_voltage, _voltage_readout;
    protected readonly Port _in_contact;

    protected power_collector_common_ports(power_collector_common_ports_definition definition): base(definition.ID)
    {
        _wire_height         = AddPort(definition.wire_height          );
        _initial_head_height = AddPort(definition.initial_head_height  );
        _head_height         = AddPort(definition.head_height          );
        _wire_voltage        = AddPort(definition.wire_voltage         );
        _voltage_readout     = AddPort(definition.supply_voltage       );
        _in_contact          = AddPort(definition.pantograph_in_contact);
    }
}
