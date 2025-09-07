using IDS.CMF;
using IDS.CMF.AttentionPointer;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Relations;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("31B9DD65-B214-404B-BCAA-6896B6483764")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant)]
    public class CMFPastillePreview : CmfCommandBase
    {
        public CMFPastillePreview()
        {
            TheCommand = this;
            var visualizationComponent = new CMFImplantPreviewVisualization();
            visualizationComponent.PastilleOnly = true;
            VisualizationComponent = visualizationComponent;
        }

        public static CMFPastillePreview TheCommand { get; private set; }

        public override string EnglishName => CommandEnglishName.CMFPastillePreview;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (!PhaseChanger.IsAllImplantsAndGuidesAreNumbered(director))
            {
                return Result.Failure;
            }

            // Sync before create pastille and implant preview
            var propertyHandler = new PropertyHandler(director);
            propertyHandler.SyncOutOfSyncProperties();

            var parameter = ImplantCreatorHelper.CreateImplantCreatorParams(director);

            var allCasesHaveNoSupport = parameter.SupportMeshRoIs.All(x => (x.SupportType == SupportType.None));
            if (allCasesHaveNoSupport)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Missing data: Either ImplantSupport or PatchSupport is required to run this command.");
                return Result.Failure;
            }

            var conflictingSupportCases = parameter.SupportMeshRoIs.Where(x => (x.SupportType == SupportType.Both)).ToList();
            if (conflictingSupportCases.Any())
            {
                var conflictingCaseNames = string.Join(", ", conflictingSupportCases.Select(x => x.CPrefDataModel.CaseName));
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Cases with conflicting support types (both Implant Support and Patch Support): {conflictingCaseNames}");
                return Result.Failure;
            }

            var pastilleCreator = new PastilleCreator(director)
            {
                IsCreateActualPastille = false,
                IsUsingV2Creator = true,
                NumberOfTasks = 2,
            };
            var success = pastilleCreator.GenerateMissingPastillePreviews(parameter);
            UpdateTrackingInfo(pastilleCreator.TrackingInfo);

            AddTrackingInfo(pastilleCreator, parameter);
            var triangleRoIsSummary = new List<string>();

            parameter.SupportMeshRoIs.ForEach(x =>
            {
                if (!x.SupportMeshIsNull())
                {
                    triangleRoIsSummary.Add($"ROI for {x.CPrefDataModel.CaseName}: {x.SupportMesh.Faces.Count.ToString("n0")} triangles");
                    x.SupportMesh.Dispose();
                }
            });

            if (pastilleCreator.SuccessfulPastilles.Any())
            {
                var message = "Created pastilles : " + string.Join(",", pastilleCreator.SuccessfulPastilles);
                IDSPluginHelper.WriteLine(LogCategory.Default, message);
            }
            if (pastilleCreator.UnsuccessfulPastilles.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Something went wrong during pastilles creation. Please refer Help website (https://home.materialise.net/sites/Materialise%20Software/Implant%20Design%20Suite/General%20Documents/Website/IDS/FAQ.html) on how to proceed.");

                var message = "Failed pastilles : " + string.Join(",", pastilleCreator.UnsuccessfulPastilles);
                IDSPluginHelper.WriteLine(LogCategory.Error, message);
            }
            if (pastilleCreator.SkippedPastilles.Any())
            {
                var message = "Skipped pastilles : " + string.Join(",", pastilleCreator.SkippedPastilles) + ", because it is already created.";
                IDSPluginHelper.WriteLine(LogCategory.Default, message);
            }

            var generatedPreviews = pastilleCreator.GeneratedPastilles;
            
            if (generatedPreviews.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Pastille Preview Summary");
                var errorSummary = new List<string>();

                // Generate summary for processing time summary
                IDSPluginHelper.WriteLine(LogCategory.Default, "> Processing Time");
                var helper = new PastillePreviewHelper(director);
                generatedPreviews.ForEach(x =>
                {
                    helper.AddPastillePreviewBuildingBlock(x.Key, x.Value.Item1);
                    IDSPluginHelper.WriteLine(LogCategory.Default, $"  > {x.Key.CaseName} took {x.Value.Item2.ToInvariantCultureString()} seconds to create");

                    if (pastilleCreator.ErrorMessages.Any(y => y.Key == x.Key))
                    {
                        var status = $"Pastille creation for {x.Key.CaseName} success with error messages:\n";
                        var errorMessages = pastilleCreator.ErrorMessages.First(y => y.Key == x.Key).Value;
                        errorSummary.Add($"{status}{string.Join("\n", errorMessages)}");
                    }
                });
                
                // Generate summary for number of triangle for ROI
                if (triangleRoIsSummary.Any())
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, "> Mesh Sizes");
                    triangleRoIsSummary.ForEach(triangRoISumm =>
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Default, $"  > {triangRoISumm}");
                    });
                }

                // Generate summary for error
                if (errorSummary.Any())
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, "> Failed Pastille");
                    errorSummary.ForEach(errSumm =>
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Default, $"  > {errSumm}");
                    });
                }
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "No pastille preview summary has been generated");
            }

            PastilleAttentionPointer.Instance.HighlightAndRefreshDeformedPastille(director);

            doc.Views.Redraw();

            return success ? Result.Success : Result.Failure;
        }

        private void RecheckDeformedPastilleInExistingScrewQc(CMFImplantDirector director)
        {
            if (director.ImplantScrewQcLiveUpdateHandler == null)
            {
                return;
            }

            var screwQcCheckManager =
                new ScrewQcCheckerManager(director, new[] { new PastilleDeformedChecker(director) });
            var screwManager = new ScrewManager(director);
            var implantScrews = screwManager.GetAllScrews(false);
            director.ImplantScrewQcLiveUpdateHandler.RecheckCertainResult(screwQcCheckManager, implantScrews);
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            base.OnCommandExecuteSuccess(doc, director);
            RecheckDeformedPastilleInExistingScrewQc(director);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            base.OnCommandExecuteFailed(doc, director);
            RecheckDeformedPastilleInExistingScrewQc(director);
        }

        private void AddTrackingInfo(PastilleCreator creator, ImplantCreatorParams parameters)
        {
            var fullySuccess = creator.SuccessfulPastilles.Where(x => !creator.ErrorMessages.Any(y => y.Key.CaseName == x));
            foreach (var implantName in fullySuccess)
            {
                TrackingParameters.Add($"{implantName} Pastille Creation", "Success");
            }

            foreach (var errorMessage in creator.ErrorMessages)
            {
                TrackingParameters.Add($"{errorMessage.Key.CaseName} Pastille Creation", "Success with error messages");
            }

            foreach (var implantName in creator.SkippedPastilles)
            {
                TrackingParameters.Add($"{implantName} Pastille Creation", "Skipped");
            }

            foreach (var implantName in creator.UnsuccessfulPastilles)
            {
                TrackingParameters.Add($"{implantName} Pastille Creation", "Failed");
            }

            for (var i = 0; i < parameters.AllCasePreferenceDataModels.Count(); ++i)
            {
                var model = parameters.AllCasePreferenceDataModels.ElementAt(i);
                var casePrefData = model.CasePrefData;
                var implantDataModel = model.ImplantDataModel;
                TrackingParameters.Add($"{model.CaseName} PlateThicknessMm", casePrefData.PlateThicknessMm.ToString());
                TrackingParameters.Add($"{model.CaseName} N Pastilles", implantDataModel.DotList.Count(x => x is DotPastille).ToString());
                TrackingParameters.Add($"{model.CaseName} N Landmarks",
                    implantDataModel.DotList.Where(x => x is DotPastille).Count(x => ((DotPastille)x).Landmark != null).ToString());

                var found = parameters.SupportMeshRoIs.Find(x => x.CPrefDataModel == model);

                if (!found.SupportMeshIsNull())
                {
                    TrackingParameters.Add($"{model.CaseName} Support Mesh N Triangles", found.SupportMesh.Faces.Count.ToString());
                    TrackingParameters.Add($"{model.CaseName} Support Mesh N Vertices", found.SupportMesh.Vertices.Count.ToString());
                }
            }
        }
    }
}
