using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Factory;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.Interface.Implant;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class ConnectionPreviewHelper
    {
        private const string IntermediateConnectionKey = "intermediate_connection";
        private const string DotListKey = "dot_list";
        private const string KeyDot = "Dot";

        private readonly CMFImplantDirector director;
        private readonly CMFObjectManager objectManager;

        public ConnectionPreviewHelper(CMFImplantDirector director)
        {
            this.director = director;
            objectManager = new CMFObjectManager(director);
        }

        public void AddConnectionPreviewBuildingBlocks(CasePreferenceDataModel casePreferenceData, List<ConnectionCreationResult> connectionCreationResults)
        {
            var implantComponent = new ImplantCaseComponent();
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.ConnectionPreview, casePreferenceData);

            foreach (var result in connectionCreationResults)
            {
                var parentGuids = new List<Guid>();
                foreach (var dot in result.Dots)
                {
                    if (dot is DotPastille dotPastille)
                    {
                        parentGuids.Add(dotPastille.Screw.Id);
                    }
                }

                var connectionCurvesId = GetConnectionCurvesIdFromIConnection(casePreferenceData, result.Dots);
                if (connectionCurvesId.Count == 0)
                {
                    throw new Exception("No connection curves found for the provided dots!");
                }

                parentGuids.AddRange(connectionCurvesId);

                // TODO: After we migrate the DotPastille and DotControlPoint to the IDSDocument, need to add DotPastille and DotControlPoint and parents for connection
                var id = IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(objectManager, director.IdsDocument, buildingBlock, parentGuids, result.FinalConnection);

                if (id == Guid.Empty)
                {
                    throw new Exception("Failed to add Connection creation result!");
                }

                var rhinoObject = director.Document.Objects.Find(id);
                rhinoObject.Attributes.UserDictionary.Set(IntermediateConnectionKey, result.IntermediateConnection);

                var dictionary = new ArchivableDictionary();
                var dotCounter = 0;
                foreach (var dot in result.Dots)
                {
                    var dotArc = DotSerializerHelper.CreateArchive(dot);
                    if (dotArc == null)
                    {
                        throw new Exception("Failed to serialize Dots in Connection creation result!");
                    }

                    dictionary.Set(KeyDot + $"_{dotCounter}", dotArc);
                    dotCounter++;
                }

                rhinoObject.Attributes.UserDictionary.Set(DotListKey, dictionary);
            }
        }

        public List<DotCurveDataModel> GetMissingConnectionPreviewDotCurveDataModels(CasePreferenceDataModel casePreferenceData, Mesh supportMesh, IEnumerable<Screw> screws, double pastillePlacementModifier, bool isUsingV2Creator)
        {
            var implant = ImplantPastilleCreationUtilities.AdjustPastilles(casePreferenceData.ImplantDataModel, supportMesh, screws, pastillePlacementModifier);

            var connectionList = implant.ConnectionList.ToList();

            List<DotCurveDataModel> dataModels;
            if (isUsingV2Creator)
            {
                dataModels = ImplantCreationUtilities
                    .CreateImplantConnectionCurveDataModelsV2(connectionList);
            }
            else
            {
                dataModels = ImplantCreationUtilities
                    .CreateImplantConnectionCurveDataModels(connectionList);
            }

            if (!HasConnectionPreviewBuildingBlock(casePreferenceData))
            {
                return dataModels;
            }

            var connectionPreviewDots = GetConnectionPreviewDots(casePreferenceData);

            var missingConnections = dataModels.ToList();
            foreach (var dataModel in dataModels)
            {
                foreach (var dotsPair in connectionPreviewDots)
                {
                    var dots = dotsPair.Key;
                    if (dataModel.Dots.All(d => dots.Any(i => i.Id == d.Id)) && dots.All(d => dataModel.Dots.Any(i => i.Id == d.Id)))
                    {
                        missingConnections.Remove(dataModel);
                        break;
                    }
                }
            }

            return missingConnections;
        }

        public bool HasConnectionPreviewBuildingBlock(CasePreferenceDataModel casePreferenceData)
        {
            var implantComponent = new ImplantCaseComponent();
            var implantBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.ConnectionPreview, casePreferenceData);
            return objectManager.HasBuildingBlock(implantBuildingBlock.Block);
        }

        private List<RhinoObject> GetConnectionPreviewRhinoObjects(CasePreferenceDataModel casePreferenceData)
        {
            var implantComponent = new ImplantCaseComponent();
            var implantBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.ConnectionPreview, casePreferenceData);
            return objectManager.GetAllBuildingBlocks(implantBuildingBlock.Block).ToList();
        }

        public List<Mesh> GetIntermediateConnectionPreviews(CasePreferenceDataModel casePreferenceData)
        {
            var rhinoObjects = GetConnectionPreviewRhinoObjects(casePreferenceData);

            var meshes = new List<Mesh>();
            foreach (var rhinoObject in rhinoObjects)
            {
                meshes.Add(GetIntermediateConnectionPreview(rhinoObject));
            }
            return meshes;
        }

        public List<Guid> GetRhinoObjectIdsFromConnections(CasePreferenceDataModel casePreferenceData, List<IConnection> connections)
        {
            var ids = new List<Guid>();
            var keyValuePairs = GetConnectionPreviewDots(casePreferenceData);

            foreach (var connection in connections)
            {
                foreach (var kvp in keyValuePairs)
                {
                    if (kvp.Key.Any(i => i.Id == connection.A.Id) && kvp.Key.Any(i => i.Id == connection.B.Id))
                    {
                        ids.Add(kvp.Value.Id);
                    }
                }
            }

            return ids;
        }

        public List<Guid> GetRhinoObjectIdsFromDots(CasePreferenceDataModel casePreferenceData, List<IDot> dots)
        {
            var ids = new List<Guid>();
            var keyValuePairs = GetConnectionPreviewDots(casePreferenceData);

            foreach (var dot in dots)
            {
                foreach (var kvp in keyValuePairs)
                {
                    if (kvp.Key.Any(i => i.Id == dot.Id))
                    {
                        ids.Add(kvp.Value.Id);
                    }
                }
            }

            return ids;
        }

        private Mesh GetIntermediateConnectionPreview(RhinoObject rhinoObject)
        {
            if (rhinoObject.Attributes.UserDictionary.ContainsKey(IntermediateConnectionKey))
            {
                return (Mesh)rhinoObject.Attributes.UserDictionary[IntermediateConnectionKey];
            }
            else
            {
                throw new Exception("Connection preview object does not have intermediate parts!");
            }
        }

        public List<KeyValuePair<List<IDot>, RhinoObject>> GetConnectionPreviewDots(CasePreferenceDataModel casePreferenceData)
        {
            var rhinoObjects = GetConnectionPreviewRhinoObjects(casePreferenceData);

            var connectionPreviewDots = new List<KeyValuePair<List<IDot>, RhinoObject>>();

            foreach (var rhinoObject in rhinoObjects)
            {
                if (rhinoObject.Attributes.UserDictionary.ContainsKey(DotListKey))
                {
                    var dots = new List<IDot>();

                    var dictionary = (ArchivableDictionary)rhinoObject.Attributes.UserDictionary[DotListKey];
                    foreach (var d in dictionary)
                    {
                        var dot = SerializationFactory.DeSerializeIDot((ArchivableDictionary)d.Value);
                        if (dot == null)
                        {
                            throw new Exception("Failed to deserialize Dots from Connection Preview!");
                        }

                        dots.Add(dot);
                    }

                    connectionPreviewDots.Add(new KeyValuePair<List<IDot>, RhinoObject>(dots, rhinoObject));
                    continue;
                }

                throw new Exception("Connection preview object does not have dot list!");
            }

            return connectionPreviewDots;
        }

        public List<Guid> GetConnectionCurvesIdFromIConnection(CasePreferenceDataModel casePreferenceData, List<IDot> dots)
        {
            var result = new List<Guid>();
            var implantComponent = new ImplantCaseComponent();
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Connection, casePreferenceData);
            var rhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock).ToList();

            foreach (var connection in rhinoObjects)
            {
                var curve = (Curve)connection.Geometry;

                if (curve.UserDictionary.ContainsKey(AttributeKeys.KeyIConnections))
                {
                    var dictionary = (ArchivableDictionary)curve.UserDictionary[AttributeKeys.KeyIConnections];

                    foreach (var d in dictionary)
                    {
                        var iConnection = SerializationFactory.DeSerializeIConnection((ArchivableDictionary)d.Value);

                        if (dots.Any(dot => dot.Id == iConnection.A.Id) && dots.Any(dot => dot.Id == iConnection.B.Id))
                        {
                            if (!result.Contains(connection.Id))
                            {
                                result.Add(connection.Id);
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
