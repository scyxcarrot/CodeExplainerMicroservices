using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IDS.Core.V2.Utilities
{
    public enum BoundaryCondition
    {
        NaturalSpline = 1,
        NotAKnot = 2
    }

    public enum SimplificationAlgorithm
    {
        None = 1,
        Linear = 2,
    }

    public static class CreateSplineUtilities
    {
        // this is an initial starting value, but it will be updated when fitting the curve
        // this is so that we dont put the _epsilon value too tight or too relaxed
        private static double _epsilon = 1e-6;

        /// <summary>
        /// Method to interpolate a set of given points with a curve
        /// </summary>
        /// <param name="inputPoints">Coordinates given</param>
        /// <param name="degree">degree of polynomial curve to fit.
        /// Lower numbers are less wavy</param>
        /// <param name="distanceBetweenPoints">Specify the distance between the output points</param>
        /// <param name="closedCurve">True = curve generated will be in 1 closed loop</param>
        /// <param name="simplificationAlgorithm">Algorithm to reduce the amount of points</param>
        /// <returns>An ICurve with all the points</returns>
        public static ICurve FitCurve(List<IPoint3D> inputPoints, int degree,
            double distanceBetweenPoints, SimplificationAlgorithm simplificationAlgorithm,
            bool closedCurve = false)
        {
            var inputCoordinates = ConvertIDSPointToDouble(inputPoints);

            var curvePointsDouble = FitCurve(inputCoordinates, degree, distanceBetweenPoints, closedCurve);

            if (simplificationAlgorithm == SimplificationAlgorithm.Linear)
            {
                curvePointsDouble = LinearSimplification(curvePointsDouble, inputCoordinates);
            }

            var curvePoints = ConvertoDoubleToIDSPoint(curvePointsDouble);

            // check if all inputPoints is in the point list
            curvePoints = EnsureInputPointsPresent(inputPoints, curvePoints);
            var curve = new IDSCurve(curvePoints.ToList());

            return curve;
        }

        private static IList<IPoint3D> EnsureInputPointsPresent(
            IEnumerable<IPoint3D> inputPoints, 
            IList<IPoint3D> curvePoints)
        {

            foreach (var inputPoint in inputPoints)
            {
                // if not in, find the closest point and replace it instead
                if (curvePoints.Contains(inputPoint))
                {
                    continue;
                }
                var distances = CalculatePointToPointDistance(curvePoints, inputPoint);
                var minDistance = distances.Min();
                var minIndex = Array.IndexOf(distances, minDistance);

                curvePoints[minIndex] = inputPoint;
            }

            return curvePoints;
        }

        /// <summary>
        /// Method to interpolate a set of given points with a curve
        /// </summary>
        /// <param name="inputCoordinates">Coordinates given, can be 2D or 3D or in any dimension</param>
        /// <param name="degree">degree of polynomial curve to fit.
        ///     Lower numbers are less wavy</param>
        /// <param name="distanceBetweenPoints">Specify the distance between the output points</param>
        /// <param name="closedCurve">True, make sure the close curve algorithm is smooth</param>
        /// <returns>A double array with each row specifying a point</returns>
        public static double[,] FitCurve(double[,] inputCoordinates, int degree, double distanceBetweenPoints, bool closedCurve = false)
        {
            // update the Epsilon based on the distance between points
            _epsilon = distanceBetweenPoints / 100;

            if (closedCurve)
            {
                inputCoordinates = AddPointToCloseCurve(inputCoordinates);
            }

            // Calculate distances
            var distances = CalculateDistances(inputCoordinates);

            // Create 'times' to interpolate to
            var tValues = CreateTValues(distances, distanceBetweenPoints);

            // break into only x, y, z coordinates
            var coordinateList = BreakToSingleCoordinate(inputCoordinates);

            // setup the array to store the curve points
            var curvePoints = new double[
                tValues.Length,
                inputCoordinates.GetLength(1)];

            var coordinateIndex = 0;
            foreach (var coordinateValues in coordinateList)
            {
                // we use the natural spline boundary condition
                // because it is the closest mapping to Rhino's CreateInterpolatedCurve
                var allSplineCoefficients =
                    GetSplineCoefficients(coordinateValues, degree, distances,
                        BoundaryCondition.NaturalSpline, closedCurve);

                var tCounter = 0;
                foreach (var tValue in tValues)
                {
                    // check if tValue is the distance to put inputCoordinate
                    // this is to ensure the curve will always have the points specified by the user
                    var tValueIndex = Array.IndexOf(distances, tValue);
                    if (tValueIndex >= 0)
                    {
                        var inputCoordinateValue = inputCoordinates[tValueIndex, coordinateIndex];
                        curvePoints[tCounter, coordinateIndex] = inputCoordinateValue;
                        tCounter++;
                        continue;
                    }

                    // check where the tValue is in the distance array
                    var distanceIndex = 1;
                    for (; distanceIndex < distances.Length; distanceIndex++)
                    {
                        if (distances[distanceIndex] >= tValue)
                        {
                            break;
                        }
                    }

                    // sometimes the comparison in distances doesnt work for the last t point
                    // so we force it back to the last spline coefficients
                    // very rare case, but just put in case
                    if (distanceIndex > allSplineCoefficients.Count)
                    {
                        distanceIndex = allSplineCoefficients.Count;
                    }

                    // calculate the coordinate value and put it into curvePoints for output
                    var splineCoefficients = allSplineCoefficients[distanceIndex - 1];

                    var coordinateValue = 0.0;
                    var order = 0;
                    foreach (var splineCoefficient in splineCoefficients)
                    {
                        var valueDifference = tValue - distances[distanceIndex - 1];
                        var termValue = splineCoefficient * Math.Pow(valueDifference, order);
                        coordinateValue += termValue;
                        order++;
                    }

                    curvePoints[tCounter, coordinateIndex] = coordinateValue;
                    tCounter++;
                }
                coordinateIndex++;
            }

            return curvePoints;
        }

        private static double[,] AddPointToCloseCurve(double[,] inputCoordinates)
        {
            // get first coordinate
            var firstCoordinate = new double[3]
            {
                inputCoordinates[0, 0],
                inputCoordinates[0, 1],
                inputCoordinates[0, 2],
            };

            var lastCoordinate = new double[3]
            {
                inputCoordinates[inputCoordinates.GetLength(0)-1, 0],
                inputCoordinates[inputCoordinates.GetLength(0)-1, 1],
                inputCoordinates[inputCoordinates.GetLength(0)-1, 2],
            };

            var isCoordianteEqual = true;
            for (var index = 0; index < firstCoordinate.Length; index++)
            {
                isCoordianteEqual &= firstCoordinate[index] == lastCoordinate[index];
            }

            if (!isCoordianteEqual)
            {
                var closedInputCoordinates = new double[
                    inputCoordinates.GetLength(0) + 1,
                    inputCoordinates.GetLength(1)];
                for (var index = 0; index < inputCoordinates.GetLength(0); index++)
                {
                    closedInputCoordinates[index, 0] = inputCoordinates[index, 0];
                    closedInputCoordinates[index, 1] = inputCoordinates[index, 1];
                    closedInputCoordinates[index, 2] = inputCoordinates[index, 2];
                }

                closedInputCoordinates[inputCoordinates.GetLength(0), 0] =
                    inputCoordinates[0, 0];
                closedInputCoordinates[inputCoordinates.GetLength(0), 1] =
                    inputCoordinates[0, 1];
                closedInputCoordinates[inputCoordinates.GetLength(0), 2] =
                    inputCoordinates[0, 2];

                return closedInputCoordinates;
            }
            else
            {
                return inputCoordinates;
            }
        }

        /// <summary>
        /// For debugging purposes to see the A matrix
        /// </summary>
        /// <param name="array">The A matrix to check</param>
        /// <returns></returns>
        private static string ArrayToString(double[,] array)
        {
            var stringBuilder = new StringBuilder();
            for (var rowIndex = 0; rowIndex < array.GetLength(0); rowIndex++)
            {
                for (var colIndex = 0; colIndex < array.GetLength(1); colIndex++)
                {
                    stringBuilder.Append(array[rowIndex, colIndex].ToString());
                    stringBuilder.Append(", ");
                }

                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }

        private static double[] CalculatePointToPointDistance(
            IList<IPoint3D> pointsFrom, 
            IPoint3D pointTo)
        {
            var distances = new double[pointsFrom.Count];

            var index = 0;
            foreach (var pointFrom in pointsFrom)
            {
                var distance = pointFrom.Sub(pointTo).GetLength();
                distances[index] = distance;
            }
            
            return distances;
        }

        private static double[] CalculateDistances(double[,] inputCoordinates)
        {
            var numberOfCoordinates = inputCoordinates.GetLength(0);
            var distances = new double[numberOfCoordinates];
            var totalDistance = 0.0;

            for (var rowIndex = 1; rowIndex < numberOfCoordinates; rowIndex++)
            {
                var totalDifferenceSquared = 0.0;
                for (var colIndex = 0; colIndex < inputCoordinates.GetLength(1); colIndex++)
                {
                    var difference = inputCoordinates[rowIndex, colIndex] - inputCoordinates[rowIndex - 1, colIndex];
                    totalDifferenceSquared += difference * difference;
                }

                var distance = Math.Sqrt(totalDifferenceSquared);

                totalDistance += distance;
                distances[rowIndex] = totalDistance;
            }

            return distances;
        }

        private static double[] CreateTValues(IEnumerable<double> distances, double distanceBetweenPoints)
        {
            var tValuesList = new List<double> { 0.0 };

            foreach (var distance in distances)
            {
                var distanceInBetween = distance - tValuesList.Last();
                var numberOfTimes = Math.Floor(
                    distanceInBetween / distanceBetweenPoints);
                for (var index = 0; index < numberOfTimes; index++)
                {
                    var tValue = tValuesList.Last() + distanceBetweenPoints;
                    tValuesList.Add(tValue);
                }

                if (tValuesList.Last() < distance)
                {
                    tValuesList.Add(distance);
                }
            }

            return tValuesList.ToArray();
        }

        private static List<List<double>> BreakToSingleCoordinate(double[,] inputCoordinates)
        {
            var coordinateList = new List<List<double>>();
            var numberOfCoordinates = inputCoordinates.GetLength(0);
            for (var colIndex = 0; colIndex < inputCoordinates.GetLength(1); colIndex++)
            {
                var coordinateValues = new List<double>();
                for (var rowIndex = 0; rowIndex < numberOfCoordinates; rowIndex++)
                {
                    var coordinateValue = inputCoordinates[rowIndex, colIndex];
                    coordinateValues.Add(coordinateValue);
                }

                coordinateList.Add(coordinateValues);
            }

            return coordinateList;
        }

        private static List<List<double>> GetSplineCoefficients(
            List<double> coordinateValues, int degree, double[] distances, BoundaryCondition boundaryCondition, bool closedCurve)
        {
            if (closedCurve &&
                Math.Abs(coordinateValues[0] - coordinateValues[coordinateValues.Count - 1]) > _epsilon)
            {
                throw new ArgumentException("In closed curve, the first and last point must be the same");
            }

            if (coordinateValues.Count < 2)
            {
                throw new ArgumentException("Number of coordinates must be 2 or more");
            }

            // overwrite the user specified degree to because its impossible to calculate.
            // higher degrees need 3 points or more
            if (coordinateValues.Count == 2)
            {
                degree = 1;
            }

            var numberOfCoordinates = coordinateValues.Count();
            var numberOfUnknowns = (degree + 1) * (numberOfCoordinates - 1);
            var coefficientArray = new double[numberOfUnknowns, numberOfUnknowns];
            var constantArray = new double[numberOfUnknowns];

            var columnIndex = 0;
            var rowIndex = 0;

            // start of spline must pass through points
            for (var index = 0; index < coordinateValues.Count - 1; index++)
            {
                coefficientArray[index, columnIndex] = 1;
                constantArray[index] = coordinateValues[index];
                columnIndex += degree + 1;
                rowIndex++;
            }

            columnIndex = 0;
            // end of spline must pass through points
            for (var index = 1; index < coordinateValues.Count; index++)
            {
                for (var increment = 0; increment <= degree; increment++)
                {
                    coefficientArray[rowIndex, columnIndex + increment] =
                        Math.Pow(distances[index] - distances[index - 1], increment);
                }

                constantArray[rowIndex] = coordinateValues[index];
                columnIndex += degree + 1;
                rowIndex++;
            }

            // derivatives of splines must be equal
            columnIndex = 0;
            for (var index = 1; index < coordinateValues.Count - 1; index++)
            {
                for (var degreeIndex = 1; degreeIndex < degree; degreeIndex++)
                {
                    for (var increment = degreeIndex; increment <= degree; increment++)
                    {
                        coefficientArray[rowIndex, columnIndex + increment] =
                            Math.Pow(distances[index] - distances[index - 1], increment - degreeIndex) * Factorial(increment) / Factorial(increment - degreeIndex);
                    }
                    coefficientArray[rowIndex, columnIndex + degree + degreeIndex + 1]
                        = -degreeIndex;

                    rowIndex++;
                }
                columnIndex += degree + 1;
            }

            // check how many more equations (boundary conditions needed)
            var boundaryConditionsNeeded = (double)(numberOfUnknowns - rowIndex);

            // closed curve condition
            // first spline and last spline have the same derivatives
            if (closedCurve)
            {
                // take the lower value between number of boundary conditions and number of possible derivatives
                var possibleDerivatives = degree - 1;
                var lowerValue = possibleDerivatives > boundaryConditionsNeeded
                    ? boundaryConditionsNeeded
                    : possibleDerivatives;

                for (var index = 0; index < lowerValue; index++)
                {
                    coefficientArray[rowIndex, index + 1]
                        = Factorial(index + 1) / Factorial(index);

                    for (var degreeIndex = 0; degreeIndex < degree - index; degreeIndex++)
                    {
                        coefficientArray[rowIndex,
                                numberOfUnknowns - degree + degreeIndex + index]
                            = -Factorial(degreeIndex + index + 1) / Factorial(degreeIndex)
                              * Math.Pow(distances[distances.Length - 1] - distances[distances.Length - 2], degreeIndex);
                    }
                    rowIndex++;
                }
            }

            // check how many more equations (boundary conditions needed)
            boundaryConditionsNeeded = numberOfUnknowns - rowIndex;

            // not a knot boundary condition (highest order derivative is equal)
            // for first two splines at start and end
            if (boundaryCondition == BoundaryCondition.NotAKnot)
            {
                // start points
                if (rowIndex < numberOfUnknowns &&
                    numberOfCoordinates > 3 &&
                    boundaryConditionsNeeded > 0)
                {
                    coefficientArray[rowIndex, degree] = Factorial(degree);
                    coefficientArray[rowIndex, degree * 2 + 1] = -Factorial(degree);
                    rowIndex++;
                    boundaryConditionsNeeded--;
                }

                // end points
                if (rowIndex < numberOfUnknowns &&
                    numberOfCoordinates > 3 &&
                    boundaryConditionsNeeded > 0)
                {
                    coefficientArray[rowIndex, numberOfUnknowns - 1] = Factorial(degree);
                    coefficientArray[rowIndex, numberOfUnknowns - degree - 2] = -Factorial(degree);
                    rowIndex++;
                }
            }

            // natural boundary condition (2nd highest order derivative = 0 at ends)
            boundaryConditionsNeeded = numberOfUnknowns - rowIndex;
            for (var index = 0; index < Math.Ceiling(boundaryConditionsNeeded / 2); index++)
            {
                coefficientArray[rowIndex, degree - index - 1] = Factorial(degree - index - 1);
                rowIndex++;
            }

            for (var index = 0; index < Math.Floor(boundaryConditionsNeeded / 2); index++)
            {
                for (var degreeIndex = 0; degreeIndex < index + 2; degreeIndex++)
                {
                    coefficientArray[rowIndex,
                            (coordinateValues.Count - 1) * (degree + 1) + degreeIndex - index - 2] =
                        Factorial(degree + degreeIndex - index - 1) / Factorial(degreeIndex) *
                        Math.Pow(distances[distances.Length - 1] - distances[distances.Length - 2], degreeIndex);
                }

                rowIndex++;
            }

            // Solve the equation with Ax=B
            var coefficientList = SimultaneousEquationUtilities.LUDecomposition(coefficientArray, constantArray);

            // Break it down to each spline
            var coefficientCounter = 0;
            var allSplineCoefficients = new List<List<double>>();
            for (var index = 0; index < numberOfCoordinates - 1; index++)
            {
                var splineCoefficients = new List<double>();
                for (var counter = 0; counter <= degree; counter++)
                {
                    var splineCoefficient = coefficientList[coefficientCounter];
                    splineCoefficients.Add(splineCoefficient);
                    coefficientCounter++;
                }

                allSplineCoefficients.Add(splineCoefficients);
            }

            return allSplineCoefficients;
        }

        private static int Factorial(int n)
        {
            if (n <= 0)
            {
                return 1;
            }

            var result = Enumerable.Range(1, n).Aggregate(1, (p, item) => p * item);
            return result;
        }

        private static double[,] ConvertIDSPointToDouble(List<IPoint3D> inputPoints)
        {
            var outputPoints = new double[inputPoints.Count, 3];
            var counter = 0;
            foreach (var inputPoint in inputPoints)
            {
                outputPoints[counter, 0] = inputPoint.X;
                outputPoints[counter, 1] = inputPoint.Y;
                outputPoints[counter, 2] = inputPoint.Z;

                counter++;
            }

            return outputPoints;
        }

        private static IList<IPoint3D> ConvertoDoubleToIDSPoint(double[,] inputPoints)
        {
            var outputPoints = new List<IPoint3D>();
            for (var index = 0; index < inputPoints.GetLength(0); index++)
            {
                var xCoordinate = inputPoints[index, 0];
                var yCoordinate = inputPoints[index, 1];
                var zCoordinate = inputPoints[index, 2];
                var point = new IDSPoint3D(xCoordinate, yCoordinate, zCoordinate);
                outputPoints.Add(point);
            }
            return outputPoints;
        }

        // we want to keep the points given by user
        private static double[,] LinearSimplification(double[,] inputArray, double[,] pointsToKeep)
        {
            // always take first point
            var pointsToTake = new List<double[]> { GetSpecificRow(inputArray, 0) };

            // in linear simplification, check if gradient on previous and next is the same
            // if same, dont need to add,
            // if different, add the point
            for (var rowIndex = 1;
                 rowIndex < inputArray.GetLength(0) - 1;
                 rowIndex++)
            {
                var previousPoint = pointsToTake[pointsToTake.Count - 1];
                var currentPoint = GetSpecificRow(inputArray, rowIndex);
                var nextPoint = GetSpecificRow(inputArray, rowIndex + 1);
                // calculate vector before and after the point
                var previousVector = SubtractPoints(currentPoint, previousPoint);
                var nextVector = SubtractPoints(nextPoint, currentPoint);

                // compare if vectors are the same
                var previousVectorUnitized = GetUnitVector(previousVector);
                var nextVectorUnitized = GetUnitVector(nextVector);
                var isVectorEqual = true;
                for (var index = 0; index < previousVectorUnitized.Length; index++)
                {
                    isVectorEqual &=
                        Math.Abs(previousVectorUnitized[index] - nextVectorUnitized[index]) < _epsilon;
                }

                // if different or it is a user point, add it in the point
                // if same, do nothing, 
                if (!isVectorEqual ||
                    ContainsArray(pointsToKeep, currentPoint) != -1)
                {
                    pointsToTake.Add(GetSpecificRow(inputArray, rowIndex));
                }
            }

            // always take last point
            pointsToTake.Add(
                GetSpecificRow(inputArray, inputArray.GetLength(0) - 1));

            // convert list<double[]> back to double array
            var outputArray = new double[pointsToTake.Count, inputArray.GetLength(1)];
            for (var rowIndex = 0; rowIndex < pointsToTake.Count; rowIndex++)
            {
                for (var columnIndex = 0;
                     columnIndex < inputArray.GetLength(1);
                     columnIndex++)
                {
                    outputArray[rowIndex, columnIndex] = pointsToTake[rowIndex][columnIndex];
                }
            }
            return outputArray;
        }

        private static double GetMagnitude(double[] inputArray)
        {
            var squaredSum = inputArray.Sum(t => Math.Pow(t, 2));
            return Math.Sqrt(squaredSum);
        }

        private static double[] GetUnitVector(double[] inputArray)
        {
            var magnitude = GetMagnitude(inputArray);

            var outputArray = new double[inputArray.Length];
            for (var index = 0; index < inputArray.Length; index++)
            {
                outputArray[index] = inputArray[index] / magnitude;
            }

            return outputArray;
        }

        private static double[] GetSpecificRow(double[,] inputArray, int rowIndex)
        {
            var outputArray = new double[inputArray.GetLength(1)];
            for (var columnIndex = 0;
                 columnIndex < inputArray.GetLength(1);
                 columnIndex++)
            {
                outputArray[columnIndex] = inputArray[rowIndex, columnIndex];
            }

            return outputArray;
        }

        private static double[] SubtractPoints(double[] finalPoint, double[] initialPoint)
        {
            if (finalPoint.Length != initialPoint.Length)
            {
                throw new ArgumentException("Final and initial vectors must have the same size");
            }

            var differenceVector = new double[finalPoint.Length];
            for (var index = 0; index < finalPoint.Length; index++)
            {
                differenceVector[index] = finalPoint[index] - initialPoint[index];
            }

            return differenceVector;
        }

        private static int ContainsArray(double[,] inputArray2D, double[] arrayToFind)
        {
            // target array length must match 2d array column length
            if (arrayToFind.Length != inputArray2D.GetLength(1))
            {
                return -1;
            }

            // Find match
            for (var rowIndex = 0; rowIndex < inputArray2D.GetLength(0); rowIndex++)
            {
                var arrayFromRowIndex = GetSpecificRow(inputArray2D, rowIndex);

                var isMatch = true;
                for (var columnIndex = 0; columnIndex < arrayFromRowIndex.Length; columnIndex++)
                {
                    isMatch &= Math.Abs(arrayFromRowIndex[columnIndex] - arrayToFind[columnIndex]) < _epsilon;
                }

                if (isMatch)
                {
                    return rowIndex;
                }
            }

            return -1;
        }
    }
}
