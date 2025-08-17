using UnityEngine;
using Verse;
using LabelsOnFloor.FontLibrary;

namespace LabelsOnFloor
{
    public class LabelDrawer
    {
        private readonly LabelHolder _labelHolder;
        private readonly MeshHandlerNew _meshHandler;

        public LabelDrawer(LabelHolder labelHolder, MeshHandlerNew meshHandler)
        {
            _labelHolder = labelHolder;
            _meshHandler = meshHandler;
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
            var color = label.CustomColor ?? Main.Instance.GetDefaultLabelColor();
            var material = _meshHandler.Font.GetMaterial(color, Main.Instance.GetOpacity());

            if (material != null && label.LabelMesh != null)
            {
                Graphics.DrawMesh(label.LabelMesh, matrix, material, 0);
            }
        }
    }
}