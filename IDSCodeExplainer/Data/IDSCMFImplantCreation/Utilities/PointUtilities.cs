using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.Utilities
{
    public static class PointUtilities
    {
        public static IPoint3D FindFurthermostPointAlongVector(IList<IVertex> vertices, IVector3D vector)
        {
            var points = vertices.Select(vertex => (IPoint3D)new IDSPoint3D(vertex)).ToList();
            return FindFurthermostPointAlongVector(points, vector);
        }

        public static IPoint3D FindFurthermostPointAlongVector(IList<IPoint3D> points, IVector3D vector)
        {
            var maxDistance = Double.MinValue;
            var pointMax = IDSPoint3D.Unset;

            foreach (var point in points)
            {
                var distance = vector.DotMul(point);

                if (distance > maxDistance)
                {
                    pointMax = new IDSPoint3D(point);
                    maxDistance = distance;
                }
            }

            return pointMax;
        }
    }
}
