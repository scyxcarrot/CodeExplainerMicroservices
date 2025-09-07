using IDS.CMF.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class CasePrefXmlSchemaTests
    {
        [TestMethod]
        public void CasePrefSchemaValid()
        {
            //Arrange
            var resource = new TestResources();

            //Act
            var checkComplete = CasePreferencesXmlSchemaChecker.ValidateCasePrefXml(resource.CasePreferencesXmlPath);

            //Assert
            Assert.IsTrue(checkComplete);
        }

        [TestMethod]
        public void CasePrefSchemaInvalid()
        {
            //Arrange
            var resource = new TestResources();

            //Act
            var checkComplete = CasePreferencesXmlSchemaChecker.ValidateCasePrefXml(resource.CasePreferencesInvalidXmlPath);

            //Assert
            Assert.IsFalse(checkComplete);
        }
    }
}
