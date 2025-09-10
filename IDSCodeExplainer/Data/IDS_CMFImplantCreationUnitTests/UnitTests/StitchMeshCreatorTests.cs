using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.Creators;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.Extensions;
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
    public class StitchMeshCreatorTests
    {
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void Creator_Throws_Exception_When_Incorrect_ComponentInfo_Given()
        {
            //Arrange
            var console = new TestConsole();
            var mockComponentInfo = new Mock<IComponentInfo>();

            //Act
            var creator = new StitchMeshCreator(console, mockComponentInfo.Object, new Configuration());
            creator.CreateComponentAsync();

            //Assert
            //Exception thrown
        }

        [TestMethod]
        public void Creator_Do_Not_Throw_Exception_When_Correct_ComponentInfo_Given()
        {
            //Arrange
            var console = new TestConsole();
            var componentInfo = new StitchMeshComponentInfo();

            //Act
            var creator = new StitchMeshCreator(console, componentInfo, new Configuration());
            var result = creator.CreateComponentAsync();

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void StitchMeshComponent_Should_Return_Same_Mesh_As_Original_Algo()
        {
            // Arrange
            var console = new TestConsole();
            var resource = new TestResources();
            var workingDir = resource.StitchMeshDataPath;
            var expectedStitchMeshOutput = new ExpectedStitchMeshOutput(workingDir);
            var stitchMeshComponentInfo = GetStitchMeshComponentInfo(workingDir);

            // Act
            CreateStitchMesh(console, stitchMeshComponentInfo,
                out var intermediateMeshes,
                out var intermediateObjects,
                out var errorMessages);

            // Assert
            Assert.AreEqual(0, errorMessages.Count);
            Assert.AreEqual(0, intermediateObjects.Count);

            var actualOffsetMesh = intermediateMeshes[PastilleKeyNames.OffsetStitchMeshResult];
            var expectedOffsetMesh = expectedStitchMeshOutput.OffsetMesh;
            TriangleSurfaceDistanceV2.DistanceBetween(console,
                actualOffsetMesh.Faces.ToFacesArray2D(), actualOffsetMesh.Vertices.ToVerticesArray2D(),
                expectedOffsetMesh.Faces.ToFacesArray2D(), expectedOffsetMesh.Vertices.ToVerticesArray2D(),
                out var offsetMeshVertexDistances, out var offsetMeshTriangleCenterDistances);
            Assert.IsTrue(offsetMeshVertexDistances.Max() < 0.01,
                $"offsetMeshVertexDistances.Max() = {offsetMeshTriangleCenterDistances.Max()}");
            Assert.IsTrue(offsetMeshTriangleCenterDistances.Max() < 0.01,
                $"offsetMeshTriangleCenterDistances.Max() = {offsetMeshTriangleCenterDistances.Max()}");

            var actualStitchedMesh = intermediateMeshes[PastilleKeyNames.StitchedStitchMeshResult];
            var expectedStitchedMesh = expectedStitchMeshOutput.StitchedMesh;
            TriangleSurfaceDistanceV2.DistanceBetween(console,
                actualStitchedMesh.Faces.ToFacesArray2D(), actualStitchedMesh.Vertices.ToVerticesArray2D(),
                expectedStitchedMesh.Faces.ToFacesArray2D(), expectedStitchedMesh.Vertices.ToVerticesArray2D(),
                out var stitchedVertexDistances, out var stitchedTriangleCenterDistances);
            Assert.IsTrue(stitchedVertexDistances.Max() < 0.01,
                $"stitchedVertexDistances.Max() = {stitchedVertexDistances.Max()}");
            Assert.IsTrue(stitchedTriangleCenterDistances.Max() < 0.01,
                $"stitchedTriangleCenterDistances.Max() = {stitchedTriangleCenterDistances.Max()}");
        }

        private StitchMeshComponentInfo GetStitchMeshComponentInfo(string workingDir)
        {
            var stitchMeshInput = new StitchMeshInput(workingDir);
            var component = new StitchMeshComponentInfo
            {
                TopMesh = stitchMeshInput.TopMesh,
                BottomMesh = stitchMeshInput.BottomMesh
            };

            return component;
        }

        private void CreateStitchMesh(IConsole console, StitchMeshComponentInfo componentInfo,
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
