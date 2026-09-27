using System.Collections.Generic;

using UnityEngine;

using CCL.Types;
using CCL.Types.Json;
using CCL.Types.Proxies.Ports;

namespace catenary_for_CCL.types;

[AddComponentMenu("Catenary Addon/Power Bus Bar Definition")]
public class power_bus_bar_definition_proxy: SimComponentDefinitionProxy, ICustomSerialized
{
    [Min(1.0f), Tooltip("Used to calculate normalized voltage port")]
    public float NominalVoltage = 1500.0f;
        
    [Min(1), Delayed]
    public int PantographCount = 1;
        
    private PortReferenceDefinition[] _all_inputs = [];
        
    [SerializeField, HideInInspector]
    private string? _raw_inputs;

    public PortReferenceDefinition[] all_inputs => _all_inputs;

    public override IEnumerable<PortDefinition> ExposedPorts =>
    [
        new(DVPortType.READONLY_OUT, DVPortValueType.VOLTS  , "SUPPLY_VOLTAGE"           ),
        new(DVPortType.READONLY_OUT, DVPortValueType.VOLTS  , "SUPPLY_VOLTAGE_NORMALIZED"),
        new(DVPortType.READONLY_OUT, DVPortValueType.AMPS   , "PANTOGRAPH_INPUT_CURRENT" ),
        new(DVPortType.READONLY_OUT, DVPortValueType.GENERIC, "PANTOGRAPHS_RAISED_COUNT" )
    ];

    public override IEnumerable<PortReferenceDefinition> ExposedPortReferences => _all_inputs;

    public override void OnValidate()
    {
        base.OnValidate();

        if (_all_inputs.Length != PantographCount)
        {
            _all_inputs = new PortReferenceDefinition[PantographCount * 2 + 1];
            for (int pantograph = 0; pantograph < PantographCount; pantograph++)
            {
                _all_inputs[pantograph * 2    ] = new(DVPortValueType.STATE, $"PANTOGRAPH_IN_CONTACT_{pantograph}");
                _all_inputs[pantograph * 2 + 1] = new(DVPortValueType.VOLTS,    $"PANTOGRAPH_VOLTAGE_{pantograph}");
            }
            _all_inputs[PantographCount * 2] = new(DVPortValueType.AMPS, "CURRENT_DRAW");
        }
        _raw_inputs = JSONObject.ToJson(_all_inputs);
    }
        
    public void AfterImport()
    {
        _all_inputs = string.IsNullOrWhiteSpace(_raw_inputs) ? [] : JSONObject.FromJson<PortReferenceDefinition[]>(_raw_inputs);
    }
}
