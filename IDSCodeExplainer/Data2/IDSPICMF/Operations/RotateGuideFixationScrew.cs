using IDS.CMF;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using Rhino;
using Rhino.Geometry;
using System.Linq;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.PICMF.Operations
{
    public class RotateGuideFixationScrew : RotateScrewBase
    {
        public bool NeedToClearUndoRedoRecords { get; private set; }

        public RotateGuideFixationScrew(Screw screw, Point3d centerOfRotation, Vector3d referenceDirection) : base(screw, centerOfRotation, referenceDirection)
        {   
            var gauges = ScrewGaugeUtilities.CreateScrewGauges(screw, screw.ScrewType);
            _gaugeConduit = new ScrewGaugeConduit(gauges);
            NeedToClearUndoRedoRecords = false;
        }

        protected override bool UpdateScrew(Point3d toPoint)
        {
            var updated = false;

            var pointOnHemisphere = GetPointOnConstraint(toPoint, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection);
            if (pointOnHemisphere != Point3d.Unset)
            {
                var direction = pointOnHemisphere - fixedPoint;
                if (!direction.IsUnitVector)
                {
                    direction.Unitize();
                }

                pointOnHemisphere = fixedPoint + direction * length;

                // Check if leveling can be done before replacing the old screw by the updated screw
                var screw = new Screw(referenceScrew.Director,
                    fixedPoint,
                    pointOnHemisphere,
                    referenceScrew.ScrewAideDictionary, referenceScrew.Index, referenceScrew.ScrewType);

                var guidesAndScrewsItSharedWith = referenceScrew.GetGuideAndScrewItSharedWith();

                var objManager = new CMFObjectManager(director);
                var refGPref = objManager.GetGuidePreference(referenceScrew);

#if (INTERNAL)
                if (CMFImplantDirector.IsDebugMode)
                { 
                    InternalUtilities.AddObject(referenceScrew.BrepGeometry, "Testing::ReferenceGuideFixationScrew");
                    InternalUtilities.AddObject(referenceScrew.GetScrewContainer(), "Testing::ReferenceGuideFixationScrewContainer");
                    InternalUtilities.AddObject(screw.BrepGeometry, "Testing::RotatedGuideFixationScrew");
                    InternalUtilities.AddObject(screw.GetScrewContainer(), "Testing::RotatedGuideFixationScrewContainer");
                }
#endif

                var calibrator = new GuideFixationScrewCalibrator();
                var calibratedScrew = calibrator.LevelScrew(screw, ConstraintMesh, referenceScrew);
                if (calibratedScrew == null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Failed to calibrate");
                    return false;
                }

#if (INTERNAL)
                if (CMFImplantDirector.IsDebugMode)
                {
                    InternalUtilities.AddObject(calibratedScrew.BrepGeometry, "Testing::CalibratedRotatedGuideFixationScrew");
                    InternalUtilities.AddObject(calibratedScrew.GetScrewContainer(), "Testing::CalibratedRotatedGuideFixationScrewContainer");
                }
#endif

                var screwManager = new ScrewManager(director);
                screwManager.UpdateGuideFixationScrewInDocument(calibratedScrew, ref referenceScrew);
                calibratedScrew.ShareWithScrews(guidesAndScrewsItSharedWith.Select(x => x.Value));


                guidesAndScrewsItSharedWith.ForEach(cp =>
                {
                    var relatedScrew = cp.Value;
                    var duplicate = new Screw(director, calibratedScrew.HeadPoint,
                        calibratedScrew.TipPoint, refGPref.GuideScrewAideData.GenerateScrewAideDictionary(), relatedScrew.Index,
                        refGPref.GuidePrefData.GuideScrewTypeValue);

                    screwManager.UpdateGuideFixationScrewInDocument(duplicate, ref relatedScrew);

                    var sharedWithScrews = calibratedScrew.GetScrewItSharedWith();
                    duplicate.ShareWithScrews(sharedWithScrews);
                    duplicate.ShareWithScrew(calibratedScrew);

                    NeedToClearUndoRedoRecords = true;
                });

                updated = true;
            }

            director.Document.Views.Redraw();

            return updated;
        }
    }
}
