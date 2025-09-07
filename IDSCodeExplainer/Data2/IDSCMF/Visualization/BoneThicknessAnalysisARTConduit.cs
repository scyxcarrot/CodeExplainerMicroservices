using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMF.Visualization
{
    public class BoneThicknessAnalysisARTConduit: DisplayConduit, IDisposable
    {
        public class ScrewMeshConduitParam: IDisposable
        {
            public Mesh Mesh { get; }

            public DisplayMaterial Material { get; }

            public ScrewMeshConduitParam(Screw screw)
            {
                Mesh = MeshUtilities.ConvertBrepToMesh((Brep)screw.Geometry); ;
                Material = new DisplayMaterial(ScrewUtilities.GetScrewTypeColor(screw));
            }

            public void Dispose()
            {
                Mesh?.Dispose();
                Material.Dispose();
            }
        }

        public List<ScrewMeshConduitParam> ConduitsParam { get; } = new List<ScrewMeshConduitParam>();

        public BoundingBox AllMeshBoundingBox
        {
            get
            {
                var boundingBox = BoundingBox.Unset;

                foreach (var meshParam in ConduitsParam)
                {
                    if (meshParam.Mesh != null)
                    {
                        var individualBoundingBox = meshParam.Mesh.GetBoundingBox(false);
                        if (boundingBox.IsValid)
                        {
                            boundingBox.Union(individualBoundingBox);
                        }
                        else
                        {
                            boundingBox = individualBoundingBox;
                        }
                    }
                }

                return boundingBox;
            }
        }

        ~BoneThicknessAnalysisARTConduit()
        {
            Dispose();
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            e.IncludeBoundingBox(AllMeshBoundingBox);
        }


        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);
            foreach (var meshParam in ConduitsParam)
            {
                e.Display.DrawMeshShaded(meshParam.Mesh, meshParam.Material);
            }
        }

        public void Dispose()
        {
            foreach (var meshParam in ConduitsParam)
            {
                meshParam.Dispose();
            }

            Enabled = false;
        }
    }
}
