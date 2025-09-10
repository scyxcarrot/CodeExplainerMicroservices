using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.Creators;
using IDS.CMFImplantCreation.DTO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class ExtrusionCreatorTests
    {
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void Creator_Throws_Exception_When_Incorrect_ComponentInfo_Given()
        {        
            //Arrange
            var console = new TestConsole();
            var mockComponentInfo = new Mock<IComponentInfo>();

            //Act
            var creator = new ExtrusionCreator(console, mockComponentInfo.Object, new Configuration());
            creator.CreateComponentAsync();

            //Assert
            //Exception thrown
        }

        [TestMethod]
        public void Creator_Do_Not_Throw_Exception_When_Correct_ComponentInfo_Given()
        {
            //Arrange
            var console = new TestConsole();
            var componentInfo = new ExtrusionComponentInfo();

            //Act
            var creator = new ExtrusionCreator(console, componentInfo, new Configuration());
            var result = creator.CreateComponentAsync();

            //Assert
            Assert.IsNotNull(result);
        }
    }
}
