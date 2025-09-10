using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.Operations
{
    public static class MakeScaffoldBottom
    {
        public static Mesh TpsTransformRayIntersections(Mesh filler, int[] hits, Point3d[] vcorr)
        {
            var vo = filler.Vertices.ToPoint3dArray();
            var fo = filler.Faces.ToArray();

            Point3d[] vTps, tpsPar;
            Tps(vo, hits.Select(idx => vo[idx]).ToArray(), vcorr, out vTps, out tpsPar);

            var deformed = new Mesh();
            vTps.ToList().ForEach(v => deformed.Vertices.Add(v));
            fo.ToList().ForEach(f => deformed.Faces.AddFace(f));

            deformed.Normals.ComputeNormals();
            deformed.Compact();
            return deformed;
        }

        public static void Tps(Point3d[] v, Point3d[] lm1, Point3d[] lm2, out Point3d[] vTps, out Point3d[] tpsPar, Point3d[] inTpsPar = null)
        {
            vTps = null;

            tpsPar = inTpsPar ?? new Point3d[] {};
            var lmFrom = lm1;

            if (lm1.Length != 0 && lm2.Length != 0 && tpsPar.Length == 0)
            {
                TPS_transform(lm1, lm2, out tpsPar, out lmFrom);
            }
            else if (tpsPar.Length == 0)
            {
                throw new IDSException("No landmarks were found");
            }

            //Inter-point distance matrix from V to LM_from
            var ipDist = IPDM(v, lmFrom, 2);

            //r^2 log(r^2)
            var zeroEls = ipDist.Select(i => i.Select(j => Math.Abs(j) < 0.0001).ToArray()).ToArray(); // Create Mask

            ipDist = MathUtilities.SetValueInMatriceUsingMask(ipDist, zeroEls, 1); // Make sure all Zeroes are 1 based on the Mask
            ipDist = DoMagic(ipDist);
            ipDist = MathUtilities.SetValueInMatriceUsingMask(ipDist, zeroEls, 0); // Make sure all previous Ones are Zero based on Mask

            //transform V using TPS parameters
            var ones = MathUtilities.CreateOnesMatrix(v.Length, 1);
            var hstacked = MathUtilities.ArrayHStack
                (new List<double[][]> {ipDist, ones, MathUtilities.ConvertPoint3DArrayToDoubleArray(v) });

            var vtpsRes = MathUtilities.MatriceDotProduct(hstacked, MathUtilities.ConvertPoint3DArrayToDoubleArray(tpsPar));
            vTps = MathUtilities.ConvertDoubleArrayToPoint3D(vtpsRes);
        }

        private static double[][] DoMagic(double[][] subject)
        {
            var a = MathUtilities.MatricePowerOf(subject, 2);
            var b = MathUtilities.MatriceLog(a);
            return MathUtilities.MatriceMultiply(a, b);
        }

        public static void TPS_transform(Point3d[] lmFrom, Point3d[] lmTo, out Point3d[] tpsPar, out Point3d[] newLmFrom)
        {
            var ipDist = IPDM(lmFrom, 2);

            //r^2 log(r^2)
            var zeroEls = ipDist.Select(i => i.Select(j => Math.Abs(j) < 0.0001).ToArray()).ToArray(); // Create Mask

            ipDist = MathUtilities.SetValueInMatriceUsingMask(ipDist, zeroEls, 1); // Make sure all Zeroes are 1 based on the Mask
            ipDist = DoMagic(ipDist);
            ipDist = MathUtilities.SetValueInMatriceUsingMask(ipDist, zeroEls, 0); // Make sure all previous Ones are Zero based on Mask

            // Determine parameters of TPS transformation
            // Construct the TPS matrix
            var ones = MathUtilities.CreateOnesMatrix(lmFrom.Length, 1);
            var lmFromDoubles = MathUtilities.ConvertPoint3DArrayToDoubleArray(lmFrom);
            var topPart = MathUtilities.ArrayHStack
                (new List<double[][]> { ipDist, ones, lmFromDoubles });
            var botPartL = MathUtilities.ArrayVStack(new List<double[][]>
            {
                MathUtilities.CreateOnesMatrix(1, lmFrom.Length),
                MathUtilities.ArrayTranspose(MathUtilities.ConvertPoint3DArrayToDoubleArray(lmFrom))
            });
            var botZeroes = MathUtilities.CreateZerosMatrix(lmFromDoubles[0].Length + 1, lmFromDoubles[0].Length + 1);
            var botPart = MathUtilities.ArrayHStack(new List<double[][]> { botPartL, botZeroes });
            var a = MathUtilities.ArrayVStack(new List<double[][]>{ topPart, botPart});

            var lmToDoubles = MathUtilities.ConvertPoint3DArrayToDoubleArray(lmTo);

            var rhsZeroes = MathUtilities.CreateZerosMatrix(lmFromDoubles[0].Length + 1, lmFromDoubles[0].Length);
            var rhs = MathUtilities.ArrayVStack(new List<double[][]> { lmToDoubles, rhsZeroes });
            var tpsParDoubles = MathUtilities.MatriceDotProduct(MathUtilities.PInvMatrice(a), rhs);

            tpsPar = MathUtilities.ConvertDoubleArrayToPoint3D(tpsParDoubles);
            newLmFrom = MathUtilities.ConvertDoubleArrayToPoint3D(lmFromDoubles);
        }

        public static double[][] IPDM(Point3d[] set1, int metric)
        {
            return IPDM(set1, set1, metric);
        }

        public static double[][] IPDM(Point3d[] set1, Point3d[] set2, int metric)
        {
            if (metric != 2)
            {
                throw new IDSException("Only the euclidean distance (2-norm) is supported!");
            }

            const int dim = 3; //Because Point3d length is 3 --> (X,Y,Z)
            var n1 = set1.Length;
            var n2 = set2.Length;

            var set1Doubles = MathUtilities.ConvertPoint3DArrayToDoubleArray(set1);
            var set2Doubles = MathUtilities.ConvertPoint3DArrayToDoubleArray(set2);

            //d = (np.tile(set1[:,0].reshape((-1,1)), [1,n2]) - np.tile(set2[:,0].reshape((1,-1)), [n1,1]))**2
            var da1 = MathUtilities.ExtractValue(set1Doubles, 0);
            var da2 = MathUtilities.ReshapeMatrice(da1, -1, 1);
            var da = MathUtilities.MatriceTile(da2, 1, n2);

            var db1 = MathUtilities.ExtractValue(set2Doubles, 0);
            var db2 = MathUtilities.ReshapeMatrice(db1, 1, -1);
            var db = MathUtilities.MatriceTile(db2, n1, 1);

            var ds = MathUtilities.MatriceSubtract(da, db);
            var d = MathUtilities.MatricePowerOf(ds, 2);

            for (var i = 1; i < dim; ++i)
            {
                //d = d + (np.tile(set1[:,i].reshape((-1,1)), [1,n2]) - np.tile(set2[:,i].reshape((1,-1)), [n1,1]))**2
                var newda1 = MathUtilities.ExtractValue(set1Doubles, i);
                var newda2 = MathUtilities.ReshapeMatrice(newda1, -1, 1);
                var newda = MathUtilities.MatriceTile(newda2, 1, n2);

                var newdb1 = MathUtilities.ExtractValue(set2Doubles, i);
                var newdb2 = MathUtilities.ReshapeMatrice(newdb1, 1, -1);
                var newdb = MathUtilities.MatriceTile(newdb2, n1, 1);

                var newds = MathUtilities.MatriceSubtract(newda, newdb);
                var newdAdd = MathUtilities.MatricePowerOf(newds, 2);

                d = MathUtilities.MatriceAdd(d, newdAdd);
            }

            return metric == 2 ? MathUtilities.MatriceSqrt(d) : d;
        }
    }

}
