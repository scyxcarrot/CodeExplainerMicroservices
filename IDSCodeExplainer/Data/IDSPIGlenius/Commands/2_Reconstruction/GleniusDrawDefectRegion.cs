using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Drawing;
using IDS.Glenius.CommandHelpers;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Glenius.Commands
{
    [
        System.Runtime.InteropServices.Guid("41d9f0e5-3fce-4827-9210-26364b3ccd4b"),
        CommandStyle(Style.ScriptRunner)
    ]
    [IDSGleniusCommand(DesignPhase.Reconstruction, IBB.Scapula)]
    public class GleniusDrawDefectRegion : CommandBase<GleniusImplantDirector>
    {
        //All the curves drawn
        private readonly Dictionary<Guid, Curve> _curves = new Dictionary<Guid, Curve>();

        public GleniusDrawDefectRegion()
        {
            Instance = this;
            VisualizationComponent = new DrawEditDefectRegionVisualization();
        }

        ///<summary>The only instance of the GleniusDrawDefectRegion command.</summary>
        public static GleniusDrawDefectRegion Instance { get; private set; }

        public override string EnglishName => "GleniusDrawDefectRegion";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            //Get scapula mesh
            var objManager = new GleniusObjectManager(director);
            var scapula = objManager.GetBuildingBlock(IBB.Scapula).Geometry as Mesh;

            doc.Views.Redraw();

            //User Input - draw multiple curve on scapula
            while (true)
            {
                // Draw curve
                var dc = new DrawCurve(doc)
                {
                    ConstraintMesh = scapula,
                    AlwaysOnTop = false,
                    UniqueCurves = true
                };
                dc.AcceptNothing(true); // Pressing ENTER is allowed
                dc.AcceptUndo(true); // Enables ctrl-z
                var curve = dc.Draw();

                if (dc.Result() != Rhino.Input.GetResult.Cancel && curve != null)
                {
                    //Remove Healthy and Reconstructed if present [Dependency]
                    var helper = new ReconstructionHelper(director);
                    helper.DeleteExecuteReconstructionRelatedObjects();

                    objManager.AddNewBuildingBlock(IBB.DefectRegionCurves, curve);
                }
                else
                {
                    if (objManager.HasBuildingBlock(IBB.DefectRegionCurves))
                    {
                        break;
                    }

                    return Result.Failure;
                }
            }

            doc.Views.Redraw();

            return Result.Success;
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, GleniusImplantDirector director)
        {
            foreach (var c in _curves)
            {
                doc.Objects.Delete(c.Key, true);
            }
        }
    }
}
