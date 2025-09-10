using IDS.CMF;
using IDS.CMF.Enumerators;
using IDS.Core.Enumerators;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;

namespace IDS.PICMF.Helper
{
    public class QCPhaseHelper
    {
        public static bool SelectQCPhase(CMFImplantDirector director, out DesignPhase selectedDesignPhase)
        {
            var go = new GetOption();
            go.SetCommandPrompt("Choose QC Phase.");
            go.AcceptNothing(true);

            var qcPhaseOptions = new List<DesignPhase> { DesignPhase.PlanningQC, DesignPhase.MetalQC };
            go.AddOptionEnumSelectionList("DesignPhase", qcPhaseOptions, 0);

            selectedDesignPhase = DesignPhase.None;

            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Cancel)
                {
                    return false;
                }

                if (res != GetResult.Option)
                {
                    continue;
                }
                selectedDesignPhase = qcPhaseOptions[go.Option().CurrentListOptionIndex];
                break;
            }

            if (!(selectedDesignPhase == DesignPhase.PlanningQC && director.CurrentDesignPhase == DesignPhase.PlanningQC ||
                  selectedDesignPhase == DesignPhase.MetalQC && director.CurrentDesignPhase == DesignPhase.MetalQC))
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, $"Not a valid option: {selectedDesignPhase}");
                return false;
            }

            return true;
        }
    }
}
