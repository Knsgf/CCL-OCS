using UnityEngine;

using LocoSim.Definitions;
using LocoSim.Implementations;

using catenary_for_CCL.types;

namespace catenary_for_CCL.importer;

[editor_proxy(typeof(electricity_meter_definition_proxy))]
public class electricity_meter_definition: SimComponentDefinition, electric_component_defition
{
    public float electric_charge_consumption_factor;

    public readonly PortDefinition electric_charge_consumed = new(PortType.READONLY_OUT, PortValueType.ELECTRIC_CHARGE, "ENERGY_CONSUMED");

    public readonly PortReferenceDefinition supply_voltage = new(PortValueType.VOLTS, "SUPPLY_VOLTAGE");
    public readonly PortReferenceDefinition current_draw   = new(PortValueType.AMPS  ,"CURRENT_DRAW"  );

    public void map_from(MonoBehaviour proxy)
    {
        var real_proxy = (electricity_meter_definition_proxy) proxy;
        ID                                 = real_proxy.ID;
        electric_charge_consumption_factor = real_proxy.ElectricChargeConsumptionFactor;
    }

    public override SimComponent InstantiateImplementation() => new electricity_meter(this);
}
