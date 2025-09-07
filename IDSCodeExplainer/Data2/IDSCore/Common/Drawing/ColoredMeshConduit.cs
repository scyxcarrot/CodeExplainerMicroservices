using Rhino.Display;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.Core.Drawing
{
    public class ColoredMeshConduit : DisplayConduit
    {
        private readonly Dictionary<Mesh, DisplayMaterial> meshesMappings;

        public ColoredMeshConduit()
        {
            meshesMappings = new Dictionary<Mesh, DisplayMaterial>();
        }

        public void AddMesh(Mesh mesh, Color color)
        {
            if (!meshesMappings.ContainsKey(mesh))
            {
                meshesMappings.Add(mesh, new DisplayMaterial(color));
            }
        }

        public void RemoveMesh(Mesh mesh)
        {
            if (meshesMappings.ContainsKey(mesh))
            {
                meshesMappings.Remove(mesh);
            }
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);

            foreach (var meshMapping in meshesMappings)
            {
                e.Display.DrawMeshShaded(meshMapping.Key, meshMapping.Value);
            }
        }
    }
}