using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.Enumerators;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Visualization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace IDS.PICMF.Forms
{
    public static class ImplantGuideRenumberingHelper
    {
        public static void HandleOnTextChanged(ref ICaseData model, CMFImplantDirector director, 
            TextBox tb, KeyEventArgs e)
        {
            if (tb.Text == "-1") //To prevent infinite Loop, SetCaseNumber will trigger this event again.
            {
                e.Handled = true;
                return;
            }

            if (tb.Text.Any() && !StringUtilities.CheckIsDigit(tb.Text))
            {
                model.SetCaseNumber(-1);
                e.Handled = true;
                return;
            }

            var newNumber = tb.Text.Any() ? int.Parse(tb.Text) : -1;

            if (director.CurrentDesignPhase != DesignPhase.Planning &&
                director.CurrentDesignPhase != DesignPhase.Implant &&
                director.CurrentDesignPhase != DesignPhase.Guide)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning,
                    "Numbering can only be done in Planning/Implant/Guide phase.");
                e.Handled = true;
                return;
            }

            var currentNumber = model.NCase;
            if (model.NCase == newNumber) //To prevent infinite Loop, SetCaseNumber will trigger this event again.
            {
                e.Handled = true;
                return;
            }

            var isImplant = model is ImplantPreferenceModel;
            var numberAlreadyExist = isImplant ? PreferencePanelHelper.NumberPresentInImplant(newNumber) : PreferencePanelHelper.NumberPresentInGuide(newNumber);

            if (numberAlreadyExist && newNumber != -1)
            {
                var implantOrGuideString = isImplant ? "implant" : "guide";
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"Number already being used in other {implantOrGuideString}! Please use a different number.");
                e.Handled = true;
                model.SetCaseNumber(currentNumber);
                return;
            }

            var doc = director.Document;

            director.CasePrefManager.HandleRenumberCaseNumber(model, newNumber);

            RhinoLayerUtilities.DeleteEmptyLayers(doc);

            var visualizationComponent = new CMFImplantPreviewVisualization();
            visualizationComponent.CommonVisualization(doc, true, true);

            if (isImplant)
            {
                PreferencePanelHelper.InvalidateAllLinkedImplantDisplayStringOnGuidePreferences(director);
            }
            else
            {
                PreferencePanelHelper.InvalidateAllLinkedGuideDisplayStringOnImplantPreferences(director);
            }

            doc.Views.Redraw();
        }

    }
}
