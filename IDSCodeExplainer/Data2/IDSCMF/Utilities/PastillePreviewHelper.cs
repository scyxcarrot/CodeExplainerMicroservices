using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class PastillePreviewHelper
    {
        private const string IntermediatePastilleKey = "intermediate_pastille";
        private const string IntermediateLandmarkKey = "intermediate_landmark";
        private const string PastilleCylinderKey = "pastille_cylinder";
        public const string DotPastilleId = "dot_pastille_id";

        private readonly CMFImplantDirector director;
        private readonly CMFObjectManager objectManager;

        public PastillePreviewHelper(CMFImplantDirector director)
        {
            this.director = director;
            objectManager = new CMFObjectManager(director);
        }

        public void AddPastillePreviewBuildingBlock(CasePreferenceDataModel casePreferenceData, List<PastilleCreationResult> pastilleCreationResults)
        {
            var implantComponent = new ImplantCaseComponent();
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PastillePreview, casePreferenceData);

            var pastilleList =
                casePreferenceData.ImplantDataModel.DotList
                    .Where(dot => dot is DotPastille).Cast<DotPastille>();
            foreach (var result in pastilleCreationResults)
            {
                var pastille = pastilleList.First(p => p.Id == result.DotPastilleId);
                var id = IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(objectManager, director.IdsDocument, buildingBlock, pastille.Screw.Id, result.FinalPastille);

                if (id == Guid.Empty)
                {
                    throw new Exception("Failed to add Pastille creation results!");
                }

                var rhinoObject = director.Document.Objects.Find(id);
                rhinoObject.Attributes.UserDictionary.Set(IntermediatePastilleKey, result.IntermediatePastille);
                if (result.IntermediateLandmark != null)
                {
                    rhinoObject.Attributes.UserDictionary.Set(IntermediateLandmarkKey, result.IntermediateLandmark);
                }

                rhinoObject.Attributes.UserDictionary.Set(PastilleCylinderKey, result.PastilleCylinder);
                rhinoObject.Attributes.UserDictionary.Set(DotPastilleId, result.DotPastilleId);
            }
        }

        public List<DotPastille> GetMissingPastillePreviews(CasePreferenceDataModel casePreferenceData, Mesh supportMesh, IEnumerable<Screw> screws, double pastillePlacementModifier)
        {
            var implant = ImplantPastilleCreationUtilities.AdjustPastilles(casePreferenceData.ImplantDataModel, supportMesh, screws, pastillePlacementModifier);

            var dotPastilles = new List<DotPastille>();
            foreach (var connection_pt in implant.DotList)
            {
                if (connection_pt is DotPastille pastille)
                {
                    dotPastilles.Add(pastille);
                }
            }

            if (!HasPastillePreviewBuildingBlock(casePreferenceData))
            {
                return dotPastilles;
            }

            var dotPastilleIdsPairs = GetDotPastilleIds(casePreferenceData);

            var missingPastilles = dotPastilles.ToList();
            foreach (var pastille in dotPastilles)
            {
                foreach (var dotPastilleIdPair in dotPastilleIdsPairs)
                {
                    var dotPastilleId = dotPastilleIdPair.Key;
                    if (pastille.Id == dotPastilleId)
                    {
                        missingPastilles.Remove(pastille);
                        break;
                    }
                }
            }

            return missingPastilles;
        }

        public Guid GetPastillePreviewBuildingBlockId(CasePreferenceDataModel casePreferenceData, DotPastille dotPastille)
        {
            var dotPastilles = new List<DotPastille> { dotPastille };
            var list = GetPastillePreviewBuildingBlockIds(casePreferenceData, dotPastilles);

            if (!list.Any())
            {
                return Guid.Empty;
            }

            return list.First();
        }

        public List<Guid> GetPastillePreviewBuildingBlockIds(CasePreferenceDataModel casePreferenceData, List<DotPastille> dotPastilles)
        {
            if (!HasPastillePreviewBuildingBlock(casePreferenceData) || !dotPastilles.Any())
            {
                return new List<Guid>();
            }

            var dotPastilleIdsPairs = GetDotPastilleIds(casePreferenceData);
            var list = new List<Guid>();

            foreach (var dotPastille in dotPastilles)
            {
                foreach (var dotPastilleIdPair in dotPastilleIdsPairs)
                {
                    var dotPastilleId = dotPastilleIdPair.Key;
                    var pastillePreview = dotPastilleIdPair.Value;
                    if (dotPastilleId == dotPastille.Id)
                    {
                        list.Add(pastillePreview.Id);
                    }
                }
            }

            return list;
        }

        public bool HasPastillePreviewBuildingBlock(CasePreferenceDataModel casePreferenceData)
        {
            var implantComponent = new ImplantCaseComponent();
            var implantBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PastillePreview, casePreferenceData);
            return objectManager.HasBuildingBlock(implantBuildingBlock.Block);
        }

        private List<RhinoObject> GetPastillePreviewRhinoObjects(CasePreferenceDataModel casePreferenceData)
        {
            var implantComponent = new ImplantCaseComponent();
            var implantBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PastillePreview, casePreferenceData);
            return objectManager.GetAllBuildingBlocks(implantBuildingBlock.Block).ToList();
        }

        public List<Mesh> GetIntermediatePastillePreviews(CasePreferenceDataModel casePreferenceData)
        {
            var rhinoObjects = GetPastillePreviewRhinoObjects(casePreferenceData);

            var meshes = new List<Mesh>();
            foreach (var rhinoObject in rhinoObjects)
            {
                meshes.Add(GetIntermediatePastillePreview(rhinoObject));
            }
            return meshes;
        }

        public List<Mesh> GetIntermediatePastilleLandmarkPreviews(CasePreferenceDataModel casePreferenceData)
        {
            var rhinoObjects = GetPastillePreviewRhinoObjects(casePreferenceData);

            var meshes = new List<Mesh>();
            foreach (var rhinoObject in rhinoObjects)
            {
                var landmark = GetIntermediatePastilleLandmarkPreview(rhinoObject);

                if (landmark != null)
                {
                    meshes.Add(landmark);
                }
            }
            return meshes;
        }

        private Mesh GetIntermediatePastilleLandmarkPreview(RhinoObject rhinoObject)
        {
            if (rhinoObject.Attributes.UserDictionary.ContainsKey(IntermediateLandmarkKey))
            {
                return (Mesh)rhinoObject.Attributes.UserDictionary[IntermediateLandmarkKey];
            }
            else
            {
                return null;
            }
        }

        private Mesh GetIntermediatePastillePreview(RhinoObject rhinoObject)
        {
            if (rhinoObject.Attributes.UserDictionary.ContainsKey(IntermediatePastilleKey))
            {
                return (Mesh)rhinoObject.Attributes.UserDictionary[IntermediatePastilleKey];
            }
            else
            {
                throw new Exception("Pastille preview object does not have intermediate parts!");
            }
        }

        public List<Mesh> GetPastilleCylinder(CasePreferenceDataModel casePreferenceData)
        {
            var pastilleRhObjs = GetPastillePreviewRhinoObjects(casePreferenceData);

            var meshes = new List<Mesh>();
            foreach (var rhinoObject in pastilleRhObjs)
            {
                if (rhinoObject.Attributes.UserDictionary.ContainsKey(PastilleCylinderKey))
                {
                    meshes.Add((Mesh)rhinoObject.Attributes.UserDictionary[PastilleCylinderKey]);
                }
                else
                {
                    throw new Exception("Pastille preview object does not have pastille cylinder!");
                }
            }
            return meshes;
        }

        private List<KeyValuePair<Guid, RhinoObject>> GetDotPastilleIds(CasePreferenceDataModel casePreferenceData)
        {
            var rhinoObjects = GetPastillePreviewRhinoObjects(casePreferenceData);

            var dotPastilleIds = new List<KeyValuePair<Guid, RhinoObject>>();
            foreach (var rhinoObject in rhinoObjects)
            {
                if (rhinoObject.Attributes.UserDictionary.ContainsKey(DotPastilleId))
                {
                    var dotPastilleId = (Guid)rhinoObject.Attributes.UserDictionary[DotPastilleId];
                    dotPastilleIds.Add(new KeyValuePair<Guid, RhinoObject>(dotPastilleId, rhinoObject));
                    continue;
                }
                throw new Exception("Pastille preview object does not have dot pastille id!");
            }

            return dotPastilleIds;
        }
    }
}
