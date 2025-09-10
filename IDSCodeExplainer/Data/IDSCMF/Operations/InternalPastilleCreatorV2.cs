using IDS.CMF.CasePreferences;
using IDS.CMF.Common;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Tracking;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.Core.Plugin;
using IDS.Core.Utilities;
using IDS.Core.V2.ExternalTools;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.RhinoInterface.Converter;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IDS.CMF.Operations
{
    public class InternalPastilleCreatorV2
    {
        public MsaiTrackingInfo TrackingInfo { get; set; }

        public int NumberOfTasks { get; set; } = 2;

        public List<PastilleCreationResult> GeneratePastilleWithFinalization(Mesh shellMesh, Mesh supportMeshFull, CasePreferenceDataModel casePreferenceData, List<DotPastille> dotPastilles, IEnumerable<Screw> screws,
            bool isCreateActualPastille)
        {
            var console = new IDSRhinoConsole();

            var shellIMesh = RhinoMeshConverter.ToIDSMesh(shellMesh);
            shellIMesh = AutoFixV2.RemoveNoiseShells(console, shellIMesh);
            shellIMesh = AutoFixV2.RemoveFreePoints(console, shellIMesh);
            var supportIMeshFull = RhinoMeshConverter.ToIDSMesh(supportMeshFull);
            supportIMeshFull = AutoFixV2.RemoveNoiseShells(console, supportIMeshFull);
            supportIMeshFull = AutoFixV2.RemoveFreePoints(console, supportIMeshFull);

            var splitDotPastilles 
                = ListUtilities.SplitListEvenly(dotPastilles, NumberOfTasks);
            var tasks = splitDotPastilles
                .Select(splitDotPastille => 
                { 
                    return Task.Run(() => GenerateImplantPastilleAndLandmark(
                    shellIMesh,
                    supportIMeshFull,
                    casePreferenceData,
                    splitDotPastille,
                    screws,
                    isCreateActualPastille,
                    true));
                });

            var taskResults = Task.WhenAll(tasks);

            var allResults = 
                taskResults.Result
                    .SelectMany(result=> result)
                    .ToList();
            return allResults;
        }

        private List<PastilleCreationResult> GenerateImplantPastilleAndLandmark(
            IMesh shellMesh, 
            IMesh supportMeshFull, 
            CasePreferenceDataModel casePreferenceData, 
            List<DotPastille> dotPastilles, 
            IEnumerable<Screw> screws,
            bool isCreateActualPastille,
            bool withFinalization)
        {
            return dotPastilles.Select(
                pastille => GenerateImplantPastilleAndLandmark(
                    ref pastille, 
                    casePreferenceData, 
                    shellMesh, 
                    supportMeshFull, 
                    screws, 
                    isCreateActualPastille,
                    withFinalization))
                .ToList();
        }

        public bool GeneratePastilleWithoutFinalization(ref ImplantDataModel implantDataModel, CasePreferenceDataModel casePreferencesData, Mesh supportMeshRoI, Mesh supportMeshFull, IEnumerable<Screw> screws,
            out List<Mesh> implantPastilleMeshes, out Mesh cylinderMeshes, out List<Mesh> pastilleLandmarkMeshes, bool isCreateActualPastille)
        {
            var console = new IDSRhinoConsole();

            var supportIMeshRoI = RhinoMeshConverter.ToIDSMesh(supportMeshRoI);
            supportIMeshRoI = AutoFixV2.RemoveNoiseShells(console, supportIMeshRoI);
            supportIMeshRoI = AutoFixV2.RemoveFreePoints(console, supportIMeshRoI);
            var supportIMeshFull = RhinoMeshConverter.ToIDSMesh(supportMeshFull);
            supportIMeshFull = AutoFixV2.RemoveNoiseShells(console, supportIMeshFull);
            supportIMeshFull = AutoFixV2.RemoveFreePoints(console, supportIMeshFull);

            var dotPastilles = implantDataModel.DotList
                .Where(d => d is DotPastille)
                .Cast<DotPastille>()
                .ToList();
            var splitDotPastilles
                = ListUtilities.SplitListEvenly(dotPastilles, NumberOfTasks);
            var tasks = splitDotPastilles
                .Select(splitDotPastille =>
                {
                    return Task.Run(() => GenerateImplantPastilleAndLandmark(
                        supportIMeshRoI,
                        supportIMeshFull,
                        casePreferencesData,
                        splitDotPastille,
                        screws,
                        isCreateActualPastille,
                        true));
                });
            var taskResults = Task.WhenAll(tasks);
            var allResults =
                taskResults.Result
                    .SelectMany(result => result)
                    .ToList();

            // if any failed, return all empty values
            if (allResults.Any(result => !result.Success))
            {
                implantPastilleMeshes = new List<Mesh>();
                cylinderMeshes = new Mesh();
                pastilleLandmarkMeshes = new List<Mesh>();
                return false;
            }

            implantPastilleMeshes = allResults
                .Select(result=>result.IntermediatePastille)
                .ToList();
                
            pastilleLandmarkMeshes = allResults
                .Select(result => result.IntermediateLandmark)
                .ToList();
            cylinderMeshes = MeshUtilities.AppendMeshes(allResults.Select(result => result.PastilleCylinder));
            return true;
        }

        private PastilleCreationResult GenerateImplantPastilleAndLandmark(
            ref DotPastille pastille,
            CasePreferenceDataModel casePreferencesData,
            IMesh supportMeshRoI,
            IMesh supportMeshFull, 
            IEnumerable<Screw> screws, 
            bool isCreateActualPastille, 
            bool withFinalization)
        {
            var screwStamp
               = ImplantPastilleCreationUtilities
                   .GetScrewStamp(screws, pastille);

            var entityName = "Pastille";
            var currPastille = pastille;
            var prevAlgo = currPastille.CreationAlgoMethod;
            var currScrew = screws.First(s => s.Id == currPastille.Screw.Id);
            var errorMessages = new List<string>();
            PastilleCreationResult pastilleCreationResult;
            try
            {
                using (TimeTracking.NewInstance(
                           $"{TrackingConstants.DevMetrics}_V2GenerateImplantPastilleAndLandmark-{casePreferencesData.CaseName}-screw{currScrew.Index} (id: {currScrew.Id})",
                           TrackingInfo.AddTrackingParameterSafely))
                {
                    entityName = "Landmark";
                    GenerateImplantLandmark(
                        ref pastille, 
                        supportMeshRoI, 
                        supportMeshFull,
                        out var pastilleLandmarkMesh,
                        ref errorMessages,
                        isCreateActualPastille, 
                        $"{currScrew.Id}");

                    entityName = "Pastille";

                    var pastilleLandmarkIMesh = pastilleLandmarkMesh == null ? null : RhinoMeshConverter.ToIDSMesh(pastilleLandmarkMesh);

                    var utility = new ImplantPastilleCreationUtilitiesV2(
                        pastille, 
                        currScrew, 
                        casePreferencesData,
                        supportMeshRoI, 
                        supportMeshFull, 
                        isCreateActualPastille,
                        pastilleLandmarkIMesh,
                        RhinoMeshConverter.ToIDSMesh(screwStamp), 
                        withFinalization);
                    utility.GenerateImplantPastille(
                        ref errorMessages,
                        out var finalFilteredMesh,
                        out var implantPastilleMesh, 
                        out var pastilleCylinder);

                    pastilleCreationResult
                        = new PastilleCreationResult
                        {
                            FinalPastille = finalFilteredMesh,
                            IntermediatePastille = implantPastilleMesh,
                            IntermediateLandmark = pastilleLandmarkMesh,
                            PastilleCylinder = pastilleCylinder,
                            DotPastilleId = pastille.Id,
                            PreviousCreationAlgoMethod = prevAlgo,
                            ErrorMessages = new List<string>(),
                            Success = true
                        };
                }

                var hasLandmarkKey = $"{TrackingConstants.DevMetrics}_V2HasLandmark-{casePreferencesData.CaseName}-screw{currScrew.Index} (id: {currScrew.Id})";
                TrackingInfo.AddTrackingParameterSafely(hasLandmarkKey, (pastille.Landmark != null).ToString());

                var pastilleFinalizationKey = $"{TrackingConstants.DevMetrics}_V2PastilleFinalization-{casePreferencesData.CaseName}-screw{currScrew.Index} (id: {currScrew.Id})";
                TrackingInfo.AddTrackingParameterSafely(pastilleFinalizationKey, withFinalization.ToString());

                return pastilleCreationResult;
            }
            catch (Exception e)
            {
                errorMessages.AddRange(
                    ImplantPastilleCreationUtilities
                        .ReportScrewRelatedException(
                            entityName, 
                            pastille, 
                            screws, 
                            casePreferencesData.NCase, 
                            e));

                pastilleCreationResult
                    = new PastilleCreationResult 
                    {
                        FinalPastille = null,
                        IntermediatePastille = null,
                        IntermediateLandmark = null,
                        PastilleCylinder = null,
                        DotPastilleId = pastille.Id,
                        PreviousCreationAlgoMethod = prevAlgo,
                        ErrorMessages = errorMessages,
                        Success = false
                    };

                return pastilleCreationResult;
            }
        }

        private void GenerateImplantLandmark(ref DotPastille pastille,
            IMesh supportMeshRoI,
            IMesh supportMeshFull,
            out Mesh pastilleLandmarkMesh,
            ref List<string> errorMessages, bool isCreateActualPastille, string id)
        {
            pastilleLandmarkMesh = null;

            if (pastille != null && pastille.Landmark != null)
            {
                var console = new IDSRhinoConsole();
                var factory = new ImplantFactory(console);

                var componentInfo = new LandmarkComponentInfo
                {
                    DisplayName = $"Landmark-{id}",
                    IsActual = isCreateActualPastille,
                    PastilleDirection = pastille.Direction,
                    PastilleThickness = pastille.Thickness,
                    PastilleLocation = pastille.Location,
                    PastilleDiameter = pastille.Diameter,
                    Type = pastille.Landmark.LandmarkType,
                    Point = pastille.Landmark.Point,
                    ComponentMeshes = new List<IMesh>(),
                    Subtractors = new List<IMesh>(),
                    ClearanceMesh = supportMeshFull,
                    SupportRoIMesh = supportMeshRoI,
                    NeedToFinalize = false
                };
                var result = factory.CreateImplant(componentInfo);

                if (result.ErrorMessages.Any())
                {
                    if (result.ErrorMessages.Count > 1)
                    {
                        errorMessages = result.ErrorMessages
                            .Take(result.ErrorMessages.Count - 1)
                            .ToList();
                    }

                    throw new Exception(result.ErrorMessages.Last());
                }

                pastilleLandmarkMesh = RhinoMeshConverter.ToRhinoMesh(result.ComponentMesh);
            }
        }
    }
}
