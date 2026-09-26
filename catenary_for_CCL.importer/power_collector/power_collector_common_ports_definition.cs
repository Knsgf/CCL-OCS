using LocoSim.Definitions;

namespace catenary_for_CCL.importer;

internal abstract class power_collector_common_ports_definition: SimComponentDefinition
{
    public readonly PortDefinition wire_height           = new(PortType.EXTERNAL_IN , PortValueType.GENERIC, "WIRE_HEIGHT"          );
    public readonly PortDefinition initial_head_height   = new(PortType.EXTERNAL_IN , PortValueType.GENERIC, "INITIAL_HEAD_HEIGHT"  );
    public readonly PortDefinition head_height           = new(PortType.EXTERNAL_IN , PortValueType.GENERIC, "HEAD_HEIGHT"          );
    public readonly PortDefinition wire_voltage          = new(PortType.EXTERNAL_IN , PortValueType.VOLTS  , "WIRE_VOLTAGE"         );
    public readonly PortDefinition pantograph_in_contact = new(PortType.EXTERNAL_IN , PortValueType.STATE  , "PANTOGRAPH_IN_CONTACT");
    public readonly PortDefinition supply_voltage        = new(PortType.READONLY_OUT, PortValueType.VOLTS  , "VOLTAGE"              );
}
