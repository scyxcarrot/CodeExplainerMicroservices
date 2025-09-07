using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Plane = Rhino.Geometry.Plane;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("D6F03FB0-3564-48D6-96C2-E52956967453")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.GuideBridge)]
    public class CMF_TestExportGuideBridge : CmfCommandBase
    {
        public CMF_TestExportGuideBridge()
        {
            Instance = this;
        }

        public static CMF_TestExportGuideBridge Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportGuideBridge";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var bridge = SelectBridgeToExport(doc);
            if (bridge == null)
            {
                return Result.Failure;
            }

            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            if (!ExportBridge(director, workingDir, bridge))
            {
                return Result.Failure;
            }

            SystemTools.OpenExplorerInFolder(workingDir);

            return Result.Success;
        }

        private RhinoObject SelectBridgeToExport(RhinoDoc doc)
        {
            Locking.UnlockGuideBridge(doc);

            var selectBridge = new GetObject();
            selectBridge.SetCommandPrompt("Select bridge to export.");
            selectBridge.EnablePreSelect(false, false);
            selectBridge.EnablePostSelect(true);
            selectBridge.AcceptNothing(true);

            while (true)
            {
                var res = selectBridge.Get();

                switch (res)
                {
                    case GetResult.Cancel:
                    case GetResult.Nothing:
                        return null;
                    case GetResult.Object:
                        var rhinoObj = selectBridge.Object(0).Object();
                        return rhinoObj;
                }
            }
        }

        private bool ExportBridge(CMFImplantDirector director, string exportDir, RhinoObject bridgeObj)
        {
            var objectManager = new CMFObjectManager(director);

            Plane cs;
            if (!objectManager.GetBuildingBlockCoordinateSystem(bridgeObj.Id, out cs))
            {
                RhinoApp.WriteLine("Failed to get coordinate system for selected bridge!");
                return false;
            }

            var parameters = CMFPreferences.GetActualGuideParameters();
            var bridgeBrep = (Brep)bridgeObj.Geometry.Duplicate();

            bridgeBrep.UserDictionary.TryGetString(AttributeKeys.KeyGuideBridgeType, out var bridgeType);
            var lwBridge = GuideBridgeUtilities.GenerateGuideBridgeWithLightweightFromBrep(bridgeBrep, cs,
                parameters.LightweightParams.SegmentRadius, parameters.LightweightParams.FractionalTriangleEdgeLength,
                bridgeType == GuideBridgeType.OctagonalBridge
                    ? parameters.LightweightParams.OctagonalBridgeCompensation
                    : 0.0);
            var bridgeMesh = MeshUtilities.ConvertBrepToMesh(bridgeBrep, true);

            var diameter = GuideBridgeUtilities.GetGuideBridgeRadius(bridgeBrep, cs) * 2;
            StlUtilities.RhinoMesh2StlBinary(bridgeMesh, $"{exportDir}\\bridgeFromBrep-{diameter}D.stl");
            StlUtilities.RhinoMesh2StlBinary(lwBridge, $"{exportDir}\\bridgeWithLightweight-{diameter}D.stl");

            return true;
        }
    }

#endif
}
