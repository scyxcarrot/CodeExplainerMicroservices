using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Preferences;
using IDS.Core.Enumerators;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Linq;

namespace IDS.Amace.Operations
{
    public static class PlateWithTransitionForExportCreator
    {
        private static Mesh CreateFinalizedImplantWithTransitions(AmaceObjectManager objectManager, Mesh screwBumpTransitions, Mesh flangeToCupTransitionPart, Mesh basePart)
        {
            Mesh combinedTransition;
            if (!Booleans.PerformBooleanUnion(out combinedTransition,
                new[] { screwBumpTransitions, flangeToCupTransitionPart }))
            {
                return null;
            }

            Mesh rawCombinedTransition;
            if (!Booleans.PerformBooleanUnion(out rawCombinedTransition,
                new[] { combinedTransition, basePart }))
            {
                return null;
            }

            var latCupSubtractor = objectManager.GetAllIBBInAMeshHelper(true, IBB.LateralCupSubtractor);
            var screwHoleSubtractor = objectManager.GetAllIBBInAMeshHelper(true, IBB.ScrewHoleSubtractor);
            var latCupSubtractedResult = Booleans.PerformBooleanSubtraction(rawCombinedTransition, latCupSubtractor);
            var finalizedImplantWithTransition = Booleans.PerformBooleanSubtraction(latCupSubtractedResult, screwHoleSubtractor);
            finalizedImplantWithTransition = AutoFix.RemoveNoiseShells(finalizedImplantWithTransition);
            return finalizedImplantWithTransition;
        }

        private static ScrewBumpTransitionModel CreateActualScrewBumpTransition(ImplantDirector director, AmaceObjectManager objectManager, bool isUseRoundedPlate)
        {
            var param = AmacePreferences.GetTransitionActualParameters();

            //ScrewBump Transition
            var screwBumpTransitionHelper = new ScrewBumpTransitionCreationCommandHelper(director)
            {
                TransitionOffset = param.ScrewBumpsTransitionParams.Parameters.WrapOperationOffset,
                GapClosingDistance = param.ScrewBumpsTransitionParams.Parameters.WrapOperationGapClosingDistance,
                TransitionResolution = param.ScrewBumpsTransitionParams.Parameters.WrapOperationSmallestDetails,
                RoiOffset = param.ScrewBumpsTransitionParams.RoiOffset
            };

            var medBumps = objectManager.GetAllBuildingBlocks(IBB.MedialBumpTrim).Select(x => (Mesh)x.Geometry);

            var plateMesh = isUseRoundedPlate? (Mesh)objectManager.GetBuildingBlock(IBB.SolidPlateRounded).Geometry 
                : (Mesh)objectManager.GetBuildingBlock(IBB.SolidPlate).Geometry;

            var medBumpsCombined = objectManager.GetAllIBBInAMeshHelper(false, IBB.MedialBumpTrim);

            var screwBumpTransition = screwBumpTransitionHelper.CreateScrewBumpTransition(new[] { plateMesh, director.cup.filledCupMesh, medBumpsCombined },
                    medBumps.ToArray());
            screwBumpTransition.ScrewBumpTransitions = AutoFix.RemoveNoiseShells(screwBumpTransition.ScrewBumpTransitions);
            return screwBumpTransition;
        }

        private static Mesh CreateActualFlangeToCupTransition(ImplantDirector director, AmaceObjectManager objectManager, bool isUseRoundedPlate)
        {
            var param = AmacePreferences.GetTransitionActualParameters();

            //Flange Transition
            var flangeTransitionHelper = new FlangeTransitionCreationCommandHelper(director)
            {
                TransitionWrapResolution = param.FlangesTransitionParams.WrapOperationSmallestDetails,
                TransitionWrapOffset = param.FlangesTransitionParams.WrapOperationOffset,
                TransitionWrapGapClosingDistance = param.FlangesTransitionParams.WrapOperationGapClosingDistance,
                IsDoPostProcessing = true,
                IsIntersectWithIntersectionEntity = true
            };

            var plateMesh = isUseRoundedPlate ? (Mesh)objectManager.GetBuildingBlock(IBB.SolidPlateRounded).Geometry
                : (Mesh)objectManager.GetBuildingBlock(IBB.SolidPlate).Geometry;

            var flangeTransition = flangeTransitionHelper.CreateFlangeTransition(new[] { plateMesh, director.cup.filledCupMesh });
            flangeTransition = AutoFix.RemoveNoiseShells(flangeTransition);
            return flangeTransition;
        }

        public static Mesh CreateForImplantQc(ImplantDirector director)
        {
            var objectManager = new AmaceObjectManager(director);

            var flangesWithTransition = CreateActualFlangeToCupTransition(director, objectManager, false);
            var screwBumpWithTransition = CreateActualScrewBumpTransition(director, objectManager, false);

            return CreateFinalizedImplantWithTransitions(objectManager, 
                screwBumpWithTransition.ScrewBumpTransitions, flangesWithTransition,
                screwBumpWithTransition.BaseModelInput);
        }

        public static QcApprovedExportTransitionModel CreateForQcApproved(ImplantDirector director)
        {
            var objectManager = new AmaceObjectManager(director);
            var result = new QcApprovedExportTransitionModel();

            var flangesToCupTransition = CreateActualFlangeToCupTransition(director, objectManager, true);
            var screwBumpWithTransition = CreateActualScrewBumpTransition(director, objectManager, true);

            result.FlangeTransitionForFinalization = flangesToCupTransition.DuplicateMesh();
            result.BumpTransitionForFinalization = screwBumpWithTransition.ScrewBumpTransitions;

            result.PlateWithTransitionForReporting = CreateFinalizedImplantWithTransitions(objectManager, 
                screwBumpWithTransition.ScrewBumpTransitions, flangesToCupTransition, screwBumpWithTransition.BaseModelInput);

            return result;
        }

        public static Mesh CreatePlateWithTransition(DocumentType currentDocumentType, ImplantDirector director)
        {
            Mesh plateWithTransition;

            switch (currentDocumentType)
            {
                case DocumentType.ImplantQC:
                    plateWithTransition = CreateForImplantQc(director);
                    break;
                case DocumentType.Export: //Approved Export
                    var res = CreateForQcApproved(director);
                    plateWithTransition = res.PlateWithTransitionForReporting;
                    break;
                default:
                    throw new Exception("Could not get plate with transition");
            }

            return plateWithTransition;
        }
    }
}
