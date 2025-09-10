using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.Core.SplashScreen;
using IDS.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rhino;
using Rhino.Geometry;
using System;
using System.IO;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class RegisterPartTests
    {
        protected RhinoDoc _rhinoDoc;
        protected CMFImplantDirector _director;
        protected CMFObjectManager _objectManager;
        protected ProPlanImportComponent _proPlanImportComponent;
        protected string _tempDirectory;

        protected OriginalPartInputInformation _originalInputInformation;
        protected PlannedPartInputInformation _plannedInputInformation;

        [TestInitialize]
        public void TestInitialize()
        {
            _rhinoDoc = RhinoDoc.CreateHeadless(null);
            var pluginInfo = new Mock<IPluginInfoModel>();
            _director = new CMFImplantDirector(_rhinoDoc, pluginInfo.Object, false);
            _objectManager = new CMFObjectManager(_director);
            _proPlanImportComponent = new ProPlanImportComponent();

            _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDirectory);

            _originalInputInformation = new OriginalPartInputInformation();
            _plannedInputInformation = new PlannedPartInputInformation();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Directory.Delete(_tempDirectory, true);
        }

        public void Target_Part_Is_Registered_To_Correct_Position_And_With_Correct_Geometry(IInputInformation sourceInputInformation, IInputInformation targetInputInformation)
        {
            var recutImporter = new RecutImporter(_director, false, true, true, true);
            Target_Part_Is_Registered_To_Correct_Position_And_With_Correct_Geometry(sourceInputInformation, targetInputInformation, recutImporter);
        }

        public void Target_Part_Is_Registered_To_Correct_Position_And_With_Correct_Geometry(IInputInformation sourceInputInformation, IInputInformation targetInputInformation, RecutImporter recutImporter)
        {
            //arrange
            Prepare_Default_Case(true, true);

            //Input:
            //Source mesh with different geometry compared to the one in case
            var recutSourceMesh = CreateCylinder();
            var sourcePartFilePath = InputAsPart(sourceInputInformation.PartName, recutSourceMesh);

            StlUtilities.StlBinary2RhinoMesh(sourcePartFilePath, out var expectedTargetMesh);
            expectedTargetMesh.Transform(sourceInputInformation.InversedTransformationMatrix);
            expectedTargetMesh.Transform(targetInputInformation.TransformationMatrix);

            //act
            var success = recutImporter.ImportRecut(_tempDirectory, out var partsThatChanged, out var numTrianglesImported);

            //assert
            Assert.IsTrue(success, "Import recut failed!");
            Assert.IsTrue(partsThatChanged.Count == 2, "Count of parts changed is not 2!");

            var replacedTargetPart = _objectManager.GetBuildingBlock(GetPartBlock(targetInputInformation.PartName));
            Assert.IsNotNull(replacedTargetPart, "Retrieved target part is null!");

            var replacedTargetMesh = (Mesh)replacedTargetPart.Geometry;
            //here we use boundingbox to check for equality
            var isSame = expectedTargetMesh.GetBoundingBox(true).Equals(replacedTargetMesh.GetBoundingBox(true));
            Assert.IsTrue(isSame, "Expected mesh and actual mesh differs!");
        }

        public void Target_Part_Is_Not_Registered_If_Target_Part_Does_Not_Exist_In_Case(IInputInformation sourceInputInformation, IInputInformation targetInputInformation)
        {
            //arrange
            //Input:
            //Source mesh with different geometry compared to the one in case
            var recutSourceMesh = CreateCylinder();
            InputAsPart(sourceInputInformation.PartName, recutSourceMesh);

            //act
            var recutImporter = new RecutImporter(_director, false, true, true, true);
            var success = recutImporter.ImportRecut(_tempDirectory, out var partsThatChanged, out var numTrianglesImported);

            //assert
            Assert.IsTrue(success, "Import recut failed!");
            Assert.IsTrue(partsThatChanged.Count == 1, "Count of parts changed is not 1!");

            var replacedTargetParts = _objectManager.GetAllBuildingBlockRhinoObjectByMatchingName(IBB.ProPlanImport, targetInputInformation.PartName);
            Assert.IsTrue(replacedTargetParts.Count == 0, "Corresponding target part is replaced!");
        }

        public void No_Registeration_Take_Place_If_Input_Contains_Corresponding_Target_Part(IInputInformation sourceInputInformation, IInputInformation targetInputInformation)
        {
            //arrange
            Prepare_Default_Case(true, true);

            //Input:
            //Source mesh with different geometry compared to the one in case
            //Target mesh with different geometry compared to the one in case
            var recutSourceMesh = CreateCylinder();
            var sourcePartFilePath = InputAsPart(sourceInputInformation.PartName, recutSourceMesh);

            var recutTargetMesh = CreateBox();
            var targetPartFilePath = InputAsPart(targetInputInformation.PartName, recutTargetMesh);

            StlUtilities.StlBinary2RhinoMesh(sourcePartFilePath, out var registeredTargetMesh);
            registeredTargetMesh.Transform(sourceInputInformation.InversedTransformationMatrix);
            registeredTargetMesh.Transform(targetInputInformation.TransformationMatrix);
            StlUtilities.StlBinary2RhinoMesh(targetPartFilePath, out var expectedTargetMesh);
            //no registration

            //act
            var recutImporter = new RecutImporter(_director, false, true, true, true);
            var success = recutImporter.ImportRecut(_tempDirectory, out var partsThatChanged, out var numTrianglesImported);

            //assert
            Assert.IsTrue(success, "Import recut failed!");
            Assert.IsTrue(partsThatChanged.Count == 2, "Count of parts changed is not 2!");

            var replacedTargetPart = _objectManager.GetBuildingBlock(GetPartBlock(targetInputInformation.PartName));
            Assert.IsNotNull(replacedTargetPart, "Retrieved target part is null!");

            var replacedTargetMesh = (Mesh)replacedTargetPart.Geometry;
            //here we use boundingbox to check for equality
            var isSame = expectedTargetMesh.GetBoundingBox(true).Equals(replacedTargetMesh.GetBoundingBox(true));
            Assert.IsTrue(isSame, "Expected mesh and actual mesh differs!");
            var isNotSame = registeredTargetMesh.GetBoundingBox(true).Equals(replacedTargetMesh.GetBoundingBox(true));
            Assert.IsFalse(isNotSame, "Registered mesh and actual mesh same!");
        }

        public void Registered_Target_Part_Maintains_Its_Transformation_Matrix(IInputInformation sourceInputInformation, IInputInformation targetInputInformation)
        {
            //arrange and act
            Target_Part_Is_Registered_To_Correct_Position_And_With_Correct_Geometry(sourceInputInformation, targetInputInformation);

            //assert
            var replacedTargetPart = _objectManager.GetBuildingBlock(GetPartBlock(targetInputInformation.PartName));
            Assert.IsTrue(replacedTargetPart.Attributes.UserDictionary.ContainsKey("transformation_matrix"), "Retrieved target part does not have transformation matrix stored in Attributes!");

            var transform = (Transform)replacedTargetPart.Attributes.UserDictionary["transformation_matrix"];
            Assert.AreEqual(targetInputInformation.TransformationMatrix, transform, "Transformation matrix from object is not equal to expected!");
        }

        public void Target_Part_Is_Not_Registered_If_Source_Part_Does_Not_Exist_In_Case(IInputInformation sourceInputInformation, IInputInformation targetInputInformation)
        {
            //arrange
            var sourcePartInCase = _objectManager.GetBuildingBlock(GetPartBlock(sourceInputInformation.PartName));
            Assert.IsNull(sourcePartInCase);

            var targetPartInCase = _objectManager.GetBuildingBlock(GetPartBlock(targetInputInformation.PartName));
            var targetPartMesh = ((Mesh)targetPartInCase.Geometry).DuplicateMesh();

            //Input:
            var recutSourceMesh = CreateCylinder();
            var sourcePartFilePath = InputAsPart(sourceInputInformation.PartName, recutSourceMesh);
            StlUtilities.StlBinary2RhinoMesh(sourcePartFilePath, out var expectedSourceMesh);

            //act
            var recutImporter = new RecutImporter(_director, false, true, true, true);
            var success = recutImporter.ImportRecut(_tempDirectory, out var partsThatChanged, out var numTrianglesImported);

            //assert
            Assert.IsTrue(success, "Import recut failed!");
            Assert.IsTrue(partsThatChanged.Count == 1, "Count of parts changed is not 1!");

            var replacedSourcePart = _objectManager.GetBuildingBlock(GetPartBlock(sourceInputInformation.PartName));
            Assert.IsNotNull(replacedSourcePart, "Retrieved source part is null!");

            var replacedSourceMesh = (Mesh)replacedSourcePart.Geometry;
            //here we use boundingbox to check for equality
            var isSame = expectedSourceMesh.GetBoundingBox(true).Equals(replacedSourceMesh.GetBoundingBox(true));
            Assert.IsTrue(isSame, "Expected mesh and actual mesh differs!");

            var afterOperationTargetPartInCase = _objectManager.GetBuildingBlock(GetPartBlock(targetInputInformation.PartName));
            var afterOperationTargetPartMesh = ((Mesh)afterOperationTargetPartInCase.Geometry).DuplicateMesh();
            isSame = afterOperationTargetPartMesh.GetBoundingBox(true).Equals(targetPartMesh.GetBoundingBox(true));
            Assert.IsTrue(isSame, "Target mesh is replaced!");
        }

        #region Helpers

        public interface IInputInformation
        {
            string PartName { get; }
            Transform TransformationMatrix { get; }
            Transform InversedTransformationMatrix { get; }
        }

        public class OriginalPartInputInformation : IInputInformation
        {
            public string PartName { get => "01GEN"; }
            public Transform TransformationMatrix { get => Transform.Translation(3, 2, 1); }
            public Transform InversedTransformationMatrix { get => Transform.Translation(-3, -2, -1); }
        }

        public class PlannedPartInputInformation : IInputInformation
        {
            public string PartName { get => "05GEN"; }
            public Transform TransformationMatrix { get => Transform.Translation(1, 2, 3); }
            public Transform InversedTransformationMatrix { get => Transform.Translation(-1, -2, -3); }
        }

        protected ExtendedImplantBuildingBlock GetPartBlock(string partName)
        {
            return _proPlanImportComponent.GetProPlanImportBuildingBlock(partName);
        }

        protected string InputAsPart(string partName, Mesh partMesh)
        {
            var partFilePath = $@"{_tempDirectory}\{partName}.stl";
            StlUtilities.RhinoMesh2StlBinary(partMesh, partFilePath);
            return partFilePath;
        }

        //each shape produces different bounding box

        protected Mesh CreateSphere()
        {
            return Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 5.0), 10, 10);
        }

        protected Mesh CreateCylinder()
        {
            return Mesh.CreateFromCylinder(new Cylinder(new Circle(5.0), 1.0), 10, 10);
        }

        protected Mesh CreateBox()
        {
            return Mesh.CreateFromBox(new Sphere(Point3d.Origin, 2.5).BoundingBox, 10, 10, 10);
        }

        protected void Prepare_Default_Case(bool addDefaultOriginalPart, bool addDefaultPlannedPart)
        {
            //arrange
            var originalMesh = CreateSphere();
            var originalTransform = _originalInputInformation.TransformationMatrix;
            originalMesh.Transform(originalTransform);
            var originalBlock = GetPartBlock(_originalInputInformation.PartName);

            if (!addDefaultOriginalPart)
            {
                originalBlock = _proPlanImportComponent.GetProPlanImportBuildingBlock("01RAM_L");
            }

            var originalPartId = _objectManager.AddNewBuildingBlockWithTransform(originalBlock, originalMesh, originalTransform);
            Assert.IsTrue(originalPartId != Guid.Empty);

            var plannedMesh = CreateSphere();
            var translated = _plannedInputInformation.TransformationMatrix;
            plannedMesh.Transform(translated);
            var plannedBlock = GetPartBlock(_plannedInputInformation.PartName);

            if (!addDefaultPlannedPart)
            {
                plannedBlock = _proPlanImportComponent.GetProPlanImportBuildingBlock("02RAM_L");
            }

            var plannedPartId = _objectManager.AddNewBuildingBlockWithTransform(plannedBlock, plannedMesh, translated);
            Assert.IsTrue(plannedPartId != Guid.Empty);
        }

        #endregion
    }

#endif
}