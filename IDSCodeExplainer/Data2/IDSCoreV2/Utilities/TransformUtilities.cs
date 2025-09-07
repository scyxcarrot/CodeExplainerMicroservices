using IDS.Interface.Geometry;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using IDS.Core.V2.Geometries;

namespace IDS.Core.V2.Utilities
{
    public static class TransformUtilities
    {
        private static Matrix<double> ToTransformationMatrix(ITransform transform)
        {
            return DenseMatrix.OfArray(new[,]
            {
                { transform.M00, transform.M01, transform.M02, transform.M03 },
                { transform.M10, transform.M11, transform.M12, transform.M13 },
                { transform.M20, transform.M21, transform.M22, transform.M23 },
                { transform.M30, transform.M31, transform.M32, transform.M33 }
            });
        }

        private static ITransform ToTransform(Matrix<double> transformMatrix)
        {
            if (transformMatrix.ColumnCount != 4 ||
                transformMatrix.RowCount != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(transformMatrix), transformMatrix,
                    "The transformation matrix is not 4x4");
            }

            return new IDSTransform()
            {
                M00 = transformMatrix[0, 0],
                M01 = transformMatrix[0, 1],
                M02 = transformMatrix[0, 2],
                M03 = transformMatrix[0, 3],

                M10 = transformMatrix[1, 0],
                M11 = transformMatrix[1, 1],
                M12 = transformMatrix[1, 2],
                M13 = transformMatrix[1, 3],

                M20 = transformMatrix[2, 0],
                M21 = transformMatrix[2, 1],
                M22 = transformMatrix[2, 2],
                M23 = transformMatrix[2, 3],

                M30 = transformMatrix[3, 0],
                M31 = transformMatrix[3, 1],
                M32 = transformMatrix[3, 2],
                M33 = transformMatrix[3, 3],
            };
        }

        private static Matrix<double> ToPositionMatrix(double x, double y, double z, double scale = 1.0)
        {
            return DenseMatrix.OfArray(new[,]
            {
                { x },
                { y },
                { z },
                { scale }
            });
        }

        public static void TransformWithoutScaling(ITransform transform, double x, double y, double z, 
            out double xNew, out double yNew, out double zNew, out double scale)
        {
            var transformMatrix = ToTransformationMatrix(transform);
            var positionMatrix = ToPositionMatrix(x, y, z);
            var newPositionMatrix = transformMatrix.Multiply(positionMatrix);

            xNew = newPositionMatrix[0,0];
            yNew = newPositionMatrix[1, 0];
            zNew = newPositionMatrix[2, 0];
            scale = newPositionMatrix[3, 0];
        }

        public static void Transform(ITransform transform, double x, double y, double z,
            out double xNew, out double yNew, out double zNew)
        {
            TransformWithoutScaling(transform, x,y,z, out var xNewRaw, out var yNewRaw, out var zNewRaw, out var scale);
            xNew = xNewRaw/scale;
            yNew = yNewRaw/scale;
            zNew = zNewRaw/scale;
        }

        /// <summary>
        /// Transform in sequence from left to right order
        /// </summary>
        /// <param name="transformSequences"></param>
        /// <returns></returns>
        public static ITransform TransformInSequence(params ITransform[] transformSequences)
        {
            if (transformSequences.Length < 2)
            {
                throw new ArgumentException("transforms must be more than 2", nameof(transformSequences));
            }

            // Matrix multiplication from right to left, reverse the sequence
            var i = 0;
            var transformMatrixRight = ToTransformationMatrix(transformSequences[i++]);

            for (; i < transformSequences.Length; i++)
            {
                var transformMatrixLeft = ToTransformationMatrix(transformSequences[i]);
                transformMatrixRight = transformMatrixLeft.Multiply(transformMatrixRight);
            }

            return ToTransform(transformMatrixRight);
        }

        public static ITransform Transpose(ITransform transform)
        {
            var transformMatrix = ToTransformationMatrix(transform);
            return ToTransform(transformMatrix.Transpose());
        }

        public static ITransform Translate(double x, double y, double z)
        {
            var transform = IDSTransform.Identity;
            transform.M03 = x;
            transform.M13 = y;
            transform.M23 = z;
            return transform;
        }

        public static ITransform Scale(double xScale, double yScale, double zScale)
        {
            var transform = IDSTransform.Identity;
            transform.M00 = xScale;
            transform.M11 = yScale;
            transform.M22 = zScale;
            return transform;
        }

        public static ITransform Scale(double scale)
        {
            return Scale(scale, scale, scale);
        }

        public static ITransform Inverse(ITransform transform)
        {
            var transformMatrix = ToTransformationMatrix(transform);
            return ToTransform(transformMatrix.Inverse());
        }

        // TODO: Rotation
    }
}
