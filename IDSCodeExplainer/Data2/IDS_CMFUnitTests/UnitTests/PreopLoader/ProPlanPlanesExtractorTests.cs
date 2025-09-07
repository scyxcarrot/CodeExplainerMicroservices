using IDS.CMF.V2.ExternalTools;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ProPlanPlanesExtractorTests
    {
        [TestMethod]
        public void Load_Planes_Test()
        {
            // Arrange
            var resources = new TestResources();
            var proPlanPlanesExtractor = new ProPlanPlanesExtractor(new TestConsole());

            // Act
            Assert.IsTrue(proPlanPlanesExtractor.GetPlanesFromSppc(resources.SPPCFilePath),
                "Current ProPlanExtractor failed to get plane");

            // Assert
            // Expected planes are export with Test Library from previous implementation
            var expectedSagittalPlane = new IDSPlane(
                new IDSPoint3D(116.2, 115.2, -86.25),
                new IDSVector3D(-0.99604286785405138, -0.079642385512587566, 0.039442310111621748));
            AreEqual(expectedSagittalPlane, proPlanPlanesExtractor.SagittalPlane, "SagittalPlane");

            var expectedAxialPlane = new IDSPlane(
                new IDSPoint3D(116.2, 115.2, -86.25),
                new IDSVector3D(0.041875653729199595, -0.02911730847363583, 0.99869845898149034));
            AreEqual(expectedAxialPlane, proPlanPlanesExtractor.AxialPlane, "AxialPlane");

            var expectedCoronalPlane = new IDSPlane(
                new IDSPoint3D(116.2, 115.2, -86.25),
                new IDSVector3D(-0.078390273770598068, 0.99639814972585916, 0.032337164394163835));
            AreEqual(expectedCoronalPlane, proPlanPlanesExtractor.CoronalPlane, "CoronalPlane");

            var expectedMidSagittalPlane = new IDSPlane(
                new IDSPoint3D(99.1629122144462, 232.01191830778228, -168.64445554160298),
                new IDSVector3D(0.99604286785405127, 0.079642385512588329, -0.03944231011162172));
            AreEqual(expectedMidSagittalPlane, proPlanPlanesExtractor.MidSagittalPlane, "MidSagittalPlane");
        }

        private void AreEqual(IPlane expected, IPlane actual, string planeName)
        {
            const double epsilon = 0.0001;

            Assert.AreEqual(expected.Origin.X, actual.Origin.X, epsilon, $"{planeName}.Origin.X");
            Assert.AreEqual(expected.Origin.Y, actual.Origin.Y, epsilon, $"{planeName}.Origin.Y");
            Assert.AreEqual(expected.Origin.Z, actual.Origin.Z, epsilon, $"{planeName}.Origin.Z");

            Assert.AreEqual(expected.Normal.X, actual.Normal.X, epsilon, $"{planeName}.Normal.X");
            Assert.AreEqual(expected.Normal.Y, actual.Normal.Y, epsilon, $"{planeName}.Normal.Y");
            Assert.AreEqual(expected.Normal.Z, actual.Normal.Z, epsilon, $"{planeName}.Normal.Z");
        }
    }
}
