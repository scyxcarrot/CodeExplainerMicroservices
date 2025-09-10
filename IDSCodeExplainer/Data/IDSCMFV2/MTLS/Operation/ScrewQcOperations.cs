using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using Materialise.MtlsAPI.Array;
using Materialise.MtlsAPI.Cmf;
using Materialise.MtlsAPI.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace IDS.CMF.V2.MTLS.Operation
{
    public static class ScrewQcOperations
    {
        [HandleProcessCorruptedStateExceptions]
        public static bool PerformQcScrewScrewIntersections(IConsole console, IScrewQcData screw1, IScrewQcData screw2)
        {
            var screwQcDatas = new List<IScrewQcData>
                {
                    screw1,
                    screw2
                };

            var results = PerformQcScrewScrewIntersections(console, screwQcDatas);
            return results.Count > 0;
        }

        [HandleProcessCorruptedStateExceptions]
        public static List<Tuple<Guid, Guid>> PerformQcScrewScrewIntersections(IConsole console, List<IScrewQcData> screws)
        {
            var intersectingScrews = new List<Tuple<Guid, Guid>>();

            var helper = new MtlsCmfImplantContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var screwIntersectionResult = new QcScrewScrewIntersections
                    {
                        ScrewHeads = Array2D.Create(context,
                            screws.Select(s => s.HeadPoint).ToList().ToPointsArray2D()),
                        ScrewTips = Array2D.Create(context,
                            screws.Select(s => s.TipPoint).ToList().ToPointsArray2D()),
                        ScrewNumbers = Array1D.Create(context, Enumerable.Range(0, screws.Count).ToArray()),
                        ScrewDiameters =
                            Array1D.Create(context, screws.Select(s => s.CylinderDiameter).ToArray()),
                        ScrewShape = ScrewShape.Capsule
                    }.Operate(context);

                    var intersectingScrewsArray = (long[,])screwIntersectionResult.IntersectingScrews.Data;
                    for (var row = 0; row < intersectingScrewsArray.RowCount(); row++)
                    {
                        var array = intersectingScrewsArray.GetRow(row);
                        intersectingScrews.Add(new Tuple<Guid, Guid>(screws[(int)array[0]].Id, screws[(int)array[1]].Id));
                    }
                }
                catch (Exception e)
                {
                    throw new MtlsException("QcScrewScrewIntersections", e.Message);
                }
            }

            return intersectingScrews;
        }

        public static double PerformQcScrewAnatomyDistance(IConsole console, IScrewQcData screw, IMesh anatomy)
        {
            return PerformQcScrewAnatomyDistance(
                console, 
                new List<IScrewQcData> { screw }, 
                anatomy)[0];
        }

        [HandleProcessCorruptedStateExceptions]
        public static double[] PerformQcScrewAnatomyDistance(IConsole console, List<IScrewQcData> screws, IMesh anatomy)
        {
            var helper = new MtlsCmfImplantContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var screwIntersectionResult = new QcScrewAnatomyDistance()
                    {
                        ScrewHeads = Array2D.Create(context,
                            screws.Select(s => s.HeadPoint).ToList().ToPointsArray2D()),
                        ScrewTips = Array2D.Create(context,
                            screws.Select(s => s.TipPoint).ToList().ToPointsArray2D()),
                        ScrewDiameters =
                            Array1D.Create(context, screws.Select(s => s.CylinderDiameter).ToArray()),
                        AnatomyTriangles = anatomy.Faces.ToFacesArray2D(),
                        AnatomyVertices = anatomy.Vertices.ToVerticesArray2D(),
                        DistanceMethod = ScrewAnatomyDistanceMethod.MeshToCapsule
                    }.Operate(context);
                    
                    return (double[]) screwIntersectionResult.Distances.Data;
                }
                catch (Exception e)
                {
                    throw new MtlsException("QcScrewAnatomyDistance", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static IMesh GenerateQcScrewCapsule(IConsole console, IScrewQcData screw)
        {
            var helper = new MtlsCmfImplantContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var qcCylinderDiameter = screw.CylinderDiameter;
                    var actualHeight = screw.HeadPoint.DistanceTo(screw.TipPoint);
                    var capsuleHeight = actualHeight - qcCylinderDiameter;

                    var capsuleResult = new Capsule()
                    {
                        Radius = qcCylinderDiameter / 2,
                        Height = capsuleHeight
                    }.Operate(context);

                    var vertexArray = (double[,])capsuleResult.Vertices.Data;
                    var triangleArray = (ulong[,])capsuleResult.Triangles.Data;

                    var capsuleMesh = new IDSMesh(vertexArray, triangleArray);

                    //Capsule API returns a capsule shape with the central axis oriented along the z-axis and the center at (0,0,0)

                    return GeometryTransformation.PerformMeshTransform(console, 
                        capsuleMesh,
                        new IDSPoint3D(0.0, 0.0, actualHeight / 2),
                        new IDSVector3D(0.0, 0.0, 1.0),
                        screw.HeadPoint,
                        screw.HeadPoint.Sub(screw.TipPoint)
                        );
                }
                catch (Exception e)
                {
                    throw new MtlsException("GenerateQcScrewCapsule", e.Message);
                }
            }
        }
    }
}
