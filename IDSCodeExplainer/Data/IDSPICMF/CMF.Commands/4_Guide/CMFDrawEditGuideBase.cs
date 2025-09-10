using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Drawing;
using IDS.PICMF.Helper;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    public abstract class CMFDrawEditGuideBase : CmfCommandBase
    {
        protected void UnLockPatches(RhinoDoc doc, CMFImplantDirector director, GuidePreferenceDataModel guidePrefModel)
        {
            var objManager = new CMFObjectManager(director);
            var patchesEIbb = objManager.GetAllGuideExtendedImplantBuildingBlocksIDs(IBB.PositiveGuideDrawings, guidePrefModel);
            patchesEIbb.AddRange(objManager.GetAllGuideExtendedImplantBuildingBlocksIDs(IBB.NegativeGuideDrawing, guidePrefModel));
            patchesEIbb.ForEach(x => { doc.Objects.Unlock(x, true); });
        }

        protected void UnLockLinks(RhinoDoc doc, CMFImplantDirector director, GuidePreferenceDataModel guidePrefModel)
        {
            var objManager = new CMFObjectManager(director);
            var linksEIbb = objManager.GetAllGuideExtendedImplantBuildingBlocksIDs(IBB.GuideLinkSurface, guidePrefModel);
            linksEIbb.ForEach(x => { doc.Objects.Unlock(x, true); });
        }

        protected void UnlockSolids(RhinoDoc doc, CMFImplantDirector director, GuidePreferenceDataModel guidePrefModel)
        {
            var objManager = new CMFObjectManager(director);
            var solidsEIbb = objManager.GetAllGuideExtendedImplantBuildingBlocksIDs(IBB.GuideSolidSurface, guidePrefModel);
            solidsEIbb.ForEach(x => { doc.Objects.Unlock(x, true); });
        }

        protected Guid PromptForPreferenceId()
        {
            return GuidePreferencesHelper.PromptForPreferenceId();
        }

        protected bool InitializeDataContext(ref DrawGuideDataContext dataContext, bool includeNegativeOption, Guid prefId, CMFImplantDirector director, bool onlyPatchMode = false)
        {
            string diameterDisplayString;
            var objManager = new CMFObjectManager(director);
            var guidePrefModel = objManager.GetGuidePreference(prefId);
            dataContext.RoIMeshDefiner = GuideDrawingUtilities.CreateRoIDefinitionMesh(director, guidePrefModel);

            if (onlyPatchMode)
            {
                diameterDisplayString = $" +Patch: {dataContext.PatchTubeDiameter}";
            }
            else
            {
                diameterDisplayString = $"Current Diameter for Skeleton: {dataContext.SkeletonTubeDiameter}," +
                                $" +Patch: {dataContext.PatchTubeDiameter}";
            }

            if (includeNegativeOption)
            {
                diameterDisplayString += $",-Patch: {dataContext.NegativePatchTubeDiameter}";
            }

            IDSPluginHelper.WriteLine(LogCategory.Default, diameterDisplayString);

            return true;
        }

        protected Mesh GetLowLoDConstraintMesh(CMFImplantDirector director)
        {
            var objManager = new CMFObjectManager(director);
            var rhObject = objManager.GetBuildingBlock(IBB.GuideSurfaceWrap);

            Mesh lowLodDrawingBase;
            objManager.GetBuildingBlockLoDLow(rhObject.Id, out lowLodDrawingBase);

            if (!lowLodDrawingBase.IsValid)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Lod Low Guide Surface Wrap is invalid. Surfaces created might have issue! Please contact development team if your guide fails to be generated.");
                Msai.TrackException(new IDSException($"[INTERNAL] Lod Low Guide Surface Wrap is invalid."), "CMF");
            }

            return lowLodDrawingBase;
        }

        protected Mesh GetGuideSurfaceCreationLowLoDBaseModel(CMFImplantDirector director)
        {
            return GetLowLoDConstraintMesh(director);
        }

        protected bool ProcessDrawResult(CMFImplantDirector director, Mesh mesh,
            List<PatchData> positiveSurfacesResult, List<PatchData> negativeSurfacesResult,
            List<PatchData> linkSurfacesResult, List<PatchData> solidSurfacesResult,
            GuidePreferenceDataModel guidePrefModel)
        {
            var positiveSurfaces = positiveSurfacesResult.Select(s => s.Patch).ToList();
            var negativeSurfaces = negativeSurfacesResult.Select(s => s.Patch).ToList();
            var linkSurfaces = linkSurfacesResult.Select(s => s.Patch).ToList();
            var solidSurfaces = solidSurfacesResult.Select(s => s.Patch).ToList();

            var guideComponent = new GuideCaseComponent();
            var positiveGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.PositiveGuideDrawings, guidePrefModel);
            var negativeGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.NegativeGuideDrawing, guidePrefModel);
            var linkSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideLinkSurface, guidePrefModel);
            var solidSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideSolidSurface, guidePrefModel);
            var guideSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideSurface, guidePrefModel);

            var objectManager = new CMFObjectManager(director);
            var existingPositiveSurfaces = objectManager.GetAllBuildingBlocks(positiveGuideDrawingEibb).Select(s => (Mesh)s.Geometry);
            var existingNegativeSurfaces = objectManager.GetAllBuildingBlocks(negativeGuideDrawingEibb).Select(s => (Mesh)s.Geometry);
            var existingLinkSurfaces = objectManager.GetAllBuildingBlocks(linkSurfaceEibb).Select(s => (Mesh)s.Geometry);
            var existingSolidSurfaces = objectManager.GetAllBuildingBlocks(solidSurfaceEibb).Select(s => (Mesh)s.Geometry);

            positiveSurfaces.AddRange(existingPositiveSurfaces);
            negativeSurfaces.AddRange(existingNegativeSurfaces);
            linkSurfaces.AddRange(existingLinkSurfaces);
            solidSurfaces.AddRange(existingSolidSurfaces);

            var osteotomies = ProPlanImportUtilities.GetAllOriginalOsteotomyPartsRhinoObjects(director.Document).Select(o => (Mesh)o.Geometry).ToList();

            var guideSurfaces = GuideSurfaceUtilities.CreateGuideSurfaces(positiveSurfaces, negativeSurfaces, linkSurfaces, solidSurfaces, osteotomies, mesh, guidePrefModel.CaseName, out var totalTime, out var smoothSurfaceTime);
            if ((guideSurfaces == null || !guideSurfaces.Any()) && (positiveSurfaces.Any() || negativeSurfaces.Any() || linkSurfaces.Any() || solidSurfaces.Any()))
            {
                return false;
            }

            guidePrefModel.PositiveSurfaces.AddRange(positiveSurfacesResult);
            guidePrefModel.NegativeSurfaces.AddRange(negativeSurfacesResult);
            guidePrefModel.LinkSurfaces.AddRange(linkSurfacesResult);
            guidePrefModel.SolidSurfaces.AddRange(solidSurfacesResult);

            //If all Ok, then alter the documents
            foreach (var surface in positiveSurfaces)
            {
                if (!existingPositiveSurfaces.Contains(surface))
                {
                    objectManager.AddNewBuildingBlock(positiveGuideDrawingEibb, surface);
                }
            }

            foreach (var surface in negativeSurfaces)
            {
                if (!existingNegativeSurfaces.Contains(surface))
                {
                    objectManager.AddNewBuildingBlock(negativeGuideDrawingEibb, surface);
                }
            }
            
            foreach (var surface in linkSurfaces)
            {
                if (!existingLinkSurfaces.Contains(surface))
                {
                    objectManager.AddNewBuildingBlock(linkSurfaceEibb, surface);
                }
            }

            foreach (var surface in solidSurfaces)
            {
                if (!existingSolidSurfaces.Contains(surface))
                {
                    objectManager.AddNewBuildingBlock(solidSurfaceEibb, surface);
                }
            }

            var existingGuideSurfaces = objectManager.GetAllBuildingBlocks(guideSurfaceEibb);
            foreach (var surface in existingGuideSurfaces)
            {
                objectManager.DeleteObject(surface.Id);
            }

            if (guideSurfaces != null)
            {
                foreach (var surface in guideSurfaces)
                {
                    objectManager.AddNewBuildingBlock(guideSurfaceEibb, surface);
                }
            }

            guidePrefModel.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.GuideSurface });

            IDSPluginHelper.WriteLine(LogCategory.Default, $"Total time taken: {totalTime}(s).");
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Smoothing surface time taken: {smoothSurfaceTime}(s).");

            return true;
        }

        protected bool ProcessAppendedGuideResult(CMFImplantDirector director, Mesh mesh, DrawGuideResult drawGuideResult, GuidePreferenceDataModel guidePrefModel)
        {
            var result = new DeleteGuideResult(drawGuideResult.GuideBaseSurfaces,
                drawGuideResult.GuideBaseNegativeSurfaces, guidePrefModel.LinkSurfaces, guidePrefModel.SolidSurfaces);
            
            return ProcessDeleteGuideResult(director, mesh, result, guidePrefModel);
        }

        protected bool ProcessAppendedGuideLinkResult(CMFImplantDirector director, Mesh mesh, DrawGuideResult drawGuideResult, GuidePreferenceDataModel guidePrefModel)
        {
            var result = new DeleteGuideResult(guidePrefModel.PositiveSurfaces, guidePrefModel.NegativeSurfaces,
                drawGuideResult.GuideBaseSurfaces, guidePrefModel.SolidSurfaces);
           
            return ProcessDeleteGuideResult(director, mesh, result, guidePrefModel);
        }

        protected bool ProcessDeleteGuideResult(CMFImplantDirector director, Mesh mesh, DeleteGuideResult deleteGuideResult, GuidePreferenceDataModel guidePrefModel)
        {
            var guideComponent = new GuideCaseComponent();
            var positiveGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.PositiveGuideDrawings, guidePrefModel);
            var negativeGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.NegativeGuideDrawing, guidePrefModel);
            var linkGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideLinkSurface, guidePrefModel);
            var solidGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideSolidSurface, guidePrefModel);

            var positiveSurfaces = deleteGuideResult.GuideBaseSurfaces.ToList();
            var negativeSurfaces = deleteGuideResult.GuideBaseNegativeSurfaces.ToList();
            var linkSurfaces = deleteGuideResult.GuideLinkSurfaces.ToList();
            var solidSurfaces = deleteGuideResult.GuideSolidSurfaces.ToList();

            //Buffer if process fail
            var tempPositivePatches = new List<PatchData>();
            var tempNegativePatches = new List<PatchData>();
            var tempLinkPatches = new List<PatchData>();
            var tempSolidPatches = new List<PatchData>();

            guidePrefModel.PositiveSurfaces.ForEach(x => tempPositivePatches.Add(x));
            guidePrefModel.NegativeSurfaces.ForEach(x => tempNegativePatches.Add(x));
            guidePrefModel.LinkSurfaces.ForEach(x => tempLinkPatches.Add(x));
            guidePrefModel.SolidSurfaces.ForEach(x => tempSolidPatches.Add(x));
            guidePrefModel.PositiveSurfaces.Clear();
            guidePrefModel.NegativeSurfaces.Clear();
            guidePrefModel.LinkSurfaces.Clear();
            guidePrefModel.SolidSurfaces.Clear();

            var tmpExistingPositiveSurfaces = new List<Mesh>();
            var tmpExistingNegativeSurfaces = new List<Mesh>();
            var tmpExistingLinkSurfaces = new List<Mesh>();
            var tmpExistingSolidSurfaces = new List<Mesh>();

            var objectManager = new CMFObjectManager(director);
            var existingPositiveSurfaces = objectManager.GetAllBuildingBlocks(positiveGuideDrawingEibb).ToList();
            var existingNegativeSurfaces = objectManager.GetAllBuildingBlocks(negativeGuideDrawingEibb).ToList();
            var existingLinkSurfaces = objectManager.GetAllBuildingBlocks(linkGuideDrawingEibb).ToList();
            var existingSolidSurfaces = objectManager.GetAllBuildingBlocks(solidGuideDrawingEibb).ToList();

            existingPositiveSurfaces.ForEach(x => tmpExistingPositiveSurfaces.Add((Mesh)x.DuplicateGeometry()));
            existingNegativeSurfaces.ForEach(x => tmpExistingNegativeSurfaces.Add((Mesh)x.DuplicateGeometry()));
            existingLinkSurfaces.ForEach(x => tmpExistingLinkSurfaces.Add((Mesh)x.DuplicateGeometry()));
            existingSolidSurfaces.ForEach(x => tmpExistingSolidSurfaces.Add((Mesh)x.DuplicateGeometry()));

            foreach (var surface in existingPositiveSurfaces)
            {
                objectManager.DeleteObject(surface.Id);
            }

            foreach (var surface in existingNegativeSurfaces)
            {
                objectManager.DeleteObject(surface.Id);
            }

            foreach (var surface in existingLinkSurfaces)
            {
                objectManager.DeleteObject(surface.Id);
            }
            
            foreach (var surface in existingSolidSurfaces)
            {
                objectManager.DeleteObject(surface.Id);
            }

            if (!ProcessDrawResult(director, mesh, positiveSurfaces, negativeSurfaces, linkSurfaces, solidSurfaces,
                guidePrefModel))
            {
                tmpExistingPositiveSurfaces.ForEach(x =>
                {
                    objectManager.AddNewBuildingBlock(positiveGuideDrawingEibb, x);
                });

                tmpExistingNegativeSurfaces.ForEach(x =>
                {
                    objectManager.AddNewBuildingBlock(negativeGuideDrawingEibb, x);
                });

                tmpExistingLinkSurfaces.ForEach(x =>
                {
                    objectManager.AddNewBuildingBlock(linkGuideDrawingEibb, x);
                });

                tmpExistingSolidSurfaces.ForEach(x =>
                {
                    objectManager.AddNewBuildingBlock(solidGuideDrawingEibb, x);
                });

                guidePrefModel.PositiveSurfaces = tempPositivePatches;
                guidePrefModel.NegativeSurfaces = tempNegativePatches;
                guidePrefModel.LinkSurfaces = tempLinkPatches;
                guidePrefModel.SolidSurfaces = tempSolidPatches;

                return false;
            }

            return true;
        }

        protected void ClearHistory(RhinoDoc doc)
        {
            doc.ClearUndoRecords(true);
            doc.ClearRedoRecords();
        }
    }
}
