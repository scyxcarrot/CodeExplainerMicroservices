using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing
{
    public static class Comparison
    {
        public static void AssertArrays(int[] expectedArray, int[] actualArray, string message)
        {
            for (var j = 0; j < expectedArray.Length; j++)
            {
                Assert.AreEqual(expectedArray[j], actualArray[j], message);
            }
        }

        public static void AssertArrays(double[] expectedArray, double[] actualArray, double delta, string message)
        {
            for (var j = 0; j < expectedArray.Length; j++)
            {
                Assert.AreEqual(expectedArray[j], actualArray[j], delta, message);
            }
        }

        public static void Assert2dArrays(double[][] expectedArrays, List<double[]> actualArrays, double delta, string message)
        {
            Assert2dArrays(expectedArrays, actualArrays.ToArray(), delta, message);
        }

        public static void Assert2dArrays(double[][] expectedArrays, double[][] actualArrays, double delta, string message)
        {
            for (var i = 0; i < expectedArrays.Length; i++)
            {
                AssertArrays(expectedArrays[i], actualArrays[i], delta, message);
                
            }
        }

        public static bool AreNestedListsEquivalent<T>(List<List<T>> nestedLists1, List<List<T>> nestedLists2)
        {
            if (nestedLists1.Count != nestedLists2.Count)
            {
                return false;
            }

            foreach (var lists1 in nestedLists1)
            {
                var equivalent = false;

                foreach (var lists2 in nestedLists2)
                {
                    if (lists1.Count == lists2.Count)
                    {
                        var list2Copy = lists2.ToList();
                        foreach (var item1 in lists1)
                        {
                            if (list2Copy.Contains(item1))
                            {
                                // Remove will only remove an item in the list even it have multiple similar item
                                list2Copy.Remove(item1);
                                continue;
                            }
                            break;
                        }

                        if (!list2Copy.Any())
                        {
                            equivalent = true;
                            break;
                        }
                    }
                }
                
                if (!equivalent)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
