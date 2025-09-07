using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class TransformUtilitiesTests
    {
        [TestMethod]
        public void Basic_Transform_Test()
        {
            const double x = 0.0;
            const double y = 0.0;
            const double z = 0.0;

            var transform = IDSTransform.Identity;
            transform.M03 = 1;
            transform.M13 = 2;
            transform.M23 = 3;

            TransformUtilities.Transform(transform, x, y, z,
                out var xNew, out var yNew, out var zNew);

            Assert.AreEqual(1, xNew, 0.0001);
            Assert.AreEqual(2, yNew, 0.0001);
            Assert.AreEqual(3, zNew, 0.0001);
        }

        [TestMethod]
        public void Basic_Transform_With_Scaling_Test()
        {
            const double x = 0.0;
            const double y = 0.0;
            const double z = 0.0;

            var transform = IDSTransform.Identity;
            transform.M03 = 2;
            transform.M13 = 4;
            transform.M23 = 6;
            transform.M33 = 2;

            TransformUtilities.Transform(transform, x, y, z,
                out var xNew, out var yNew, out var zNew);

            Assert.AreEqual(1, xNew, 0.0001);
            Assert.AreEqual(2, yNew, 0.0001);
            Assert.AreEqual(3, zNew, 0.0001);
        }

        [TestMethod]
        public void Basic_Transform_Without_Scaling_Test()
        {
            const double x = 0.0;
            const double y = 0.0;
            const double z = 0.0;

            var transform = IDSTransform.Identity;
            transform.M03 = 1;
            transform.M13 = 2;
            transform.M23 = 3;
            transform.M33 = 2;

            TransformUtilities.TransformWithoutScaling(transform, x, y, z,
                out var xNew, out var yNew, out var zNew, out var scale);

            Assert.AreEqual(1, xNew, 0.0001);
            Assert.AreEqual(2, yNew, 0.0001);
            Assert.AreEqual(3, zNew, 0.0001);
            Assert.AreEqual(2, scale, 0.0001);
        }

        [TestMethod]
        public void Transform_Transpose_Test()
        {
            /*
             *  1  2  3  4 
             *  5  6  7  8
             *  9 10 11 12 
             * 13 14 15 16
             */
            var transform = new IDSTransform()
            {
                M00 = 1,
                M01 = 2,
                M02 = 3,
                M03 = 4,

                M10 = 5,
                M11 = 6,
                M12 = 7,
                M13 = 8,

                M20 = 9,
                M21 = 10,
                M22 = 11,
                M23 = 12,

                M30 = 13,
                M31 = 14,
                M32 = 15,
                M33 = 16,
            };

            var actualTransform = TransformUtilities.Transpose(transform);

            /*
             *  1  5  9 13
             *  2  6 10 14
             *  3  7 11 15
             *  4  8 12 16
             */
            var expectedTransform = new IDSTransform()
            {
                M00 = 1,
                M01 = 5,
                M02 = 9,
                M03 = 13,

                M10 = 2,
                M11 = 6,
                M12 = 10,
                M13 = 14,

                M20 = 3,
                M21 = 7,
                M22 = 11,
                M23 = 15,

                M30 = 4,
                M31 = 8,
                M32 = 12,
                M33 = 16,
            };

            AreEqual(expectedTransform, actualTransform);
        }

        [TestMethod]
        public void Transform_Translate_Test()
        {
            var xTranslate = 1;
            var yTranslate = 2;
            var zTranslate = 3;
            var actualTransform = TransformUtilities.Translate(xTranslate, yTranslate, zTranslate);

            /*
             *  1  0  0  x
             *  0  1  0  y
             *  0  0  1  z
             *  0  0  0  1
             */
            var expectedTransform = new IDSTransform()
            {
                M00 = 1,
                M01 = 0,
                M02 = 0,
                M03 = xTranslate,

                M10 = 0,
                M11 = 1,
                M12 = 0,
                M13 = yTranslate,

                M20 = 0,
                M21 = 0,
                M22 = 1,
                M23 = zTranslate,

                M30 = 0,
                M31 = 0,
                M32 = 0,
                M33 = 1,
            };

            AreEqual(expectedTransform, actualTransform);
        }

        [TestMethod]
        public void Transform_Scale_Test()
        {
            var xScale = 1;
            var yScale = 2;
            var zScale = 3;
            var actualTransform = TransformUtilities.Scale(xScale, yScale, zScale);

            /*
             *  x  0  0  0
             *  0  y  0  0
             *  0  0  z  0
             *  0  0  0  1
             */
            var expectedTransform = new IDSTransform()
            {
                M00 = xScale,
                M01 = 0,
                M02 = 0,
                M03 = 0,

                M10 = 0,
                M11 = yScale,
                M12 = 0,
                M13 = 0,

                M20 = 0,
                M21 = 0,
                M22 = zScale,
                M23 = 0,

                M30 = 0,
                M31 = 0,
                M32 = 0,
                M33 = 1,
            };

            AreEqual(expectedTransform, actualTransform);
        }

        [TestMethod]
        public void Transform_Uniform_Scale_Test()
        {
            var scale = 1;
            var actualTransform = TransformUtilities.Scale(scale);

            /*
             *  s  0  0  0
             *  0  s  0  0
             *  0  0  s  0
             *  0  0  0  1
             */
            var expectedTransform = new IDSTransform()
            {
                M00 = scale,
                M01 = 0,
                M02 = 0,
                M03 = 0,

                M10 = 0,
                M11 = scale,
                M12 = 0,
                M13 = 0,

                M20 = 0,
                M21 = 0,
                M22 = scale,
                M23 = 0,

                M30 = 0,
                M31 = 0,
                M32 = 0,
                M33 = 1,
            };

            AreEqual(expectedTransform, actualTransform);
        }

        [TestMethod]
        public void Transform_Transforms_Test()
        {
            /* Verified with Numpy
             * ----------------------python script----------------------
             * import numpy as np
             * import math
             * translate = np.array([[1,0,0,1], [0,1,0,2],[0,0,1,3],[0,0,0,1]])
             * scale = np.array([[2,0,0,0], [0,2,0,0],[0,0,2,0],[0,0,0,1]])
             * sin45 = cos45 = math.sqrt(2)/2
             * rotate = np.array([[1,0,0,0], [0,cos45,sin45,0],[0,-sin45,cos45,0],[0,0,0,1]])
             * print(np.matmul(rotate, np.matmul(scale, translate)))
             * ---------------------------------------------------------
             * | 1  0  0  1| |2  0  0  0| |1    0     0   0|   |2.  0.         0.         2.        |
             * | 0  1  0  2|x|0  2  0  0|x|0  0.707 0.707 0| = |0.  1.41421356 1.41421356 7.07106781|
             * | 0  0  1  3| |0  0  2  0| |0 -0.707 0.707 0|   |0. -1.41421356 1.41421356 1.41421356|
             * | 0  0  0  1| |0  0  0  1| |0    0     0   1|   |0.  0.         0.         1.        |
             */
            var translate = TransformUtilities.Translate(1, 2, 3);
            var scale = TransformUtilities.Scale(2);

            var sin45 = Math.Sqrt(2)/2; // sin(45) = cos(45) = sqrt(2)/2
            var cos45 = sin45;
            var rotation = new IDSTransform()
            {
                M00 = 1,
                M01 = 0,
                M02 = 0,
                M03 = 0,

                M10 = 0,
                M11 = cos45,
                M12 = sin45,
                M13 = 0,

                M20 = 0,
                M21 = -sin45,
                M22 = cos45,
                M23 = 0,

                M30 = 0,
                M31 = 0,
                M32 = 0,
                M33 = 1,
            };

            var actualTransform = TransformUtilities.TransformInSequence(translate, scale, rotation);

            var expectedTransform = new IDSTransform()
            {
                M00 = 2,
                M01 = 0,
                M02 = 0,
                M03 = 2,

                M10 = 0,
                M11 = 1.41421356,
                M12 = 1.41421356,
                M13 = 7.07106781,

                M20 = 0,
                M21 = -1.41421356,
                M22 = 1.41421356,
                M23 = 1.41421356,

                M30 = 0,
                M31 = 0,
                M32 = 0,
                M33 = 1,
            };

            AreEqual(expectedTransform, actualTransform);
        }


        private void AreEqual(ITransform expected, ITransform actual)
        {
            Assert.AreEqual(expected.M00, actual.M00, 0.0001, "M00");
            Assert.AreEqual(expected.M01, actual.M01, 0.0001, "M01");
            Assert.AreEqual(expected.M02, actual.M02, 0.0001, "M02");
            Assert.AreEqual(expected.M03, actual.M03, 0.0001, "M03");

            Assert.AreEqual(expected.M10, actual.M10, 0.0001, "M10");
            Assert.AreEqual(expected.M11, actual.M11, 0.0001, "M11");
            Assert.AreEqual(expected.M12, actual.M12, 0.0001, "M12");
            Assert.AreEqual(expected.M13, actual.M13, 0.0001, "M13");

            Assert.AreEqual(expected.M20, actual.M20, 0.0001, "M20");
            Assert.AreEqual(expected.M21, actual.M21, 0.0001, "M21");
            Assert.AreEqual(expected.M22, actual.M22, 0.0001, "M22");
            Assert.AreEqual(expected.M23, actual.M23, 0.0001, "M23");

            Assert.AreEqual(expected.M30, actual.M30, 0.0001, "M30");
            Assert.AreEqual(expected.M31, actual.M31, 0.0001, "M31");
            Assert.AreEqual(expected.M32, actual.M32, 0.0001, "M32");
            Assert.AreEqual(expected.M33, actual.M33, 0.0001, "M33");
        }
    }
}
