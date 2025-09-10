using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class GuideBaseCreator
    {
        //Int for Intermediate, Res for result
        public Mesh IntGuideBaseSurfaceOffset { get; private set; }
        public Mesh IntGuideBaseSurfaceOffsetCompensatedSize { get; private set; }
        public Mesh IntGuideBaseSurface { get; private set; }
        public Mesh IntGuideBaseSurfaceLightWeight { get; private set; }
        public Mesh ResGuideBase { get; private set; }

        public bool CreateGuideBaseSurface(Mesh guideSurface, GuideParams parameter)
        {
            var guideSurfaceFirstRemeshed = guideSurface.DuplicateMesh();

            var IsocurveDistance = parameter.GuideSurfaceIsoCurveDistance;

            IntGuideBaseSurfaceOffset = guideSurfaceFirstRemeshed;

            IntGuideBaseSurfaceOffsetCompensatedSize = new Mesh();
            IntGuideBaseSurfaceOffset = MeshUtilities.RemoveNoiseShells(IntGuideBaseSurfaceOffset, 1);
            var disjointedIntGuideBaseSurfaceOffset = IntGuideBaseSurfaceOffset.SplitDisjointPieces().ToList();
            

            foreach (var mesh in disjointedIntGuideBaseSurfaceOffset)
            {
                var compensated = SurfaceUtilities.CreateCompensatedMesh(mesh, IsocurveDistance);
                if (compensated == null)
                {
                    return false;
                }
                IntGuideBaseSurfaceOffsetCompensatedSize.Append(compensated);
            }

            //QPRT 2 times
            var secondRemeshParams = parameter.RemeshParams;
            var secondRemeshSurface = IntGuideBaseSurfaceOffsetCompensatedSize.DuplicateMesh();
            for (var i = 0; i < secondRemeshParams.OperationCount; ++i)
            {
                secondRemeshSurface = ExternalToolInterop.PerformQualityPreservingReduceTriangles(secondRemeshSurface,
                    secondRemeshParams.QualityThreshold, secondRemeshParams.MaximalGeometricError,
                    secondRemeshParams.CheckMaximalEdgeLength, secondRemeshParams.MaximalEdgeLength,
                    secondRemeshParams.NumberOfIterations, secondRemeshParams.SkipBadEdges,
                    secondRemeshParams.PreserveSurfaceBorders);
            }

            IntGuideBaseSurface = secondRemeshSurface.DuplicateMesh();

            return true;
        }

        public bool CreateGuideBaseLightweight(Mesh guideBase, GuideParams parameter)
        {
            IntGuideBaseSurface = guideBase.DuplicateMesh();

            //Apply lightweight
            IntGuideBaseSurfaceLightWeight = MeshFromPolyline.PerformMeshFromPolyline(IntGuideBaseSurface,
                parameter.LightweightParams.SegmentRadius,
                parameter.LightweightParams.FractionalTriangleEdgeLength);

            ResGuideBase = IntGuideBaseSurfaceLightWeight;

            return true;
        }


    }
}
