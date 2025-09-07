using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.Creators;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class ComponentCreatorFactoryTests
    {
        [TestMethod]
        public void PastilleCreator_Is_Returned_When_Given_PastilleComponentInfo()
        {
            var componentInfo = new PastilleComponentInfo();
            var expectedCreatorType = typeof(PastilleCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_ComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void IntersectionCurveCreator_Is_Returned_When_Given_IntersectionCurveComponentInfo()
        {
            var componentInfo = new PastilleIntersectionCurveComponentInfo();
            var expectedCreatorType = typeof(PastilleIntersectionCurveCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_ComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void ExtrusionCreator_Is_Returned_When_Given_ExtrusionComponentInfo()
        {
            var componentInfo = new ExtrusionComponentInfo();
            var expectedCreatorType = typeof(ExtrusionCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_ComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void PatchCreator_Is_Returned_When_Given_PatchComponentInfo()
        {
            var componentInfo = new PatchComponentInfo();
            var expectedCreatorType = typeof(PatchCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_ComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void StitchMeshCreator_Is_Returned_When_Given_StitchMeshComponentInfo()
        {
            var componentInfo = new StitchMeshComponentInfo();
            var expectedCreatorType = typeof(StitchMeshCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_ComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void ScrewStampImprintCreator_Is_Returned_When_Given_ScrewStampImprintComponentInfo()
        {
            var componentInfo = new ScrewStampImprintComponentInfo();
            var expectedCreatorType = typeof(ScrewStampImprintCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_ComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void FinalizationCreator_Is_Returned_When_Given_FinalizationComponentInfo()
        {
            var componentInfo = new FinalizationComponentInfo();
            var expectedCreatorType = typeof(FinalizationCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_ComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void ConnectionIntersectionCurveCreator_Is_Returned_When_Given_ConnectionIntersectionCurveComponentInfo()
        {
            var componentInfo = new ConnectionIntersectionCurveComponentInfo();
            var expectedCreatorType = typeof(ConnectionIntersectionCurveCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_ComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void GenerateConnectionCreator_Is_Returned_When_Given_GenerateConnectionComponentInfo()
        {
            var componentInfo = new GenerateConnectionComponentInfo();
            var expectedCreatorType = typeof(GenerateConnectionCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_ComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void ConnectionCreator_Is_Returned_When_Given_ConnectionComponentInfo()
        {
            var componentInfo = new ConnectionComponentInfo();
            var expectedCreatorType = typeof(ConnectionCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_ComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void LandmarkCreator_Is_Returned_When_Given_LandmarkComponentInfo()
        {
            var componentInfo = new LandmarkComponentInfo();
            var expectedCreatorType = typeof(LandmarkCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_ComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void PastilleCreator_Is_Returned_When_Given_PastilleFileIOComponentInfo()
        {
            var componentInfo = new PastilleFileIOComponentInfo();
            var expectedCreatorType = typeof(PastilleCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void IntersectionCurveCreator_Is_Returned_When_Given_IntersectionCurveFileIOComponentInfo()
        {
            var componentInfo = new PastilleIntersectionCurveFileIOComponentInfo();
            var expectedCreatorType = typeof(PastilleIntersectionCurveCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void ExtrusionCreator_Is_Returned_When_Given_ExtrusionFileIOComponentInfo()
        {
            var componentInfo = new ExtrusionFileIOComponentInfo();
            var expectedCreatorType = typeof(ExtrusionCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void PatchCreator_Is_Returned_When_Given_PatchFileIOComponentInfo()
        {
            var componentInfo = new PatchFileIOComponentInfo();
            var expectedCreatorType = typeof(PatchCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void StitchMeshCreator_Is_Returned_When_Given_StitchMeshFileIOComponentInfo()
        {
            var componentInfo = new StitchMeshFileIOComponentInfo();
            var expectedCreatorType = typeof(StitchMeshCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void ScrewStampImprintCreator_Is_Returned_When_Given_ScrewStampImprintFileIOComponentInfo()
        {
            var componentInfo = new ScrewStampImprintFileIOComponentInfo();
            var expectedCreatorType = typeof(ScrewStampImprintCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void FinalizationCreator_Is_Returned_When_Given_FinalizationFileIOComponentInfo()
        {
            var componentInfo = new FinalizationFileIOComponentInfo();
            var expectedCreatorType = typeof(FinalizationCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void ConnectionIntersectionCurveCreator_Is_Returned_When_Given_ConnectionIntersectionCurveFileIOComponentInfo()
        {
            var componentInfo = new ConnectionIntersectionCurveFileIOComponentInfo();
            var expectedCreatorType = typeof(ConnectionIntersectionCurveCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void GenerateConnectionCreator_Is_Returned_When_Given_GenerateConnectionFileIOComponentInfo()
        {
            var componentInfo = new GenerateConnectionFileIOComponentInfo();
            var expectedCreatorType = typeof(GenerateConnectionCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void ConnectionCreator_Is_Returned_When_Given_ConnectionFileIOComponentInfo()
        {
            var componentInfo = new ConnectionFileIOComponentInfo();
            var expectedCreatorType = typeof(ConnectionCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void LandmarkCreator_Is_Returned_When_Given_LandmarkFileIOComponentInfo()
        {
            var componentInfo = new LandmarkFileIOComponentInfo();
            var expectedCreatorType = typeof(LandmarkCreator);
            ComponentFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo(componentInfo, expectedCreatorType);
        }

        private void ComponentFactory_Returns_Correct_Creator_Based_On_Given_ComponentInfo(IComponentInfo componentInfo, Type expectedCreatorType)
        {
            //Arrange
            var console = new TestConsole();

            //Act
            var componentFactory = new ComponentFactory();
            var creator = componentFactory.CreateComponentCreator(console, componentInfo, new Configuration());

            //Assert
            Assert.IsTrue(creator.GetType() == expectedCreatorType);
        }

        private void ComponentFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo(IFileIOComponentInfo componentInfo, Type expectedCreatorType)
        {
            //Arrange
            var console = new TestConsole();

            //Act
            var componentFactory = new ComponentFactory();
            var creator = componentFactory.CreateComponentCreatorFromFile(console, componentInfo, new Configuration());

            //Assert
            Assert.IsTrue(creator.GetType() == expectedCreatorType);
        }
    }
}
