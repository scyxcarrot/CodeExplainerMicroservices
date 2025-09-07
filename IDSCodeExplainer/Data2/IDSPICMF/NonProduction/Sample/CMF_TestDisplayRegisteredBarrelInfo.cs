using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("C4563207-84BB-4FC3-BE4F-A2330C0FD2FD")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    [CommandStyle(Style.ScriptRunner)]
    public class CMF_TestDisplayRegisteredBarrelInfo : CmfCommandBase
    {
        public CMF_TestDisplayRegisteredBarrelInfo()
        {
            Instance = this;
        }

        public static CMF_TestDisplayRegisteredBarrelInfo Instance { get; private set; }

        public override string EnglishName => "CMF_TestDisplayRegisteredBarrelInfo";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);

            foreach (var guidePref in director.CasePrefManager.GuidePreferences)
            {
                RhinoApp.WriteLine($"Guide {guidePref.NCase}: ");

                var linkedImplantScrews = guidePref.LinkedImplantScrews;

                var screwObjects = new Dictionary<int, List<Screw>>();

                foreach (var implantScrewId in linkedImplantScrews)
                {
                    var rhinoObject = (Screw)director.Document.Objects.Find(implantScrewId);
                    if (rhinoObject == null)
                    {
                        throw new Exception("Unable to find screw object!");
                    }

                    var implantPref = objectManager.GetCasePreference(rhinoObject);

                    if (screwObjects.ContainsKey(implantPref.NCase))
                    {
                        screwObjects[implantPref.NCase].Add(rhinoObject);
                    }
                    else
                    {
                        screwObjects.Add(implantPref.NCase, new List<Screw> { rhinoObject });
                    }
                }

                var orderedDict = screwObjects.OrderBy(s => s.Key);

                foreach (var keyValuePair in orderedDict)
                {
                    RhinoApp.WriteLine($"Implant {keyValuePair.Key}: ");

                    var orderedScrew = keyValuePair.Value.OrderBy(s => s.Index);
                    foreach (var screwObj in orderedScrew)
                    {
                        RhinoApp.Write($"{screwObj.Index} - [{screwObj.Id}]: ");
                        var gotRegisteredBarrel = screwObj.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel);
                        if (gotRegisteredBarrel)
                        {
                            RhinoApp.WriteLine($"RegisteredBarrel = {screwObj.ScrewGuideAidesInDocument[IBB.RegisteredBarrel]}");
                        }
                        else
                        {
                            RhinoApp.WriteLine($"NO RegisteredBarrel!");
                        }
                    }
                }

                RhinoApp.WriteLine();
            }

            return Result.Success;
        }
    }

#endif
}
