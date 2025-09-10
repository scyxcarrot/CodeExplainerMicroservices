using IDS.CMF.CasePreferences;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMF.Operations
{
    public class ScrewCreator
    {
        private readonly CMFImplantDirector _director;
        public ScrewCreator(CMFImplantDirector director)
        {
            this._director = director;
        }

        public Screw CreateScrewObjectOnPastille(Point3d location, Vector3d direction, Dictionary<string, GeometryBase> screwAideDict, 
            double screwLength, string screwType, string barrelType, int screwIndex = -1)
        {
            var headPoint = location;
            var tipPoint = Point3d.Add(headPoint, Vector3d.Multiply(direction, screwLength));
            return new Screw(_director, headPoint, tipPoint, screwAideDict, screwIndex, screwType, barrelType);
        }

        public Screw CreateCalibratedScrewObjectOnPastille(Point3d location, Vector3d direction, Dictionary<string, GeometryBase> screwAideDict,
            double screwLength, double plateThickness, Mesh calibrationMesh, string screwType, string barrelType, int screwIndex)
        {
            var screw = CreateScrewObjectOnPastille(location, direction, screwAideDict, screwLength, screwType, barrelType, screwIndex);

            var screwCalibrator = new ScrewCalibrator(calibrationMesh);
            if (!screwCalibrator.LevelHeadOnTopOfMesh(screw, plateThickness, true))
            {
                return null;
            }

            return screwCalibrator.CalibratedScrew;
        }

        public bool CreateScrewBuildingBlock(DotPastille pastille, CasePreferenceDataModel casePref, Mesh constraintMesh)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            var objectManager = new CMFObjectManager(_director);
            var implantSupportManager = new ImplantSupportManager(objectManager);

            if (constraintMesh == null)
            {
                constraintMesh = implantSupportManager.GetImplantSupportMesh(casePref);
                implantSupportManager.ImplantSupportNullCheck(constraintMesh, casePref);
            }

            var screwAideDict = casePref.ScrewAideData.GenerateScrewAideDictionary();
            var calibratedScrew = CreateCalibratedScrewObjectOnPastille(
                RhinoPoint3dConverter.ToPoint3d(pastille.Location),
                -RhinoVector3dConverter.ToVector3d(pastille.Direction), screwAideDict,
                casePref.CasePrefData.ScrewLengthMm, casePref.CasePrefData.PlateThicknessMm, constraintMesh,
                casePref.CasePrefData.ScrewTypeValue, casePref.CasePrefData.BarrelTypeValue, -1);

            if (calibratedScrew != null)
            {
                var screwBb = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, casePref);
                var idsDocument = calibratedScrew.Director.IdsDocument;
                var parentGuid = pastille.Id;
                IdsDocumentUtilities.AddNewRhinoObjectBuildingBlock(
                    objectManager, idsDocument, screwBb, parentGuid, calibratedScrew);
            }

            ScrewPastilleManager.UpdateScrewDataInPastille(pastille, calibratedScrew);
            return true;
        }

        public Guid RegenerateScrewBuildingBlock(ScrewRegenerationInfo regenerationInfo, CasePreferenceDataModel casePref, Mesh constraintMesh)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            var objectManager = new CMFObjectManager(_director);
            var implantSupportManager = new ImplantSupportManager(objectManager);

            if (constraintMesh == null)
            {
                constraintMesh = implantSupportManager.GetImplantSupportMesh(casePref);
                implantSupportManager.ImplantSupportNullCheck(constraintMesh, casePref);
            }

            if (regenerationInfo.OldScrewId != Guid.Empty && 
                regenerationInfo.ScrewGroupIndex >= 0 && 
                regenerationInfo.ScrewGroupIndex < _director.ScrewGroups.Groups.Count)
            {
                _director.ScrewGroups.Groups[regenerationInfo.ScrewGroupIndex].ScrewGuids.Remove(regenerationInfo.OldScrewId);
            }

            var screwAideDict = casePref.ScrewAideData.GenerateScrewAideDictionary();
            var calibratedScrew = CreateCalibratedScrewObjectOnPastille(
                RhinoPoint3dConverter.ToPoint3d(regenerationInfo.Pastille.Location),
                -RhinoVector3dConverter.ToVector3d(regenerationInfo.Pastille.Direction), screwAideDict,
                casePref.CasePrefData.ScrewLengthMm, casePref.CasePrefData.PlateThicknessMm, constraintMesh,
                casePref.CasePrefData.ScrewTypeValue, casePref.CasePrefData.BarrelTypeValue, regenerationInfo.ScrewIndex);

            if (calibratedScrew != null)
            {
                var screwBb = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, casePref);
                var idsDocument = calibratedScrew.Director.IdsDocument;
                var parentGuid = regenerationInfo.Pastille.Id;
                var newScrewId = IdsDocumentUtilities.AddNewRhinoObjectBuildingBlock(
                    objectManager, idsDocument, screwBb, parentGuid, calibratedScrew);

                if (regenerationInfo.ScrewGroupIndex >= 0 && regenerationInfo.ScrewGroupIndex < _director.ScrewGroups.Groups.Count)
                {
                    _director.ScrewGroups.Groups[regenerationInfo.ScrewGroupIndex].ScrewGuids.Add(newScrewId);
                }

                ScrewPastilleManager.UpdateScrewDataInPastille(regenerationInfo.Pastille, calibratedScrew);
                return newScrewId;
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Create screws and calibrate it to the mesh.
        /// Specify a mesh to constraintMesh that the screw will be calibrated to.
        /// If it is null, then it will calibrate on support if it exist.
        /// </summary>
        /// <param name="skipIfScrewAlreadyExist">Skip creating screws if it was already created</param>
        /// <param name="casePrefData">CasePreferenceDataModel</param>
        /// <param name="constraintMesh">Constraint mesh for the screw to be calibrated on</param>
        /// <returns>bool</returns>
        public bool CreateAllScrewBuildingBlock(bool skipIfScrewAlreadyExist, CasePreferenceDataModel casePrefData, Mesh constraintMesh = null)
        {
            var implant = casePrefData.ImplantDataModel;
            var implant_point_list = implant.DotList;

            foreach (var connection_pt in implant_point_list)
            {
                if (connection_pt is DotPastille pastille)
                {
                    if (skipIfScrewAlreadyExist && pastille.Screw != null)
                    {
                        continue;
                    }

                    if (!CreateScrewBuildingBlock(pastille, casePrefData, constraintMesh))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
