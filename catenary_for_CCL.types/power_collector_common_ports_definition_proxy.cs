using System.Collections.Generic;

using CCL.Types.Proxies.Ports;

namespace catenary_for_CCL.types;

public abstract class power_collector_common_ports_definition_proxy: SimComponentDefinitionProxy
{
    public override IEnumerable<PortDefinition> ExposedPorts =>
    [
        new(DVPortType.EXTERNAL_IN , DVPortValueType.GENERIC, "WIRE_HEIGHT"          ),
        new(DVPortType.EXTERNAL_IN , DVPortValueType.GENERIC, "INITIAL_HEAD_HEIGHT"  ),
        new(DVPortType.EXTERNAL_IN , DVPortValueType.GENERIC, "HEAD_HEIGHT"          ),
        new(DVPortType.EXTERNAL_IN , DVPortValueType.VOLTS  , "WIRE_VOLTAGE"         ),
        new(DVPortType.EXTERNAL_IN , DVPortValueType.STATE  , "PANTOGRAPH_IN_CONTACT"),
        new(DVPortType.READONLY_OUT, DVPortValueType.VOLTS  , "VOLTAGE"              )
    ];
}
