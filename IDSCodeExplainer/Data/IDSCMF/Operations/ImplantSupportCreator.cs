using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Constants;
using IDS.CMF.V2.Logics;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using IDS.RhinoInterface.Converter;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Operations
{
    public class ImplantSupportCreator
    {
        public bool PerformImplantSupportBiggerRoICreation(CMFImplantDirector director,
            Dictionary<string, string> trackingReport, out Mesh biggerRoI)
        {
            var objectManager = new CMFObjectManager(director);
            var constraintMeshQuery = new ConstraintMeshQuery(objectManager);
            var plannedBones = constraintMeshQuery.GetConstraintRhinoObjectForImplant();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var biggerRoIParts = plannedBones.Select(i => (Mesh)i.Geometry).ToList();
            if (objectManager.HasBuildingBlock(IBB.ImplantSupportTeethIntegrationRoI))
            {
                var teethIntegrationRoI = objectManager.GetBuildingBlock(IBB.ImplantSupportTeethIntegrationRoI);
                biggerRoIParts.Add((Mesh)teethIntegrationRoI.Geometry);
            }

            if (objectManager.HasBuildingBlock(IBB.ImplantSupportRemovedMetalIntegrationRoI))
            {
                var removedMetalIntegrationRoI = objectManager.GetBuildingBlock(IBB.ImplantSupportRemovedMetalIntegrationRoI);
                biggerRoIParts.Add((Mesh)removedMetalIntegrationRoI.Geometry);
            }

            if (objectManager.HasBuildingBlock(IBB.ImplantSupportRemainedMetalIntegrationRoI))
            {
                var remainedMetalIntegrationRoI = objectManager.GetBuildingBlock(IBB.ImplantSupportRemainedMetalIntegrationRoI);
                biggerRoIParts.Add((Mesh)remainedMetalIntegrationRoI.Geometry);
            }

            var check = Booleans.PerformBooleanUnion(out biggerRoI, biggerRoIParts.ToArray());
            stopwatch.Stop();

            trackingReport.Add("Union RoI", StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));
            return check;
        }

        public bool PerformIndividualImplantSupportCreation(CMFImplantDirector director, Mesh biggerRoI, CasePreferenceDataModel casePreferenceData,
            Dictionary<string, string> trackingReport, ref SupportCreationDataModel dataModel)
        {
            var objectManager = new CMFObjectManager(director);
            var stopwatch = new Stopwatch();

            var implantSupportInputRoIs = new List<Mesh>();
            var parameters = CMFPreferences.GetImplantSupportParameters();

            IDSPluginHelper.WriteLine(LogCategory.Default, $"Start generating implant support for {casePreferenceData.CaseName}...");

            stopwatch.Start();
            if (!casePreferenceData.ImplantDataModel.ConnectionList.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"Skipping {casePreferenceData.CaseName} because it has no design.");
                return false;
            }

            var implantSupportInputRoI = ImplantCreationUtilities.GenerateImplantRoI(casePreferenceData, biggerRoI,
                parameters.RoIConnectionRadius, parameters.RoIPastilleRadius, parameters.RoILandmarkRadius, out var tubeMesh);
#if (INTERNAL)
            InternalUtilities.ReplaceObject(implantSupportInputRoI, $"implantRoI-{casePreferenceData.CaseName}");
#endif
            implantSupportInputRoIs.Add(implantSupportInputRoI);
            stopwatch.Stop();
            trackingReport.Add($"Generate Implant RoI-{casePreferenceData.CaseName}", StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));

            stopwatch.Restart();

            // Whole transition and margin need to be include instead of RoI, get the parts that was collided
            implantSupportInputRoIs.AddRange(GetRelatedSupportInputMeshes(objectManager, tubeMesh));

            var check = Booleans.PerformBooleanUnion(out var unitedImplantSupportInputRoI, implantSupportInputRoIs.ToArray());

            stopwatch.Stop();
            trackingReport.Add($"Union Margins/Transitions-{casePreferenceData.CaseName}", StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));

            var generatedImplantSupport = GenerateSupport(casePreferenceData.CaseName,
                unitedImplantSupportInputRoI, ref trackingReport, ref dataModel);
            dataModel.FinalResult = generatedImplantSupport;

#if (INTERNAL)
            InternalUtilities.ReplaceObject(generatedImplantSupport, $"generatedImplantSupport-{casePreferenceData.CaseName}");
#endif

            IDSPluginHelper.WriteLine(LogCategory.Default, $"Completed implant support generation for {casePreferenceData.CaseName} successfully");

            return check;
        }

        private IEnumerable<Mesh> GetRelatedSupportInputMeshes(CMFObjectManager objectManager, Mesh tubeRoIMesh)
        {
            var relatedSupportInputMeshes = new List<Mesh>();

            relatedSupportInputMeshes.AddRange(GetRelatedSupportInputMeshesWithBuildingBlocks(
                objectManager, tubeRoIMesh, IBB.ImplantMargin));

            relatedSupportInputMeshes.AddRange(GetRelatedSupportInputMeshesWithBuildingBlocks(
                objectManager, tubeRoIMesh, IBB.ImplantTransition));

            return relatedSupportInputMeshes;
        }

        private IEnumerable<Mesh> GetRelatedSupportInputMeshesWithBuildingBlocks(CMFObjectManager objectManager, Mesh tubeRoIMesh,
            IBB buildingBlock)
        {
            if (!objectManager.HasBuildingBlock(buildingBlock))
            {
                return new List<Mesh>();
            }

            var supportInputRhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock);
            var supportInputMeshes = supportInputRhinoObjects.Where(i => i.Geometry is Mesh)
                .Select(i => (Mesh)i.Geometry);
            return MeshUtilities.GetCollidedMeshes(tubeRoIMesh, supportInputMeshes, 0, false);
        }

        private Mesh GenerateSupport(string caseName, Mesh inputRoI, ref Dictionary<string, string> trackingReport, ref SupportCreationDataModel dataModel)
        {
            dataModel.InputRoI = inputRoI;
            dataModel.GapClosingDistanceForWrapRoI1 = ImplantSupportCreationParameters.DefaultGapClosingDistanceForWrapRoI1;
            dataModel.SmallestDetailForWrapUnion = ImplantSupportCreationParameters.DefaultSmallestDetailForWrapUnion;
            dataModel.SkipWrapRoI2 = false;

            var creator = new SupportCreator();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var success = creator.PerformRoIWrap1(ref dataModel);
            stopwatch.Stop();
            trackingReport.Add($"Implant Support Wrap 1-{caseName}", StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));

            success = creator.PerformSupportCreation(ref dataModel, out var performanceReport);
            foreach (var keyValuePair in performanceReport)
            {
                trackingReport.Add($"Implant Support {keyValuePair.Key}-{caseName}", keyValuePair.Value);
            }

            return dataModel.FinalResult;
        }

        #region Patch Support
        private static List<Mesh> GetExtraSupportInputMesh(CMFImplantDirector director, IBB buildingBlock)
        {
            var objectManager = new CMFObjectManager(director);
            if (!objectManager.HasBuildingBlock(buildingBlock))
            {
                return new List<Mesh>();
            }

            var supportInputRhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock);
            return supportInputRhinoObjects.Where(i => i.Geometry is Mesh)
                .Select(i => (Mesh)i.Geometry).ToList();
        }

        public Mesh CreateBiggerSupportRoI(CMFImplantDirector director, out Dictionary<string, string> trackingReport)
        {
            trackingReport = new Dictionary<string, string>();

            PerformImplantSupportBiggerRoICreation(director, trackingReport, out var biggerRoI);
            var implantSupportInputRoIs = new List<Mesh>() { biggerRoI };

            var unionStopWatch = new Stopwatch();
            unionStopWatch.Start();
            implantSupportInputRoIs.AddRange(GetExtraSupportInputMesh(director, IBB.ImplantMargin));
            implantSupportInputRoIs.AddRange(GetExtraSupportInputMesh(director, IBB.ImplantTransition));
            var console = new IDSRhinoConsole();
            BooleansV2.PerformBooleanUnion(console, out var biggerRoIIdsMesh, implantSupportInputRoIs.Select(RhinoMeshConverter.ToIDSMesh).ToArray());
            biggerRoI = RhinoMeshConverter.ToRhinoMesh(biggerRoIIdsMesh);
            unionStopWatch.Stop();
            trackingReport.Add("Union Margins/Transitions", $"{StringUtilitiesV2.ElapsedTimeSpanToString(unionStopWatch.Elapsed)}");

            return biggerRoI;
        }

        public Dictionary<Curve, SupportCreationContext> GeneratePatchSupports(
            CMFObjectManager objectManager,
            CasePreferenceDataModel casePreferenceDataModel, Mesh biggerRoI,
            out Dictionary<Curve, string> roiTrackingReport,
            out Dictionary<Curve, Dictionary<string, string>> supportTrackingReport)
        {
            var parameters = CMFPreferences.GetImplantSupportParameters();
            var connectionRoIs = ImplantCreationUtilities.GenerateImplantPatchRoI(objectManager, casePreferenceDataModel,
                biggerRoI, parameters.RoIConnectionRadius, parameters.RoIPastilleRadius, parameters.RoILandmarkRadius,
                out roiTrackingReport);

            var dataModels = new Dictionary<Curve, SupportCreationContext>();
            supportTrackingReport = new Dictionary<Curve, Dictionary<string, string>>();

            var count = 0;
            foreach (var connectionRoI in connectionRoIs)
            {
                var curve = connectionRoI.Key;
                var connectionName = $"Connection {count++}";
                var roi = connectionRoI.Value;

                var trackingReport = new Dictionary<string, string>();
                var connectionStopwatch = new Stopwatch();
                connectionStopwatch.Start();

                var stopwatchSmallerRoI = new Stopwatch();
                stopwatchSmallerRoI.Start();
                CreateTubeMesh(
                    casePreferenceDataModel, 
                    curve, roi, 
                    out var biggerTubeMesh, out var smallerTubeMesh);
                stopwatchSmallerRoI.Stop();
                trackingReport.Add($"Implant Support RoI Tube-{connectionName}", $"{StringUtilitiesV2.ElapsedTimeSpanToString(stopwatchSmallerRoI.Elapsed)}");

                var dataModel = GenerateSupport(connectionName, roi, smallerTubeMesh, biggerTubeMesh, ref trackingReport);
                connectionStopwatch.Stop();

                trackingReport.Add($"{connectionName}[Full]", $"{StringUtilitiesV2.ElapsedTimeSpanToString(connectionStopwatch.Elapsed)}");

                supportTrackingReport.Add(curve, trackingReport);
                dataModels.Add(curve, dataModel);
            }

            return dataModels;
        }

        private static void CreateTubeMesh(
            CasePreferenceDataModel casePreferenceDataModel,
            Curve curve, Mesh roi, 
            out Mesh biggerTubeMesh, out Mesh smallerTubeMesh)
        {
            var connections = DataModelUtilities.GetConnections(curve, casePreferenceDataModel.ImplantDataModel.ConnectionList);
            var connectionsDuplicated = connections.Select(c => (IConnection)c.Clone());
            var implantDataModel = new ImplantDataModel(connectionsDuplicated);

            var tubeRadius = casePreferenceDataModel.CasePrefData.LinkWidthMm > casePreferenceDataModel.CasePrefData.PlateWidthMm ?
                casePreferenceDataModel.CasePrefData.LinkWidthMm / 2 + 1 : casePreferenceDataModel.CasePrefData.PlateWidthMm / 2 + 1.2;
            var pastilleRadius = casePreferenceDataModel.CasePrefData.PastilleDiameter / 2;
            biggerTubeMesh = ImplantCreationUtilities.GenerateImplantRoITube(implantDataModel,
                roi, tubeRadius, pastilleRadius + 1.2, pastilleRadius, 1.8);
            smallerTubeMesh = ImplantCreationUtilities.GenerateImplantRoITube(implantDataModel,
                roi, tubeRadius, pastilleRadius + 1.2, pastilleRadius, 1.2);
        }

        private SupportCreationContext GenerateSupport(
            string postfix, 
            Mesh inputRoI, Mesh smallerTubeMesh, Mesh biggerTubeMesh,
            ref Dictionary<string, string> trackingReport)
        {
            var console = new IDSRhinoConsole();
            var inputRoIIdsMesh = RhinoMeshConverter.ToIDSMesh(inputRoI);
            var supportCreationContext = new SupportCreationContext(console)
            {
                InputRoI = inputRoIIdsMesh,
                GapClosingDistanceForWrapRoI1 = ImplantSupportCreationParameters.DefaultGapClosingDistanceForWrapRoI1,
                SmallestDetailForWrapUnion = ImplantSupportCreationParameters.DefaultSmallestDetailForWrapUnion,
                SkipWrapRoI2 = false
            };
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var supportWrap1CreationLogic = new SupportWrap1CreationLogic(console);
            supportWrap1CreationLogic.Execute(supportCreationContext);
            stopwatch.Stop();
            trackingReport.Add($"SupportWrap1CreationLogic Wrap 1-{postfix}",
                StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));

            stopwatch.Restart();
            var biggerTubeIdsMesh = RhinoMeshConverter.ToIDSMesh(biggerTubeMesh);
            supportCreationContext.InputRoI = BooleansV2.PerformBooleanIntersection(
                console,
                inputRoIIdsMesh,
                biggerTubeIdsMesh);
            supportCreationContext.WrapRoI1 = BooleansV2.PerformBooleanIntersection(
                console,
                supportCreationContext.WrapRoI1, 
                biggerTubeIdsMesh);
            stopwatch.Stop();
            trackingReport.Add($"Implant Support Rescale Input RoI-{postfix}",
                StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));

            var supportRemainingPartCreationLogic = 
                new SupportRemainingPartCreationLogic(console);
            supportRemainingPartCreationLogic.Execute(supportCreationContext);
            foreach (var keyValuePair in supportCreationContext.TrackingInfo.TrackingParameters)
            {
                trackingReport.Add($"SupportRemainingPartCreationLogic {keyValuePair.Key}-{postfix}", keyValuePair.Value);
            }

            var stopwatchFixing = new Stopwatch();
            stopwatchFixing.Start();

            supportCreationContext.FixedFinalResult = 
                PerformFullyFixSupport(supportCreationContext.FinalResult);
            stopwatchFixing.Stop();
            trackingReport.Add($"Implant Support Fixing-{postfix}",
                StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));

            stopwatch.Restart();

            var smallerTubeIdsMesh = RhinoMeshConverter.ToIDSMesh(smallerTubeMesh);
            supportCreationContext.SmallerRoI = BooleansV2.PerformBooleanIntersection(
                console,
                supportCreationContext.FixedFinalResult,
                smallerTubeIdsMesh);
            stopwatch.Stop();
            trackingReport.Add($"Implant Support Smaller Final RoI-{postfix}",
                StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));

            return supportCreationContext;
        }

        private static IMesh PerformFullyFixSupport(IMesh rawSupportMesh)
        {
            var console = new IDSRhinoConsole();
            const int maxFixingIteration = 2;
            try
            {
                var timer = new Stopwatch();
                timer.Start();
                var resultantMesh = MeshFixingUtilitiesV2.PerformComplexFullyFix(console,
                    rawSupportMesh, maxFixingIteration,
                    ComplexFixingParameters.ComplexSharpTriangleWidthThreshold,
                    ComplexFixingParameters.ComplexSharpTriangleAngleThreshold);
                timer.Stop();
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic,
                    $"It took {timer.ElapsedMilliseconds * 0.001} seconds to fix support.");

                return resultantMesh;
            }
            catch (Exception ex)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"Error: {ex.Message}");
                return rawSupportMesh;
            }
        }
        #endregion
    }
}
