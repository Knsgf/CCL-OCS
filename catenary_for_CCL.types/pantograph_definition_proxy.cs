using System.Collections.Generic;
using UnityEngine;

using CCL.Types.Proxies.Ports;

namespace catenary_for_CCL.types;

[AddComponentMenu("Catenary Addon/Pantograph Definition")]
public class pantograph_definition_proxy: power_collector_common_ports_definition_proxy, IHasFuseIdFields
{
    [Min(0.01f), Tooltip("Pantograph head movement speed in m/s")]
    public float HeadMovementSpeed = 1.0f;

    [Min(0.0f), Tooltip("Maximum reach height above a rail head. The minimum height is taken from initial position. Must match the reach from pantograph animation")]
    public float MaximumRaise;

    [FuseId(true)]
    public string PowerFuseId = string.Empty;

    public override IEnumerable<PortDefinition> ExposedPorts =>
    [
        .. base.ExposedPorts,
        new(DVPortType.READONLY_OUT, DVPortValueType.GENERIC, "PANTOGRAPH_RAISE"           ),
        new(DVPortType.READONLY_OUT, DVPortValueType.GENERIC, "PANTOGRAPH_RAISE_NORMALIZED")
    ];

    public override IEnumerable<PortReferenceDefinition> ExposedPortReferences =>
    [
        new(DVPortValueType.CONTROL, "TOGGLE")
    ];

    public IEnumerable<FuseIdField> ExposedFuseIdFields =>
    [
        new(this, nameof(PowerFuseId), PowerFuseId, required: true)
    ];
}
