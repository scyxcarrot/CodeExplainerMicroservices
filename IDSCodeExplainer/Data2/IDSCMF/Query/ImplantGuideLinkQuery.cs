using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Query
{
    public static class ImplantGuideLinkQuery
    {
        public static void SetLinkedImplantsDisplayString(CMFImplantDirector director, ref List<GuidePreferenceModel> guidePreferenceModels)
        {
            var implantBarrels = GetRegisteredBarrels(director);

            foreach (var guidePreference in guidePreferenceModels)
            {
                var linkedRegisteredBarrels = RegisteredBarrelUtilities.GetLinkedRegisteredBarrels(director, guidePreference);
                if (!linkedRegisteredBarrels.Any())
                {
                    guidePreference.LinkedImplantsDisplayString = string.Empty;
                    continue;
                }

                var linkedImplants = GetLinkedImplants(implantBarrels, linkedRegisteredBarrels);
                var orderedList = linkedImplants.OrderBy(i => i.Key.NCase).Select(i => $"{i.Value}.I{i.Key.NCase}");
                var displayStr = string.Join(" ", orderedList);
                guidePreference.LinkedImplantsDisplayString = displayStr;
            }
        }

        public static List<int> GetLinkedImplantNumbers(CMFImplantDirector director, GuidePreferenceDataModel guidePreference)
        {
            var linkedRegisteredBarrels = RegisteredBarrelUtilities.GetLinkedRegisteredBarrels(director, guidePreference);
            if (!linkedRegisteredBarrels.Any())
            {
                return new List<int>();
            }

            var implantBarrels = GetRegisteredBarrels(director);

            var linkedImplants = GetLinkedImplants(implantBarrels, linkedRegisteredBarrels);
            var implantNumbers = linkedImplants.Select(i => i.Key.NCase).ToList();
            implantNumbers.Sort();
            return implantNumbers;
        }

        public static void SetLinkedGuidesDisplayString(CMFImplantDirector director, ref List<ImplantPreferenceModel> implantPreferenceModels)
        {
            var guideBarrels = new Dictionary<GuidePreferenceDataModel, List<Guid>>();

            foreach (var guidePreference in director.CasePrefManager.GuidePreferences)
            {
                var linkedRegisteredBarrels = RegisteredBarrelUtilities.GetLinkedRegisteredBarrels(director, guidePreference);
                if (!linkedRegisteredBarrels.Any())
                {
                    continue;
                }
                guideBarrels.Add(guidePreference, linkedRegisteredBarrels.ToList());
            }

            foreach (var implantPreference in implantPreferenceModels)
            {
                var allRegisteredBarrels = GetRegisteredBarrels(director, implantPreference);
                if (!allRegisteredBarrels.Any())
                {
                    implantPreference.LinkedGuidesDisplayString = string.Empty;
                    continue;
                }

                var linkedGuides = new Dictionary<GuidePreferenceDataModel, int>();

                foreach (var keyPair in guideBarrels)
                {
                    var ids = keyPair.Value;

                    var linkedRegisteredBarrels = allRegisteredBarrels.Where(r => ids.Contains(r.Id));
                    if (!linkedRegisteredBarrels.Any())
                    {
                        continue;
                    }

                    linkedGuides.Add(keyPair.Key, linkedRegisteredBarrels.Count());
                }

                var linkedRegisteredBarrelsCount = linkedGuides.Sum(i => i.Value);
                var orderedList = linkedGuides.OrderBy(i => i.Key.NCase).Select(i => $"{i.Value}.G{i.Key.NCase}");
                var displayStr = $"{linkedRegisteredBarrelsCount}/{allRegisteredBarrels.Count} B  {string.Join(" ", orderedList)}";
                implantPreference.LinkedGuidesDisplayString = displayStr;
            }
        }

        private static List<RhinoObject> GetRegisteredBarrels(CMFImplantDirector director, CasePreferenceDataModel implantPreference)
        {
            var objectManager = new CMFObjectManager(director);
            var implantComponent = new ImplantCaseComponent();
            var eblock = implantComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, implantPreference);
            var registeredBarrels = objectManager.GetAllBuildingBlocks(eblock);
            return registeredBarrels.ToList();
        }

        private static Dictionary<CasePreferenceDataModel, List<RhinoObject>> GetRegisteredBarrels(CMFImplantDirector director)
        {
            var implantBarrels = new Dictionary<CasePreferenceDataModel, List<RhinoObject>>();

            foreach (var implantPreference in director.CasePrefManager.CasePreferences)
            {
                var registeredBarrels = GetRegisteredBarrels(director, implantPreference);
                if (registeredBarrels.Any())
                {
                    implantBarrels.Add(implantPreference, registeredBarrels);
                }                
            }

            return implantBarrels;
        }

        private static Dictionary<CasePreferenceDataModel, int> GetLinkedImplants(Dictionary<CasePreferenceDataModel, List<RhinoObject>> implantBarrels, List<Guid> linkedRegisteredBarrelIds)
        {
            var linkedImplants = new Dictionary<CasePreferenceDataModel, int>();

            foreach (var keyPair in implantBarrels)
            {
                var registeredBarrels = keyPair.Value;

                var linkedRegisteredBarrels = registeredBarrels.Where(r => linkedRegisteredBarrelIds.Contains(r.Id));
                if (linkedRegisteredBarrels.Any())
                {
                    linkedImplants.Add(keyPair.Key, linkedRegisteredBarrels.Count());
                }
            }

            return linkedImplants;
        }
    }
}
