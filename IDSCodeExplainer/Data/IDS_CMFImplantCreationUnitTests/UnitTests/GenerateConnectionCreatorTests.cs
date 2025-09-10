using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.Creators;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class GenerateConnectionCreatorTests
    {
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void Creator_Throws_Exception_When_Incorrect_ComponentInfo_Given()
        {
            //Arrange
            var console = new TestConsole();
            var mockComponentInfo = new Mock<IComponentInfo>();

            //Act
            var creator = new GenerateConnectionCreator(console, mockComponentInfo.Object, new Configuration());
            creator.CreateComponentAsync();

            //Assert
            //Exception thrown
        }

        [TestMethod]
        public void Creator_Do_Not_Throw_Exception_When_Correct_ComponentInfo_Given()
        {
            //Arrange
            var console = new TestConsole();
            var componentInfo = new GenerateConnectionComponentInfo();

            //Act
            var creator = new GenerateConnectionCreator(console, componentInfo, new Configuration());
            var result = creator.CreateComponentAsync();

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GenerateConnectionComponent_Should_Be_Same_As_Original_Sharp_Curve()
        {
            // Arrange
            var console = new TestConsole();
            var resource = new TestResources();
            var workingDir = resource.GenerateConnectionWithSharpCurvesDataPath;
            var expectedGenerateConnectionOutput
                = new ExpectedGenerateConenctionOutput(workingDir);
            var generateConnectionComponentInfo
                = GetGenerateConnectionComponentInfo(workingDir);

            // Act
            GenerateConnection(console, generateConnectionComponentInfo,
                out var intermediateMeshes,
                out var intermediateObjects,
                out var errorMessages);

            // Assert
            Assert.AreEqual(0, errorMessages.Count);
            Assert.AreEqual(1, intermediateObjects.Count);
            Assert.AreEqual(1, intermediateMeshes.Count);

            // check mesh is correct or not
            var actualConnectionMesh = 
                intermediateMeshes[ConnectionKeyNames.ConnectionMeshResult];
            var expectedConnectionMesh = 
                expectedGenerateConnectionOutput.ConnectionMesh;
            TriangleSurfaceDistanceV2.DistanceMeshToMesh(console,
                actualConnectionMesh,
                expectedConnectionMesh,
                out var offsetMeshVertexDistances, 
                out var offsetMeshTriangleCenterDistances);
            Assert.IsTrue(offsetMeshVertexDistances.Max() < 0.03,
                "offsetMeshVertexDistances.Max() is more than expected amount");
            Assert.IsTrue(offsetMeshTriangleCenterDistances.Max() < 0.05,
                "offsetMeshTriangleCenterDistances.Max() is more than expected amount");

            // check curve correct or not
            var actualSharpCurves
                = (List<ICurve>) intermediateObjects
                [ConnectionKeyNames.SharpCurvesResult];
            var actualSharpCurve = actualSharpCurves.First();
            var distance = Distance.PerformDistancePointsToPoints(
                console,
                actualSharpCurve.Points,
                expectedGenerateConnectionOutput.SharpCurves.First().Points);
            Assert.IsTrue(distance.Max() < 0.01,
                "distances.Max() is more than expected amount");
            Assert.AreEqual(actualSharpCurves.Count, expectedGenerateConnectionOutput.SharpCurves.Count);
        }

        [TestMethod]
        public void GenerateConnectionComponent_Should_Be_Same_As_Original()
        {
            // Arrange
            var console = new TestConsole();
            var resource = new TestResources();
            var workingDir = resource.GenerateConnectionDataPath;
            var expectedGenerateConnectionOutput
                = new ExpectedGenerateConenctionOutput(workingDir);
            var generateConnectionComponentInfo
                = GetGenerateConnectionComponentInfo(workingDir);

            // Act
            GenerateConnection(console, generateConnectionComponentInfo,
                out var intermediateMeshes,
                out var intermediateObjects,
                out var errorMessages);

            // Assert
            Assert.AreEqual(0, errorMessages.Count);
            Assert.AreEqual(1, intermediateObjects.Count);
            Assert.AreEqual(1, intermediateMeshes.Count);

            // check mesh is correct or not
            var actualConnectionMesh =
                intermediateMeshes[ConnectionKeyNames.ConnectionMeshResult];
            var expectedConnectionMesh = 
                expectedGenerateConnectionOutput.ConnectionMesh;
            TriangleSurfaceDistanceV2.DistanceMeshToMesh(console,
                actualConnectionMesh,
                expectedConnectionMesh,
                out var offsetMeshVertexDistances,
                out var offsetMeshTriangleCenterDistances);
            Assert.IsTrue(offsetMeshVertexDistances.Max() < 0.03,
                $"offsetMeshVertexDistances.Max() is more than expected amount");
            Assert.IsTrue(offsetMeshTriangleCenterDistances.Max() < 0.03,
                $"offsetMeshTriangleCenterDistances.Max() is more than expected amount");

            // check curve correct or not
            var actualSharpCurves
                = (List<ICurve>)intermediateObjects
                [ConnectionKeyNames.SharpCurvesResult];
            Assert.AreEqual(actualSharpCurves.Count, expectedGenerateConnectionOutput.SharpCurves.Count);
        }

        [TestMethod]
        public void GenerateConnectionComponent_Should_Be_Same_As_Original_For_Sharp_Connection()
        {
            // Arrange
            var console = new TestConsole();
            var resource = new TestResources();
            var workingDir = resource.GenerateSharpConnectionDataPath;
            var expectedGenerateConnectionOutput
                = new ExpectedGenerateSharpConenctionOutput(workingDir);
            var generateConnectionComponentInfo
                = GetGenerateSharpConnectionComponentInfo(workingDir);

            // Act
            GenerateConnection(console, generateConnectionComponentInfo,
                out var intermediateMeshes,
                out var intermediateObjects,
                out var errorMessages);

            // Assert
            Assert.AreEqual(0, errorMessages.Count);
            Assert.AreEqual(0, intermediateObjects.Count);
            Assert.AreEqual(1, intermediateMeshes.Count);

            // check mesh is correct or not
            var actualConnectionMesh =
                intermediateMeshes[ConnectionKeyNames.ConnectionMeshResult];
            var expectedConnectionMesh =
                expectedGenerateConnectionOutput.ConnectionMesh;
            TriangleSurfaceDistanceV2.DistanceMeshToMesh(console,
                actualConnectionMesh,
                expectedConnectionMesh,
                out var offsetMeshVertexDistances,
                out var offsetMeshTriangleCenterDistances);
            Assert.IsTrue(offsetMeshVertexDistances.Max() < 0.04,
                "offsetMeshVertexDistances.Max() is more than expected amount");
            Assert.IsTrue(offsetMeshTriangleCenterDistances.Max() < 0.04,
                "offsetMeshTriangleCenterDistances.Max() is more than expected amount");
        }

        private GenerateConnectionComponentInfo GetGenerateConnectionComponentInfo(
            string workingDir)
        {
            var generateConnectionInput = new GenerateConnectionInput(workingDir);
            var component = new GenerateConnectionComponentInfo
            {
                IsSharpConnection = false,
                IntersectionCurve = generateConnectionInput.IntersectionCurve,
                WrapBasis = generateConnectionInput.GenerateConnection.WrapBasis,
                Width = generateConnectionInput.GenerateConnection.ConnectionWidth,
                Thickness =
                    generateConnectionInput.GenerateConnection.ConnectionThickness,
                PulledCurve = generateConnectionInput.PulledCurve,
                SupportRoIMesh = generateConnectionInput.SupportMesh
            };

            return component;
        }

        private GenerateConnectionComponentInfo GetGenerateSharpConnectionComponentInfo(
            string workingDir)
        {
            var generateConnectionInput = new GenerateSharpConnectionInput(workingDir);
            var component = new GenerateConnectionComponentInfo
            {
                IsSharpConnection = true,
                IntersectionCurve = generateConnectionInput.IntersectionCurve,
                WrapBasis = generateConnectionInput.GenerateConnection.WrapBasis,
                Width = generateConnectionInput.GenerateConnection.ConnectionWidth,
                Thickness =
                    generateConnectionInput.GenerateConnection.ConnectionThickness,
                PulledCurve = generateConnectionInput.PulledCurve,
                SupportRoIMesh = generateConnectionInput.SupportMesh
            };

            return component;
        }

        private void GenerateConnection(IConsole console, GenerateConnectionComponentInfo componentInfo,
            out Dictionary<string, IMesh> intermediateMeshes,
            out Dictionary<string, object> intermediateObjects,
            out List<string> errorMessages)
        {
            var factory = new ImplantFactory(console);
            var taskResult = factory.CreateImplantAsync(componentInfo).Result;

            intermediateMeshes = taskResult.IntermediateMeshes;
            intermediateObjects = taskResult.IntermediateObjects;
            errorMessages = taskResult.ErrorMessages;
        }
    }
}
