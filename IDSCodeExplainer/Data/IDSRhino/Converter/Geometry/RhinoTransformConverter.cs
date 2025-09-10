using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using Rhino.Geometry;

namespace IDS.RhinoInterfaces.Converter
{
    public static class RhinoTransformConverter
    {
        public static Transform ToRhinoTransformationMatrix(ITransform transform)
        {
            var rhinoTransform = new Transform
            {
                M00 = transform.M00,
                M01 = transform.M01,
                M02 = transform.M02,
                M03 = transform.M03,
                M10 = transform.M10,
                M11 = transform.M11,
                M12 = transform.M12,
                M13 = transform.M13,
                M20 = transform.M20,
                M21 = transform.M21,
                M22 = transform.M22,
                M23 = transform.M23,
                M30 = transform.M30,
                M31 = transform.M31,
                M32 = transform.M32,
                M33 = transform.M33
            };

            return rhinoTransform;
        }

        public static IDSTransform ToIDSTransformationMatrix(Transform transform)
        {
            var idsTransform = new IDSTransform()
            {
                M00 = transform.M00,
                M01 = transform.M01,
                M02 = transform.M02,
                M03 = transform.M03,
                M10 = transform.M10,
                M11 = transform.M11,
                M12 = transform.M12,
                M13 = transform.M13,
                M20 = transform.M20,
                M21 = transform.M21,
                M22 = transform.M22,
                M23 = transform.M23,
                M30 = transform.M30,
                M31 = transform.M31,
                M32 = transform.M32,
                M33 = transform.M33
            };

            return idsTransform;
        }

        public static ITransform ToITransformationMatrix(Transform transform)
        {
            return ToIDSTransformationMatrix(transform);
        }

        public static Transform ToRhinoTransform(this ITransform transform)
        {
            return ToRhinoTransformationMatrix(transform);
        }

        public static IDSTransform ToIDSTransform(this Transform transform)
        {
            return ToIDSTransformationMatrix(transform);
        }

        public static ITransform ToITransform(this Transform transform)
        {
            return ToITransformationMatrix(transform);
        }
    }
}
