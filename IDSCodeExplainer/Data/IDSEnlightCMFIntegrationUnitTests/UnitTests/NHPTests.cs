using IDS.EnlightCMFIntegration.DataModel;
using IDS.EnlightCMFIntegration.Operations;
using IDS.EnlightCMFIntegration.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDSEnlightCMFIntegration.Testing.UnitTests
{
    [TestClass]
    public class NHPTests
    {
        [TestMethod]
        public void There_Should_Be_Only_One_Sagittal_Plane_In_A_Case()
        {
            There_Should_Be_Only_One_Unique_Plane_In_A_Case(NHPPlane.SagittalPlaneName);
        }

        [TestMethod]
        public void There_Should_Be_Only_One_Axial_Plane_In_A_Case()
        {
            There_Should_Be_Only_One_Unique_Plane_In_A_Case(NHPPlane.AxialPlaneName);
        }

        [TestMethod]
        public void There_Should_Be_Only_One_Coronal_Plane_In_A_Case()
        {
            There_Should_Be_Only_One_Unique_Plane_In_A_Case(NHPPlane.CoronalPlaneName);
        }

        private void There_Should_Be_Only_One_Unique_Plane_In_A_Case(string uniqueName)
        {
            var resource = new TestResources();

            //Act: Retrieve planes from mcs file using integration project
            var reader = new EnlightCMFReader(resource.EnlightCmfFullWorkflow1FilePath);

            List<PlaneProperties> planes;
            reader.GetAllPlaneProperties(out planes);

            var planeProperties = planes.Where(s => s.Name.Equals(uniqueName));
            Assert.IsTrue(planeProperties.Count() == 1);
        }
    }
}
