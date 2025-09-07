using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class GuideGuidingOutlineCreator
    {
        private readonly CMFImplantDirector _director;
        public GuideGuidingOutlineCreator(CMFImplantDirector director)
        {
            _director = director;
        }

        public void CreateGuideFlangeGuidingOutline(out List<Curve> flangeGuidingOutline)
        {
            var objManager = new CMFObjectManager(_director);
            var guideSupport = (Mesh)objManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
            var osteotomyParts = ProPlanImportUtilities.GetAllOriginalOsteotomyParts(_director.Document);
            if (osteotomyParts.Count == 0)
            {
                flangeGuidingOutline = new List<Curve>();
                return;
            }
            
            flangeGuidingOutline = GetOsteotomyIntersectionOutlines(guideSupport, false, osteotomyParts);
        }

        private List<Curve> GetOsteotomyIntersectionOutlines(Mesh guideSupport, bool isWrapped, List<Mesh> osteotomyParts)
        {
            Mesh mergedOsteotomyMesh = new Mesh();
            if(isWrapped)
            {
                mergedOsteotomyMesh = MeshUtilities.OffsetMesh(osteotomyParts.ToArray(), 0.25, 0.0, false, true);
            }
            else
            {
                osteotomyParts.ForEach(x => mergedOsteotomyMesh.Append(x.DuplicateMesh()));
            }

            var intersectionCurves = MeshIntersectionCurve.IntersectionCurve(guideSupport, mergedOsteotomyMesh);
            return Curve.JoinCurves(intersectionCurves.Select(x => x.ToNurbsCurve()), 0.1).ToList();
        }
    }
}
