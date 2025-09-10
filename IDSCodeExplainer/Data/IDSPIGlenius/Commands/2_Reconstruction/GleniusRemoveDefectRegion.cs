using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Glenius.CommandHelpers;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using IDSCore.Common;
using IDSCore.Glenius.Drawing;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("d4963250-e747-42ff-9427-c5d30ea0ba3c")]
    [IDSGleniusCommand(DesignPhase.Reconstruction, IBB.Scapula, IBB.DefectRegionCurves)]
    public class GleniusRemoveDefectRegion : CommandBase<GleniusImplantDirector>
    {
        static GleniusRemoveDefectRegion _instance;
        public GleniusRemoveDefectRegion()
        {
            _instance = this;
            VisualizationComponent = new RemoveDefectRegionVisualization();
        }

        ///<summary>The only instance of the GleniusRemoveDefectRegion command.</summary>
        public static GleniusRemoveDefectRegion Instance => _instance;

        public override string EnglishName => "GleniusRemoveDefectRegion";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            //Get scapula mesh
            var objManager = new GleniusObjectManager(director);
            var scapula = objManager.GetBuildingBlock(IBB.Scapula).Geometry as Mesh;

            //Split surface and get defect regions
            var scapulaCopy = new Mesh();
            scapulaCopy.CopyFrom(scapula);

            var regionCurves = objManager.GetAllBuildingBlocks(IBB.DefectRegionCurves).Select(x => (Curve)x.Geometry).ToList();

            var scapulaPartsSorted = MeshOperations.SplitMeshWithCurves(scapulaCopy, regionCurves, true);
            if (scapulaPartsSorted == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "At least one curve fail to split a surface. Please re-adjust curve.");
                return Result.Failure;
            }

            if (scapulaPartsSorted.Count < 2)
            {
                return Result.Failure;
            }

            var biggestPart = scapulaPartsSorted.LastOrDefault();
            if (biggestPart == null)
            {
                return Result.Failure;
            }

            //Define the OK part and non OK part
            var nonDefectParts = new List<Mesh>() { biggestPart.DuplicateMesh() };
            scapulaPartsSorted.RemoveAt(scapulaPartsSorted.Count - 1);
            var defectParts = scapulaPartsSorted;

            //Perform Region Selection
            var defectRegionConduits = new DefectRegionMeshConduit
            {
                Enabled = true,
                DefectRegions = defectParts,
                NonDefectRegions = nonDefectParts,
                DrawnDefectCurves = regionCurves.ToList()
            };
            RhinoDoc.ActiveDoc.Views.Redraw();

            var handleSelection = new HandleDefectRegionMeshConduit(defectRegionConduits);
            handleSelection.AcceptNothing(true);
            handleSelection.EnableTransparentCommands(false);
            while (true)
            {
                var res = handleSelection.Get(true);

                if (res == GetResult.Cancel)
                {
                    //Add back removed one
                    defectRegionConduits.Enabled = false;
                    return Result.Cancel;
                }

                if (defectRegionConduits.NonDefectRegions.Count == 0)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "At least one surface should be unmarked.");
                    continue;
                }

                if (res == GetResult.Nothing)
                {
                    break;
                }

            }

            //Remove original if present [Dependency]
            var helper = new ReconstructionHelper(director);
            helper.DeleteExecuteReconstructionRelatedObjects();

            defectRegionConduits.Enabled = false;

            //Add final desired result into document
            var finalResultScapula = MeshUtilities.AppendMeshes(defectRegionConduits.NonDefectRegions);
            objManager.AddNewBuildingBlock(IBB.ScapulaDefectRegionRemoved, finalResultScapula);

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}
