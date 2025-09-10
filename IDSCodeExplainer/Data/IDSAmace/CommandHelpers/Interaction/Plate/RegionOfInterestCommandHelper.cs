using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using View = IDS.Amace.Visualization.View;

namespace IDS.Amace.CommandHelpers
{
    public class RegionOfInterestCommandHelper
    {
        private const string KeyName = "ContourPlane";

        private readonly AmaceObjectManager _objectManager;

        public RegionOfInterestCommandHelper(AmaceObjectManager objectManager)
        {
            _objectManager = objectManager;
        }

        public void AddRoiContour(Curve contour, Plane contourPlane)
        {
            var newGuid = _objectManager.AddNewBuildingBlock(IBB.ROIContour, contour);
            SetUserData(newGuid, contourPlane);
        }

        public void SetRoiContour(Curve contour, Plane contourPlane, Guid oldId)
        {
            var updatedGuid = _objectManager.SetBuildingBlock(IBB.ROIContour, contour, oldId);
            SetUserData(updatedGuid, contourPlane);
        }
        
        public Plane GetContourPlaneBasedOnRoiCurve(Guid roiCurveId, Plane contourPlane)
        {
            //sync back in case the contour plane and roi curve is out of sync due to undo/redo
            var rhinoObj = _objectManager.GetAllBuildingBlocks(IBB.ROIContour).First(c => c.Id == roiCurveId);
            var roiCurve = (Curve) rhinoObj.Geometry;
            if (CurveUtilities.IsPlanarCurveParallelTo(roiCurve, contourPlane.Normal)) return contourPlane;

            object plane;
            rhinoObj.Attributes.UserDictionary.TryGetValue(KeyName, out plane);
            if (plane is Plane)
            {
                return (Plane)plane;
            }

            return contourPlane;
        }

        public Plane GetContourPlaneBasedOnAcetabularPlane(RhinoDoc doc, Point3d origin)
        {
            return View.SetView(doc, origin, CameraView.Acetabular, false);
        }

        public List<Curve> GetRoiContourBasedOnSkirtBoneCurve(Plane planeForContour)
        {
            var skirtBoneCurves = _objectManager.GetAllBuildingBlocks(IBB.SkirtBoneCurve).ToList().Select(c => (Curve)c.Geometry).ToList();
            var curvesProjectedOnPlane = skirtBoneCurves.Select(c => CurveUtilities.ProjectContourToPlane(planeForContour, c)).ToList();
            return curvesProjectedOnPlane;
        }

        private void SetUserData(Guid roiCurveId, Plane contourPlane)
        {
            var rhinoObj = _objectManager.GetAllBuildingBlocks(IBB.ROIContour).First(c => c.Id == roiCurveId);
            rhinoObj.Attributes.UserDictionary.Set(KeyName, contourPlane);
        }
    }
}