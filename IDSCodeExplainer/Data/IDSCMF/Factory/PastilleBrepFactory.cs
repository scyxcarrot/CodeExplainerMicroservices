using IDS.CMF.DataModel;
using IDS.Core.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;

namespace IDS.CMF.Factory
{
    public class PastilleBrepFactory
    {
        public Brep CreatePastille(DotPastille dot)
        {
            return CreatePastille(dot, dot.Diameter, dot.Thickness);
        }

        public Brep CreatePastille(DotPastille dot, double overwriteDiameter, double overwriteThickness)
        {
            var cylinder = CylinderUtilities.CreateCylinder(overwriteDiameter, RhinoPoint3dConverter.ToPoint3d(dot.Location), RhinoVector3dConverter.ToVector3d(dot.Direction), overwriteThickness);
            return Brep.CreateFromCylinder(cylinder, true, true);
        }

        public Brep CreatePastille(DotPastille dot, Vector3d overwriteDirection)
        {
            return CreatePastille(dot, overwriteDirection, Point3d.Unset, dot.Diameter, dot.Thickness);
        }

        public Brep CreatePastille(DotPastille dot, Vector3d overwriteDirection, Point3d overwriteLocation, double overwriteDiameter, double overwriteThickness)
        {
            var dotOverriden = (DotPastille)dot.Clone();
            dotOverriden.Direction = RhinoVector3dConverter.ToIVector3D(overwriteDirection);
            if (overwriteLocation != Point3d.Unset)
            {
                dotOverriden.Location = RhinoPoint3dConverter.ToIPoint3D(overwriteLocation);
            }
            return CreatePastille(dotOverriden, overwriteDiameter, overwriteThickness);
        }

        public Brep CreatePastilleSubtractor(DotPastille dot, Vector3d overwriteDirection)
        {
            var screwDiameter = 2.0;
            var height = dot.Thickness + 2;
            var bottomCenterPoint = Point3d.Add(RhinoPoint3dConverter.ToPoint3d(dot.Location), Vector3d.Multiply(overwriteDirection, -height / 2));
            var cylinder = CylinderUtilities.CreateCylinder(screwDiameter, bottomCenterPoint, overwriteDirection, height);
            return Brep.CreateFromCylinder(cylinder, true, true);
        }
    }
}
