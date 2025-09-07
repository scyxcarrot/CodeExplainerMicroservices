using IDS.CMF;
using IDS.CMF.Common;
using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.Core.SplashScreen;
using IDS.Core.Utilities;
using IDS.Interface.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    internal class MockOsteotomyHandler : IOsteotomyHandler
    {
        public string Name { get; }

        public string Type { get; }

        public double Thickness { get; }

        public string[] Identifier { get; }

        public double[,] Coordinate { get; }

        public MockOsteotomyHandler(string name, string type, double thickness, string[] identifier, double[,] coordinates)
        {
            Name = name;
            Type = type;
            Thickness = thickness;
            Identifier = identifier;
            Coordinate = coordinates;
        }
    }

    [TestClass]
    public class UpdateAnatomyTests
    {
        private RhinoDoc _rhinoDoc;
        private CMFImplantDirector _director;
        private CMFObjectManager _objectManager;
        private ProPlanImportComponent _proPlanImportComponent;
        private string _tempDirectory;

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
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Directory.Delete(_tempDirectory, true);
        }

        [TestMethod]
        public void New_Imported_Planned_Part_Has_Correct_Non_Identity_Transformation_Matrix()
        {
            var plannedPartTransform = Transform.Translation(1, 2, 3);
            New_Imported_Planned_Part_Has_Correct_Transformation_Matrix(plannedPartTransform);
        }

        [TestMethod]
        public void New_Imported_Planned_Part_Has_Correct_Identity_Transformation_Matrix()
        {
            //Bug 1076582: C: Null Exception Error - Update ProPlan for planned bone
            var plannedPartTransform = Transform.Identity;
            New_Imported_Planned_Part_Has_Correct_Transformation_Matrix(plannedPartTransform);
        }

        [TestMethod]
        public void New_Imported_Planned_Part_Has_Correct_Osteotomy_Handler()
        {
            var osteotomyHandler = new List<IOsteotomyHandler>
            {
                new MockOsteotomyHandler("05GEN", "05GEN", 5.0, new []
                {
                    "1", "2"
                }, new[,]
                {
                    {1.0, 2.0, 3.0}, 
                    {1.0, 2.0, 3.0}
                })
            };

            New_Imported_Planned_Part_Has_Correct_Osteotomy_Handler(osteotomyHandler);
        }

        #region Helpers

        private void New_Imported_Planned_Part_Has_Correct_Transformation_Matrix(Transform tranformToCheck)
        {
            //arrange
            Prepare_Default_Case();

            var recutPlannedMesh = Mesh.CreateFromCylinder(new Cylinder(new Circle(5.0), 1.0), 10, 10);
            var plannedPartName = "05GEN";
            var plannedPartFilePath = $@"{_tempDirectory}\{plannedPartName}.stl";
            StlUtilities.RhinoMesh2StlBinary(recutPlannedMesh, plannedPartFilePath);

            var plannedPartTransform = tranformToCheck;
            var transformationMatrixMap = new List<Tuple<string, Transform>>();
            transformationMatrixMap.Add(new Tuple<string, Transform>(plannedPartName, plannedPartTransform));

            //act
            var anatomyUpdater = new AnatomyUpdater(_director, transformationMatrixMap, new List<IOsteotomyHandler>());
            var success = anatomyUpdater.ImportRecut(_tempDirectory, out var partsThatChanged, out var numTrianglesImported);

            //assert
            Assert.IsTrue(success, "Update anatomy failed!");
            Assert.IsTrue(partsThatChanged.Count == 1, "Count of parts changed is not 1!");

            var plannedPartBlock = _proPlanImportComponent.GetProPlanImportBuildingBlock(plannedPartName);
            var retrievedPlannedPart = _objectManager.GetBuildingBlock(plannedPartBlock);
            Assert.IsTrue(retrievedPlannedPart.Attributes.UserDictionary.ContainsKey("transformation_matrix"), "Retrieved planned part does not have transformation matrix stored in Attributes!");

            var retrievedTransform = (Transform)retrievedPlannedPart.Attributes.UserDictionary["transformation_matrix"];
            Assert.AreEqual(plannedPartTransform, retrievedTransform, "Transformation matrix from object is not equal to expected!");
        }

        private void New_Imported_Planned_Part_Has_Correct_Osteotomy_Handler(List<IOsteotomyHandler> osteotomyHandler)
        {
            //arrange
            Prepare_Default_Case();

            var recutPlannedMesh = Mesh.CreateFromCylinder(new Cylinder(new Circle(5.0), 1.0), 10, 10);
            var plannedPartFilePath = $@"{_tempDirectory}\{osteotomyHandler.FirstOrDefault().Name}.stl";
            StlUtilities.RhinoMesh2StlBinary(recutPlannedMesh, plannedPartFilePath);

            //act
            var anatomyUpdater = new AnatomyUpdater(_director, new List<Tuple<string, Transform>>(), osteotomyHandler);
            var success = anatomyUpdater.ImportRecut(_tempDirectory, out var partsThatChanged, out var numTrianglesImported);

            //assert
            Assert.IsTrue(success, "Update anatomy failed!");
            Assert.IsTrue(partsThatChanged.Count == 1, "Count of parts changed is not 1!");

            var plannedPartBlock = _proPlanImportComponent.GetProPlanImportBuildingBlock(osteotomyHandler.FirstOrDefault().Name);
            var retrievedPlannedPart = _objectManager.GetBuildingBlock(plannedPartBlock);
            Assert.IsTrue(retrievedPlannedPart.Attributes.UserDictionary.TryGetString(AttributeKeys.KeyOsteotomyType, out var osteotomyType), 
                "Retrieved planned part does not have osteotomy type stored in Attributes!");
            Assert.IsTrue(retrievedPlannedPart.Attributes.UserDictionary.TryGetDouble(AttributeKeys.KeyOsteotomyThickness, out var osteotomyThickness), 
                "Retrieved planned part does not have osteotomy thickness stored in Attributes!");
            Assert.IsTrue(retrievedPlannedPart.Attributes.UserDictionary.TryGetValue(AttributeKeys.KeyOsteotomyHandlerIdentifier, out var handlerIdentifier), 
                "Retrieved planned part does not have osteotomy identifier stored in Attributes!");
            Assert.IsTrue(retrievedPlannedPart.Attributes.UserDictionary.TryGetValue(AttributeKeys.KeyOsteotomyHandlerCoordinate, out double[,] handlerCoordinate), 
                "Retrieved planned part does not have osteotomy coordinate stored in Attributes!");

            Assert.AreEqual(osteotomyType, osteotomyHandler.FirstOrDefault().Type, "Osteotomy Type from object is not equal to expected!");
            Assert.AreEqual(osteotomyThickness, osteotomyHandler.FirstOrDefault().Thickness, "Osteotomy Thickness from object is not equal to expected!");
            Assert.IsTrue(ArrayUtilities.IsHasSameValuesAndElements((string[])handlerIdentifier, osteotomyHandler.FirstOrDefault().Identifier), 
                "Handler Identifier from object is not equal to expected!");
            Assert.IsTrue(ArrayUtilities.Compare2DDoubleArrays(handlerCoordinate, osteotomyHandler.FirstOrDefault().Coordinate),
                "Handler Coordinates from object is not equal to expected!");
        }

        private void Prepare_Default_Case()
        {
            //arrange
            //need to add something into Original layer to avoid exception thrown
            var originalPartName = "01GEN";
            var originalMesh = Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 5.0), 10, 10);
            var originalBlock = _proPlanImportComponent.GetProPlanImportBuildingBlock(originalPartName);
            var originalPartId = _objectManager.AddNewBuildingBlockWithTransform(originalBlock, originalMesh, Transform.Identity);
            Assert.IsTrue(originalPartId != Guid.Empty);
        }

        #endregion
    }

#endif
}