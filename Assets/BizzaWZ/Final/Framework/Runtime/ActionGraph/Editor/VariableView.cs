// using System.Collections;
// using System.Collections.Generic;
// using Sirenix.OdinInspector.Editor;
// using UnityEngine;
// using UnityEngine.UIElements;
//
//
// {
//     public class VariableView : VisualElement
//     {
//         public new class UxmlFactory : UxmlFactory<VariableView, UxmlTraits>
//         {
//
//         }
//
//         private PropertyTree _propertyTree;
//
//         public VariableView()
//         {
//             // var label = new Label();
//             // label.text = "abc";
//             // Add(label);
//         }
//
//         public void Init(VariableContainer container)
//         {
//             if (container == null)
//             {
//                 return;
//             }
//
//             _propertyTree = PropertyTree.Create(container);
//
//             IMGUIContainer imGUIContainer = new IMGUIContainer(() =>
//             {
//                 GUILayout.BeginArea(new Rect(0, 0, 300, 300));
//                 GUILayout.EndArea();
//                 if (container != null)
//                 {
//                     _propertyTree.Draw(false);
//                 }
//             });
//             imGUIContainer.style.maxHeight = 200;
//             Add(imGUIContainer);
//         }
//     }
//
// }
//
