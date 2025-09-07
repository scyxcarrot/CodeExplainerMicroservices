using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.Interaction;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI.Gumball;
using System;
using System.Collections.Generic;
using System.Linq;
using Plane = Rhino.Geometry.Plane;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("F6AD98DB-43F1-43A6-B21D-3FAB32E89D5F")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideBridge)]
    public class CMFEditGuideBridge : CmfCommandBase
    {
        public override string EnglishName => "CMFEditGuideBridge";
        public static CMFEditGuideBridge TheCommand { get; private set; }
        public CMFEditGuideBridge()
        {
            TheCommand = this;
            VisualizationComponent = new CMFGuideBridgeVisualization();
        }

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            IDS.CMF.Operations.Locking.UnlockGuideBridge(doc);

            var selectBridgeBlock = new GetObject();
            selectBridgeBlock.SetCommandPrompt("Select bridge to transform.");
            selectBridgeBlock.EnablePreSelect(false, false);
            selectBridgeBlock.EnablePostSelect(true);
            selectBridgeBlock.AcceptNothing(true);
            selectBridgeBlock.EnableTransparentCommands(false);
            selectBridgeBlock.EnableHighlight(false);

            var objectManager = new CMFObjectManager(director);

            // Get user input
            GetResult res = selectBridgeBlock.Get();

            if ((res == GetResult.Nothing) || (res == GetResult.Cancel))
            {
                return Result.Failure;
            }

            if (res == GetResult.Object)
            {
                // Get selected objects
                List<RhinoObject> selectedBridgeBlocks = doc.Objects.GetSelectedObjects(false, false).ToList();
                RhinoObject rhobj = selectedBridgeBlocks[0];
                Brep bridge = (Brep)rhobj.Geometry;

                var startEndPtList = GuideBridgeUtilities.GetStartEndPoints(bridge);
                if (startEndPtList.Capacity < 2)
                {
                    return Result.Failure;
                }

                Plane cs;
                if (!objectManager.GetBuildingBlockCoordinateSystem(rhobj.Id, out cs))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Selected guide bridge does not have coordinate system, please recreate the bridge.");
                    return Result.Failure;
                }

                var appearance = new GumballAppearanceSettings
                {
                    ScaleXEnabled = false,
                    ScaleYEnabled = false,
                    ScaleZEnabled = false,
                    ScaleGripSize = 8
                };

                var allowKeyboardEvents = false;
                var commandPrompt = "Drag gumball. Press Enter when done. ";
                bridge.UserDictionary.TryGetDouble(
                    AttributeKeys.KeyGuideBridgeDiameter, 
                    out var previousDiameter);
                if (bridge.UserDictionary.TryGetString(AttributeKeys.KeyGuideBridgeType, out var bridgeType))
                {
                    if (bridgeType == GuideBridgeType.OctagonalBridge)
                    {
                        allowKeyboardEvents = true;
                        commandPrompt += "Adjust bridge diameter with the +/- key (Only during TransformObject mode).";
                    }
                }

                var gTransform = new GumballTransformGuideBridge(director, allowKeyboardEvents, commandPrompt);
                var objectTransform = gTransform.TransformGuideBridge(rhobj.Id, appearance, cs);

                if (bridgeType == GuideBridgeType.OctagonalBridge && 
                    objectTransform == Transform.Identity && 
                    Math.Abs(
                        gTransform.GuideBridgeAfterOperation
                            .UserDictionary.GetDouble(
                                AttributeKeys.KeyGuideBridgeDiameter) - 
                        previousDiameter) < 0.01)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, 
                        "There are no changes in guide bridge");
                    return Result.Failure;
                }

                if ((bridgeType == string.Empty || bridgeType == GuideBridgeType.HexagonalBridge) && objectTransform == Transform.Identity)
                {
                    return Result.Failure;
                }

                var csNew = cs;
                csNew.Transform(objectTransform);
                objectManager.SetBuildingBlockCoordinateSystem(rhobj.Id, csNew);

                var objManager = new CMFObjectManager(director);
                var casePref = objManager.GetGuidePreference(rhobj);
                casePref.Graph.NotifyBuildingBlockHasChanged(new[] {IBB.GuideBridge});
                return Result.Success;
            }

            return Result.Success;
        }
    }
}
