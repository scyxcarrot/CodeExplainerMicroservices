using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.ImplantBuildingBlocks
{
    public class ScrewPastilleManager
    {
        public void UpdatePastille(Point3d newLocation, IScrew screwData, DotPastille pastille, Mesh constraintMesh)
        {
            var averageNormal = VectorUtilities.FindAverageNormal(constraintMesh, newLocation, ScrewAngulationConstants.AverageNormalRadiusPastille);

            pastille.Location = RhinoPoint3dConverter.ToIPoint3D(newLocation);
            pastille.Direction = RhinoVector3dConverter.ToIVector3D(averageNormal);

            pastille.Screw = screwData;
        }

        public static void UpdateScrewsAfterMovePastilles(CMFImplantDirector director, List<IConnection> newConns, Dictionary<Guid, (DotPastille Pastille, int ScrewIndex, int ScrewGroupIndex)> differencePastilleToScrewInfoMap, CasePreferenceDataModel casePrefData, Mesh calibrationMesh)
        {
            if (!differencePastilleToScrewInfoMap.Any())
            {
                return;
            }

            foreach (var pastilleInfo in differencePastilleToScrewInfoMap.Values)
            {
                var screwCreator = new ScrewCreator(director);
                var regenerationInfo = new ScrewRegenerationInfo(pastilleInfo.Pastille, pastilleInfo.ScrewIndex, pastilleInfo.ScrewGroupIndex);
                var success = screwCreator.RegenerateScrewBuildingBlock(regenerationInfo, casePrefData, calibrationMesh);

                if (success == Guid.Empty)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Failed to regenerate moved screw on CaseID: {casePrefData.CaseGuid}");
                }
            }
        }

        public void UpdateScrewAfterMovePastille(CMFObjectManager objectManager, Mesh constraintMesh,
            DotPastille newPastille, Screw screw, double plateThickness, ICaseData casePreference)
        {
            var pastillePt = RhinoPoint3dConverter.ToPoint3d(newPastille.Location);

            var currHeadPt = screw.HeadPoint;
            var currTipPt = screw.TipPoint;
            var testLine = new Line(currHeadPt, currTipPt);

            //check whether the pastille's location is within the given tolerance from a line generated with screw's HeadPoint and TipPoint
            //this will determine whether the screw needs to be updated to the latest location or not
            if (testLine.DistanceTo(pastillePt, true) > 0.001)
            {
                //the orientation should follow the surface normal of the bone (pastille's direction)
                var headPoint = RhinoPoint3dConverter.ToPoint3d(newPastille.Location);
                var tipPoint = Point3d.Add(headPoint, Vector3d.Multiply(-RhinoVector3dConverter.ToVector3d(newPastille.Direction), (screw.HeadPoint - screw.TipPoint).Length));
                var newScrew = new Screw(screw.Director, headPoint, tipPoint, screw.ScrewAideDictionary, screw.Index, screw.ScrewType, screw.BarrelType);

                var screwCalibrator = new ScrewCalibrator(constraintMesh);
                if (!screwCalibrator.LevelHeadOnTopOfMesh(newScrew, plateThickness, true))
                {
                    return;
                }

                var calibratedScrew = screwCalibrator.CalibratedScrew;
                var screwData = new ScrewData
                {
                    Id = newPastille.Screw.Id
                };
                var theScrew = (Screw)objectManager.GetDirector().Document.Objects.Find(newPastille.Screw.Id);

                RegisteredBarrelUtilities.NotifyBuildingBlockHasChanged(objectManager.GetDirector(), theScrew.Id);

                var screwManager = new ScrewManager(objectManager.GetDirector());
                screwManager.ReplaceExistingScrewInDocument(calibratedScrew, ref theScrew, casePreference, true);

                newPastille.Screw = screwData;
                newPastille.CreationAlgoMethod = DotPastille.CreationAlgoMethods[0];
                //added for targeted invalidation
                IdsDocumentUtilities.DeleteChildrenOnly(objectManager.GetDirector().IdsDocument, newPastille.Screw.Id);
            }
        }

        public static void UpdateScrewDataInPastille(DotPastille pastille, Screw screw, bool updateScrewIdUsingInputScrew = true)
        {
            if (pastille == null || screw == null)
            {
                return;
            }

            pastille.Screw = new ScrewData
            {
                Id = updateScrewIdUsingInputScrew ? screw.Id : pastille.Screw.Id
            };
        }
    }
}
