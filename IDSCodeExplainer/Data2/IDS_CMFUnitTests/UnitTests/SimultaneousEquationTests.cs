using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class SimultaneousEquationTests
    {
        private const double Epsilon = 0.01;

        /// <summary>
        /// The equation question is based on video https://www.youtube.com/watch?v=NlpykbGDzF8
        /// We use this as a sample to make sure that the simultaneous equation solver works
        /// </summary>
        [TestMethod]
        public void Simultaneous_Equation_Works_For_3_Equations()
        {
            // Arrange
            var aMatrix = new double[,]
            {
                {3, 2, -1},
                {2, -3, 1},
                {5, 1, -2},
            };
            var bMatrix = new double[] { 11, 7, 12 };

            // Act
            var xValues = SimultaneousEquationUtilities.LUDecomposition(aMatrix, bMatrix);

            // Assert
            Assert.AreEqual(xValues.Length, bMatrix.Length);
            Assert.AreEqual(xValues[0], 4, Epsilon);
            Assert.AreEqual(xValues[1], 2, Epsilon);
            Assert.AreEqual(xValues[2], 5, Epsilon);
        }

        /// <summary>
        /// The equation question is based on video https://www.youtube.com/watch?v=d6vyYvx8URw
        /// Sample to make sure that the solver works for any number of equations
        /// </summary>
        [TestMethod]
        public void Simultaneous_Equation_Works_For_2_Equations()
        {
            // Arrange
            var aMatrix = new double[,]
            {
                {3, 2},
                {5, -3},
            };
            var bMatrix = new double[] { 7, 37 };

            // Act
            var xValues = SimultaneousEquationUtilities.LUDecomposition(aMatrix, bMatrix);

            // Assert
            Assert.AreEqual(xValues.Length, bMatrix.Length);
            Assert.AreEqual(xValues[0], 5, Epsilon);
            Assert.AreEqual(xValues[1], -4, Epsilon);
        }

        /// <summary>
        /// The equation question is based on video https://www.youtube.com/watch?v=hc1VMzVVtMg&amp;t=162s
        /// Sample to make sure that partial pivoting works
        /// </summary>
        [TestMethod]
        public void Simultaneous_Equation_Works_For_Partial_Pivoting_3_Equations()
        {
            // Arrange
            var aMatrix = new double[,]
            {
                {1, -1, 1},
                {-6, 1, -1},
                {3, 1, 1},
            };
            var bMatrix = new double[] { 2, 3, 4 };

            // Act
            var xValues = SimultaneousEquationUtilities.LUDecomposition(aMatrix, bMatrix);

            // Assert
            Assert.AreEqual(xValues.Length, bMatrix.Length);
            Assert.AreEqual(xValues[0], -1, Epsilon);
            Assert.AreEqual(xValues[1], 2, Epsilon);
            Assert.AreEqual(xValues[2], 5, Epsilon);
        }

        /// <summary>
        /// Throw error if A and B matrix not same length
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException),
            "aMatrix and bMatrix must have equal length")]
        public void Simultaneous_Equation_Throw_Error_For_Invalid_ABMatrix_Inputs()
        {
            // Arrange
            var aMatrix = new double[,]
            {
                {1, -1, 1},
                {-6, 1, -1},
                {3, 1, 1},
            };
            var bMatrix = new double[] { 2, 3 };

            // Act
            var xValues = SimultaneousEquationUtilities.LUDecomposition(aMatrix, bMatrix);

            // Assert - Exception expected
        }

        /// <summary>
        /// This makes sure that the A matrix must be a square matrix
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException),
            "aMatrix given must be a square matrix")]
        public void Simultaneous_Equation_Throw_Error_For_Non_Square_AMatrix()
        {
            // Arrange
            var aMatrix = new double[,]
            {
                {1, -1, 1},
                {-6, 1, -1},
            };
            var bMatrix = new double[] { 2, 3 };

            // Act
            var xValues = SimultaneousEquationUtilities.LUDecomposition(aMatrix, bMatrix);

            // Assert - Exception expected
        }

        /// <summary>
        /// This makes sure that the A matrix if cannot be solved, throw error to warn
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception),
            "Error: No solution for this, AX=B, system found.")]
        public void Simultaneous_Equation_Throw_Error_If_Unable_To_Get_Row_Echleon()
        {
            // Arrange
            var aMatrix = new double[,]
            {
                {1, 1, 1},
                {2, 2, 2},
                {3, 3, 3},
            };
            var bMatrix = new double[] { 2, 3, 4 };

            // Act
            var xValues = SimultaneousEquationUtilities.LUDecomposition(aMatrix, bMatrix);

            // Assert - Exception expected
        }
    }
}
