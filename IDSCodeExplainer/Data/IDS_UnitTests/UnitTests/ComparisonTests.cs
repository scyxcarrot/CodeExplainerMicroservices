using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ComparisonTests
    {
        [TestMethod]
        //Sample code to try out the expectation of CollectionAssert
        public void Collection_Assert_Test()
        {
            var x = new List<int>() {1, 2, 3, 4};
            var y = new List<int>() {1, 2, 3, 4};
            CollectionAssert.AreEqual(x, y);

            x = new List<int>() {1, 2, 3, 4};
            y = new List<int>() {1, 2, 4, 3};
            CollectionAssert.AreNotEqual(x, y);

            CollectionAssert.AreEquivalent(x, y);
        }

        [TestMethod]
        public void Nested_Lists_Sequence_Equivalent_Test()
        {
            var p = new List<List<int>>()
            {
                new List<int>() {1, 2, 3},
                new List<int>() {4, 5, 6},
                new List<int>() {7, 8},
            };
            var q = new List<List<int>>()
            {
                new List<int>() {1, 2, 3},
                new List<int>() {4, 5, 6},
                new List<int>() {7, 8},
            };

            Assert.IsTrue(Comparison.AreNestedListsEquivalent(p, q));
        }

        [TestMethod]
        public void Nested_Lists_Sequence_Not_Equivalent_Test()
        {
            var p = new List<List<int>>()
            {
                new List<int>() {1, 1, 2},
                new List<int>() {4, 5, 6},
                new List<int>() {7, 8},
            };
            var q = new List<List<int>>()
            {
                new List<int>() {1, 2, 2},
                new List<int>() {4, 5, 6},
                new List<int>() {7, 8},
            };

            Assert.IsFalse(Comparison.AreNestedListsEquivalent(p, q));
        }

        [TestMethod]
        public void Nested_Lists_Shuffler_Row_Equivalent_Test()
        {

            var p = new List<List<int>>()
            {
                new List<int>() {1, 2, 3},
                new List<int>() {4, 5, 6},
                new List<int>() {7, 8},
            };
            var q = new List<List<int>>()
            {
                new List<int>() {4, 5, 6},
                new List<int>() {1, 2, 3},
                new List<int>() {7, 8},
            };

            Assert.IsTrue(Comparison.AreNestedListsEquivalent(p, q));

        }

        [TestMethod]
        public void Nested_Lists_Shuffler_Column_Equivalent_Test()
        {
            var p = new List<List<int>>()
            {
                new List<int>() {1, 3, 2},
                new List<int>() {4, 5, 6},
                new List<int>() {7, 8},
            };

            var q = new List<List<int>>()
            {
                new List<int>() {1, 2, 3},
                new List<int>() {4, 5, 6},
                new List<int>() {7, 8},
            };

            Assert.IsTrue(Comparison.AreNestedListsEquivalent(p, q));

        }

        [TestMethod]
        public void Nested_Lists_Not_Equivalent_Test()
        {
            var p = new List<List<int>>()
            {
                new List<int>() { 1, 3, 4},
                new List<int>() { 4, 5, 6},
                new List<int>() { 7, 8},
            };

            var q = new List<List<int>>()
            {
                new List<int>() { 1, 2, 3},
                new List<int>() { 4, 5, 6},
                new List<int>() { 7, 8},
            };

            Assert.IsFalse(Comparison.AreNestedListsEquivalent(p, q));
        }
    }
}
