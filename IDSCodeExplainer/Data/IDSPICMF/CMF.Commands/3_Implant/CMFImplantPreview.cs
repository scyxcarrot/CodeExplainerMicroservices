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
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
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
    [System.Runtime.InteropServices.Guid("91A886F3-5F1B-4554-A1F2-7ED498AF4DB9")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant)]
    public class CMFImplantPreview : CmfCommandBase
    {
        public CMFImplantPreview()
        {
            TheCommand = this;
            VisualizationComponent = new CMFImplantPreviewVisualization();
        }

        /// The one and only instance of this command
        public static CMFImplantPreview TheCommand { get; private set; }

        /// The command name as it appears on the Rhino command line
        public override string EnglishName => CommandEnglishName.CMFImplantPreview;

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

            var pastillePreviewNotComplete = ImplantCreationUtilities.FindImplantWithMissingPastillePreview(director);
            if (pastillePreviewNotComplete.Any())
            {
                RhinoApp.RunScript($"_-CMFPastillePreview", true);

                pastillePreviewNotComplete = ImplantCreationUtilities.FindImplantWithMissingPastillePreview(director);

                if (pastillePreviewNotComplete.Any())
                {
                    var theOneThatMissings = pastillePreviewNotComplete.Select(x => x.CaseName).ToList();
                    var strMissing = string.Join(",", theOneThatMissings);
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Some pastille for {strMissing} are missing, please inspect and do necessary adjustments.");
                }
            }

            var connectionPreviewNotComplete = ConnectionCreator.FindImplantWithMissingConnectionPreview(director, true);
            if (connectionPreviewNotComplete.Any())
            {
                var connectionParameter = ImplantCreatorHelper.CreateImplantCreatorParams(director);
                var connectionCreator = new ConnectionCreator(director)
                {
                    IsCreateActualConnection = false,
                    IsUsingV2Creator = true,
                    NumberOfTasks = 2,
                };
                connectionCreator.GenerateMissingConnectionPreviews(connectionParameter);
                UpdateTrackingInfo(connectionCreator.TrackingInfo);

                var generatedConnections = connectionCreator.GeneratedConnections;
                if (generatedConnections.Any())
                {
                    var helper = new ConnectionPreviewHelper(director);
                    generatedConnections.ForEach(x =>
                    {
                        helper.AddConnectionPreviewBuildingBlocks(x.Key, x.Value.Item1);
                    });
                }

                connectionPreviewNotComplete = ConnectionCreator.FindImplantWithMissingConnectionPreview(director, true);

                if (connectionPreviewNotComplete.Any())
                {
                    var theOneThatMissings = connectionPreviewNotComplete.Select(x => x.CaseName).ToList();
                    var strMissing = string.Join(",", theOneThatMissings);
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Some connection for {strMissing} are missing, please inspect and do necessary adjustments.");
                }
            }

            var implantsToSkip = new List<CasePreferenceDataModel>();
            implantsToSkip.AddRange(pastillePreviewNotComplete);
            implantsToSkip.AddRange(connectionPreviewNotComplete);

            var implantParameter = ImplantCreatorHelper.CreateImplantCreatorParams(director);
            var implantCreator = new ImplantCreator(director)
            {
                IsCreateActualImplant = false,
                NumberOfTasks = 2,
            };
            var success = implantCreator.GenerateMissingImplantPreviews(implantParameter, implantsToSkip.Distinct().ToList());
            var triangleRoIsSummary = new List<string>();

            AddTrackingInfo(implantCreator, implantParameter);
            implantParameter.SupportMeshRoIs.ForEach(x =>
            {
                if (!x.SupportMeshIsNull())
                {
                    triangleRoIsSummary.Add($"ROI for {x.CPrefDataModel.CaseName}: {x.SupportMesh.Faces.Count.ToString("n0")} triangles");
                    x.SupportMesh?.Dispose();
                }
            });

            if (implantCreator.SuccessfulImplants.Any())
            {
                var message = "Created implants : " + string.Join(",", implantCreator.SuccessfulImplants.Select(x => x.Key));
                IDSPluginHelper.WriteLine(LogCategory.Default, message);
            }
            if (implantCreator.UnsuccessfulImplants.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Something went wrong during implant creation. Please refer Help website (https://home.materialise.net/sites/Materialise%20Software/Implant%20Design%20Suite/General%20Documents/Website/IDS/FAQ.html) on how to proceed.");

                var message = "Failed implants : " + string.Join(",", implantCreator.UnsuccessfulImplants.Select(x => x.Key));
                IDSPluginHelper.WriteLine(LogCategory.Error, message);
            }
            if (implantCreator.SkippedImplants.Any())
            {
                var message = "Skipped because pastille\\connection(s) are missing : " + string.Join(",", implantCreator.SkippedImplants.Select(x => x.Key));
                IDSPluginHelper.WriteLine(LogCategory.Default, message);
            }
            if (implantCreator.AlreadyExistImplants.Any())
            {
                var message = "Skipped because already created implants : " + string.Join(",", implantCreator.AlreadyExistImplants.Select(x => x.Key));
                IDSPluginHelper.WriteLine(LogCategory.Default, message);
            }

            var generatedPreviews = implantCreator.GeneratedImplants;

            var objectManager = new CMFObjectManager(director);
            var implantComponent = new ImplantCaseComponent();
            if (generatedPreviews.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Implant Preview Summary");

                // Generate summary for processing time summary
                IDSPluginHelper.WriteLine(LogCategory.Default, "> Processing Time");

                generatedPreviews.ForEach(x =>
                {
                    var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.ImplantPreview, x.Key);

                    var pastillePreviewIds = objectManager
                        .GetAllImplantExtendedImplantBuildingBlocksIDs(IBB.PastillePreview, x.Key);
                    var connectionPreviewIds = objectManager
                        .GetAllImplantExtendedImplantBuildingBlocksIDs(IBB.ConnectionPreview, x.Key);
                    var parentIds = pastillePreviewIds.Concat(connectionPreviewIds).ToList();
                    IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(objectManager, director.IdsDocument, buildingBlock, parentIds, x.Value.FinalImplant);

                    IDSPluginHelper.WriteLine(LogCategory.Default, $"  > {x.Key.CaseName} took {x.Value.TotalTime.ToInvariantCultureString()} seconds to create");
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
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "No implant preview summary has been generated");
            }

            PastilleAttentionPointer.Instance.HighlightAndRefreshDeformedPastille(director);

            doc.Views.Redraw();

            return success ? Result.Success : Result.Failure;
        }

        private void AddTrackingInfo(ImplantCreator creator, ImplantCreatorParams parameters)
        {
            if (creator.SuccessfulImplants.Any())
            {
                TrackingParameters.Add($"Success Implants", string.Join(",", creator.SuccessfulImplants.Select(x => x.Value)));
            }

            if (creator.UnsuccessfulImplants.Any())
            {
                TrackingParameters.Add($"Failed Implants", string.Join(",", creator.UnsuccessfulImplants.Select(x => x.Value)));
            }

            if (creator.AlreadyExistImplants.Any())
            {
                TrackingParameters.Add($"Skipped Implants", string.Join(",", creator.AlreadyExistImplants.Select(x => x.Value)));
            }

            for (var i = 0; i < parameters.AllCasePreferenceDataModels.Count(); ++i)
            {
                var model = parameters.AllCasePreferenceDataModels.ElementAt(i);
                var casePrefData = model.CasePrefData;
                var implantDataModel = model.ImplantDataModel;
                TrackingParameters.Add($"{model.CaseName} LinkWidthMm", casePrefData.LinkWidthMm.ToString());
                TrackingParameters.Add($"{model.CaseName} PlateWidthMm", casePrefData.PlateWidthMm.ToString());
                TrackingParameters.Add($"{model.CaseName} PlateThicknessMm", casePrefData.PlateThicknessMm.ToString());
                TrackingParameters.Add($"{model.CaseName} N Pastilles", implantDataModel.DotList.Count(x => x is DotPastille).ToString());
                TrackingParameters.Add($"{model.CaseName} N ControlPoints", implantDataModel.DotList.Count(x => x is DotControlPoint).ToString());
                TrackingParameters.Add($"{model.CaseName} N Landmarks",
                    implantDataModel.DotList.Where(x => x is DotPastille).Count(x => ((DotPastille)x).Landmark != null).ToString());

                var found = parameters.SupportMeshRoIs.Find(x => x.CPrefDataModel == model);

                TrackingParameters.Add($"{model.CaseName} N Connections", implantDataModel.ConnectionList.Count.ToString());

                if (!found.SupportMeshIsNull())
                {
                    TrackingParameters.Add($"{model.CaseName} Support Mesh N Triangles", found.SupportMesh.Faces.Count.ToString());
                    TrackingParameters.Add($"{model.CaseName} Support Mesh N Vertices", found.SupportMesh.Vertices.Count.ToString());

                    TrackingParameters.Add($"{model.CaseName} Support Mesh Full N Triangles", found.SupportMeshFull.Faces.Count.ToString());
                    TrackingParameters.Add($"{model.CaseName} Support Mesh Full N Vertices", found.SupportMeshFull.Vertices.Count.ToString());
                }
            }

        }
    }
}
