using IDS.Amace.CommandHelpers;
using IDS.Amace.Constants;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Common;
using IDS.Common.Visualization;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using System.Linq;
using View = IDS.Amace.Visualization.View;

namespace IDS.Amace.Commands
{
    [System.Runtime.InteropServices.Guid("E9507CA5-F7DE-4387-98B8-39D59FB95A55")]
    [IDSCommandAttributes(true, DesignPhase.Plate, IBB.ROIContour, IBB.TransitionPreview)]
    public class EditRegionOfInterest : RegionOfInterestCommand
    {
        public EditRegionOfInterest()
        {
            TheCommand = this;
        }
        
        public static EditRegionOfInterest TheCommand { get; private set; }
        
        public override string EnglishName => "EditRegionOfInterest";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            Visualization.Visibility.EditRegionOfInterest(doc);

            var objectManager = new AmaceObjectManager(director);
            var oldContourId = objectManager.GetBuildingBlockId(IBB.ROIContour); //only one at the moment
            
            var helper = new RegionOfInterestCommandHelper(objectManager);
            director.ContourPlane = helper.GetContourPlaneBasedOnRoiCurve(oldContourId, director.ContourPlane);

            View.SetContourPlaneView(doc);

            var constraintPlane = new Plane(director.ContourPlane);
            var roiCurve = ((Curve)objectManager.GetAllBuildingBlocks(IBB.ROIContour).First(c => c.Id == oldContourId).Geometry).DuplicateCurve();

            var editRoi = new EditRoi(doc, constraintPlane, ContourPlane.Size, roiCurve);
            editRoi.AcceptNothing(true); // Pressing ENTER is allowed
            editRoi.SetCommandPrompt("Drag points to adjust the curve");
            var contour = editRoi.Draw();
            var result = editRoi.Result();

            Visibility.SetHidden(doc, BuildingBlocks.Blocks[IBB.ROIContour].Layer);

            if (result != GetResult.Nothing)
            {
                return Result.Cancel;
            }
            
            director.ContourPlane = editRoi.GetConstraintPlane();

            helper.SetRoiContour(contour, director.ContourPlane, oldContourId);

            var dependencies = new Dependencies();
            var success = dependencies.UpdateTransitionPreview(director, true);

            return success ? Result.Success : Result.Failure;
        }
    }
}