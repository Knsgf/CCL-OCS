using System.Collections.Generic;

using UnityEngine;

using CCL.Types;
using CCL.Types.Proxies.Ports;

namespace catenary_for_CCL.types;

[AddComponentMenu("Catenary Addon/Pantograph Sim Controller")]
public class pantograph_sim_controller_proxy: MonoBehaviour, IHasPortIdFields, ISelfValidation
{
    public Transform? PantographBase;
    public Transform? ContactStripFirstEnd, ContactStripSecondEnd;
        
    [Min(0.01f), Tooltip("Maximum vertical offset between wire and strip midpoint for contact to register")]
    public float ContactTolerance = 0.2f;

    [PortId(DVPortType.EXTERNAL_IN, DVPortValueType.GENERIC, local: true)]
    public string InitialHeightPortId = string.Empty;
    [PortId(DVPortType.EXTERNAL_IN, DVPortValueType.GENERIC, local: true)]
    public string HeadHeightPortId = string.Empty;
    [PortId(DVPortType.EXTERNAL_IN, DVPortValueType.GENERIC, local: true)]
    public string WireHeightPortId = string.Empty;
    [PortId(DVPortType.EXTERNAL_IN, DVPortValueType.VOLTS  , local: true)]
    public string WireVoltagePortId = string.Empty;
    [PortId(DVPortType.EXTERNAL_IN, DVPortValueType.STATE  , local: true)]
    public string IsInContactPortId = string.Empty;

    [PortId(DVPortValueType.AMPS)]
    public string InputCurrentPortId = string.Empty;

    public IEnumerable<PortIdField> ExposedPortIdFields =>
    [
        new(this, nameof(InitialHeightPortId), InitialHeightPortId, DVPortType.EXTERNAL_IN, DVPortValueType.GENERIC),
        new(this, nameof(   HeadHeightPortId),    HeadHeightPortId, DVPortType.EXTERNAL_IN, DVPortValueType.GENERIC),
        new(this, nameof(   WireHeightPortId),    WireHeightPortId, DVPortType.EXTERNAL_IN, DVPortValueType.GENERIC),
        new(this, nameof(  WireVoltagePortId),   WireVoltagePortId, DVPortType.EXTERNAL_IN, DVPortValueType.VOLTS  ),
        new(this, nameof(  IsInContactPortId),   IsInContactPortId, DVPortType.EXTERNAL_IN, DVPortValueType.STATE  ),
        new(this, nameof( InputCurrentPortId),  InputCurrentPortId, DVPortValueType.AMPS)
    ];

    public SelfValidationResult Validate(out string message, out string? highlight)
    {
        if (PantographBase == null)
            return this.FailForNull(nameof(       PantographBase), out message, out highlight); 
        if (ContactStripFirstEnd == null)
            return this.FailForNull(nameof( ContactStripFirstEnd), out message, out highlight); 
        if (ContactStripSecondEnd == null)
            return this.FailForNull(nameof(ContactStripSecondEnd), out message, out highlight); 
        if (PantographBase == ContactStripFirstEnd || PantographBase == ContactStripSecondEnd)
        {
            message   = $"{nameof(PantographBase)} should not be identical to {nameof(ContactStripFirstEnd)} or {nameof(ContactStripSecondEnd)}";
            highlight =    nameof(PantographBase);
            return SelfValidationResult.Warning;
        }
        if (ContactStripFirstEnd == ContactStripSecondEnd)
        {
            message   = $"{nameof(ContactStripFirstEnd)} and {nameof(ContactStripSecondEnd)} should not be identical";
            highlight =                                       nameof(ContactStripSecondEnd);
            return SelfValidationResult.Warning;
        }
        return this.Pass(out message, out highlight);
    }

    public void ConnectPowerCollector(power_collector_common_ports_definition_proxy powerCollectorProxy)
    {
        InitialHeightPortId = powerCollectorProxy.GetFullPortId("INITIAL_HEAD_HEIGHT"  );
        HeadHeightPortId    = powerCollectorProxy.GetFullPortId("HEAD_HEIGHT"          );
        WireHeightPortId    = powerCollectorProxy.GetFullPortId("WIRE_HEIGHT"          );
        WireVoltagePortId   = powerCollectorProxy.GetFullPortId("WIRE_VOLTAGE"         );
        IsInContactPortId   = powerCollectorProxy.GetFullPortId("PANTOGRAPH_IN_CONTACT");
    }

    private void Reset()
    {
        if (gameObject.TryGetComponent<power_collector_common_ports_definition_proxy>(out power_collector_common_ports_definition_proxy powerCollectorProxy))
        {
            ConnectPowerCollector(powerCollectorProxy); 
        }
    }
}
