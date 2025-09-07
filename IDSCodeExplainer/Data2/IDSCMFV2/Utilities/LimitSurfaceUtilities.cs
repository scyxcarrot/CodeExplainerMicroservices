using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System.Collections.Generic;

namespace IDS.CMF.V2.Utilities
{
    public static class LimitSurfaceUtilities
    {
        public static void GenerateCurveExtensionPoints(IConsole console, List<IPoint3D> points, double extensionLength, out List<IPoint3D> originalCurvePoints, out List<IPoint3D> outerCurvePoints)
        {
            originalCurvePoints = new List<IPoint3D>();
            outerCurvePoints = new List<IPoint3D>();
            // Get local frames directly and process them
            var frameData = Curves.LocalFrames(console, new IDSCurve(points));

            foreach (var frameEntry in frameData)
            {
                // Each frame should have exactly one position with its vectors
                var framePosition = frameEntry.Key;
                var frameVectors = frameEntry.Value;

                // Ensure we have all three vectors (tangent, normal, binormal)
                if (frameVectors == null || frameVectors.Count < 3)
                {
                    continue; // Skip invalid frames
                }

                // Get the normal vector (index 1 in the list: [tangent, normal, binormal])
                var normalVector = frameVectors[1];

                // Origin point from the frame position
                var origin = new IDSPoint3D(
                    framePosition.X,
                    framePosition.Y,
                    framePosition.Z);

                // Calculate the outer normal vector (extend in negative normal direction)
                var outerNormalVector = new IDSVector3D(-normalVector.X, -normalVector.Y, -normalVector.Z);
                outerNormalVector.Unitize();

                var extendedOuterNormalVector = outerNormalVector.Mul(extensionLength);

                // Create the outer point (negative normal direction)
                var outerPoint = new IDSPoint3D(
                    origin.X + extendedOuterNormalVector.X,
                    origin.Y + extendedOuterNormalVector.Y,
                    origin.Z + extendedOuterNormalVector.Z);

                // Add points to respective lists
                originalCurvePoints.Add(origin);
                outerCurvePoints.Add(outerPoint);
            }
        }

    }
}
