using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using IDSPIGlenius.Commands.Shared;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("8979fbe2-7d5c-4e77-987a-6746fa6820c3")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScaffoldPrimaryBorder, IBB.BasePlateBottomContour, IBB.ScaffoldSide, IBB.SolidWallWrap, IBB.SolidWallCurve)]
    public class GleniusEditSolidWall : CommandBase<GleniusImplantDirector>
    {
        static GleniusEditSolidWall _instance;
        public GleniusEditSolidWall()
        {
            _instance = this;
            VisualizationComponent = new SolidWallGenericVisualization();
        }

        ///<summary>The only instance of the GleniusEditSolidWall command.</summary>
        public static GleniusEditSolidWall Instance => _instance;

        public override string EnglishName => "GleniusEditSolidWall";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            SolidWallHelper.SetForCurveManipulation(true);
            var objManager = new GleniusObjectManager(director);
            var scaffoldSide = objManager.GetBuildingBlock(IBB.ScaffoldSide).Geometry as Mesh;
            var primaryBorder = objManager.GetBuildingBlock(IBB.ScaffoldPrimaryBorder).Geometry as Curve;
            var basePlateBottomContour = objManager.GetBuildingBlock(IBB.BasePlateBottomContour).Geometry as Curve;

            Guid selectedSolidWallCurveId;
            Curve selectedSolidWallCurve;

            Locking.UnlockSolidWallWrap(doc);

            var getObject = new GetObject();
            getObject.SetCommandPrompt("Select SolidWallWrap to edit");
            getObject.EnablePreSelect(false, false);
            getObject.EnablePostSelect(true);
            getObject.AcceptNothing(true);
            getObject.EnableHighlight(false);
            getObject.EnableTransparentCommands(false);

            var goResult = getObject.Get();
            doc.Objects.UnselectAll();

            if (goResult == GetResult.Object)
            {
                selectedSolidWallCurveId = director.SolidWallObjectManager.GetSolidWallCurveId(getObject.Object(0).ObjectId);
                selectedSolidWallCurve =(Curve)doc.Objects.Find(selectedSolidWallCurveId).Geometry;
            }
            else
            {
                return Result.Failure;
            }

            Locking.UnlockSolidWallCurve(doc);

            var creator = new SolidWallCreator(doc, basePlateBottomContour, primaryBorder, scaffoldSide);

            KeyValuePair<Curve, Mesh> editedSolidWall;
            var res = creator.EditSolidWall(selectedSolidWallCurve.DuplicateCurve(), out editedSolidWall);

            if (res == SolidWallCreator.EResult.Success)
            {
                director.SolidWallObjectManager.ReplaceSolidWall(selectedSolidWallCurveId, editedSolidWall.Key,
                    editedSolidWall.Value);

                //Because when ReplaceSolidWall, both of these two got modified/deleted
                director.Graph.NotifyBuildingBlockHasChanged(IBB.SolidWallCurve, IBB.SolidWallWrap);
                return Result.Success;
            }

            if (res == SolidWallCreator.EResult.Failed)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "The Solid Wall cannot be created. Please adjust its borders.");
            }

            return Result.Failure;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, GleniusImplantDirector director)
        {
            SolidWallHelper.LoadSavedOsnapSettings();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, GleniusImplantDirector director)
        {
            SolidWallHelper.LoadSavedOsnapSettings();
        }

        public override void OnCommandExecuteCanceled(RhinoDoc doc, GleniusImplantDirector director)
        {
            SolidWallHelper.LoadSavedOsnapSettings();
        }

    }
}
