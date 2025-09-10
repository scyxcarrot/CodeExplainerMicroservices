using Materialise.MtlsMimicsRW.Core.Primitives;

namespace IDS.EnlightCMFIntegration.Utilities
{
    public static class Converters
    {
        public static double[] ToTransform(TransformationMatrix trans)
        {
            var transform = new double[16];
            transform[0] = trans.a11;
            transform[1] = trans.a12;
            transform[2] = trans.a13;
            transform[3] = trans.a14;

            transform[4] = trans.a21;
            transform[5] = trans.a22;
            transform[6] = trans.a23;
            transform[7] = trans.a24;

            transform[8] = trans.a31;
            transform[9] = trans.a32;
            transform[10] = trans.a33;
            transform[11] = trans.a34;

            transform[12] = trans.a41;
            transform[13] = trans.a42;
            transform[14] = trans.a43;
            transform[15] = trans.a44;

            return transform;
        }
    }
}
