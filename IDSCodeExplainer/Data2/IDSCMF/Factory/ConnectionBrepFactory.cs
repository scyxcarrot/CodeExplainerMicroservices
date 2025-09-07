using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Geometry;

namespace IDS.CMF.Factory
{
    public static class ConnectionBrepFactory
    {
        private static double tolerance = 0.001;

        public static Brep CreateConnection(IConnection line)
        {
            return CreateConnection(line, line.Width, line.Thickness, false);
        }

        public static Brep CreateConnection(IConnection line, double width, double thickness, bool bothSides)
        {
            var meanNormal = Vector3d.Divide(Vector3d.Add(RhinoVector3dConverter.ToVector3d(line.A.Direction),
                RhinoVector3dConverter.ToVector3d(line.B.Direction)), 2);
            meanNormal.Unitize();

            var locationA = RhinoPoint3dConverter.ToPoint3d(line.A.Location);
            var locationB = RhinoPoint3dConverter.ToPoint3d(line.B.Location);
            var lineDir = locationB - locationA;
            lineDir.Unitize();

            var extrudeDir = Vector3d.CrossProduct(meanNormal, lineDir);
            extrudeDir.Unitize();

            var centerA = locationA;
            var sideA1 = Point3d.Add(centerA, Vector3d.Multiply(extrudeDir, width / 2));
            var sideA2 = Point3d.Add(centerA, Vector3d.Multiply(-extrudeDir, width / 2));

            var centerB = locationB;
            var sideB1 = Point3d.Add(centerB, Vector3d.Multiply(extrudeDir, width / 2));
            var sideB2 = Point3d.Add(centerB, Vector3d.Multiply(-extrudeDir, width / 2));

            var surface = Brep.CreateFromCornerPoints(sideA1, sideA2, sideB2, sideB1, tolerance);
            if (surface.Faces[0].NormalAt(1.0, 1.0).IsParallelTo(meanNormal, RhinoMath.ToRadians(90)) != 1)
            {
                surface.Flip();
            }
            var brep = Brep.CreateFromOffsetFace(surface.Faces[0], thickness, tolerance, bothSides, true);
            return brep;
        }
    }
}
