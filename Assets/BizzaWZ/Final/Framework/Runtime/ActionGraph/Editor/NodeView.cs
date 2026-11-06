// using System;
// using System.Collections.Generic;
// using UnityEditor;
// using UnityEditor.Experimental.GraphView;
// using UnityEngine;
//
// namespace Bizza.ActionSystem.Editor
// {
//     public class NodeView : UnityEditor.Experimental.GraphView.Node
//     {
//         public Node Node { get { return m_node; } }
//
//         public Dictionary<string, Port> inputPorts;
//         public Dictionary<string, Port> outputPorts;
//
//         private Node m_node;
//         public override bool showInMiniMap { get => true; set => base.showInMiniMap = value; }
//         private Action<NodeView> m_onSelected;
//
//         public NodeView(Node node, Orientation orientation, E_GraphDirection graphDirection, Action<NodeView> onSelected)
//         {
//             m_node = node;
//             viewDataKey = node.guid;
//             style.left = node.position.x;
//             style.top = node.position.y;
//             m_onSelected = onSelected;
//             inputPorts = new Dictionary<string, Port>();
//             outputPorts = new Dictionary<string, Port>();
//
//             var nodeType = node.GetType();
//
//             CreatePorts(node, nodeType.GetFields(), orientation, graphDirection);
//
//             if (GetAttribute(nodeType, out NodeAttribute nodeAttr))
//             {
//                 title = nodeAttr.title;
//                 style.width = nodeAttr.width;
//             }
//             else
//             {
//                 title = node.name;
//                 style.width = 200f;
//             }
//
//             if (GetAttribute(nodeType, out NodeColorAttribute nodeColorAttr))
//             {
//                 titleContainer.style.backgroundColor = nodeColorAttr.titleColor;
//             }
//         }
//
//         public override void SetPosition(Rect newPos)
//         {
//             base.SetPosition(newPos);
//             m_node.position.x = newPos.xMin;
//             m_node.position.y = newPos.yMin;
//             EditorUtility.SetDirty(m_node);
//         }
//
//         private void CreatePorts(Node node, System.Reflection.FieldInfo[] fields, Orientation orientation, E_GraphDirection graphDirection)
//         {
//             for (int i = 0; i < fields.Length; i++)
//             {
//                 System.Reflection.FieldInfo f = fields[i];
//                 if (f.FieldType.IsSubclassOf(typeof(InputNodePort)))
//                 {
//                     var capacity = Port.Capacity.Single;
//                     if (GetAttribute(f, out NodePortAttribute nodePortAttribute))
//                     {
//                         capacity = nodePortAttribute.Capacity == E_PortCapacity.Single ? Port.Capacity.Single : Port.Capacity.Multi;
//                     }
//                     NodePort nodePort = f.GetValue(node) as NodePort;
//                     if (graphDirection == E_GraphDirection.Left2Right)
//                         CreateInputPort(orientation, capacity, nodePort.GetPortType, f.Name, nodePort);
//                     else
//                         CreateOutputPort(orientation, capacity, nodePort.GetPortType, f.Name, nodePort);
//
//                 }
//                 else if (f.FieldType.IsSubclassOf(typeof(OutputNodePort)))
//                 {
//                     var capacity = Port.Capacity.Single;
//                     if (GetAttribute(f, out NodePortAttribute nodePortAttribute))
//                     {
//                         capacity = nodePortAttribute.Capacity == E_PortCapacity.Single ? Port.Capacity.Single : Port.Capacity.Multi;
//                     }
//                     NodePort nodePort = f.GetValue(node) as NodePort;
//                     if (graphDirection == E_GraphDirection.Left2Right)
//                         CreateOutputPort(orientation, capacity, nodePort.GetPortType, f.Name, nodePort);
//                     else
//                         CreateInputPort(orientation, capacity, nodePort.GetPortType, f.Name, nodePort);
//                 }
//             }
//         }
//
//         private void CreateInputPort(Orientation orientation, Port.Capacity capacity, Type portType, string portName, NodePort nodePort)
//         {
//             var inputPort = InstantiatePort(orientation, Direction.Input, capacity, portType);
//             inputPort.portName = portName;
//             inputPort.source = nodePort;
//             inputPorts.Add(portName, inputPort);
//             inputContainer.Add(inputPort);
//         }
//
//         private void CreateOutputPort(Orientation orientation, Port.Capacity capacity, Type portType, string portName, NodePort nodePort)
//         {
//             var outputPort = InstantiatePort(orientation, Direction.Output, capacity, portType);
//             outputPort.portName = portName;
//             outputPort.source = nodePort;
//             outputPorts.Add(portName, outputPort);
//             outputContainer.Add(outputPort);
//         }
//
//         private bool GetAttribute<T>(Type nodeType, out T attr) where T : class
//         {
//             var nodeAttributes = nodeType.GetCustomAttributes(typeof(T), true);
//             if (nodeAttributes.Length > 0)
//             {
//                 attr = nodeAttributes[0] as T;
//                 return true;
//             }
//             else
//             {
//                 attr = null;
//                 return false;
//             }
//         }
//
//         private bool GetAttribute<T>(System.Reflection.FieldInfo fieldInfo, out T attr) where T : class
//         {
//             var nodeAttributes = fieldInfo.GetCustomAttributes(typeof(T), true);
//             if (nodeAttributes.Length > 0)
//             {
//                 attr = nodeAttributes[0] as T;
//                 return true;
//             }
//             else
//             {
//                 attr = null;
//                 return false;
//             }
//         }
//
//         public override void OnSelected()
//         {
//             base.OnSelected();
//             if (m_onSelected != null)
//                 m_onSelected(this);
//         }
//     }
//
// }
