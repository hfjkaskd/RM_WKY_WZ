using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class Empty4Raycast : MaskableGraphic
{

    protected Empty4Raycast()
    {
        useLegacyMeshGeneration = false;
    }


    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
    }
}
