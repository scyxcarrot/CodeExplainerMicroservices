using IDS.Amace.CommandHelpers;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Common;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Commands;
using System.Linq;

namespace IDS.Amace.Commands
{
    [System.Runtime.InteropServices.Guid("48714B7C-93EB-400B-A2D6-7CD73ED4732E")]
    [IDSCommandAttributes(true, DesignPhase.Plate, IBB.ROIContour, IBB.TransitionPreview)]
    public class ResetRegionOfInterest : RegionOfInterestCommand
    {
        public ResetRegionOfInterest()
        {
            TheCommand = this;
        }
        
        public static ResetRegionOfInterest TheCommand { get; private set; }
        
        public override string EnglishName => "ResetRegionOfInterest";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            var objectManager = new AmaceObjectManager(director);
            var oldContourId = objectManager.GetBuildingBlockId(IBB.ROIContour); //only one at the moment
            
            var helper = new RegionOfInterestCommandHelper(objectManager);
            director.ContourPlane = helper.GetContourPlaneBasedOnAcetabularPlane(doc, director.cup.cupRimPlane.Origin);

            var roiCurves = helper.GetRoiContourBasedOnSkirtBoneCurve(director.ContourPlane);
            helper.SetRoiContour(roiCurves.First(), director.ContourPlane, oldContourId);

            var dependencies = new Dependencies();
            var success = dependencies.UpdateTransitionPreview(director, true);

            Visibility.SetHidden(doc, BuildingBlocks.Blocks[IBB.ROIContour].Layer);

            doc.Views.Redraw();

            return success ? Result.Success : Result.Failure;
        }
    }
}