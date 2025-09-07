using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Visualization;
using IDS.PICMF.Helper;
using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("c3c8ad02-89c7-43eb-b9bd-11c2922fc72e")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.RegisteredBarrel)]
    public class CMFToggleBarrelInfoBubble : CmfCommandBase
    {
        static CMFToggleBarrelInfoBubble _instance;
        public CMFToggleBarrelInfoBubble()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFToggleBarrelInfoBubble command.</summary>
        public static CMFToggleBarrelInfoBubble Instance => _instance;

        public override string EnglishName => "CMFToggleBarrelInfoBubble";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var screwManager = new ScrewManager(director);
            var screwsWithOverriddenBarrels = GetScrewsWithOverriddenBarrels(screwManager, screwManager.GetAllScrews(false));

            var conduitProxyInstance = BarrelInfoConduitProxy.GetInstance();
            var isShowing = conduitProxyInstance.IsShowing();

            if (!isShowing && screwsWithOverriddenBarrels.Any())
            {
                conduitProxyInstance.SetUp(screwsWithOverriddenBarrels);
                conduitProxyInstance.Show(true);
                BarrelInfoBubbleToggleOffUtilities.SubscribeEvent();
            }
            else
            {
                conduitProxyInstance.Show(false);
                conduitProxyInstance.Reset();
                BarrelInfoBubbleToggleOffUtilities.UnsubscribeEvent();
            }

            return Result.Success;
        }

        private List<Screw> GetScrewsWithOverriddenBarrels(ScrewManager screwManager, List<Screw> screws)
        {
            var screwsWithOverriddenBarrels = new List<Screw>();
            foreach (var screw in screws)
            {
                var screwImplantPreferenceModel = screwManager.GetImplantPreferenceTheScrewBelongsTo(screw);
                var selectedBarrelType = screwImplantPreferenceModel.SelectedBarrelType;
                if (screw.BarrelType == selectedBarrelType)
                {
                    continue;
                }

                screwsWithOverriddenBarrels.Add(screw);
            }

            return screwsWithOverriddenBarrels;
        }
    }
}