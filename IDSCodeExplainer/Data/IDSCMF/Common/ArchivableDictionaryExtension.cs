using Rhino.Collections;

namespace IDS.CMF.Common
{
    public class Point3dListForArchivableDict
    {
        public readonly double[,] Points3D;

        public Point3dListForArchivableDict(double[,] points3D)
        {
            Points3D = points3D;
        }

        public Point3dListForArchivableDict(double[] points1D)
        {
            Points3D = new double[points1D.GetLength(0) / 3, 3];

            for (var i = 0; i < Points3D.GetLength(0); i++)
            {
                var j = i * 3;
                Points3D[i, 0] = points1D[j];
                Points3D[i, 1] = points1D[j + 1];
                Points3D[i, 2] = points1D[j + 2];
            }
        }

        public double[] ToDouble1DArray()
        {
            var array = new double[Points3D.GetLength(0) * 3];

            for (var i = 0; i < Points3D.GetLength(0); i++)
            {
                var j = i * 3;
                array[j] = Points3D[i, 0];
                array[j + 1] = Points3D[i, 1];
                array[j + 2] = Points3D[i, 2];
            }

            return array;
        }
    }

    public static class ArchivableDictionaryExtension
    {
        public static bool Set(this ArchivableDictionary source, string key, double[,] point3d)
        {
            return source.Set(key, new Point3dListForArchivableDict(point3d).ToDouble1DArray());
        }

        public static bool TryGetValue(this ArchivableDictionary source, string key, out double[,] point3d)
        {
            point3d = new double[0, 0];
            if (source.TryGetValue(key, out var doubleArrayObj))
            {
                var point3dListForArchivebleDict = new Point3dListForArchivableDict((double[])doubleArrayObj);
                point3d = point3dListForArchivebleDict.Points3D;
                return true;
            }

            return false;
        }
    }
}
