using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using Rhino.Geometry;

namespace IDS.CMF.Quality
{
    public class CMFScrewAtOriginalPositionHelper
    {
        public readonly ScrewRegistration _screwRegistration;

        public CMFScrewAtOriginalPositionHelper(ScrewRegistration screwRegistration)
        {
            _screwRegistration = screwRegistration;
        }

        public Screw GetScrewAtOriginalPosition(Screw screw, out Transform transformationToOriginal)
        {
            return GetScrewAtOriginalPosition(screw, out _, out transformationToOriginal, out _);
        }

        public Screw GetScrewAtOriginalPosition(Screw screwOnPlanned, out Mesh originalBone,
            out Transform transformationToOriginal, out Mesh plannedBone)
        {
            originalBone = null;
            transformationToOriginal = Transform.Unset;
            plannedBone = null;

            var result = _screwRegistration.PerformImplantScrewRegistrationToOriginalPosition(screwOnPlanned);

            if (result.IntersectedWithPlannedMeshObject == null)
            {
                return null;
            }

            if (result.RegisteredOnOriginalMeshObject == null)
            {
                return null;
            }

            if (result.IsFloatingScrew)
            {
                return null;
            }

            var originalTransformation = result.RegisteredOnOriginalMeshObject.Transform;
            var plannedTransformation = result.IntersectedWithPlannedMeshObject.Transform;

            Transform inverseTrans;
            if (!plannedTransformation.TryGetInverse(out inverseTrans))
            {
                return null;
            }

            transformationToOriginal = Transform.Multiply(originalTransformation, inverseTrans);
            var headPointAtOriginal = new Point3d(screwOnPlanned.HeadPoint);
            headPointAtOriginal.Transform(transformationToOriginal);
            var tipPointAtOriginal = new Point3d(screwOnPlanned.TipPoint);
            tipPointAtOriginal.Transform(transformationToOriginal);
            var screwAtOriginalPosition = new Screw(screwOnPlanned.Director,
                headPointAtOriginal, tipPointAtOriginal, screwOnPlanned.ScrewAideDictionary,
                screwOnPlanned.Index, screwOnPlanned.ScrewType, screwOnPlanned.BarrelType);

            originalBone = result.RegisteredOnOriginalMeshObject.Mesh;
            plannedBone = result.IntersectedWithPlannedMeshObject.Mesh;
            return screwAtOriginalPosition;
        }
    }
}
