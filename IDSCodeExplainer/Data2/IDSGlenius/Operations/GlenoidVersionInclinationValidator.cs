using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace IDS.Glenius.Operations
{
    public static class GlenoidVersionInclinationValidator
    {
        public static bool CheckIfGlenoidVersionShouldBeNegative(Vector3d axAP, Vector3d glenoidVersionVector)
        {
            return Vector3d.Multiply(axAP, glenoidVersionVector) > 0;
        }
        public static bool CheckIfGlenoidVersionShouldBeNegative(Plane coronalPlane, Vector3d glenoidVersionVector, bool isLeft)
        {
            return Vector3d.Multiply(glenoidVersionVector, isLeft ? -coronalPlane.Normal : coronalPlane.Normal) > 0;
        }

        public static bool CheckIfGlenoidInclicinationShouldBeNegative(Vector3d axIS, Vector3d GlenoidInclinationVec)
        {
            return Vector3d.Multiply(axIS, GlenoidInclinationVec) < 0;
        }
    }
}
