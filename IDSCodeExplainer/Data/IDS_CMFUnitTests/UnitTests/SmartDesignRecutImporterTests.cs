using IDS.CMF.Operations;
using IDS.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class SmartDesignRecutImporterTests : RegisterPartTests
    {
        [TestMethod]
        public void Import_Takes_Place_Even_When_Mesh_Is_Detected_Repositioned()
        {
            //arrange
            var resource = new TestResources();
            var isRepositioned = MeshRepositionedCheckerTests.IsMeshRepositioned(resource.OriginalRamusStlFilePath, resource.RepositionedRamusStlFilePath);
            Assert.IsTrue(isRepositioned);

            Prepare_Default_Case(true, true);

            var partName = "01RAM_L";
            StlUtilities.StlBinary2RhinoMesh(resource.OriginalRamusStlFilePath, out var expectedTargetMesh);
            var partBlock = GetPartBlock(partName);
            var originalPartId = _objectManager.AddNewBuildingBlockWithTransform(partBlock, expectedTargetMesh, Transform.Identity);
            Assert.IsTrue(originalPartId != Guid.Empty);

            StlUtilities.StlBinary2RhinoMesh(resource.RepositionedRamusStlFilePath, out var recutSourceMesh);
            InputAsPart(partName, recutSourceMesh);

            //we use boundingbox to check for equality
            var meshBeforeOperationIsSame = recutSourceMesh.GetBoundingBox(true).Equals(expectedTargetMesh.GetBoundingBox(true));
            Assert.IsFalse(meshBeforeOperationIsSame, "Expected mesh and actual mesh same!");

            //act
            var recutImporter = new SmartDesignRecutImporter(_director, new List<string>());
            var success = recutImporter.ImportRecut(_tempDirectory, out var partsThatChanged, out var numTrianglesImported);

            //assert
            Assert.IsTrue(success, "Import recut failed!");
            Assert.IsTrue(partsThatChanged.Count == 1, "Count of parts changed is not 1!");

            var replacedTargetPart = _objectManager.GetBuildingBlock(partBlock);
            Assert.IsNotNull(replacedTargetPart, "Retrieved target part is null!");

            var replacedTargetMesh = (Mesh)replacedTargetPart.Geometry;
            var meshAfterOperationIsSame = recutSourceMesh.GetBoundingBox(true).Equals(replacedTargetMesh.GetBoundingBox(true));
            Assert.IsTrue(meshAfterOperationIsSame, "Expected mesh and actual mesh differs!");
        }

        [TestMethod]
        public void Planned_Part_Is_Registered()
        {
            var recutImporter = new SmartDesignRecutImporter(_director, new List<string>());
            Target_Part_Is_Registered_To_Correct_Position_And_With_Correct_Geometry(_originalInputInformation, _plannedInputInformation, recutImporter);
        }
    }

#endif
}