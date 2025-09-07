using IDS.CMF.DataModel;
using IDS.CMF.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    /// <summary>
    /// Summary description for ImplantTemplateDataModelTests
    /// </summary>
    [TestClass]
    public class ImplantTemplateDataModelTests
    {
        [TestMethod]
        public void ImplantTemplateSchemaValid()
        {
            //Arrange
            var resource = new CMFResources();
            var implantTemplateValidator = new ImplantTemplateDataModelValidator();

            //Act
            var isValid = implantTemplateValidator.IsValidImplantTemplateXml(resource.ImplantTemplateXmlPath);

            //Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void ImplantTemplateSchemaInvalid()
        {
            //Arrange
            var resource = new TestResources();
            var implantTemplateValidator = new ImplantTemplateDataModelValidator();

            ////Act
            var isValid = implantTemplateValidator.IsValidImplantTemplateXml(resource.ImplantTemplateInvalidXmlPath);

            ////Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void ImplantTemplateDataModelValid()
        {
            //Arrange
            var implantTemplateGroupsDataModel = ImplantTemplateGroupsDataModelManager.LoadImplantTemplate();
            var implantTemplateValidator = new ImplantTemplateDataModelValidator();

            ////Act
            var isValid = implantTemplateValidator.IsValidImplantTemplateGroupsDataModel(implantTemplateGroupsDataModel);

            ////Assert
            Assert.IsTrue(isValid);
        }
    }
}
