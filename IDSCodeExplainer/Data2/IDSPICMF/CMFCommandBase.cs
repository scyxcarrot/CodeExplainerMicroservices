using IDS.CMF;
using IDS.CMF.Constants;
using IDS.Core.CommandBase;
using IDS.PICMF.Helper;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using System.Linq;

namespace IDS.PICMF
{
    public abstract class CmfCommandBase : CommandBase<CMFImplantDirector>
    {
        private void SetMeasurementIsVisible(Layer x, bool isVisible)
        {
            x.IsVisible = isVisible;

            var childLayers = x.GetChildren();

            if (childLayers != null)
            {
                foreach (var childLayer in childLayers)
                {
                    childLayer.IsVisible = isVisible;
                }
            }
        }

        private void DoMeasurementVisibility(RhinoDoc doc, CMFImplantDirector director)
        {
            if (EnglishName == CommandEnglishName.CMFDoMeasurements || EnglishName == CommandEnglishName.CMFDeleteMeasurements)
            {
                doc.Layers?.ToList().ForEach(x =>
                {
                    if (x.Name != null && !x.IsDeleted && x.Name.Contains(LayerName.MeasurementsPrefix))
                    {
                        if (x.Name.Contains(director.CurrentDesignPhaseName))
                        {
                            SetMeasurementIsVisible(x, true);
                        }
                        else
                        {
                            SetMeasurementIsVisible(x, false);
                        }

                    }
                });
            }

            doc.Views.Redraw();
        }

        private void DisableTSGGuideAnalysis(CMFImplantDirector director)
        {
            if (EnglishName != CommandEnglishName.CMFTSGTeethImpressionDepthAnalysis && EnglishName != CommandEnglishName.CMFTSGTeethBlockThicknessAnalysis)
            {
                TSGGuideCommandHelper.DisableIfHasActiveAnalysis(director);
            }
        }

        public override bool CheckCommandCanExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (!base.CheckCommandCanExecute(doc, mode, director))
            {
                return false;
            }

            DoMeasurementVisibility(doc, director);
            DisableTSGGuideAnalysis(director);
            return true;
        }

        public override void OnCommandJustExecuted(RhinoDoc doc, CMFImplantDirector director)
        {
            DoMeasurementVisibility(doc, director);
        }

        protected override void AddCaseInfo(RhinoDoc doc, CMFImplantDirector director)
        {
            base.AddCaseInfo(doc, director);
            TrackingParameters.Add("N Implants", director.CasePrefManager.CasePreferences.Count.ToString());
            TrackingParameters.Add("N Guides", director.CasePrefManager.GuidePreferences.Count.ToString());

            var implantTypeStrings = director.CasePrefManager.CasePreferences.Select(x => x.CasePrefData.ImplantTypeValue).ToList();
            implantTypeStrings.Sort();

            var guideTypeStrings = director.CasePrefManager.GuidePreferences.Select(x => x.GuidePrefData.GuideTypeValue).ToList();
            guideTypeStrings.Sort();

            var typeOfImplantString = implantTypeStrings.Any() ? string.Join(",", implantTypeStrings) : "None";
            var typeOfGuideString = guideTypeStrings.Any() ? string.Join(",", guideTypeStrings) : "None";

            TrackingParameters.Add("Type of Implants", typeOfImplantString);
            TrackingParameters.Add("Type of Guides", typeOfGuideString);
        }

    }
}
