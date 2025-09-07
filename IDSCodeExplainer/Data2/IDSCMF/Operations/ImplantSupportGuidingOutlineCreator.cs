using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class ImplantSupportGuidingOutlineCreator
    {
        private readonly CMFImplantDirector _director;
        public ImplantSupportGuidingOutlineCreator(CMFImplantDirector director)
        {
            _director = director;
        }

        private List<Mesh> GetPreopParts()
        {
            var parts = new List<Mesh>();

            var rhinoObjects = ProPlanImportUtilities.GetAllPreOpLayerObjects(_director.Document).Where(
                x => x.Geometry is Mesh && x.Name.Contains(Constants.ProPlanImport.ObjectPrefix));

            foreach (var rhinoObject in rhinoObjects)
            {
                var mesh = rhinoObject.Geometry as Mesh;

                if (ProPlanImportUtilities.IsPartAsRangePartType(
                    new List<ProPlanImportPartType>() {ProPlanImportPartType.Other, ProPlanImportPartType.Nerve, ProPlanImportPartType.Metal, ProPlanImportPartType.Teeth},
                    _director.Document.Layers[rhinoObject.Attributes.LayerIndex].Name))
                {
                    continue;
                }

                parts.Add(mesh);
            }

            return parts;
        }

        private List<RhinoObject> GetPlacableOriginalParts()
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var placablePlannedParts = GetPlacablePlannedParts();
            var placablePurePartsName = placablePlannedParts.Select(r =>
                proPlanImportComponent.GetPurePartNameFromBlockName(r.Name, out _, out var purePartName) ? purePartName : r.Name);

            var originalRhinoObjects = ProPlanImportUtilities.GetAllOriginalLayerObjects(_director.Document).Where(x =>
                x.Geometry is Mesh &&
                x.Name.Contains(Constants.ProPlanImport.ObjectPrefix));

            var placableOriginalPartsNameAndObject = new List<RhinoObject>();

            foreach (var originalRhinoObject in originalRhinoObjects)
            {
                foreach (var placablePurePartName in placablePurePartsName)
                {
                    if (!proPlanImportComponent.GetPurePartNameFromBlockName(originalRhinoObject.Name, out _, out var originalPurePartName))
                    {
                        originalPurePartName = originalRhinoObject.Name;
                    }

                    if (originalPurePartName != placablePurePartName)
                    {
                        continue;
                    }

                    placableOriginalPartsNameAndObject.Add(originalRhinoObject);
                    break;
                }
            }

            return placableOriginalPartsNameAndObject;
        }

        private List<RhinoObject> GetPlacablePlannedParts()
        {
            var objectManager = new CMFObjectManager(_director);
            var proPlanImportComponent = new ProPlanImportComponent();
            var partNamePatterns = proPlanImportComponent.GetImplantPlacablePartNames();

            return objectManager.GetAllBuildingBlockRhinoObjectByMatchingNames(
                ProPlanImportComponent.StaticIBB, partNamePatterns);
        }

        private Dictionary<Curve, RhinoObject> GetOsteotomyIntersectionOutlines(Mesh mergedOsteotomyMesh, Mesh mergedPreopMesh)
        {
            var intersectionCurves = MeshIntersectionCurve.IntersectionCurve(mergedPreopMesh, mergedOsteotomyMesh);
            var jointedIntersectionCurves = Curve.JoinCurves(intersectionCurves.Select(x => x.ToNurbsCurve()),
                ImplantSupportOutlinesConstants.JoinCurveDistance).ToList();
            
            var implantSupportGuidingOutlinesInfo = new Dictionary<Curve, RhinoObject>();
            
            var placableOriginalParts = GetPlacableOriginalParts();
            foreach (var placableOriginalPart in placableOriginalParts)
            {
                var mesh = placableOriginalPart.Geometry as Mesh;
                if (mesh == null)
                {
                    continue;
                }

                var curves = CurveUtilities.TrimCurveNotCloseToMesh(mesh, jointedIntersectionCurves,
                    ImplantSupportOutlinesConstants.CurveMeshDistanceThreshold,
                    ImplantSupportOutlinesConstants.MovingAverageWindowsSize).Where(c => c != null).ToList();

                if (!curves.Any())
                {
                    continue;
                }
                
                curves = Curve.JoinCurves(curves.Select(x => x.ToNurbsCurve()),
                    ImplantSupportOutlinesConstants.JoinCurveDistance).ToList();

                foreach (var curve in curves)
                {
                    implantSupportGuidingOutlinesInfo.Add(curve, placableOriginalPart);
                }
            }

            return implantSupportGuidingOutlinesInfo;
        }

        public bool CreateImplantSupportGuidingOutlines(out Dictionary<Curve, RhinoObject> implantSupportGuidingOutlinesInfo, out Mesh osteotomiesPreop)
        {
            var preopParts = GetPreopParts();
            var osteotomyParts = ProPlanImportUtilities.GetAllOriginalOsteotomyParts(_director.Document);

            implantSupportGuidingOutlinesInfo = new Dictionary<Curve, RhinoObject>();
            osteotomiesPreop = null;

            if (preopParts.Count == 0 || osteotomyParts.Count == 0 ||
                !Booleans.PerformBooleanUnion(out Mesh mergedPreopMesh, preopParts.ToArray()) || 
                !Booleans.PerformBooleanUnion(out Mesh mergedOsteotomyMesh, osteotomyParts.ToArray()))
            {
                return false;
            }

            implantSupportGuidingOutlinesInfo = GetOsteotomyIntersectionOutlines(mergedPreopMesh, mergedOsteotomyMesh);
            var tolerance = 0.1;
            Wrap.PerformWrap(new[] { mergedOsteotomyMesh }, 1, 0, ImplantMarginParameters.MaxThickness + tolerance, 
                false, false, true, true, out var wrappedOsteotomyMesh);

            osteotomiesPreop = Booleans.PerformBooleanIntersection(wrappedOsteotomyMesh, mergedPreopMesh);
            return true;
        }
    }
}
