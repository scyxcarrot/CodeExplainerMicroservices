using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.Visualization;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.UI;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class PostImplantSupportCreationHelper: PostSupportCreationHelper
    {
        private readonly CMFImplantDirector _director;

        private readonly Dictionary<CasePreferenceDataModel, SupportCreationDataModel> _fixedSupportDataModelsWithCase;

        private readonly Dictionary<CasePreferenceDataModel, AnalysisResult> _fixedSupportAnalysisResult;

        private bool _exportMeshOverlappingTriangleOnly;

        public PostImplantSupportCreationHelper(CMFImplantDirector director,
            Dictionary<CasePreferenceDataModel, SupportCreationDataModel> fixedSupportDataModelsWithCase)
        {
            _director = director;
            _fixedSupportDataModelsWithCase = fixedSupportDataModelsWithCase.ToDictionary(kv => kv.Key,
                kv => kv.Value);
            _fixedSupportAnalysisResult = new Dictionary<CasePreferenceDataModel, AnalysisResult>();
            _exportMeshOverlappingTriangleOnly = false;
        }

        public AnalysisResult AssignAnalysisResult(CasePreferenceDataModel casePreferenceData,
            MeshDiagnostics.MeshDiagnosticsResult results)
        {
            var result = AnalysisResult.Unknown;
            if (!_fixedSupportDataModelsWithCase.ContainsKey(casePreferenceData))
            {
                return result;
            }

            result = GetAnalysisResult(results);

            _fixedSupportAnalysisResult.Add(casePreferenceData, result);
            return result;
        }
        public void ConfirmationToExportOverlappingTriangle()
        {
            var overlappingTriangleOnlySupportMeshes = new List<Mesh>();
            foreach (var analysisResult in _fixedSupportAnalysisResult)
            {
                if (analysisResult.Value != AnalysisResult.OverlappingTriangleOnly)
                {
                    continue;
                }
                overlappingTriangleOnlySupportMeshes.Add(_fixedSupportDataModelsWithCase[analysisResult.Key].FixedFinalResult);
            }

            if (!overlappingTriangleOnlySupportMeshes.Any())
            {
                return;
            }

            var conduits = new List<OverlappingTriangleConduit>();
            foreach (var overlappingTriangleOnlySupportMesh in overlappingTriangleOnlySupportMeshes)
            {
                var overlappingTriangleConduit = new OverlappingTriangleConduit(overlappingTriangleOnlySupportMesh);
                overlappingTriangleConduit.Enabled = true;
                conduits.Add(overlappingTriangleConduit);
            }

            _director.Document.Views.Redraw();

            var getOption = new GetOption();
            getOption.SetCommandPrompt("Please observe the overlapping triangle(Blue sphere = Attention Region; Red triangle with wire frame = overlapping triangle), and press <Enter> to proceed");
            getOption.Get();

            var proceedSupportReplacement = Dialogs.ShowMessage($"Click <Yes> to keep the implant support.\n\n" +
                                                                $"Click <No> to export \"ImplantSupport_I<case index>NotFullyFix.stl\" and " +
                                                                $"\"ImplantSupport_I<case index>Raw.stl\" to a folder for further analysis.",
                                                                "Overlapping Triangle Found", ShowMessageButton.YesNo, ShowMessageIcon.Exclamation);

            foreach (var conduit in conduits)
            {
                conduit.Enabled = false;
                conduit.CleanUp();
            }

            _exportMeshOverlappingTriangleOnly = (proceedSupportReplacement == ShowMessageResult.No);
        }

        public bool CategorizeMeshes(
            out Dictionary<CasePreferenceDataModel, SupportCreationDataModel> exportSupportCreationDataModel,
            out Dictionary<CasePreferenceDataModel, SupportCreationDataModel> replaceSupportCreationDataModel)
        {
            exportSupportCreationDataModel = null;
            replaceSupportCreationDataModel = null;

            if (!_fixedSupportDataModelsWithCase.All(d =>
                _fixedSupportAnalysisResult.Where(r => r.Value != AnalysisResult.Unknown)
                    .Select(kv => kv.Key).Contains(d.Key)))
            {
                return false;
            }

            exportSupportCreationDataModel = new Dictionary<CasePreferenceDataModel, SupportCreationDataModel>();
            replaceSupportCreationDataModel = new Dictionary<CasePreferenceDataModel, SupportCreationDataModel>();

            foreach (var analysisResult in _fixedSupportAnalysisResult)
            {
                switch (analysisResult.Value)
                {
                    case AnalysisResult.BadTriangle:
                        exportSupportCreationDataModel.Add(analysisResult.Key, _fixedSupportDataModelsWithCase[analysisResult.Key]);
                        break;
                    case AnalysisResult.OverlappingTriangleOnly:
                        if (_exportMeshOverlappingTriangleOnly)
                        {
                            exportSupportCreationDataModel.Add(analysisResult.Key, _fixedSupportDataModelsWithCase[analysisResult.Key]);
                        }
                        else
                        {
                            replaceSupportCreationDataModel.Add(analysisResult.Key, _fixedSupportDataModelsWithCase[analysisResult.Key]);
                        }
                        break;
                    case AnalysisResult.CompletelyOk:
                        replaceSupportCreationDataModel.Add(analysisResult.Key, _fixedSupportDataModelsWithCase[analysisResult.Key]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return true;
        }
    }
}
