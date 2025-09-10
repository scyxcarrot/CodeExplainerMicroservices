using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.CMF.Invalidation
{
    public static class ImportRecutInvalidationUtilities
    {
        //take note that the values of partsThatChanged from Import Recut contain prefix ""ProPlanImport_"
        public static List<PartProperties> GetPartsWithDependentPartName(Dictionary<PartProperties, List<PartProperties>> graph, List<PartProperties> partsThatChanged)
        {
            var parts = new List<PartProperties>();

            foreach (var partName in partsThatChanged)
            {
                foreach (var item in graph)
                {
                    if (parts.Contains(item.Key))
                    {
                        continue;
                    }

                    //both contain prefix ""ProPlanImport_"
                    if (item.Value.Any(p => p.Name.ToLower() == partName.Name.ToLower()))
                    {
                        parts.Add(item.Key);
                    }
                }
            }

            return parts;
        }

        public static bool HasImplantSupportGuidingOutlineDependantParts(List<string> partsThatChanged)
        {
            var hasGuidingOutlineDependantParts = false;

            foreach (var partName in partsThatChanged)
            {
                //trim prefix ""ProPlanImport_"
                var trimmedPartName = TrimPartName(partName);
                if (IsOriginalOsteotomyPlane(trimmedPartName) || IsPreopBoneOrGraft(trimmedPartName))
                {
                    hasGuidingOutlineDependantParts = true;
                    break;
                }
            }

            return hasGuidingOutlineDependantParts;
        }

        public static bool HasImplantPlaceable(List<string> partsThatChanged)
        {
            return GetImplantPlaceable(partsThatChanged).Any();
        }

        public static IEnumerable<string> GetImplantPlaceable(List<string> partsThatChanged)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var implantPlaceable = proPlanImportComponent.GetConstraintMeshesNameForImplant(partsThatChanged.Select(p => TrimPartName(p)));
            return implantPlaceable;
        }

        public static bool IsImplantPlaceable(string partName)
        {
            return HasImplantPlaceable(new List<string> { partName });
        }

        public static PartProperties GetImplantPlaceablePartByOriginalPart(RhinoDoc doc, Guid originalPartGuid)
        {
            var originalPart = doc.Objects.Find(originalPartGuid);
            var plannedPartRhObject = ProPlanImportUtilities.GetPlannedObjectByOriginalObject(doc, originalPart);

            if (plannedPartRhObject == null)
            {
                if (!IsImplantPlaceable(originalPart.Name))
                {
                    return null;
                }
                else
                {
                    plannedPartRhObject = originalPart;
                }
            }

            return new PartProperties(plannedPartRhObject.Id, plannedPartRhObject.Name, IBB.ProPlanImport);
        }

        public static bool IsPartOf(string partName, ProplanBoneType boneType, ProPlanImportPartType partType)
        {
            return IsPartOf(partName, boneType, new List<ProPlanImportPartType> { partType });
        }

        private static bool IsPartOf(string partName, ProplanBoneType boneType, List<ProPlanImportPartType> partTypes)
        {
            //trim prefix ""ProPlanImport_"
            var trimmedPartName = TrimPartName(partName);

            if (!ProPlanImportUtilities.IsPartOfBoneType(trimmedPartName, boneType))
            {
                return false;
            }

            var proPlanImportComponent = new ProPlanImportComponent();
            var partBlocks = proPlanImportComponent.Blocks.Where(b => partTypes.Contains(b.PartType));
            foreach (var block in partBlocks)
            {
                if (Regex.IsMatch(trimmedPartName, $"^{block.PartNamePattern}$", RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsOriginalOsteotomyPlane(string partName)
        {
            return IsPartOf(partName, ProplanBoneType.Original, new List<ProPlanImportPartType> { ProPlanImportPartType.OsteotomyPlane });
        }

        private static bool IsPreopBoneOrGraft(string partName)
        {
            return IsPartOf(partName, ProplanBoneType.Preop, new List<ProPlanImportPartType> { ProPlanImportPartType.Bone, ProPlanImportPartType.Graft });
        }

        private static string TrimPartName(string partName)
        {
            return partName.Replace(ProPlanImport.ObjectPrefix, string.Empty);
        }
    }
}
