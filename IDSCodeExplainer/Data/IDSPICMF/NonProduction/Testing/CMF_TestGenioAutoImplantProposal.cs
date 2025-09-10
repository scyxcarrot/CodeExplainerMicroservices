#if (STAGING)
using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantProposal;
using IDS.CMF.Query;
using IDS.CMF.V2.MTLS.Operation;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.V2.Utilities;
using IDS.PICMF.Helper;
using IDS.RhinoInterfaces.Converter;
using Newtonsoft.Json;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using System.IO;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("914626FA-BE35-43BB-8A56-566A14241F5D")]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestGenioAutoImplantProposal : CmfCommandBase
    {
        public CMF_TestGenioAutoImplantProposal()
        {
            Instance = this;
        }

        public static CMF_TestGenioAutoImplantProposal Instance { get; private set; }

        public override string EnglishName => "CMF_TestGenioAutoImplantProposal";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var implantProposalInput =
                new ImplantProposalInput(director);
            var success = implantProposalInput
                .GetImplantPreferenceModel(out var implantPreferenceModel);
            if (!success)
            {
                return Result.Failure;
            }

            var implantProposalDataModelPath = string.Empty;
            var getStringResult = RhinoGet.GetString("ImplantProposalDataModelPath", false, ref implantProposalDataModelPath);
            if (getStringResult != Result.Success ||
                !File.Exists(implantProposalDataModelPath))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"Invalid ImplantProposalDataModelPath, " +
                    $"ImplantProposalDataModelPath = {implantProposalDataModelPath}");
                return Result.Failure;
            }

            var implantProposalGenioModel
                = JsonUtilities
                    .DeserializeFile<ImplantProposalGenioModel>(
                        implantProposalDataModelPath);

            if (string.IsNullOrEmpty(implantProposalGenioModel.PlannedNerveLeft))
            {
                implantProposalGenioModel.PlannedNerveLeft =
                    ImplantProposalGenioModel.Default().PlannedNerveLeft;
            }

            if (string.IsNullOrEmpty(implantProposalGenioModel.PlannedNerveRight))
            {
                implantProposalGenioModel.PlannedNerveRight =
                    ImplantProposalGenioModel.Default().PlannedNerveRight;
            }

            if (string.IsNullOrEmpty(implantProposalGenioModel.PlannedMandibleTeeth))
            {
                implantProposalGenioModel.PlannedMandibleTeeth =
                    ImplantProposalGenioModel.Default().PlannedMandibleTeeth;
            }

            if (string.IsNullOrEmpty(implantProposalGenioModel.PlannedGenio))
            {
                implantProposalGenioModel.PlannedGenio =
                    ImplantProposalGenioModel.Default().PlannedGenio;
            }

            if (string.IsNullOrEmpty(implantProposalGenioModel.PlannedMandible))
            {
                implantProposalGenioModel.PlannedMandible =
                    ImplantProposalGenioModel.Default().PlannedMandible;
            }

            if (string.IsNullOrEmpty(implantProposalGenioModel.OriginalGenioCut))
            {
                implantProposalGenioModel.OriginalGenioCut =
                    ImplantProposalGenioModel.Default().OriginalGenioCut;
            }

            var implantProposalGenio = new ImplantProposalGenio(director);
            success = implantProposalGenio.GetImplantProposalInputMeshes(
                implantProposalGenioModel,
                out var plannedGenioMesh,
                out var plannedMandibleMesh,
                out var plannedMandibleTeethWrappedMesh,
                out var plannedGenioCutMesh,
                out var plannedMandibleCutMesh,
                out var plannedNerveLeftMesh,
                out var plannedNerveRightMesh
            );

            if (!success)
            {
                return Result.Failure;
            }

            var screwDiameter = Queries.GetScrewDiameter(
                implantPreferenceModel.CasePrefData.ScrewTypeValue);
            var console = new IDSRhinoConsole();
            if(director.MedicalCoordinateSystem != null && director.MedicalCoordinateSystem.MidSagittalPlane.IsValid)
            {
                implantProposalGenioModel.MidSagittalPlane = RhinoPlaneConverter.ToIPlane(director.MedicalCoordinateSystem.MidSagittalPlane);
            }
            else
            {
                implantProposalGenioModel.MidSagittalPlane = ImplantProposalGenioModel.Default().MidSagittalPlane;
            }
            var autoImplantProposalResult =
                AutoImplantProposal.GetGenioScrewProposalAndConnections(
                console,
                plannedGenioMesh,
                plannedMandibleMesh,
                plannedMandibleTeethWrappedMesh,
                plannedGenioCutMesh,
                plannedMandibleCutMesh,
                plannedNerveLeftMesh,
                plannedNerveRightMesh,
                implantPreferenceModel.CasePrefData.ScrewLengthMm,
                screwDiameter,
                implantProposalGenioModel.ScrewAngulation,
                implantProposalGenioModel.IncludeMiddlePlate,
                implantProposalGenioModel.ScrewInsertionDirection,
                implantProposalGenioModel.MandibleInterScrewDistance,
                implantProposalGenioModel.GenioInterScrewDistance,
                implantProposalGenioModel.MinInterScrewDistance,
                implantProposalGenioModel.MinDistanceToCut,
                implantProposalGenioModel.MinDistanceToBoneEdge,
                implantProposalGenioModel.MidSagittalPlane);

            var currentFolder = Path.GetDirectoryName(director.Document.Path);
            var resultPath = Path.Combine(currentFolder,
                "autoImplantProposalResult.json");
            JsonUtilities.SerializeFile(resultPath, autoImplantProposalResult, 
                format: Formatting.Indented);

            var implantProposalOutput = new ImplantProposalOutput(director);
            implantProposalOutput.CreateScrewsAndDotPastilles(
                autoImplantProposalResult, ref implantPreferenceModel);
            return Result.Success;
        }
    }
}

#endif