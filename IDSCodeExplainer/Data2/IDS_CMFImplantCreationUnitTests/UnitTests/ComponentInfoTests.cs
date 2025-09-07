using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class ComponentInfoTests
    {
        [TestMethod]
        public void ToDefaultComponentInfo_Will_Set_Values_To_Properties()
        {
            //Arrange
            var id = Guid.NewGuid();
            var displayName = "DisplayName";
            var isActual = true;
            var needToFinalize = false;
            var partToStl = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestData", "Cylinder.stl");
            var console = new TestConsole();

            var mockFileIOComponentInfo = new Mock<IFileIOComponentInfo>();
            mockFileIOComponentInfo.Setup(x => x.Id).Returns(id);
            mockFileIOComponentInfo.Setup(x => x.DisplayName).Returns(displayName);
            mockFileIOComponentInfo.Setup(x => x.IsActual).Returns(isActual);
            mockFileIOComponentInfo.Setup(x => x.NeedToFinalize).Returns(needToFinalize);
            mockFileIOComponentInfo.Setup(x => x.ClearanceMeshSTLFilePath).Returns(partToStl);
            mockFileIOComponentInfo.Setup(x => x.SubtractorsSTLFilePaths).Returns(new List<string> { partToStl });
            mockFileIOComponentInfo.Setup(x => x.ComponentMeshesSTLFilePaths).Returns(new List<string> { partToStl });

            //Act
            var componentInfo = mockFileIOComponentInfo.Object.ToDefaultComponentInfo<PastilleComponentInfo>(console);

            //Assert
            Assert.AreEqual(id, componentInfo.Id);
            Assert.AreEqual(displayName, componentInfo.DisplayName);
            Assert.AreEqual(isActual, componentInfo.IsActual);
            Assert.AreEqual(needToFinalize, componentInfo.NeedToFinalize);
            Assert.IsNotNull(componentInfo.ClearanceMesh);
            Assert.IsNotNull(componentInfo.Subtractors);
            Assert.IsTrue(componentInfo.Subtractors.Count == 1);
            Assert.IsNotNull(componentInfo.ComponentMeshes);
            Assert.IsTrue(componentInfo.ComponentMeshes.Count == 1);
        }

        [TestMethod]
        public void Connection_ToDefaultComponentInfo_Will_Set_Values_To_Properties()
        {
            //Arrange
            var id = Guid.NewGuid();
            var displayName = "DisplayName";
            var isActual = true;
            var needToFinalize = false;
            var partToStl = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestData", "Cylinder.stl");
            var console = new TestConsole();

            var mockFileIOComponentInfo = new Mock<IFileIOComponentInfo>();
            mockFileIOComponentInfo.Setup(x => x.Id).Returns(id);
            mockFileIOComponentInfo.Setup(x => x.DisplayName).Returns(displayName);
            mockFileIOComponentInfo.Setup(x => x.IsActual).Returns(isActual);
            mockFileIOComponentInfo.Setup(x => x.NeedToFinalize).Returns(needToFinalize);
            mockFileIOComponentInfo.Setup(x => x.ClearanceMeshSTLFilePath).Returns(partToStl);
            mockFileIOComponentInfo.Setup(x => x.SubtractorsSTLFilePaths).Returns(new List<string> { partToStl });
            mockFileIOComponentInfo.Setup(x => x.ComponentMeshesSTLFilePaths).Returns(new List<string> { partToStl });

            //Act
            var componentInfo = mockFileIOComponentInfo.Object.ToDefaultComponentInfo<ConnectionComponentInfo>(console);

            //Assert
            Assert.AreEqual(id, componentInfo.Id);
            Assert.AreEqual(displayName, componentInfo.DisplayName);
            Assert.AreEqual(isActual, componentInfo.IsActual);
            Assert.AreEqual(needToFinalize, componentInfo.NeedToFinalize);
            Assert.IsNotNull(componentInfo.ClearanceMesh);
            Assert.IsNotNull(componentInfo.Subtractors);
            Assert.IsTrue(componentInfo.Subtractors.Count == 1);
            Assert.IsNotNull(componentInfo.ComponentMeshes);
            Assert.IsTrue(componentInfo.ComponentMeshes.Count == 1);
        }
    }
}
