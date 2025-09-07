using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.Factory;
using IDS.CMF.Operations;
using IDS.CMF.Preferences;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.TreeDb.Model;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.ImplantBuildingBlocks
{
    public class ImplantManager
    {
        private readonly CMFObjectManager objectManager;
        private readonly PlanningImplantBrepFactory _planningImplantBrepFactory;
        private readonly ImplantCaseComponent implantComponent;
        private readonly LandmarkBrepFactory landmarkBrepFactory;

        public ImplantManager(CMFObjectManager objManager)
        {
            objectManager = objManager;
            _planningImplantBrepFactory = new PlanningImplantBrepFactory();
            implantComponent = new ImplantCaseComponent();

            var parameters = CMFPreferences.GetActualImplantParameters();
            landmarkBrepFactory = new LandmarkBrepFactory(parameters.LandmarkImplantParams);

            SupportRoICreationInformation = new ImplantSupportRoICreationInformation();
        }

        public void AddPlanningImplantBuildingBlock(CasePreferenceDataModel data)
        {
            var implant = _planningImplantBrepFactory.CreateImplant(data.ImplantDataModel);

            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PlanningImplant, data);

            objectManager.AddNewBuildingBlock(buildingBlock, implant);
        }

        public void AddAllConnectionsBuildingBlock(CasePreferenceDataModel data)
        {
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Connection, data);

            // It is possible to not have implant design if user wants to design the guide directly using info from case preference panel.  
            if (data.ImplantDataModel == null)
            {
                return;
            }

            var idsDocument = objectManager.GetDirector().IdsDocument;
            var implantConnectionCurves = ImplantCreationUtilities.CreateImplantConnectionCurves(data.ImplantDataModel.ConnectionList, linkIConnectionToRhinoCurve: true);
            implantConnectionCurves.ForEach(curve =>
            {
                AddConnectionsBuildingBlock(idsDocument, buildingBlock, curve);
            });
        }

        public void AddConnectionsBuildingBlock(IDSDocument document, ExtendedImplantBuildingBlock buildingBlock, Curve curve)
        {
            var iConnectionIds = new List<Guid>();
            if (curve.UserDictionary.ContainsKey(AttributeKeys.KeyIConnections))
            {
                var dictionary = (ArchivableDictionary)curve.UserDictionary[AttributeKeys.KeyIConnections];

                foreach (var d in dictionary)
                {
                    var iConnection = SerializationFactory.DeSerializeIConnection((ArchivableDictionary)d.Value);
                    iConnectionIds.Add(iConnection.Id);
                }
            }
            IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(objectManager, document, buildingBlock,
                iConnectionIds, curve);
        }

        public void AddAllIDotToDocument(CasePreferenceDataModel data)
        {
            if (data.ImplantDataModel == null)
            {
                return;
            }

            var dotList = data.ImplantDataModel.DotList;
            AddIDotToDocument(dotList, new List<Guid> { data.CaseGuid });
        }

        public void AddIDotToDocument(List<IDot> iDots, List<Guid> parentGuids)
        {
            var idsDocument = objectManager.GetDirector().IdsDocument;

            iDots.ForEach(d =>
            {
                IdsDocumentUtilities.AddNewClassObject(d.ToDictionary(), d.Id, idsDocument, parentGuids);
            });
        }

        public void AddAllIConnectionToDocument(CasePreferenceDataModel data)
        {
            if (data.ImplantDataModel == null)
            {
                return;
            }

            var connectionList = data.ImplantDataModel.ConnectionList;
            AddIConnectionToDocument(connectionList);
        }

        public void AddIConnectionToDocument(List<IConnection> iConnections)
        {
            var idsDocument = objectManager.GetDirector().IdsDocument;

            iConnections.ForEach(c =>
            {
                var parentGuids = new List<Guid>();
                if (c.A != null)
                {
                    parentGuids.Add(c.A.Id);
                }

                if (c.B != null)
                {
                    parentGuids.Add(c.B.Id);
                }

                IdsDocumentUtilities.AddNewClassObject(c.ToDictionary(), c.Id, idsDocument, parentGuids);
            });
        }

        public void AddScrewWithCalibrationBuildingBlock(CasePreferenceDataModel data)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            var targetLowLoDMeshes = new List<Mesh>();
            var implantSupport = implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantSupport, data);

            if (!objectManager.HasBuildingBlock(implantSupport))
            {
                var constraintMeshQuery = new ConstraintMeshQuery(objectManager);
                targetLowLoDMeshes = constraintMeshQuery.GetConstraintMeshesForImplant(true).ToList();
            }

            var screwCreator = new ScrewCreator(objectManager.GetDirector());
            if (!screwCreator.CreateAllScrewBuildingBlock(true, data, !targetLowLoDMeshes.Any() ?
                    null :
                    MeshUtilities.AppendMeshes(targetLowLoDMeshes)))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Failed to create screws for CaseID: {data.CaseGuid}");
                return;
            }

            if (targetLowLoDMeshes.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Uncalibrated screws created on CaseID: {data.CaseGuid}");
            }
        }

        public void UpdateIDotAndIConnection(CasePreferenceDataModel data, List<IConnection> newConnections)
        {
            // We have to add the dots into the IdsDocument before adding the connections because of the dependency
            var newDots = DataModelUtilities.FindDotDifferenceInNewConnection<IDot>(data.ImplantDataModel.ConnectionList,
                newConnections);
            var obsoleteDots = DataModelUtilities.FindDotDifferenceInNewConnection<IDot>(newConnections,
                data.ImplantDataModel.ConnectionList);

            UpdateIDotInDocument(data, obsoleteDots, newDots);

            var newConnection = DataModelUtilities.FindConnectionDifferenceInNewConnection(data.ImplantDataModel.ConnectionList,
                newConnections);
            var obsoleteConnection = DataModelUtilities.FindConnectionDifferenceInNewConnection(newConnections,
                data.ImplantDataModel.ConnectionList);

            UpdateIConnectionInDocument(obsoleteConnection, newConnection);
        }

        public void UpdateConnectionBuildingBlock(CasePreferenceDataModel data, List<IConnection> oldConnections, bool deleteObsoleteCurves)
        {
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Connection, data);
            var idsDocument = objectManager.GetDirector().IdsDocument;

            if (deleteObsoleteCurves)
            {
                DeleteObsoleteConnectionsBuildingBlock(data, oldConnections);
            }

            /*
             * A-----C1-----C2-----C3------B
             * We regenerate the whole connection (Point A to B), when we move control point C2.
             * While the connections C1-C2 and C2-C3 are invalidated when we move C2, the tree will eventually remove
             * the whole connection curve A-B.
            */
            var newConnections = DataModelUtilities.FindDifferenceConnectionsWithEndPoints(oldConnections,
                data.ImplantDataModel.ConnectionList);

            var implantConnectionCurves = ImplantCreationUtilities.CreateImplantConnectionCurves(newConnections, linkIConnectionToRhinoCurve: true);
            implantConnectionCurves.ForEach(curve =>
            {
                AddConnectionsBuildingBlock(idsDocument, buildingBlock, curve);
            });
        }

        public void InvalidateAllIDotInDocument(CasePreferenceDataModel data)
        {
            DeleteAllIDotFromDocument(data);
            AddAllIDotToDocument(data);
        }

        public void UpdateIDotInDocument(CasePreferenceDataModel data, List<IDot> obsoleteDots, List<IDot> newDots)
        {
            DeleteIDotFromDocument(obsoleteDots.Select(d => d.Id).ToList());
            AddIDotToDocument(newDots, new List<Guid> { data.CaseGuid });
        }

        public void InvalidateAllIConnectionInDocument(CasePreferenceDataModel data)
        {
            DeleteAllIConnectionFromDocument(data);
            AddAllIConnectionToDocument(data);
        }

        public void UpdateIConnectionInDocument(List<IConnection> obsoleteConnections, List<IConnection> newConnections)
        {
            DeleteIConnectionFromDocument(obsoleteConnections.Select(c => c.Id).ToList());
            AddIConnectionToDocument(newConnections);
        }

        public void InvalidateConnectionBuildingBlock(CasePreferenceDataModel data)
        {
            DeleteAllConnectionsBuildingBlock(data);
            AddAllConnectionsBuildingBlock(data);
        }

        public void InvalidateLandmarkBuildingBlock(CasePreferenceDataModel data)
        {
            DeleteLandmarksBuildingBlock(data);
            AddLandmarksBuildingBlock(data);
        }

        public bool IsConnectionBuildingBlockExist(CasePreferenceDataModel data)
        {
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Connection, data);
            return objectManager.HasBuildingBlock(buildingBlock);
        }

        public void DeleteAllIDotFromDocument(CasePreferenceDataModel data)
        {
            var document = objectManager.GetDirector().IdsDocument;
            var dotPastilleList = new List<Guid>();
            IdsDocumentUtilities.RecursiveSearchClassInTree(document, data.CaseGuid, typeof(DotPastille).FullName).ForEach(
                id =>
                {
                    dotPastilleList.Add(id);
                });

            DeleteIDotFromDocument(dotPastilleList);

            var dotControlPointList = new List<Guid>();
            IdsDocumentUtilities.RecursiveSearchClassInTree(document, data.CaseGuid, typeof(DotControlPoint).FullName).ForEach(
                id =>
                {
                    dotControlPointList.Add(id);
                });

            DeleteIDotFromDocument(dotControlPointList);
        }

        public void DeleteIDotFromDocument(List<Guid> iDotGuids)
        {
            var document = objectManager.GetDirector().IdsDocument;
            iDotGuids.ForEach(id => document.Delete(id));
        }

        public void DeleteAllIConnectionFromDocument(CasePreferenceDataModel data)
        {
            var document = objectManager.GetDirector().IdsDocument;
            var dotConnectionPlateList = new List<Guid>();
            IdsDocumentUtilities.RecursiveSearchClassInTree(document, data.CaseGuid, typeof(ConnectionPlate).FullName).ForEach(
                id =>
                {
                    dotConnectionPlateList.Add(id);
                });

            DeleteIConnectionFromDocument(dotConnectionPlateList);

            var dotConnectionLinkList = new List<Guid>();
            IdsDocumentUtilities.RecursiveSearchClassInTree(document, data.CaseGuid, typeof(ConnectionLink).FullName).ForEach(
                id =>
                {
                    dotConnectionLinkList.Add(id);
                });

            DeleteIConnectionFromDocument(dotConnectionLinkList);
        }

        public void DeleteIConnectionFromDocument(List<Guid> iConnectionGuids)
        {
            var document = objectManager.GetDirector().IdsDocument;
            iConnectionGuids.ForEach(id => document.Delete(id));
        }

        public void DeleteAllConnectionsBuildingBlock(CasePreferenceDataModel data)
        {
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Connection, data);
            var connections = objectManager.GetAllBuildingBlocks(buildingBlock);
            var document = objectManager.GetDirector().IdsDocument;
            foreach (var connection in connections)
            {
                DeleteConnectionsBuildingBlock(document, connection);
            }
        }

        public void DeleteObsoleteConnectionsBuildingBlock(CasePreferenceDataModel data, List<IConnection> oldConnections)
        {
            // We check for obsolete dots instead of curves for rotate screws
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Connection, data);
            var idsDocument = objectManager.GetDirector().IdsDocument;
            var connections = objectManager.GetAllBuildingBlocks(buildingBlock).ToList();
            var obsoleteIDot = DataModelUtilities.FindDotDifferenceInNewConnection<IDot>(data.ImplantDataModel.ConnectionList,
                    oldConnections);
            var obsoleteConnection = DataModelUtilities.GetConnectionsBasedOnDots(oldConnections,
                obsoleteIDot);

            connections.ForEach(rhinoObject =>
            {
                var curve = rhinoObject.Geometry as Curve;
                if (curve != null && curve.UserDictionary.ContainsKey(AttributeKeys.KeyIConnections))
                {
                    var dictionary = (ArchivableDictionary)curve.UserDictionary[AttributeKeys.KeyIConnections];

                    foreach (var d in dictionary)
                    {
                        var iConnection = SerializationFactory.DeSerializeIConnection((ArchivableDictionary)d.Value);

                        if (obsoleteConnection.Any(c => c.Id == iConnection.Id))
                        {
                            DeleteConnectionsBuildingBlock(idsDocument, rhinoObject);
                        }
                    }
                }
            });
        }

        public void DeleteConnectionsBuildingBlock(IDSDocument document, RhinoObject connection)
        {
            var node = document.GetNode(connection.Id);
            if (node != null)
            {
                document.Delete(connection.Id);
            }
        }

        public List<Curve> GetConnectionsBuildingBlockCurves(CasePreferenceDataModel data)
        {
            var connections = objectManager.GetAllBuildingBlocks(IBB.Connection);

            return (from connection in connections let dataModel = objectManager.GetImplantDataModel(connection) where dataModel == data.ImplantDataModel select (Curve)connection.Geometry).ToList();
        }

        //Partial Automation (Dependency Management?)
        //////////////////////
        public void HandleDeleteAllPlanningImplantRelatedItems(CasePreferenceDataModel data)
        {
            //delete ImplantPlanning and Connections
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PlanningImplant, data);
            var planningImplantRhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock).ToList();
            planningImplantRhinoObjects.ForEach(x => objectManager.DeleteObject(x.Id));

            data.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.PlanningImplant });
            //             objectManager.GetDirector().DependencyManager
            //                 .NotifyImplantBuildingBlockHasChanged(IBB.PlanningImplant, data);
        }

        public void InvalidatePlanningImplantBuildingBlock(CasePreferenceDataModel data)
        {
            var implant = _planningImplantBrepFactory.CreateImplant(data.ImplantDataModel);
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PlanningImplant, data);
            var oldImplantGuid = objectManager.GetBuildingBlockId(buildingBlock);
            objectManager.SetBuildingBlock(buildingBlock, implant, oldImplantGuid);
        }

        public void HandleAddNewImplant(CasePreferenceDataModel data, bool createConnectionBuildingBlock)
        {
            //It is possible that there is no design required
            if (data.ImplantDataModel == null)
            {
                return;
            }

            AddPlanningImplantBuildingBlock(data);
            AddScrewWithCalibrationBuildingBlock(data);

            if (createConnectionBuildingBlock)
            {
                AddAllConnectionsBuildingBlock(data);
            }
        }

        public void HandleUpdatePlanningImplant(CasePreferenceDataModel data)
        {
            //if ImplantDataModel is null or has no LineList - delete all
            if (data.ImplantDataModel == null || !data.ImplantDataModel.IsHasConstruction())
            {
                HandleDeleteAllPlanningImplantRelatedItems(data);
                return;
            }

            //if new - add
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PlanningImplant, data);
            if (!objectManager.HasBuildingBlock(buildingBlock))
            {
                HandleAddNewImplant(data, false);
            }
            else
            {
                //if exist - update
                HandleAllPlanningImplantRelatedItemsInvalidation(data);
            }

            //if in DesignPhase.Implant - update
            //director.ImplantManager.InvalidateConnectionBuildingBlock(implantDataModel);
            //screw move along and recalibrated
        }

        //TODO: Dependency Mgmt
        public void HandleAllPlanningImplantRelatedItemsInvalidation(CasePreferenceDataModel data, ImplantDataModel oldImplantDataModel = null)
        {
            InvalidatePlanningImplantBuildingBlock(data);

            if (IsConnectionBuildingBlockExist(data))
            {
                if (oldImplantDataModel != null)
                {
                    // Just regenerate missing curves, the dependency management from IdsDocument should remove the invalidated curves
                    UpdateConnectionBuildingBlock(data, oldImplantDataModel.ConnectionList, false);
                }
                else
                {
                    InvalidateConnectionBuildingBlock(data);
                }
            }

            if (IsLandmarkBuildingBlockExist(data))
            {
                InvalidateLandmarkBuildingBlock(data);
            }
        }

        public bool IsLandmarkBuildingBlockExist(CasePreferenceDataModel data)
        {
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Landmark, data);
            return objectManager.HasBuildingBlock(buildingBlock);
        }

        public void DeleteLandmarksBuildingBlock(CasePreferenceDataModel data)
        {
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Landmark, data);
            var landmarks = objectManager.GetAllBuildingBlocks(buildingBlock.Block.Name);
            var document = objectManager.GetDirector().IdsDocument;
            foreach (var landmark in landmarks)
            {
                var node = document.GetNode(landmark.Id);
                if (node != null)
                {
                    document.Delete(landmark.Id);
                }
            }
        }

        public void AddLandmarksBuildingBlock(CasePreferenceDataModel data)
        {
            //It is possible to not have implant design if user wants to design the guide directly using info from case preference panel.
            if (data.ImplantDataModel == null)
            {
                return;
            }

            foreach (var dot in data.ImplantDataModel.DotList)
            {
                var pastille = dot as DotPastille;
                if (pastille?.Landmark == null)
                {
                    continue;
                }

                AddLandmarkToDocument(pastille.Landmark);
            }
        }

        public void AddLandmarkToDocument(Landmark landmark)
        {
            var dataModels = objectManager.GetAllCasePreferenceData();
            foreach (var data in dataModels)
            {
                //It is possible not to have any design.
                if (data.ImplantDataModel == null)
                {
                    continue;
                }

                foreach (var dot in data.ImplantDataModel.DotList)
                {
                    var pastille = dot as DotPastille;

                    if (pastille?.Landmark == null || pastille.Landmark.Id != landmark.Id)
                    {
                        continue;
                    }

                    var landmarkBrep = landmarkBrepFactory.CreateLandmark(pastille.Landmark.LandmarkType, pastille.Thickness, pastille.Diameter / 2);
                    var transform = landmarkBrepFactory.GetTransform(pastille.Landmark.LandmarkType, RhinoPoint3dConverter.ToPoint3d(pastille.Location),
                        RhinoVector3dConverter.ToVector3d(pastille.Direction), RhinoPoint3dConverter.ToPoint3d(pastille.Landmark.Point), pastille.Diameter / 2);
                    landmarkBrep.Transform(transform);

                    var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Landmark, data);
                    var idsDocument = objectManager.GetDirector().IdsDocument;
                    var parentGuid = pastille.Screw.Id;
                    var guid = IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(objectManager, idsDocument, buildingBlock, parentGuid, landmarkBrep);
                    pastille.Landmark.Id = guid;
                    break;
                }
            }
        }

        public void DeleteLandmark(Landmark landmark)
        {
            var dataModels = objectManager.GetAllImplantDataModel();
            foreach (var data in dataModels)
            {
                foreach (var dot in data.DotList)
                {
                    var pastille = dot as DotPastille;

                    if (pastille?.Landmark == null || pastille.Landmark.Id != landmark.Id)
                    {
                        continue;
                    }

                    var document = objectManager.GetDirector().IdsDocument;

                    var node = document.GetNode(landmark.Id);
                    if (node != null)
                    {
                        document.Delete(landmark.Id);
                    }

                    pastille.Landmark = null;
                    break;
                }
            }
        }

        public void InvalidateImplantScrew(CasePreferenceDataModel casePrefModel)
        {
            var implantSupportManager = new ImplantSupportManager(objectManager);
            var supportRhObj = implantSupportManager.GetImplantSupportRhObj(casePrefModel);
            if (supportRhObj == null)
            {
                return;
            }

            var supportMesh = ImplantCreationUtilities.GetImplantRoIVolume(objectManager, casePrefModel, ref supportRhObj);

            var implantDataModel = casePrefModel.ImplantDataModel;
            if (implantDataModel == null)
            {
                return;
            }

            var pastilles = implantDataModel.DotList.Select(pastille => pastille as DotPastille).Where(x => x != null).ToList();
            foreach (var pastille in pastilles)
            {
                if (pastille.Screw == null)
                {
                    var screwCreator = new ScrewCreator(objectManager.GetDirector());
                    var screwAideDict = casePrefModel.ScrewAideData.GenerateScrewAideDictionary();
                    var screw = screwCreator.CreateScrewObjectOnPastille(RhinoPoint3dConverter.ToPoint3d(pastille.Location),
                        -RhinoVector3dConverter.ToVector3d(pastille.Direction),
                        screwAideDict, casePrefModel.CasePrefData.ScrewLengthMm,
                        casePrefModel.CasePrefData.ScrewTypeValue, casePrefModel.CasePrefData.BarrelTypeValue);

                    var screwCalibrator = new ScrewCalibrator(supportMesh);
                    if (screwCalibrator.LevelHeadOnTopOfMesh(screw, casePrefModel.CasePrefData.PlateThicknessMm, true))
                    {
                        screw = screwCalibrator.CalibratedScrew;
                    }

                    var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePrefModel);
                    objectManager.AddNewBuildingBlock(buildingBlock, screw);

                    ScrewPastilleManager.UpdateScrewDataInPastille(pastille, screw);
                }
                else
                {
                    var pastillePt = RhinoPoint3dConverter.ToPoint3d(pastille.Location);

                    var theScrewNow = (Screw)objectManager.GetDirector().Document.Objects.Find(pastille.Screw.Id);
                    var currHeadPt = theScrewNow.HeadPoint;
                    var currTipPt = theScrewNow.TipPoint;
                    var testLine = new Rhino.Geometry.Line(currHeadPt, currTipPt);

                    //check whether the pastille's location is within the given tolerance from a line generated with screw's HeadPoint and TipPoint
                    //this will determine whether the screw needs to be updated to the latest location or not
                    if (testLine.DistanceTo(pastillePt, true) > 0.01)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, $"Screw({theScrewNow.Index}) of implant({casePrefModel.CaseName}) is not tally with screw data");

                        var pastilleManager = new ScrewPastilleManager();
                        pastilleManager.UpdateScrewAfterMovePastille(objectManager, supportMesh, pastille,
                            theScrewNow, casePrefModel.CasePrefData.PlateThicknessMm, casePrefModel);
                    }
                }
            }
        }

        private const string KeyImplantSupportRoICreationInformation = "ImplantSupportRoICreationInformation";
        private ImplantSupportRoICreationInformation SupportRoICreationInformation { get; set; }

        public ImplantSupportRoICreationData GetImplantSupportRoICreationDataModel()
        {
            return new ImplantSupportRoICreationData
            {
                HasMetalIntegration = SupportRoICreationInformation.HasMetalIntegration,
                ResultingOffsetForRemovedMetal = SupportRoICreationInformation.ResultingOffsetForRemovedMetal,
                ResultingOffsetForRemainedMetal = SupportRoICreationInformation.ResultingOffsetForRemainedMetal,
                HasTeethIntegration = SupportRoICreationInformation.HasTeethIntegration,
                ResultingOffsetForTeeth = SupportRoICreationInformation.ResultingOffsetForTeeth,
            };
        }

        public void SetImplantSupportRoICreationInformation(ImplantSupportRoICreationData dataModel)
        {
            SupportRoICreationInformation.HasMetalIntegration = dataModel.HasMetalIntegration;
            SupportRoICreationInformation.ResultingOffsetForRemovedMetal = dataModel.ResultingOffsetForRemovedMetal;
            SupportRoICreationInformation.ResultingOffsetForRemainedMetal = dataModel.ResultingOffsetForRemainedMetal;
            SupportRoICreationInformation.HasTeethIntegration = dataModel.HasTeethIntegration;
            SupportRoICreationInformation.ResultingOffsetForTeeth = dataModel.ResultingOffsetForTeeth;
        }

        public void ResetImplantSupportRoICreationInformation()
        {
            SupportRoICreationInformation = new ImplantSupportRoICreationInformation();
        }

        public void ResetImplantSupportRoITeethIntegrationInformation()
        {
            var supportRoICreationInformation = new ImplantSupportRoICreationInformation();
            SupportRoICreationInformation.HasTeethIntegration = supportRoICreationInformation.HasTeethIntegration;
            SupportRoICreationInformation.ResultingOffsetForTeeth = supportRoICreationInformation.ResultingOffsetForTeeth;
        }

        public void ResetImplantSupportRoIMetalIntegrationInformation()
        {
            var supportRoICreationInformation = new ImplantSupportRoICreationInformation();
            SupportRoICreationInformation.HasMetalIntegration = supportRoICreationInformation.HasMetalIntegration;
            SupportRoICreationInformation.ResultingOffsetForRemovedMetal = supportRoICreationInformation.ResultingOffsetForRemovedMetal;
            SupportRoICreationInformation.ResultingOffsetForRemainedMetal = supportRoICreationInformation.ResultingOffsetForRemainedMetal;
        }

        public void SeparateTeethWrappedBuildingBlock()
        {
            var wrappedTeethCreator = new WrappedTeethCreator(objectManager);

            if (objectManager.HasBuildingBlock(IBB.OriginalTeethWrapped) &&
                !objectManager.HasBuildingBlock(IBB.OriginalMaxillaTeethWrapped) &&
                !objectManager.HasBuildingBlock(IBB.OriginalMandibleTeethWrapped))
            {
                var originalMaxillaWrapTeeth = wrappedTeethCreator.CreateOriginalWrapTeeth(TeethLayer.MaxillaTeeth);
                objectManager.AddNewBuildingBlock(IBB.OriginalMaxillaTeethWrapped, originalMaxillaWrapTeeth);
                var originalMandibleWrapTeeth = wrappedTeethCreator.CreateOriginalWrapTeeth(TeethLayer.MandibleTeeth);
                objectManager.AddNewBuildingBlock(IBB.OriginalMandibleTeethWrapped, originalMandibleWrapTeeth);

                objectManager.DeleteObject(objectManager.GetBuildingBlockId(IBB.OriginalTeethWrapped));
            }

            if (objectManager.HasBuildingBlock(IBB.PlannedTeethWrapped) &&
                !objectManager.HasBuildingBlock(IBB.PlannedMaxillaTeethWrapped) &&
                !objectManager.HasBuildingBlock(IBB.PlannedMandibleTeethWrapped))
            {
                var plannedMaxillaWrapTeeth = wrappedTeethCreator.CreatePlannedWrapTeeth(TeethLayer.MaxillaTeeth);
                objectManager.AddNewBuildingBlock(IBB.PlannedMaxillaTeethWrapped, plannedMaxillaWrapTeeth);
                var plannedMandibleWrapTeeth = wrappedTeethCreator.CreatePlannedWrapTeeth(TeethLayer.MandibleTeeth);
                objectManager.AddNewBuildingBlock(IBB.PlannedMandibleTeethWrapped, plannedMandibleWrapTeeth);

                objectManager.DeleteObject(objectManager.GetBuildingBlockId(IBB.PlannedTeethWrapped));
            }
        }

        public bool SaveImplantInformationTo3Dm(ArchivableDictionary dict)
        {
            return SaveImplantSupportRoICreationInformationTo3Dm(dict);
        }

        public bool LoadImplantInformationFrom3Dm(ArchivableDictionary dict)
        {
            if (dict.ContainsKey(KeyImplantSupportRoICreationInformation))
            {
                if (!LoadImplantSupportRoICreationInformationFrom3Dm(dict))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to load ImplantSupportRoICreation information! Default will be used.");
                }
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "No ImplantSupportRoICreation information found! Default will be used.");
            }

            return true;
        }

        private bool SaveImplantSupportRoICreationInformationTo3Dm(ArchivableDictionary dict)
        {
            var data = SerializationFactory.CreateSerializedArchive(SupportRoICreationInformation);
            return dict.Set(KeyImplantSupportRoICreationInformation, data);
        }

        private bool LoadImplantSupportRoICreationInformationFrom3Dm(ArchivableDictionary dict)
        {
            var loadedData = new ImplantSupportRoICreationInformation();

            if (!loadedData.DeSerialize((ArchivableDictionary)dict[KeyImplantSupportRoICreationInformation]))
            {
                return false;
            }

            SupportRoICreationInformation = loadedData;

            return true;
        }
    }
}
