using UnityEngine;

using LocoSim.Definitions;
using LocoSim.Implementations;

using catenary_for_CCL.types;

namespace catenary_for_CCL.importer;

[editor_proxy(typeof(pantograph_definition_proxy))]
internal class pantograph_definition: power_collector_common_ports_definition, electric_component_defition
{
    public float maximum_raise;
    public float head_movement_speed;

    public string power_fuse_ID = string.Empty;

    public readonly PortDefinition pantograph_raise            = new(PortType.READONLY_OUT, PortValueType.GENERIC, "PANTOGRAPH_RAISE"           );
    public readonly PortDefinition pantograph_raise_normalized = new(PortType.READONLY_OUT, PortValueType.GENERIC, "PANTOGRAPH_RAISE_NORMALIZED");

    public readonly PortReferenceDefinition toggle = new(PortValueType.CONTROL, "TOGGLE");

    public void map_from(MonoBehaviour proxy)
    {
        var real_proxy = (pantograph_definition_proxy) proxy;
        ID                  = real_proxy.ID;
        maximum_raise       = real_proxy.MaximumRaise;
        head_movement_speed = real_proxy.HeadMovementSpeed;
        power_fuse_ID       = real_proxy.PowerFuseId;
    }

    public override SimComponent InstantiateImplementation() => new pantograph(this);
}
