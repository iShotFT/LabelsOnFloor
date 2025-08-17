using System.Collections.Generic;
using UnityEngine;

namespace LabelsOnFloor.FontLibrary
{
    public class FontRenderer
    {
        private readonly Dictionary<string, CachedMesh> _meshCache = new Dictionary<string, CachedMesh>();
        private readonly IFont _font;

        private class CachedMesh
        {
            public Mesh Mesh { get; set; }
            public float Width { get; set; }
        }

        public FontRenderer(IFont font)
        {
            _font = font;
        }

        public Mesh GenerateMesh(string text, out float totalWidth)
        {
            if (string.IsNullOrEmpty(text))
            {
                totalWidth = 0;
                return null;
            }

            string cacheKey = $"{_font.Name}:{text}";
            if (_meshCache.TryGetValue(cacheKey, out var cached))
            {
                totalWidth = cached.Width;
                return cached.Mesh;
            }

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var colors = new List<Color>();

            float xOffset = 0f;
            
            foreach (char c in text)
            {
                if (!_font.SupportsCharacter(c) && c != ' ')
                {
                    continue;
                }

                var glyph = _font.GetGlyph(c);
                if (!glyph.IsValid)
                {
                    continue;
                }

                int vertexIndex = vertices.Count;
                
                // Add slight vertical padding (0.1f) to push text up from bottom
                float verticalPadding = 0.1f;
                float bottom = -0.4f + verticalPadding;
                float top = bottom + glyph.Height;
                
                vertices.Add(new Vector3(xOffset, 0f, bottom));
                vertices.Add(new Vector3(xOffset, 0f, top));
                vertices.Add(new Vector3(xOffset + glyph.Width, 0f, top));
                vertices.Add(new Vector3(xOffset + glyph.Width, 0f, bottom));

                uvs.Add(glyph.UvBottomLeft);
                uvs.Add(glyph.UvTopLeft);
                uvs.Add(glyph.UvTopRight);
                uvs.Add(glyph.UvBottomRight);

                colors.Add(Color.white);
                colors.Add(Color.white);
                colors.Add(Color.white);
                colors.Add(Color.white);

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 3);

                xOffset += glyph.Width;
            }

            if (vertices.Count == 0)
            {
                totalWidth = 0;
                return null;
            }

            var mesh = new Mesh
            {
                name = $"Label_{text}",
                vertices = vertices.ToArray(),
                uv = uvs.ToArray(),
                triangles = triangles.ToArray(),
                colors = colors.ToArray()
            };

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            var cachedMesh = new CachedMesh
            {
                Mesh = mesh,
                Width = xOffset
            };

            _meshCache[cacheKey] = cachedMesh;
            totalWidth = xOffset;

            return mesh;
        }

        public void ClearCache()
        {
            foreach (var cached in _meshCache.Values)
            {
                if (cached.Mesh != null)
                {
                    Object.Destroy(cached.Mesh);
                }
            }
            _meshCache.Clear();
        }
    }
}