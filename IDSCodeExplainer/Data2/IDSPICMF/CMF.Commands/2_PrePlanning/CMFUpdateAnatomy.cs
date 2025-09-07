using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Invalidation;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    public abstract class CMFUpdateAnatomy : CmfCommandBase
    {
        public bool ExecuteOperation(RhinoDoc doc, RunMode mode, CMFImplantDirector director, string folderPath, RecutImporter recutImporter)
        {
            AllScrewGaugesProxy.Instance.IsEnabled = false;

            var invalidator = new ImportRecutInvalidator(director);
            invalidator.SetImplantSupportInputsDependencyGraph();

            var success = ImportRecut(director, folderPath, mode, recutImporter, out var partsThatChanged);

            if (!success)
            {
                invalidator.CleanUp();
                return false;
            }

            success = HandleIfOsteotomyPlanesHasChanged(director, doc, partsThatChanged);

            if (success)
            {

                invalidator.InvalidateDependentImportSupportInputs(partsThatChanged);

                var regenerateImplantSupportGuidingOutline = ImportRecutInvalidationUtilities.HasImplantSupportGuidingOutlineDependantParts(partsThatChanged);
                if (regenerateImplantSupportGuidingOutline)
                {
                    invalidator.UpdateImplantSupportGuidingOutlines();
                }
                invalidator.CleanUp();
            }

            CasePreferencePanel.GetView().InvalidateUI();

            return success;
        }

        private bool HandleIfOsteotomyPlanesHasChanged(CMFImplantDirector director, RhinoDoc doc, List<string> partsThatChanged)
        {
            var objectManager = new CMFObjectManager(director);
            var guideSupport = objectManager.GetBuildingBlock(IBB.GuideSupport);

            //Because in RecutImporter if guide support has been placed, the guide outlines will be recreated already.
            //Also take note that the import flow is importing osteotomy first (if present) then it will import the guide support.
            //This means when the guide outlines is created, it will always use the latest osteotomy planes.
            if (guideSupport != null && partsThatChanged.Any(x => guideSupport.Name.Contains(x)))
            {
                return true;
            }

            var osteotomyObjects = ProPlanImportUtilities.GetAllOriginalOsteotomyPartsRhinoObjects(doc);
            var existingOsteotomyPlanesName = osteotomyObjects.Select(x => x.Name).ToList();

            var hasOriginalOsteotomyParts = false;
            foreach (var x in partsThatChanged)
            {
                if (existingOsteotomyPlanesName.Contains(x))
                {
                    hasOriginalOsteotomyParts = true;
                    director.CasePrefManager.NotifyBuildingBlockHasChangedToAll(new[] { IBB.GuideFlangeGuidingOutline });
                    break;
                }
            }

            var guideSupportWrap = objectManager.GetBuildingBlock(IBB.GuideSurfaceWrap);
            if (guideSupportWrap != null && hasOriginalOsteotomyParts)
            {
                var success = ProPlanImportUtilities.RegenerateGuideGuidingOutlines(objectManager);

                if (!success)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"No Guide Flange Guiding outline created.");
                }

                director.CasePrefManager.NotifyBuildingBlockHasChangedToAll(new[] { IBB.GuideFlangeGuidingOutline });
                
                var osteotomyMeshes = osteotomyObjects.Select(o => (Mesh)o.Geometry.Duplicate()).ToList();
                success = RegenerateGuideSurfaces(objectManager, director.CasePrefManager.GuidePreferences, osteotomyMeshes, (Mesh)guideSupportWrap.Geometry.Duplicate());
                if (!success)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ImportRecut(CMFImplantDirector director, string folderPath, RunMode mode, RecutImporter recutImporter, out List<string> partsThatChanged)
        {
            partsThatChanged = new List<string>();
            var doc = director.Document;

            var mismatchedPartNames = recutImporter.GetPartsWithWrongNamingConvention(folderPath);
            var notGoingToBeImportedPartNames = recutImporter.GetPartsThatAreNotGoingToBeImported(folderPath);
            if ((mismatchedPartNames.Any() || notGoingToBeImportedPartNames.Any()) && mode == RunMode.Interactive)
            {
                var window = new ImportRecut();
                window.SetWrongPartNameList(mismatchedPartNames);
                window.SetNotGoingToBeImportedPartNameList(notGoingToBeImportedPartNames);
                var result = window.ShowDialog();
                if (result == false || result == null)
                {
                    return false;
                }
            }

            //Important! Order does matter! Import stls and swap (Note: not importing support mesh anymore)
            var success = recutImporter.ImportRecut(folderPath, out partsThatChanged, out var numTrianglesImported);
            if (success)
            {
                recutImporter.HandleGuideSupportRoIDependencies(folderPath, partsThatChanged);
                
                TrackingParameters.Add("Import Recut Meshes Total N Triangles", numTrianglesImported.ToString());
            }

            return success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            bool needToRedraw = false;
            AnalysisScaleConduit.ConduitProxy.Enabled = false;
            if (BoneThicknessAnalyzableObjectManager.CheckIfGotVertexColor(doc))
            {
                BoneThicknessAnalyzableObjectManager.HandleRemoveAllVertexColor(director);
                needToRedraw = true;
            }
            
            if (needToRedraw)
            {
                doc.Views.Redraw();
            }
        }

        private bool RegenerateGuideSurfaces(CMFObjectManager objectManager, List<GuidePreferenceDataModel> guidePreferences, List<Mesh> osteotomies, Mesh guideSurfaceWrap)
        {
            var guideComponent = new GuideCaseComponent();

            foreach (var guidePrefModel in guidePreferences)
            {
                var positiveGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.PositiveGuideDrawings, guidePrefModel);
                var negativeGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.NegativeGuideDrawing, guidePrefModel);
                var linkSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideLinkSurface, guidePrefModel);
                var solidSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideSolidSurface, guidePrefModel);
                var guideSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideSurface, guidePrefModel);

                var existingPositiveSurfaces = objectManager.GetAllBuildingBlocks(positiveGuideDrawingEibb).Select(s => (Mesh)s.Geometry).ToList();
                var existingNegativeSurfaces = objectManager.GetAllBuildingBlocks(negativeGuideDrawingEibb).Select(s => (Mesh)s.Geometry).ToList();
                var existingLinkSurfaces = objectManager.GetAllBuildingBlocks(linkSurfaceEibb).Select(s => (Mesh)s.Geometry).ToList();
                var existingSolidSurfaces = objectManager.GetAllBuildingBlocks(solidSurfaceEibb).Select(s => (Mesh)s.Geometry).ToList();

                if (!existingPositiveSurfaces.Any() && !existingNegativeSurfaces.Any() && !existingLinkSurfaces.Any())
                {
                    continue;
                }
                
                var guideSurfaces = GuideSurfaceUtilities.CreateGuideSurfaces(existingPositiveSurfaces, existingNegativeSurfaces, existingLinkSurfaces,
                                                                              existingSolidSurfaces, osteotomies, guideSurfaceWrap, guidePrefModel.CaseName);
                if (guideSurfaces == null || !guideSurfaces.Any())
                {
                    return false;
                }
                 
                var existingGuideSurfaces = objectManager.GetAllBuildingBlocks(guideSurfaceEibb);
                foreach (var surface in existingGuideSurfaces)
                {
                    objectManager.DeleteObject(surface.Id);
                }

                foreach (var surface in guideSurfaces)
                {
                    objectManager.AddNewBuildingBlock(guideSurfaceEibb, surface);
                }

                guidePrefModel.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.GuideSurface });
            }

            return true;
        }

        protected Result GetRegisterOriginalPartFlagForScriptedMode(ref bool registerOriginalPartForScriptedMode)
        {
            var result = RhinoGet.GetBool("RegisterOriginalPart", false, "No", "Yes", ref registerOriginalPartForScriptedMode);
            if (result != Result.Success)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Invalid input for RegisterOriginalPart");
            }
            return result;
        }

        protected Result GetProceedIfRepositionedFlagForScriptedMode(ref bool proceedIfRepositionedForScriptedMode)
        {
            var result = RhinoGet.GetBool("ProceedIfRepositioned", false, "No", "Yes", ref proceedIfRepositionedForScriptedMode);
            if (result != Result.Success)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Invalid input for ProceedIfRepositioned");
            }
            return result;
        }

        protected Result GetRegisterPlannedPartFlagForScriptedMode(ref bool registerPlannedPartForScriptedMode)
        {
            var result = RhinoGet.GetBool("RegisterPlannedPart", false, "No", "Yes", ref registerPlannedPartForScriptedMode);
            if (result != Result.Success)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Invalid input for RegisterPlannedPart");
            }
            return result;
        }
    }
}