using UnityEngine.UIElements;

namespace Bizza.ActionSystem
{
    public class GraphMiniMap : UnityEditor.Experimental.GraphView.MiniMap
    {
        public class CustomUxmlFactory : UxmlFactory<GraphMiniMap, UxmlTraits> { }
    }
}
