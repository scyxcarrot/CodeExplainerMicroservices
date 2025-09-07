using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.V2.MTLS.Operation;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System.Linq;

namespace IDS.CMF.ImplantProposal
{
    public class ImplantProposalGenio
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly string _keyTransformationMatrix = "transformation_matrix";

        public ImplantProposalGenio(CMFImplantDirector director)
        {
            _director = director;
            _objectManager = new CMFObjectManager(director);
        }

        public bool PerformGenioImplantProposal(
            ImplantProposalGenioModel implantProposalGenioModel,
            ref ImplantPreferenceModel implantPreferenceModel,
            out AutoImplantProposalResult autoImplantProposalResult)
        {
            autoImplantProposalResult = null;
            var success = GetImplantProposalInputMeshes(
                implantProposalGenioModel,
                out var plannedGenioMesh,
                out var plannedMandibleMesh,
                out var plannedMandibleTeethMesh,
                out var plannedGenioCutMesh,
                out var plannedMandibleCutMesh,
                out var plannedNerveLeftMesh,
                out var plannedNerveRightMesh
            );

            if (!success)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    "Error getting input meshes");
                return false;
            }
            if (_director.MedicalCoordinateSystem != null && _director.MedicalCoordinateSystem.MidSagittalPlane.IsValid)
            {
                implantProposalGenioModel.MidSagittalPlane = RhinoPlaneConverter.ToIPlane(_director.MedicalCoordinateSystem.MidSagittalPlane);
            }
            var screwDiameter = Queries.GetScrewDiameter(
                implantPreferenceModel.CasePrefData.ScrewTypeValue);
            var console = new IDSRhinoConsole();
            autoImplantProposalResult =
                AutoImplantProposal.GetGenioScrewProposalAndConnections(
                    console,
                    plannedGenioMesh,
                    plannedMandibleMesh,
                    plannedMandibleTeethMesh,
                    plannedGenioCutMesh,
                    plannedMandibleCutMesh,
                    plannedNerveLeftMesh,
                    plannedNerveRightMesh,
                    implantPreferenceModel.CasePrefData.ScrewLengthMm,
                    screwDiameter,
                    includeMiddlePlate: implantProposalGenioModel.IncludeMiddlePlate,
                    genioInterScrewDistance: implantProposalGenioModel.GenioInterScrewDistance,
                    mandibleInterScrewDistance: implantProposalGenioModel.MandibleInterScrewDistance,
                    sagittalPlane: implantProposalGenioModel.MidSagittalPlane);

            if (autoImplantProposalResult.ScrewHeads.GetLength(0) == 0)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Implant proposal algorithm failed");
                return false;
            }

            return true;
        }

        public bool GetImplantProposalInputMeshes(
            ImplantProposalGenioModel implantProposalGenioModel, 
            out IMesh plannedGenioMesh,
            out IMesh plannedMandibleMesh,
            out IMesh plannedMandibleTeethMesh,
            out IMesh plannedGenioCutMesh,
            out IMesh plannedMandibleCutMesh,
            out IMesh plannedNerveLeftMesh,
            out IMesh plannedNerveRightMesh)
        {
            var success = GetProPlanImportMeshAndTransform(
                implantProposalGenioModel.PlannedGenio, Transform.Identity, 
                out plannedGenioMesh, out var originPlannedGenioTransform);
            success &= GetProPlanImportMeshAndTransform(
                implantProposalGenioModel.PlannedMandible, Transform.Identity,
                out plannedMandibleMesh, out var originPlannedMandibleTransform);

            var originalGenioString = GetOriginalStringFromPlannedString(
                implantProposalGenioModel.PlannedGenio);
            success &= GetProPlanImportMeshAndTransform(
                originalGenioString, Transform.Identity,
                out _, out var originOriginalGenioTransform);

            var originalMandibleString = GetOriginalStringFromPlannedString(
                implantProposalGenioModel.PlannedMandible);
            success &= GetProPlanImportMeshAndTransform(
                originalMandibleString, Transform.Identity,
                out _, out var originOriginalMandibleTransform);

            success &= GetProPlanImportMeshAndTransform(
                implantProposalGenioModel.PlannedMandibleTeeth, Transform.Identity,
                out plannedMandibleTeethMesh, out _);

            success &= GetProPlanImportMeshAndTransform(
                implantProposalGenioModel.PlannedNerveLeft, Transform.Identity,
                out plannedNerveLeftMesh, out _);
            success &= GetProPlanImportMeshAndTransform(
                implantProposalGenioModel.PlannedNerveRight, Transform.Identity,
                out plannedNerveRightMesh, out _);

            // multiply to get transformation
            success &= originOriginalGenioTransform
                .TryGetInverse(out var originalOriginGenioTransform);
            var genioTransform = Transform.Multiply(
                originPlannedGenioTransform, originalOriginGenioTransform);

            success &= originOriginalMandibleTransform
                .TryGetInverse(out var originalOriginMandibleTransform);
            var mandibleTransform = Transform.Multiply(
                originPlannedMandibleTransform, originalOriginMandibleTransform);

            success &= GetProPlanImportMeshAndTransform(
                implantProposalGenioModel.OriginalGenioCut, genioTransform,
                out plannedGenioCutMesh, out _);

            success &= GetProPlanImportMeshAndTransform(
                implantProposalGenioModel.OriginalGenioCut, mandibleTransform,
                out plannedMandibleCutMesh, out _);

            return success;
        }

        private bool GetProPlanImportMeshAndTransform(
            string searchString,
            Transform transformFoundMesh,
            out IMesh foundMesh,
            out Transform foundTransform)
        {
            foundMesh = null;
            foundTransform = Transform.Unset;

            var foundRhinoObjects =
                _objectManager.GetAllBuildingBlockRhinoObjectByMatchingName(
                    IBB.ProPlanImport, $"{searchString}$");
            if (!foundRhinoObjects.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"{searchString} not found");
                return false;
            }

            var foundObject = foundRhinoObjects.First();
            foundObject.Geometry.Transform(transformFoundMesh);
            foundMesh = RhinoMeshConverter.ToIDSMesh(
                (Mesh)foundObject.Geometry);
            foundTransform = (Transform)foundObject.Attributes
                .UserDictionary[_keyTransformationMatrix];
            return true;
        }

        private string GetOriginalStringFromPlannedString(string plannedSearchString)
        {
            var plannedRhinoObjects =
                _objectManager.GetAllBuildingBlockRhinoObjectByMatchingName(
                    IBB.ProPlanImport, $"{plannedSearchString}$");
            var plannedRhinoObject = plannedRhinoObjects.FirstOrDefault();
            if (plannedRhinoObject == null)
            {
                return null;
            }

            var proPlanImportComponent = new ProPlanImportComponent();
            proPlanImportComponent.GetPurePartNameFromBlockName(plannedRhinoObject.Name,
                out var surgeryState, out var purePartName);
            var originalString = $"01{purePartName}$";

            return originalString;
        }
    }
}
