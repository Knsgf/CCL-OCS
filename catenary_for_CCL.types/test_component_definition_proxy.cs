using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using CCL.Types.Proxies.Ports;

namespace catenary_for_CCL.types;

[AddComponentMenu("Catenary/Test Component Definition")]
public class test_component_definition_proxy: SimComponentDefinitionProxy
{
    public int val;
    public override IEnumerable<PortDefinition> ExposedPorts =>
    [
        new PortDefinition(DVPortType.READONLY_OUT, DVPortValueType.GENERIC, "COUNTER")
    ];

    public override IEnumerable<PortReferenceDefinition> ExposedPortReferences =>
    [
        new PortReferenceDefinition(DVPortValueType.CONTROL, "HANDLE")
    ];
}
