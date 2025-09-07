using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Creators;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using IDSPICMF.Drawing.SurfaceDrawing;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.CMF
{
    [System.Runtime.InteropServices.Guid("E05A8965-68B4-4DA2-A8F5-6B0341AC7D19")]
    [IDSCMFCommandAttributes(DesignPhase.TeethBlock)]
    public class CMFTSGEditLimitSurfaces : CmfCommandBase
    {
        public CMFTSGEditLimitSurfaces()
        {
            TheCommand = this;
            VisualizationComponent = new DrawSurfaceVisualization();
        }

        public static CMFTSGEditLimitSurfaces TheCommand { get; private set; }
        public override string EnglishName => "CMFTSGEditLimitSurfaces";
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            var proPlan = new ProPlanImportComponent();
            var visualization = (DrawSurfaceVisualization)VisualizationComponent;

            if (!TSGGuideCommandHelper.IsLimitSurfaceExist(objectManager, out var limitSurfaceIbb))
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "No limit surfaces found to edit.");
                return Result.Cancel;
            }
            
            TeethSupportedGuideUtilities.GetCastPartAvailability(objectManager, out List<ExtendedImplantBuildingBlock> availableParts, out _);
            visualization.SetCastAndSurfacesVisibility(doc, limitSurfaceIbb, availableParts, true);

            var surfaceObject = mode == RunMode.Scripted
                ? TSGGuideCommandHelper.GetSurfaceFromScript(availableParts, limitSurfaceIbb, director)
                : TSGGuideCommandHelper.GetSurfaceFromUser(director, limitSurfaceIbb);
            
            if (surfaceObject == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "No limiting surface was selected.");
                return Result.Cancel;
            }

            var ibb = limitSurfaceIbb.FirstOrDefault(b => b.ToString() == surfaceObject.Name);
                var patchData = TeethSupportedGuideUtilities.GetPatchDatas(director, ibb);
            var castPart = surfaceObject.Attributes.UserDictionary.GetString("CastPart");
            var castObj = objectManager.GetBuildingBlock(castPart);

            visualization.SetCastAndSurfacesVisibility(doc,
                limitSurfaceIbb.Where(b => b.ToString() != surfaceObject.Name).ToList(),
                availableParts.Where(b => b.Block.Name != castObj.Name).ToList(),
                false);

            var editSurface = new EditLimitSurfaceHelper((Mesh)castObj.Geometry, director);
            var res = editSurface.Execute(patchData);

            if (!res)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Failed to edit the limiting surface.");
            return Result.Failure;
            }

            var castPartIbb = ProPlanImportUtilities.GetProPlanImportExtendedImplantBuildingBlock(director, castObj);
            var castPartName = proPlan.GetPartName(castPartIbb.Block.Name);
            var limitSurfaceCreator = new LimitSurfaceCreator(new IDSRhinoConsole())
            {
                CastPart = RhinoMeshConverter.ToIDSMesh((Mesh)castObj.Geometry)
            };
            var innerSurface = editSurface.EditSurfaceResult.Surfaces.Values.First();
            
            var surfaceData = (PatchSurface)innerSurface.GuideSurfaceData;
            var orientedPoints = 
                TeethSupportedGuideUtilities.EnsureClockwiseOrientation(
                    surfaceData.ControlPoints);
            var idsOrientedPoints = orientedPoints.ConvertAll(RhinoPoint3dConverter.ToIPoint3D);
            limitSurfaceCreator.CreateLimitSurfacesAsMesh(idsOrientedPoints, surfaceData.Diameter);

            if (!limitSurfaceCreator.IsSuccessful)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Failed to create limit surfaces");
                return Result.Failure;
            }

            if (!TSGGuideCommandHelper.AddLimitSurfaceToDocument(director, innerSurface, limitSurfaceCreator, castObj))
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Failed to add limiting surface to layer.");
                return Result.Failure;
            }

            IDSPluginHelper.WriteLine(LogCategory.Default, $"Successfully updated limiting surface for {castPartName}.");

            foreach (var guidePreferenceDataModel in director.CasePrefManager.GuidePreferences)
            {
                TeethSupportedGuideUtilities.InvalidateTeethBlock(director, guidePreferenceDataModel);
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}
