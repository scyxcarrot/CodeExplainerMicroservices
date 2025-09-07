using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Core.Primitives;
using MtlsIds34.Math;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class GeometryMath
    {
        [HandleProcessCorruptedStateExceptions]
        public static List<IPoint3D> ProjectPointsOnPlane(IConsole console, List<IPoint3D> points, IPoint3D planeOrigin, IVector3D planeDirection)
        {
            var list = new List<IPoint3D>();

            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    foreach (var point in points)
                    {
                        var result = new ProjectPointOnPlane()
                        {
                            Point = new Vector3(point.X, point.Y, point.Z),
                            Plane = new Plane(new Vector3(planeOrigin.X, planeOrigin.Y, planeOrigin.Z),
                                new Vector3(planeDirection.X, planeDirection.Y, planeDirection.Z)),
                        }.Operate(context);

                        list.Add(new IDSPoint3D(result.Point.x, result.Point.y, result.Point.z));
                    }

                    return list;
                }
                catch (Exception e)
                {
                    throw new MtlsException("ProjectPointOnPlane", e.Message);
                }
            }
        }
    }
}
