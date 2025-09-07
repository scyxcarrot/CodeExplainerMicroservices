using IDS.CMF.TestLib.Components;
using IDS.Core.V2.Geometries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonUtilities = IDS.Core.V2.Utilities.JsonUtilities;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class MedicalCoordinateSystemConvertTests
    {
        [TestMethod]
        public void TestConvertMedicalCoordinateSystem()
        {
            // Arrange
            var sagittalPlane = new IDSPlane(new IDSPoint3D(1, 2, 3), new IDSVector3D(0, 1, 0));
            var coronalPlane = new IDSPlane(new IDSPoint3D(1, 2, 3), new IDSVector3D(1, 0, 0));
            var axialPlane = new IDSPlane(new IDSPoint3D(1, 2, 3), new IDSVector3D(0, 0, 1));

            var medicalCoordinateSystem = new MedicalCoordinateSystemComponent();
            medicalCoordinateSystem.SagittalPlane = sagittalPlane;
            medicalCoordinateSystem.CoronalPlane = coronalPlane;
            medicalCoordinateSystem.AxialPlane = axialPlane;

            // Act
            var config = JsonUtilities.Serialize(medicalCoordinateSystem);
            var generatedMedicalCoordinateSystem = JsonUtilities.Deserialize<MedicalCoordinateSystemComponent>(config);

            // Assert
            var epsilon = 0.0001;
            Assert.IsTrue(generatedMedicalCoordinateSystem.AxialPlane.EpsilonEquals(axialPlane, epsilon));
            Assert.IsTrue(generatedMedicalCoordinateSystem.CoronalPlane.EpsilonEquals(coronalPlane, epsilon));
            Assert.IsTrue(generatedMedicalCoordinateSystem.SagittalPlane.EpsilonEquals(sagittalPlane, epsilon));
        }
    }
}
