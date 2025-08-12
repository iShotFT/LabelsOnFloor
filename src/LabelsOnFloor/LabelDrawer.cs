using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    public class LabelDrawer
    {
        private readonly LabelHolder _labelHolder;

        private readonly FontHandler _fontHandler;

        public LabelDrawer(LabelHolder labelHolder, FontHandler fontHandler)
        {
            _labelHolder = labelHolder;
            _fontHandler = fontHandler;
        }

        public void Draw()
        {
            var currentViewRect = Find.CameraDriver.CurrentViewRect;
            foreach (var label in _labelHolder.GetLabels())
            {
                if (!currentViewRect.Contains(label.LabelPlacementData.Position))
                    continue;

                DrawLabel(label);
            }
        }

        private void DrawLabel(Label label)
        {
            Matrix4x4 matrix = default;
            var pos = label.LabelPlacementData.Position.ToVector3();
            pos.x += 0.2f;

            var rotation = Quaternion.identity;
            if (label.LabelPlacementData.Flipped)
            {
                rotation = Quaternion.AngleAxis(90, Vector3.up);
                pos.z++;
                pos.z -= 0.2f;
            }
            else
            {
                pos.z += 0.2f;
            }

            matrix.SetTRS(pos, rotation, label.LabelPlacementData.Scale);

            // Get the material and apply custom color if available
            var material = _fontHandler.GetMaterial();
            if (label.CustomColor.HasValue)
            {
                // Create a new material instance with the custom color
                material = new Material(material);
                var colorWithOpacity = label.CustomColor.Value;
                colorWithOpacity.a = Main.Instance.GetOpacity();
                material.color = colorWithOpacity;
            }

            Graphics.DrawMesh(label.LabelMesh, matrix, material, 0);

        }
    }
}