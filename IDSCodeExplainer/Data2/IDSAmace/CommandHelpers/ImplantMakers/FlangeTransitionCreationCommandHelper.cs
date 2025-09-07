using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Linq;

namespace IDS.Amace.Operations
{
    public class FlangeTransitionCreationCommandHelper
    {
        public double TransitionWrapResolution { get; set; }
        public double TransitionWrapOffset { get; set; }
        public double TransitionWrapGapClosingDistance { get; set; }
        public bool IsIntersectWithIntersectionEntity { get; set; }
        public bool IsDoPostProcessing { get; set; }
        public ImplantDirector Director { get; set; }


        public FlangeTransitionCreationCommandHelper(ImplantDirector director)
        {
            IsIntersectWithIntersectionEntity = true;
            IsDoPostProcessing = true;
            Director = director;
        }

        public bool CreateFlangeTransition(Mesh[] baseParts, IBB destinationIbb)
        {
            var transitioned = CreateFlangeTransition(baseParts);

            if (transitioned == null)
            {
                return false;
            }

            var objectManager = new AmaceObjectManager(Director);

            var biggerLatCupSubtractor = Director.cup.GetReamingVolumeMesh(Director.cup.innerCupDiameter + 0.1);
            var latSubtractedTransition = Booleans.PerformBooleanSubtraction(transitioned, biggerLatCupSubtractor);

            var screwHoles = objectManager.GetAllIBBInAMeshHelper(false, IBB.ScrewHoleSubtractor);
            var screwHolesSubtractedTransition = Booleans.PerformBooleanSubtraction(latSubtractedTransition, screwHoles);

            //Add to Document
            if (!objectManager.HasBuildingBlock(destinationIbb))
            {
                objectManager.AddNewBuildingBlock(destinationIbb, screwHolesSubtractedTransition);
            }
            else
            {
                var oldIds = objectManager.GetAllBuildingBlockIds(destinationIbb).ToList();
                oldIds.ForEach(id => objectManager.DeleteObject(id));

                objectManager.AddNewBuildingBlock(destinationIbb, screwHolesSubtractedTransition);
            }

            return true;
        }
        
        //return null on failure
        public Mesh CreateFlangeTransition(Mesh[] baseParts)
        {
            //Make Preview
            var objectManager = new AmaceObjectManager(Director);

            var roiCurves = objectManager.GetAllBuildingBlocks(IBB.ROIContour).ToList().Select(c => (Curve)c.Geometry).ToArray();
            var roiBlockGenerator = new RoiBlockGenerator(roiCurves);

            var roiBlock = roiBlockGenerator.GenerateRegionOfInterestBlock();
            if (roiBlock == null)
            {
                return null;
            }

            var basePartMesh = MeshUtilities.UnionMeshes(baseParts);

            Mesh transitioned;
            if (IsIntersectWithIntersectionEntity)
            {
                var intersectionEntity =
                    TransitionIntersectionEntityCommandHelper.HandleGetIntersectionEntity(Director, Constants.ImplantTransitions.IntersectionEntityResolution, true);

                transitioned = TransitionMaker.CreatePlateWithFlangeTransitions(basePartMesh,
                    roiBlock, TransitionWrapResolution, TransitionWrapGapClosingDistance, TransitionWrapOffset, IsDoPostProcessing, intersectionEntity);
            }
            else
            {
                transitioned = TransitionMaker.CreatePlateWithFlangeTransitions(basePartMesh,
                    roiBlock, TransitionWrapResolution, TransitionWrapGapClosingDistance, TransitionWrapOffset, IsDoPostProcessing);
            }

            return transitioned;
        }

    }
}
