using IDS.Core.Fea;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [DeploymentItem(@"UnitTestData", @"UnitTestData")]
    [TestClass]
    public class FrdTests
    {
        [TestMethod]
        public void FatigueCalculation()
        {
            // This depends on a manual calculation using the current Titanium material parameters
            // \todo adjust the values once this is final



            var stressValues = new double[] { 0, 100, 500, 567, 1234, 25789, 35896, 41230, 49999, 50000 };
            var expectedFatigue = new double[] { 1000000, 2.438380096, 0.48767697, 0.430050263, 0.197600126, 0.00945514, 0.006792919, 0.005914106, 0.00487687, 0.004876772 };

            for(var i = 0; i<stressValues.Length; i++)
            {
                Assert.AreEqual(expectedFatigue[i], Frd.CalculateFatigue(stressValues[i], Materials.Titanium),1e-8);
            }
        }

        /// <summary>
        /// Verify that the stress tensor arrays are correctly assigned to the StressTensor objects
        /// </summary>
        [TestMethod]
        public void FrdStressTensorRead()
        {
            const double sxx = -2.09662E+01;
            const double syy = -4.35665E+00;
            const double szz = -2.36132E-01;
            const double sxy = 4.87878E-01;
            const double syz = -5.90040E-01;
            const double szx = 1.35111E-01;

            var resources = new TestResources();
            var frd = new Frd(resources.FrdInputFile);

            Assert.AreEqual(sxx, frd.StressTensors[0].Sxx,6);
            Assert.AreEqual(syy, frd.StressTensors[0].Syy, 6);
            Assert.AreEqual(szz, frd.StressTensors[0].Szz, 6);
            Assert.AreEqual(sxy, frd.StressTensors[0].Sxy, 6);
            Assert.AreEqual(syz, frd.StressTensors[0].Syz, 6);
            Assert.AreEqual(szx, frd.StressTensors[0].Szx, 6);
        }

        /// <summary>
        /// Read a sample FRD file and verify that the first 10 elements of some arrays are correct
        /// </summary>
        [TestMethod]
        public void FrdReadTest()
        {
            var expectedStressTensors = new StressTensor[] {new StressTensor( -2.09662E+01, -4.35665E+00, -2.36132E-01,  4.87878E-01, -5.90040E-01,  1.35111E-01),
                                                                        new StressTensor( -2.09505E+01, -4.47408E+00, -3.74445E-01,  1.70522E-01,  5.12647E-01,  1.65260E-01),
                                                                        new StressTensor( -5.86621E-01, -4.16075E-01,  7.85153E-01, -8.30902E-01,  4.70649E-02, -5.02307E-01),
                                                                        new StressTensor( -1.39674E+00, -3.87411E-01,  3.25243E-01, -1.16808E+00, -1.74127E-01,  3.70153E-01),
                                                                        new StressTensor(  2.07648E-01, -6.34336E+00, -1.69377E+00, -2.49148E+00,  4.09448E-01, -9.12907E-01),
                                                                        new StressTensor( -7.01415E-01, -5.71014E+00, -2.12271E+00, -1.75534E+00, -3.67738E-01,  9.58942E-01),
                                                                        new StressTensor( -1.37551E-01, -6.16724E-01,  6.59862E-01,  5.31573E-01,  2.99328E-01, -4.67824E-01),
                                                                        new StressTensor( -2.77066E-01, -7.44172E-01,  5.16820E-01,  5.51864E-01, -2.53298E-02,  3.87881E-01),
                                                                        new StressTensor(  2.17460E-01, -4.92524E+00, -1.67098E+00,  2.12278E+00, -6.04313E-01, -1.03341E+00),
                                                                        new StressTensor(  6.02003E-01, -4.34296E+00, -1.38619E+00,  1.81046E+00,  3.89376E-01,  9.79367E-01)};

            var expectedStrains = new double[][] {   new double[]{ -1.19557E-02,  2.57769E-03, 6.18314E-03,  4.26893E-04, -5.16285E-04,  1.18222E-04 },
                                                            new double[]{ -1.18819E-02,  2.53494E-03, 6.12212E-03,  1.49207E-04,  4.48566E-04,  1.44602E-04 },
                                                            new double[]{ -4.58908E-04, -3.09680E-04, 7.41395E-04, -7.27039E-04,  4.11818E-05, -4.39518E-04 },
                                                            new double[]{ -8.57422E-04,  2.57429E-05, 6.49315E-04, -1.02207E-03, -1.52361E-04,  3.23884E-04 },
                                                            new double[]{  2.13906E-03, -3.59307E-03, 4.75320E-04, -2.18005E-03,  3.58267E-04, -7.98794E-04 },
                                                            new double[]{  1.51983E-03, -2.86280E-03, 2.76192E-04, -1.53593E-03, -3.21771E-04,  8.39075E-04 },
                                                            new double[]{ -9.67538E-05, -5.16030E-04, 6.00983E-04,  4.65126E-04,  2.61912E-04, -4.09346E-04 },
                                                            new double[]{ -1.16328E-04, -5.25046E-04, 5.78322E-04,  4.82881E-04, -2.21636E-05,  3.39396E-04 },
                                                            new double[]{  1.78497E-03, -2.71490E-03, 1.32585E-04,  1.85743E-03, -5.28774E-04, -9.04236E-04 },
                                                            new double[]{  1.80854E-03, -2.51830E-03, 6.88685E-05,  1.58416E-03,  3.40704E-04,  8.56947E-04 } };

            var expectedDisplacements = new double[][] { new double[]{  1.66311E-01, -1.17454E+00,  4.86474E-02 },
                                                                new double[]{  1.65000E-01, -1.17445E+00, -4.84612E-02 },
                                                                new double[]{  3.30263E-01, -9.96178E-03, -6.57861E-03 },
                                                                new double[]{  3.30602E-01, -1.01679E-02,  6.64992E-03 },
                                                                new double[]{  0.00000E+00,  0.00000E+00,  0.00000E+00 },
                                                                new double[]{  0.00000E+00,  0.00000E+00,  0.00000E+00 },
                                                                new double[]{ -3.68017E-04, -1.09468E-02,  7.36308E-03 },
                                                                new double[]{ -5.12510E-04, -1.13874E-02, -6.79640E-03 },
                                                                new double[]{  3.30871E-01,  0.00000E+00,  0.00000E+00 },
                                                                new double[]{  3.31823E-01,  0.00000E+00,  0.00000E+00 }};

            const double expectedNumberOfStressTensors = 887;
            const double expectedNumberOfStrainArrays = 887;
            const double expectedNumberOfDisplacementArrays = 887;

            var resources = new TestResources();

            var frd = new Frd(resources.FrdInputFile);

            Assert.AreEqual(expectedNumberOfStressTensors, frd.StressTensors.Count);
            Assert.AreEqual(expectedNumberOfStrainArrays, frd.Strains.Count);
            Assert.AreEqual(expectedNumberOfDisplacementArrays, frd.Displacements.Count);

            CompareStressTensorArrays(expectedStressTensors, frd.StressTensors);
            Comparison.Assert2dArrays(expectedStrains, frd.Strains, 0, "Strain");
            Comparison.Assert2dArrays(expectedDisplacements, frd.Displacements, 0, "Displacement");
        }

        private static void CompareStressTensorArrays(StressTensor[] expectedStressTensors, List<StressTensor> actualStressTensors)
        {
            CompareStressTensorArrays(expectedStressTensors, actualStressTensors.ToArray());
        }

        private static void CompareStressTensorArrays(StressTensor[] expectedStressTensors, StressTensor[] actualStressTensors)
        {
            for (var i = 0; i < expectedStressTensors.Length; i++)
            {
                Assert.AreEqual(expectedStressTensors[i], actualStressTensors[i], $"Stress Tensor {i:D}");
            }
        }

        private static string CleanString(string inputString)
        {
            return inputString.ToLower().Trim().Replace(" ", string.Empty);
        }
    }
}
