using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public static class RegisteredBarrelUtilities
    {
        public static List<Guid> GetLinkedRegisteredBarrels(CMFImplantDirector director, GuidePreferenceDataModel guidePrefModel)
        {
            var registeredBarrelIds = new List<Guid>();

            var linkedImplantScrews = guidePrefModel.LinkedImplantScrews.ToList();

            foreach (var linkedImplantScrewId in linkedImplantScrews)
            {
                var rhinoObject = director.Document.Objects.Find(linkedImplantScrewId);
                if (!(rhinoObject is Screw))
                {
                    //this can happen during load file (before RestoreCustomRhinoObjects is called)
                    break;
                }

                var screw = (Screw)rhinoObject;
                if (screw != null && screw.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel))
                {
                    registeredBarrelIds.Add(screw.ScrewGuideAidesInDocument[IBB.RegisteredBarrel]);
                }
            }

            return registeredBarrelIds;
        }

        public static void SetLinkedRegisteredBarrels(CMFImplantDirector director, GuidePreferenceDataModel guidePrefModel, List<Guid> registeredBarrelIds)
        {
            var implantScrewIds = new List<Guid>();

            var objectManager = new CMFObjectManager(director);
            var implantScrews = objectManager.GetAllBuildingBlocks(IBB.Screw).Cast<Screw>();

            foreach (var registeredBarrelId in registeredBarrelIds)
            {
                var screw = implantScrews.First(s => s.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel) && s.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] == registeredBarrelId);
                implantScrewIds.Add(screw.Id);
            }

            var linkedImplantScrews = guidePrefModel.LinkedImplantScrews.ToList();
            guidePrefModel.LinkedImplantScrews.Clear();
            guidePrefModel.LinkedImplantScrews.AddRange(implantScrewIds);
            
            if (!(linkedImplantScrews.All(implantScrewIds.Contains) && implantScrewIds.All(linkedImplantScrews.Contains)))
            {
                NotifyBuildingBlockHasChanged(guidePrefModel);
            }
        }

        public static void SetDefaultLinkedImplantScrews(CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            var implantComponent = new ImplantCaseComponent();

            foreach (var guidePref in director.CasePrefManager.GuidePreferences)
            {
                var linkedImplantScrews = guidePref.LinkedImplantScrews.ToList();
                guidePref.LinkedImplantScrews.Clear();

                var correspondingImplantCasePref = director.CasePrefManager.CasePreferences.FirstOrDefault(x => x.NCase == guidePref.NCase);
                if (correspondingImplantCasePref == null)
                {
                    continue;
                }
                
                var implantScrewEibb = implantComponent.GetImplantBuildingBlock(IBB.Screw, correspondingImplantCasePref);
                var implantScrews = objectManager.GetAllBuildingBlocks(implantScrewEibb);
                if (!implantScrews.Any())
                {
                    continue;
                }

                foreach (var implantScrew in implantScrews)
                {
                    guidePref.LinkedImplantScrews.Add(implantScrew.Id);
                }

                if (!(linkedImplantScrews.All(guidePref.LinkedImplantScrews.Contains) && guidePref.LinkedImplantScrews.All(linkedImplantScrews.Contains)))
                {
                    NotifyBuildingBlockHasChanged(guidePref);
                }
            }
        }

        public static void UnlinkAllImplantScrews(CMFImplantDirector director, ICaseData implantData)
        {
            var implantComponent = new ImplantCaseComponent();
            var screwBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, implantData);
            var objectManager = new CMFObjectManager(director);
            var implantScrewIds = objectManager.GetAllBuildingBlockIds(screwBlock);

            foreach (var guidePref in director.CasePrefManager.GuidePreferences)
            {
                var unlinked = guidePref.LinkedImplantScrews.RemoveAll(implantScrewIds.Contains);
                if (unlinked > 0)
                {
                    NotifyBuildingBlockHasChanged(guidePref);
                }
            }
        }

        public static void UnlinkAllImplantScrews(CMFImplantDirector director)
        {
            foreach (var implantPref in director.CasePrefManager.CasePreferences)
            {
                UnlinkAllImplantScrews(director, implantPref);
            }
        }

        public static void UnlinkImplantScrew(CMFImplantDirector director, Guid implantScrewId)
        {
            foreach (var guidePref in director.CasePrefManager.GuidePreferences)
            {
                if (guidePref.LinkedImplantScrews.Contains(implantScrewId))
                {
                    guidePref.LinkedImplantScrews.Remove(implantScrewId);
                    NotifyBuildingBlockHasChanged(guidePref);
                    break;
                }
            }
        }

        public static void ReplaceLinkedImplantScrew(CMFImplantDirector director, Guid implantScrewIdToReplace, Guid newImplantScrewId)
        {
            var newImplantScrew = director.Document.Objects.Find(newImplantScrewId);

            foreach (var guidePref in director.CasePrefManager.GuidePreferences)
            {
                if (guidePref.LinkedImplantScrews.Contains(implantScrewIdToReplace))
                {
                    guidePref.LinkedImplantScrews.Remove(implantScrewIdToReplace);

                    if (newImplantScrew != null)
                    {
                        guidePref.LinkedImplantScrews.Add(newImplantScrewId);
                    }

                    NotifyBuildingBlockHasChanged(guidePref);
                    break;
                }
            }
        }

        public static bool GetRegisteredBarrelIdAndObject(CMFImplantDirector director, Screw srew, out Guid registeredBarrelId, out RhinoObject registeredBarrelObject)
        {
            registeredBarrelId = srew.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel) ?
                srew.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] : Guid.Empty;
            registeredBarrelObject = null;

            if (registeredBarrelId == Guid.Empty)
            {
                return false;
            }

            registeredBarrelObject = director.Document.Objects.Find(registeredBarrelId);
            return true;
        }

        public static bool ConvertRegisteredBarrelIdsToImplantScrewIds(CMFImplantDirector director)
        {
            var hasOldLinkage = false;

            foreach (var guidePref in director.CasePrefManager.GuidePreferences)
            {
                var linkedRegisteredBarrels = guidePref.LinkedImplantScrews.ToList();
                if (!linkedRegisteredBarrels.Any())
                {
                    continue;
                }

                var testId = linkedRegisteredBarrels.First();
                var rhinoObject = director.Document.Objects.Find(testId);
                if (rhinoObject == null)
                {
                    throw new Exception("Error in Implant-Guide linkage!");
                }
                else if (rhinoObject is Screw)
                {
                    break;
                }
                else
                {
                    hasOldLinkage = true;
                    break;
                }
            }

            if (hasOldLinkage)
            {
                var objectManager = new CMFObjectManager(director);
                var implantScrews = objectManager.GetAllBuildingBlocks(IBB.Screw);
                var map = new Dictionary<Guid, Guid>();

                foreach (Screw screw in implantScrews)
                {
                    if (screw.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel))
                    {
                        map.Add(screw.ScrewGuideAidesInDocument[IBB.RegisteredBarrel], screw.Id);
                    }
                }

                foreach (var guidePref in director.CasePrefManager.GuidePreferences)
                {
                    var linkedRegisteredBarrels = guidePref.LinkedImplantScrews.ToList();
                    if (!linkedRegisteredBarrels.Any())
                    {
                        continue;
                    }

                    foreach (var linkedRegisteredBarrel in linkedRegisteredBarrels)
                    {
                        if (!map.ContainsKey(linkedRegisteredBarrel))
                        {
                            throw new Exception("Error in Implant-Guide linkage!");
                        }

                        var screwId = map[linkedRegisteredBarrel];
                        var index = guidePref.LinkedImplantScrews.IndexOf(linkedRegisteredBarrel);
                        guidePref.LinkedImplantScrews.Remove(linkedRegisteredBarrel);
                        guidePref.LinkedImplantScrews.Insert(index, screwId);
                    }
                }
            }

            return hasOldLinkage;
        }

        public static void NotifyBuildingBlockHasChanged(CMFImplantDirector director, Guid implantScrewId)
        {
            NotifyBuildingBlockHasChanged(director, new List<Guid>{ implantScrewId });
        }

        public static void NotifyBuildingBlockHasChanged(CMFImplantDirector director, List<Guid> implantScrewIds)
        {
            foreach (var guidePref in director.CasePrefManager.GuidePreferences)
            {
                if (guidePref.LinkedImplantScrews.Any(id => implantScrewIds.Contains(id)))
                {
                    NotifyBuildingBlockHasChanged(guidePref);
                }
            }
        }

        private static void NotifyBuildingBlockHasChanged(GuidePreferenceDataModel guidePref)
        {
            guidePref.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.RegisteredBarrel });
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Guide {guidePref.NCase} - RegisteredBarrel changed!");
        }
    }
}
