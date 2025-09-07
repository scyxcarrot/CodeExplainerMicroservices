using IDS.CMF.CasePreferences;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.UI;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class ScrewEntitiesHelper
    {
        private readonly CMFImplantDirector director;

        public ScrewEntitiesHelper(CMFImplantDirector director)
        {
            this.director = director;
        }

        public void UpdateScrewEntities()
        {
            if (!director.NeedToUpdateScrewEntities)
            {
                UpdateMidfaceScrewEntities();
                return;
            }

            director.CasePrefManager.CasePreferences.ForEach(x =>
            {
                x.Graph.InvalidateGraph();
            });

            director.CasePrefManager.GuidePreferences.ForEach(x =>
            {
                x.Graph.InvalidateGraph();
            });

            var propertyHandler = new PropertyHandler(director);

            director.CasePrefManager.CasePreferences.ForEach(x =>
            {
                propertyHandler.RecalibrateImplantScrews(x);
            });

            director.CasePrefManager.GuidePreferences.ForEach(x =>
            {
                propertyHandler.RecalibrateGuideFixationScrews(x);
            });

            var message = "Implant screw entities and/or Guide fixation screw entities were updated.";
            message += "\nImplant(s) and/or Guide(s) were deleted. Please re-generate them.";
            IDSPluginHelper.WriteLine(LogCategory.Warning, message);
            IDSDialogHelper.ShowSuppressibleMessage(message, "Obsoleted Screw Entities Found", ShowMessageIcon.Warning);

            director.Document.ClearUndoRecords(true);
            director.Document.ClearRedoRecords();
            RhinoLayerUtilities.DeleteEmptyLayers(director.Document);

            director.NeedToUpdateScrewEntities = false;
        }

        // Support for 4C0411 due to introduce the changed in Midface screw
        public void UpdateMidfaceScrewEntities()
        {
            if (!director.NeedToUpdateMidfaceScrewEntities)
            {
                return;
            }

            var propertyHandler = new PropertyHandler(director);
            var updated = false;
            const string screwType = "Matrix Midface";

            director.CasePrefManager.CasePreferences.Where(c => c.CasePrefData.ScrewTypeValue.Contains(screwType)).ToList().ForEach(x =>
            {
                x.Graph.InvalidateGraph();
                propertyHandler.RecalibrateImplantScrews(x);
                updated = true;
            });

            director.CasePrefManager.GuidePreferences.Where(g => g.GuidePrefData.GuideScrewTypeValue.Contains(screwType)).ToList().ForEach(x =>
            {
                x.Graph.InvalidateGraph();
                propertyHandler.RecalibrateGuideFixationScrews(x);
                updated = true;
            });

            if (updated)
            {
                var message = "Matrix Midface screw entities in Implant(s) and/or Guide(s) were updated.";
                message += "\nImplant(s) and/or Guide(s) were deleted. Please re-generate them.";
                IDSPluginHelper.WriteLine(LogCategory.Warning, message);

                director.Document.ClearUndoRecords(true);
                director.Document.ClearRedoRecords();
                RhinoLayerUtilities.DeleteEmptyLayers(director.Document);
            }

            director.NeedToUpdateMidfaceScrewEntities = false;
        }
    }
}