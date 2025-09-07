using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Creators;
using IDS.CMF.V2.DataModel;
using IDS.Core.Plugin;
using IDS.Core.V2.DataModels;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using IDS.RhinoInterface.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("0CE40557-4FEC-4CC5-867F-89A8717367D0")]
    [IDSCMFCommandAttributes(DesignPhase.TeethBlock)]
    public class CMFTSGCreateTeethBlock : CmfCommandBase
    {
        public CMFTSGCreateTeethBlock()
        {
            TheCommand = this;
            VisualizationComponent = new CMFTSGCreateTeethBlockVisualization();
        }

        public static CMFTSGCreateTeethBlock TheCommand { get; private set; }

        public override string EnglishName => "CMFTSGCreateTeethBlock";

        // _-CMFTSGCreateTeethBlock TeethType Mandibular GuideCaseNumber 1
        // _-CMFTSGCreateTeethBlock TeethType Maxilla GuideCaseNumber 1
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (!TSGGuideCommandHelper.PromptAndCheckInformation(
                    director,
                    out GuidePreferenceDataModel guidePreferenceDataModel,
                    out IBB limitingSurfaceIbb,
                    out IBB limitingSurfaceExtrusionIbb,
                    out IBB bracketRegionIbb,
                    out IBB bracketExtrusionIbb,
                    out IBB teethBlockRoiIbb,
                    out ExtendedImplantBuildingBlock castEIbb,
                    out IBB finalSupportIbb,
                    out IBB finalSupportWrappedIbb,
                    out IBB reinforcementRegionIbb,
                    out IBB reinforcementExtrusionIbb,
                    out ExtendedImplantBuildingBlock teethBaseRegionEIbb,
                    out ExtendedImplantBuildingBlock teethBaseExtrusionEIbb))
            {
                return Result.Failure;
            }

            var console = new IDSRhinoConsole();
            var teethBlockCreatorInput = InitializeTeethBlockCreatorInput(
                console, 
                director, 
                limitingSurfaceIbb, 
                bracketRegionIbb, 
                castEIbb, 
                reinforcementRegionIbb, 
                teethBaseRegionEIbb);
            var currentDataModel = InitializeTeethBlockCreatorDataModel(
                director,
                limitingSurfaceIbb,
                limitingSurfaceExtrusionIbb, 
                bracketRegionIbb, 
                bracketExtrusionIbb, 
                teethBlockRoiIbb, 
                finalSupportIbb,
                reinforcementRegionIbb,
                reinforcementExtrusionIbb,
                teethBaseRegionEIbb, 
                teethBaseExtrusionEIbb, 
                finalSupportWrappedIbb,
                guidePreferenceDataModel);
            var teethBlockCreator = new TeethBlockCreator(teethBlockCreatorInput, currentDataModel);

            teethBlockCreator.CreateTeethBlock();

            ProcessOutputs(
                director, 
                teethBlockCreator, 
                currentDataModel, 
                limitingSurfaceExtrusionIbb, 
                bracketExtrusionIbb, 
                teethBlockRoiIbb,
                finalSupportIbb,
                finalSupportWrappedIbb,
                reinforcementExtrusionIbb,
                teethBaseExtrusionEIbb,
                guidePreferenceDataModel);

            var createTeethBlockVisualizationComponent =
                (CMFTSGCreateTeethBlockVisualization)VisualizationComponent;
            createTeethBlockVisualizationComponent.ShowTeethBlockOnly(
                doc, 
                guidePreferenceDataModel);

            return Result.Success;
        }

        private static void ProcessOutputs(
            CMFImplantDirector director, 
            TeethBlockCreator teethBlockCreator,
            TeethBlockCreatorDataModel currentDataModel, 
            IBB limitingSurfaceExtrusionIbb, 
            IBB bracketExtrusionIbb,
            IBB teethBlockRoiIbb,
            IBB finalSupportIbb,
            IBB finalSupportWrappedIbb,
            IBB reinforcementExtrusionIbb,
            ExtendedImplantBuildingBlock teethBaseExtrusionEIbb,
            GuidePreferenceDataModel guidePreferenceDataModel)
        {
            var objectManager = new CMFObjectManager(director);

            // register limiting surface extrusion
            foreach (var limitingSurfaceIdAndExtrusion in teethBlockCreator.Output.LimitingSurfaceIdAndExtrusionMap)
            {
                var limitingSurfaceId = limitingSurfaceIdAndExtrusion.Key;
                if (currentDataModel.LimitingSurfaceIdAndExtrusionMap.ContainsKey(limitingSurfaceId))
                {
                    continue;
                }

                var limitingSurfaceIdsMesh = limitingSurfaceIdAndExtrusion.Value;
                var limitingSurfaceExtrusionMesh = RhinoMeshConverter.ToRhinoMesh(limitingSurfaceIdsMesh);
                IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                    objectManager,
                    director.IdsDocument,
                    limitingSurfaceExtrusionIbb,
                    limitingSurfaceId,
                    limitingSurfaceExtrusionMesh);
            }

            // register bracket extrusion
            foreach (var bracketRegionIdAndExtrusion in teethBlockCreator.Output.BracketRegionIdAndExtrusionMap)
            {
                var bracketRegionId = bracketRegionIdAndExtrusion.Key;
                if (currentDataModel.BracketRegionIdAndExtrusionMap.ContainsKey(bracketRegionId))
                {
                    continue;
                }

                var bracketExtrusionIdsMesh = bracketRegionIdAndExtrusion.Value;
                var bracketExtrusionMesh = RhinoMeshConverter.ToRhinoMesh(bracketExtrusionIdsMesh);
                IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                    objectManager,
                    director.IdsDocument,
                    bracketExtrusionIbb,
                    bracketRegionId,
                    bracketExtrusionMesh);
            }

            // register reinforcement extrusion
            foreach (var reinforcementRegionIdAndExtrusion in teethBlockCreator.Output.ReinforcementRegionIdAndExtrusionMap)
            {
                var reinforcementRegionId = reinforcementRegionIdAndExtrusion.Key;
                if (currentDataModel.ReinforcementRegionIdAndExtrusionMap.ContainsKey(reinforcementRegionId))
                {
                    continue;
                }

                var reinforcementExtrusionIdsMesh = reinforcementRegionIdAndExtrusion.Value;
                var reinforcementExtrusionMesh = RhinoMeshConverter.ToRhinoMesh(reinforcementExtrusionIdsMesh);
                IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                    objectManager,
                    director.IdsDocument,
                    reinforcementExtrusionIbb,
                    reinforcementRegionId,
                    reinforcementExtrusionMesh);
            }

            // register teeth base extrusion
            foreach (var teethBaseRegionIdAndExtrusion in teethBlockCreator.Output.TeethBaseRegionIdAndExtrusionMap)
            {
                var teethBaseRegionId = teethBaseRegionIdAndExtrusion.Key;
                if (currentDataModel.TeethBaseRegionIdAndExtrusionMap.ContainsKey(teethBaseRegionId))
                {
                    continue;
                }

                var teethBaseExtrusionIdsMesh = teethBaseRegionIdAndExtrusion.Value;
                var teethBaseExtrusionMesh = RhinoMeshConverter.ToRhinoMesh(teethBaseExtrusionIdsMesh);
                IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                    objectManager,
                    director.IdsDocument,
                    teethBaseExtrusionEIbb,
                    teethBaseRegionId,
                    teethBaseExtrusionMesh);
            }

            // register teeth block Roi
            if (currentDataModel.TeethBlockRoi == null)
            {
                var limitingSurfaceExtrusionIds = objectManager
                    .GetAllBuildingBlockIds(limitingSurfaceExtrusionIbb);
                var bracketExtrusionIds = objectManager
                    .GetAllBuildingBlockIds(bracketExtrusionIbb);
                var teethBlockRoiParentIds = bracketExtrusionIds
                    .Concat(limitingSurfaceExtrusionIds)
                    .ToList();
                IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                    objectManager,
                    director.IdsDocument,
                    teethBlockRoiIbb,
                    teethBlockRoiParentIds,
                    RhinoMeshConverter.ToRhinoMesh(teethBlockCreator.Output.TeethBlockRoi));
            }

            // register Final Support
            if (currentDataModel.FinalSupport == null)
            {
                var teethBlockRoiIds = objectManager
                    .GetAllBuildingBlockIds(teethBlockRoiIbb);
                var limitingSurfaceExtrusionIds = objectManager
                    .GetAllBuildingBlockIds(limitingSurfaceExtrusionIbb);
                var finalSupportParentIds = teethBlockRoiIds
                    .Concat(limitingSurfaceExtrusionIds)
                    .ToList();

                IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                    objectManager,
                    director.IdsDocument,
                    finalSupportIbb,
                    finalSupportParentIds,
                    RhinoMeshConverter.ToRhinoMesh(teethBlockCreator.Output.FinalSupport));
            }

            // register FinalSupportWrapped
            if (currentDataModel.FinalSupportWrapped == null)
            {
                var finalSupportIds = objectManager
                    .GetAllBuildingBlockIds(finalSupportIbb);
                var reinforcementExtrusionIds = objectManager
                    .GetAllBuildingBlockIds(reinforcementExtrusionIbb);
                var finalSupportWrappedParentIds =
                    finalSupportIds.Concat(reinforcementExtrusionIds).ToList();
                IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                    objectManager,
                    director.IdsDocument,
                    finalSupportWrappedIbb,
                    finalSupportWrappedParentIds,
                    RhinoMeshConverter.ToRhinoMesh(teethBlockCreator.Output.FinalSupportWrapped));
            }

            // register teeth block
            if (currentDataModel.TeethBlock == null)
            {
                var finalSupportWrappedIds = objectManager
                    .GetAllBuildingBlockIds(finalSupportWrappedIbb);
                var teethBaseExtrusionIds = objectManager
                    .GetAllBuildingBlockIds(teethBaseExtrusionEIbb);
                var teethBaseParentIds =
                    finalSupportWrappedIds.Concat(teethBaseExtrusionIds).ToList();

                var guideCaseComponent = new GuideCaseComponent();
                var teethBlockEIbb = guideCaseComponent.GetGuideBuildingBlock(
                    IBB.TeethBlock, guidePreferenceDataModel);
                IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                    objectManager,
                    director.IdsDocument,
                    teethBlockEIbb,
                    teethBaseParentIds,
                    RhinoMeshConverter.ToRhinoMesh(teethBlockCreator.Output.TeethBlock));
                guidePreferenceDataModel.Graph.NotifyBuildingBlockHasChanged(
                    new[] { IBB.TeethBlock });
            }
        }

        // Initialize it so that if the parts are previously created, the creator can reuse
        private static TeethBlockCreatorDataModel InitializeTeethBlockCreatorDataModel(
            CMFImplantDirector director,
            IBB limitingSurfaceIbb,
            IBB limitingSurfaceExtrusionIbb,
            IBB bracketRegionIbb,
            IBB bracketExtrusionIbb,
            IBB teethBlockRoiIbb,
            IBB finalSupportIbb,
            IBB reinforcementRegionIbb,
            IBB reinforcementExtrusionIbb,
            ExtendedImplantBuildingBlock teethBaseRegionEIbb,
            ExtendedImplantBuildingBlock teethBaseExtrusionEIbb,
            IBB finalSupportWrappedIbb,
            GuidePreferenceDataModel guidePreferenceDataModel)
        {
            var limitingSurfaceIdAndExtrusions = GetSurfaceIdAndExtrusions(
                director, limitingSurfaceIbb, limitingSurfaceExtrusionIbb);
            var bracketRegionIdAndExtrusions = GetSurfaceIdAndExtrusions(
                director, bracketRegionIbb, bracketExtrusionIbb);
            var reinforcementRegionIdAndExtrusions = GetSurfaceIdAndExtrusions(
                director, reinforcementRegionIbb, reinforcementExtrusionIbb);
            var teethBaseRegionIdAndExtrusions = GetSurfaceIdAndExtrusions(
                director, teethBaseRegionEIbb, teethBaseExtrusionEIbb);

            var teethBlockRoi = GetIdsAndMeshes(director, teethBlockRoiIbb);
            var finalSupport = GetIdsAndMeshes(director, finalSupportIbb);
            var finalSupportWrapped = GetIdsAndMeshes(director, finalSupportWrappedIbb);

            var guideCaseComponent = new GuideCaseComponent();
            var teethBlockEIbb = guideCaseComponent.GetGuideBuildingBlock(IBB.TeethBlock, guidePreferenceDataModel);
            var teethBlock = GetIdsAndMeshes(director, teethBlockEIbb);
            var teethBlockCreatorOutput = new TeethBlockCreatorDataModel()
            {
                BracketRegionIdAndExtrusionMap = bracketRegionIdAndExtrusions,
                TeethBlockRoi = MeshUtilitiesV2.AppendMeshes(teethBlockRoi.Values),
                LimitingSurfaceIdAndExtrusionMap = limitingSurfaceIdAndExtrusions,
                FinalSupport = MeshUtilitiesV2.AppendMeshes(finalSupport.Values),
                ReinforcementRegionIdAndExtrusionMap = reinforcementRegionIdAndExtrusions,
                TeethBaseRegionIdAndExtrusionMap = teethBaseRegionIdAndExtrusions,
                FinalSupportWrapped = MeshUtilitiesV2.AppendMeshes(finalSupportWrapped.Values),
                TeethBlock = MeshUtilitiesV2.AppendMeshes(teethBlock.Values),
            };
            return teethBlockCreatorOutput;
        }

        private static Dictionary<Guid, IMesh> GetSurfaceIdAndExtrusions(
            CMFImplantDirector director, 
            IBB surfaceIbb,
            IBB surfaceExtrusionIbbExpected)
        {
            var surfaceIdAndExtrusions = new Dictionary<Guid, IMesh>();
            var surfaceIds = GetIdsAndMeshes(director, surfaceIbb)
                .Select(x=>x.Key);
            foreach (var surfaceId in surfaceIds)
            {
                var surfaceChildrenIds = director
                    .IdsDocument
                    .GetChildrenInTree(surfaceId);

                var matchingId = Guid.Empty;
                foreach (var surfaceChildId in surfaceChildrenIds)
                {
                    var node = (ObjectValueData) director.IdsDocument.GetNode(surfaceChildId);
                    var ibbStringValue = node.Value.Attributes["IBB"].ToString().Trim('"');
                    if (ibbStringValue == surfaceExtrusionIbbExpected.ToString())
                    {
                        matchingId = surfaceChildId;
                        break;
                    }
                }

                if (matchingId == Guid.Empty)
                {
                    continue;
                }

                // check if the surface extrusion exist
                var surfaceExtrusionObject = director.Document.Objects.Find(matchingId);
                if (surfaceExtrusionObject == null)
                {
                    director.IdsDocument.Delete(matchingId);
                    continue;
                }

                var surfaceExtrusion = RhinoMeshConverter.ToIDSMesh(
                    (Mesh)director.Document.Objects.Find(matchingId).Geometry);
                surfaceIdAndExtrusions.Add(surfaceId, surfaceExtrusion);
            }

            return surfaceIdAndExtrusions;
        }

        private static Dictionary<Guid, IMesh> GetSurfaceIdAndExtrusions(
            CMFImplantDirector director,
            ExtendedImplantBuildingBlock surfaceEIbb,
            ExtendedImplantBuildingBlock surfaceExtrusionEIbb)
        {
            var surfaceIdAndExtrusions = new Dictionary<Guid, IMesh>();
            var surfaceIds = GetIdsAndMeshes(director, surfaceEIbb)
                .Select(x => x.Key);
            foreach (var surfaceId in surfaceIds)
            {
                var surfaceChildrenIds = director
                    .IdsDocument
                    .GetChildrenInTree(surfaceId);

                var matchingId = Guid.Empty;
                foreach (var surfaceChildId in surfaceChildrenIds)
                {
                    var node = (ObjectValueData)director.IdsDocument.GetNode(surfaceChildId);
                    var ibbStringValue = node.Value.Attributes["IBB"].ToString().Trim('"');
                    if (ibbStringValue == surfaceExtrusionEIbb.PartOf.ToString())
                    {
                        matchingId = surfaceChildId;
                        break;
                    }

                }

                if (matchingId == Guid.Empty)
                {
                    continue;
                }

                // check if the surface extrusion exist
                var surfaceExtrusionObject = director.Document.Objects.Find(matchingId);
                // if the surface doesnt exist in rhino, but exist in IdsDocument
                // user did some unlock->delete business and we have to make sure the IdsDocument updates
                if (surfaceExtrusionObject == null)
                {
                    director.IdsDocument.Delete(matchingId);
                    continue;
                }
                var surfaceExtrusion = RhinoMeshConverter.ToIDSMesh(
                    (Mesh)director.Document.Objects.Find(matchingId).Geometry);
                surfaceIdAndExtrusions.Add(surfaceId, surfaceExtrusion);
            }

            return surfaceIdAndExtrusions;
        }

        private static TeethBlockCreatorInput InitializeTeethBlockCreatorInput(
            IConsole console, 
            CMFImplantDirector director,
            IBB limitingSurfaceIbb,
            IBB bracketRegionIbb,
            ExtendedImplantBuildingBlock castEIbb,
            IBB reinforcementRegionIbb,
            ExtendedImplantBuildingBlock teethBaseRegionEIbb)
        {
            var limitingSurfaces = GetIdsAndMeshes(director, limitingSurfaceIbb);
            var bracketRegions = GetIdsAndMeshes(director, bracketRegionIbb);
            var reinforcementRegions = GetIdsAndMeshes(director, reinforcementRegionIbb);
            var teethBaseRegions = GetIdsAndMeshes(director, teethBaseRegionEIbb);
            var teethCast = GetIdsAndMeshes(director, castEIbb);

            var teethBlockCreatorInput = new TeethBlockCreatorInput()
            {
                Console = console,
                BracketRegions = bracketRegions,
                LimitingSurfaces = limitingSurfaces,
                TeethCast = teethCast,
                ReinforcementRegions = reinforcementRegions,
                TeethBaseRegions = teethBaseRegions
            };
            return teethBlockCreatorInput;
        }

        private static Dictionary<Guid, IMesh> GetIdsAndMeshes(CMFImplantDirector director, IBB ibb)
        {
            var objectManager = new CMFObjectManager(director);
            var idsAndMeshes =
                objectManager.GetAllBuildingBlocks(ibb)
                    .ToDictionary(
                        rhinoObject => rhinoObject.Id,
                        rhinoObject => RhinoMeshConverter.ToIDSMesh((Mesh)rhinoObject.Geometry));
            return idsAndMeshes;
        }

        private static Dictionary<Guid, IMesh> GetIdsAndMeshes(
            CMFImplantDirector director, 
            ExtendedImplantBuildingBlock eIbb)
        {
            var objectManager = new CMFObjectManager(director);
            var idsAndMeshes =
                objectManager.GetAllBuildingBlocks(eIbb)
                    .ToDictionary(
                        rhinoObject => rhinoObject.Id,
                        rhinoObject => RhinoMeshConverter.ToIDSMesh((Mesh)rhinoObject.Geometry));
            return idsAndMeshes;
        }
    }
}