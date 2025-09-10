using IDS.CMF.Query;
using IDS.CMF.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Linq;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class PastilleTests
    {
        [TestMethod]
        public void CreateCylinderMesh_Returns_Solid_Mesh()
        {
            //Bug 1115439: C: Deformed pastille generated due to incorrect smarties result
            var mesh = ImplantPastilleCreationUtilities.CreateCylinderMesh(Plane.WorldXY, 2.5, 6.0);

            Assert.IsTrue(mesh.IsSolid, "CylinderMesh created is not solid");
        }

        [TestMethod]
        public void StampImprint_Is_Needed_For_Pastille_With_Thickness_Less_Than_Or_Equals_1_Dot_5()
        {
            //arrange
            var screwLengthsPreferences = CasePreferencesHelper.LoadScrewLengthData();
            var screwTypes = screwLengthsPreferences.ScrewLengths.Select(d => d.ScrewType);
            var minThickness = 0.10;
            var maxThicknessToCheck = 1.50;
            var thicknessIncrement = 0.05;

            foreach (var screwType in screwTypes)
            {
                for (var thickness = minThickness; thickness <= maxThicknessToCheck; thickness += thicknessIncrement)
                {
                    //act
                    var stampImprintIsNeeded = IsNeedToAddStampImprintShape(screwType, thickness);

                    //assert
                    Assert.IsTrue(stampImprintIsNeeded, $"Stamp imprint is needed for pastille with ScrewType: {screwType} and Thickness: {thickness}!");
                }
            }
        }

        [TestMethod]
        public void StampImprint_Is_Not_Needed_For_Pastille_With_Thickness_More_Than_1_Dot_5()
        {
            //arrange
            var screwLengthsPreferences = CasePreferencesHelper.LoadScrewLengthData();
            var screwTypes = screwLengthsPreferences.ScrewLengths.Select(d => d.ScrewType);
            var minThicknessToCheck = 1.55;
            var maxThickness = 3.00;
            var thicknessIncrement = 0.05;

            foreach (var screwType in screwTypes)
            {
                for (var thickness = minThicknessToCheck; thickness <= maxThickness; thickness += thicknessIncrement)
                {
                    //act
                    var stampImprintIsNeeded = IsNeedToAddStampImprintShape(screwType, thickness);

                    //assert
                    Assert.IsFalse(stampImprintIsNeeded, $"Stamp imprint is not needed for pastille with ScrewType: {screwType} and Thickness: {thickness}!");
                }
            }
        }

        [TestMethod]
        public void StampImprint_Generated_Has_Correct_Height()
        {
            //arrange
            var screwLengthsPreferences = CasePreferencesHelper.LoadScrewLengthData();
            var screwTypes = screwLengthsPreferences.ScrewLengths.Select(d => d.ScrewType);

            var screwHeadPoint = Point3d.Origin;
            var screwDirection = -Vector3d.ZAxis;

            foreach (var screwType in screwTypes)
            {
                var width = Queries.GetStampImprintShapeWidth(screwType);
                var height = Queries.GetStampImprintShapeHeight(screwType);
                var shapeOffset = Queries.GetStampImprintShapeOffset(screwType);
                var shapeSectionHeightRatio = Queries.GetStampImprintShapeSectionHeightRatio(screwType);

                var expectedHeight = shapeSectionHeightRatio * height;

                //act and assert
                var stampImprintMesh = ImplantPastilleCreationUtilities.GenerateStampImprintShapeMesh(screwHeadPoint, screwDirection, shapeOffset, width, height, shapeSectionHeightRatio);

                Assert.IsNotNull(stampImprintMesh, $"Stamp imprint not generated for ScrewType: {screwType}");

                var dimensions = MeshDimensions.GetMeshDimensions(stampImprintMesh);

                var actualHeight = Math.Abs(dimensions.BoundingBoxMax[2] - dimensions.BoundingBoxMin[2]);
                Assert.AreEqual(expectedHeight, actualHeight, 0.01, $"Stamp imprint's height for pastille with ScrewType: {screwType} is {actualHeight}! Expect: {expectedHeight}");
            }
        }

        private bool IsNeedToAddStampImprintShape(string screwTypeValue, double pastilleThickness)
        {
            var shapeCreationMaxPastilleThickness = Queries.GetStampImprintShapeCreationMaxPastilleThickness(screwTypeValue);
            return ImplantPastilleCreationUtilities.IsNeedToAddStampImprintShape(pastilleThickness, shapeCreationMaxPastilleThickness);
        }
    }

#endif
}