using IDS.CMF.AttentionPointer;
using IDS.CMF.CasePreferences;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using Rhino.Geometry;
using System.Linq;

namespace IDS.CMF.Relations
{
    public class PhaseChanger : Core.Relations.PhaseChanger
    {
        public delegate void OnPhaseEnterExitComponentDelegate();
        public static readonly OnPhaseEnterExitComponentDelegate OnStartPlanningPhaseComponents;
        public static readonly OnPhaseEnterExitComponentDelegate OnStartPlanningPhaseFromLowerComponents;
        public static readonly OnPhaseEnterExitComponentDelegate OnStopPlanningPhaseComponents;

        public static bool StartPlanningPhase(CMFImplantDirector director)
        {
            // Set visualization
            Visibility.Default(director.Document);
            OnStartPlanningPhaseComponents?.Invoke();

            // Success
            return true;
        }

        public static bool StartPlanningFromLowerPhase(CMFImplantDirector director)
        {
            // Set visualization
            Visibility.Default(director.Document);
            OnStartPlanningPhaseFromLowerComponents?.Invoke();

            // Success
            return true;
        }

        public static bool StopPlanningPhase(CMFImplantDirector director, DesignPhase phase)
        {
            if (director.CasePrefManager.HasUnsetCasePreference())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Please ensure all case preference(s) implant are set!");
                return false;
            }

            if (director.CasePrefManager.HasInvalidCasePreferencesValues())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Please ensure all case preference(s) field(s) value(s) are not empty and valid!");
                return false;
            }

            if (director.CasePrefManager.HasUnsetGuidePreference())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Please ensure all guide preference(s) guide are set!");
                return false;
            }

            var implantScrews = new ScrewManager(director).GetAllScrews(false);
            if (implantScrews.Exists(s => s.Index == -1))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Please assign implant screw numbers!");
                return false;
            }

            var propertyHandler = new PropertyHandler(director);
            propertyHandler.SyncOutOfSyncProperties();

            // Set visualization
            OnStopPlanningPhaseComponents?.Invoke();

            // Success
            return true;
        }

        public static bool StartPlanningQCPhase(CMFImplantDirector director)
        {
            return true;
        }

        public static bool StopPlanningQCPhase(CMFImplantDirector director, DesignPhase phase)
        {
            return true;
        }

        public static bool StartImplantPhase(CMFImplantDirector director)
        {
            // Set visualization
            Visibility.ImplantDefault(director.Document);

            PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(director);

            // Success
            return true;
        }

        public static bool StopImplantPhase(CMFImplantDirector director, DesignPhase phase)
        {
            var implantScrews = new ScrewManager(director).GetAllScrews(false);
            if (implantScrews.Exists(s => s.Index == -1))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Please assign implant screw numbers!");
                return false;
            }

            if (OutdatedImplantSupportHelper.HasAnyOutdatedImplantSupports(director))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Outdated implant support! Please re-generate your implant support!");
                return false;
            }

            var screwNumberProxy = CMFScrewNumberBubbleConduitProxy.GetInstance();
            if (screwNumberProxy.IsVisible)
            {
                screwNumberProxy.IsVisible = false;
            }

            BoneThicknessAnalyzableObjectManager.HandleRemoveAllVertexColor(director);
            AnalysisScaleConduit.ConduitProxy.Enabled = false;
            AllScrewGaugesProxy.Instance.IsEnabled = false;
            PastilleAttentionPointer.Instance.HideAndClearDeformedPastille(director);

            // Success
            return true;
        }

        public static bool StartGuidePhase(CMFImplantDirector director)
        {
            director.GuidePhaseStarted = true;
            return true;
        }

        public static bool StopGuidePhase(CMFImplantDirector director, DesignPhase phase)
        {
            var guideScrews = new ScrewManager(director).GetAllScrews(true);
            if (guideScrews.Exists(x => x.Index == -1))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Please assign guide fixation screw numbers!");
                return false;
            }

            var proxy = CMFGuideScrewQcBubbleConduitProxy.Instance;
            proxy.TurnOff();
            AllGuideFixationScrewGaugesProxy.Instance.IsEnabled = false;

            var screwNumberProxy = CMFScrewNumberBubbleConduitProxy.GetInstance();
            if (screwNumberProxy.IsVisible)
            {
                screwNumberProxy.IsVisible = false;
            }

            return true;
        }

        public static bool StartTeethBlockPhase(CMFImplantDirector director)
        {
            return true;
        }

        public static bool StopTeethBlockPhase(CMFImplantDirector director, DesignPhase phase)
        {
            return true;
        }

        public static bool StartMetalQCPhase(CMFImplantDirector director)
        {
            var screwBarrelRegistration = new CMFBarrelRegistrator(director);
            var barrelRegistered = false;
            var objManager = new CMFObjectManager(director);

            Mesh guideSupport = null;

            if (objManager.HasBuildingBlock(IBB.GuideSupport))
            {
                guideSupport = (Mesh)objManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
            }
            
            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                bool dummy;
                if (!screwBarrelRegistration.RegisterScrewsBarrel(casePreferenceData, guideSupport, true, out dummy))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "New barrel registration failed.");
                    screwBarrelRegistration.Dispose();
                    return false;
                }
                else
                {
                    barrelRegistered = true;
                }
            }

            if (barrelRegistered)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "New barrels are registered to match the changes made.");
            }
            screwBarrelRegistration.Dispose();

            Visibility.Default(director.Document);
            return true;
        }

        public static bool StopMetalQCPhase(CMFImplantDirector director, DesignPhase phase)
        {
            return true;
        }

        public static bool ChangePhase(IImplantDirector director, DesignPhase targetPhase)
        {
            return IsAllImplantsAndGuidesAreNumbered((CMFImplantDirector)director) &&
                   ChangePhase(director, targetPhase, true);
        }

        public static bool ChangePhase(IImplantDirector director, DesignPhase targetPhase, bool askConfirmation)
        {
            return IsAllImplantsAndGuidesAreNumbered((CMFImplantDirector)director) &&
                   ChangePhase(director, DesignPhases.Phases[targetPhase], askConfirmation);
        }

        public static bool IsAllImplantsAndGuidesAreNumbered(CMFImplantDirector director)
        {
            if (director.CasePrefManager.CasePreferences.All(x => x.NCase != -1) &&
                director.CasePrefManager.GuidePreferences.All(x => x.NCase != -1))
            {
                return true;
            }

            IDSPluginHelper.WriteLine(LogCategory.Error, "Please ensure all Implants/Guides are already numbered!");
            return false;
        }
    }
}
