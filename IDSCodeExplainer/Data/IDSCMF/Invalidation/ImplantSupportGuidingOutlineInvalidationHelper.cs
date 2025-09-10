using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Invalidation
{
    public class ImplantSupportGuidingOutlineInvalidationHelper
    {
        private readonly CMFImplantDirector _director;
        private readonly List<CurveProperties> _preGuidingOutlineMap;
        private readonly List<CurveProperties> _postGuidingOutlineMap;
        private readonly List<CurveProperties> _toMaintainGuidingOutlineMap;

        public ImplantSupportGuidingOutlineInvalidationHelper(CMFImplantDirector director)
        {
            _director = director;
            _preGuidingOutlineMap = new List<CurveProperties>();
            _postGuidingOutlineMap = new List<CurveProperties>();
            _toMaintainGuidingOutlineMap = new List<CurveProperties>();
        }

        public class CurveProperties
        {
            public Guid Id { get; set; }
            public Curve Curve { get; set; }
            public string TouchingPartName { get; set; }

            public CurveProperties(Guid id, Curve curve, string touchingPartName)
            {
                Id = id;
                Curve = curve;
                TouchingPartName = touchingPartName;
            }
        }

        public void SetPreGuidingOutlineInfo()
        {
            _preGuidingOutlineMap.Clear();

            var objectManager = new CMFObjectManager(_director);

            var implantSupportOutlineObjects = objectManager.GetAllBuildingBlocks(IBB.ImplantSupportGuidingOutline);

            foreach (var implantSupportOutlineObject in implantSupportOutlineObjects)
            {
                var curveProperties = new CurveProperties(implantSupportOutlineObject.Id, (Curve)implantSupportOutlineObject.Geometry.Duplicate(), null);

                if (ImplantSupportGuidingOutlineHelper.ExtractTouchingOriginalPartId(implantSupportOutlineObject, out var originalPartGuid))
                {
                    var originalPart = _director.Document.Objects.Find(originalPartGuid);
                    curveProperties.TouchingPartName = originalPart.Name;
                }

                _preGuidingOutlineMap.Add(curveProperties);
            }
        }

        public bool IsGuidingOutlineChanged(Guid preOutlineId)
        {
            if (!_postGuidingOutlineMap.Any())
            {
                SetPostGuidingOutlineInfo();
            }

            var oldCurve = _preGuidingOutlineMap.FirstOrDefault(o => o.Id == preOutlineId);
            if (oldCurve == null)
            {
                return false;
            }

            if (_toMaintainGuidingOutlineMap.Contains(oldCurve))
            {
                return false;
            }
            else if (ContainsCurve(_postGuidingOutlineMap, oldCurve, out var newCurve))
            {
                _postGuidingOutlineMap.Remove(newCurve);
                _toMaintainGuidingOutlineMap.Add(oldCurve);
                return false;
            }
            else
            {
                return true;
            }
        }

        public void UpdateImplantSupportGuidingOutlines()
        {
            var objectManager = new CMFObjectManager(_director);

            if (!_postGuidingOutlineMap.Any())
            {
                ProPlanImportUtilities.RegenerateImplantSupportGuidingOutlines(objectManager);
                return;
            }

            var implantSupportGuidingBlocks = objectManager.GetAllBuildingBlocks(IBB.ImplantSupportGuidingOutline);

            foreach (var block in implantSupportGuidingBlocks)
            {
                if (!_toMaintainGuidingOutlineMap.Select(o => o.Id).Contains(block.Id))
                {
                    objectManager.DeleteObject(block.Id);
                }
            }

            var implantSupportGuidingOutlineHelper = new ImplantSupportGuidingOutlineHelper(_director);

            foreach (var implantSupportGuidingOutlineInfo in _postGuidingOutlineMap)
            {
                var touchingPart = objectManager.GetAllBuildingBlockRhinoObjectByMatchingName(IBB.ProPlanImport, $"^{implantSupportGuidingOutlineInfo.TouchingPartName}$").First();

                implantSupportGuidingOutlineHelper.AddImplantSupportGuidingOutlineBuildingBlocks(implantSupportGuidingOutlineInfo.Curve, touchingPart);
            }
        }

        public void CleanUp()
        {
            _preGuidingOutlineMap?.Clear();
            _postGuidingOutlineMap?.Clear();
            _toMaintainGuidingOutlineMap?.Clear();
        }

        private void SetPostGuidingOutlineInfo()
        {
            var implantSupportGuidingOutlineCreator = new ImplantSupportGuidingOutlineCreator(_director);

            implantSupportGuidingOutlineCreator.CreateImplantSupportGuidingOutlines(
                out var implantSupportGuidingOutlinesInfo, out var osteotomiesPreop);

            if (implantSupportGuidingOutlinesInfo.Count == 0 || osteotomiesPreop == null)
            {
                return;
            }

            var implantMarginInputGetterHelper = new ImplantMarginInputGetterHelper(_director);

            foreach (var implantSupportGuidingOutlineInfo in implantSupportGuidingOutlinesInfo)
            {
                var touchingOriginalPartRhObject = implantSupportGuidingOutlineInfo.Value;
                var transform = implantMarginInputGetterHelper.GetMarginTransform(touchingOriginalPartRhObject);

                var curve = implantSupportGuidingOutlineInfo.Key.DuplicateCurve();
                curve.Transform(transform);

                _postGuidingOutlineMap.Add(new CurveProperties(Guid.Empty, curve, touchingOriginalPartRhObject.Name));
            }

            if (_postGuidingOutlineMap.Any())
            {
                _director.OsteotomiesPreop = osteotomiesPreop;
            }
        }

        private bool ContainsCurve(List<CurveProperties> list, CurveProperties curveToFind, out CurveProperties foundCurve)
        {
            foundCurve = null;

            if (string.IsNullOrEmpty(curveToFind.TouchingPartName))
            {
                return false;
            }

            var tolerance = 0.001;
            var minDistanceToCompare = 100.0;
            var maxDistanceToCompare = 100.0;

            var length = curveToFind.Curve.GetLength();

            foreach (var curveProp in list)
            {
                if (curveProp.TouchingPartName != curveToFind.TouchingPartName)
                {
                    continue;
                }

                if (Math.Abs(curveProp.Curve.GetLength() - length) > tolerance)
                {
                    continue;
                }

                Curve.GetDistancesBetweenCurves(curveToFind.Curve, curveProp.Curve, tolerance,
                    out var maxDistance,
                    out var maxDistanceParameterA,
                    out var maxDistanceParameterB,
                    out var minDistance,
                    out var minDistanceParameterA,
                    out var minDistanceParameterB);

                if (minDistance <= minDistanceToCompare)
                {
                    var maxA = curveToFind.Curve.PointAt(maxDistanceParameterA);
                    var maxB = curveProp.Curve.PointAt(maxDistanceParameterB);

                    if (curveProp.Curve.ClosestPoint(maxA, out var tB) && curveToFind.Curve.ClosestPoint(maxB, out var tA))
                    {
                        var nearestToMaxA = curveProp.Curve.PointAt(tB);
                        var distanceForParameterA = (nearestToMaxA - maxA).Length;

                        var nearestToMaxB = curveToFind.Curve.PointAt(tA);
                        var distanceForParameterB = (nearestToMaxB - maxB).Length;

                        var distance = distanceForParameterA > distanceForParameterB ? distanceForParameterA : distanceForParameterB;

                        if (distance <= maxDistanceToCompare)
                        {
                            minDistanceToCompare = minDistance;
                            maxDistanceToCompare = distance;
                            foundCurve = curveProp;
                        }
                    }
                }
            }

            if (minDistanceToCompare < tolerance && maxDistanceToCompare < tolerance)
            {
                return true;
            }

            return false;
        }
    }
}
