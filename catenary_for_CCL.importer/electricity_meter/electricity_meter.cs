using System.Collections.Generic;

using Newtonsoft.Json.Linq;
using UnityEngine;

using DV.JObjectExtstensions;
using DV.ServicePenalty;
using DV.ThingTypes;
using DV.Utils;
using LocoSim.Implementations;

namespace catenary_for_CCL.importer;

public class electricity_meter: SimComponent
{
    private class private_vehicle_meter: LocoDebtTrackerBase
    {
        private const float start_value = 524287.0f;
        
        public private_vehicle_meter(TrainCar vehicle)
        {
            debtData = new(vehicle.ID, vehicle.carType, InitializeDebtComponents());
        }
        
        public override DebtComponent[] InitializeDebtComponents()
        {
            return [ new(start_value, ResourceType.ElectricCharge) ];
        }

        public override bool IsDebtOnlyEnvironmental() => false;

        public override void ResetState()
        {
            if (_fee_trackers.TryGetValue(this, out electricity_meter meter))
                meter.Reset();
        }

        public override void TurnOffDebtSources()
        {
            if (_fee_trackers.TryGetValue(this, out electricity_meter meter))
                meter._unit?.SimController?.controlsOverrider?.SetNeutralState(); 
        }

        public override void UpdateDebtValues()
        {
            if (_fee_trackers.TryGetValue(this, out electricity_meter meter))
            {
                foreach (DebtComponent current_fee in GetTrackedDebts())
                {
                    if (current_fee.Type == ResourceType.ElectricCharge)
                    { 
                        current_fee.UpdateEndValue(Mathf.Clamp(start_value - (float) meter._energy_consumed, 0.0f, start_value));
                        break;
                    }
                }
            }
        }
    }

    private static readonly Dictionary<TrainCar,   electricity_meter> _cars_with_meters = [];
    private static readonly Dictionary<TrainCar, LocoDebtTrackerBase> _new_trackers     = [];
    private static readonly Dictionary<    LocoDebtTrackerBase, electricity_meter> _fee_trackers            = [];
    private static readonly Dictionary<SimulatedCarDebtTracker,             float> _initial_electric_charge = [];
        
    private readonly TrainCar?     _unit;
    private readonly Port          _electric_charge_consumed;
    private readonly PortReference _supply_voltage, _current_draw;

    private LocoDebtTrackerBase? _fee_tracker;

    private float  _energy_consumption_factor;
    private double _energy_consumed = 0.0;

    public static Dictionary<SimulatedCarDebtTracker,             float> initial_electric_charge => _initial_electric_charge;
    public static Dictionary<    LocoDebtTrackerBase, electricity_meter> fee_trackers            => _fee_trackers;
    public          double energyConsumed => _energy_consumed;
    public override bool   HasSaveData    => true;
        
    public electricity_meter(electricity_meter_definition definition) : base(definition.ID)
    {
        _energy_consumption_factor = definition.electric_charge_consumption_factor / (1000.0f * 3600.0f);

        _electric_charge_consumed = AddPort(definition.electric_charge_consumed);
        _supply_voltage = AddPortReference(definition.supply_voltage);
        _current_draw = AddPortReference(definition.current_draw);

        _unit = TrainCar.Resolve(definition.gameObject);
        if (_unit == null)
        {
            Main.release_log("Train car not found, electricity meter disabled");
            _fee_tracker = null;
            return;
        }
        if (_cars_with_meters.ContainsKey(_unit))
        {
            Main.release_log("Another electricity meter present on the car, duplicate meters disabled");
            _fee_tracker = null;
            return;
        }
        _cars_with_meters[_unit] = this;
        try_set_up_fee_tracker();
        if (gameParams == null)
            _unit.LogicCarInitialized += adjust_energy_consumption_factor;
        else
            adjust_energy_consumption_factor();
        _unit.OnDestroyCar += dispose_fee_tracker;
    }

    private void adjust_energy_consumption_factor()
    { 
        if (_unit != null)
        {
            _unit.LogicCarInitialized  -= adjust_energy_consumption_factor;
            _energy_consumption_factor *= gameParams.ResourceConsumptionModifier;
        }
    }

    internal static void assign_new_tracker(TrainCar vehicle, SimulatedCarDebtTracker? standard_tracker)
    {
        if (vehicle.playerSpawnedCar)
            return;

        bool tracker_assigned;
        if (vehicle.uniqueCar)
        {
            _new_trackers[vehicle] = new private_vehicle_meter(vehicle);
            SingletonBehaviour<LocoDebtController>.Instance.RegisterLocoDebtTracker(vehicle, _new_trackers[vehicle]);
            tracker_assigned = true;
        }
        else if (standard_tracker != null)
        { 
            _new_trackers[vehicle] = standard_tracker; 
            tracker_assigned = true;
        }
        else
            tracker_assigned = false;
        if (tracker_assigned && _cars_with_meters.TryGetValue(vehicle, out electricity_meter meter))
            meter.try_set_up_fee_tracker();
    }

    internal static void replace_tracker_on_ownership_change(TrainCar vehicle, SimulatedCarDebtTracker? standard_tracker)
    { 
        TrainCar?          unit  = null;
        electricity_meter? meter = null;
        foreach (KeyValuePair<LocoDebtTrackerBase, electricity_meter> current_tracker in _fee_trackers)
        {
            meter = current_tracker.Value;
            unit  = meter._unit;
            if (unit == vehicle && (current_tracker.Key is private_vehicle_meter) != unit.uniqueCar)
            {
                meter.dispose_fee_tracker(unit);
                if (!unit.uniqueCar)
                    meter.Reset();      // Any leftover electricity bill on a privately owned vehicle is staged and documented by the dispose call above
                if (unit.uniqueCar || standard_tracker != null)
                {
                    Main.log($"Re-registering fee tracker for {(unit.uniqueCar ? "private" : "DVRT")} vehicle {unit.ID}");
                    _cars_with_meters[unit] = meter;
                    unit.OnDestroyCar      += meter.dispose_fee_tracker;
                    assign_new_tracker(unit, standard_tracker);
                }
                break;
            }
        }
    }

    private void try_set_up_fee_tracker()
    {
        if (_unit != null && _cars_with_meters.ContainsKey(_unit) && _new_trackers.TryGetValue(_unit, out _fee_tracker))
        {
            _fee_trackers[_fee_tracker] = this;
            _new_trackers.Remove(_unit);
            _fee_tracker.UpdateDebtValues();
            Main.log($"Set up a fee tracker <{_fee_tracker.GetType()}> for car {_unit.ID}");
        }
    }

    private void dispose_fee_tracker(TrainCar? unit)
    {
        if (unit == null)
            return;

        unit.OnDestroyCar -= dispose_fee_tracker;
        if (_fee_tracker != null)
        {
            _fee_trackers.Remove(_fee_tracker);
            if (_fee_tracker is SimulatedCarDebtTracker standardTracker)
                _initial_electric_charge.Remove(standardTracker);
            else if (_fee_tracker is private_vehicle_meter)
            { 
                SingletonBehaviour<LocoDebtController>.Instance.StageLocoDebtOnLocoDestroy(_fee_tracker);
                Main.log($"Staged remaining electricity fees on car {unit.ID}");
            }
            _fee_tracker = null;
        }
        _cars_with_meters.Remove(unit);
        _new_trackers.Remove    (unit); 
        Main.log($"Removed fee tracker for car {unit.ID}");
    }

    public override void Tick(float delta)
    {
        if (_fee_tracker != null)
        { 
            float load    = _current_draw.Value;
            float voltage = _supply_voltage.Value;
            if (load != 0.0f && !float.IsNaN(load) && !float.IsInfinity(load) && !float.IsNaN(voltage) && !float.IsInfinity(voltage))
            {
                _energy_consumed               += load * voltage * _energy_consumption_factor * delta;
                _electric_charge_consumed.Value = (float) _energy_consumed;
            }
        }
    }

    internal void Reset()
    {
        _energy_consumed                = 0.0;
        _electric_charge_consumed.Value = 0.0f;
    }

    public override JObject? GetSaveStateData()
    {
        if (_fee_tracker == null)
            return null; 
        JObject savedData = [];
        savedData.SetDouble("energyConsumed", _energy_consumed);
        return savedData;
    }

    public override void SetSaveStateData(JObject? savedData)
    {
        if (savedData == null)
            Reset();
        else
        {
            _energy_consumed = savedData.GetDouble("energyConsumed") ?? 0.0;
            if (double.IsNaN(_energy_consumed) || double.IsInfinity(_energy_consumed))
                Reset();
            else
                _electric_charge_consumed.Value = (float) _energy_consumed;
        }
        _fee_tracker?.UpdateDebtValues();
    }
}
