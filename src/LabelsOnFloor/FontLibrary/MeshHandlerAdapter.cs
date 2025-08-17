using UnityEngine;

namespace LabelsOnFloor.FontLibrary
{
    public class MeshHandlerAdapter : MeshHandler
    {
        private readonly MeshHandlerNew _newHandler;

        public MeshHandlerAdapter(MeshHandlerNew newHandler) : base(null)
        {
            _newHandler = newHandler;
        }

        public override Mesh GetMeshFor(string label)
        {
            return _newHandler.GetMeshFor(label);
        }
    }
}