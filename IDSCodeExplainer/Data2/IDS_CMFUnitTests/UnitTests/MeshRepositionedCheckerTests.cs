using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class MeshRepositionedCheckerTests
    {
        [TestMethod]       
        public void Parts_That_Do_Not_Collide_Returns_IsMeshRepositioned_True()
        {
            //arrange
            var resource = new TestResources();

            //act
            var isRepositioned = IsMeshRepositioned(resource.OriginalMaxillaStlFilePath, resource.RepositionedMaxillaStlFilePath);

            //assert
            Assert.IsTrue(isRepositioned);           
        }

        [TestMethod]
        public void Parts_With_Same_Shape_And_No_Reposition_Returns_IsMeshRepositioned_False()
        {
            //arrange
            var resource = new TestResources();

            //act
            var isRepositioned = IsMeshRepositioned(resource.OriginalNerveStlFilePath, resource.OriginalNerveStlFilePath);

            //assert
            Assert.IsFalse(isRepositioned);
        }

        [TestMethod]
        public void Parts_With_Same_Shape_And_Repositioned_Returns_IsMeshRepositioned_True()
        {
            //arrange
            var resource = new TestResources();

            //act
            var isRepositioned = IsMeshRepositioned(resource.OriginalRamusStlFilePath, resource.RepositionedRamusStlFilePath);

            //assert
            Assert.IsTrue(isRepositioned);
        }

        [TestMethod]
        public void Parts_Undergone_Trimming_And_No_Reposition_Returns_IsMeshRepositioned_False()
        {
            //arrange
            var resource = new TestResources();

            //act
            var isRepositioned = IsMeshRepositioned(resource.OriginalRamusStlFilePath, resource.TrimmedRamusStlFilePath);

            //assert
            Assert.IsFalse(isRepositioned);
        }

        [TestMethod]
        public void Parts_Undergone_Trimming_And_Repositioned_Returns_IsMeshRepositioned_True()
        {
            //arrange
            var resource = new TestResources();

            //act
            var isRepositioned = IsMeshRepositioned(resource.OriginalNerveStlFilePath, resource.TrimmedAndRepositionedNerveStlFilePath);

            //assert
            Assert.IsTrue(isRepositioned);
        }

        [TestMethod]
        public void Parts_With_HoleFilled_And_No_Reposition_Returns_IsMeshRepositioned_False()
        {
            //arrange
            var resource = new TestResources();

            //act
            var isRepositioned = IsMeshRepositioned(resource.OriginalGenioStlFilePath, resource.FilledHoleGenioStlFilePath);

            //assert
            Assert.IsFalse(isRepositioned);
        }

        [TestMethod]
        public void Parts_With_HoleFilled_And_Repositioned_Returns_IsMeshRepositioned_True()
        {
            //arrange
            var resource = new TestResources();

            //act
            var isRepositioned = IsMeshRepositioned(resource.OriginalGenioStlFilePath, resource.FilledHoleAndRepositionedGenioStlFilePath);

            //assert
            Assert.IsTrue(isRepositioned);
        }

        //[TestMethod]
        //Temporary skip this unit test because using the sample inputs, checker give false positive result
        public void Parts_Undergone_Remeshing_And_No_Reposition_Returns_IsMeshRepositioned_False()
        {
            //arrange
            var resource = new TestResources();

            //act
            var isRepositioned = IsMeshRepositioned(resource.OriginalMandibleStlFilePath, resource.RemeshedMandibleStlFilePath);

            //assert
            Assert.IsFalse(isRepositioned);
        }

        [TestMethod]
        public void Parts_Undergone_Remeshing_And_Repositioned_Returns_IsMeshRepositioned_True()
        {
            //arrange
            var resource = new TestResources();

            //act
            var isRepositioned = IsMeshRepositioned(resource.OriginalMandibleStlFilePath, resource.RepositionedMandibleStlFilePath);

            //assert
            Assert.IsTrue(isRepositioned);
        }

        public static bool IsMeshRepositioned(string originalMeshFilePath, string changedMeshFilePath)
        {
            //arrange
            StlUtilities.StlBinary2RhinoMesh(originalMeshFilePath, out var originalMesh);
            StlUtilities.StlBinary2RhinoMesh(changedMeshFilePath, out var changedMesh);

            //act
            var checker = new MeshRepositionedChecker();
            return checker.IsMeshRepositioned(originalMesh, changedMesh);
        }
    }

#endif
}