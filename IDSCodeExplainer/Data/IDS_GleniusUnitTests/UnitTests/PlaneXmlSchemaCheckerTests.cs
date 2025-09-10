using IDS.Glenius.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class PlaneXmlSchemaCheckerTests
    {
        [TestMethod]
        public void Single_Plane_Xml_Will_Pass()
        {
            //Arrange
            var resource = new TestResources();

            //Act
            var checkComplete = XmlSchemaChecker.ValidatePlaneXml(resource.SinglePlaneXmlFile);

            //Assert
            Assert.IsTrue(checkComplete);
        }
        
        [TestMethod]
        public void Multiple_Planes_Xml_Will_Fail()
        {
            //Arrange
            var resource = new TestResources();

            //Act
            var checkComplete = XmlSchemaChecker.ValidatePlaneXml(resource.MultiplePlanesXmlFile);

            //Assert
            Assert.IsFalse(checkComplete);
        }
        
        [TestMethod]
        public void Xml_With_Non_Plane_Entities_Will_Fail()
        {
            //Arrange
            var resource = new TestResources();

            //Act
            var checkComplete = XmlSchemaChecker.ValidatePlaneXml(resource.MultipleEntitiesXmlFile);

            //Assert
            Assert.IsFalse(checkComplete);
        }

        [TestMethod]
        public void Xml_With_No_Plane_Will_Fail()
        {
            //Arrange
            var resource = new TestResources();

            //Act
            var checkComplete = XmlSchemaChecker.ValidatePlaneXml(resource.NoPlaneXmlFile);

            //Assert
            Assert.IsFalse(checkComplete);
        }
    }
}
