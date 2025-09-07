using System;
using System.Linq;

namespace IDS.Core.V2.Utilities
{
    internal class XFinder
    {
        private readonly double[,] _luMatrix;
        private readonly double[] _bMatrix;
        private double[] _yMatrix;
        private double[] _xMatrix;
        private readonly int[] _rowPermutations;

        private readonly int _numberOfRows;

        public XFinder(double[,] luMatrix, int[] rowPermutations, double[] bMatrix)
        {
            _luMatrix = luMatrix;
            _rowPermutations = rowPermutations;
            _bMatrix = bMatrix;

            _numberOfRows = _luMatrix.GetLength(0);

            _yMatrix = new double[_numberOfRows];
            _xMatrix = new double[_numberOfRows];
        }

        private void ReorderB()
        {
            var reorderedB = new double[_bMatrix.Length];

            for (var row = 0; row < _numberOfRows; row++)
            {
                reorderedB[row] = _bMatrix[_rowPermutations[row]];
            }

            for (var row = 0; row < _numberOfRows; row++)
            {
                _bMatrix[row] = reorderedB[row];
            }
        }

        void SolveYFromLYEqualB()
        {

            for (var rowIndex = 0; rowIndex < _bMatrix.Length; rowIndex++)
            {
                var sum = 0.0;
                for (var colIndex = 0; colIndex < rowIndex; colIndex++)
                {
                    sum += _yMatrix[colIndex] * _luMatrix[rowIndex, colIndex];
                }
                _yMatrix[rowIndex] = _bMatrix[rowIndex] - sum;
            }
        }

        void SolveXFromUXEqualY()
        {
            for (var rowIndex = _yMatrix.Length - 1; rowIndex > -1; rowIndex--)
            {
                var sum = 0.0;
                for (var colIndex = rowIndex + 1; colIndex < _xMatrix.Length; colIndex++)
                {
                    sum += _xMatrix[colIndex] * _luMatrix[rowIndex, colIndex];
                }
                _xMatrix[rowIndex] = (_yMatrix[rowIndex] - sum) / _luMatrix[rowIndex, rowIndex];
            }
        }

        private void CheckX()
        {
            if (_xMatrix.Any(t => double.IsNaN(t)))
            {
                throw new Exception("Error: No solution for this, AX=B, system found.");
            }
        }

        public double[] Solve()
        {
            ReorderB();
            SolveYFromLYEqualB();
            SolveXFromUXEqualY();
            CheckX();
            return _xMatrix;
        }
    }
}
