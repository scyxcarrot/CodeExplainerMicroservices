using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.ExternalTools;
using IDS.Interface.Implant;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class ConnectionCreationResult
    {
        public Mesh FinalConnection { get; private set; }

        public Mesh IntermediateConnection { get; private set; }

        public List<IDot> Dots { get; private set; }

        public ConnectionCreationResult(Mesh finalConnection, Mesh intermediateConnection, List<IDot> dots)
        {
            FinalConnection = finalConnection;
            IntermediateConnection = intermediateConnection;
            Dots = dots;
        }
    }

    public class ConnectionCreator
    {
        public int NumberOfTasks { get; set; }

        public List<KeyValuePair<CasePreferenceDataModel, Tuple<List<ConnectionCreationResult>, double>>> GeneratedConnections { get; private set; }
        public List<KeyValuePair<CasePreferenceDataModel, List<string>>> ErrorMessages { get; private set; }

        public List<string> SuccessfulConnections { get; set; } = new List<string>();
        public List<string> UnsuccessfulConnections { get; set; } = new List<string>();
        public List<string> SkippedConnections { get; set; } = new List<string>();

        public bool IsCreateActualConnection { get; set; }
        public bool IsUsingV2Creator { get; set; }
        public MsaiTrackingInfo TrackingInfo { get; }

        private readonly CMFImplantDirector _director;

        public ConnectionCreator(CMFImplantDirector director) : this(director, CreateTrackingInfo())
        {

        }

        public ConnectionCreator(CMFImplantDirector director, MsaiTrackingInfo trackingInfo)
        {
            _director = director;
            IsCreateActualConnection = false;
            GeneratedConnections = new List<KeyValuePair<CasePreferenceDataModel, Tuple<List<ConnectionCreationResult>, double>>>();
            ErrorMessages = new List<KeyValuePair<CasePreferenceDataModel, List<string>>>();
            TrackingInfo = trackingInfo;
        }

        private static MsaiTrackingInfo CreateTrackingInfo()
        {
            var console = new IDSRhinoConsole();
            return new MsaiTrackingInfo(console);
        }

        public bool GenerateAllConnections(ImplantDataModel implantDataModel, CasePreferenceDataModel casePreferencesData,
            IndividualImplantParams individualImplantParams, Mesh supportMesh, Mesh supportMeshFull, IEnumerable<Screw> screws, out List<Mesh> implantSurfaces)
        {
            var connectionList = implantDataModel.ConnectionList;

            List<DotCurveDataModel> dataModels;
            if (IsUsingV2Creator)
            {
                dataModels = ImplantCreationUtilities
                    .CreateImplantConnectionCurveDataModelsV2(connectionList);
            }
            else
            {
                dataModels = ImplantCreationUtilities
                    .CreateImplantConnectionCurveDataModels(connectionList);
            }

            var success = false;
            Dictionary<Mesh, List<IDot>> tubeDotMeshes;
            if (IsUsingV2Creator)
            {
                var creator = new InternalConnectionCreatorV2()
                { 
                    NumberOfTasks = NumberOfTasks,
                    TrackingInfo = TrackingInfo
                };
                success = creator.GenerateImplantTubes(
                    dataModels, casePreferencesData, supportMesh, supportMeshFull, screws,
                    IsCreateActualConnection, out tubeDotMeshes);
            }
            else
            {
                var creator = new InternalConnectionCreatorV1()
                {
                    TrackingInfo = TrackingInfo
                };
                success = creator.GenerateImplantTubes(
                    dataModels, casePreferencesData, 
                    individualImplantParams, supportMesh, 
                    supportMeshFull, screws,
                    IsCreateActualConnection, out tubeDotMeshes);
            }

            implantSurfaces = tubeDotMeshes
                .Select(t => t.Key).ToList();

            return success;
        }

        private bool GenerateConnection(Mesh supportMeshRoI, Mesh supportMeshRoIBigger, CasePreferenceDataModel casePreferenceData,
            OverallImplantParams overallImplantParams, IndividualImplantParams individualImplantParams,
            IEnumerable<Screw> screws, List<DotCurveDataModel> connectionDataModels)
        {
            if (!casePreferenceData.ImplantDataModel.ConnectionList.Any())
            {
                return true;
            }

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Start generating Connection only {casePreferenceData.CaseName}");
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"There are {connectionDataModels.Count} missing Connection(s)");

            var timer = new Stopwatch();
            timer.Start();

            //TODO [AH] later test to replace this with full Support Mesh, the RoI doesnt need to select shells by right.
            var shellMesh = ImplantCreationUtilities.FindSupportShell(casePreferenceData.ImplantDataModel, supportMeshRoI, screws);

            var success = false;
            Dictionary<Mesh, List<IDot>> tubeDotMeshes;
            if (IsUsingV2Creator)
            {
                var creator = new InternalConnectionCreatorV2()
                { 
                    NumberOfTasks = NumberOfTasks,
                    TrackingInfo = TrackingInfo
                };
                success = creator.GenerateImplantTubes(connectionDataModels, casePreferenceData, shellMesh, supportMeshRoIBigger, screws, IsCreateActualConnection, out tubeDotMeshes);
            }
            else
            {
                var creator = new InternalConnectionCreatorV1()
                {
                    TrackingInfo = TrackingInfo
                };
                success = creator.GenerateImplantTubes(connectionDataModels, casePreferenceData, individualImplantParams, shellMesh, supportMeshRoIBigger, screws, IsCreateActualConnection, out tubeDotMeshes);
            }

            if (!success)
            {
                Msai.TrackException(new IDSException("GenerateImplantTubes Failed"), "CMF");
                return false;
            }

            var connectionResults = new List<ConnectionCreationResult>();
            foreach (var tubeDotMesh in tubeDotMeshes)
            {
                var finalConnectionMesh = FinalizeImplant(shellMesh, tubeDotMesh.Key, casePreferenceData,
                    overallImplantParams, screws, out _, out _);

                connectionResults.Add(new ConnectionCreationResult(finalConnectionMesh, tubeDotMesh.Key, tubeDotMesh.Value));
            }

            timer.Stop();

            GeneratedConnections.Add(new KeyValuePair<CasePreferenceDataModel, Tuple<List<ConnectionCreationResult>, double>>(casePreferenceData, new Tuple<List<ConnectionCreationResult>, double>(connectionResults, timer.ElapsedMilliseconds * 0.001)));

            return true;
        }

        private Mesh FinalizeImplant(Mesh shellMesh, Mesh implantSurface, CasePreferenceDataModel casePreferenceData,
            OverallImplantParams overallImplantParams, IEnumerable<Screw> screws, out Mesh implantMesh, out Mesh implantSubtractedWithSupportMesh)
        {
            var wrapRatio = overallImplantParams.WrapOperationOffset;
            var componentSmallestDetail = overallImplantParams.WrapOperationSmallestDetails;
            var componentGapClosingDistance = overallImplantParams.WrapOperationGapClosingDistance;
            if (!Wrap.PerformWrap(new[] { implantSurface }, componentSmallestDetail, componentGapClosingDistance, wrapRatio, false, true, false, false, out implantMesh))
            {
                throw new IDSException("wrapped implant plate failed.");
            }

            if (overallImplantParams.IsDoPostProcessing && IsCreateActualConnection)
            {
                implantMesh = ImplantCreationUtilities.RemeshAndSmoothImplant(implantMesh);
            }

            implantSubtractedWithSupportMesh = ImplantCreationUtilities.SubstractImplantWithSupport(implantMesh, shellMesh);

            var screwStamp = ImplantPastilleCreationUtilities.GetScrewStamps(screws, casePreferenceData);
            var finalImplant = ImplantPastilleCreationUtilities.SubstractPastilleWithScrew(implantSubtractedWithSupportMesh, screwStamp);

            var finalFilteredMesh = finalImplant;
            finalFilteredMesh.Faces.CullDegenerateFaces();
            if (finalFilteredMesh.DisjointMeshCount > 1)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning,
                    $"{casePreferenceData.CaseName} consists of more than 1 part." +
                    " IDS will choose the largest volume of shell as implant");

                finalFilteredMesh = finalFilteredMesh.SplitDisjointPieces().OrderBy(
                    MeshUtilities.ComputeTotalSurfaceArea).LastOrDefault();
            }

            return finalFilteredMesh;
        }

        public bool GenerateMissingConnectionPreviews(ImplantCreatorParams parameter)
        {
            var helper = new ConnectionPreviewHelper(_director);
            Func<CasePreferenceDataModel, Mesh, Mesh, IEnumerable<Screw>, double, List<DotCurveDataModel>> getMissingConnections =
                (casePreferenceData, supportMesh, supportMeshFull, screws, pastillePlacementModifier)
                => helper.GetMissingConnectionPreviewDotCurveDataModels(casePreferenceData, supportMesh, screws, pastillePlacementModifier, IsUsingV2Creator);
            return GenerateConnectionPreviews(getMissingConnections, parameter);
        }

        public static List<CasePreferenceDataModel> FindImplantWithMissingConnectionPreview(
            CMFImplantDirector director, bool isUsingV2Creator)
        {
            var res = new List<CasePreferenceDataModel>();

            var helper = new ConnectionPreviewHelper(director);
            var dataModels = director.CasePrefManager.CasePreferences;

            foreach (var casePreferenceDataModel in dataModels)
            {
                //this is an estimation
                var connectionList = casePreferenceDataModel.ImplantDataModel.ConnectionList.ToList();

                int nConnectionDataModels;
                if (isUsingV2Creator)
                {
                    nConnectionDataModels = ImplantCreationUtilities
                        .CreateImplantConnectionCurveDataModelsV2(connectionList)
                        .Count();
                }
                else
                {
                    nConnectionDataModels = ImplantCreationUtilities
                        .CreateImplantConnectionCurveDataModels(connectionList)
                        .Count();
                }

                var nConnectionMeshes = helper.GetIntermediateConnectionPreviews(casePreferenceDataModel).Count;

                if (nConnectionDataModels != nConnectionMeshes)
                {
                    res.Add(casePreferenceDataModel);
                }
            }

            return res;
        }

        private bool GenerateConnectionPreviews(Func<CasePreferenceDataModel, Mesh, Mesh, IEnumerable<Screw>, double, List<DotCurveDataModel>> getMissingConnections,
            ImplantCreatorParams parameter)
        {
            var parameters = CMFPreferences.GetActualImplantParameters();

            var generated = true;

            if (parameter.AllScrewsAvailable.ToList().Exists(x => x.Index == -1))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Please ensure the screw number(s) are set!");
            }

            ErrorUtilities.RemoveErrorAnalysisLayerIfExist();

            foreach (var casePreferenceData in parameter.AllCasePreferenceDataModels)
            {
                if (!casePreferenceData.ImplantDataModel.ConnectionList.Any())
                {
                    continue;
                }

                var found = parameter.SupportMeshRoIs.Find(x => x.CPrefDataModel == casePreferenceData);

                if (found.MissingImplantSupportRhObj() || 
                    found.ContainOutdatedImplantSupport())
                {
                    UnsuccessfulConnections.Add(casePreferenceData.CaseName);
                    continue;
                }

                var supportMeshRoI = found.SupportMesh;

                if (!supportMeshRoI.FaceNormals.Any())
                {
                    supportMeshRoI.FaceNormals.ComputeFaceNormals();
                }

                //TODO [AH] need not to search from the ROI, but full instead.
                var shellMesh = ImplantCreationUtilities.FindSupportShell(casePreferenceData.ImplantDataModel,
                    supportMeshRoI, parameter.AllScrewsAvailable);

                        var dotConnections = getMissingConnections(casePreferenceData, shellMesh, found.SupportMeshFull,
                            parameter.AllScrewsAvailable, parameters.IndividualImplantParams.PastillePlacementModifier);

                        if (!dotConnections.Any())
                        {
                            SkippedConnections.Add(casePreferenceData.CaseName);
                            continue;
                        }

                        try
                        {
                            var successful = GenerateConnection(supportMeshRoI, found.SupportMeshBigger, casePreferenceData,
                                parameters.OverallImplantParams, parameters.IndividualImplantParams,
                                parameter.AllScrewsAvailable, dotConnections);
                            generated = generated && successful;

                            if (successful)
                            {
                                SuccessfulConnections.Add(casePreferenceData.CaseName);
                            }
                            else
                            {
                                UnsuccessfulConnections.Add(casePreferenceData.CaseName);
                            }

                        }
                        catch (Exception e)
                        {
                            UnsuccessfulConnections.Add(casePreferenceData.CaseName);
                            Msai.TrackException(e, "CMF");

                            if (e is MtlsException exception)
                            {
                                IDSPluginHelper.WriteLine(LogCategory.Error, $"Operation {exception.OperationName} failed to complete.");
                            }

                            IDSPluginHelper.WriteLine(LogCategory.Error, $"The following unknown exception was thrown. Please report this to the development team.\n{e}");
                        }
            }

            return generated;
        }
    }
}
