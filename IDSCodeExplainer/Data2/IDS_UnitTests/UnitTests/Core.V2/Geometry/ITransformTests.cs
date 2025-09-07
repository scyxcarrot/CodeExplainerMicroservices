using IDS.Core.V2.Geometries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class ITransformTests
    {
        #region MatricesVariable
        private const double M00 = 123.45;
        private const double M01 = 234.56;
        private const double M02 = 345.67;
        private const double M03 = 456.78;
        private const double M10 = 567.89;
        private const double M11 = 678.90;
        private const double M12 = 789.01;
        private const double M13 = 890.12;
        private const double M20 = 901.23;
        private const double M21 = 321.54;
        private const double M22 = 432.65;
        private const double M23 = 543.76;
        private const double M30 = 654.87;
        private const double M31 = 765.98;
        private const double M32 = 876.09;
        private const double M33 = 987.10;

        private const double UnsetValue = -1.23432101234321E+308;

        private IDSTransform IdsTransform = new IDSTransform()
        {
            M00 = M00,
            M01 = M01,
            M02 = M02,
            M03 = M03,
            M10 = M10,
            M11 = M11,
            M12 = M12,
            M13 = M13,
            M20 = M20,
            M21 = M21,
            M22 = M22,
            M23 = M23,
            M30 = M30,
            M31 = M31,
            M32 = M32,
            M33 = M33
        };

        #endregion

        [TestMethod]
        public void IDSTransform_Get_Value_By_Row_Column_Test()
        {
            var row = 2;
            var column = 2;

            Assert.AreEqual(IdsTransform[row, column], M22, "IDSTransform got the wrong value!");
        }

        [TestMethod]
        public void IDSTransform_Set_Value_By_Row_Column_Test()
        {
            var row = 2;
            var column = 2;
            var value = 666.66;

            var idsTransform = new IDSTransform(IdsTransform);
            idsTransform[row, column] = value;

            Assert.AreEqual(idsTransform[row, column], value, "IDSTransform set the wrong value!");
        }

        [TestMethod]
        public void IDSTransform_Should_Be_Unset_Test()
        {
            var unsetTransform = IDSTransform.Unset();

            #region Assertion
            Assert.AreEqual(unsetTransform.M00, UnsetValue, "Unset value M00 is wrong!");
            Assert.AreEqual(unsetTransform.M01, UnsetValue, "Unset value M01 is wrong!");
            Assert.AreEqual(unsetTransform.M02, UnsetValue, "Unset value M02 is wrong!");
            Assert.AreEqual(unsetTransform.M03, UnsetValue, "Unset value M03 is wrong!");
            Assert.AreEqual(unsetTransform.M10, UnsetValue, "Unset value M10 is wrong!");
            Assert.AreEqual(unsetTransform.M11, UnsetValue, "Unset value M11 is wrong!");
            Assert.AreEqual(unsetTransform.M12, UnsetValue, "Unset value M12 is wrong!");
            Assert.AreEqual(unsetTransform.M13, UnsetValue, "Unset value M13 is wrong!");
            Assert.AreEqual(unsetTransform.M20, UnsetValue, "Unset value M20 is wrong!");
            Assert.AreEqual(unsetTransform.M21, UnsetValue, "Unset value M21 is wrong!");
            Assert.AreEqual(unsetTransform.M22, UnsetValue, "Unset value M22 is wrong!");
            Assert.AreEqual(unsetTransform.M23, UnsetValue, "Unset value M23 is wrong!");
            Assert.AreEqual(unsetTransform.M30, UnsetValue, "Unset value M30 is wrong!");
            Assert.AreEqual(unsetTransform.M31, UnsetValue, "Unset value M31 is wrong!");
            Assert.AreEqual(unsetTransform.M32, UnsetValue, "Unset value M32 is wrong!");
            Assert.AreEqual(unsetTransform.M33, UnsetValue, "Unset value M33 is wrong!");
            #endregion
        }

        [TestMethod]
        public void IDSTransform_ITransform_Casting_Test()
        {
            var testTransform = new IDSTransform(IdsTransform);

            #region Assertion
            Assert.AreEqual(testTransform.M00, IdsTransform.M00, "Casted value M00 is wrong!");
            Assert.AreEqual(testTransform.M01, IdsTransform.M01, "Casted value M01 is wrong!");
            Assert.AreEqual(testTransform.M02, IdsTransform.M02, "Casted value M02 is wrong!");
            Assert.AreEqual(testTransform.M03, IdsTransform.M03, "Casted value M03 is wrong!");
            Assert.AreEqual(testTransform.M10, IdsTransform.M10, "Casted value M10 is wrong!");
            Assert.AreEqual(testTransform.M11, IdsTransform.M11, "Casted value M11 is wrong!");
            Assert.AreEqual(testTransform.M12, IdsTransform.M12, "Casted value M12 is wrong!");
            Assert.AreEqual(testTransform.M13, IdsTransform.M13, "Casted value M13 is wrong!");
            Assert.AreEqual(testTransform.M20, IdsTransform.M20, "Casted value M20 is wrong!");
            Assert.AreEqual(testTransform.M21, IdsTransform.M21, "Casted value M21 is wrong!");
            Assert.AreEqual(testTransform.M22, IdsTransform.M22, "Casted value M22 is wrong!");
            Assert.AreEqual(testTransform.M23, IdsTransform.M23, "Casted value M23 is wrong!");
            Assert.AreEqual(testTransform.M30, IdsTransform.M30, "Casted value M30 is wrong!");
            Assert.AreEqual(testTransform.M31, IdsTransform.M31, "Casted value M31 is wrong!");
            Assert.AreEqual(testTransform.M32, IdsTransform.M32, "Casted value M32 is wrong!");
            Assert.AreEqual(testTransform.M33, IdsTransform.M33, "Casted value M33 is wrong!");
            #endregion
        }
    }
}
