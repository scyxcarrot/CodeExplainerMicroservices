using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.Fea
{
    public class StressTensor
    {
        public double Sxx { get; }
        public double Syy { get; }
        public double Szz { get; }
        public double Sxy { get; }
        public double Syz { get; }
        public double Szx { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StressTensor"/> class.
        /// </summary>
        /// <param name="sxx">The Sxx value</param>
        /// <param name="syy">The Syy value</param>
        /// <param name="szz">The Szz value</param>
        /// <param name="sxy">The sxy value</param>
        /// <param name="syz">The syz value</param>
        /// <param name="szx">The Szx value</param>
        public StressTensor(double sxx, double syy, double szz, double sxy, double syz, double szx)
        {
            Sxx = sxx;
            Syy = syy;
            Szz = szz;
            Sxy = sxy;
            Syz = syz;
            Szx = szx;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StressTensor"/> class.
        /// </summary>
        /// <param name="sarray">The sarray.</param>
        public StressTensor(double[] sarray)
        {
            Sxx = sarray[0];
            Syy = sarray[1];
            Szz = sarray[2];
            Sxy = sarray[3];
            Syz = sarray[4];
            Szx = sarray[5];
        }

        public double[] ToArray()
        {
            return new [] { Sxx, Syy, Szz, Sxy, Syz, Szx };
        }

        public double vonMisesStress => Math.Sqrt( ( Math.Pow(Sxx - Syy, 2) 
                                                     + Math.Pow(Syy - Szz, 2) 
                                                     + Math.Pow(Szz - Sxx, 2) 
                                                     + 6 * (Math.Pow(Sxy, 2) + Math.Pow(Syz, 2) + Math.Pow(Szx, 2))
                                                   ) / 2 
        );

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            var otherTensor = obj as StressTensor;
            if ((System.Object)otherTensor == null)
            {
                return false;
            }

            const double epsilon = 0.00001;

            // Return true if the fields match:
            return  Math.Abs(Sxx - otherTensor.Sxx) < epsilon &&
                    Math.Abs(Syy - otherTensor.Syy) < epsilon &&
                    Math.Abs(Szz - otherTensor.Szz) < epsilon &&
                    Math.Abs(Sxy - otherTensor.Sxy) < epsilon &&
                    Math.Abs(Syz - otherTensor.Syz) < epsilon &&
                    Math.Abs(Szx - otherTensor.Szx) < epsilon;
        }

        /// <summary>
        /// Converts the list of stress tensors to list of double arrays.
        /// </summary>
        /// <param name="stressTensors">The stress tensors.</param>
        /// <returns></returns>
        public static List<double[]> ConvertListOfStressTensorsToListOfDoubleArrays(List<StressTensor> stressTensors)
        {
            return stressTensors.Select(stressTensor => stressTensor.ToArray()).ToList();
        }

        /// <summary>
        /// Converts the list of double arrays to list of stress tensors.
        /// </summary>
        /// <param name="doubleArrays">The double arrays.</param>
        /// <returns></returns>
        public static List<StressTensor> ConvertListOfDoubleArraysToListOfStressTensors(List<double[]> doubleArrays)
        {
            return doubleArrays.Select(doubleArray => new StressTensor(doubleArray)).ToList();
        }
    }
}
