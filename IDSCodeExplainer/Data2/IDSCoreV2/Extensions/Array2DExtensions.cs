using System;
using System.Linq;

namespace IDS.Core.V2.Extensions
{
    public static class Array2DExtensions
    {
        public static TType[,] CastedClone<TType>(this TType[,] array2D)
        {
            return (TType[,])array2D.Clone();
        }

        public static int RowCount<TType>(this TType[,] array2D)
        {
            return array2D.GetLength(0);
        }

        public static int ColumnCount<TType>(this TType[,] array2D)
        {
            return array2D.GetLength(1);
        }

        public static TType[] GetRow<TType>(this TType[,] array2D, int row)
        {
            return Enumerable.Range(0, array2D.GetLength(1))
                .Select(x => array2D[row, x])
                .ToArray();
        }

        public static void SetRow<TType>(this TType[,] array2D, int row, TType[] sourceRow)
        {
            if (array2D.ColumnCount() != sourceRow.Length)
            {
                throw new IndexOutOfRangeException("array2D had different column number with rowData");
            }

            for (var i = 0; i < sourceRow.Length; i++)
            {
                array2D[row, i] = sourceRow[i];
            }
        }
    }
}
