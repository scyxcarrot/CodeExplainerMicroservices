using IDS.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class MathUtilitiesTests
    {
        [TestMethod]
        public void TestMatriceSubtract()
        {
            var arr1 = new double[][]
            {
                new double[] {1, 2},
                new double[] {3, 4},
                new double[] {6, 7}
            };

            var arr2 = new double[][]
            {
                new double[] {2, 5},
                new double[] {1, 2},
                new double[] {5, 6}
            };

            var expected = new double[][]
            {
                new double[] {-1, -3},
                new double[] {2, 2},
                new double[] {1, 1}
            };

            var res = MathUtilities.MatriceSubtract(arr1, arr2);

            for (var i = 0; i < expected.Length; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.001, "");
            }
        }

        [TestMethod]
        public void TestMatriceAdd()
        {
            var arr1 = new double[][]
            {
                new double[] {1, 2},
                new double[] {3, 4},
                new double[] {6, 7}
            };

            var arr2 = new double[][]
            {
                new double[] {2, 5},
                new double[] {1, 2},
                new double[] {5, 6}
            };

            var expected = new double[][]
            {
                new double[] {3, 7},
                new double[] {4, 6},
                new double[] {11, 13}
            };

            var res = MathUtilities.MatriceAdd(arr1, arr2);

            for (var i = 0; i < expected.Length; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void TestMatriceSqrt()
        {
            var arr1 = new double[][]
            {
                new double[] {3, 7},
                new double[] {4, 6},
                new double[] {11, 13}
            };

            var expected = new double[][]
            {
                new double[] { 1.73205081, 2.64575131} ,
                new double[] { 2.0, 2.44948974},
                new double[] { 3.31662479, 3.60555128 }
            };

            var res = MathUtilities.MatriceSqrt(arr1);

            for (var i = 0; i < expected.Length; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void TestMatriceLog()
        {
            var arr1 = new double[][]
            {
                new double[] {3, 7},
                new double[] {4, 6},
                new double[] {11, 13}
            };

            var expected = new double[][]
            {
                new double[] { 1.09861229, 1.94591015 } ,
                new double[] { 1.38629436, 1.79175947},
                new double[] { 2.39789527, 2.56494936 }
            };

            var res = MathUtilities.MatriceLog(arr1);

            for (var i = 0; i < expected.Length; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void TestMatriceMultiply()
        {
            var arr1 = new double[][]
            {
                new double[] {3, 7},
                new double[] {4, 6},
                new double[] {11, 13}
            };

            var arr2 = new double[][]
            {
                new double[] {2, 2},
                new double[] {3, 2},
                new double[] {5, 6}
            };

            var expected = new double[][]
            {
                new double[] { 6, 14 } ,
                new double[] { 12, 12},
                new double[] { 55, 78 }
            };

            var res = MathUtilities.MatriceMultiply(arr1, arr2);

            for (var i = 0; i < expected.Length; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void TestMatricePower()
        {
            var arr = new double[][]
            {
                new double[] {-1, -3},
                new double[] {2, 2},
                new double[] {1, 1}
            };

            var expected = new double[][]
            {
                new double[] {1, 9},
                new double[] {4, 4},
                new double[] {1, 1}
            };

            var res = MathUtilities.MatricePowerOf(arr, 2);

            for (var i = 0; i < expected.Length; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void TestReshape()
        {
            //Test1
            var arr1 = new double[] {7, 8, 9};

            var expected1 = new double[][]
            {
                new double[] {7},
                new double[] {8},
                new double[] {9}
            };
            var res1 = MathUtilities.ReshapeMatrice(arr1, -1,1);

            for (var i = 0; i < expected1.Length; ++i)
            {
                Comparison.AssertArrays(expected1[i], res1[i], 0.01, "");
            }

            //Test2
            var expected2 = new double[][]
            {
                arr1
            };

            var res2 = MathUtilities.ReshapeMatrice(arr1, 1, -1);

            Comparison.AssertArrays(expected2[0], res2[0], 0.01, "");
        }

        [TestMethod]
        public void TestFormMatrice()
        {
            var arr = new double[][]
            {
                new double[] {1, 2, 7},
                new double[] {3, 4, 8},
                new double[] {5, 6, 9}
            };

            var expected = new double[] { 7, 8, 9};

            var res = MathUtilities.ExtractValue(arr,2);
            Comparison.AssertArrays(expected, res, 0.0001,"");

            expected = new double[] { 2, 4, 6 };
            res = MathUtilities.ExtractValue(arr, 1);
            Comparison.AssertArrays(expected, res, 0.0001, "");
        }

        [TestMethod]
        public void TestMatriceTiling1D()
        {
            var arr = new double[][]
            {
                new double[] {1, 2},
                new double[] {3, 4},
                new double[] {5, 6}
            };

            var expected = new double[][]
            {
                new double[] {1, 2, 1, 2, 1, 2},
                new double[] {3, 4, 3, 4, 3, 4},
                new double[] {5, 6, 5, 6, 5, 6}
            };

            var res = MathUtilities.MatriceTile(arr, 3);

            for (var i = 0; i < expected.Length; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void TestMatriceTiling2D()
        {
            var arr = new double[][]
            {
                new double[] {1, 2},
                new double[] {3, 4},
                new double[] {5, 6}
            };

            var expected = new double[][]
            {
                new double[] {1, 2, 1, 2, 1, 2},
                new double[] {3, 4, 3, 4, 3, 4},
                new double[] {5, 6, 5, 6, 5, 6},
                new double[] {1, 2, 1, 2, 1, 2},
                new double[] {3, 4, 3, 4, 3, 4},
                new double[] {5, 6, 5, 6, 5, 6},
                new double[] {1, 2, 1, 2, 1, 2},
                new double[] {3, 4, 3, 4, 3, 4},
                new double[] {5, 6, 5, 6, 5, 6},
                new double[] {1, 2, 1, 2, 1, 2},
                new double[] {3, 4, 3, 4, 3, 4},
                new double[] {5, 6, 5, 6, 5, 6}
            };

            var res = MathUtilities.MatriceTile(arr, 4, 3);

            for (var i = 0; i < expected.Length; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }

            expected = new double[][]
            {
                new double[] {1, 2, 1, 2, 1, 2},
                new double[] {3, 4, 3, 4, 3, 4},
                new double[] {5, 6, 5, 6, 5, 6}
            };

            res = MathUtilities.MatriceTile(arr, 1, 3);

            for (var i = 0; i < expected.Length; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void TestPseudoInverseMatrice()
        {
            var arr = new double[][]
                {
                    new double[] {1, 2},
                    new double[] {4, 2},
                    new double[] {6, 4}
                };

            var expected = new double[][]
                {
                    new double[] { -0.37931034, 0.24137931, 0.06896552},
                    new double[] { 0.62068966, -0.25862069,  0.06896552}
                };

            var res = MathUtilities.PInvMatrice(arr);

            for (var i = 0; i < expected.Length; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void TestUniformToJaggedMatrice()
        {
            var arr = new double[3, 2]
            {
                {1, 2},
                {4, 2},
                {6, 4},
            };

            var res = MathUtilities.ToJaggedMatrice(arr);

            var expected = new double[][]
            {
                new double[] {1, 2},
                new double[] {4, 2},
                new double[] {6, 4},
            };

            for (var i = 0; i < expected.Length; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void TestJaggedToUniformMatrice()
        {
            var arr = new double[][]
            {
                new double[] {1, 2},
                new double[] {4, 2},
                new double[] {6, 4},
            };

            var res = MathUtilities.ToUniformMatrice(arr);

            var expected = new double[3,2]
            {
                {1, 2},
                {4, 2},
                {6, 4},
            };

            var equal = MathUtilities.TwoDArrayIsEqual(res, expected);

            Assert.IsTrue(equal);
        }

        public void TestTwoDArrayEqualityCheck()
        {
            var arr1 = new double[3, 2]
            {
                {1, 2},
                {4, 2},
                {6, 4},
            };

            var arr2 = new double[3, 2]
            {
                {1, 2},
                {4, 2},
                {6, 4},
            };

            var equal = MathUtilities.TwoDArrayIsEqual(arr1, arr2);

            Assert.IsTrue(equal);
        }

        [TestMethod]
        public void MatriceTranspose()
        {
            var arr = new double[][]
            {
                new double[] {1, 2, 3},
                new double[] {4, 2, 3},
                new double[] {6, 4, 3},
                new double[] {1, 9, 3},
                new double[] {3, 5, 4},
                new double[] {4, 4, 2},
                new double[] {2, 3, 4},
            };

            var expected = new double[][]
            {
                new double[] {1, 4, 6, 1, 3, 4, 2},
                new double[] {2, 2, 4, 9, 5, 4, 3},
                new double[] {3, 3, 3, 3, 4, 2, 4}
            };

            var res = MathUtilities.ArrayTranspose(arr);

            for (var i = 0; i < expected.Length; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void MatriceSetValueUsingMask()
        {
            var arr = new double[][]
            {
                new double[] {36, 16, 20},
                new double[] {0, 12, 16},
                new double[] {26, 12, 0}
            };

            var mask = new bool[][]
            {
                new bool[] {false, false, false},
                new bool[] {true, false, false},
                new bool[] { false, false, true }
            };

            var res = MathUtilities.SetValueInMatriceUsingMask(arr, mask, 1);

            var expected = new double[][]
            {
                new double[] {36, 16, 20},
                new double[] {1, 12, 16},
                new double[] {26, 12, 1}
            };

            Comparison.AssertArrays(expected[0], res[0], 0.01, "");
            Comparison.AssertArrays(expected[1], res[1], 0.01, "");
            Comparison.AssertArrays(expected[2], res[2], 0.01, "");
        }

        [TestMethod]
        public void MatriceDotProduct()
        {
            var arr1 = new double[] { 1,2,3,4,5};
            var arr2 = new double[] {2, 3, 4, 5, 6};

            var res = MathUtilities.DotProduct(arr1, arr2);
            Assert.AreEqual(70, res, 0.001);
        }

        [TestMethod]
        public void TwoDMatriceDotProduct()
        {
            var arr1 = new double[][]
            {
                new double[] {3,2,3},
                new double[] {1,2,3},
                new double[] {2,2,2}
            };
            var arr2 = new double[][]
            {
                new double[] {2,2,2},
                new double[] {3,2,4},
                new double[] {8,2,2}
            };

            var expected = new double[][]
            {
                new double[] {36, 16, 20},
                new double[] {32, 12, 16},
                new double[] {26, 12, 16}
            };

            var res = MathUtilities.MatriceDotProduct(arr1, arr2);

            for (var i = 0; i < 3; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void OnesMatrice()
        {
            var res = MathUtilities.CreateOnesMatrix(4, 3);

            var expected = new double[][]
            {
                new double[] { 1, 1, 1 },
                new double[] { 1, 1, 1 },
                new double[] { 1, 1, 1 },
                new double[] { 1, 1, 1 },
            };

            for (var i = 0; i < 4; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }

            res = MathUtilities.CreateOnesMatrix(1, 3);

            expected = new double[][]
            {
                new double[] { 1, 1, 1 },
            };

            Comparison.AssertArrays(expected[0], res[0], 0.01, "");

            res = MathUtilities.CreateOnesMatrix(6, 1);

            expected = new double[][]
            {
                new double[] { 1},
                new double[] { 1},
                new double[] { 1},
                new double[] { 1},
                new double[] { 1},
                new double[] { 1},
            };

            for (var i = 0; i < 6; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void ZerosMatrice()
        {
            var res = MathUtilities.CreateZerosMatrix(4, 3);

            var expected = new double[][]
            {
                new double[] { 0, 0, 0 },
                new double[] { 0, 0, 0 },
                new double[] { 0, 0, 0 },
                new double[] { 0, 0, 0 },
            };

            for (var i = 0; i < 4; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }

            res = MathUtilities.CreateZerosMatrix(1, 3);

            expected = new double[][]
            {
                new double[] { 0, 0, 0 },
            };

            Comparison.AssertArrays(expected[0], res[0], 0.01, "");
        }

        [TestMethod]
        public void ValuedMatrice()
        {
            var res = MathUtilities.CreateValuedMatrix(4, 3,2);

            var expected = new double[][]
            {
                new double[] { 2, 2, 2 },
                new double[] { 2, 2, 2 },
                new double[] { 2, 2, 2 },
                new double[] { 2, 2, 2 },
            };

            for (var i = 0; i < 4; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void VerticalStackArray()
        {
            var arr1 = new double[][]
            {
                new double[] {1,2,3}, new double[] {4,5,6}, new double[] {7,8,9}
            };
            var arr2 = new double[][]
            {
                new double[] {10,11,12}, new double[] {13,14,15}, new double[] {16,17,18}
            };

            var res = MathUtilities.ArrayVStack(new List<double[][]> {arr1, arr2});

            var expected = new double[][]
            {
                new double[] {1,2,3}, new double[] {4,5,6}, new double[] {7,8,9}, new double[] {10,11,12}, new double[] {13,14,15}, new double[] {16,17,18}
            };

            for (var i = 0; i < expected.Length; ++i)
            {
                Comparison.AssertArrays(expected[i], res[i], 0.01, "");
            }
        }

        [TestMethod]
        public void HorizontalStackArray()
        {
            const int size = 3;
            var arr1 = new double[size][] { new double[] {10,11,12}, new double[] { 13, 14, 15 }, new double[] { 16, 17, 18 }};
            var arr2 = new double[size][] { new double[] { 1, 2, 3 }, new double[] { 4, 5, 6 }, new double[] { 7, 8, 9 }};
            var arr3 = new double[size][] { new double[] { 2 }, new double[] { 2 }, new double[] { 2 } };
            var arr4 = new double[size][] { new double[] { 1 }, new double[] { 1 }, new double[] { 1 } };

            var res = MathUtilities.ArrayHStack(new List<double[][]>{ arr1, arr2, arr3, arr4 });
            const int resExpectedElementLength = 8;

            Assert.AreEqual(res.Length, size);
            res.ToList().ForEach(x => Assert.AreEqual(x.Length, resExpectedElementLength));

            var expected = new double[size][]
            {
                new double[resExpectedElementLength] { 10, 11, 12, 1, 2, 3, 2, 1 },
                new double[resExpectedElementLength] { 13, 14, 15, 4, 5, 6, 2, 1 },
                new double[resExpectedElementLength] { 16, 17,18, 7, 8, 9, 2, 1 }
            };

            Comparison.AssertArrays(expected[0], res[0], 0.01, "#1 First HStacked Element");
            Comparison.AssertArrays(expected[1], res[1], 0.01, "#1 Second HStacked Element");
            Comparison.AssertArrays(expected[2], res[2], 0.01, "#1 Third HStacked Element");

            arr1 = new double[size][] { new double[] { 10, 11 }, new double[] { 13, 14 }, new double[] { 16, 17 } };
            arr2 = new double[size][] { new double[] { 1, 2 }, new double[] { 4, 5 }, new double[] { 7, 8 } };
            arr3 = new double[size][] { new double[] { 2 }, new double[] { 2 }, new double[] { 2 } };
            arr4 = new double[size][] { new double[] { 1 }, new double[] { 1 }, new double[] { 1 } };

            res = MathUtilities.ArrayHStack(new List<double[][]> { arr1, arr2, arr3, arr4 });
            const int newResExpectedElementLength = 6;

            Assert.AreEqual(res.Length, size);
            res.ToList().ForEach(x => Assert.AreEqual(x.Length, newResExpectedElementLength));

            expected = new double[size][]
            {
                new double[newResExpectedElementLength] { 10, 11, 1, 2, 2, 1 },
                new double[newResExpectedElementLength] { 13, 14, 4, 5, 2, 1 },
                new double[newResExpectedElementLength] { 16, 17, 7, 8, 2, 1 }
            };

            Comparison.AssertArrays(expected[0], res[0], 0.01, "#2 First HStacked Element");
            Comparison.AssertArrays(expected[1], res[1], 0.01, "#2 Second HStacked Element");
            Comparison.AssertArrays(expected[2], res[2], 0.01, "#2 Third HStacked Element");
        }

        [TestMethod]
        public void HorizontalStackArrayInConsistentSizeExceptionTest()
        {
            const int size = 3;
            var arr1 = new double[2][] { new double[] { 10, 11, 12 }, new double[] { 13, 14, 15 } };
            var arr2 = new double[size][] { new double[] { 1, 2, 3 }, new double[] { 4, 5, 6 }, new double[] { 7, 8, 9 } };
            var arr3 = new double[size][] { new double[] { 2 }, new double[] { 2 }, new double[] { 2 } };
            var arr4 = new double[size][] { new double[] { 1 }, new double[] { 1 }, new double[] { 1 } };

            var exceptionCought = false;
            try
            {
                MathUtilities.ArrayHStack(new List<double[][]> { arr1, arr2, arr3, arr4 });
            }
            catch (System.Exception ex)
            {
                exceptionCought = true;
            }

            Assert.IsTrue(exceptionCought);
        }

        [TestMethod]
        public void HorizontalStackArrayInConsistentInnerSizeExceptionTest()
        {
            const int size = 3;
            var arr1 = new double[size][] { new double[] { 10, 11, 12 }, new double[] { 13, 14 }, new double[] { 16, 17, 18 } };
            var arr2 = new double[size][] { new double[] { 1, 2, 3 }, new double[] { 4, 5, 6 }, new double[] { 7, 8, 9 } };
            var arr3 = new double[size][] { new double[] { 2 }, new double[] { 2 }, new double[] { 2 } };
            var arr4 = new double[size][] { new double[] { 1 }, new double[] { 1 }, new double[] { 1 } };

            var exceptionCought = false;
            try
            {
                MathUtilities.ArrayHStack(new List<double[][]> { arr1, arr2, arr3, arr4 });
            }
            catch (System.Exception ex)
            {
                exceptionCought = true;
            }

            Assert.IsTrue(exceptionCought);
        }

        [TestMethod]
        public void RangeInt()
        {
            var expectedRange1 = new[] { -5, -4, -3, -2, -1, 0, 1, 2, 3 };
            var actualRange1 = MathUtilities.Range(-5, 3, 1).ToArray();
            Comparison.AssertArrays(expectedRange1, actualRange1, "Int 1");

            var expectedRange2 = new[] { -5, -3, -1, 1, 3 };
            var actualRange2 = MathUtilities.Range(-5, 3, 2).ToArray();
            Comparison.AssertArrays(expectedRange2, actualRange2, "Int 2");
        }

        [TestMethod]
        public void RangeDouble()
        {
            var expectedRange1 = new[] { -5.3, -4.3, -3.3, -2.3, -1.3, -0.3, 0.7, 1.7, 2.7 };
            var actualRange1 = MathUtilities.Range(-5.3, 3.3, 1.0).ToArray();
            Comparison.AssertArrays(expectedRange1, actualRange1, 0.01, "Double 1");

            var expectedRange2 = new[] { -5.3, -3.65, -2.0, -0.35, 1.3, 2.95 };
            var actualRange2 = MathUtilities.Range(-5.3, 3, 1.65).ToArray();
            Comparison.AssertArrays(expectedRange2, actualRange2, 0.01, "Double 2");
        }

        [TestMethod]
        public void CapValue()
        {
            const double min = -1.4321;
            const double max = 3.5345;

            var inputValues = new[] { -60.0, -1.4322, -1.4321, -1.4320, 2.352, 3.5344, 3.5345, 3.5346, 42.13 };
            var expectedValues = new[] { -1.4321, -1.4321, -1.4321, -1.4320, 2.352, 3.5344, 3.5345, 3.5345, 3.5345 };

            for(var i = 0; i < inputValues.Length; i++)
            {
                var actualValue = MathUtilities.CapValue(inputValues[i], min, max);
                Assert.AreEqual(expectedValues[i], actualValue);
            }
        }
    }
}
