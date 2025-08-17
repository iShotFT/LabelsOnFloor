using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LabelsOnFloor.FontLibrary
{
    public class MeshHandlerNew
    {
        private readonly Dictionary<string, Mesh> _cachedMeshes = new Dictionary<string, Mesh>();
        private FontRenderer _renderer;
        private IFont _font;

        public MeshHandlerNew()
        {
            _font = FontRegistry.DefaultFont;
            _renderer = new FontRenderer(_font);
        }

        public MeshHandlerNew(IFont font)
        {
            _font = font ?? FontRegistry.DefaultFont;
            _renderer = new FontRenderer(_font);
        }
        
        public MeshHandlerNew(string fontName)
        {
            SetFont(fontName);
        }
        
        public void SetFont(string fontName)
        {
            var newFont = FontRegistry.GetFont(fontName);
            if (newFont == null)
            {
                ModLog.Warning($"Font '{fontName}' not found, using default font");
                newFont = FontRegistry.DefaultFont;
            }
            
            if (newFont != _font)
            {
                _font = newFont;
                _renderer = new FontRenderer(_font);
                ClearCache();
                ModLog.Message($"MeshHandlerNew: Switched to font '{fontName}'");
            }
        }

        public Mesh GetMeshFor(string label)
        {
            if (string.IsNullOrEmpty(label))
                return null;

            if (_cachedMeshes.TryGetValue(label, out var mesh))
                return mesh;

            float width;
            mesh = _renderer.GenerateMesh(label, out width);
            
            if (mesh != null)
            {
                _cachedMeshes[label] = mesh;
            }

            return mesh;
        }

        public void ClearCache()
        {
            foreach (var mesh in _cachedMeshes.Values)
            {
                if (mesh != null)
                {
                    Object.Destroy(mesh);
                }
            }
            _cachedMeshes.Clear();
            _renderer.ClearCache();
        }

        public IFont Font => _font;
    }
}