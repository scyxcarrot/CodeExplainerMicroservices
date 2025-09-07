using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.V2.ExternalTools;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class PastilleCreator
    {
        public List<KeyValuePair<CasePreferenceDataModel, Tuple<List<PastilleCreationResult>, double>>> GeneratedPastilles { get; private set; }
        public List<KeyValuePair<CasePreferenceDataModel, List<string>>> ErrorMessages { get; private set; }

        public List<string> SuccessfulPastilles { get; set; } = new List<string>();
        public List<string> UnsuccessfulPastilles { get; set; } = new List<string>();
        public List<string> SkippedPastilles { get; set; } = new List<string>();

        public bool IsCreateActualPastille { get; set; }
        public bool IsUsingV2Creator { get; set; }
        public MsaiTrackingInfo TrackingInfo { get; }

        public int NumberOfTasks { get; set; } = 2;

        private readonly CMFImplantDirector _director;
        public PastilleCreator(CMFImplantDirector director)
        {
            this._director = director;
            IsCreateActualPastille = false;
            IsUsingV2Creator = true;
            GeneratedPastilles = new List<KeyValuePair<CasePreferenceDataModel, Tuple<List<PastilleCreationResult>, double>>>();
            ErrorMessages = new List<KeyValuePair<CasePreferenceDataModel, List<string>>>();

            var console = new IDSRhinoConsole();
            TrackingInfo = new MsaiTrackingInfo(console);
        }

        private bool GeneratePastillePreview(Mesh shellMesh, Mesh supportMeshFull, CasePreferenceDataModel casePreferenceData, List<DotPastille> dotPastilles,
            OverallImplantParams overallImplantParams, IndividualImplantParams individualImplantParams, LandmarkImplantParams landmarkImplantParams, IEnumerable<Screw> screws,
            long preElapsedMilliseconds)
        {
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Start generating Pastille only {casePreferenceData.CaseName}");
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"There are {dotPastilles.Count} missing Pastille(s)");

            var timer = new Stopwatch();
            timer.Start();

            var liveCasePref = _director.CasePrefManager.CasePreferences.FirstOrDefault(x => x.CaseGuid == casePreferenceData.CaseGuid);
            var liveDotPastilles = liveCasePref.ImplantDataModel.DotList.ToList();

            var resultList = new List<PastilleCreationResult>();
            var errorMessages = new List<string>();

            if (IsUsingV2Creator)
            {
                var creator = new InternalPastilleCreatorV2()
                {
                    NumberOfTasks = NumberOfTasks,
                    TrackingInfo = TrackingInfo
                };
                resultList = creator.GeneratePastilleWithFinalization(
                    shellMesh, 
                    supportMeshFull, 
                    casePreferenceData, 
                    dotPastilles, 
                    screws, 
                    IsCreateActualPastille);
                var pastilleErrorMessages
                    = resultList.SelectMany(result => result.ErrorMessages);
                errorMessages.AddRange(pastilleErrorMessages);
            }
            else
            {
                var creator = new InternalPastilleCreatorV1()
                {
                    TrackingInfo = TrackingInfo
                };
                resultList = creator.GeneratePastillePreview(shellMesh, supportMeshFull, casePreferenceData, dotPastilles,
                overallImplantParams, individualImplantParams, landmarkImplantParams, screws,
                IsCreateActualPastille, ref errorMessages);
            }

            foreach (var pastille in dotPastilles)
            {
                var currPastille = pastille;
                var result = resultList.Where(r => r.DotPastilleId == currPastille.Id).FirstOrDefault();
                if (result != null)
                {
                    var prevAlgo = result.PreviousCreationAlgoMethod;
                    if (currPastille.CreationAlgoMethod != prevAlgo)
                    {
                        var thatPastille = ImplantCreationUtilities.FindClosestDotPastille(liveDotPastilles,
                            RhinoPoint3dConverter.ToPoint3d(currPastille.Location));
                        thatPastille.CreationAlgoMethod = currPastille.CreationAlgoMethod;
                    }
                }
            }

            timer.Stop();

            if (errorMessages.Any())
            {
                ErrorMessages.Add(new KeyValuePair<CasePreferenceDataModel, List<string>>(casePreferenceData, errorMessages));
            }

            var timeTaken = (preElapsedMilliseconds + timer.ElapsedMilliseconds) * 0.001;
            GeneratedPastilles.Add(new KeyValuePair<CasePreferenceDataModel, Tuple<List<PastilleCreationResult>, double>>(casePreferenceData, new Tuple<List<PastilleCreationResult>, double>(resultList, timeTaken)));

            return true;
        }

        public bool GenerateMissingPastillePreviews(ImplantCreatorParams parameter)
        {
            var helper = new PastillePreviewHelper(_director);
            Func<CasePreferenceDataModel, Mesh, Mesh, IEnumerable<Screw>, double, List<DotPastille>> getMissingPastilles =
                (casePreferenceData, supportMesh, supportMeshFull, screws, pastillePlacementModifier)
                => helper.GetMissingPastillePreviews(casePreferenceData, supportMesh, screws, pastillePlacementModifier);
            return GeneratePastillePreviews(getMissingPastilles, parameter);
        }

        private bool GeneratePastillePreviews(Func<CasePreferenceDataModel, Mesh, Mesh, IEnumerable<Screw>, double, List<DotPastille>> getMissingPastilles, ImplantCreatorParams parameter)
        {
            SuccessfulPastilles = new List<string>();
            UnsuccessfulPastilles = new List<string>();
            SkippedPastilles = new List<string>();

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

                if (found.MissingImplantSupportRhObj() || found.ContainOutdatedImplantSupport())
                {
                    UnsuccessfulPastilles.Add(casePreferenceData.CaseName);
                    continue;
                }

                var supportMesh = found.SupportMesh;

                if (!supportMesh.FaceNormals.Any())
                {
                    supportMesh.FaceNormals.ComputeFaceNormals();
                }

                var timer = new Stopwatch();
                timer.Start();

                bool isPatchSupport = found.SupportType == SupportType.PatchSupport;

                if (isPatchSupport)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Processing patch support for {casePreferenceData.CaseName}");

                    var dotPastilles = getMissingPastilles(casePreferenceData, supportMesh, found.SupportMeshFull,
                        parameter.AllScrewsAvailable, parameters.IndividualImplantParams.PastillePlacementModifier);

                    if (!dotPastilles.Any())
                    {
                        SkippedPastilles.Add(casePreferenceData.CaseName);
                        timer.Stop();
                        continue;
                    }

                    try
                    {
                        timer.Stop();
                        var preElapsedMilliseconds = timer.ElapsedMilliseconds;

                        var successfulPatchPastille = ProcessPatchSupportPastilles(found, casePreferenceData, dotPastilles, 
                            parameter.AllScrewsAvailable, parameters, preElapsedMilliseconds);
                        
                        generated = generated && successfulPatchPastille;

                        if (successfulPatchPastille)
                        {
                            SuccessfulPastilles.Add(casePreferenceData.CaseName);
                        }
                        else
                        {
                            UnsuccessfulPastilles.Add(casePreferenceData.CaseName);
                        }
                    }
                    catch (Exception e)
                    {
                        UnsuccessfulPastilles.Add(casePreferenceData.CaseName);
                        Msai.TrackException(e, "CMF");

                        if (e is MtlsException exception)
                        {
                            IDSPluginHelper.WriteLine(LogCategory.Error,
                                $"Operation {exception.OperationName} failed to complete.");
                        }

                        IDSPluginHelper.WriteLine(LogCategory.Error,
                            $"The following unknown exception was thrown. Please report this to the development team.\n{e}");
                    }
                }
                else
                {
                    var shellMesh = ImplantCreationUtilities.FindSupportShell(casePreferenceData.ImplantDataModel,
                        supportMesh, parameter.AllScrewsAvailable);

                    var dotPastilles = getMissingPastilles(casePreferenceData, shellMesh, found.SupportMeshFull,
                        parameter.AllScrewsAvailable, parameters.IndividualImplantParams.PastillePlacementModifier);

                    if (!dotPastilles.Any())
                    {
                        SkippedPastilles.Add(casePreferenceData.CaseName);
                        timer.Stop();
                        continue;
                    }

                    try
                    {
                        timer.Stop();
                        var preElapsedMilliseconds = timer.ElapsedMilliseconds;

                        var successful = GeneratePastillePreview(shellMesh, found.SupportMeshFull,
                            casePreferenceData,
                            dotPastilles,
                            parameters.OverallImplantParams, parameters.IndividualImplantParams,
                            parameters.LandmarkImplantParams, parameter.AllScrewsAvailable, preElapsedMilliseconds);
                        generated = generated && successful;

                        if (successful)
                        {
                            SuccessfulPastilles.Add(casePreferenceData.CaseName);
                        }
                        else
                        {
                            UnsuccessfulPastilles.Add(casePreferenceData.CaseName);
                        }
                    }
                    catch (Exception e)
                    {
                        UnsuccessfulPastilles.Add(casePreferenceData.CaseName);
                        Msai.TrackException(e, "CMF");

                        if (e is MtlsException exception)
                        {
                            IDSPluginHelper.WriteLine(LogCategory.Error,
                                $"Operation {exception.OperationName} failed to complete.");
                        }

                        IDSPluginHelper.WriteLine(LogCategory.Error,
                            $"The following unknown exception was thrown. Please report this to the development team.\n{e}");
                    }
                }
            }

            return generated;
        }

        private bool ProcessPatchSupportPastilles(ImplantCreatorHelper.ImplantCreatorParamsSupportData supportData, 
            CasePreferenceDataModel casePreferenceData, List<DotPastille> dotPastilles, 
            IEnumerable<Screw> screws, ActualImplantParams parameters, long preElapsedMilliseconds)
        {
            var pastilleGroups = GroupPastillesByPatch(supportData.PatchSupportDataList, dotPastilles);
            bool allSuccessful = true;
            var allResults = new List<PastilleCreationResult>();
            var allErrors = new List<string>();

            foreach (var group in pastilleGroups)
            {
                if (!group.Value.Any())
                    continue;

                var patchData = group.Key;
                List<DotPastille> patchDotPastilles = group.Value;

                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Processing {patchDotPastilles.Count} pastilles for patch {patchData.PatchSupportId}");

                var patchMesh = patchData.SmallerConstraintMesh;
                
                if (!patchMesh.FaceNormals.Any())
                {
                    patchMesh.FaceNormals.ComputeFaceNormals();
                }

                var patchShellMesh = ImplantCreationUtilities.FindSupportShell(casePreferenceData.ImplantDataModel,
                    patchMesh, screws);

                var successful = GeneratePastillePreviewForPatch(patchShellMesh, supportData.SupportMeshFull,
                    casePreferenceData, patchDotPastilles, parameters.OverallImplantParams, 
                    parameters.IndividualImplantParams, parameters.LandmarkImplantParams, 
                    screws, preElapsedMilliseconds, allResults, allErrors);

                allSuccessful = allSuccessful && successful;
            }

            if (allResults.Any())
            {
                var timeTaken = preElapsedMilliseconds * 0.001;
                GeneratedPastilles.Add(new KeyValuePair<CasePreferenceDataModel, Tuple<List<PastilleCreationResult>, double>>(
                    casePreferenceData, new Tuple<List<PastilleCreationResult>, double>(allResults, timeTaken)));
            }

            if (allErrors.Any())
            {
                ErrorMessages.Add(new KeyValuePair<CasePreferenceDataModel, List<string>>(casePreferenceData, allErrors));
            }

            return allSuccessful;
        }

        private bool GeneratePastillePreviewForPatch(Mesh shellMesh, Mesh supportMeshFull, CasePreferenceDataModel casePreferenceData, 
            List<DotPastille> dotPastilles, OverallImplantParams overallImplantParams, IndividualImplantParams individualImplantParams, 
            LandmarkImplantParams landmarkImplantParams, IEnumerable<Screw> screws, long preElapsedMilliseconds,
            List<PastilleCreationResult> allResults, List<string> allErrors)
        {
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Start generating Pastille for patch - {dotPastilles.Count} pastille(s)");

            var timer = new Stopwatch();
            timer.Start();

            var liveCasePref = _director.CasePrefManager.CasePreferences.FirstOrDefault(x => x.CaseGuid == casePreferenceData.CaseGuid);
            var liveDotPastilles = liveCasePref.ImplantDataModel.DotList.ToList();

            var resultList = new List<PastilleCreationResult>();
            var errorMessages = new List<string>();

            if (IsUsingV2Creator)
            {
                var creator = new InternalPastilleCreatorV2()
                {
                    NumberOfTasks = NumberOfTasks,
                    TrackingInfo = TrackingInfo
                };
                resultList = creator.GeneratePastilleWithFinalization(
                    shellMesh, 
                    supportMeshFull, 
                    casePreferenceData, 
                    dotPastilles, 
                    screws, 
                    IsCreateActualPastille);
                var pastilleErrorMessages = resultList.SelectMany(result => result.ErrorMessages);
                errorMessages.AddRange(pastilleErrorMessages);
            }
            else
            {
                var creator = new InternalPastilleCreatorV1()
                {
                    TrackingInfo = TrackingInfo
                };
                resultList = creator.GeneratePastillePreview(shellMesh, supportMeshFull, casePreferenceData, dotPastilles,
                overallImplantParams, individualImplantParams, landmarkImplantParams, screws,
                IsCreateActualPastille, ref errorMessages);
            }

            foreach (var pastille in dotPastilles)
            {
                var currPastille = pastille;
                var result = resultList.Where(r => r.DotPastilleId == currPastille.Id).FirstOrDefault();
                if (result != null)
                {
                    var prevAlgo = result.PreviousCreationAlgoMethod;
                    if (currPastille.CreationAlgoMethod != prevAlgo)
                    {
                        var thatPastille = ImplantCreationUtilities.FindClosestDotPastille(liveDotPastilles,
                            RhinoPoint3dConverter.ToPoint3d(currPastille.Location));
                        thatPastille.CreationAlgoMethod = currPastille.CreationAlgoMethod;
                    }
                }
            }

            timer.Stop();

            allResults.AddRange(resultList);
            allErrors.AddRange(errorMessages);

            return true;
        }

        private Dictionary<PatchSupportData, List<DotPastille>> GroupPastillesByPatch(List<PatchSupportData> patchSupportDataList, List<DotPastille> dotPastilles)
        {
            var result = new Dictionary<PatchSupportData, List<DotPastille>>();
            
            foreach (var patchData in patchSupportDataList)
            {
                result[patchData] = new List<DotPastille>();
            }

            foreach (var pastille in dotPastilles)
            {
                PatchSupportData closestPatchData = null;
                double minDistance = double.MaxValue;
                var pastillePoint = RhinoPoint3dConverter.ToPoint3d(pastille.Location);

                foreach (var patchData in patchSupportDataList)
                {
                    var patchMesh = patchData.SmallerConstraintMesh;
                    
                    var cp = patchMesh.ClosestPoint(pastillePoint);
                    var distance = cp.DistanceTo(pastillePoint);
                    
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPatchData = patchData;
                    }
                }

                if (closestPatchData != null)
                {
                    result[closestPatchData].Add(pastille);
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Pastille {pastille.Id} assigned to patch {closestPatchData.PatchSupportId} (distance: {minDistance:F2})");
                }
            }

            return result;
        }
    }
}
