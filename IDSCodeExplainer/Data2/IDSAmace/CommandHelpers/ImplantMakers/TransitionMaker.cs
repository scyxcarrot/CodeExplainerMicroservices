using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Amace.Operations
{
    public static class TransitionMaker
    {
        //Screw Bump Transitions
        //Return Null on failure
        public static ScrewBumpTransitionModel CreateScrewBumpTransition(ImplantDirector director, Mesh[] baseParts, Mesh[] medialBumps,
            double roiOffset, double transitionResolution, double gapClosingDistance, double transitionOffset)
        {
            return CreateScrewBumpTransition(director, baseParts, medialBumps, null, roiOffset,
                transitionResolution, gapClosingDistance, transitionOffset);
        }

        //Return Null on failure
        public static ScrewBumpTransitionModel CreateScrewBumpTransition(ImplantDirector director, Mesh[] baseParts,
            Mesh[] medialBumps, Mesh intersectionEntity, double roiOffset, double transitionResolution,
            double gapClosingDistance, double transitionOffset)
        {
            var result = new ScrewBumpTransitionModel();

            var basePartMesh = MeshUtilities.UnionMeshes(baseParts);
            result.BaseModelInput = basePartMesh.DuplicateMesh();
            var toDoTransition = new List<Mesh>();

            try
            {
                medialBumps.ToList().ForEach(x =>
                {
                    Mesh w;

                    if (!Wrap.PerformWrap(new[] { x.DuplicateMesh() }, 0.8, 0.0, roiOffset, false, true, false, false, out w))
                    {
                        throw new IDSException("CreateScrewBumpTransition > PerformWrap failed at making medial Bumps Wrap");
                    }

                    var roi = Booleans.PerformBooleanIntersection(basePartMesh.DuplicateMesh(), w);

                    Mesh r;
                    if (!Booleans.PerformBooleanUnion(out r, roi, x.DuplicateMesh()))
                    {
                        throw new IDSException("CreateScrewBumpTransition > PerformBooleanUnion failed at making transition base for toDoTransition");
                    }
                    toDoTransition.Add(r);
                });

                var screwBumpTransitionsInRoiRaw = new List<Mesh>();
                toDoTransition.ForEach(x =>
                {
                    Mesh r;
                    if (!Wrap.PerformWrap(new[] { x }, transitionResolution, gapClosingDistance, transitionOffset, false, true, false, false, out r))
                    {
                        throw new IDSException("CreateScrewBumpTransition > PerformWrap failed at making transition");
                    }
                    screwBumpTransitionsInRoiRaw.Add(r.DuplicateMesh());
                });

                var screwBumpTransitions = new List<Mesh>();
                screwBumpTransitionsInRoiRaw.ForEach(x =>
                {
                    var finalBumpInRoi = intersectionEntity != null
                        ? Booleans.PerformBooleanIntersection(x, intersectionEntity.DuplicateMesh()) : x;

                    screwBumpTransitions.Add(finalBumpInRoi);
                });

                result.ScrewBumpTransitions = MeshUtilities.UnionMeshes(screwBumpTransitions);

                return result;
            }
            catch
            {
                return null;
            }
        }

        //Flanges Transition
        public static Mesh CreatePlateWithFlangeTransitions(Mesh basePart, Brep roi, double resolution,
            double gapClosingDistance, double offset, bool doPostProcessing)
        {
            return CreatePlateWithFlangeTransitions(basePart, roi, resolution, gapClosingDistance, offset, doPostProcessing, null);
        }

        public static Mesh CreatePlateWithFlangeTransitions(Mesh basePart, Brep roi, double resolution,
            double gapClosingDistance, double offset, bool doPostProcessing, Mesh intersectionEntity)
        {

            var basePartDuplicate = basePart.DuplicateMesh();
            var areaOfInterestBase = MeshUtilities.AppendMeshes(Mesh.CreateFromBrep(roi));
            var areaOfInterest = areaOfInterestBase;
            var plateOptimized = Booleans.PerformBooleanIntersection(basePartDuplicate, areaOfInterest);

            Mesh transitionedRaw;

            if (!Wrap.PerformWrap(new[] { plateOptimized }, resolution, gapClosingDistance, offset, false, true, false, false,
                out transitionedRaw))
            {
                return null;
            }

            var plateWithTransition = transitionedRaw;

            if (doPostProcessing)
            {

                var remeshed = Remesh.PerformRemesh(transitionedRaw, 0.9, 1.8, 0.2, 0.05, 0.4, false, 3);
                plateWithTransition = ExternalToolInterop.PerformSmoothing(remeshed, true, true, false, 30.0, 0.7, 3);
            }

            var finalMesh = plateWithTransition;
            if (intersectionEntity != null)
            {
                finalMesh = Booleans.PerformBooleanIntersection(plateWithTransition, intersectionEntity);
            }
            return finalMesh;
        }
    }
}