using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// RenderingUtils
    /// </summary>
    public static class RenderingUtils
    {
        static Mesh _quadMesh;
        static Texture2D _transparentTexture;

        public static Mesh quadMesh
        {
            get
            {
                if (!_quadMesh)
                {
                    var vertices = new[]
                    {
                        new Vector3(-0.5f, -0.5f, 0f),
                        new Vector3(0.5f,  0.5f, 0f),
                        new Vector3(0.5f, -0.5f, 0f),
                        new Vector3(-0.5f,  0.5f, 0f)
                    };

                    var uvs = new[]
                    {
                        new Vector2(0f, 0f),
                        new Vector2(1f, 1f),
                        new Vector2(1f, 0f),
                        new Vector2(0f, 1f)
                    };

                    var indices = new[] { 0, 1, 2, 1, 0, 3 };

                    _quadMesh = new Mesh
                    {
                        vertices = vertices,
                        uv = uvs,
                        triangles = indices
                    };

                    _quadMesh.RecalculateNormals();
                    _quadMesh.RecalculateBounds();
                }

                return _quadMesh;
            }
        }

        public static Texture2D transparentTexture
        {
            get
            {
                if (!_transparentTexture)
                {
                    _transparentTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
                    _transparentTexture.SetPixel(0, 0, default);
                    _transparentTexture.Apply(false, true);
                }
                return _transparentTexture;
            }
        }

    } // class RenderingUtils

} // namespace UnityExtensions