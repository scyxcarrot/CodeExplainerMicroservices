using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IDS.Common.Strings;

namespace IDS.Common.Fea
{
    public static class StressFunctions
    {
        public static double StressTensorToVonMises(double[] tensor)
        {
            double Sxx = tensor[0];
            double Syy = tensor[1];
            double Szz = tensor[2];
            double Sxy = tensor[3];
            double Syz = tensor[4];
            double Szx = tensor[5];

            double VonMisesStress = 1 / 
                                    Math.Sqrt(2) * 
                                    Math.Sqrt(   Math.Pow(Sxx - Syy, 2) +
                                                 Math.Pow(Syy - Szz, 2) +
                                                 Math.Pow(Szz - Sxx, 2) +
                                                 6 * Math.Pow(Sxy, 2) +
                                                 6 * Math.Pow(Syz, 2) +
                                                 6 * Math.Pow(Szx, 2));

            return VonMisesStress;
        }
    }
}