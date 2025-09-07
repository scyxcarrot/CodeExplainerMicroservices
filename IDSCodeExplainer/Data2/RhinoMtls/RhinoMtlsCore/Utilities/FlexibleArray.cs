using System;

namespace RhinoMtlsCore.Utilities
{
    public class FlexibleArray<T>
    {
        public T[,] Data;

        public FlexibleArray()
        {
            Data = new T[0,0];
        }

        public FlexibleArray(int rows, int columns)
        {
            Data = new T[rows, columns];
        }

        public FlexibleArray(T[,] array)
        {
            Data = array;
        }

        public int Rows => Data.GetLength(0);
        public int Columns => Data.GetLength(1);

        public void AddRows(T[,] additionalArray)
        {
            if (Columns != additionalArray.GetLength(1))
            {
                throw new Exception("Number of columns has to match to add rows.");
            }

            // Copy existing data into temporary variable
            var tempData = new T[Rows, Columns];
            for (var i = 0; i < Rows; i++)
            {
                for (var j = 0; j < Columns; j++)
                {
                    tempData[i, j] = Data[i, j];
                }
            }

            // Reinitialize data
            Data = new T[Rows + additionalArray.GetLength(0), Columns];
            // Add old data
            for (var i = 0; i < tempData.GetLength(0); i++)
            {
                for (var j = 0; j < Columns; j++)
                {
                    Data[i, j] = tempData[i, j];
                }
            }
            // Add additional data
            for (var i = 0; i < additionalArray.GetLength(0); i++)
            {
                for (var j = 0; j < Columns; j++)
                {
                    Data[tempData.GetLength(0) + i, j] = additionalArray[i, j];
                }
            }
        }
    }
}
