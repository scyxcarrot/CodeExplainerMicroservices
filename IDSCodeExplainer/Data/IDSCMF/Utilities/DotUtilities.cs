using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System.Drawing;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Utilities
{
    public static class DotUtilities
    {
        public static double MaximumDistanceAllowed
        {
            get
            {
                var maximumDistanceAllowed = 30.0;
#if (INTERNAL)
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"MaximumDistanceAllowed = {maximumDistanceAllowed}");
#endif
                return maximumDistanceAllowed;
            }
        }

        public static IDot FindDotOnDifferentMesh(IDot dot, Mesh mesh, double maximumDistanceAllowed)
        {
            var point = RhinoPoint3dConverter.ToPoint3d(dot.Location);
            var meshPoint = mesh.ClosestMeshPoint(point, maximumDistanceAllowed);
            if (meshPoint == null)
            {
                return null;
            }

            var normalRadius = ScrewAngulationConstants.AverageNormalRadiusControlPoint;

            if (dot is DotPastille)
            {
                normalRadius = ScrewAngulationConstants.AverageNormalRadiusPastille;
            }

            var averageNormal = VectorUtilities.FindAverageNormal(mesh, meshPoint.Point, normalRadius);

#if (INTERNAL)
            if (CMFImplantDirector.IsDebugMode)
            {
                InternalUtilities.AddPoint(meshPoint.Point, "ClosestMeshPoint", Color.Blue);
                InternalUtilities.AddVector(meshPoint.Point, averageNormal, 10.0, Color.Red);
            }
#endif

            var duplicateDot = (IDot)dot.Clone();
            duplicateDot.Location = RhinoPoint3dConverter.ToIPoint3D(meshPoint.Point);
            duplicateDot.Direction = RhinoVector3dConverter.ToIVector3D(averageNormal);
            return duplicateDot;
        }
    }
}
