using IDS.EnlightCMFIntegration.DataModel;
using IDS.EnlightCMFIntegration.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDSEnlightCMFIntegration.Testing.UnitTests
{
    [TestClass]
    public class PlaneTests
    {
        [TestMethod]
        public void Sagittal_Plane_Exported_From_Integration_Has_Same_Property_Values()
        {
            //Arrange
            var originFromMimics = new double[]
            {
                8.0000,
                -196.0000,
                -115.7500
            };

            var normalFromMimics = new double[]
            {
                1.0000,
                0.0000,
                0.0000
            };

            Plane_Exported_From_Integration_Has_Same_Property_Values(originFromMimics, normalFromMimics, "SAGITTAL");
        }

        [TestMethod]
        public void Axial_Plane_Exported_From_Integration_Has_Same_Property_Values()
        {
            //Arrange
            var originFromMimics = new double[]
            {
                8.0000,
                -196.0000,
                -115.7500
            };

            var normalFromMimics = new double[]
            {
                0.0000,
                0.0000,
                1.0000
            };

            Plane_Exported_From_Integration_Has_Same_Property_Values(originFromMimics, normalFromMimics, "AXIAL");
        }

        [TestMethod]
        public void Coronal_Plane_Exported_From_Integration_Has_Same_Property_Values()
        {
            //Arrange
            var originFromMimics = new double[]
            {
                8.0000,
                -196.0000,
                -115.7500
            };

            var normalFromMimics = new double[]
            {
                0.0000,
                1.0000,
                0.0000
            };

            Plane_Exported_From_Integration_Has_Same_Property_Values(originFromMimics, normalFromMimics, "CORONAL");
        }

        private void Plane_Exported_From_Integration_Has_Same_Property_Values(double[] originFromMimics, double[] normalFromMimics, string planeName)
        {
            var resource = new TestResources();

            //Act: Retrieve plane's origin and normal values from mcs file using integration project
            var reader = new EnlightCMFReader(resource.EnlightCmfFilePath);

            List<PlaneProperties> planes;
            reader.GetAllPlaneProperties(out planes);

            var planePropertiesFromIntegration = planes.First(s => s.Name.ToUpper().Contains("NHP") && s.Name.ToUpper().Contains(planeName.ToUpper()));

            //Assert
            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(originFromMimics[i], planePropertiesFromIntegration.Origin[i], 0.001);
                Assert.AreEqual(normalFromMimics[i], planePropertiesFromIntegration.Normal[i], 0.001);
            }
        }
    }
}
