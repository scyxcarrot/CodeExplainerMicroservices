using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using Materialise.MtlsAPI.Cmf;
using Materialise.MtlsAPI.Core.Primitives;
using System;
using System.Runtime.ExceptionServices;

namespace IDS.CMF.V2.MTLS.Operation
{
    public static class AutoImplantProposal
    {
        /// <summary>
        /// Get screw locations and directions for genio part.
        /// </summary>
        /// <param name="console">Console to log progress in the UI</param>
        /// <param name="plannedGenio">Genio mesh (chin area), need to make sure 1 shell or it won't work</param>
        /// <param name="plannedMandible">Mandible mesh</param>
        /// <param name="plannedTeeth">Teeth mesh, use wrapped mostly because users also use wrapped</param>
        /// <param name="plannedGenioCutMesh">Transformed planned osteotomy plane at genio</param>
        /// <param name="plannedMandibleCutMesh">Transformed planned osteotomy plane at mandible</param>
        /// <param name="plannedNerveLeft">Nerve Left mesh, use wrapped mostly because users also use wrapped</param>
        /// <param name="plannedNerveRight">Nerve Right mesh, use wrapped mostly because users also use wrapped</param>
        /// <param name="screwLength">Screw length, need to put for future qc checks</param>
        /// <param name="screwDiameter"></param>
        /// <param name="screwAngulation">Default 15 degrees</param>
        /// <param name="includeMiddlePlate">Default true</param>
        /// <param name="screwInsertionDirection">Default null</param>
        /// <param name="mandibleInterScrewDistance">Default 7.5mm</param>
        /// <param name="genioInterScrewDistance">Default 10.0mm</param>
        /// <param name="minInterScrewDistance">Default 6.2mm, This value must be below mandibleInterScrewDistance and genioInterScrewDistance or else it will throw an error</param>
        /// <param name="minDistanceToCut">Default 4.0mm, distance to osteotomy planes</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        public static AutoImplantProposalResult GetGenioScrewProposalAndConnections(
            IConsole console,
            IMesh plannedGenio, IMesh plannedMandible,
            IMesh plannedTeeth,
            IMesh plannedGenioCutMesh,
            IMesh plannedMandibleCutMesh,
            IMesh plannedNerveLeft, IMesh plannedNerveRight,
            double screwLength,
            double screwDiameter,
            double screwAngulation = 15,
            bool includeMiddlePlate = true,
            IVector3D screwInsertionDirection = null,
            double mandibleInterScrewDistance = 7.5,
            double genioInterScrewDistance = 10.0,
            double minInterScrewDistance = 6.2,
            double minDistanceToCut = 4.0,
            double minDistanceToBoneEdge = 3.5,
            IPlane sagittalPlane = null)
        {
            // need to remove noise shells before plugging into implant proposal
            // otherwise the outputs are all wrong and weird (screws are too close to each other)
            var fixedPlannedNerveLeft = FixMeshBeforeImplantProposal(
                console, plannedNerveLeft);
            var fixedPlannedNerveRight = FixMeshBeforeImplantProposal(
                console, plannedNerveRight);
            var fixedPlannedTeeth = FixMeshBeforeImplantProposal(
                console, plannedTeeth);
            var fixedPlannedGenio = FixMeshBeforeImplantProposal(
                console, plannedGenio);
            var fixedPlannedMandible = FixMeshBeforeImplantProposal(
                console, plannedMandible);
            var fixedPlannedGenioCutMesh = FixMeshBeforeImplantProposal(
                console, plannedGenioCutMesh);
            var fixedPlannedMandibleCutMesh = FixMeshBeforeImplantProposal(
                console, plannedMandibleCutMesh);

            var helper = new MtlsCmfImplantContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var genioImplantProposal = new GenioImplantProposal()
                {
                    // Meshes here
                    ManTriangles = fixedPlannedMandible.Faces.ToFacesArray2D(),
                    ManVertices = fixedPlannedMandible.Vertices.ToVerticesArray2D(),
                    NerveLeftTriangles = fixedPlannedNerveLeft.Faces.ToFacesArray2D(),
                    NerveLeftVertices = fixedPlannedNerveLeft.Vertices.ToVerticesArray2D(),
                    NerveRightTriangles = fixedPlannedNerveRight.Faces.ToFacesArray2D(),
                    NerveRightVertices = fixedPlannedNerveRight.Vertices.ToVerticesArray2D(),
                    GenTriangles = fixedPlannedGenio.Faces.ToFacesArray2D(),
                    GenVertices = fixedPlannedGenio.Vertices.ToVerticesArray2D(),
                    TeethTriangles = fixedPlannedTeeth.Faces.ToFacesArray2D(),
                    TeethVertices = fixedPlannedTeeth.Vertices.ToVerticesArray2D(),
                    CutgenTriangles = fixedPlannedGenioCutMesh.Faces.ToFacesArray2D(),
                    CutgenVertices = fixedPlannedGenioCutMesh.Vertices.ToVerticesArray2D(),
                    CutmanTriangles = fixedPlannedMandibleCutMesh.Faces.ToFacesArray2D(),
                    CutmanVertices = fixedPlannedMandibleCutMesh.Vertices.ToVerticesArray2D(),

                    // control screw here
                    ScrewLength = screwLength,
                    Angulation = screwAngulation,
                    ScrewDiameter = screwDiameter,

                    // control plates / links
                    IncludeMiddlePlate = includeMiddlePlate,
                    ManInterScrewDistance = mandibleInterScrewDistance,
                    GenInterScrewDistance = genioInterScrewDistance,
                    MinInterScrewDistance = minInterScrewDistance,
                    MinDistanceToCut = minDistanceToCut,
                    MinDistanceToBoneEdge = minDistanceToBoneEdge,
                };

                if (screwInsertionDirection != null &&
                    !screwInsertionDirection.EpsilonEquals(IDSVector3D.Zero, 0.01))
                {
                    genioImplantProposal.InsertionDirection = new Vector3(
                        screwInsertionDirection.X,
                        screwInsertionDirection.Y,
                        screwInsertionDirection.Z
                    );
                }

                if(sagittalPlane != null && !sagittalPlane.IsUnset())
                {
                    genioImplantProposal.MidSagittalPlaneOrigin = new Vector3(
                            sagittalPlane.Origin.X,
                            sagittalPlane.Origin.Y,
                            sagittalPlane.Origin.Z
                        );

                    var normalPlane = sagittalPlane.Normal;
                    if (!normalPlane.EpsilonEquals(IDSVector3D.Zero, 0.01))
                    {
                        genioImplantProposal.MidSagittalPlaneNormal = new Vector3(
                                normalPlane.X,
                                normalPlane.Y,
                                normalPlane.Z
                            );
                    }
                }

                try
                {
                    var genioImplantProposalResult = genioImplantProposal.Operate(context);
                    var linkConnections = (long[,])genioImplantProposalResult.LinkConnections.Data;
                    var plateConnections = (long[,])genioImplantProposalResult.PlateConnections.Data;

                    var screwHeads = (double[,])genioImplantProposalResult.ScrewHeads.Data;
                    var screwTips = (double[,])genioImplantProposalResult.ScrewTips.Data;
                    var screwNumbers = (long[])genioImplantProposalResult.ScrewNumbers.Data;
                    var screwIssues = (byte[])genioImplantProposalResult.ScrewIssues.Data;

                    var result = new AutoImplantProposalResult()
                    {
                        LinkConnections = linkConnections,
                        PlateConnections = plateConnections,
                        ScrewHeads = screwHeads,
                        ScrewTips = screwTips,
                        ScrewNumbers = screwNumbers,
                        ScrewIssues = screwIssues,
                    };

                    return result;
                }
                catch (Exception e)
                {
                    throw new MtlsException("GenioAutoImplantProposal", e.Message);
                }
            }
        }

        private static IMesh FixMeshBeforeImplantProposal(IConsole console, IMesh inMesh)
        {
            var freePointsRemovedMesh = AutoFixV2.RemoveFreePoints(console, inMesh);
            var noiseShellsRemovedMesh = 
                AutoFixV2.RemoveNoiseShells(console, freePointsRemovedMesh);

            return noiseShellsRemovedMesh;
        }
    }
}
