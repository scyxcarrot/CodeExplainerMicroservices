using System;

namespace IDS.Core.V2.Utilities
{
    internal class PLUDecomposer
    {
        private readonly double[,] _aMatrix;
        private int[] _rowPermutations;

        private readonly SubMatrixBoundaries _lowerTriangleBounds;
        private readonly int _numberOfColumns;
        private readonly int _numberOfRows;

        public PLUDecomposer(double[,] aMatrix)
        {
            _aMatrix = aMatrix;
            _numberOfRows = aMatrix.GetLength(0);
            _numberOfColumns = aMatrix.GetLength(1);

            InitializeRowPermutations();

            _lowerTriangleBounds = new SubMatrixBoundaries()
            {
                StartRow = 1,
                EndRow = _numberOfRows,
                StartColumn = 0,
                EndColumn = _numberOfColumns - 1,
            };
        }

        private void InitializeRowPermutations()
        {
            _rowPermutations = new int[_numberOfRows];
            for (int row = 0; row < _numberOfRows; row++)
            {
                _rowPermutations[row] = row;
            }
        }

        public void FindRowPermutationAndLU(out int[] rowPermutations, out double[,] luMatrix)
        {
            MakeLowerTriangleZeroAndFillWithL();
            rowPermutations = _rowPermutations;
            luMatrix = _aMatrix;
        }

        private void MakeLowerTriangleZeroAndFillWithL()
        {
            for (var colIndex = _lowerTriangleBounds.StartColumn; 
                 colIndex < _lowerTriangleBounds.EndColumn; 
                 colIndex++)
            {
                MakeSureDiagonalElementIsMaximum(colIndex);
                MakeColumnZero(colIndex);
            }
        }

        private void MakeSureDiagonalElementIsMaximum(int focusedColumn)
        {

            var rowOfDiagonal = focusedColumn;
            var rowOfMaxElement = GetRowOfMaxElementUnderDiagonal(focusedColumn);

            if (rowOfMaxElement != rowOfDiagonal)
            {
                SwapRows(rowOfMaxElement, rowOfDiagonal);
            };

        }

        private int GetRowOfMaxElementUnderDiagonal(int focusedColumn)
        {
            var rowOfDiagonal = focusedColumn;
            var columnOfDiagonal = focusedColumn;
            var rowAfterDiagonal = rowOfDiagonal + 1;

            var maxElement = _aMatrix[rowOfDiagonal, columnOfDiagonal];
            var rowOfMaxElement = rowOfDiagonal;


            for (var rowIndex = rowAfterDiagonal; rowIndex < _numberOfRows; rowIndex++)
            {
                if (Math.Abs(_aMatrix[rowIndex, focusedColumn]) > maxElement)
                {
                    maxElement = Math.Abs(_aMatrix[rowIndex, focusedColumn]);
                    rowOfMaxElement = rowIndex;
                }
            }
            return rowOfMaxElement;
        }

        private void SwapRows(int row1, int row2)
        {
            for (int column = 0; column < _numberOfColumns; column++)
            {
                var tmp = _aMatrix[row1, column];
                _aMatrix[row1, column] = _aMatrix[row2, column];
                _aMatrix[row2, column] = tmp;
            }
            RecordSwap(row1, row2);
        }

        private void RecordSwap(int row1, int row2)
        {
            var tmp = _rowPermutations[row1];
            _rowPermutations[row1] = _rowPermutations[row2];
            _rowPermutations[row2] = tmp;
        }

        private void MakeColumnZero(int column)
        {
            var rowUnderDiagonalElement = column + 1;
            for (var rowIndex = rowUnderDiagonalElement; 
                 rowIndex < _lowerTriangleBounds.EndRow; 
                 rowIndex++)
            {
                MakeElementZeroAndFillWithLowerMatrixElement(rowIndex, column);
            }
        }

        private void MakeElementZeroAndFillWithLowerMatrixElement(int elementRow, int elementColumn)
        {
            var element = _aMatrix[elementRow, elementColumn];
            var sameColumnDiagonalElement = _aMatrix[elementColumn, elementColumn];

            var rowMultiplier = - element / sameColumnDiagonalElement;

            for (var colIndex = elementColumn; 
                 colIndex < _numberOfColumns; 
                 colIndex++)
            {
                _aMatrix[elementRow, colIndex] += rowMultiplier * _aMatrix[elementColumn, colIndex];
            }

            var lowerMatrixElement = - rowMultiplier;
            _aMatrix[elementRow, elementColumn] = lowerMatrixElement;
        }
    }
}
