using IDS.Core.V2.Extensions;
using IDS.Core.V2.Logic;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Logic;
using IDS.Interface.Tools;
using System.Diagnostics;

namespace IDS.CMF.V2.Logics
{
    public class SupportRemainingPartCreationLogic:
        LogicV2<SupportCreationContext>
    {
        public SupportRemainingPartCreationLogic(IConsole console) : base(console)
        {
        }

        public override LogicStatus Execute(SupportCreationContext context)
        {
            // 1. Wrap roi ==> Wrap 2
            // 2. Union Wrap 1 and Wrap 2
            // 3. Wrap
            // 4. Apply adaptive remesh
            // 5. Apply smoothing

            // ----- Step #1: Wrap roi ------------------------------------------------------------------------------------------
            var gapSize = 0.0;
            var resolution = 0.2;
            var resultingOffset = 0.0;

            if (!context.SkipWrapRoI2)
            {
                console.WriteLine("Generating RoI wrap 2 mesh...");
                console.WriteLine($"Parameter values used: Gap closing distance={gapSize}, Smallest detail: {resolution}, Resulting offset: {resultingOffset}");

                var wrap2StopWatch = new Stopwatch();
                wrap2StopWatch.Start();

                IMesh wrappedRoI;
                if (!WrapV2.PerformWrap(console, new[] { context.InputRoI }, resolution, gapSize, resultingOffset, 
                        false, true, false, false, out wrappedRoI))
                {
                    console.WriteErrorLine("Error while generating wrapped roi...");

                    wrap2StopWatch.Stop();
                    return LogicStatus.Failure;
                }
                else
                {
                    context.WrapRoI2 = wrappedRoI;

                    wrap2StopWatch.Stop();
                    context.TrackingInfo.ForceAddTrackingParameterSafely("Wrap 2", StringUtilitiesV2.ElapsedTimeSpanToString(wrap2StopWatch.Elapsed));
                }
            }
            else
            {
                console.WriteLine("Skipping RoI wrap 2... duplicating RoI as RoI wrap 2...");

                context.WrapRoI2 = context.InputRoI.DuplicateIDSMesh();
            }
            // ----- Step #1: Wrap roi ------------------------------------------------------------------------------------------

            // ----- Step #2: Union Wrap 1 and Wrap 2 ---------------------------------------------------------------------------
            console.WriteLine("Performing boolean union...");

            var wrapUnionStopWatch = new Stopwatch();
            wrapUnionStopWatch.Start();

            var unionedMesh = MeshUtilitiesV2.UnionMeshes(console, new IMesh[] { context.WrapRoI1, context.WrapRoI2 });
            if (unionedMesh == null)
            {
                console.WriteErrorLine("Error while performing boolean union...");

                wrapUnionStopWatch.Stop();
                return LogicStatus.Failure;
            }
            else
            {
                context.UnionedMesh = unionedMesh;

                wrapUnionStopWatch.Stop();
                context.TrackingInfo.ForceAddTrackingParameterSafely("Boolean Union Wrap 1 and Wrap 2", StringUtilitiesV2.ElapsedTimeSpanToString(wrapUnionStopWatch.Elapsed));
            }
            // ----- Step #2: Union Wrap 1 and Wrap 2 ---------------------------------------------------------------------------

            // ----- Step #3: Wrap ----------------------------------------------------------------------------------------------
            gapSize = 2.0;
            resolution = context.SmallestDetailForWrapUnion;
            resultingOffset = 0.1;

            console.WriteLine("Generating unioned wrap mesh...");
            console.WriteLine($"Parameter values used: Gap closing distance={gapSize}, Smallest detail: {resolution}, Resulting offset: {resultingOffset}");

            var unionedWrapStopWatch = new Stopwatch();
            unionedWrapStopWatch.Start();

            IMesh unionedWrap;
            if (!WrapV2.PerformWrap(console, new[] { context.UnionedMesh }, resolution, gapSize, resultingOffset, false, 
                    true, false, false, out unionedWrap))
            {
                console.WriteErrorLine("Error while generating wrapped union...");

                unionedWrapStopWatch.Stop();
                return LogicStatus.Failure;
            }
            else
            {
                context.WrapUnion = unionedWrap;

                unionedWrapStopWatch.Stop();
                context.TrackingInfo.ForceAddTrackingParameterSafely("Wrap Union of Wrap 1 and Wrap 2", StringUtilitiesV2.ElapsedTimeSpanToString(unionedWrapStopWatch.Elapsed));
            }
            // ----- Step #3: Wrap ----------------------------------------------------------------------------------------------

            // ----- Step #4: Apply adaptive remesh -----------------------------------------------------------------------------
            var minEdgeLength = 0.0;
            var maxEdgeLength = 0.3;
            var growthTreshold = 0.2;
            var geometricalError = 0.02;
            var qualityThreshold = 0.3;
            var remeshedPreserveSharpEdges = true;
            var remeshedIterations = 3;

            console.WriteLine("Performing adaptive remesh...");

            IMesh remeshed = null;

            var remeshStopWatch = new Stopwatch();
            remeshStopWatch.Start();

            try
            {
                remeshed = RemeshV2.PerformRemesh(console, context.WrapUnion, minEdgeLength, maxEdgeLength, growthTreshold, geometricalError, qualityThreshold, remeshedPreserveSharpEdges, remeshedIterations);
            }
            catch
            {
                remeshed = null;
            }

            remeshStopWatch.Stop();
            context.TrackingInfo.ForceAddTrackingParameterSafely("Remesh", StringUtilitiesV2.ElapsedTimeSpanToString(remeshStopWatch.Elapsed));

            if (remeshed == null)
            {
                console.WriteErrorLine("Error while remeshing...");
                return LogicStatus.Failure;
            }
            else
            {
                context.RemeshedMesh = remeshed;
            }
            // ----- Step #4: Apply adaptive remesh -----------------------------------------------------------------------------

            // ----- Step #5: Apply smoothing -----------------------------------------------------------------------------------
            const bool useCompensation = true;
            const bool preserveBadEdges = true;
            const bool smoothenPreserveSharpEdges = true;
            var sharpEdgeAngle = 30.0;
            var smoothenFactor = 0.7;
            var smoothenIterations = 10;

            console.WriteLine("Applying smoothing...");

            var smoothingStopWatch = new Stopwatch();
            smoothingStopWatch.Start();

            var smoothen = MeshUtilitiesV2.PerformSmoothing(console, context.RemeshedMesh, 
                useCompensation, preserveBadEdges, smoothenPreserveSharpEdges, 
                sharpEdgeAngle, smoothenFactor, smoothenIterations);
            if (smoothen == null)
            {
                console.WriteErrorLine("Error while apply smoothing...");

                smoothingStopWatch.Stop();
                return LogicStatus.Failure;
            }
            else
            {
                context.SmoothenMesh = smoothen;

                smoothingStopWatch.Stop();
                context.TrackingInfo.ForceAddTrackingParameterSafely("Smoothing", StringUtilitiesV2.ElapsedTimeSpanToString(smoothingStopWatch.Elapsed));

            }
            // ----- Step #5: Apply smoothing -----------------------------------------------------------------------------------

            context.FinalResult = context.SmoothenMesh;

            return LogicStatus.Success;
        }
    }
}
