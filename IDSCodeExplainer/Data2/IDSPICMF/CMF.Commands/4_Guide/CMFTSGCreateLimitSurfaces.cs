using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Creators;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.PICMF.Drawing;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace IDS.PICMF.CMF
{
    [System.Runtime.InteropServices.Guid("4AD0F166-C9A0-4BA5-AF51-F8B45066A288")]
    [IDSCMFCommandAttributes(DesignPhase.TeethBlock)]
    public class CMFTSGCreateLimitSurfaces : CmfCommandBase
    {
        public CMFTSGCreateLimitSurfaces()
        {
            TheCommand = this;
            VisualizationComponent = new DrawSurfaceVisualization();
        }

        public static CMFTSGCreateLimitSurfaces TheCommand { get; private set; }
        public override string EnglishName => "CMFTSGCreateLimitSurfaces";
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (!TSGGuideCommandHelper.PromptForCastPart(
                    director,
                    mode,
                    (DrawSurfaceVisualization)VisualizationComponent,
                    out var castObject,
                    out var castPartType))
            {
                return Result.Cancel;
            }

            var console = new IDSRhinoConsole();
            var limitSurfaceCreator = new LimitSurfaceCreator(console)
            {
                CastPart = RhinoMeshConverter.ToIDSMesh((Mesh)castObject.Geometry)
            };
            // Get mesh and draw surfaces
            var drawResult = GetUserPoints((Mesh)castObject.Geometry);
            if (drawResult == null || drawResult.ControlPoints == null || drawResult.ControlPoints.Count == 0)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "No points were selected.");
                return Result.Cancel;
            }
            // Create and add surfaces
            // Check orientation of points
            var orientedPoints = TeethSupportedGuideUtilities.EnsureClockwiseOrientation(drawResult.ControlPoints);
            var idsOrientedPoints = orientedPoints.ConvertAll(RhinoPoint3dConverter.ToIPoint3D);

            limitSurfaceCreator.CreateLimitSurfacesAsMesh(idsOrientedPoints, drawResult.ExtensionLength);
            if (!limitSurfaceCreator.IsSuccessful)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Failed to create limit surfaces");
                return Result.Failure;
            }

            var addedToDocument = TSGGuideCommandHelper.AddLimitSurfaceToDocument(
                director,
                drawResult.InnerSurfaces[0],
                limitSurfaceCreator,
                castObject);
            if (!addedToDocument)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"Failed to add limiting surface to layer.");
                return Result.Failure;
            }

            foreach (var guidePreferenceDataModel in director.CasePrefManager.GuidePreferences)
            {
                TeethSupportedGuideUtilities.InvalidateTeethBlock(director, guidePreferenceDataModel);
            }


            doc.Views.Redraw();
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Successfully created limiting surface for {castPartType.ToString()}.");
            return Result.Success;
        }

        private DrawLimitSurfaceResult GetUserPoints(Mesh mesh)
        {
            var context = new DrawSurfaceDataContext { PatchTubeDiameter = 0.3 };
            var drawSurface = new DrawLimitSurface(context, mesh);
            drawSurface.SetCommandPrompt("Pick point(s) to create limiting surfaces. Default extension surface is 10mm");

            return drawSurface.Execute() ? drawSurface.DrawSurfaceResult : null;
        }
    }
}