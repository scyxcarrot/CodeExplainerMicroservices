using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public abstract class WrappedIbbCreator
    {
        protected readonly CMFObjectManager objectManager;

        protected WrappedIbbCreator(CMFObjectManager objectManager)
        {
            this.objectManager = objectManager;
        }

        protected Mesh CreateWrapBuildingBlock(IEnumerable<string> layerPaths)
        {
            var rhinoObjects = new List<RhinoObject>();

            foreach (var layerPath in layerPaths)
            {
                rhinoObjects.AddRange(objectManager.GetAllObjectsByLayerPath(layerPath));
            }

            return CreateWrap(rhinoObjects.Select(x => (Mesh)x.Geometry));
        }

        protected Mesh CreateWrap(IEnumerable<Mesh> meshes)
        {
            Mesh wrapped;
            Wrap.PerformWrap(meshes.ToArray(), 0.5, 0.0, 1.0, false, true, false, false, out wrapped);
            return wrapped;
        }
    }
}
