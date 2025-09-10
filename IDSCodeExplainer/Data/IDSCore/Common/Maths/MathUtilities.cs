using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Plane = Rhino.Geometry.Plane;

namespace IDS.Core.Utilities
{
    public static class MathUtilities
    {
        public static double ConvertBytesToMegabytes(ulong bytes)
        {
            return (bytes / 1024f) / 1024f;
        }

        public static T[] ExtractValue<T>(T[][] inArray, int nthElement)
        {
            var res = new T[inArray.Length];

            for (var i = 0; i < inArray.Length; ++i)
            {
                res[i] = inArray[i][nthElement];
            }

            return res;
        }

        public static T[][] ReshapeMatrice<T>(T[] inArray, int row, int col)
        {
            var r = row;
            if (row == -1)
            {
                r = inArray.Length;
            }

            var c = col;
            if (col == -1)
            {
                c = inArray.Length;
            }

            return Accord.Math.Jagged.Reshape(inArray, r, c);
        }

        public static double[][] MatriceSubtract(double[][] first, double[][] second)
        {
            return DoMatriceModification(first, (r, c, d) => d - second[r][c]);
        }

        public static double[][] MatriceAdd(double[][] first, double[][] second)
        {
            return DoMatriceModification(first, (r, c, d) => second[r][c] + d);
        }

        public static double[][] MatricePowerOf(double[][] inArray, int power)
        {
            return DoMatriceModification(inArray, (r, c, d) => Math.Pow(d, power));
        }

        public static double[][] MatriceSqrt(double[][] inArray)
        {
            return DoMatriceModification(inArray, (r, c, d) => Math.Sqrt(d));
        }

        public static double[][] MatriceLog(double[][] inArray)
        {
            return DoMatriceModification(inArray, (r, c, d) => Math.Log(d));
        }

        public static double[][] MatriceMultiply(double[][] first, double[][] second)
        {
            return DoMatriceModification(first, (r, c, d) => second[r][c] * d);
        }


        public static double[][] DoMatriceModification(double[][] inArray, Func<int, int, double, double> func)
        {
            var nrow = inArray.Length;
            var ncol = inArray[0].Length;

            var res = new double[nrow][];

            for (var r = 0; r < nrow; ++r)
            {
                res[r] = new double[ncol];
                for (var c = 0; c < ncol; ++c)
                {
                    res[r][c] = func(r, c, inArray[r][c]);
                }
            }

            return res;
        }

        //Compute the (Moore-Penrose) pseudo-inverse of a matrix.
        public static double[][] PInvMatrice(double[][] inArray)
        {
            return Accord.Math.Matrix.PseudoInverse(inArray);
        }

        public static T[][] MatriceTile<T>(T[][] inArray, int colReps)
        {
            var nrow = inArray.Length;
            var res = new T[nrow][];

            for (var i = 0; i < nrow; ++i)
            {
                var newnSize = inArray[i].Length * colReps;
                var newRowVal = new T[newnSize];

                var n = 0;
                for (var j = 0; j < colReps; ++j)
                {
                    var ncol = inArray[i].Length;
                    for (var k = 0; k < ncol; ++k)
                    {
                        newRowVal[n] = inArray[i][k];
                        n++;
                    }
                }
                res[i] = newRowVal;
            }

            return res;
        }

        public static T[][] MatriceTile<T>(T[][] inArray, int rowReps, int colReps)
        {
            var nrow = inArray.Length * rowReps;
            var res = new T[nrow][];

            var count = 0;

            for (var i = 0; i < rowReps; ++i)
            {
                var resInner = MatriceTile(inArray, colReps);

                foreach (var r in resInner)
                {
                    res[count] = r;
                    count++;
                }
            }

            return res;
        }

        public static T[][] ToJaggedMatrice<T>(T[,] inMatrice)
        {
            var rowsFirstIndex = inMatrice.GetLowerBound(0);
            var rowsLastIndex = inMatrice.GetUpperBound(0);
            var numberOfRows = rowsLastIndex + 1;

            var columnsFirstIndex = inMatrice.GetLowerBound(1);
            var columnsLastIndex = inMatrice.GetUpperBound(1);
            var numberOfColumns = columnsLastIndex + 1;

            var jaggedArray = new T[numberOfRows][];
            for (var i = rowsFirstIndex; i <= rowsLastIndex; i++)
            {
                jaggedArray[i] = new T[numberOfColumns];

                for (var j = columnsFirstIndex; j <= columnsLastIndex; j++)
                {
                    jaggedArray[i][j] = inMatrice[i, j];
                }
            }
            return jaggedArray;
        }

        public static T[,] ToJaggedMatrice<T>(T[][] inArray)
        {
            var row = inArray.Length;
            var col = inArray[0].Length;

            var res = new T[row, col];

            for (var r = 0; r < row; ++r)
            {
                for (var c = 0; c < col; ++c)
                {
                    res[r, c] = inArray[r][c];
                }
            }

            return res;
        }

        public static T[,] ToUniformMatrice<T>(T[][] inArray)
        {
            var row = inArray.Length;
            var col = inArray[0].Length;

            var res = new T[row, col];

            for (var r = 0; r < row; ++r)
            {
                for (var c = 0; c < col; ++c)
                {
                    if (inArray[r].Length != col)
                    {
                        throw new IDSException($"Size is not uniform at arraw row :{r}, size is:{inArray[r].Length} when expected size is {col}");
                    }
                    res[r, c] = inArray[r][c];
                }
            }

            return res;
        }

        public static bool TwoDArrayIsEqual<T>(T[,] first, T[,] second)
        {
            return first.Rank == second.Rank &&
                    Enumerable.Range(0, first.Rank).All(dimension => first.GetLength(dimension) == second.GetLength(dimension)) &&
                    first.Cast<double>().SequenceEqual(second.Cast<double>());
        }

        public static T[][] ArrayTranspose<T>(T[][] inArray)
        {

            var size = inArray[0].Length;
            var res = new T[size][];

            for (var i = 0; i < size; ++i)
            {
                res[i] = inArray.Select(arr => arr[i]).ToArray();
            }

            return res;
        }

        public static T[][] ArrayVStack<T>(List<T[][]> inArrays)
        {
            var size = 0;
            var inArrayList = inArrays.ToList();
            inArrayList.ForEach(x => size += x.Length);

            var res = new T[size][];

            var count = 0;

            inArrayList.ForEach(x => x.ToList().ForEach(y =>
            {
                res[count] = y;
                ++count;
            }));

            return res;
        }


        public static double[][] SetValueInMatriceUsingMask(double[][] matrice, bool[][] mask, double value)
        {
            var res = matrice;

            for (var i = 0; i < res.Length; ++i) // Make sure all Zeroes are 1 based on the Mask
            {
                var eli = res[i];
                for (var j = 0; j < eli.Length; ++j)
                {
                    if (mask[i][j])
                    {
                        eli[j] = 1;
                    }
                }
                res[i] = eli;
            }

            return res;
        }

        public static double DotProduct(double[] first, double[] second)
        {
            if (first.Length != second.Length)
            {
                throw new IDSException("Size must be the same!");
            }

            return first.Select((t, i) => t * second[i]).Sum();
        }


        public static double[][] MatriceDotProduct(double[][] arr1, double[][] arr2)
        {
            var res = new double[arr1.Length][];

            for (var row = 0; row < arr1.Length; ++row)
            {
                var currArr1Row = arr1[row];

                var currArr1RowColumnRes = new double[arr2[0].Length];
                for (var column = 0; column < arr2[0].Length; ++column)
                {
                    var currArr2Column = arr2.Select(x => x[column]).ToArray();
                    currArr1RowColumnRes[column] = DotProduct(currArr1Row, currArr2Column);
                }

                res[row] = currArr1RowColumnRes;
            }

            return res;
        }

        public static double[][] ConvertPoint3DArrayToDoubleArray(Point3d[] points)
        {
            var res = new double[points.Length][];
            for (var i = 0; i < points.Length; ++i)
            {
                res[i] = new[] { points[i].X, points[i].Y, points[i].Z };
            }

            return res;
        }

        public static Point3d[] ConvertDoubleArrayToPoint3D(double[][] inArray)
        {
            var res = new Point3d[inArray.Length];
            for (var i = 0; i < inArray.Length; ++i)
            {
                res[i] = new Point3d(inArray[i][0], inArray[i][1], inArray[i][2]);
            }

            return res;
        }

        public static double[][] CreateOnesMatrix(int row, int column)
        {
            return CreateValuedMatrix(row, column, 1);
        }

        public static double[][] CreateZerosMatrix(int row, int column)
        {
            return CreateValuedMatrix(row, column, 0);
        }

        public static double[][] CreateValuedMatrix(int row, int column, int value)
        {
            var res = new double[row][];
            for (var i = 0; i < row; ++i)
            {
                res[i] = new double[column];

                for (var j = 0; j < column; ++j)
                {
                    res[i][j] = value;
                }
            }

            return res;
        }

        public static T[][] ArrayHStack<T>(List<T[][]> inArrays)
        {
            var size = inArrays[0].Length;

            if (inArrays.Count == 1)
            {
                return inArrays[0];
            }

            inArrays.ForEach(x =>
            {
                if (x.Length != size)
                {
                    throw new IDSException("Array size must be the same!");
                }
            });

            var expectedElementSize = inArrays.Aggregate(0, (current, arr) => current + arr[0].Length);

            for (var i = 1; i < size; ++i)
            {
                if (inArrays.Aggregate(0, (current, arr) => current + arr[i].Length) != expectedElementSize)
                {
                    throw new IDSException("Array inner element size must be the same!");
                }
            }

            var res = new T[size][];

            for (var i = 0; i < size; ++i)
            {
                var val = new List<T>();
                foreach (var arr in inArrays)
                {
                    val.AddRange(arr[i]);
                }
                res[i] = val.ToArray();
            }

            return res;
        }

        /// <summary>
        /// Generate range of values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="step">The step.</param>
        /// <returns></returns>
        public static IEnumerable<double> Range(double min, double max, double step)
        {
            for (double i = min; i <= max; i = i + step)
            {
                yield return i;
            }
        }

        /// <summary>
        /// Generate range of values
        /// </summary>
        /// <typeparam name="Double">The type of the ouble.</typeparam>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="step">The step.</param>
        /// <returns></returns>
        public static IEnumerable<int> Range(int min, int max, int step)
        {
            for (int i = min; i <= max; i = i + step)
                yield return i;
        }

        /// <summary>
        /// Caps the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="minValue">The minimum value.</param>
        /// <param name="maxValue">The maximum value.</param>
        /// <returns></returns>
        public static double CapValue(double value, double minValue, double maxValue)
        {
            double cappedValue = value;

            if (value > maxValue)
                cappedValue = maxValue;
            else if (value < minValue)
                cappedValue = minValue;

            return cappedValue;
        }

        /// <summary>
        /// Calculates the arc angle.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="arcLength">Length of the arc.</param>
        /// <returns></returns>
        public static double CalculateArcAngle(double radius, double arcLength)
        {
            return arcLength * 180 / (Math.PI * radius);
        }

        /// <summary>
        /// Calculates the arc length.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="arcAngle">Length of the arc.</param>
        /// <returns></returns>
        public static double CalculateArcLength(double radius, double arcAngle)
        {
            return arcAngle * (Math.PI * radius) / 180;
        }

        /// <summary>
        /// Calculate the length of the diagonal side of a right-angled triangle where the length of one straight side and the angle between the 
        /// straight and diagonal sides are known
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="straightSideLength"></param>
        /// <returns></returns>
        public static double DiagonalEdgeLength(double angle, double straightSideLength)
        {
            return straightSideLength / Math.Sin((Math.PI / 180) * angle);
        }

        /// <summary>
        /// Calculate the angle between the diagonal side and one straight side of a straight angled triangle
        /// </summary>
        /// <param name="diagonalSideLength"></param>
        /// <param name="straightSideLength"></param>
        /// <returns></returns>
        public static double SharpCornerAngle(double diagonalSideLength, double straightSideLength)
        {
            return Math.Acos(straightSideLength / diagonalSideLength) / Math.PI * 180;
        }

        /// <summary>
        /// Anteversion and inclination to vector.
        /// </summary>
        /// <param name="anteversionDegrees">The anteversion degrees.</param>
        /// <param name="inclinationDegrees">The inclination degrees.</param>
        /// <param name="coordinateSystem">The coordinate system.</param>
        /// <param name="defectIsLeft">if set to <c>true</c> [defect is left].</param>
        /// <returns></returns>
        public static Vector3d AnteversionInclinationToVector(double anteversionDegrees, double inclinationDegrees, Plane coordinateSystem, bool defectIsLeft)
        {
            // Compute angles
            double inclinationRadians = RhinoMath.ToRadians(inclinationDegrees);
            double anteversionRadians = RhinoMath.ToRadians(anteversionDegrees);

            // Compute vectors and angles
            Vector3d YAxis = defectIsLeft ? coordinateSystem.YAxis : -coordinateSystem.YAxis;
            Vector3d XAxis = coordinateSystem.XAxis;
            Vector3d ZAxis = coordinateSystem.ZAxis;
            XAxis.Unitize();
            YAxis.Unitize();
            ZAxis.Unitize();

            const double epsilon = 0.00001;

            double x, y, z;
            if (Math.Abs(inclinationDegrees % 360) < epsilon)
            {
                z = -1;
                x = 0;
                y = 0;
            }
            else if (Math.Abs(inclinationDegrees % 180) < epsilon)
            {
                z = 1;
                x = 0;
                y = 0;
            }
            else if (Math.Abs(inclinationDegrees % 90) < epsilon)
            {
                z = 0;
                y = Math.Cos(anteversionRadians);
                x = Math.Sin(anteversionRadians);
            }
            else
            {
                y = Math.Cos(anteversionRadians);
                x = Math.Sin(anteversionRadians);
                z = -1.0 * Math.Cos(anteversionRadians) / Math.Tan(inclinationRadians);
            }

            Vector3d orientation = (x * XAxis) + (y * YAxis) + (z * ZAxis);
            orientation.Unitize();
            return orientation;
        }

        public static Vector3d GleniusAnteversionInclinationToVector(double anteversionDegrees, double inclinationDegrees, Plane coordinateSystem, bool defectIsLeft)
        {
            var epsilon = 0.00001;

            // Compute angles
            double inclinationRadians = RhinoMath.ToRadians(inclinationDegrees);
            double anteversionRadians = RhinoMath.ToRadians(anteversionDegrees);

            // Compute vectors and angles
            Vector3d axialNormal = coordinateSystem.YAxis;
            Vector3d coronalNormal = -coordinateSystem.XAxis;
            Vector3d sagittalNormal = defectIsLeft ? coordinateSystem.ZAxis : -coordinateSystem.ZAxis;
            coronalNormal.Unitize();
            axialNormal.Unitize();
            sagittalNormal.Unitize();

            double x, y, z;
            if (Math.Abs(inclinationDegrees - 90) < epsilon)
            {
                y = 1;
                x = 0;
                z = 0;
            }
            else if (Math.Abs(inclinationDegrees + 90) < epsilon)
            {
                y = -1;
                x = 0;
                z = 0;
            }
            else if (Math.Abs(anteversionDegrees - 90) < epsilon)
            {
                y = 0;
                x = 1;
                z = 0;
            }
            else if (Math.Abs(anteversionDegrees + 90) < epsilon)
            {
                y = 0;
                x = -1;
                z = 0;
            }
            else
            {
                y = Math.Tan(inclinationRadians);
                x = Math.Tan(anteversionRadians);
                z = 1;
            }

            Vector3d orientation = (x * coronalNormal) + (y * axialNormal) + (z * sagittalNormal);
            orientation.Unitize();
            return orientation;
        }

        /// <summary>
        /// Gets the offset.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="reference">The reference.</param>
        /// <param name="measured">The measured.</param>
        /// <returns></returns>
        public static double GetOffset(Vector3d axis, Point3d reference, Point3d measured)
        {
            axis.Unitize();
            return ((Vector3d)measured * axis - (Vector3d)reference * axis);
        }

        /// <summary>
        /// Calculates the dot product.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns></returns>
        public static double CalculateDotProduct(Vector2d a, Vector2d b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        /// <summary>
        /// Calculates the angle between two 2d vectors.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns></returns>
        public static double Angle(Vector2d a, Vector2d b)
        {
            a.Unitize();
            b.Unitize();
            return Math.Acos(CalculateDotProduct(a, b));
        }

        public static bool TryParseAsDouble(string doubleString, out double result)
        {
            result = 0;
            var success = double.TryParse(doubleString, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
            return success;
        }

        public static double ParseAsDouble(string doubleString)
        {
            return double.Parse(doubleString, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Creates transformation matrice using two planes
        /// </summary>
        /// <param name="from">Original coordinate system</param>
        /// <param name="to">Desired coordinate system</param>
        /// <returns></returns>
        public static Transform CreateTransformation(Plane source, Plane target)
        {
            var rotationZ = Transform.Rotation(source.ZAxis, target.ZAxis, source.Origin);
            var sourceZRotated = source;
            sourceZRotated.Transform(rotationZ); //To calculate Y rotation

            var rotationY = Transform.Rotation(sourceZRotated.YAxis, target.YAxis, sourceZRotated.Origin);
            var rotationZY = Transform.Multiply(rotationY, rotationZ);            //If RotateZ then RotateY, need to multiply by RotateY * RotateZ

            var translation = Transform.Translation(target.Origin - source.Origin);
            var transformation = Transform.Multiply(translation, rotationZY);

            return transformation;
        }

        public static bool IsPlaneMathemathicallyEqual(Plane first, Plane second)
        {
            const double epsilon = 0.001;
            first.Normal.Unitize();
            second.Normal.Unitize();

            var equal = true;
            equal &= first.Normal.EpsilonEquals(second.Normal, epsilon);
            equal &= first.Origin.EpsilonEquals(second.Origin, epsilon);
            return equal;
        }

        public static double CalculateDegrees(Vector3d fromVector, Vector3d toVector)
        {
            fromVector.Unitize();
            toVector.Unitize();
            var dotResult = Vector3d.Multiply(fromVector, toVector);

            //Because chances are the dot resulted in 1.00000002 which will be passed to Math.Acos that accepts max of 1.0,
            //otherwise NaN will be returned. However if a dot product is 1 the angle should be zero.
            if (Math.Abs(dotResult - 1) < 0.0001)
            {
                return 0.0;
            }

            var rad = Math.Acos(dotResult);
            return RhinoMath.ToDegrees(rad);
        }

        //Returns 0 if point is in mesh
        public static double CalculatePointClosestDistanceToMesh(Point3d point, Mesh mesh)
        {
            if (mesh.IsPointInside(point, 0.0, true))
            {
                return 0.0;
            }

            var pointOnMesh = mesh.PullPointsToMesh(new[] {point}).FirstOrDefault();
            return pointOnMesh.DistanceTo(point);
        }

        public static bool IsWithin(double value, double minimum, double maximum)
        {
            return value >= minimum && value <= maximum;
        }

        public static bool DoubleEqual(double value1, double value2, double epsilon)
        {
            return Math.Abs(value1 - value2) < epsilon;
        }

        public static bool MovingAverage(IEnumerable<double> rawValues, int windowSize, out IEnumerable<double> movingAverageValues)
        {
            movingAverageValues = null;
            if (rawValues == null || !rawValues.Any())
            {
                return false;
            }

            var rawValuesList = rawValues.ToList();
            var containerSize = rawValuesList.Count;
            var movingAverageValuesList = new List<double>();
            var halfWindowSize = windowSize / 2;

            for (var i = 0; i < containerSize; i++)
            {
                var startIdx = i - halfWindowSize;
                var endIdx = i + halfWindowSize;
                startIdx = (startIdx < 0) ? 0 : startIdx;
                endIdx = (endIdx >= containerSize) ? containerSize - 1 : endIdx;
                var range = endIdx - startIdx;
                movingAverageValuesList.Add(rawValuesList.GetRange(startIdx, range).Average());
            }

            movingAverageValues = movingAverageValuesList;
            return true;
        }
    }
}