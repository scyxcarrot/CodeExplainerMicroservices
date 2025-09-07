using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class RhinoTransformConverterTests
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
        #endregion

        [TestMethod]
        public void Convert_To_Rhino_Transform_With_ITransform_Test()
        {
            var idsTransform = new IDSTransform()
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

            var rhinoTransform = RhinoTransformConverter.ToRhinoTransformationMatrix(idsTransform);

            #region Assertion
            Assert.AreEqual(rhinoTransform.M00, idsTransform.M00, "Converted value M00 is wrong!");
            Assert.AreEqual(rhinoTransform.M01, idsTransform.M01, "Converted value M01 is wrong!");
            Assert.AreEqual(rhinoTransform.M02, idsTransform.M02, "Converted value M02 is wrong!");
            Assert.AreEqual(rhinoTransform.M03, idsTransform.M03, "Converted value M03 is wrong!");
            Assert.AreEqual(rhinoTransform.M10, idsTransform.M10, "Converted value M10 is wrong!");
            Assert.AreEqual(rhinoTransform.M11, idsTransform.M11, "Converted value M11 is wrong!");
            Assert.AreEqual(rhinoTransform.M12, idsTransform.M12, "Converted value M12 is wrong!");
            Assert.AreEqual(rhinoTransform.M13, idsTransform.M13, "Converted value M13 is wrong!");
            Assert.AreEqual(rhinoTransform.M20, idsTransform.M20, "Converted value M20 is wrong!");
            Assert.AreEqual(rhinoTransform.M21, idsTransform.M21, "Converted value M21 is wrong!");
            Assert.AreEqual(rhinoTransform.M22, idsTransform.M22, "Converted value M22 is wrong!");
            Assert.AreEqual(rhinoTransform.M23, idsTransform.M23, "Converted value M23 is wrong!");
            Assert.AreEqual(rhinoTransform.M30, idsTransform.M30, "Converted value M30 is wrong!");
            Assert.AreEqual(rhinoTransform.M31, idsTransform.M31, "Converted value M31 is wrong!");
            Assert.AreEqual(rhinoTransform.M32, idsTransform.M32, "Converted value M32 is wrong!");
            Assert.AreEqual(rhinoTransform.M33, idsTransform.M33, "Converted value M33 is wrong!");
            #endregion
        }

        [TestMethod]
        public void Convert_To_IDSTransform_With_Rhino_Transform()
        {
            var rhinoTransform = new Transform()
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

            var idsTransform = RhinoTransformConverter.ToIDSTransformationMatrix(rhinoTransform);

            #region Assertion
            Assert.AreEqual(idsTransform.M00, rhinoTransform.M00, "Converted value M00 is wrong!");
            Assert.AreEqual(idsTransform.M01, rhinoTransform.M01, "Converted value M01 is wrong!");
            Assert.AreEqual(idsTransform.M02, rhinoTransform.M02, "Converted value M02 is wrong!");
            Assert.AreEqual(idsTransform.M03, rhinoTransform.M03, "Converted value M03 is wrong!");
            Assert.AreEqual(idsTransform.M10, rhinoTransform.M10, "Converted value M10 is wrong!");
            Assert.AreEqual(idsTransform.M11, rhinoTransform.M11, "Converted value M11 is wrong!");
            Assert.AreEqual(idsTransform.M12, rhinoTransform.M12, "Converted value M12 is wrong!");
            Assert.AreEqual(idsTransform.M13, rhinoTransform.M13, "Converted value M13 is wrong!");
            Assert.AreEqual(idsTransform.M20, rhinoTransform.M20, "Converted value M20 is wrong!");
            Assert.AreEqual(idsTransform.M21, rhinoTransform.M21, "Converted value M21 is wrong!");
            Assert.AreEqual(idsTransform.M22, rhinoTransform.M22, "Converted value M22 is wrong!");
            Assert.AreEqual(idsTransform.M23, rhinoTransform.M23, "Converted value M23 is wrong!");
            Assert.AreEqual(idsTransform.M30, rhinoTransform.M30, "Converted value M30 is wrong!");
            Assert.AreEqual(idsTransform.M31, rhinoTransform.M31, "Converted value M31 is wrong!");
            Assert.AreEqual(idsTransform.M32, rhinoTransform.M32, "Converted value M32 is wrong!");
            Assert.AreEqual(idsTransform.M33, rhinoTransform.M33, "Converted value M33 is wrong!");
            #endregion
        }
    }
}
