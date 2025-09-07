using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Constants;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.ExternalTools;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
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
    public class ImplantCreatorParams
    {
        public IEnumerable<Screw> AllScrewsAvailable { get; private set; }
        public List<ImplantCreatorHelper.ImplantCreatorParamsSupportData> SupportMeshRoIs { get; private set; }
        public IEnumerable<CasePreferenceDataModel> AllCasePreferenceDataModels { get; private set; }
        public IEnumerable<ImplantCreatorHelper.PastillePreviewIntermediateParamsData> PastillePreviewIntermediatesData
        {
            get;
            private set;
        }

        public IEnumerable<ImplantCreatorHelper.ConnectionPreviewIntermediateParamsData> ConnectionPreviewIntermediatesData
        {
            get;
            private set;
        }

        public ImplantCreatorParams(IEnumerable<Screw> allScrewsAvailable,
            List<ImplantCreatorHelper.ImplantCreatorParamsSupportData> supportMeshRoIs,
            IEnumerable<CasePreferenceDataModel> allCasePreferenceDataModels,
            IEnumerable<ImplantCreatorHelper.PastillePreviewIntermediateParamsData> pastillePreviewIntermediatesData,
            IEnumerable<ImplantCreatorHelper.ConnectionPreviewIntermediateParamsData> connectionPreviewIntermediatesData)
        {
            AllScrewsAvailable = allScrewsAvailable;
            SupportMeshRoIs = supportMeshRoIs;
            AllCasePreferenceDataModels = allCasePreferenceDataModels;
            PastillePreviewIntermediatesData = pastillePreviewIntermediatesData;
            ConnectionPreviewIntermediatesData = connectionPreviewIntermediatesData;
        }
    }

    public class ImplantCreationResult
    {
        public Mesh FinalImplant { get; private set; }

        public double FixingTime { get; private set; }

        public double TotalTime { get; private set; }

        public ImplantCreationResult(Mesh finalImplant, double fixingTime,
            double totalTime)
        {
            FinalImplant = finalImplant;
            FixingTime = fixingTime;
            TotalTime = totalTime;
        }
    }

    public class ImplantCreator
    {
        public List<KeyValuePair<CasePreferenceDataModel, Mesh>> PastilleCylinders { get; private set; }

        public List<KeyValuePair<CasePreferenceDataModel, ImplantCreationResult>> GeneratedImplants { get; private set; }
        public List<KeyValuePair<CasePreferenceDataModel, Mesh>> GeneratedImplantsWithoutStampSubtraction { get; private set; }
        public List<KeyValuePair<CasePreferenceDataModel, Mesh>> GeneratedImplantSurfaces { get; private set; }
        public List<KeyValuePair<CasePreferenceDataModel, Tuple<Mesh, Dictionary<string, double>>>> GeneratedImplantsImprintSubtractEntities { get; private set; }
        public List<KeyValuePair<CasePreferenceDataModel, Tuple<Mesh, Dictionary<string, double>>>> GeneratedImplantsScrewIndentationSubtractEntities { get; private set; }

        public List<KeyValuePair<string, string>> SuccessfulImplants { get; private set; } =
            new List<KeyValuePair<string, string>>();
        public List<KeyValuePair<string, string>> UnsuccessfulImplants { get; private set; } =
            new List<KeyValuePair<string, string>>();
        public List<KeyValuePair<string, string>> SkippedImplants { get; private set; } = new List<KeyValuePair<string, string>>();
        public List<KeyValuePair<string, string>> AlreadyExistImplants { get; private set; } =
            new List<KeyValuePair<string, string>>();
        public bool IsCreateActualImplant { get; set; }
        public bool IsUsingV2Creator { get; set; }
        public MsaiTrackingInfo TrackingInfo { get; private set; }

        private readonly CMFImplantDirector _director;

        public int NumberOfTasks { get; set; } = 2;

        public ImplantCreator(CMFImplantDirector director)
        {
            this._director = director;
            IsCreateActualImplant = false;
            IsUsingV2Creator = true;
            GeneratedImplants = new List<KeyValuePair<CasePreferenceDataModel, ImplantCreationResult>>();
            GeneratedImplantsWithoutStampSubtraction = new List<KeyValuePair<CasePreferenceDataModel, Mesh>>();
            GeneratedImplantSurfaces = new List<KeyValuePair<CasePreferenceDataModel, Mesh>>();
            GeneratedImplantsImprintSubtractEntities = new List<KeyValuePair<CasePreferenceDataModel, Tuple<Mesh, Dictionary<string, double>>>>();
            GeneratedImplantsScrewIndentationSubtractEntities = new List<KeyValuePair<CasePreferenceDataModel, Tuple<Mesh, Dictionary<string, double>>>>();
            PastilleCylinders = new List<KeyValuePair<CasePreferenceDataModel, Mesh>>();

            var console = new IDSRhinoConsole();
            TrackingInfo = new MsaiTrackingInfo(console);
        }

        private bool GenerateImplant(Mesh supportMeshRoI, Mesh supportMeshRoIBigger, Mesh supportMeshFull, CasePreferenceDataModel casePreferenceData,
            OverallImplantParams overallImplantParams, IndividualImplantParams individualImplantParams,
            LandmarkImplantParams landmarkImplantParams, IEnumerable<Screw> screws, IEnumerable<Mesh> implantPastillesIntermediateMeshes,
            IEnumerable<Mesh> pastilleCylinder, IEnumerable<Mesh> pastilleIntermediateLandmark, IEnumerable<Mesh> implantConnectionsIntermediateMeshes,
            ImplantCreatorHelper.ImplantCreatorParamsSupportData supportData)
        {
            if (!casePreferenceData.ImplantDataModel.ConnectionList.Any())
            {
                return true;
            }

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Start generating {casePreferenceData.CaseName}");

            var timer = new Stopwatch();
            timer.Start();

            // Determine shell mesh - this logic works for both support types
            Mesh shellMesh;
            if (supportData?.SupportType == SupportType.PatchSupport && supportData.PatchSupportDataList != null && supportData.PatchSupportDataList.Any())
            {
                if (screws != null && screws.Any())
                {
                    var relevantPatchSupports = new List<Mesh>();

                    foreach (var screw in screws)
                    {
                        foreach (var patchData in supportData.PatchSupportDataList)
                        {
                            var screwPoint = screw.HeadPoint;
                            var closestPoint = patchData.SmallerConstraintMesh.ClosestPoint(screwPoint);

                            if (closestPoint.DistanceTo(screwPoint) < 0.1)
                            {
                                relevantPatchSupports.Add(patchData.SmallerConstraintMesh);
                                break;
                            }
                        }
                    }

                    if (relevantPatchSupports.Any())
                    {
                        Booleans.PerformBooleanUnion(out shellMesh, relevantPatchSupports.ToArray());
                    }
                    else
                    {
                        var allSmallerMeshes = supportData.PatchSupportDataList.Select(p => p.SmallerConstraintMesh).ToArray();
                        Booleans.PerformBooleanUnion(out shellMesh, allSmallerMeshes);
                    }
                }
                else
                {
                    var relevantPatchSupports = new List<Mesh>();

                    foreach (var connection in casePreferenceData.ImplantDataModel.ConnectionList)
                    {
                        foreach (var patchData in supportData.PatchSupportDataList)
                        {
                            var connectionsForPatch = patchData.GetIntersectingConnections(casePreferenceData);
                            if (connectionsForPatch.Contains(connection))
                            {
                                relevantPatchSupports.Add(patchData.SmallerConstraintMesh);
                                break; // Each connection should only match one patch
                            }
                        }
                    }

                    if (relevantPatchSupports.Any())
                    {
                        Booleans.PerformBooleanUnion(out shellMesh, relevantPatchSupports.ToArray());
                    }
                    else
                    {
                        var allSmallerMeshes = supportData.PatchSupportDataList.Select(p => p.SmallerConstraintMesh).ToArray();
                        Booleans.PerformBooleanUnion(out shellMesh, allSmallerMeshes);
                    }
                }
            }
            else
            {
                shellMesh = ImplantCreationUtilities.FindSupportShell(casePreferenceData.ImplantDataModel, supportMeshRoI, screws);
            }

            var implantNum = casePreferenceData.NCase;
            var implant = ImplantPastilleCreationUtilities.AdjustPastilles(casePreferenceData.ImplantDataModel, shellMesh, screws, individualImplantParams.PastillePlacementModifier);

            var implantMeshes = new List<Mesh>();

            var connectionsIntermediateMeshes = implantConnectionsIntermediateMeshes.ToList();
            if (!connectionsIntermediateMeshes.Any())
            {
                var connectionCreator = new ConnectionCreator(_director, TrackingInfo)
                {
                    IsCreateActualConnection = IsCreateActualImplant,
                    IsUsingV2Creator = IsUsingV2Creator,
                    NumberOfTasks = NumberOfTasks,
                };

                if (!connectionCreator.GenerateAllConnections(implant, casePreferenceData, individualImplantParams, shellMesh, supportMeshRoIBigger, screws, out implantMeshes))
                {
                    Msai.TrackException(new IDSException("GenerateImplantTubes Failed"), "CMF");
                    return false;
                }
            }
            else
            {
                implantMeshes.AddRange(connectionsIntermediateMeshes);
            }

            var pastilleCylinders = MeshUtilities.AppendMeshes(pastilleCylinder);
            var implantPastilles = new List<Mesh>();
            var pastillesIntermediateMeshes = implantPastillesIntermediateMeshes.ToList();
            var landmarks = new List<Mesh>();
            var intermediateLandmark = pastilleIntermediateLandmark.ToList();

            if (!pastillesIntermediateMeshes.Any())
            {
                pastilleCylinders?.Dispose();
                if (IsUsingV2Creator)
                {
                    var pastilleCreator = new InternalPastilleCreatorV2()
                    {
                        NumberOfTasks = NumberOfTasks,
                        TrackingInfo = TrackingInfo
                    };
                    if (!pastilleCreator.GeneratePastilleWithoutFinalization(ref implant, casePreferenceData,
                            shellMesh, supportMeshFull, screws, out implantPastilles, out pastilleCylinders, out landmarks, IsCreateActualImplant))
                    {
                        Msai.TrackException(new IDSException("GenerateImplantPastillesAndLandmarks V2 Failed"), "CMF");
                        return false;
                    }
                }
                else
                {
                    if (!ImplantPastilleCreationUtilities.GenerateImplantPastillesAndLandmarks(ref implant, casePreferenceData, individualImplantParams, landmarkImplantParams,
                            shellMesh, supportMeshFull, screws, out implantPastilles, out pastilleCylinders, out landmarks))
                    {
                        Msai.TrackException(new IDSException("GenerateImplantPastillesAndLandmarks V1 Failed"), "CMF");
                        return false;
                    }
                }

                var secondAlgoIndexes = ImplantPastilleCreationUtilities.GetPastilleIndexesThatUsesNonPrimaryCreationAlgo(implant.DotList);
                if (secondAlgoIndexes.Any())
                {
                    var liveCasePref = _director.CasePrefManager.CasePreferences.FirstOrDefault(x => x.CaseGuid == casePreferenceData.CaseGuid);

                    secondAlgoIndexes.ForEach(i =>
                    {
                        var dotPastille = (DotPastille)liveCasePref.ImplantDataModel.DotList[i];
                        dotPastille.CreationAlgoMethod = DotPastille.CreationAlgoMethods[1];
                    });
                }
            }
            else
            {
                implantPastilles.AddRange(pastillesIntermediateMeshes);
                landmarks.AddRange(intermediateLandmark);
            }

            implantMeshes.AddRange(implantPastilles);
            PastilleCylinders.Add(new KeyValuePair<CasePreferenceDataModel, Mesh>(casePreferenceData, pastilleCylinders));
            implantMeshes.AddRange(landmarks);

            var implantSurface = MeshUtilities.AppendMeshes(implantMeshes);
            GeneratedImplantSurfaces.Add(new KeyValuePair<CasePreferenceDataModel, Mesh>(casePreferenceData, implantSurface));

            implantMeshes.Clear();
            landmarks.Clear();

            var finalFilteredMesh = FinalizeImplant(shellMesh, implantSurface, casePreferenceData,
                overallImplantParams, screws, casePreferenceData.NCase, out var implantMesh, out var implantSubtractedWithSupportMesh);

            GeneratedImplantsWithoutStampSubtraction.Add(new KeyValuePair<CasePreferenceDataModel, Mesh>(casePreferenceData, implantSubtractedWithSupportMesh));

            var fixingTime = 0.0;

            if (IsCreateActualImplant)
            {
                var fixingTimer = new Stopwatch();
                fixingTimer.Start();

                finalFilteredMesh = MeshFixingUtilities.PerformComplexFullyFix(finalFilteredMesh, overallImplantParams.FixingIterations,
                    ComplexFixingParameters.ComplexSharpTriangleWidthThreshold, ComplexFixingParameters.ComplexSharpTriangleAngleThreshold);

                fixingTimer.Stop();
                fixingTime = fixingTimer.ElapsedMilliseconds * 0.001;
            }

            timer.Stop();

            GeneratedImplants.Add(new KeyValuePair<CasePreferenceDataModel, ImplantCreationResult>(casePreferenceData, new ImplantCreationResult(finalFilteredMesh, fixingTime, timer.ElapsedMilliseconds * 0.001)));

            if (IsCreateActualImplant)
            {
                var trackingParameters = new Dictionary<string, double>();

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var imprintOutline = PlasticEntitiesCreatorUtilities.GenerateImplantImprintOutlines(implantMesh, shellMesh);
                stopwatch.Stop();
                trackingParameters.Add("GenerateImplantPlasticImprint", stopwatch.ElapsedMilliseconds * 0.001);

                var imprintSubtractionEntities = PlasticEntitiesCreatorUtilities.GenerateGeneralImprintSubtractionEntities(imprintOutline, shellMesh,
                    "Implant", ref trackingParameters);
                GeneratedImplantsImprintSubtractEntities.Add(new KeyValuePair<CasePreferenceDataModel, Tuple<Mesh, Dictionary<string, double>>>
                    (casePreferenceData, new Tuple<Mesh, Dictionary<string, double>>(MeshUtilities.AppendMeshes(imprintSubtractionEntities), trackingParameters)));

                // Need to get individual screw for it direction instead of using all in one screw stamp
                trackingParameters = new Dictionary<string, double>();
                stopwatch.Restart();
                var screwManager = new ScrewManager(_director);
                var screwsCurrentCase = screwManager.GetScrews(screws, casePreferenceData, false);
                var screwIndentationSubtractionEntities =
                    PlasticEntitiesCreatorUtilities.GenerateGeneralScrewIndentationSubtractionEntities(screwsCurrentCase, shellMesh);
                stopwatch.Stop();
                trackingParameters.Add("GenerateImplantPlasticScrewsIndentation", stopwatch.ElapsedMilliseconds * 0.001);

                GeneratedImplantsScrewIndentationSubtractEntities.Add(new KeyValuePair<CasePreferenceDataModel, Tuple<Mesh, Dictionary<string, double>>>(casePreferenceData,
                   new Tuple<Mesh, Dictionary<string, double>>(MeshUtilities.AppendMeshes(screwIndentationSubtractionEntities), trackingParameters)));
            }

            implantMesh.Dispose();

            return true;
        }

        Mesh FinalizeImplant(Mesh shellMesh, Mesh implantSurface, CasePreferenceDataModel casePreferenceData,
            OverallImplantParams overallImplantParams, IEnumerable<Screw> screws, int implantNum, out Mesh implantMesh, out Mesh implantSubtractedWithSupportMesh)
        {
            var wrapRatio = overallImplantParams.WrapOperationOffset;
            var componentSmallestDetail = overallImplantParams.WrapOperationSmallestDetails;
            var componentGapClosingDistance = overallImplantParams.WrapOperationGapClosingDistance;
            if (!Wrap.PerformWrap(new[] { implantSurface }, componentSmallestDetail, componentGapClosingDistance, wrapRatio, false, true, false, false, out implantMesh))
            {
                throw new IDSException("wrapped implant plate failed.");
            }

#if (INTERNAL)
            InternalUtilities.AddObject(implantMesh, $"wrappedImplant", $"Test Implant::Implant {implantNum}");
#endif

            if (overallImplantParams.IsDoPostProcessing && IsCreateActualImplant)
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

        public bool GenerateAllImplantPreviews(ImplantCreatorParams parameter)
        {
            Func<CasePreferenceDataModel, bool> needToGenerate = (casePreferenceData) => true;
            return GenerateImplantPreviews(needToGenerate, parameter);
        }

        public bool GenerateMissingActualImplant(ImplantCreatorParams parameter)
        {
            IsCreateActualImplant = true;
            Func<CasePreferenceDataModel, bool> needToGenerate = (casePreferenceData) => !HasActualImplant(casePreferenceData);
            return GenerateImplantPreviews(needToGenerate, parameter);
        }

        public bool GenerateMissingImplantPreviews(ImplantCreatorParams parameter, List<CasePreferenceDataModel> implantsToSkip)
        {
            Func<CasePreferenceDataModel, bool> needToGenerate = (casePreferenceData) =>
            {
                var alreadyExist = HasImplantPreview(casePreferenceData);
                if (alreadyExist)
                {
                    AlreadyExistImplants.Add(new KeyValuePair<string, string>(casePreferenceData.CaseName, casePreferenceData.CasePrefData.ImplantTypeValue));
                }
                return !alreadyExist && !implantsToSkip.Contains(casePreferenceData);
            };

            var generated = GenerateImplantPreviews(needToGenerate, parameter);

            AlreadyExistImplants.ForEach(i =>
            {
                SkippedImplants.Remove(i);
            });

            return generated;
        }

        private bool GenerateImplantPreviews(Func<CasePreferenceDataModel, bool> needToGenerate, ImplantCreatorParams parameter)
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
                if (!needToGenerate(casePreferenceData))
                {
                    SkippedImplants.Add(new KeyValuePair<string, string>(casePreferenceData.CaseName,
                        casePreferenceData.CasePrefData.ImplantTypeValue));
                    continue;
                }

                if (!casePreferenceData.ImplantDataModel.ConnectionList.Any())
                {
                    continue;
                }

                var foundPastilleData = parameter.PastillePreviewIntermediatesData.ToList().Find(x => x.CPrefDataModel.CaseGuid == casePreferenceData.CaseGuid);
                var foundConnectionData = parameter.ConnectionPreviewIntermediatesData.ToList().Find(x => x.CPrefDataModel.CaseGuid == casePreferenceData.CaseGuid);
                var found = parameter.SupportMeshRoIs.Find(x => x.CPrefDataModel == casePreferenceData);

                if (found.MissingImplantSupportRhObj() || found.ContainOutdatedImplantSupport())
                {
                    UnsuccessfulImplants.Add(new KeyValuePair<string, string>(casePreferenceData.CaseName,
                        casePreferenceData.CasePrefData.ImplantTypeValue));
                    continue;
                }

                var supportMeshRoI = found.SupportMesh;

                if (!supportMeshRoI.FaceNormals.Any())
                {
                    supportMeshRoI.FaceNormals.ComputeFaceNormals();
                }

                try
                {
                    var successful = IsCreateActualImplant ?
                        GenerateImplant(supportMeshRoI, found.SupportMeshBigger, found.SupportMeshFull, casePreferenceData,
                            parameters.OverallImplantParams, parameters.IndividualImplantParams, parameters.LandmarkImplantParams,
                            parameter.AllScrewsAvailable, new List<Mesh>(), new List<Mesh>(), new List<Mesh>(), new List<Mesh>(), found) :
                        GenerateImplant(supportMeshRoI, found.SupportMeshBigger, found.SupportMeshFull, casePreferenceData,
                            parameters.OverallImplantParams, parameters.IndividualImplantParams, parameters.LandmarkImplantParams,
                            parameter.AllScrewsAvailable, foundPastilleData.pastillePreviewIntermediates,
                            foundPastilleData.pastilleCylinder, foundPastilleData.pastillePreviewLandmarkIntermediates,
                            foundConnectionData.ConnectionPreviewIntermediates, found);

                    generated = generated && successful;

                    if (successful)
                    {
                        SuccessfulImplants.Add(new KeyValuePair<string, string>(casePreferenceData.CaseName, casePreferenceData.CasePrefData.ImplantTypeValue));
                    }
                    else
                    {
                        UnsuccessfulImplants.Add(new KeyValuePair<string, string>(casePreferenceData.CaseName, casePreferenceData.CasePrefData.ImplantTypeValue));
                    }

                }
                catch (Exception e)
                {
                    UnsuccessfulImplants.Add(new KeyValuePair<string, string>(casePreferenceData.CaseName, casePreferenceData.CasePrefData.ImplantTypeValue));
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

        public bool HasImplantPreview(CasePreferenceDataModel casePreferenceData)
        {
            return HasImplantBuildingBlock(casePreferenceData, IBB.ImplantPreview);
        }

        private bool HasActualImplant(CasePreferenceDataModel casePreferenceData)
        {
            return HasImplantBuildingBlock(casePreferenceData, IBB.ActualImplant);
        }

        private bool HasImplantBuildingBlock(CasePreferenceDataModel casePreferenceData, IBB block)
        {
            var objectManager = new CMFObjectManager(_director);
            var implantComponent = new ImplantCaseComponent();
            var implantBuildingBlock = implantComponent.GetImplantBuildingBlock(block, casePreferenceData);
            return objectManager.HasBuildingBlock(implantBuildingBlock.Block);
        }

        public int GetNumberOfMissingActualImplant(ImplantCreatorParams parameter)
        {
            var count = 0;

            foreach (var casePreferenceData in parameter.AllCasePreferenceDataModels)
            {
                if (!HasActualImplant(casePreferenceData) || !casePreferenceData.ImplantDataModel.ConnectionList.Any())
                {
                    count++;
                }
            }

            return count;
        }
    }
}
