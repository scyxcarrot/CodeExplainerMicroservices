using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class OsteotomyIntersectionScrewQcTests
    {
        //Point will generate screw at position where it has no intersection with osteotomy
        private IDSPoint3D _screwLocationWithoutIntersectionWithOsteotomy = new IDSPoint3D(2.0, 7.3223, 11.8215);

        //Point will generate screw at position where it has intersection with osteotomy when the planned bone it is placed on has Identity transformation
        private IDSPoint3D _screwLocationWithIntersectionWithOsteotomy = new IDSPoint3D(2.0, 10.7629, 5.8421);

        [TestMethod]
        public void OsteotomyIntersectionScrewQcCheck_Should_Return_IsIntersected_False_When_No_Intersection()
        {
            var testPoint = _screwLocationWithoutIntersectionWithOsteotomy;

            var result = RunOsteotomyIntersectionScrewQcCheck(testPoint, Transform.Identity);

            Assert.IsFalse(result.IsIntersected, "Result of OsteotomyIntersectionChecker for No Intersection is incorrect!");
        }

        [TestMethod]
        public void OsteotomyIntersectionScrewQcCheck_Should_Return_IsIntersected_True_When_Has_Intersection()
        {
            var testPoint = _screwLocationWithIntersectionWithOsteotomy;

            var result = RunOsteotomyIntersectionScrewQcCheck(testPoint, Transform.Identity);

            Assert.IsTrue(result.IsIntersected, "Result of OsteotomyIntersectionChecker for Has Intersection is incorrect!");
        }

        [TestMethod]
        public void OsteotomyIntersectionScrewQcCheck_Should_Return_IsIntersected_False_When_No_Intersection_With_Transformation()
        {
            var testPoint = _screwLocationWithIntersectionWithOsteotomy;

            var result = RunOsteotomyIntersectionScrewQcCheck(testPoint, Transform.Translation(0, 4.5, 0));

            Assert.IsFalse(result.IsIntersected, "Result of OsteotomyIntersectionChecker for No Intersection with transformation is incorrect!");
        }

        [TestMethod]
        public void OsteotomyIntersectionScrewQcCheck_Should_Return_IsIntersected_True_When_Has_Intersection_With_Transformation()
        {
            var transform = Transform.Translation(0, 4.5, 0);
            var testPoint = _screwLocationWithIntersectionWithOsteotomy;
            var intermediatePoint = RhinoPoint3dConverter.ToPoint3d(testPoint);
            intermediatePoint.Transform(transform);
            var translatedPoint = new IDSPoint3D(intermediatePoint.X, intermediatePoint.Y, intermediatePoint.Z);

            var result = RunOsteotomyIntersectionScrewQcCheck(translatedPoint, transform);

            Assert.IsTrue(result.IsIntersected, "Result of OsteotomyIntersectionChecker for Has Intersection with transformation is incorrect!");
        }

        [TestMethod]
        public void OsteotomyIntersectionScrewQcCheck_Should_Return_HasOsteotomyPlane_False_When_Case_Has_No_Osteotomy()
        {
            var testPoint = _screwLocationWithIntersectionWithOsteotomy;
            var screw = ImplantScrewTestUtilities.CreateScrew(testPoint, Transform.Identity, true);

            var screwAtOriginalPosOptimizer = new ScrewAtOriginalPosOptimizer(new PreImplantScrewQcInput(screw.Director));
            var checker = new OsteotomyIntersectionProxyChecker(screwAtOriginalPosOptimizer);
            var result = checker.Check(screw);
            var content = result.GetSerializableScrewQcResult() as OsteotomyIntersectionContent;

            Assert.IsFalse(content.HasOsteotomyPlane);
            Assert.IsFalse(content.IsIntersected);
        }

        [TestMethod]
        public void OsteotomyIntersectionScrewQcCheck_Should_Return_IsFloatingScrew_True_For_Screw_On_Graft()
        {
            var testPoint = _screwLocationWithIntersectionWithOsteotomy;

            var result = RunOsteotomyIntersectionScrewQcCheck(testPoint, "05FIB_part1");

            Assert.IsTrue(result.IsFloatingScrew);
            Assert.IsFalse(result.IsIntersected);
        }

        [TestMethod]
        public void OsteotomyIntersectionScrewQcCheck_Should_Return_IsFloatingScrew_True_For_Screw_Without_Planned_Bone()
        {
            var testPoint = _screwLocationWithIntersectionWithOsteotomy;

            var result = RunOsteotomyIntersectionScrewQcCheck(testPoint, "05MAN_teeth");

            Assert.IsTrue(result.IsFloatingScrew);
            Assert.IsFalse(result.IsIntersected);
        }

        [TestMethod]
        public void OsteotomyIntersectionScrewQcCheck_Should_Return_IsFloatingScrew_True_For_Screw_Without_Original_Bone()
        {
            var testPoint = _screwLocationWithIntersectionWithOsteotomy;

            var result = RunOsteotomyIntersectionScrewQcCheck(testPoint, "05GEN", "01GEN1");

            Assert.IsTrue(result.IsFloatingScrew);
            Assert.IsFalse(result.IsIntersected);
        }

        private OsteotomyIntersectionContent RunOsteotomyIntersectionScrewQcCheck(IDSPoint3D testPoint, string plannedPartName = "05GEN", string originalPartName = "01GEN")
        {
            return RunOsteotomyIntersectionScrewQcCheck(testPoint, Transform.Identity, plannedPartName, originalPartName);
        }

        private OsteotomyIntersectionContent RunOsteotomyIntersectionScrewQcCheck(IDSPoint3D testPoint, Transform plannedPartTransform, string plannedPartName = "05GEN", string originalPartName = "01GEN")
        {
            //arrange
            var screw = ImplantScrewTestUtilities.CreateScrew(testPoint, plannedPartTransform, false, plannedPartName, originalPartName);

            //act
            var screwAtOriginalPosOptimizer = new ScrewAtOriginalPosOptimizer(new PreImplantScrewQcInput(screw.Director));
            var checker = new OsteotomyIntersectionProxyChecker(screwAtOriginalPosOptimizer);
            var result = checker.Check(screw);
            var content = result.GetSerializableScrewQcResult() as OsteotomyIntersectionContent;
            return content;
        }
    }
}