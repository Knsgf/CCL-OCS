using System.Collections.Generic;

using UnityEngine;

using LocoSim.Implementations;

namespace catenary_for_CCL.importer;

internal class pantograph: power_collector_common_ports
{
    private static readonly Dictionary<TrainCar, List<pantograph>> _all_pantographs = [];
        
    private readonly FuseReference _power_fuse;
    private readonly Port          _raise_readout, _raise_normalised_readout;
    private readonly PortReference _pantograph_toggle;
    //private readonly TrainCar?     _unit;

    private readonly float _maximum_raise, _head_movement_speed;
        
    private bool  _disabled      = false;
    private float _minimum_raise = 0.0f, _maximum_raise_difference;

    public pantograph(pantograph_definition definition): base(definition)
    {
        _head_movement_speed = definition.head_movement_speed;
        _maximum_raise       = definition.maximum_raise;

        _power_fuse               = AddFuseReference(definition.power_fuse_ID);
        _raise_readout            = AddPort(definition.pantograph_raise           );
        _raise_normalised_readout = AddPort(definition.pantograph_raise_normalized);
        _pantograph_toggle        = AddPortReference(definition.toggle);
        _initial_head_height.ValueUpdatedInternally += check_initial_height;

        check_initial_height(_initial_head_height.Value);
        if (_disabled)
            return; 
        if (_head_movement_speed <= 0.0f)
        {
            Main.log("Head movement speed negative or zero, pantograph disabled");
            _disabled = true;
            return;
        }
        var unit = TrainCar.Resolve(definition.gameObject);
        //_unit    = unit;
        if (unit == null)
        {
            Main.log("Car not resolved, pantograph disabled");
            _disabled = true;
            return;
        }
            
        if (_all_pantographs.TryGetValue(unit, out List<pantograph> installedPantographs))
            installedPantographs.Add(this);
        else
            unit.OnDestroyCar += vehicle_destroyed;
    }

    private void check_initial_height(float initial_height)
    {
        if (!_disabled)
        { 
            if (_maximum_raise <= initial_height)
            {
                Main.log("Maximum reach is below initial position, pantograph disabled");
                _disabled = true;
                return;
            }
            _minimum_raise            = initial_height;
            _maximum_raise_difference = _maximum_raise - initial_height;
        }
    }
        
    private void vehicle_destroyed(TrainCar? unit)
    {
        if (unit != null && _all_pantographs.TryGetValue(unit, out List<pantograph> installed_pantographs))
        { 
            unit.OnDestroyCar -= vehicle_destroyed;
            foreach (pantograph currentPantograph in installed_pantographs)
            {
                currentPantograph._disabled = true;
                currentPantograph._initial_head_height.ValueUpdatedInternally -= currentPantograph.check_initial_height;
            }
            installed_pantographs.Clear();
            _all_pantographs.Remove(unit);
        }
    }

    private void move(float delta, float raise_height, bool pantograph_on)
    {
        const float proximity_slowdown = 0.2f;

        float current_raise = _raise_readout.Value + _minimum_raise;
        float target_raise;
        float raise_difference;
        if (pantograph_on)
        {
            target_raise     = raise_height;
            raise_difference = target_raise - _head_height.Value;
        }
        else
        {
            target_raise     = _minimum_raise;
            raise_difference = target_raise - current_raise;
        }
        if (raise_difference > 0.006f)
        {
            float movement_speed            = Mathf.Min(_head_movement_speed, raise_difference / proximity_slowdown );
            current_raise                   = Mathf.Min(_maximum_raise      , current_raise + movement_speed * delta);
            _raise_readout.Value            = current_raise - _minimum_raise;
            _raise_normalised_readout.Value = Mathf.Clamp((current_raise - _minimum_raise) / _maximum_raise_difference, 0.0f, 0.999f);
        }
        else if (raise_difference < -0.006f)
        {
            float movement_speed            = Mathf.Min(_head_movement_speed, raise_difference / (-proximity_slowdown));
            current_raise                   = Mathf.Max(_minimum_raise      , current_raise - movement_speed * delta  );
            _raise_readout.Value            = current_raise - _minimum_raise;
            _raise_normalised_readout.Value = Mathf.Clamp((current_raise - _minimum_raise) / _maximum_raise_difference, 0.0f, 0.999f);
        }
    }

    public override void Tick(float delta)
    {
        if (_disabled)
            return; 
        bool  pantograph_on = _pantograph_toggle.Value >= 0.5f && _power_fuse.State;
        float wire_height   = _wire_height.Value;
        float raise_height;
        if (!pantograph_on)
            raise_height = _minimum_raise;
        else
            raise_height = (wire_height > 0.0f) ? wire_height : _maximum_raise; 
        move(delta, raise_height, pantograph_on);
        _voltage_readout.Value = (_in_contact.Value >= 0.5f) ? _wire_voltage.Value : 0.0f;
    }
}
