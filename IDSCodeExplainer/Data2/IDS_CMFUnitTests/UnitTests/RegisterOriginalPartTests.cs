using IDS.CMF.Operations;
using IDS.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class RegisterOriginalPartTests : RegisterPartTests
    {
        //Unit tests for Register parts from Original to Planned position
        //Source = Original Part; Target = Planned Part

        [TestMethod]       
        public void Planned_Part_Is_Registered_To_Correct_Position_And_With_Correct_Geometry()
        {
            Target_Part_Is_Registered_To_Correct_Position_And_With_Correct_Geometry(_originalInputInformation, _plannedInputInformation);           
        }

        [TestMethod]
        public void Planned_Part_Is_Not_Registered_If_Planned_Part_Does_Not_Exist_In_Case()
        {
            //arrange
            Prepare_Default_Case(true, false);

            Target_Part_Is_Not_Registered_If_Target_Part_Does_Not_Exist_In_Case(_originalInputInformation, _plannedInputInformation);
        }

        [TestMethod]
        public void No_Registeration_Take_Place_If_Input_Contains_Corresponding_Planned_Part()
        {
            No_Registeration_Take_Place_If_Input_Contains_Corresponding_Target_Part(_originalInputInformation, _plannedInputInformation);
        }

        [TestMethod]
        public void Registeration_Take_Place_If_Input_Contains_Corresponding_Planned_Part_But_Different_Surgery_Stage_Compared_To_In_Case()
        {
            //arrange
            Prepare_Default_Case(true, true);

            //Input:
            //Original mesh with different geometry compared to the one in case
            //Planned mesh with different surgery stage and geometry compared to the one in case
            var recutOriginalMesh = CreateCylinder();
            var originalPartFilePath = InputAsPart(_originalInputInformation.PartName, recutOriginalMesh);

            var theOtherPlannedPartName = "06GEN";
            var theOtherPlannedPartFilePath = $@"{_tempDirectory}\{theOtherPlannedPartName}.stl";
            var recutPlannedMesh = CreateBox();
            StlUtilities.RhinoMesh2StlBinary(recutPlannedMesh, theOtherPlannedPartFilePath);

            StlUtilities.StlBinary2RhinoMesh(originalPartFilePath, out var expectedPlannedMesh);
            expectedPlannedMesh.Transform(_originalInputInformation.InversedTransformationMatrix);
            expectedPlannedMesh.Transform(_plannedInputInformation.TransformationMatrix);
            StlUtilities.StlBinary2RhinoMesh(theOtherPlannedPartFilePath, out var inputPlannedMesh);

            //act
            var recutImporter = new RecutImporter(_director, false, true, true, true);
            var success = recutImporter.ImportRecut(_tempDirectory, out var partsThatChanged, out var numTrianglesImported);

            //assert
            Assert.IsTrue(success, "Import recut failed!");
            Assert.IsTrue(partsThatChanged.Count == 2, "Count of parts changed is not 2!");

            var replacedPlannedPart = _objectManager.GetBuildingBlock(GetPartBlock(_plannedInputInformation.PartName));
            Assert.IsNotNull(replacedPlannedPart, "Retrieved planned part is null!");

            var replacedPlannedMesh = (Mesh)replacedPlannedPart.Geometry;
            //here we use boundingbox to check for equality
            var isSame = expectedPlannedMesh.GetBoundingBox(true).Equals(replacedPlannedMesh.GetBoundingBox(true));
            Assert.IsTrue(isSame, "Expected mesh and actual mesh differs!");
            var isNotSame = inputPlannedMesh.GetBoundingBox(true).Equals(replacedPlannedMesh.GetBoundingBox(true));
            Assert.IsFalse(isNotSame, "Input mesh and actual mesh same!");

            var inputPlannedBlock = _proPlanImportComponent.GetProPlanImportBuildingBlock(theOtherPlannedPartName);
            var inputPlannedPart = _objectManager.GetBuildingBlock(inputPlannedBlock);
            Assert.IsNull(inputPlannedPart, "Retrieved planned part is not null!");
        }

        [TestMethod]
        public void Registered_Planned_Part_Maintains_Its_Transformation_Matrix()
        {
            Registered_Target_Part_Maintains_Its_Transformation_Matrix(_originalInputInformation, _plannedInputInformation);
        }

        [TestMethod]
        public void Planned_Part_Is_Not_Registered_If_Original_Part_Does_Not_Exist_In_Case()
        {
            //arrange
            Prepare_Default_Case(false, true);

            Target_Part_Is_Not_Registered_If_Source_Part_Does_Not_Exist_In_Case(_originalInputInformation, _plannedInputInformation);
        }
    }

#endif
}