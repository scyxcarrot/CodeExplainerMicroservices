using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Drawing;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("1652c41f-a2f0-4052-b4d5-09df3efb6a02")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScaffoldGuides)]
    public class GleniusEditScaffoldGuide : CommandBase<GleniusImplantDirector>
    {
        public GleniusEditScaffoldGuide()
        {
            Instance = this;
            VisualizationComponent = new ScaffoldGuideGenericVisualization();
        }

        ///<summary>The only instance of the GleniusEditScaffoldGuide command.</summary>
        public static GleniusEditScaffoldGuide Instance { get; private set; }

        public override string EnglishName => "GleniusEditScaffoldGuide";

        private List<Tuple<Guid,Curve>> _originalGuideCurves;
        private GleniusObjectManager _objectManager;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            _originalGuideCurves = new List<Tuple<Guid, Curve>>();
            _objectManager = new GleniusObjectManager(director);

            foreach (var obj in _objectManager.GetAllBuildingBlocks(IBB.ScaffoldGuides))
            {
                _originalGuideCurves.Add(new Tuple<Guid, Curve>(obj.Id, ((Curve)obj.Geometry).DuplicateCurve()));
            }

            while (true)
            {
                Locking.UnlockScaffoldGuides(doc);
                RhinoDoc.ActiveDoc.Views.Redraw();

                var getObject = new GetObject();
                getObject.SetCommandPrompt("Select Scaffold guide curves to Edit, Enter to accept, or Esc to cancel changes");
                getObject.EnablePreSelect(false, false);
                getObject.EnablePostSelect(true);
                getObject.AcceptNothing(true);
                getObject.EnableTransparentCommands(false);

                var res = getObject.Get();

                if (res == GetResult.Object)
                {
                    var selectedCurveId = getObject.Object(0).ObjectId;
                    var curveToEdit = doc.Objects.Find(selectedCurveId).Geometry as Curve;

                    var drawConnectionCurve = new ConnectionCurveCreator(doc);
                    var editedCurve = drawConnectionCurve.Edit(curveToEdit);

                    if (drawConnectionCurve.Result() == GetResult.Cancel)
                    {
                        return Result.Failure;
                    }

                    if (drawConnectionCurve.Result() == GetResult.Nothing)
                    {
                        break;
                    }

                    if (editedCurve != null)
                    {
                        _objectManager.SetBuildingBlock(IBB.ScaffoldGuides, editedCurve, selectedCurveId);
                    }

                }
                else if (res == GetResult.Nothing)
                {
                    break;
                }
                else
                {
                    return Result.Failure;
                }
            }

            director.Graph.NotifyBuildingBlockHasChanged(IBB.ScaffoldGuides);
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, GleniusImplantDirector director)
        {
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, GleniusImplantDirector director)
        {
            foreach (var c in _originalGuideCurves)
            {
                _objectManager.SetBuildingBlock(IBB.ScaffoldGuides, c.Item2, c.Item1);
            }

            doc.Views.Redraw();
        }
    }
}
