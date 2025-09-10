using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.V2.Utilities;
using Newtonsoft.Json;
using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;
using System.IO;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)
    [System.Runtime.InteropServices.Guid("368B95AA-4C3D-41E1-8742-B97C4D59C13A")]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestDumpGuidePrefPanelIdAndRegisteredBarrelId : CmfCommandBase
    {
        public CMF_TestDumpGuidePrefPanelIdAndRegisteredBarrelId()
        {
            Instance = this;
        }

        public static CMF_TestDumpGuidePrefPanelIdAndRegisteredBarrelId Instance { get; private set; }

        public override string EnglishName => "CMF_TestDumpGuidePrefPanelIdAndRegisteredBarrelId";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var guidePreferences = director.CasePrefManager.GuidePreferences;
            var guidePreferenceIdAndRegisteredBarrel = new List<GuidePrefPanelIdAndRegisteredBarrelIds>();

            foreach (var guidePreference in guidePreferences)
            {
                var guidePrefPanelId = guidePreference.CaseGuid;
                var linkedImplantScrewIds = guidePreference.LinkedImplantScrews;
                var registeredBarrelGuids = new List<Guid>();

                foreach (var linkedImplantScrewId in linkedImplantScrewIds)
                {
                    var screw = director.Document.Objects.Find(linkedImplantScrewId) as Screw;
                    registeredBarrelGuids.Add(screw.RegisteredBarrelId);
                }

                guidePreferenceIdAndRegisteredBarrel.Add(
                    new GuidePrefPanelIdAndRegisteredBarrelIds(guidePrefPanelId, registeredBarrelGuids));
            }

            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            var path = $"{workingDir}\\GuidePrefPanelIdAndRegisteredBarrelId.json";
            JsonUtilities.SerializeFile(path, guidePreferenceIdAndRegisteredBarrel);

            return Result.Success;
        }

        private class GuidePrefPanelIdAndRegisteredBarrelIds
        {
            public Guid GuidePreferenceId;
            public List<Guid> RegisteredBarrelIds;

            public GuidePrefPanelIdAndRegisteredBarrelIds(Guid guidePreferenceId, List<Guid> registeredBarrelIds)
            {
                GuidePreferenceId = guidePreferenceId;
                RegisteredBarrelIds = registeredBarrelIds;
            }
        }
    }
#endif
}
