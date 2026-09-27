using System.Collections.Generic;

using UnityEngine;

using CCL.Types.Proxies.Ports;

namespace catenary_for_CCL.types;

[AddComponentMenu("Catenary Addon/Electricity Meter")]
public class electricity_meter_definition_proxy: SimComponentDefinitionProxy
{
    [Min(0.0f), Tooltip("Electric charge consumption multiplier for computing electricity fees. The default setting 0.6666667 is equivalent to $10/kWh at standard difficulty")]
    public float ElectricChargeConsumptionFactor = 10.0f / 15.0f;

    public override IEnumerable<PortDefinition> ExposedPorts =>
    [
        new(DVPortType.READONLY_OUT, DVPortValueType.ELECTRIC_CHARGE, "ENERGY_CONSUMED")
    ];

    public override IEnumerable<PortReferenceDefinition> ExposedPortReferences =>
    [
        new(DVPortValueType.VOLTS, "SUPPLY_VOLTAGE"),
        new(DVPortValueType.AMPS , "CURRENT_DRAW"  )
    ];
}
