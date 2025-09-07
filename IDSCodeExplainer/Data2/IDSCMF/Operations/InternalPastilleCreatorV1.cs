using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Tracking;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.ExternalTools;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class InternalPastilleCreatorV1
    {
        public MsaiTrackingInfo TrackingInfo { get; set; }

        //Old operations
        public List<PastilleCreationResult> GeneratePastillePreview(Mesh shellMesh, Mesh supportMeshFull, CasePreferenceDataModel casePreferenceData, List<DotPastille> dotPastilles,
            OverallImplantParams overallImplantParams, IndividualImplantParams individualImplantParams, LandmarkImplantParams landmarkImplantParams, IEnumerable<Screw> screws,
            bool isCreateActualPastille, ref List<string> errorMessages)
        {
            var resultList = new List<PastilleCreationResult>();
            foreach (var pastille in dotPastilles)
            {
                var currPastille = pastille;
                var prevAlgo = currPastille.CreationAlgoMethod;
                var currScrew = screws.First(s => s.Id == currPastille.Screw.Id);

                Mesh pastilleCylinder;
                Mesh implantPastilleMesh;
                Mesh pastilleLandmarkMesh;

                using (TimeTracking.NewInstance(
                           $"{TrackingConstants.DevMetrics}_V1GenerateImplantPastilleAndLandmark-Implant {casePreferenceData.CaseName}-screw{currScrew.Index} (id: {currScrew.Id})",
                           TrackingInfo.AddTrackingParameterSafely))
                {
                    if (!ImplantPastilleCreationUtilities.GenerateImplantPastilleAndLandmark(ref currPastille,
                            casePreferenceData, individualImplantParams, landmarkImplantParams,
                            shellMesh, supportMeshFull, screws, out implantPastilleMesh, out pastilleLandmarkMesh,
                            out pastilleCylinder, ref errorMessages))
                    {
                        continue;
                    }
                }

                TrackingInfo.AddTrackingParameterSafely($"{TrackingConstants.DevMetrics}_V1HasLandmark-Implant {casePreferenceData.CaseName}-screw{currScrew.Index} (id: {currScrew.Id})",
                    (pastille.Landmark != null).ToString());

                var pastilleSurface =
                    MeshUtilities.AppendMeshes(new[] { implantPastilleMesh, pastilleLandmarkMesh });

                Mesh pastilleMesh;
                var wrapRatio = overallImplantParams.WrapOperationOffset;
                var componentSmallestDetail = overallImplantParams.WrapOperationSmallestDetails;
                var componentGapClosingDistance = overallImplantParams.WrapOperationGapClosingDistance;
                if (!Wrap.PerformWrap(new[] { pastilleSurface }, componentSmallestDetail,
                        componentGapClosingDistance, wrapRatio, false, true, false, false, out pastilleMesh))
                {
                    throw new IDSException("wrapped pastille failed.");
                }

                if (overallImplantParams.IsDoPostProcessing && isCreateActualPastille)
                {
                    pastilleMesh = ImplantCreationUtilities.RemeshAndSmoothImplant(pastilleMesh);
                }

                var pastilleSubtractedWithSupportMesh =
                    ImplantCreationUtilities.SubstractImplantWithSupport(pastilleMesh, shellMesh);

                if (pastilleSubtractedWithSupportMesh.DisjointMeshCount > 1)
                {
                    var disjointedMeshes = pastilleSubtractedWithSupportMesh.SplitDisjointPieces();
                    var pastillePoint = RhinoPoint3dConverter.ToPoint3d(currPastille.Location);
                    var closestMeshes = disjointedMeshes.Where(
                        disjointedMesh => disjointedMesh.ClosestMeshPoint(pastillePoint, 0.1) != null);

                    if (closestMeshes.Any())
                    {
                        pastilleSubtractedWithSupportMesh = MeshUtilities.AppendMeshes(closestMeshes);
                    }
                }

                var screwStamp = ImplantPastilleCreationUtilities.GetScrewStamp(screws, currPastille);

                var finalPastille = ImplantPastilleCreationUtilities.SubstractPastilleWithScrew(pastilleSubtractedWithSupportMesh, screwStamp);
                pastilleMesh.Dispose();

                var finalFilteredMesh = MeshUtilities.RemoveNoiseShells(finalPastille, 2.00);

                MeshUtilities.RepairMesh(ref implantPastilleMesh);

                var pastilleCreationResult = new PastilleCreationResult()
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
                resultList.Add(pastilleCreationResult);

                screwStamp.Dispose();
            }

            return resultList;
        }
    }
}
