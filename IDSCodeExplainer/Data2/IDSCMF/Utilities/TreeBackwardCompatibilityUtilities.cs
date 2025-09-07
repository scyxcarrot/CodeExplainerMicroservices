using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Factory;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.DataModel;
using IDS.Core.V2.DataModels;
using IDS.Core.V2.TreeDb.Model;
using IDS.Interface.Implant;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public static class TreeBackwardCompatibilityUtilities
    {
        /// <summary>
        /// Create tree for old ids files
        /// When updating this for future backward compatibility, please add the functions at the top side since we are moving the data from bottom to top
        /// Also, change the CheckIBBNodeConnectedToRootNode to the highest IBB node
        /// </summary>
        /// <param name="director">Director class</param>
        public static void CreateTree(CMFImplantDirector director)
        {
            var rootGuid = CreateDummyRootNode(director.IdsDocument);
            var tsgGuid = CreateDummyRootTSGNode(director.IdsDocument);
            // check if the top most node has the guid for the part that we want, 
            // if they have the guid for the correct part, then skip backward compatibility
            // if not, delete the children of Root node (if exist) and perform backward compatibility from scratch
            var isCasePreferenceConnectedToRootNode = CheckCasePreferenceNodeConnectedToRootNode(director);
            var isIDotConnectedToCasePreference = CheckIDotNodeConnectedToCasePreferenceNode(director);

            if (!isCasePreferenceConnectedToRootNode || !isIDotConnectedToCasePreference)
            {
                //Skip recreating tree for TSG
                var firstLevelChildren = director.IdsDocument.GetChildrenInTree(rootGuid).Where(guids => guids != IdsDocumentUtilities.TSGRootGuid);
                foreach (var id in firstLevelChildren)
                {
                    director.IdsDocument.Delete(id);
                }

                BackwardCompatibleCasePreferenceAndDataModels(director);
                BackwardCompatibleIDotAndIConnection(director);
                BackwardCompatibleDataModelToScrew(director);
                BackwardCompatibleDataModelToConnectionCurve(director);
                BackwardCompatibleImplantScrewToPastillePreviewAndLandmark(director);
                BackwardCompatibleImplantScrewToConnectionPreview(director);
                BackwardCompatibleImplantScrewToBarrel(director);
                BackwardCompatibleImplantObjects(director,
                   new List<IBB>() { IBB.PastillePreview, IBB.ConnectionPreview },
                   IBB.ImplantPreview);
                BackwardCompatibleImplantObjects(director,
                    IBB.ImplantPreview, IBB.ActualImplant);
                BackwardCompatibleImplantObjects(director,
                    IBB.ImplantPreview, IBB.ActualImplantImprintSubtractEntity);
                BackwardCompatibleImplantObjects(director,
                    IBB.ImplantPreview, IBB.ActualImplantSurfaces);
                BackwardCompatibleImplantObjects(director,
                    IBB.ImplantPreview, IBB.ActualImplantWithoutStampSubtraction);
                BackwardCompatibleImplantObjects(director,
                    IBB.ImplantPreview, IBB.ImplantScrewIndentationSubtractEntity);

                director.IdsDocument.ClearUndoRedo();
            }
        }

        /// <summary>
        /// We need to create a dummy root node inside the IDSDocument
        /// because we are building the tree from the bottom to the top.
        /// Once we finish building the tree fully,
        /// we can remove this method and replace it with the actual root node in the document
        /// </summary>
        /// <param name="idsDocument">IDSDocument to create the dummy root node in</param>
        /// <returns></returns>
        private static Guid CreateDummyRootNode(IDSDocument idsDocument)
        {
            if (!idsDocument.IsNodeInTree(IdsDocumentUtilities.RootGuid))
            {
                var guidValueData = new GuidValueData(IdsDocumentUtilities.RootGuid,
                    new List<Guid>(), IdsDocumentUtilities.RootGuid);
                idsDocument.Create(guidValueData);
            }

            return IdsDocumentUtilities.RootGuid;
        }
        private static Guid CreateDummyRootTSGNode(IDSDocument idsDocument)
        {
            if (!idsDocument.IsNodeInTree(IdsDocumentUtilities.TSGRootGuid))
            {
                var guidValueData = new GuidValueData(IdsDocumentUtilities.TSGRootGuid,
                    new List<Guid>() { IdsDocumentUtilities.RootGuid }, IdsDocumentUtilities.TSGRootGuid);
                idsDocument.Create(guidValueData);
            }

            return IdsDocumentUtilities.TSGRootGuid;
        }

        private static bool BackwardCompatibleImplantObjects(
            CMFImplantDirector director,
            IBB parentIbb, IBB childIbb)
        {
            return BackwardCompatibleImplantObjects(director,
                new List<IBB>() { parentIbb }, childIbb);
        }

        /// <summary>
        /// Creates the tree inside the database for backward compatibility
        /// </summary>
        /// <param name="director">director to create object manager and access IdsDocument</param>
        /// <param name="parentIbbs">IBB of the parent objects</param>
        /// <param name="childIbb"></param>
        private static bool BackwardCompatibleImplantObjects(
            CMFImplantDirector director,
            List<IBB> parentIbbs, IBB childIbb)
        {
            var success = true;
            var objectManager = new CMFObjectManager(director);

            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var implantChildren =
                    objectManager.GetAllImplantExtendedImplantBuildingBlocks(
                            childIbb, casePreferenceDataModel);

                foreach (var implantChild in implantChildren)
                {
                    var implantChildId = implantChild.Id;
                    var node = director.IdsDocument.GetNode(implantChildId);
                    if (node != null && node.Parents.Count > 0)
                    {
                        director.IdsDocument.Delete(implantChildId);
                    }

                    var implantParentIds = new List<Guid>();
                    foreach (var parentIbb in parentIbbs)
                    {
                        implantParentIds.AddRange(
                            objectManager.GetAllImplantExtendedImplantBuildingBlocks(
                                    parentIbb, casePreferenceDataModel)
                                .Select(implantRhinoObject => implantRhinoObject.Id).ToList());
                    }

                    if (implantChild.ObjectType == ObjectType.Mesh)
                    {
                        var objectValueData = CreateObjectValueData(implantChildId,
                            implantParentIds, childIbb);
                        success &= director.IdsDocument.Create(objectValueData);
                    }
                    else
                    {
                        var guidValueData = new GuidValueData(implantChildId,
                           implantParentIds, implantChildId);
                        success &= director.IdsDocument.Create(guidValueData);
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Creates the tree inside the database for backward compatibility
        /// </summary>
        /// <param name="director">director to create object manager and access IdsDocument</param>
        /// <param name="parentGuid">GUID of the parent node</param>
        /// <param name="childIbb"></param>
        private static bool BackwardCompatibleImplantObjects(
            CMFImplantDirector director,
            Guid parentGuid, IBB childIbb)
        {
            var success = true;
            var objectManager = new CMFObjectManager(director);

            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var implantChildren =
                    objectManager.GetAllImplantExtendedImplantBuildingBlocks(
                            childIbb, casePreferenceDataModel);

                foreach (var implantChild in implantChildren)
                {
                    var implantChildId = implantChild.Id;
                    var node = director.IdsDocument.GetNode(implantChildId);
                    if (node != null && node.Parents.Count > 0)
                    {
                        director.IdsDocument.Delete(implantChildId);
                    }

                    if (implantChild.ObjectType == ObjectType.Mesh ||
                        implantChild.ObjectType == ObjectType.Brep)
                    {
                        var objectValueData = CreateObjectValueData(implantChildId,
                            new List<Guid>() { parentGuid }, childIbb);
                        success &= director.IdsDocument.Create(objectValueData);
                    }
                    else
                    {
                        var guidValueData = new GuidValueData(implantChildId,
                            new List<Guid>() { parentGuid }, implantChildId);
                        success &= director.IdsDocument.Create(guidValueData);
                    }
                }
            }

            return success;
        }

        private static bool BackwardCompatibleCasePreferenceAndDataModels(
            CMFImplantDirector director)
        {
            var success = true;

            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var node = director.IdsDocument.GetNode(casePreferenceDataModel.CaseGuid);
                if (node != null && node.Parents.Count > 0)
                {
                    director.IdsDocument.Delete(casePreferenceDataModel.CaseGuid);
                }

                var guidValueData = new GuidValueData(casePreferenceDataModel.CaseGuid,
                    new List<Guid>() { IdsDocumentUtilities.RootGuid }, casePreferenceDataModel.CaseGuid);
                success &= director.IdsDocument.Create(guidValueData);
            }

            return success;
        }

        private static void BackwardCompatibleIDotAndIConnection(CMFImplantDirector director)
        {
            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                director.ImplantManager.InvalidateAllIDotInDocument(casePreferenceDataModel);
                director.ImplantManager.InvalidateAllIConnectionInDocument(casePreferenceDataModel);
            }
        }

        private static bool BackwardCompatibleDataModelToScrew(
           CMFImplantDirector director)
        {
            var success = true;

            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var connectionList = casePreferenceDataModel.ImplantDataModel.ConnectionList;

                foreach (var connection in connectionList)
                {
                    success &= AddScrews(director, connection);
                }
            }

            return success;
        }

        public static bool AddScrews(CMFImplantDirector director, IConnection connection, List<Guid> additionalParents = null)
        {
            var success = true;

            success &= AddScrew(director, connection.A, additionalParents);
            success &= AddScrew(director, connection.B, additionalParents);

            return success;
        }

        private static bool AddScrew(CMFImplantDirector director, IDot dot, List<Guid> additionalParents = null)
        {
            var success = true;

            if (dot is DotPastille dotPastille && dotPastille.Screw != null)
            {
                var node = director.IdsDocument.GetNode(dotPastille.Screw.Id);
                if (node != null)
                {
                    return success;
                }

                var parentGuids = new List<Guid>() { dotPastille.Id };
                if (additionalParents != null && additionalParents.Any())
                {
                    parentGuids.AddRange(additionalParents);
                }

                var objectValueData = CreateObjectValueData(dotPastille.Screw.Id,
                   parentGuids, IBB.Screw);
                success &= director.IdsDocument.Create(objectValueData);
            }

            return success;
        }

        private static bool BackwardCompatibleDataModelToConnectionCurve(
           CMFImplantDirector director)
        {
            // It's fine to regenerate the connection curves with a new Guid
            // since it is not dependent on other objects and it does not invalidate the old connection previews
            director.CasePrefManager.CasePreferences.ForEach(c =>
            {
                var implantComponent = new ImplantCaseComponent();
                var objectManager = new CMFObjectManager(director);
                var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Connection, c);
                var connectionIds = objectManager.GetAllBuildingBlockIds(buildingBlock).ToList();
                connectionIds.ForEach(id => objectManager.DeleteObject(id));

                director.ImplantManager.AddAllConnectionsBuildingBlock(c);
            });
            return true;
        }

        private static bool CheckCasePreferenceNodeConnectedToRootNode(CMFImplantDirector director)
        {
            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var node = director.IdsDocument.GetNode(casePreferenceDataModel.CaseGuid);
                if (node != null)
                {
                    if (!node.Parents.Contains(IdsDocumentUtilities.RootGuid))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CheckIDotNodeConnectedToCasePreferenceNode(CMFImplantDirector director)
        {
            var result = true;
            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var node = director.IdsDocument.GetChildrenInTree(casePreferenceDataModel.CaseGuid);

                if (node == null || !node.Any())
                {
                    return false;
                }

                result &= node.TrueForAll(n =>
                {
                    var objectValueData = director.IdsDocument.GetNode(n) as ObjectValueData;
                    if (objectValueData == null)
                    {
                        return false;
                    }

                    if (!objectValueData.Value.Attributes["Class"].ToString().Contains(typeof(DotPastille).FullName) ||
                        !objectValueData.Value.Attributes["Class"].ToString().Contains(typeof(DotControlPoint).FullName))
                    {
                        return false;
                    }

                    return true;
                });
            }

            return result;
        }

        /// <summary>
        /// Check if the IBB node is connected to root node because we want to know if we need to perform backward compatibility
        /// In the future, if we move more parts of the document into the new tree, we will need to change the IBB to check
        /// </summary>
        /// <param name="director">director to create object manager and access IdsDocument</param>
        /// <param name="ibb">IBB to check if it is connected to root node</param>
        /// <returns>True if all objects in IBB is connected to root node, false if not connected or IBB objects not present</returns>
        private static bool CheckIBBNodeConnectedToRootNode(CMFImplantDirector director, IBB ibb)
        {
            var objectManager = new CMFObjectManager(director);

            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var implantObjectIds =
                    objectManager.GetAllImplantExtendedImplantBuildingBlocks(
                            ibb, casePreferenceDataModel)
                        .Select(implantObject => implantObject.Id);
                if (!implantObjectIds.Any())
                {
                    return false;
                }

                foreach (var implantObjectId in implantObjectIds)
                {
                    var node = director.IdsDocument.GetNode(implantObjectId);
                    if (node != null)
                    {
                        if (!node.Parents.Contains(IdsDocumentUtilities.RootGuid))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static ObjectValueData CreateObjectValueData(Guid id, IEnumerable<Guid> parents, IBB bb)
        {
            return new ObjectValueData(id, parents, new ObjectValue
            {
                Attributes = new Dictionary<string, object>
                    {
                        { "IBB", bb.ToString() }
                    }
            });
        }

        private static bool BackwardCompatibleImplantScrewToPastillePreviewAndLandmark(
            CMFImplantDirector director)
        {
            var success = true;

            var objectManager = new CMFObjectManager(director);
            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var pastilleList =
                    casePreferenceDataModel.ImplantDataModel.DotList
                        .Where(dot => dot is DotPastille).Cast<DotPastille>();

                var pastillePreviews =
                    objectManager.GetAllImplantExtendedImplantBuildingBlocks(
                        IBB.PastillePreview, casePreferenceDataModel);
                foreach (var pastillePreview in pastillePreviews)
                {
                    pastillePreview.Attributes.UserDictionary
                        .TryGetGuid(PastillePreviewHelper.DotPastilleId,
                            out var dotPastilleId);
                    var dotPastille = pastilleList.First(
                        p => p.Id == dotPastilleId);

                    var screwParentIds = new List<Guid>() { dotPastille.Screw.Id };
                    if (dotPastille.Landmark != null)
                    {
                        var landmarkParentIds = new List<Guid>() { dotPastille.Screw.Id };
                        var landmarkValueData = CreateObjectValueData(
                            dotPastille.Landmark.Id,
                            landmarkParentIds,
                            IBB.Landmark);
                        success &= director.IdsDocument.Create(landmarkValueData);

                        screwParentIds = new List<Guid>() { dotPastille.Screw.Id, dotPastille.Landmark.Id };
                    }

                    var pastilleScrewValueData = CreateObjectValueData(
                        pastillePreview.Id,
                        screwParentIds,
                        IBB.PastillePreview);
                    success &= director.IdsDocument.Create(pastilleScrewValueData);
                }
            }

            return success;
        }

        private static bool BackwardCompatibleImplantScrewToConnectionPreview(
            CMFImplantDirector director)
        {
            var success = true;

            var connectionPreviewHelper = new ConnectionPreviewHelper(director);
            var objectManager = new CMFObjectManager(director);
            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var screwIds =
                    objectManager.GetAllImplantExtendedImplantBuildingBlocksIDs(
                        IBB.Screw, casePreferenceDataModel);
                var connectionList =
                    casePreferenceDataModel.ImplantDataModel.ConnectionList;

                var connectionIdToParentMap = new Dictionary<Guid, List<Guid>>();
                foreach (var screwId in screwIds)
                {
                    var connectionsAffected = connectionList.Where(
                        connection => CheckIfScrewWillAffectConnection(
                            screwId, connection))
                        .ToList();
                    var connectionPreviewIds =
                        connectionPreviewHelper.GetRhinoObjectIdsFromConnections(
                            casePreferenceDataModel, connectionsAffected.ToList());

                    foreach (var connectionPreviewId in connectionPreviewIds)
                    {
                        AddOrAppendIfPresent(ref connectionIdToParentMap, connectionPreviewId, screwId);
                    }
                }

                BackwardCompatibleConnectionCurveToConnectionPreview(director, casePreferenceDataModel,
                    ref connectionIdToParentMap);

                foreach (var connectionIdToParent in connectionIdToParentMap)
                {
                    var objectValueData = CreateObjectValueData(connectionIdToParent.Key,
                        connectionIdToParent.Value, IBB.ConnectionPreview);
                    success &= director.IdsDocument.Create(objectValueData);
                }
            }

            return success;
        }

        private static bool BackwardCompatibleConnectionCurveToConnectionPreview(CMFImplantDirector director, CasePreferenceDataModel casePreferenceDataModel,
            ref Dictionary<Guid, List<Guid>> map)
        {
            var connectionPreviewHelper = new ConnectionPreviewHelper(director);
            var connectionDots = connectionPreviewHelper.GetConnectionPreviewDots(casePreferenceDataModel);

            foreach (var keyValue in map)
            {
                var connectionDotsMapping = connectionDots.Find(c => c.Value.Id == keyValue.Key);
                var connectionCurveId = MatchScrewsWithConnectionCurves(director, casePreferenceDataModel, connectionDotsMapping.Key);

                foreach (var id in connectionCurveId)
                {
                    AddOrAppendIfPresent(ref map, keyValue.Key, id);
                }
            }

            return true;
        }

        private static List<Guid> MatchScrewsWithConnectionCurves(CMFImplantDirector director, CasePreferenceDataModel casePreferenceDataModel, List<IDot> iDots)
        {
            var result = new List<Guid>();
            var objectManager = new CMFObjectManager(director);
            var implantComponent = new ImplantCaseComponent();
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Connection, casePreferenceDataModel);
            var rhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock).ToList();

            foreach (var connection in rhinoObjects)
            {
                var curve = (Curve)connection.Geometry;

                if (curve.UserDictionary.ContainsKey(AttributeKeys.KeyIConnections))
                {
                    var dictionary = (ArchivableDictionary)curve.UserDictionary[AttributeKeys.KeyIConnections];
                    var dotId = new List<Guid>();

                    foreach (var d in dictionary)
                    {
                        var iConnection = SerializationFactory.DeSerializeIConnection((ArchivableDictionary)d.Value);
                        dotId.Add(iConnection.A.Id);
                        dotId.Add(iConnection.B.Id);

                    }

                    if (iDots.All(m => dotId.Contains(m.Id)) && !result.Contains(connection.Id))
                    {
                        result.Add(connection.Id);
                    }
                }
            }

            return result;
        }

        private static bool BackwardCompatibleImplantScrewToBarrel(CMFImplantDirector director)
        {
            var success = true;
            var objectManager = new CMFObjectManager(director);

            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var screws = objectManager
                    .GetAllImplantExtendedImplantBuildingBlocks(
                        IBB.Screw,
                        casePreferenceDataModel);
                foreach (var screw in screws)
                {
                    screw.Attributes.UserDictionary.TryGetGuid(
                        AttributeKeys.KeyRegisteredBarrel,
                        out var barrelId);

                    if (barrelId == Guid.Empty)
                    {
                        continue;
                    }

                    var objectValueData = CreateObjectValueData(
                        barrelId,
                        new List<Guid>() { screw.Id },
                        IBB.RegisteredBarrel);

                    success &= director.IdsDocument.Create(objectValueData);

                }
            }
            return success;
        }
        
        public static void AddOrAppendIfPresent(ref Dictionary<Guid, List<Guid>> map, Guid connectionId, Guid parentId)
        {
            if (map.ContainsKey(connectionId))
            {
                // Connection ID exists, add the parent ID to the list if it's not already there
                if (!map[connectionId].Contains(parentId))
                {
                    map[connectionId].Add(parentId);
                }
            }
            else
            {
                // Connection ID doesn't exist, create a new list with the parent ID
                map[connectionId] = new List<Guid> { parentId };
            }
        }


        // if connection dot pastille is the same as screwId, then it will affect
        private static bool CheckIfScrewWillAffectConnection(Guid screwId, IConnection connection)
        {
            var screwMatch = false;
            if (connection.A is DotPastille dotPastilleA)
            {
                screwMatch |= dotPastilleA.Screw.Id == screwId;
            }

            if (connection.B is DotPastille dotPastilleB)
            {
                screwMatch |= dotPastilleB.Screw.Id == screwId;
            }

            return screwMatch;
        }
    }
}
