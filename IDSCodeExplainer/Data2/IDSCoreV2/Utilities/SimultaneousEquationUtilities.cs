using System;

namespace IDS.Core.V2.Utilities
{
    public class SimultaneousEquationUtilities
    {
		/// <summary>
        /// Finds roots of n linear equations using LU Decomposition,
        /// use when matrix is not diagonally dominant		
        /// </summary>
        /// <param name="aMatrix">2D double array containing the co-efficients</param>
        /// <param name="bMatrix">Double array containing the constants</param>
        /// <returns>Double array containing the roots</returns>
        public static double[] LUDecomposition(double[,] aMatrix, double[] bMatrix)
        {
            if (aMatrix.GetLength(0) != aMatrix.GetLength(1))
            {
                throw new ArgumentException("aMatrix given must be a square matrix");
            }

            if (aMatrix.GetLength(0) != bMatrix.Length)
            {
                throw new ArgumentException("aMatrix and bMatrix must have equal length");
            }

            var solver = new LUDecompositionSolver(aMatrix, bMatrix);
            var x = solver.FindUnknowns();

            return x;
        }

        private static bool IsInputMatricesValid(double[,] aMatrix, double[] bMatrix, out string message)
        {
            var aMatrixRowCount = aMatrix.GetLength(0);
            var aMatrixColCount = aMatrix.GetLength(1);
            if (aMatrixRowCount != aMatrixColCount)
            {
                message = "aMatrix given must have the same number of rows and columns";
                return false;
            }

            if (aMatrixRowCount != bMatrix.Length)
            {
                message = "aMatrix row / column number must be the same as the number of rows in bMatrix";
				return false;
            }

            message = string.Empty;
            return true;
        }

        private static bool PartialPivoting(double[,] aMatrix, double[] bMatrix, 
            out double[,] aMatrixPivot, out double[] bMatrixPivot)
        {
            aMatrixPivot = aMatrix.Clone() as double[,];
            bMatrixPivot = bMatrix.Clone() as double[];

            var rowCount = aMatrixPivot.GetLength(0);
            // pivoting
            for (var rowIndex = 0; rowIndex + 1 < rowCount; rowIndex++)
            {
                // check for zero coefficients
                if (aMatrixPivot[rowIndex, rowIndex] != 0)
                {
                    continue;
                }

                // find non-zero coefficient
                var swapRow = rowIndex + 1;
                for (; swapRow < rowCount; swapRow++)
                {
                    if (aMatrixPivot[swapRow, rowIndex] != 0)
                    {
                        break;
                    }
                }

                if (swapRow < rowCount && aMatrixPivot[swapRow, rowIndex] != 0) // found a non-zero coefficient?
                {
                    // yes, then swap it with the above
                    for (var i = 0; i < rowCount; i++)
                    {
                        var aTemp = aMatrixPivot[swapRow, i];
                        aMatrixPivot[swapRow, i] = aMatrixPivot[rowIndex, i];
                        aMatrixPivot[rowIndex, i] = aTemp;
                    }

                    var bTemp = bMatrixPivot[swapRow];
                    bMatrixPivot[swapRow] = bMatrixPivot[rowIndex];
                    bMatrixPivot[rowIndex] = bTemp;
                }
                else
                {
                    return false; // no, then the matrix has no unique solution
                }
            }
            return true;
        }
	}
}
