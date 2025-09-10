using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Geometry;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.Core.V2.Utilities
{
    public static class MeshUtilitiesV2
    {
        public static IMesh CreateUnsharedVerticesMesh(IMesh sourceMesh)
        {
            var newMesh = new IDSMesh();

            var faces = sourceMesh.Faces;
            var vertices = sourceMesh.Vertices;

            for (var i = 0; i < faces.Count; ++i)
            {
                var face = faces[i];

                newMesh.Vertices.Add(new IDSVertex(vertices[Convert.ToInt32(face.A)]));
                newMesh.Vertices.Add(new IDSVertex(vertices[Convert.ToInt32(face.B)]));
                newMesh.Vertices.Add(new IDSVertex(vertices[Convert.ToInt32(face.C)]));

                newMesh.Faces.Add(new IDSFace(Convert.ToUInt64(i * 3),
                    Convert.ToUInt64(i * 3 + 1), Convert.ToUInt64(i * 3 + 2)));
            }

            return newMesh;
        }

        /// <summary>
        /// Combine all meshes into one mesh
        /// </summary>
        /// <param name="meshes">The array of meshes.</param>
        /// <returns></returns>
        public static IMesh AppendMeshes(IMesh[] meshes)
        {
            if (meshes == null || !meshes.Any())
                return null;

            var merged = new IDSMesh();

            foreach (var mesh in meshes)
            {
                merged.Append(mesh);
            }

            return merged;
        }

        public static IMesh AppendMeshes(IEnumerable<IMesh> meshes)
        {
            return AppendMeshes(meshes.ToArray());
        }

        public static IMesh UnionMeshes(IConsole console, IEnumerable<IMesh> meshes)
        {
            return BooleansV2.PerformBooleanUnion(console, out var unioned, meshes.ToArray()) ? unioned : null;
        }

        public static IMesh PerformSmoothing(IConsole console, IMesh mesh, bool useCompensation, bool preserveBadEdges,
            bool preserveSharpEdges, double sharpEdgeAngle, double smoothenFactor, int iterations)
        {
            var meshStlPath = StlUtilitiesV2.WriteStlTempFile(mesh);
            var smoothenStlTargetPath = Path.GetTempPath() + "IDS_" + Guid.NewGuid() + ".stl";

            var filesCreated = new List<string> { meshStlPath, smoothenStlTargetPath };

            var cmdArgs = $"Smooth {meshStlPath} {smoothenStlTargetPath} FIRST_ORDER_LAPLACIAN {useCompensation} {preserveBadEdges} {preserveSharpEdges} {sharpEdgeAngle} {smoothenFactor} {iterations}";

            if (!ExternalToolsUtilities.RunMatSdkConsolex86Executable(cmdArgs, filesCreated, console, true))
            {
                return null;
            }

            IMesh smoothen;
            StlUtilitiesV2.StlBinaryToIDSMesh(smoothenStlTargetPath, out smoothen);
            filesCreated.ForEach(f => { if (File.Exists(f)) { File.Delete(f); } });

            return smoothen;
        }

        public static List<IMesh> GetSurfaces(IMesh mesh, ulong[] surfaceStructure)
        {
            var surfaceIndices = surfaceStructure.Distinct().ToArray();
            return surfaceIndices.Select(surfaceIndex => GetSurface(mesh, surfaceStructure, surfaceIndex)).ToList();
        }

        public static IMesh GetSurface(IMesh mesh, ulong[] surfaceStructure, ulong surfaceIndex)
        {
            var subSurface = new IDSMesh(mesh.Vertices.ToVerticesArray2D(), new ulong[0, 3]);

            for (ulong i = 0; i < (ulong)surfaceStructure.Length; i++)
            {
                if (surfaceStructure[i] == surfaceIndex)
                {
                    subSurface.Faces.Add(mesh.Faces[(int)i]);
                }
            }

            return subSurface;
        }

        public static IMesh CorrectMeshNormalDirection(IConsole console, IMesh meshA, IMesh meshB, IVector3D averageNormal)
        {
            IMesh invertedMesh;
            // Get the center of gravity of the surface mesh using MeshDiagnostics
            var surfaceBDimensions = MeshDiagnostics.GetMeshDimensions(console, meshB);
            var surfaceBCenterOfGravity = new IDSPoint3D(
                surfaceBDimensions.CenterOfGravity[0],
                surfaceBDimensions.CenterOfGravity[1],
                surfaceBDimensions.CenterOfGravity[2]);

            // Step 3: Get the center of gravity of the cast mesh using MeshDiagnostics
            var surfaceADimensions = MeshDiagnostics.GetMeshDimensions(console, meshA);
            var surfaceACenterOfGravity = new IDSPoint3D(
                surfaceADimensions.CenterOfGravity[0],
                surfaceADimensions.CenterOfGravity[1],
                surfaceADimensions.CenterOfGravity[2]);

            // Calculate CastToSurfaceVector (Center of gravity of surface - Center of gravity of cast)
            var castToSurfaceVector = surfaceBCenterOfGravity.Sub(surfaceACenterOfGravity);
            castToSurfaceVector.Unitize(); // Step 6: Get unit vector of CastToSurfaceVector

            // Calculate dot product between CastToSurfaceUnitVector and AverageUnitNormal
            var dotProduct = VectorUtilitiesV2.DotProduct(castToSurfaceVector, averageNormal);

            // Check dot product and invert normals if necessary
            if (dotProduct < 0)
            {
                // Opposite direction detected - invert normals
                invertedMesh = AutoFixV2.InvertNormal(console, meshB);
            }
            else
            {
                invertedMesh = meshB; // return the original mesh if normals are already correct
            }

            return invertedMesh;
        }
    }
}
