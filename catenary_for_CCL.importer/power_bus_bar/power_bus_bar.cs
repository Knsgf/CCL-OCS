using System;
using System.Reflection;

using UnityEngine;

using LocoSim.Implementations;

namespace catenary_for_CCL.importer;

internal static class port_accessor
{
    private static readonly FieldInfo _port_reference_internal_port_info;

    static port_accessor()
    {
        _port_reference_internal_port_info = typeof(PortReference).GetField("port", BindingFlags.Instance | BindingFlags.NonPublic);
        assert.test(_port_reference_internal_port_info != null);
    }

    public static Port? GetPort(this PortReference port_reference)
    {
        return (Port?) _port_reference_internal_port_info.GetValue(port_reference);
    }
}

internal class power_bus_bar: SimComponent
{
    
    private readonly float _nominal_voltage;

    private readonly Port _supply_voltage;
    private readonly Port _supply_voltage_normalised;
    private readonly Port _pantograph_input_current;
    private readonly Port _raised_count;

    private readonly PortReference[] _pantograph_in_contact = [];
    private readonly PortReference[] _pantograph_voltages   = [];
    private readonly PortReference   _current_draw;

    private readonly bool[] _pantograph_raised = [];

    private int _raised_pantographs_count = 0;

    private Action<float> create_contact_handler(int pantograph_index)
    {
        return delegate (float contact_state)
        {
            bool now_in_contact = contact_state >= 0.5f;
            if (_pantograph_raised[pantograph_index] != now_in_contact)
            {
                if (now_in_contact)
                {
                    _raised_count.Value = ++_raised_pantographs_count;
                }
                else
                { 
                    _raised_count.Value = --_raised_pantographs_count; 
                }
            }
            _pantograph_raised[pantograph_index] = now_in_contact;
        };
    }

    public power_bus_bar(power_bus_bar_definition definition) : base(definition.ID)
    {
        _nominal_voltage = definition.nominal_voltage;
            
        _supply_voltage            = AddPort(definition.supply_voltage           );
        _supply_voltage_normalised = AddPort(definition.supply_voltage_normalised);
        _pantograph_input_current  = AddPort(definition.pantograph_input_current );
        _raised_count              = AddPort(definition.raised_pantographs_count );
        _current_draw              = AddPortReference(definition.current_draw);

        if (_nominal_voltage <= 0.0f)
        { 
            Main.log("Bus bar nominal voltage negative or zero, overhead power will not be collected"); 
            return;
        }
        int pantograph_count = definition.pantograph_count;
        if (   definition.pantograph_in_contact.Length == 0 
            || definition.pantograph_in_contact.Length != pantograph_count
            || definition.pantograph_in_contact.Length != definition.pantograph_voltage.Length)
        { 
            Main.log("Bus bar has no or invalid number of inputs, overhead power will not be collected"); 
            return;
        }
            
        _pantograph_raised     = new          bool[pantograph_count];
        _pantograph_in_contact = new PortReference[pantograph_count];
        _pantograph_voltages   = new PortReference[pantograph_count];
        for (int pantograph_index = pantograph_count - 1; pantograph_index >= 0; --pantograph_index)
        {
            _pantograph_in_contact[pantograph_index] = AddPortReference(definition.pantograph_in_contact[pantograph_index]);
            _pantograph_voltages  [pantograph_index] = AddPortReference(definition.pantograph_voltage   [pantograph_index]);
        }
    }

    public override void InitializationAfterConnecting()
    {
        for (int pantograph_index = 0; pantograph_index < _pantograph_in_contact.Length; ++pantograph_index)
        {
            Port? in_contact_port = _pantograph_in_contact[pantograph_index].GetPort();
            if (in_contact_port == null || !_pantograph_voltages[pantograph_index].IsConnected)
                Main.log($"Empty pantograph connection {pantograph_index} to bus bar"); 
            else
            {
                Action<float> contact_handler = create_contact_handler(pantograph_index);
                in_contact_port.ValueUpdatedInternally += contact_handler;
                contact_handler(in_contact_port.Value);
            }
        }
    }

    public override void Tick(float delta)
    {
        if (_raised_pantographs_count == 0)
            _supply_voltage.Value = _supply_voltage_normalised.Value = _pantograph_input_current.Value = 0.0f;
        else
        {
            float voltage = 0.0f;
            for (int pantograph_index = 0; pantograph_index < _pantograph_voltages.Length; ++pantograph_index)
            {
                if (_pantograph_raised![pantograph_index])
                    voltage = Mathf.Max(voltage, _pantograph_voltages[pantograph_index].Value); 
            }
            _supply_voltage.Value            = voltage;
            _supply_voltage_normalised.Value = voltage             / _nominal_voltage;
            _pantograph_input_current.Value  = _current_draw.Value / _raised_pantographs_count;
        }
    }
}
