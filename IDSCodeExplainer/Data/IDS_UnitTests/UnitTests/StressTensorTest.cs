using IDS.Core.Fea;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class StressTensorTests
    {
        [TestMethod]
        public void StressTensorVonMises()
        {
            var stressTensorArrays = new double[][]{ new double[] { 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 },
                                                            new double[] { 0.5, 0.6, 0.7, 0.2, 0.3, 0.4 },
                                                            new double[] { 0.2, 0.3, 0.4, 0.5, 0.6, 0.7 },
                                                            new double[] { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6 } };

            var expectedVonMises = new double[] {  2.418677324,
                                                        0.948683298,
                                                        1.824828759,
                                                        1.529705854 };

            for(var i = 0; i < stressTensorArrays.Length; i++)
            {
                var stressTensorArray = stressTensorArrays[i];
                var tensor = new StressTensor(stressTensorArray[0], stressTensorArray[1], stressTensorArray[2], stressTensorArray[3], stressTensorArray[4], stressTensorArray[5]);
                Assert.AreEqual(expectedVonMises[i], tensor.vonMisesStress, 1e-9);
            }
        }

        [TestMethod]
        public void StressTensorEquality()
        {
            var referenceStressTensor = new StressTensor(0.4, 0.5, 0.6, 0.7, 0.8, 0.9);
            var equalStressTensor = new StressTensor(0.4, 0.5, 0.6, 0.7, 0.8, 0.9);
            var notEqualStressTensors = new StressTensor[] { new StressTensor( 0.41, 0.5, 0.6, 0.7, 0.8, 0.9) ,
                                                                        new StressTensor( 0.4, 0.49999, 0.6, 0.7, 0.8, 0.9) ,
                                                                        new StressTensor( 0.4, 0.5, 0.60001, 0.7, 0.8, 0.9) ,
                                                                        new StressTensor( 0.4, 0.5, 0.6, 0.701, 0.8, 0.9) ,
                                                                        new StressTensor( 0.4, 0.5, 0.6, 0.7, 0.69999, 0.9) ,
                                                                        new StressTensor( 0.4, 0.5, 0.6, 0.7, 0.8, 0.91) };

            Assert.AreEqual(referenceStressTensor, equalStressTensor);
            foreach(var notEqualTensor in notEqualStressTensors)
            {
                Assert.AreNotEqual(referenceStressTensor, notEqualTensor);
            }
        }
    }
}
