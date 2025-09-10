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
    public class ConnectionCreatorTests
    {
        [TestMethod]
        public void Creator_Throws_Exception_When_Incorrect_ComponentInfo_Given()
        {
            //Arrange
            var console = new TestConsole();
            var mockComponentInfo = new Mock<IComponentInfo>();

            //Act
            var creator = new ConnectionCreator(console, mockComponentInfo.Object, new Configuration());

            try
            {
                creator.CreateComponentAsync();
            }
            catch (Exception exception)
            {
                //Assert
                //Exception thrown
                Assert.IsInstanceOfType(exception, typeof(Exception));
            }
        }

        [TestMethod]
        public void Creator_Do_Not_Throw_Exception_When_Correct_ComponentInfo_Given()
        {
            //Arrange
            var console = new TestConsole();
            var componentInfo = new ConnectionComponentInfo();

            //Act
            var creator = new ConnectionCreator(console, componentInfo, new Configuration());
            var result = creator.CreateComponentAsync();

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void ConnectionCreatorComponent_Should_Be_Same_As_Original()
        {
            // Arrange
            var console = new TestConsole();
            var resource = new TestResources();
            var workingDir = resource.ConnectionDataPath;
            var expectedConnectionOutput
                = new ExpectedConnectionOutput(workingDir);
            var connectionComponentInfo
                = GetConnectionComponentInfo(workingDir);

            // Act
            CreateConnection(console, connectionComponentInfo,
                out var intermediateMeshes,
                out var intermediateObjects,
                out var errorMessages,
                out var componentMesh);

            // Assert
            Assert.AreEqual(0, errorMessages.Count);

            // check mesh is correct or not
            var actualConnectionMesh = componentMesh;
            var expectedConnectionMesh =
                expectedConnectionOutput.ConnectionMesh;
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
                = (List<ICurve>)intermediateObjects
                [ConnectionKeyNames.SharpCurvesResult];
            Assert.IsFalse(actualSharpCurves.Any());
        }

        private ConnectionComponentInfo GetConnectionComponentInfo(
            string workingDir)
        {
            var connectionInput = new ConnectionInput(workingDir);
            var component = new ConnectionComponentInfo
            {
                Id = Guid.NewGuid(),
                DisplayName = "UnitTest",
                IsActual = connectionInput.ConnectionInputValue.IsActual,
                AverageConnectionDirection =
                    connectionInput.ConnectionInputValue.AverageConnectionDirection,
                Width = connectionInput.ConnectionInputValue.Width,
                Thickness = connectionInput.ConnectionInputValue.Thickness,
                ConnectionCurve = connectionInput.ConnectionCurve,
                SupportMeshFull = connectionInput.SupportMeshFull,
                SupportRoIMesh = connectionInput.SupportMeshRoI,
            };

            return component;
        }

        private void CreateConnection(IConsole console,
            ConnectionComponentInfo componentInfo,
            out Dictionary<string, IMesh> intermediateMeshes,
            out Dictionary<string, object> intermediateObjects,
            out List<string> errorMessages,
            out IMesh componentMesh)
        {
            var factory = new ImplantFactory(console);
            var taskResult = factory.CreateImplantAsync(componentInfo).Result;

            intermediateMeshes = taskResult.IntermediateMeshes;
            intermediateObjects = taskResult.IntermediateObjects;
            errorMessages = taskResult.ErrorMessages;
            componentMesh = taskResult.ComponentMesh;
        }
    }
}
