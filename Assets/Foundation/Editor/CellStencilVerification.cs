using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // GPU check of the original mask passes. This is diagnostic rendering only.
    public static class CellStencilVerification
    {
        public static void Verify(OriginalCellMask mask)
        {
            Require(mask.readMask.sharedMaterial.shader.name == "Custom/StencilMaskRead" && mask.writeMask.sharedMaterial.shader.name == "Custom/StencilMaskWrite", "Original stencil shader bindings");
            var mesh = new Mesh { name = "Cell stencil verification quad" };
            mesh.vertices = new[] { new Vector3(-1,-1,0),new Vector3(1,-1,0),new Vector3(1,1,0),new Vector3(-1,1,0) };
            mesh.triangles = new[] { 0,1,2,0,2,3,2,1,0,3,2,0 };
            mesh.colors = new[] { Color.white,Color.white,Color.white,Color.white };
            mesh.uv = new[] { Vector2.zero,Vector2.right,Vector2.one,Vector2.up };
            var target = new RenderTexture(32,32,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(32,32,TextureFormat.RGBA32,false,true);
            var probe = new Material(Shader.Find("UI/Default"));
            var commands = new CommandBuffer { name = "Verify native cell stencil passes" };
            var previous = RenderTexture.active;
            try
            {
                Require(target.Create() && probe.shader.isSupported, "Stencil GPU verification target/probe");
                probe.SetColor("_Color",Color.red);
                probe.SetInt("_StencilComp",(int)CompareFunction.Equal);
                probe.SetInt("_StencilOp",(int)StencilOp.Keep);
                probe.SetInt("_StencilReadMask",255);
                probe.SetInt("_StencilWriteMask",0);
                probe.SetInt("_ColorMask",15);
                probe.SetTexture("_MainTex",Texture2D.whiteTexture);
                for(int scenario = 0; scenario < 4; scenario++)
                {
                    commands.Clear(); commands.SetRenderTarget(target);
                    commands.ClearRenderTarget(true,true,Color.clear);
                    commands.SetViewProjectionMatrices(Matrix4x4.identity,Matrix4x4.identity);
                    if(scenario != 1) commands.DrawMesh(mesh,Matrix4x4.identity,mask.writeMask.sharedMaterial);
                    if(scenario == 3) commands.DrawMesh(mesh,Matrix4x4.identity,mask.writeMask.sharedMaterial);
                    if(scenario != 0) commands.DrawMesh(mesh,Matrix4x4.identity,mask.readMask.sharedMaterial);
                    Graphics.ExecuteCommandBuffer(commands);
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0,0,32,32),0,0); pixels.Apply(false);
                    var unchanged = pixels.GetPixel(16,16);
                    Require(unchanged.maxColorComponent < .01f && unchanged.a < .01f, "Mask passes preserve the color buffer");
                    int expectedStencil = scenario == 0 || scenario == 3 ? 1 : 0;
                    probe.SetInt("_Stencil",expectedStencil);
                    commands.Clear();commands.SetRenderTarget(target);
                    commands.SetViewProjectionMatrices(Matrix4x4.identity,Matrix4x4.identity);
                    commands.DrawMesh(mesh,Matrix4x4.identity,probe);
                    Graphics.ExecuteCommandBuffer(commands);
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0,0,32,32),0,0); pixels.Apply(false);
                    var color = pixels.GetPixel(16,16);
                    Require(color.r > .99f && color.g < .01f && color.b < .01f && color.a > .99f, "Native stencil increment/decrement/saturation result");
                }
            }
            finally
            {
                RenderTexture.active = previous; commands.Release(); target.Release();
                Object.DestroyImmediate(mesh);Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);Object.DestroyImmediate(probe);
            }
        }
        private static void Require(bool value,string label) { if(!value) throw new InvalidOperationException(label); }
    }
}
