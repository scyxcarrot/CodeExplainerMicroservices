using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Utilities;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Operations
{
    public class TranslateImplantScrew : TranslateScrewBase
    {
        // It consist full detail of support mesh
        public RhinoObject ActualSupportRhObject { get; set; }

        public ImplantDataModel OldImplantDataModel { get; set; }

        private Point3d _pastilleLocation = Point3d.Unset;

        public TranslateImplantScrew(Screw screw) : base(screw)
        {
            var objectManager = new CMFObjectManager(director);
            _casePreferenceDataModel = objectManager.GetCasePreference(referenceScrew);
        }

        protected override bool UpdateBuildingBlock(Screw calibratedScrew)
        {
            var updated = false;

            var pastilleList = _casePreferenceDataModel.ImplantDataModel.DotList.Where(d => d is DotPastille).Cast<DotPastille>();
            var pastille = pastilleList.First(p => p.Screw.Id == referenceScrew.Id);

            var objectManager = new CMFObjectManager(director);
            
            var screwPastilleManager = new ScrewPastilleManager();
            var screwData = new ScrewData
            {
                Id = pastille.Screw.Id
            };

            if (_pastilleLocation.IsValid)
            {
                var screwManager = new ScrewManager(director);
                screwManager.ReplaceExistingScrewInDocument(calibratedScrew, ref referenceScrew, _casePreferenceDataModel, true);

                var rhSupport = ActualSupportRhObject;
                if (rhSupport != null)
                {
                    var actualSupportMeshRoI = ImplantCreationUtilities.GetImplantRoIVolumeWithoutCheck(objectManager, _casePreferenceDataModel,
                        ref rhSupport);
                    screwPastilleManager.UpdatePastille(_pastilleLocation, screwData, pastille, actualSupportMeshRoI);
                }
                else
                {
                    screwPastilleManager.UpdatePastille(_pastilleLocation, screwData, pastille, LowLoDSupportMesh);
                }
                referenceScrew = calibratedScrew;

                RegisteredBarrelUtilities.NotifyBuildingBlockHasChanged(director, calibratedScrew.Id);

                // Tech Debt 1307992
                // TODO: We should make the ImplantDataModel a singleton and handle any invalidation based on the changes to the IDot/IConnection list
                director.ImplantManager.UpdateConnectionBuildingBlock(_casePreferenceDataModel, OldImplantDataModel.ConnectionList, true);
                
                // ORDERS MATTER; LANDMARK IS A CHILD OF SCREW; LANDMARK IS RECREATED WHEN SCREW IS TRANSLATED
                IdsDocumentUtilities.DeleteChildrenOnly(
                    director.IdsDocument, calibratedScrew.Id);
                director.ImplantManager.InvalidateLandmarkBuildingBlock(_casePreferenceDataModel);

                //leveling and update was successful
                updated = true;
            }

            return updated;
        }

        protected override Screw CalibratePreviewScrew(Screw originScrew)
        {
            var calibrator = new ScrewCalibrator(LowLoDSupportMesh);
            //use fast calibration for preview
            if (calibrator.FastLevelHeadOnTopOfMesh(originScrew, _casePreferenceDataModel.CasePrefData.PlateThicknessMm, true))
            {
                return calibrator.CalibratedScrew;
            }
            return null;
        }

        protected override Point3d GetFinalizedPickedPoint(Point3d currentPoint, Point3d cameraLocation, Vector3d cameraDirection)
        {
            var objectManager = new CMFObjectManager(director);

            var pointOnLowLoD = GetPointOnConstraint(currentPoint, cameraLocation, cameraDirection, LowLoDSupportMesh);
            if (!pointOnLowLoD.IsValid)
            {
                return Point3d.Unset;
            }

            var rhSupport = ActualSupportRhObject;
            if (rhSupport != null)
            {
                var lowLoDSupportSurface = ImplantCreationUtilities.GetImplantRoISurfaceWithoutCheck(objectManager,
                    _casePreferenceDataModel, ref rhSupport);

                _needDoubleRecalibration = ImplantCreationUtilities.IsNeedCreateNewImplantRoIMetadata(pointOnLowLoD,
                    _casePreferenceDataModel.CasePrefData.PastilleDiameter, lowLoDSupportSurface);

                Mesh actualSupportMeshRoI = null;
                if (_needDoubleRecalibration) //Happens if screw is out of the RoI/intersecting with RoI inner curve.
                {
                    var additionalConnections = CreateConnectionForecast(pointOnLowLoD, LowLoDSupportMesh);
                    actualSupportMeshRoI = ImplantCreationUtilities.GetImplantRoIVolume(objectManager, _casePreferenceDataModel,
                        ref rhSupport, null, additionalConnections);
                }
                else
                {
                    actualSupportMeshRoI = ImplantCreationUtilities.GetImplantRoIVolumeWithoutCheck(objectManager,
                        _casePreferenceDataModel, ref rhSupport);
                }

                _pastilleLocation = GetPointOnConstraint(currentPoint, cameraLocation, cameraDirection, actualSupportMeshRoI);
            }
            else
            {
                _pastilleLocation = GetPointOnConstraint(currentPoint, cameraLocation, cameraDirection, LowLoDSupportMesh);
            }

            return _pastilleLocation;
        }

        protected override Screw CalibrateActualScrew(Screw originScrew)
        {
            var objectManager = new CMFObjectManager(director);
            var constraintMesh = LowLoDSupportMesh;
            var rhSupport = ActualSupportRhObject;
            if (rhSupport != null)
            {
                var actualSupportMesh = ImplantCreationUtilities.GetImplantRoIVolumeWithoutCheck(objectManager, _casePreferenceDataModel,
                    ref rhSupport);
                constraintMesh = actualSupportMesh;
            }
            var calibrator = new ScrewCalibrator(constraintMesh);
            if (calibrator.LevelHeadOnTopOfMesh(originScrew, _casePreferenceDataModel.CasePrefData.PlateThicknessMm, true))
            {
                return calibrator.CalibratedScrew;
            }
            return null;
        }

        private List<IConnection> CreateConnectionForecast(Point3d newAdditionalPt, Mesh lowLoDSupportMesh)
        {
            var additionalPt = lowLoDSupportMesh.ClosestPoint(newAdditionalPt);

            var tmpDot = new DotControlPoint {Location = RhinoPoint3dConverter.ToIPoint3D(additionalPt) };

            var refScrewDot = ScrewUtilities.FindDotTheScrewBelongsTo(referenceScrew, _casePreferenceDataModel.ImplantDataModel.DotList);
            var neighboringConnections = ConnectionUtilities.
                FindConnectionsTheDotsBelongsTo(_casePreferenceDataModel.ImplantDataModel.ConnectionList, refScrewDot);

            var additionalConnections = new List<IConnection>();
            neighboringConnections.ForEach(conn =>
            {
                var tmpConn = (IConnection)conn.Clone();
                if (conn.A.Equals(refScrewDot))
                {
                    tmpConn.A = tmpDot;
                }
                else
                {
                    tmpConn.B = tmpDot;
                }

                additionalConnections.Add(tmpConn);
            });

            return additionalConnections;
        }
    }
}