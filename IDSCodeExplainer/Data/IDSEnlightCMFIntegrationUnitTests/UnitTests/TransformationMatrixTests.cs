using IDS.EnlightCMFIntegration.DataModel;
using IDS.EnlightCMFIntegration.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDSEnlightCMFIntegration.Testing.UnitTests
{
    [TestClass]
    public class TransformationMatrixTests
    {
        [TestMethod]
        public void Transformation_Matrix_Exported_From_Integration_Is_Same_As_Values()
        {
            var resource = new TestResources();

            //Arrange
            var transformationMatrixFromMimics = new double[]
            {
                0.9986240082132765,
                -0.05243638452709234,
                0.0007181906271720935,
                -10.183392867921144,
                0.05240310896155629,
                0.9972805224844477,
                -0.051821555788209274,
                -6.950587547225932,
                0.002001097502193073,
                0.051787885174760316,
                0.9986561022483729,
                9.978860549484637,
                0.0,
                0.0,
                0.0,
                1.0
            };

            //Act: Retrieve part's transformation matrix values from mcs file using integration project
            var reader = new EnlightCMFReader(resource.EnlightCmfFilePath);

            List<StlProperties> stls;
            reader.GetAllStlProperties(out stls);

            var stlPropertiesFromIntegration = stls.First(s => s.Name.ToUpper() == "01GEN");

            //Assert
            for (var i = 0; i < 16; i++)
            {
                Assert.AreEqual(transformationMatrixFromMimics[i], stlPropertiesFromIntegration.TransformationMatrix[i], 0.001);
            }
        }
    }
}
