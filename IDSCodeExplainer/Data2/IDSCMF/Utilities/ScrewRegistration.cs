using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.RhinoInterfaces.Converter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class ScrewRegistration : IDisposable
    {
        private readonly CMFImplantDirector _director;
        private readonly bool _skipRegistrationIfScrewOnGraft;

        private readonly List<MeshObject> _plannedBones;
        private readonly List<MeshObject> _originalParts;

        public class ScrewRegistrationProperties
        {
            public bool IsScrewOnGraft { get; set; }
            public MeshObject IntersectedWithPlannedMeshObject { get; set; }
            public double PastillePointToPlannedMeshDistance { get; set; }
            public bool IsFloatingScrew { get; set; }
            public MeshObject RegisteredOnOriginalMeshObject { get; set; }
            public bool RegistrationSuccessful { get; set; }
        }

        public ScrewRegistration(CMFImplantDirector director, bool skipIfScrewOnGraft)
        {
            _director = director;

            var helper = new OriginalPositionedScrewAnalysisHelper(director);
            _plannedBones = helper.GetPlannedBones();
            _originalParts = helper.GetOriginalParts();

            _skipRegistrationIfScrewOnGraft = skipIfScrewOnGraft;
        }
                
        public ScrewRegistrationProperties PerformImplantScrewRegistrationToOriginalPosition(Screw screw)
        {
            var outcome = new ScrewRegistrationProperties
            {
                IsScrewOnGraft = false,
                IntersectedWithPlannedMeshObject = null,
                IsFloatingScrew = true,
                RegisteredOnOriginalMeshObject = null,
                PastillePointToPlannedMeshDistance = double.NaN,
                RegistrationSuccessful = false
            };

            if (ScrewUtilities.IsScrewOnGraft(_plannedBones.ToList<MeshProperties>(), screw))
            {
                outcome.IsScrewOnGraft = true;
                if (_skipRegistrationIfScrewOnGraft)
                {
                    outcome.IsFloatingScrew = true;
                    outcome.RegistrationSuccessful = false;
                    return outcome;
                }
            }

            //Screw registration on difficult areas
            //Algorithm is based on description in REQUIREMENT 816308 C: Barrel Registration - Improve algorithm for Barrels Registration
            //Search algorithm: 
            //Software searches for the bone to be registered to, by using a list of control points around the screw head and getting the nearest planned bones. 
            //If all the control points have the same nearest planned bone, the bone to be registered to is found. 

            //There are 2 scenarios:
            //1. Screws that have intersection with a planned bone but are placed on cut slot
            // - these screws are considered as floating screw. They should NOT be registered. 
            // - to identify that they are placed on cut slot, the distance between the pastille and intersected planned bone should be more than the floating screw check tolerance 
            //   and based on the search algorithm above, no bone found or the found bone is not the bone it is intersected with

            //2. Screws that are placed on areas where there are no intersection with planned bone. Eg: positioned on old metal/hole/not cut slot
            // - these screws are NOT floating screw. They should be registered. 
            // - to identify that they are placed on difficult areas, the search algorithm above is executed.

            var plannedMesh = ScrewUtilities.FindIntersection(_plannedBones.Select(i => i.Mesh), screw);
            var foundPlannedPartsOnDifficultArea = false;

            if (plannedMesh == null)
            {
                var boneToRegister = ScrewUtilities.FindBoneToRegister(_plannedBones.Select(i => i.Mesh), screw);
                if (boneToRegister == null)
                {
                    outcome.IntersectedWithPlannedMeshObject = null;
                    outcome.IsFloatingScrew = true;
                    outcome.RegistrationSuccessful = false;
                    return outcome; // if no implant mesh, skip registration
                }

                foundPlannedPartsOnDifficultArea = true;
                plannedMesh = boneToRegister;
            }

            var pastille =
                ImplantCreationUtilities.FindClosestDotPastille(ImplantCreationUtilities.GetAllExistingDots(_director), screw.HeadPoint);

            //The rotation point
            var pastilleLocation = RhinoPoint3dConverter.ToPoint3d(pastille.Location);

            var plannedMeshObj = _plannedBones.First(o => o.Mesh == plannedMesh);
            outcome.IntersectedWithPlannedMeshObject = plannedMeshObj;

            //To ensure that elongated or rotated screw is not accidentally detected as non floating.
            var closestPtOnMesh = plannedMeshObj.Mesh.ClosestPoint(pastilleLocation);
            var ret = true;
            var dist = closestPtOnMesh.DistanceTo(pastilleLocation);
            outcome.PastillePointToPlannedMeshDistance = dist;
            if (dist > QCValues.FloatingScrewCheckTolerance)
            {
                if (!foundPlannedPartsOnDifficultArea)
                {
                    var boneToRegister = ScrewUtilities.FindBoneToRegister(_plannedBones.Select(i => i.Mesh), screw);
                    if (boneToRegister != null)
                    {
                        var difficultAreaPlannedMesh = _plannedBones.First(o => o.Mesh == boneToRegister);
                        if (difficultAreaPlannedMesh.Name != plannedMeshObj.Name)
                        {
                            ret = false;
                        }
                    }
                    else
                    {
                        ret = false;
                    }
                }

                if (!ret)
                {
                    outcome.IsFloatingScrew = true;
                    outcome.RegistrationSuccessful = false;
                    return outcome;
                }
            }

            var originalMesh = GetOriginalMeshObject(plannedMeshObj.Name);
            if (originalMesh == null)
            {
                outcome.RegisteredOnOriginalMeshObject = null;
                outcome.IsFloatingScrew = true;
                outcome.RegistrationSuccessful = false;
                return outcome;
            }

            outcome.IsFloatingScrew = false;
            outcome.RegisteredOnOriginalMeshObject = originalMesh;
            outcome.RegistrationSuccessful = true;
            return outcome;
        }

        private MeshObject GetOriginalMeshObject(string plannedMeshName)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var plannedPartName = proPlanImportComponent.GetPartName(plannedMeshName).Remove(0, 2);
            var originalObj = _originalParts.FirstOrDefault(obj => proPlanImportComponent.GetPartName(obj.Name).Remove(0, 2).ToLower() == plannedPartName.ToLower());
            return originalObj;
        }

        public void Dispose()
        {
            _plannedBones.ForEach(x => x.Mesh.Dispose());
            _originalParts.ForEach(x => x.Mesh.Dispose());

            _plannedBones.Clear();
            _originalParts.Clear();
        }
    }
}
