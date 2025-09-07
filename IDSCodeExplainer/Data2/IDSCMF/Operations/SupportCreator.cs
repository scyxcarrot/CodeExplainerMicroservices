using IDS.CMF.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Operations
{
    public class SupportCreator
    {
        [Obsolete("Obsolete, please use SupportWrap1CreationLogic")]
        public bool PerformRoIWrap1(ref SupportCreationDataModel dataModel)
        {
            ResetDataModel(ref dataModel, true);

            var gapSize = dataModel.GapClosingDistanceForWrapRoI1;
            var resolution = 0.1 * gapSize;
            var resultingOffset = 0.0;

            IDSPluginHelper.WriteLine(LogCategory.Default, "Generating RoI wrap 1 mesh...");
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Parameter values used: Gap closing distance={gapSize}, Smallest detail: {resolution}, Resulting offset: {resultingOffset}");

            Mesh wrappedRoI;
            if (!Wrap.PerformWrap(new[] { dataModel.InputRoI }, resolution, gapSize, resultingOffset, false, true, false, false, out wrappedRoI))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Error while generating wrapped roi...");
                return false;
            }
            else
            {
                dataModel.WrapRoI1 = wrappedRoI;
#if (INTERNAL)
                InternalUtilities.ReplaceObject(dataModel.WrapRoI1, "Intermediate - WrapRoI1"); //temporary
#endif
                return true;
            }
        }

        [Obsolete("Obsolete, please use SupportRemainingPartCreationLogic")]
        public bool PerformSupportCreation(ref SupportCreationDataModel dataModel, out Dictionary<string, string> performanceReport)
        {
            performanceReport = new Dictionary<string,string>();

            ResetDataModel(ref dataModel, false);

            // 1. Wrap roi ==> Wrap 2
            // 2. Union Wrap 1 and Wrap 2
            // 3. Wrap
            // 4. Apply adaptive remesh
            // 5. Apply smoothing

            // ----- Step #1: Wrap roi ------------------------------------------------------------------------------------------
            var gapSize = 0.0;
            var resolution = 0.2;
            var resultingOffset = 0.0;

            if (!dataModel.SkipWrapRoI2)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Generating RoI wrap 2 mesh...");
                IDSPluginHelper.WriteLine(LogCategory.Default, $"Parameter values used: Gap closing distance={gapSize}, Smallest detail: {resolution}, Resulting offset: {resultingOffset}");

                var wrap2StopWatch = new Stopwatch();
                wrap2StopWatch.Start();

                Mesh wrappedRoI;
                if (!Wrap.PerformWrap(new[] { dataModel.InputRoI }, resolution, gapSize, resultingOffset, false, true, false, false, out wrappedRoI))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Error while generating wrapped roi...");

                    wrap2StopWatch.Stop();
                    return false;
                }
                else
                {
                    dataModel.WrapRoI2 = wrappedRoI;

                    wrap2StopWatch.Stop();
                    performanceReport.Add("Wrap 2", StringUtilitiesV2.ElapsedTimeSpanToString(wrap2StopWatch.Elapsed));
#if (INTERNAL)
                    InternalUtilities.ReplaceObject(dataModel.WrapRoI2, "Intermediate - WrapRoI2"); //temporary
#endif
                }
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Skipping RoI wrap 2... duplicating RoI as RoI wrap 2...");

                dataModel.WrapRoI2 = dataModel.InputRoI.DuplicateMesh();
#if (INTERNAL)
                InternalUtilities.ReplaceObject(dataModel.WrapRoI2, "Intermediate - WrapRoI2 - Duplication of RoI"); //temporary
#endif
            }
            // ----- Step #1: Wrap roi ------------------------------------------------------------------------------------------

            // ----- Step #2: Union Wrap 1 and Wrap 2 ---------------------------------------------------------------------------
            IDSPluginHelper.WriteLine(LogCategory.Default, "Performing boolean union...");

            var wrapUnionStopWatch = new Stopwatch();
            wrapUnionStopWatch.Start();

            var unionedMesh = MeshUtilities.UnionMeshes(new Mesh[] { dataModel.WrapRoI1, dataModel.WrapRoI2 });
            if (unionedMesh == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Error while performing boolean union...");

                wrapUnionStopWatch.Stop();
                return false;
            }
            else
            {
                dataModel.UnionedMesh = unionedMesh;

                wrapUnionStopWatch.Stop();
                performanceReport.Add("Boolean Union Wrap 1 and Wrap 2", StringUtilitiesV2.ElapsedTimeSpanToString(wrapUnionStopWatch.Elapsed));
#if (INTERNAL)
                InternalUtilities.ReplaceObject(dataModel.UnionedMesh, "Intermediate - UnionedMesh"); //temporary
#endif
            }
            // ----- Step #2: Union Wrap 1 and Wrap 2 ---------------------------------------------------------------------------

            // ----- Step #3: Wrap ----------------------------------------------------------------------------------------------
            gapSize = 2.0;
            resolution = dataModel.SmallestDetailForWrapUnion;
            resultingOffset = 0.1;

            IDSPluginHelper.WriteLine(LogCategory.Default, "Generating unioned wrap mesh...");
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Parameter values used: Gap closing distance={gapSize}, Smallest detail: {resolution}, Resulting offset: {resultingOffset}");

            var unionedWrapStopWatch = new Stopwatch();
            unionedWrapStopWatch.Start();

            Mesh unionedWrap;
            if (!Wrap.PerformWrap(new[] { dataModel.UnionedMesh }, resolution, gapSize, resultingOffset, false, true, false, false, out unionedWrap))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Error while generating wrapped union...");

                unionedWrapStopWatch.Stop();
                return false;
            }
            else
            {
                dataModel.WrapUnion = unionedWrap;

                unionedWrapStopWatch.Stop();
                performanceReport.Add("Wrap Union of Wrap 1 and Wrap 2", StringUtilitiesV2.ElapsedTimeSpanToString(unionedWrapStopWatch.Elapsed));

#if (INTERNAL)
                InternalUtilities.ReplaceObject(dataModel.WrapUnion, "Intermediate - WrapUnion"); //temporary
#endif
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
            
            IDSPluginHelper.WriteLine(LogCategory.Default, "Performing adaptive remesh...");

            Mesh remeshed = null;

            var remeshStopWatch = new Stopwatch();
            remeshStopWatch.Start();

            try
            {
                remeshed = Remesh.PerformRemesh(dataModel.WrapUnion, minEdgeLength, maxEdgeLength, growthTreshold, geometricalError, qualityThreshold, remeshedPreserveSharpEdges, remeshedIterations);
            }
            catch
            {
                remeshed = null;
            }

            remeshStopWatch.Stop();
            performanceReport.Add("Remesh", StringUtilitiesV2.ElapsedTimeSpanToString(remeshStopWatch.Elapsed));

            if (remeshed == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Error while remeshing...");
                return false;
            }
            else
            {
                dataModel.RemeshedMesh = remeshed;
#if (INTERNAL)
                InternalUtilities.ReplaceObject(dataModel.RemeshedMesh, "Intermediate - RemeshedMesh"); //temporary
#endif
            }
            // ----- Step #4: Apply adaptive remesh -----------------------------------------------------------------------------

            // ----- Step #5: Apply smoothing -----------------------------------------------------------------------------------
            var useCompensation = true;
            var preserveBadEdges = true;
            var smoothenPreserveSharpEdges = true;
            var sharpEdgeAngle = 30.0;
            var smoothenFactor = 0.7;
            var smoothenIterations = 10;

            IDSPluginHelper.WriteLine(LogCategory.Default, "Applying smoothing...");

            var smoothingStopWatch = new Stopwatch();
            smoothingStopWatch.Start();

            var smoothen = ExternalToolInterop.PerformSmoothing(dataModel.RemeshedMesh, useCompensation, preserveBadEdges, smoothenPreserveSharpEdges, sharpEdgeAngle, smoothenFactor, smoothenIterations);
            if (smoothen == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Error while apply smoothing...");

                smoothingStopWatch.Stop();
                return false;
            }
            else
            {
                dataModel.SmoothenMesh = smoothen;

                smoothingStopWatch.Stop();
                performanceReport.Add("Smoothing", StringUtilitiesV2.ElapsedTimeSpanToString(smoothingStopWatch.Elapsed));
#if (INTERNAL)
                InternalUtilities.ReplaceObject(dataModel.SmoothenMesh, "Intermediate - SmoothenMesh"); //temporary
#endif
            }
            // ----- Step #5: Apply smoothing -----------------------------------------------------------------------------------

            dataModel.FinalResult = dataModel.SmoothenMesh;
#if (INTERNAL)
            InternalUtilities.ReplaceObject(dataModel.FinalResult, "Intermediate - FinalResult"); //temporary
#endif
            return true;
        }

        private void ResetDataModel(ref SupportCreationDataModel dataModel, bool includingWrapRoI1)
        {
            if (includingWrapRoI1)
            {
                dataModel.WrapRoI1 = null;
            }

            dataModel.WrapRoI2 = null;
            dataModel.UnionedMesh = null;
            dataModel.WrapUnion = null;
            dataModel.RemeshedMesh = null;
            dataModel.SmoothenMesh = null;
            dataModel.FinalResult = null;
        }
    }
}
