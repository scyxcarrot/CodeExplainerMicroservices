using IDS.Core.Operations;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;

namespace IDS.Glenius.Operations
{
    public class SolidWallWrapCreator
    {
        private readonly Curve solidWallCurve;
        private readonly Mesh scaffoldSide;

        public Mesh SolidWall { get; private set; }

        public SolidWallWrapCreator(Curve solidWallCurve, Mesh scaffoldSide)
        {
            this.solidWallCurve = solidWallCurve;
            this.scaffoldSide = scaffoldSide;
        }

        public bool Create()
        {
            var scaffoldDuplicate = scaffoldSide.DuplicateMesh();
            var sideWallCurveDupe = solidWallCurve.DuplicateCurve();

            sideWallCurveDupe = sideWallCurveDupe.PullToMesh(scaffoldDuplicate, 0.05);

            var splittedSurfaces = MeshOperations.SplitMeshWithCurves(scaffoldDuplicate, new List<Curve>() { sideWallCurveDupe }, true);

            if (splittedSurfaces != null && splittedSurfaces.Count >= 2)
            {
                splittedSurfaces.RemoveAt(splittedSurfaces.Count - 1);
                splittedSurfaces = MeshUtilities.FilterSmallMeshes(splittedSurfaces, 3.0);
                var solidWallRaw = MeshUtilities.AppendMeshes(splittedSurfaces);

                if (solidWallRaw != null)
                {
                    Mesh solidWallWrap;
                    //var wrapParams = new MDCKShrinkWrapParameters(0.3, 0.0, 1.0, false, true, false, false);
                    if (Wrap.PerformWrap(new[] { solidWallRaw }, 0.3, 0.0, 1.0, false, true, false, false, out solidWallWrap))
                    {
                        SolidWall = solidWallWrap;
                        return true;
                    }
                }
            }

            return false;
        }

    }
}
