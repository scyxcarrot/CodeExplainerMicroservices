using IDS.CMF.ScrewQc;
using IDS.Core.V2.Geometries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class OsteotomyDistanceScrewQcCheckTests
    {
        [TestMethod]
        public void OsteotomyDistanceScrewQcCheck_Should_Return_Pass_For_Distance_More_Than_Minimun_Value()
        {
            var testPoint = new IDSPoint3D(2.0, 7.3223, 11.8215);
            var expectedDistance = 4.4277;

            var result = RunOsteotomyDistanceScrewQcCheck(testPoint, 4.4);

            //assert
            Assert.IsTrue(result.IsOk);
            Assert.AreEqual(expectedDistance, result.Distance, 0.01);
        }

        [TestMethod]
        public void OsteotomyDistanceScrewQcCheck_Should_Return_Fail_For_Distance_Lesser_Than_Minimun_Value()
        {
            var testPoint = new IDSPoint3D(2.0, 9.7629, 5.8421);
            var expectedDistance = 1.9871;

            var result = RunOsteotomyDistanceScrewQcCheck(testPoint, 2.0);

            //assert
            Assert.IsFalse(result.IsOk);
            Assert.AreEqual(expectedDistance, result.Distance, 0.01);
        }

        [TestMethod]
        public void OsteotomyDistanceScrewQcCheck_Should_Return_Distance_NaN_For_Case_With_No_Osteotomy()
        {
            var testPoint = new IDSPoint3D(2.0, 9.7629, 5.8421);

            var result = RunOsteotomyDistanceScrewQcCheck(testPoint, 2.0, true);

            //assert
            Assert.IsTrue(result.IsOk);
            Assert.IsTrue(double.IsNaN(result.Distance));
        }

        [TestMethod]
        public void OsteotomyDistanceScrewQcCheck_Should_Return_Distance_NaN_For_Screw_On_Graft()
        {
            var testPoint = new IDSPoint3D(2.0, 9.7629, 5.8421);

            var result = RunOsteotomyDistanceScrewQcCheck(testPoint, 2.0, false, "05FIB_part1");

            //assert
            Assert.IsTrue(result.IsOk);
            Assert.IsTrue(double.IsNaN(result.Distance));
        }

        [TestMethod]
        public void OsteotomyDistanceScrewQcCheck_Should_Return_Distance_NaN_For_Screw_Without_Planned_Bone()
        {
            var testPoint = new IDSPoint3D(2.0, 9.7629, 5.8421);

            var result = RunOsteotomyDistanceScrewQcCheck(testPoint, 2.0, false, "05MAN_teeth");

            //assert
            Assert.IsTrue(result.IsOk);
            Assert.IsTrue(double.IsNaN(result.Distance));
        }

        [TestMethod]
        public void OsteotomyDistanceScrewQcCheck_Should_Return_Distance_NaN_For_Screw_Without_Original_Bone()
        {
            var testPoint = new IDSPoint3D(2.0, 9.7629, 5.8421);

            var result = RunOsteotomyDistanceScrewQcCheck(testPoint, 2.0, false, "05GEN", "01GEN1");

            //assert
            Assert.IsTrue(result.IsOk);
            Assert.IsTrue(double.IsNaN(result.Distance));
        }

        [TestMethod]
        public void OsteotomyDistanceScrewQcCheck_Should_Not_Return_Distance_From_Osteotomy_Edge_Behind_Surface_1()
        {
            var testPoint = new IDSPoint3D(2.0, 7.3223, 11.8215);
            var expectedDistance = 1.8543;

            var result = RunOsteotomyDistanceScrewQcCheck(testPoint, 1.8, false, "05GEN", "01GEN", true);

            //assert
            Assert.IsTrue(result.IsOk);
            Assert.AreEqual(expectedDistance, result.Distance, 0.01);
        }

        //[TestMethod]
        //Commented this out as current algorithm is unable to filter
        public void OsteotomyDistanceScrewQcCheck_Should_Not_Return_Distance_From_Osteotomy_Edge_Behind_Surface_2()
        {
            var testPoint = new IDSPoint3D(2.0, 3.8211, 9.5500);
            var expectedDistance = 5.3536;
           
            var result = RunOsteotomyDistanceScrewQcCheck(testPoint, 5.0, false, "05GEN", "01GEN", true);

            //assert
            Assert.IsTrue(result.IsOk);
            Assert.AreEqual(expectedDistance, result.Distance, 0.01);
        }

        [TestMethod]
        public void OsteotomyDistanceScrewQcCheck_Should_Measure_From_Implant_Placable_Bone()
        {
            var testPoint = new IDSPoint3D(6.5, 8.4515, 9.4016);
            var expectedDistance = 3.2985;

            var result = RunOsteotomyDistanceScrewQcCheck(testPoint, 4.0, Transform.Translation(4.5, 0, 0), false, "05GEN", "01GEN", false);

            //assert
            //Original bone will be further away from guiding outline, this will give result.IsOk = true
            Assert.IsFalse(result.IsOk);
            Assert.AreEqual(expectedDistance, result.Distance, 0.01);
        }

        private OsteotomyDistanceContent RunOsteotomyDistanceScrewQcCheck(IDSPoint3D testPoint, double acceptableMinDistance, bool skipOsteotomy = false,
            string plannedPartName = "05GEN", string originalPartName = "01GEN", bool slantedOsteotomy = false)
        {
            return RunOsteotomyDistanceScrewQcCheck(testPoint, acceptableMinDistance, Transform.Identity, skipOsteotomy, plannedPartName, originalPartName, slantedOsteotomy);
        }

        private OsteotomyDistanceContent RunOsteotomyDistanceScrewQcCheck(IDSPoint3D testPoint, double acceptableMinDistance, Transform plannedPartTransform, bool skipOsteotomy = false, 
            string plannedPartName = "05GEN", string originalPartName = "01GEN", bool slantedOsteotomy = false)
        {
            var screw = ImplantScrewTestUtilities.CreateScrew(testPoint, plannedPartTransform, skipOsteotomy, plannedPartName, originalPartName, slantedOsteotomy);

            //act
            var screwAtOriginalPosOptimizer = new ScrewAtOriginalPosOptimizer(new PreImplantScrewQcInput(screw.Director));
            var distanceChecker = new OsteotomyDistanceChecker(screw.Director, screwAtOriginalPosOptimizer);
            var result = distanceChecker.PerformOsteotomyDistanceCheck(screw, acceptableMinDistance);
            return result;
        }
    }

#endif
}