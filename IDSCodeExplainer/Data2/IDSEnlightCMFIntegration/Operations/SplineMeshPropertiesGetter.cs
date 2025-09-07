using IDS.EnlightCMFIntegration.DataModel;
using MtlsIds34.Core;
using MtlsIds34.MeshDesign;
using System;

namespace IDS.EnlightCMFIntegration.Operations
{
    public class SplineMeshPropertiesGetter
    {
        public SplineMeshPropertiesGetter()
        {

        }

        public void GetSplineMeshProperties(SplineProperties splineProperties)
        {
            using (var context = new Context())
            {
                var operation = new Sweep
                {
                    PathPoints = splineProperties.GeometryPoints,
                    ProfilePoints = GenerateCircularProfilePoints(splineProperties.Diameter / 2)
                };

                var result = operation.Operate(context);

                splineProperties.Triangles = (ulong[,])result.Triangles.Data;
                splineProperties.Vertices = (double[,])result.Vertices.Data;
            }
        }

        private double[,] GenerateCircularProfilePoints(double radius)
        {
            var circumference = 2 * Math.PI * radius;
            var numberOfSegments = (int)(circumference / 0.1);
            var twoDimPoints = new double[numberOfSegments + 1, 2];

            for (var i = 0; i < numberOfSegments; i++)
            {
                var degree = ((2 * Math.PI) / numberOfSegments) * i;
                twoDimPoints[i, 0] = radius * Math.Cos(degree);
                twoDimPoints[i, 1] = radius * Math.Sin(degree);
            }

            twoDimPoints[numberOfSegments, 0] = twoDimPoints[0, 0];
            twoDimPoints[numberOfSegments, 1] = twoDimPoints[0, 1];

            return twoDimPoints;
        }
    }
}
