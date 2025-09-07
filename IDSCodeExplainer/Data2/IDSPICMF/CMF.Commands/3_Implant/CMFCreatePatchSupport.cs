#if (STAGING)
using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.V2.DataModels;
using IDS.PICMF.Commands;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Style = Rhino.Commands.Style;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("3BD528FC-01E1-4205-9614-540F93362728")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.PlanningImplant)]
    public class CMFCreatePatchSupport : CMFCreateSupportBase
    {
        public CMFCreatePatchSupport()
        {
            TheCommand = this;
        }

        public static CMFCreatePatchSupport TheCommand { get; private set; }

        public override string EnglishName => "CMFCreatePatchSupport";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var implantSupportCreator = new ImplantSupportCreator();
            var biggerRoI = implantSupportCreator.CreateBiggerSupportRoI(director, out _);

            var implantComponent = new ImplantCaseComponent();
            var objectManager = new CMFObjectManager(director);
            var idsDocument = director.IdsDocument;

            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                if (!casePreferenceDataModel.ImplantDataModel.ConnectionList.Any())
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default,
                        $"Skipping {casePreferenceDataModel.CaseName} " +
                        $"because it has no design.");
                    continue;
                }

                var dataModels = implantSupportCreator.GeneratePatchSupports(objectManager, casePreferenceDataModel, biggerRoI,
                    out _, out _);

                // building the tree
                var patchSupportBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PatchSupport, casePreferenceDataModel);
                var connectionBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Connection, casePreferenceDataModel);
                var createdPatchSupport = new Dictionary<Curve, RhinoObject>();
                foreach (var dataModelPair in dataModels)
                {
                    var connectionCurve = dataModelPair.Key;
                    var patchSupport = dataModelPair.Value.FixedFinalResult;
                    var smallerPatchSupport = dataModelPair.Value.SmallerRoI;

                    var connections = DataModelUtilities.GetConnections(connectionCurve, casePreferenceDataModel.ImplantDataModel.ConnectionList);

                    //add patch support
                    var patchSupportRhinoMesh =
                        RhinoMeshConverter.ToRhinoMesh(patchSupport);
                    var patchSupportId = IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                        objectManager, 
                        idsDocument, 
                        patchSupportBuildingBlock, 
                        connections.Select(conn => conn.Id).ToList(), 
                        patchSupportRhinoMesh);
                    var rhinoObject = director.Document.Objects.Find(patchSupportId);

                    var smallerPatchSupportRhinoMesh = 
                        RhinoMeshConverter.ToRhinoMesh(smallerPatchSupport);
                    rhinoObject.Attributes.UserDictionary.Set(
                        PatchSupportKeys.SmallerRoIKey, smallerPatchSupportRhinoMesh);
                    rhinoObject.Attributes.UserDictionary.Set(PatchSupportKeys.PatchSupportCurveKey, connectionCurve);
                    createdPatchSupport.Add(connectionCurve, rhinoObject);

                    // add back ConnectionCurve
                    var connectionId = objectManager.AddNewBuildingBlock(connectionBuildingBlock, connectionCurve);
                    if (connectionId != Guid.Empty)
                    {
                        var parentGuids = connections.Select(conn => conn.Id).ToList();
                        parentGuids.Add(patchSupportId);

                        var objectValueData = new ObjectValueData(connectionId, parentGuids, new ObjectValue
                        {
                            Attributes = new Dictionary<string, object>
                            {
                                { "IBB", IBB.Connection.ToString() }
                            }
                        });

                        idsDocument.Create(objectValueData);
                    }
                }

                // list to prevent reprocessing of shared IDots multiple times
                var dotIds = new List<Guid>();
                var connectionCurves = 
                    dataModels.Select(dataModelPair => dataModelPair.Key);
                foreach (var connectionCurve in connectionCurves)
                {
                    var connections = DataModelUtilities.GetConnections(connectionCurve, casePreferenceDataModel.ImplantDataModel.ConnectionList);

                    foreach (var connection in connections)
                    {
                        var dotA = connection.A;
                        if (!dotIds.Contains(dotA.Id) && dotA is DotPastille dotPastilleA)
                        {
                            RecalibratePastilleScrewWithPatchSupport(
                                director, 
                                casePreferenceDataModel,
                                createdPatchSupport[connectionCurve], 
                                dotPastilleA);
                            dotIds.Add(dotA.Id);
                        }

                        var dotB = connection.B;
                        if (!dotIds.Contains(dotB.Id) && dotB is DotPastille dotPastilleB)
                        {
                            RecalibratePastilleScrewWithPatchSupport(
                                director, 
                                casePreferenceDataModel,
                                createdPatchSupport[connectionCurve], 
                                dotPastilleB);
                            dotIds.Add(dotB.Id);
                        }
                    }
                }

                director.ImplantManager.InvalidatePlanningImplantBuildingBlock(casePreferenceDataModel);

                // Delete Implant Support if exist
                var implantSupportBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.ImplantSupport, casePreferenceDataModel);
                if (objectManager.HasBuildingBlock(implantSupportBuildingBlock))
                {
                    objectManager.DeleteObject(objectManager.GetBuildingBlockId(implantSupportBuildingBlock));
                }
            }

            return Result.Success;
        }

        private void RecalibratePastilleScrewWithPatchSupport(CMFImplantDirector director, CasePreferenceDataModel casePreferenceDataModel, RhinoObject patchSupportObject, 
            DotPastille dotPastille)
        {
            // Get reference to the original screw
            var originalScrew = director.Document.Objects.Find(dotPastille.Screw.Id) as Screw;
            int originalIndex = originalScrew.Index;

            // Delete the screw
            director.IdsDocument.Delete(dotPastille.Screw.Id);

            // Create a new screw
            var screwCreator = new ScrewCreator(director);
            var newScrew = screwCreator.CreateCalibratedScrewObjectOnPastille(
                RhinoPoint3dConverter.ToPoint3d(dotPastille.Location),
                -RhinoVector3dConverter.ToVector3d(dotPastille.Direction),
                casePreferenceDataModel.ScrewAideData.GenerateScrewAideDictionary(),
                casePreferenceDataModel.CasePrefData.ScrewLengthMm,
                casePreferenceDataModel.CasePrefData.PlateThicknessMm,
                (Mesh)patchSupportObject.Geometry,
                casePreferenceDataModel.CasePrefData.ScrewTypeValue,
                casePreferenceDataModel.CasePrefData.BarrelTypeValue, originalIndex);

            if (newScrew == null)
            {
                return;
            }

            var implantCaseComponent = new ImplantCaseComponent();
            var screwBb = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, 
                casePreferenceDataModel);
            var idsDocument = director.IdsDocument;

            var parentGuid = new List<Guid>() { dotPastille.Id, patchSupportObject.Id };
            var objectManager = new CMFObjectManager(director);
            IdsDocumentUtilities.AddNewRhinoObjectBuildingBlock(
                objectManager, idsDocument, screwBb, parentGuid, newScrew);

            // register the new screw back to pastille
            var screwData = new ScrewData
            {
                Id = newScrew.Id
            };
            dotPastille.Screw = screwData;
            dotPastille.CreationAlgoMethod = DotPastille.CreationAlgoMethods[0];

            // create landmark back since deleted the screw
            if (dotPastille.Landmark != null)
            {
                director.ImplantManager.AddLandmarkToDocument(dotPastille.Landmark);
            }

            RecreateScrewBarrels(director, casePreferenceDataModel);
        }

        private static void RecreateScrewBarrels(
            CMFImplantDirector director, 
            CasePreferenceDataModel casePreferenceDataModel)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            var implantCaseBarrels = implantCaseComponent.GetImplantBuildingBlock(
                IBB.RegisteredBarrel, casePreferenceDataModel);
            var implantCaseScrews = implantCaseComponent.GetImplantBuildingBlock(
                IBB.Screw, casePreferenceDataModel);

            var objectManager = new CMFObjectManager(director);
            var registeredCaseBarrels =
                objectManager.GetAllBuildingBlocks(implantCaseBarrels);
            var implantCaseScrewObjs =
                objectManager.GetAllBuildingBlocks(implantCaseScrews);

            // if user just got into the implant phase without guide phase,
            // registeredBarrels = 0
            if (!registeredCaseBarrels.Any() || 
                registeredCaseBarrels.Count() == implantCaseScrewObjs.Count())
            {
                return;
            }

            Mesh guideSupport = null;
            var hasGuideSupport = objectManager.HasBuildingBlock(IBB.GuideSupport);
            if (hasGuideSupport)
            {
                guideSupport = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
            }
            using (var barrelRegistration = new CMFBarrelRegistrator(director))
            {
                barrelRegistration.RegisterAllGuideRegisteredBarrel(guideSupport, out _);
            }
        }
    }
}
#endif