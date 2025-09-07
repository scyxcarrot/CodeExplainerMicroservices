using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.Factory;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.V2.MTLS.Operation;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using IDS.RhinoInterface.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("31E6D717-F008-47F2-A080-B49A8275B296")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideSurfaceWrap)]
    public class CMFCreateGuideBridge : CmfCommandBase
    {
        public CMFCreateGuideBridge()
        {
            TheCommand = this;
            VisualizationComponent = new CMFGuideBridgeVisualization();
        }

        public static CMFCreateGuideBridge TheCommand { get; private set; }

        public CMFGuidePrefPanelVisualizationHelper GuidePrefPanelVisualizationHelper =
            new CMFGuidePrefPanelVisualizationHelper();

        public override string EnglishName => "CMFCreateGuideBridge";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var guideCaseGuid = GuidePreferencesHelper.PromptForPreferenceId();

            if (guideCaseGuid == Guid.Empty)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Guide preference not found!");
                return Result.Failure;
            }

            var objectManager = new CMFObjectManager(director);
            var guidePrefModel = objectManager.GetGuidePreference(guideCaseGuid);

            GuidePrefPanelVisualizationHelper.GuidePrefPanelOpVisualization(guidePrefModel, doc, false, true);
            
            var rhObject = objectManager.GetBuildingBlock(IBB.GuideSurfaceWrap);
            Mesh lowLoDConstraintMesh;
            objectManager.GetBuildingBlockLoDLow(rhObject.Id, out lowLoDConstraintMesh);

            // add the teeth block to allow user to create bridge to teeth block
            var guideCaseComponent = new GuideCaseComponent();
            var teethBuildingBlock = guideCaseComponent.GetGuideBuildingBlock(IBB.TeethBlock, guidePrefModel);
            var teethBlockObjects = objectManager.GetAllBuildingBlocks(teethBuildingBlock);

            var guideBridgeDrawConstraintMesh = lowLoDConstraintMesh;
            if (teethBlockObjects.Any())
            {
                var teethBlockObjectIdsMeshes = teethBlockObjects
                    .Select(teethBlockObject =>
                        RhinoMeshConverter.ToIDSMesh((Mesh)teethBlockObject.Geometry));
                var console = new IDSRhinoConsole();
                BooleansV2.PerformBooleanUnion(console,
                    out var teethBlockIdsMesh, teethBlockObjectIdsMeshes.ToArray());

                var lowLoDConstraintIdsMesh = RhinoMeshConverter
                    .ToIDSMesh(lowLoDConstraintMesh);
                BooleansV2.PerformBooleanUnion(console,
                    out var guideBridgeDrawConstraintMeshIds, new[] { teethBlockIdsMesh, lowLoDConstraintIdsMesh });
                guideBridgeDrawConstraintMesh = RhinoMeshConverter
                    .ToRhinoMesh(guideBridgeDrawConstraintMeshIds);
            }

            var helper = new GuideBridgeCreatorHelper();
            var success = helper.DrawBridge(guideBridgeDrawConstraintMesh,
                CMFPreferences.GetGuideBridgeParameters().DefaultDiameter,
                CMFPreferences.GetGuideBridgeParameters().MinimumDiameter,
                CMFPreferences.GetGuideBridgeParameters().MaximumDiameter);
            if (success)
            {
                var guideBridgeBrepFactory = new GuideBridgeBrepFactory(helper.BridgeType, helper.BridgeGenio);

                var bridge = guideBridgeBrepFactory.CreateGuideBridgeWithRatio(helper.StartPoint, 
                                                                                    helper.EndPoint, 
                                                                                    helper.UpDirection,
                                                                                    diameter: helper.BridgeDiameter
                                                                                    );

                var buildingBlock = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideBridge, guidePrefModel);

                var bridgeCs = GuideBridgeUtilities.CreateBridgeCoordinateSystem(helper.StartPoint, helper.EndPoint, helper.UpDirection);

                if (helper.BridgeType == GuideBridgeType.OctagonalBridge)
                {
                    bridge.UserDictionary.Set(AttributeKeys.KeyGuideBridgeType, GuideBridgeType.OctagonalBridge);
                    bridge.UserDictionary.Set(AttributeKeys.KeyGuideBridgeGenio, helper.BridgeGenio);
                    bridge.UserDictionary.Set(AttributeKeys.KeyGuideBridgeDiameter, helper.BridgeDiameter);
                }
                
                objectManager.AddNewBuildingBlockWithCoordinateSystem(buildingBlock, bridge, bridgeCs);

                guidePrefModel.Graph.InvalidateGraph();
                guidePrefModel.Graph.NotifyBuildingBlockHasChanged(new[] {IBB.GuideBridge});
            }

            return success ? Result.Success : Result.Failure;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            GuidePrefPanelVisualizationHelper.RestoreVisualisation(doc, false);
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteCanceled(RhinoDoc doc, CMFImplantDirector director)
        {
            GuidePrefPanelVisualizationHelper.RestoreVisualisation(doc, false);
            doc.Views.Redraw();
        }
    }
}
