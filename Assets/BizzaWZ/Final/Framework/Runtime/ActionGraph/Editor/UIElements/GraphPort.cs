using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public enum E_PortType
{
    Action,
    VariableInput,
    VariableOutput,
}

public class PortData
{
    public E_PortType portType;

    public NodeBase actionData;
    public GraphDebugUtils.VariableViewData variableInputData;
    public (Type, string) variableOutputData;
}

public class GraphPort : Port
{
    protected GraphPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type) : base(portOrientation, portDirection, portCapacity, type)
    {

    }
}
