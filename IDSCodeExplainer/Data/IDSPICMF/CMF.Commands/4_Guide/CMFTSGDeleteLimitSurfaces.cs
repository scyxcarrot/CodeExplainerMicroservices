using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;

namespace IDS.PICMF.CMF
{
    [System.Runtime.InteropServices.Guid("2E8FC892-BD70-4D5F-8896-C99BB21E33F7")]
    [IDSCMFCommandAttributes(DesignPhase.TeethBlock)]
    public class CMFTSGDeleteLimitSurfaces : CmfCommandBase
    {
        public CMFTSGDeleteLimitSurfaces()
        {
            TheCommand = this;
            VisualizationComponent = new DrawSurfaceVisualization();
        }

        public static CMFTSGDeleteLimitSurfaces TheCommand { get; private set; }
        public override string EnglishName => "CMFTSGDeleteLimitSurfaces";
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            var hasLimitSurface = TSGGuideCommandHelper.IsLimitSurfaceExist(objectManager, out var limitSurfacesIbb);
            if (!hasLimitSurface)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "No limit surface found to delete.");
                return Result.Cancel;
            }

            TeethSupportedGuideUtilities.GetCastPartAvailability(objectManager, out List<ExtendedImplantBuildingBlock> availableParts, out _);

            ((DrawSurfaceVisualization)VisualizationComponent).SetCastAndSurfacesVisibility(doc, limitSurfacesIbb, availableParts, true);

            var surfaceObject = mode == RunMode.Scripted
                ? TSGGuideCommandHelper.GetSurfaceFromScript(availableParts, limitSurfacesIbb, director)
                : TSGGuideCommandHelper.GetSurfaceFromUser(director, limitSurfacesIbb);

            if (surfaceObject == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "No limit surface was deleted.");
                return Result.Cancel;
            }

            director.IdsDocument.Delete(surfaceObject.Id);

            IDSPluginHelper.WriteLine(LogCategory.Default, $"Deleted: {surfaceObject.Name}");

            foreach (var guidePreferenceDataModel in director.CasePrefManager.GuidePreferences)
            {
                TeethSupportedGuideUtilities.InvalidateTeethBlock(director, guidePreferenceDataModel);
                }

            return Result.Success;
        }
    }
}