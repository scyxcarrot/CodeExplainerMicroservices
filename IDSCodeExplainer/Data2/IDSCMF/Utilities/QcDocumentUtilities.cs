using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;

namespace IDS.CMF.Utilities
{
    public static class QcDocumentUtilities
    {
        public static void FillDictionaryForMeshFixing(ref Dictionary<string, string> valueDictionary, Mesh mesh, string tagKey, ref Dictionary<string, string> parameterValueTracking)
        {
            var results = MeshDiagnostics.GetMeshDiagnostics(mesh);

            valueDictionary.Add($"CELL_{tagKey}_FIXING_INVERTED_NORMALS", GenerateMeshFixingResultCell(results.NumberOfInvertedNormal));
            valueDictionary.Add($"CELL_{tagKey}_FIXING_BAD_EDGES", GenerateMeshFixingResultCell(results.NumberOfBadEdges));
            valueDictionary.Add($"CELL_{tagKey}_FIXING_BAD_CONTOURS", GenerateMeshFixingResultCell(results.NumberOfBadContours));
            valueDictionary.Add($"CELL_{tagKey}_FIXING_NEAR_BAD_EDGES", GenerateMeshFixingResultCell(results.NumberOfNearBadEdges));
            valueDictionary.Add($"CELL_{tagKey}_FIXING_HOLES", GenerateMeshFixingResultCell(results.NumberOfHoles));
            valueDictionary.Add($"CELL_{tagKey}_FIXING_SHELLS", GenerateMeshFixingResultCell(results.NumberOfShells, 1));
            valueDictionary.Add($"CELL_{tagKey}_FIXING_OVERLAPPING_TRIANGLES", GenerateMeshFixingResultCell(results.NumberOfOverlappingTriangles));
            valueDictionary.Add($"CELL_{tagKey}_FIXING_INTERSECTING_TRIANGLES", GenerateMeshFixingResultCell(results.NumberOfIntersectingTriangles));

            parameterValueTracking.Add($"InvertedNormals", $"{results.NumberOfInvertedNormal}");
            parameterValueTracking.Add($"BadEdges", $"{results.NumberOfBadEdges}");
            parameterValueTracking.Add($"BadContours", $"{results.NumberOfBadContours}");
            parameterValueTracking.Add($"NearBadEdges", $"{results.NumberOfNearBadEdges}");
            parameterValueTracking.Add($"Holes", $"{results.NumberOfHoles}");
            parameterValueTracking.Add($"Shells", $"{results.NumberOfShells}");
            parameterValueTracking.Add($"OverlappingTriangles", $"{results.NumberOfOverlappingTriangles}");
            parameterValueTracking.Add($"IntersectingTriangles", $"{results.NumberOfIntersectingTriangles}");
        }

        private static string GenerateMeshFixingResultCell(long value, long expectedResult = 0)
        {
            var cellColor = "col_green";
            if (value != expectedResult)
            {
                cellColor = "col_red";
            }

            var cellString = $"<td class=\"{ cellColor }\">{ value }</td>";
            return cellString;
        }
    }
}
