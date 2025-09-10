using IDS.CMF.DataModel;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    public static class DotTestUtilities
    {
        public static void AssertDotAreEqual(IDot expected, IDot actual)
        {
            Assert.AreEqual(expected.GetType(), actual.GetType(), "Dot type is not match");

            Assert.AreEqual(expected.Location.X, actual.Location.X, 0.0001,
                "Dot location X is not match");
            Assert.AreEqual(expected.Location.Y, actual.Location.Y, 0.0001,
                "Dot location Y is not match");
            Assert.AreEqual(expected.Location.Z, actual.Location.Z, 0.0001,
                "Dot location Z is not match");

            Assert.AreEqual(expected.Direction.X, actual.Direction.X, 0.0001,
                "Dot direction X is not match");
            Assert.AreEqual(expected.Direction.Y, actual.Direction.Y, 0.0001,
                "Dot direction Y is not match");
            Assert.AreEqual(expected.Direction.Z, actual.Direction.Z, 0.0001,
                "Dot direction Z is not match");

            if ((expected is DotPastille expectedPastille) &&
                (actual is DotPastille actualPastille))
            {
                AssertDotPastilleAreEqual(expectedPastille, actualPastille);
            }
        }

        public static void AssertDotPastilleAreEqual(DotPastille expected, DotPastille actual)
        {
            Assert.AreEqual(expected.Id, actual.Id, "pastille Id is not match");
            Assert.AreEqual(expected.Diameter, actual.Diameter, "pastille diameter is not match");
            Assert.AreEqual(expected.Thickness, actual.Thickness, "pastille thickness is not match");
            Assert.AreEqual(expected.CreationAlgoMethod, actual.CreationAlgoMethod,
                "pastille create algo is not match");

            Assert.AreEqual(expected.Screw == null, actual.Screw == null,
                "one of the pastille screw is null but the other is not");
            if (expected.Screw != null && actual.Screw != null)
            {
                Assert.AreEqual(expected.Screw.Id, actual.Screw.Id,
                    "pastille screw Id is not match");
            }

            Assert.AreEqual(expected.Landmark == null, actual.Landmark == null,
                "one of the pastille landmark is null but the other is not");

            if (expected.Landmark != null && actual.Landmark != null)
            {
                Assert.AreEqual(expected.Landmark.Id, actual.Landmark.Id,
                    "pastille landmark Id is not match");
                Assert.AreEqual(expected.Landmark.LandmarkType, actual.Landmark.LandmarkType,
                    "pastille landmark type is not match");
                Assert.AreEqual(expected.Landmark.Point.X, actual.Landmark.Point.X, 0.0001,
                    "pastille landmark point X is not match");
                Assert.AreEqual(expected.Landmark.Point.Y, actual.Landmark.Point.Y, 0.0001,
                    "pastille landmark point Y is not match");
                Assert.AreEqual(expected.Landmark.Point.Z, actual.Landmark.Point.Z, 0.0001,
                    "pastille landmark point Z is not match");
            }
        }
    }
}
