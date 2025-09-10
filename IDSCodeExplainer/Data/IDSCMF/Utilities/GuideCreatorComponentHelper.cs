using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class GuideCreatorComponentHelper
    {
        public static Mesh AddScrewEyesOrLabelTag(IEnumerable<Screw> guideFixationScrew)
        {
            var screwComponent = new Mesh();

            guideFixationScrew.ToList().ForEach(screw =>
            {
                var screwLabelTagHelper = new ScrewLabelTagHelper(screw.Director);
                if (!double.IsNaN(screwLabelTagHelper.GetLabelTagAngle(screw)))
                {
                    var tagShape = screw.GetScrewLabelTagShapeInlabelTagAlignment();
                    screwComponent.Append(Mesh.CreateFromBrep(tagShape,
                        MeshParameters.IDS(Constants.GuideCreationParameters.MeshingParameterMinEdgeLength,
                            Constants.GuideCreationParameters.MeshingParameterMaxEdgeLength)));
                }
                else
                {
                    screwComponent.Append(Mesh.CreateFromBrep(screw.GetScrewEyeShape(),
                        MeshParameters.IDS(Constants.GuideCreationParameters.MeshingParameterMinEdgeLength,
                            Constants.GuideCreationParameters.MeshingParameterMaxEdgeLength)));
                }
            });

            return screwComponent;
        }

        public static Mesh AddScrewEyesOrLabelTagSafe(IEnumerable<Screw> guideFixationScrew)
        {
            return !guideFixationScrew.Any() ? null : AddScrewEyesOrLabelTag(guideFixationScrew);
        }

        public static Mesh AddFlanges(IEnumerable<Mesh> flanges, Mesh osteotomyMesh, Mesh guideSurfaceWrap)
        {
            var guideFlanges = new Mesh();
            flanges.ToList().ForEach(x =>
            {
                var f = x.DuplicateMesh();
                guideFlanges.Append(AutoFix.PerformUnify(f));
            });

            var flangesSubtracted = Booleans.PerformBooleanSubtraction(guideFlanges, guideSurfaceWrap);

            if (osteotomyMesh != null)
            {
                flangesSubtracted = Booleans.PerformBooleanSubtraction(flangesSubtracted, osteotomyMesh);
            }

            return flangesSubtracted;
        }

        public static Mesh AddFlangesSafe(IEnumerable<Mesh> flanges, Mesh osteotomyMesh, Mesh guideSurfaceWrap)
        {
            if (!flanges.Any() || osteotomyMesh == null || guideSurfaceWrap == null)
                return null;

            return AddFlanges(flanges, osteotomyMesh, guideSurfaceWrap);
        }

        public static Mesh AddBarrels(IEnumerable<Mesh> barrelsShape, Mesh osteotomyMesh)
        {
            var barrelsMesh = MeshUtilities.AppendMeshes(barrelsShape);

            if (osteotomyMesh != null)
            {
                barrelsMesh = Booleans.PerformBooleanSubtraction(barrelsMesh, osteotomyMesh);
            }

            return barrelsMesh;
        }

        public static Mesh AddBarrelsSafe(IEnumerable<Mesh> barrelsShape, Mesh osteotomyMesh)
        {
            if (!barrelsShape.Any() || osteotomyMesh == null)
                return null;

            return AddBarrels(barrelsShape, osteotomyMesh);
        }
    }
}
