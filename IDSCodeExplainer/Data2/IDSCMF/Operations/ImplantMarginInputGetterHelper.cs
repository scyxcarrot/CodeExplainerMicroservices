using IDS.CMF.Constants;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class ImplantMarginInputGetterHelper
    {
        private readonly CMFImplantDirector _director;

        public ImplantMarginInputGetterHelper(CMFImplantDirector director)
        {
            _director = director;
        }

        public void SetVisibleForAffectedParts(IEnumerable<RhinoObject> affectedParts)
        {
            var affectedPartsFullPath = affectedParts.Select(o =>
                _director.Document.Layers[o.Attributes.LayerIndex].FullPath).ToList();
            Visibility.SetVisible(_director.Document, affectedPartsFullPath, true, false, true);
        }

        private static Transform CalculateMarginTransform(RhinoObject originalPart, RhinoObject plannedPart)
        {
            var originalTransform =
                new Transform((Transform)originalPart.Attributes.UserDictionary[ImplantMarginConstants.KeyTransformationMatrix]);
            var plannedTransform =
                new Transform((Transform)plannedPart.Attributes.UserDictionary[ImplantMarginConstants.KeyTransformationMatrix]);
            
            if (!originalTransform.TryGetInverse(out Transform inverseTrans))
            {
                inverseTrans = originalTransform;
            }

            var finalTransform = Transform.Multiply(plannedTransform, inverseTrans);
            return finalTransform;
        }

        public Transform GetMarginTransform(RhinoObject originalPart)
        {
            var plannedPart = ProPlanImportUtilities.GetPlannedObjectByOriginalObject(_director.Document, originalPart);
            return plannedPart == null ? Transform.Identity : CalculateMarginTransform(originalPart, plannedPart);
        }

        public static Curve GetPickedCurve(List<Curve> curves, Point3d pickedPoint)
        {
            var ptClosest = CurveUtilities.GetClosestPointFromCurves(
                curves, pickedPoint, out var endPtClosestCurve, out _);

            if (pickedPoint.DistanceTo(ptClosest) > 0.001)
            {
                return null;
            }

            return endPtClosestCurve;
        }

        public static Curve TrimCurve(Point3d pointA, Point3d pointB, Curve selectedCurve)
        {
            selectedCurve.ClosestPoint(pointA, out var pointAOnCurveParam);
            selectedCurve.ClosestPoint(pointB, out var pointBOnCurveParam);

            if (Math.Abs(pointAOnCurveParam - pointBOnCurveParam) < 0.001)
            {
                return null;
            }

            var curve1 = selectedCurve.Trim(pointAOnCurveParam, pointBOnCurveParam);
            var curve2 = selectedCurve.Trim(pointBOnCurveParam, pointAOnCurveParam);

            if (curve1 == null)
            {
                return curve2;
            }

            if (curve2 == null)
            {
                return curve1;
            }

            return curve1.GetLength() < curve2.GetLength() ? curve1 : curve2;
        }
    }
}
