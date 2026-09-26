using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using DV.Simulation.Controllers;
using LocoSim.Attributes;
using LocoSim.Definitions;
using LocoSim.Implementations;

using catenary_for_CCL.types;

namespace catenary_for_CCL.importer;

[editor_proxy(typeof(pantograph_sim_controller_proxy))]
internal class pantograph_sim_controller: ASimInitializedController, electric_component_defition
{
    #region OCS interface

    private const string OCS_class_name                          = "electric_sim.catenary.overhead_equipment, electric_sim";
    private const string OCS_property_name                       = "system";
    private const string OCS_wire_height_and_voltage_method_name = "relative_wire_height_and_voltage";
    private const string OCS_activation_event_name               = "catenary_activated";
    private const string OCS_deactivation_event_name             = "catenary_deactivated";

    private static Type?         _OCS_type                         = null;
    private static MethodInfo?   _get_wire_height_and_voltage_info = null;
    private static PropertyInfo? _OCS_object_info                  = null;
    private static object?       _OCS_instance                     = null;

    #endregion

    private static readonly Dictionary<TrainCar, HashSet<pantograph_sim_controller>> _all_catenary_controllers = new();

    [SerializeField]
    private Transform? _pantograph_base;
    [SerializeField]
    private Transform? _contact_strip_first_end, _contact_strip_second_end;
    [SerializeField]
    private float      _contact_tolerance = 0.2f;

    [PortId(PortType.EXTERNAL_IN, PortValueType.GENERIC, local: true), SerializeField]
    private string _initial_height_port_ID = string.Empty;
    [PortId(PortType.EXTERNAL_IN, PortValueType.GENERIC, local: true), SerializeField]
    private string _head_height_port_ID    = string.Empty;
    [PortId(PortType.EXTERNAL_IN, PortValueType.GENERIC, local: true), SerializeField]
    private string _wire_height_port_ID    = string.Empty;
    [PortId(PortType.EXTERNAL_IN, PortValueType.VOLTS, local: true), SerializeField]
    private string _wire_voltage_port_ID   = string.Empty;
    [PortId(PortType.EXTERNAL_IN, PortValueType.STATE, local: true), SerializeField]
    private string _is_in_contact_port_ID  = string.Empty;
    [PortId(PortValueType.AMPS), SerializeField]
    private string _input_current_port_ID  = string.Empty;

    private Func<Transform, Transform, Transform, Transform, float, (float?, float)>? get_wire_height_and_voltage = null;
    private TrainCar? _unit;
    private Port?     _initial_head_height, _head_height, _wire_height;
    private Port?            _wire_voltage,  _in_contact, _input_current;
    private Vector3   _last_tip_position = new(0.0f, float.MinValue, 0.0f);
    private float     _last_head_midpoint_height;

    public override bool ExternalTick => true;

    private static void try_get_OCS_type()
    {
        if (_OCS_type != null)
            return;
        _OCS_type = Type.GetType(OCS_class_name, false);
        if (_OCS_type == null)
        {
            Main.log("Catenary not installed; overhead power will be unavailable");
            return;
        }
        _get_wire_height_and_voltage_info = _OCS_type.GetMethod(OCS_wire_height_and_voltage_method_name, 
            [typeof(Transform), typeof(Transform), typeof(Transform), typeof(Transform), typeof(float)]);
        _OCS_object_info = _OCS_type.GetProperty(OCS_property_name, BindingFlags.Public | BindingFlags.Static);
        EventInfo? OCSActivationInfo   = _OCS_type.GetEvent(  OCS_activation_event_name, BindingFlags.Public | BindingFlags.Static);
        EventInfo? OCSDeactivationInfo = _OCS_type.GetEvent(OCS_deactivation_event_name, BindingFlags.Public | BindingFlags.Static);
        if (_get_wire_height_and_voltage_info == null || _OCS_object_info == null || OCSActivationInfo == null || OCSDeactivationInfo == null)
        {
            Main.log("Unable to retreive OCS class information; overhead power will be unavailable");
            _OCS_type = null;
            return;
        }
        OCSActivationInfo.AddEventHandler  (null, set_up_connection_for_all_controllers);
        OCSDeactivationInfo.AddEventHandler(null,  sever_connection_for_all_controllers);
        set_up_connection_for_all_controllers();
    }

    private static void set_up_connection_for_all_controllers()
    {
        if (_OCS_type != null && _OCS_object_info != null)
        {
            try
            {
                _OCS_instance = _OCS_object_info.GetValue(null);
            }
            catch (InvalidOperationException _)
            {
                Main.log("Catenary inactive, overhead power not available");
                _OCS_instance = null;
                return;
            }
            Main.log("Catenary activated, restoring overhead power access");
            foreach (HashSet<pantograph_sim_controller> carCatenaryControllers in _all_catenary_controllers.Values)
            {
                foreach (pantograph_sim_controller controller in carCatenaryControllers)
                { 
                    controller.set_up_catenary_connection(); 
                }
            }
        }
    }
        
    private static void sever_connection_for_all_controllers()
    {
        Main.log("Catenary deactivated, turning off overhead power");
        _OCS_instance = null;
        foreach (HashSet<pantograph_sim_controller> carCatenaryControllers in _all_catenary_controllers.Values)
        {
            foreach (pantograph_sim_controller controller in carCatenaryControllers)
            { 
                controller.disable_power(); 
            }
        }
    }

    private void disable_power()
    {
        get_wire_height_and_voltage = null;
        _wire_height!.Value  = -1.0f;
        _wire_voltage!.Value =  0.0f;
    }
        
    private void set_up_catenary_connection()
    {
        if (_OCS_instance == null || _get_wire_height_and_voltage_info == null)
        { 
            disable_power();
            return; 
        }
        get_wire_height_and_voltage = _get_wire_height_and_voltage_info.CreateDelegate(typeof(Func<Transform, Transform, Transform, Transform, float, (float?, float)>), _OCS_instance)
            as Func<Transform, Transform, Transform, Transform, float, (float?, float)>;
        if (get_wire_height_and_voltage != null)
            Main.log($"Connection to OCS successfully established for car {_unit!.name}");
        else
        { 
            Debug.LogError($"Unable to connect car {_unit!.name} to OCS, pantograph will not receive power", this); 
            disable_power();
        }
    }

    public override void Init(TrainCar car, SimulationFlow simFlow)
    {
        if (_pantograph_base == null || _contact_strip_first_end == null || _contact_strip_second_end == null)
        {
            Debug.LogError($"Pantograph control transforms not set, pantograph sim controller disabled", this);
            Destroy(this);
            return;
        }
        if (   !simFlow.TryGetPort(_initial_height_port_ID, out _initial_head_height)
            || !simFlow.TryGetPort(   _head_height_port_ID, out _head_height        )
            || !simFlow.TryGetPort(   _wire_height_port_ID, out _wire_height        )
            || !simFlow.TryGetPort(  _wire_voltage_port_ID, out _wire_voltage       )
            || !simFlow.TryGetPort( _is_in_contact_port_ID, out _in_contact         )
            || !simFlow.TryGetPort( _input_current_port_ID, out _input_current      ))
        { 
            Debug.LogError($"Referenced ports not set, pantograph sim controller disabled", this);
            Destroy(this);
            return;
        }
        var unit = TrainCar.Resolve(gameObject);
        if (unit == null)
        { 
            Debug.LogError($"Car unresolved, pantograph sim controller disabled", this);
            Destroy(this);
            return;
        }
        _unit = unit;

        try_get_OCS_type();
        set_up_catenary_connection();
        if (_all_catenary_controllers.TryGetValue(unit, out HashSet<pantograph_sim_controller> pantographContollers))
            pantographContollers.Add(this); 
        else
            _all_catenary_controllers[unit] = [this]; 
        (_initial_head_height.Value, _) = get_head_midpoint_height();
    }

    private (float height, bool position_changed) get_head_midpoint_height()
    {
        Vector3 current_tip_position = _contact_strip_first_end!.position;
        Vector3 position_difference  = current_tip_position - _last_tip_position;
        bool    position_changed;
        if (Math.Abs(position_difference.y) < 0.003f && Math.Abs(position_difference.x) + Math.Abs(position_difference.z) < 0.1f)
            position_changed = false;
        else
        {
            position_changed           = true;
            _last_tip_position         = current_tip_position;
            _last_head_midpoint_height = _unit!.transform.InverseTransformPoint((current_tip_position + _contact_strip_second_end!.position) / 2.0f).y;
        }
        return (_last_head_midpoint_height, position_changed);
    }

    public override void Tick(float deltaTime)
    {
        if (get_wire_height_and_voltage == null)
            return; 

        float input_current = (_in_contact!.Value >= 0.5f) ? _input_current!.Value : 0.0f;
        if (float.IsNaN(input_current) || float.IsInfinity(input_current))
            input_current = 0.0f;
        (float head_height, bool head_position_changed) = get_head_midpoint_height();
        _head_height!.Value = head_height;
        if (Mathf.Abs(input_current) > 0.1f || head_position_changed)
        {
            (float? wire_height, _wire_voltage!.Value) = get_wire_height_and_voltage(_unit!.transform, _pantograph_base!, _contact_strip_first_end!, _contact_strip_second_end!, input_current);
            if (wire_height == null)
            {
                _wire_height!.Value = -1.0f;
                _in_contact.Value   = 0.0f;
            }
            else
            {
                float real_wire_height = (float) wire_height;
                _wire_height!.Value    = real_wire_height;
                _in_contact.Value      = (Mathf.Abs(real_wire_height - head_height) <= _contact_tolerance) ? 1.0f : 0.0f;
            }
        }
    }

    private void OnDestroy()
    {
        disable_power();
        TrainCar? unit = _unit;
        if (unit is not null && _all_catenary_controllers.TryGetValue(unit, out HashSet<pantograph_sim_controller> pantographControllers))
        {
            pantographControllers.Remove(this);
            if (pantographControllers.Count == 0)
                _all_catenary_controllers.Remove(unit); 
        }
    }

    public void map_from(MonoBehaviour proxy)
    {
        var real_proxy = (pantograph_sim_controller_proxy) proxy;
        _pantograph_base          = real_proxy.PantographBase;
        _contact_strip_first_end  = real_proxy.ContactStripFirstEnd;
        _contact_strip_second_end = real_proxy.ContactStripSecondEnd;
        _contact_tolerance        = real_proxy.ContactTolerance;

        _initial_height_port_ID = real_proxy.InitialHeightPortId;
        _head_height_port_ID    = real_proxy.HeadHeightPortId;
        _wire_height_port_ID    = real_proxy.WireHeightPortId;
        _wire_voltage_port_ID   = real_proxy.WireVoltagePortId;
        _is_in_contact_port_ID  = real_proxy.IsInContactPortId;
        _input_current_port_ID  = real_proxy.InputCurrentPortId;
    }
}
