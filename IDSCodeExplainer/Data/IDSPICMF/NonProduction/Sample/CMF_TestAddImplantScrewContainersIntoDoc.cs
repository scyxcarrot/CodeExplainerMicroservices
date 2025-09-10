#if (INTERNAL)

using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.NonProduction;
using Rhino;
using Rhino.Commands;
using System.Drawing;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("914626FA-BE35-43BB-8A56-566A34241F5D")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    [CommandStyle(Style.ScriptRunner)]
    public class CMF_TestAddImplantScrewContainersIntoDoc : CmfCommandBase
    {
        public CMF_TestAddImplantScrewContainersIntoDoc()
        {
            Instance = this;
        }

        public static CMF_TestAddImplantScrewContainersIntoDoc Instance { get; private set; }

        public override string EnglishName => "CMF_TestAddImplantScrewContainersIntoDoc";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            var screwObjects = objectManager.GetAllBuildingBlocks(IBB.Screw);
            foreach (var screwObj in screwObjects)
            {
                var screw = screwObj as Screw;
                var container = screw.GetScrewContainer();
                InternalUtilities.AddObject(container, "Testing::ImplantScrewContainer");

                var screwHeadRef = screw.GetScrewHeadRef();
                InternalUtilities.AddCurve(screwHeadRef, "Testing::ImplantScrewHeadRef", "ScrewHeadRef", Color.Red);
            }

            return Result.Success;
        }
    }
}

#endif