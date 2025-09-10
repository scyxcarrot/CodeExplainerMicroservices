using IDS.Core.V2.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class Array2DExtensionTests
    {        
        [TestMethod]
        public void PrimitiveCastedCloneTest()
        {
            var doubleArray2D = new[,] {{1.0, 2.0}, {3.0, 4.0}};

            var clonedDoubleArray2D = doubleArray2D.CastedClone();

            CollectionAssert.AreEqual(doubleArray2D, clonedDoubleArray2D);

            doubleArray2D[0, 1] = 5.0;
            CollectionAssert.AreNotEqual(doubleArray2D, clonedDoubleArray2D);
        }

        [TestMethod]
        public void StringCastedCloneTest()
        {
            var doubleArray2D = new[,] { { "A", "B" }, { "C", "D" } };

            var clonedDoubleArray2D = doubleArray2D.CastedClone();

            CollectionAssert.AreEqual(doubleArray2D, clonedDoubleArray2D);

            doubleArray2D[0, 1] = "E";
            CollectionAssert.AreNotEqual(doubleArray2D, clonedDoubleArray2D);
        }

        [TestMethod]
        public void RowCountTest()
        {
            var doubleArray2D = new[,] { { 1.0, 2.0 }, { 3.0, 4.0 }, { 5.0, 6.0 } };
            
            Assert.AreEqual(3, doubleArray2D.RowCount());
        }

        [TestMethod]
        public void ColumnCountTest()
        {
            var doubleArray2D = new[,] { { 1.0, 2.0 }, { 3.0, 4.0 }, { 5.0, 6.0 } };

            Assert.AreEqual(2, doubleArray2D.ColumnCount());
        }


        [TestMethod]
        public void GetRowTest()
        {
            var doubleArray2D = new[,] { { 1.0, 2.0 }, { 3.0, 4.0 } };
            var rowArray = doubleArray2D.GetRow(1);

            var expectedRow = new[] {3.0, 4.0};

            CollectionAssert.AreEqual(expectedRow, rowArray);
        }
    }
}
