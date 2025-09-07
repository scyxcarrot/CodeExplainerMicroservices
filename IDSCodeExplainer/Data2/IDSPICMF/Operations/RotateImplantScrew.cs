using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.Graph;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Quality;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using Rhino;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.PICMF.Operations
{
    public class RotateImplantScrew : RotateScrewBase
    {
        private ScrewInfoDisplayConduit _screwInfoDisplayConduit;
        private readonly CMFScrewAnalysis _screwAnalysis;

        public RotateImplantScrew(Screw screw, Point3d centerOfRotation, Vector3d referenceDirection) : this(screw,
            centerOfRotation, referenceDirection, false)
        {

        }

        public RotateImplantScrew(Screw screw, Point3d centerOfRotation, Vector3d referenceDirection, bool minimalPreviews) : base(screw,
            centerOfRotation, referenceDirection, minimalPreviews)
        {
            movingPoint = fixedPoint + referenceDirection * _lengthCompensated;
            var screwToPreview = new Screw(screw.Director, screw.HeadPoint, movingPoint, screw.ScrewAideDictionary,
                screw.Index, screw.ScrewType, screw.BarrelType);
            screwPreview = (Brep)screwToPreview.Geometry;

            var gauges = ScrewGaugeUtilities.CreateScrewGauges(screwToPreview, screw.ScrewType);
            _gaugeConduit = new ScrewGaugeConduit(gauges);

            _screwAnalysis = new CMFScrewAnalysis(director);
        }

        protected override void SetupBeforeRotate(GetPoint getPoint)
        {
            if (!_minimalPreviews)
            {
                _screwInfoDisplayConduit = new ScrewInfoDisplayConduit()
                {
                    Enabled = true
                };

                var originalAngle = _screwAnalysis.CalculateScrewAngle(referenceScrew, referenceDirection);
                _screwInfoDisplayConduit.OriginalScrewAngle = originalAngle;
            }

            base.SetupBeforeRotate(getPoint);// base.SetupBeforeRotate will redraw the conduit
        }

        protected override void TeardownAfterRotated(GetPoint getPoint)
        {
            base.TeardownAfterRotated(getPoint);

            if (!_minimalPreviews)
            {
                _screwInfoDisplayConduit.Enabled = false;
            }
        }

        protected override bool UpdateScrew(Point3d toPoint)
        {
            var updated = false;

            var pointOnHemisphere = GetPointOnConstraint(toPoint,
                RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation,
                RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection);
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
                    referenceScrew.ScrewAideDictionary, referenceScrew.Index, referenceScrew.ScrewType,
                    referenceScrew.BarrelType);

#if (INTERNAL)
                if (CMFImplantDirector.IsDebugMode)
                {
                    InternalUtilities.AddObject(referenceScrew.BrepGeometry, "Testing::ReferenceScrew");
                    InternalUtilities.AddObject(referenceScrew.GetScrewContainer(), "Testing::ReferenceScrewContainer");
                    InternalUtilities.AddObject(screw.BrepGeometry, "Testing::RotatedScrew");
                    InternalUtilities.AddObject(screw.GetScrewContainer(), "Testing::RotatedScrewContainer");
                }
#endif
                var objectManager = new CMFObjectManager(director);
                var casePreferenceData = objectManager.GetCasePreference(referenceScrew);
                var screwCalibrator = new ScrewCalibrator(ConstraintMesh);
                if (!screwCalibrator.LevelHeadOnTopOfMesh(screw, casePreferenceData.CasePrefData.PlateThicknessMm,
                    true))
                {
                    return false;
                }

                var calibratedScrew = screwCalibrator.CalibratedScrew;

#if (INTERNAL)
                if (CMFImplantDirector.IsDebugMode)
                {
                    InternalUtilities.AddObject(calibratedScrew.BrepGeometry, "Testing::CalibratedRotatedScrew");
                    InternalUtilities.AddObject(calibratedScrew.GetScrewContainer(),
                        "Testing::CalibratedRotatedScrewContainer");
                }
#endif

                var pastilleList = casePreferenceData.ImplantDataModel.DotList.Where(d => d is DotPastille)
                    .Cast<DotPastille>();
                var pastille = pastilleList.First(p => p.Screw.Id == referenceScrew.Id);
                var screwData = new ScrewData
                {
                    Id = pastille.Screw.Id
                };

                var screwManager = new ScrewManager(director);
                screwManager.ReplaceExistingScrewInDocument(calibratedScrew, ref referenceScrew, casePreferenceData,
                    true);

                RegisteredBarrelUtilities.NotifyBuildingBlockHasChanged(director, calibratedScrew.Id);

                pastille.Screw = screwData;

                // Tech Debt 1307992
                // TODO: We should make the ImplantDataModel a singleton and handle any invalidation based on the changes to the IDot/IConnection list
                director.ImplantManager.UpdateConnectionBuildingBlock(casePreferenceData, OldImplantDataModel.ConnectionList, true);

                // ORDERS MATTER; LANDMARK IS A CHILD OF SCREW; LANDMARK IS RECREATED WHEN SCREW IS ROTATED
                IdsDocumentUtilities.DeleteChildrenOnly(
                    director.IdsDocument, calibratedScrew.Id);
                director.ImplantManager.InvalidateLandmarkBuildingBlock(casePreferenceData);

                //leveling and update was successful
                updated = true;
            }

            director.Document.Views.Redraw();

            return updated;
        }

        protected override void DynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            base.DynamicDraw(sender, e);

            if (!_minimalPreviews)
            {
                var dummyScrew = new Screw(director, _centerOfRotation, movingPoint, referenceScrew.ScrewAideDictionary,
                    referenceScrew.Index,
                    referenceScrew.ScrewType, referenceScrew.BarrelType);

                var currAngle = _screwAnalysis.CalculateScrewAngle(dummyScrew, referenceDirection);

                var bubbleDir = _centerOfRotation - movingPoint;
                bubbleDir.Unitize();

                var rightSideCameraVector = VectorUtilities.GetCameraRightSideVector();

                _screwInfoDisplayConduit.Location = _centerOfRotation + rightSideCameraVector * 10;
                _screwInfoDisplayConduit.CurrentScrewAngle = currAngle;
            }
        }
    }
}
