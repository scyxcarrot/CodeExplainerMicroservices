using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Creators;
using IDS.CMF.V2.Logics;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Interface.Geometry;
using IDS.PICMF.Visualization;
using IDS.RhinoInterface.Converter;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Helper
{
    public static class TSGGuideCommandHelper
    {
        public static bool PromptForCastPart(CMFImplantDirector director, RunMode mode, DrawSurfaceVisualization visualizationComponent,
            out RhinoObject castObject, out ProPlanImportPartType castPartType)
        {
            castObject = null;
            castPartType = ProPlanImportPartType.NonProPlanItem;

            var objManager = new CMFObjectManager(director);
            var proPlan = new ProPlanImportComponent();

            // Check cast parts
            TeethSupportedGuideUtilities.GetCastPartAvailability(objManager, out var availableParts, out var missingParts);

            if (!availableParts.Any())
            {
                var missingPartNames = missingParts.Select(p => proPlan.GetPartName(p.Block.Name));
                IDSPluginHelper.WriteLine(LogCategory.Default, $"Missing required cast part(s): {string.Join(", ", missingPartNames)}");
                return false;
            }

            if (mode == RunMode.Scripted)
            {
                var inputCastPart = string.Empty;
                var res = RhinoGet.GetString("CastPart", false, ref inputCastPart);
                if (res == Result.Success)
                {
                    availableParts = availableParts
                        .Where(p => proPlan.GetPartName(p.Block.Name) == inputCastPart)
                        .ToList();
                }
            }

            // Select cast part
            if (!SelectCastPart(director, availableParts, visualizationComponent, out castObject, out castPartType))
            {
                return false;
            }

            return true;
        }

        public static bool DisableIfHasActiveAnalysis(CMFImplantDirector director)
        {
            var doc = director.Document;
            var hasActiveAnalysis = CastAnalysisManager.CheckIfGotVertexColor(doc) ||
                TeethBlockAnalysisManager.CheckIfGotVertexColor(director);

            if (hasActiveAnalysis)
            {
                CastAnalysisManager.HandleRemoveAllVertexColor(director); 
                TeethBlockAnalysisManager.HandleRemoveAllVertexColor(director);

                AnalysisScaleConduit.ConduitProxy.Enabled = false;

                doc.Views.Redraw();
            }

            return hasActiveAnalysis;
        }

        private static bool SelectCastPart(CMFImplantDirector director, List<ExtendedImplantBuildingBlock> availableParts, DrawSurfaceVisualization visualizationComponent,
            out RhinoObject castObject, out ProPlanImportPartType castPartType)
        {
            var doc = director.Document;
            visualizationComponent.SetCastVisibility(doc, availableParts, true);

            var proPlan = new ProPlanImportComponent();
            var objManager = new CMFObjectManager(director);

            // Auto-select if only one part
            if (availableParts.Count == 1)
            {
                var part = availableParts[0];
                var partName = proPlan.GetPartName(part.Block.Name);
                castObject = objManager.GetBuildingBlock(part.Block);
                castPartType = proPlan.GetBlock(partName).PartType;
                IDSPluginHelper.WriteLine(LogCategory.Default, $"Auto-selected: {castPartType}_{partName}");
                return true;
            }

            // Multi-select
            foreach (var part in availableParts)
            {
                director.Document.Objects.Unlock(objManager.GetBuildingBlock(part), true);
            }

            var selector = new GetObject();
            selector.SetCommandPrompt("Select a cast part");
            selector.EnablePreSelect(false, false);
            selector.EnablePostSelect(true);
            selector.EnableHighlight(false);

            while (true)
            {
                var result = selector.Get();
                if (result == GetResult.Cancel)
                {
                    castObject = null;
                    castPartType = ProPlanImportPartType.NonProPlanItem;
                    return false;
                }

                if (result == GetResult.Object)
                {
                    var selectedObj = director.Document.Objects.Find(selector.Object(0).ObjectId);
                    var selectedPart = availableParts.FirstOrDefault(p => p.Block.Name == selectedObj.Name);
                    if (selectedPart != null)
                    {
                        var partName = proPlan.GetPartName(selectedPart.Block.Name);
                        castObject = selectedObj;
                        castPartType = proPlan.GetBlock(partName).PartType;

                        // Hide other parts
                        var otherParts = availableParts.Where(p => p.Block.Name != selectedObj.Name).ToList();
                        visualizationComponent.SetCastVisibility(doc, otherParts, false);
                        return true;
                    }
                }
            }
        }

        private static bool SelectLimitSurface(CMFImplantDirector director, List<IBB> availableIbb, string commandPrompt, out RhinoObject rhinoObject)
        {
            var objManager = new CMFObjectManager(director);
            foreach (var ibb in availableIbb)
            {
                director.Document.Objects.Unlock(objManager.GetBuildingBlock(ibb), true);
            }

            var selector = new GetObject();
            selector.SetCommandPrompt(commandPrompt);
            selector.EnablePreSelect(false, false);
            selector.EnablePostSelect(true);
            selector.EnableHighlight(false);

            while (true)
            {
                var result = selector.Get();
                if (result == GetResult.Cancel)
                {
                    rhinoObject = null;
                    return false;
                }

                if (result == GetResult.Object)
                {
                    rhinoObject = director.Document.Objects.Find(selector.Object(0).ObjectId);
                    break;
                }
            }
            return true;
        }

        public static RhinoObject GetSurfaceFromScript(List<ExtendedImplantBuildingBlock> availableParts, List<IBB> limitSurfaceIbb, CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            var proPlan = new ProPlanImportComponent();

            var inputCastPart = string.Empty;
            if (RhinoGet.GetString("CastPart", false, ref inputCastPart) != Result.Success) return null;

            var castPart = availableParts.FirstOrDefault(p => proPlan.GetPartName(p.Block.Name) == inputCastPart);
            if (castPart == null) return null;

            var castTeeth = objectManager.GetBuildingBlock(castPart);
            var limitingSurfaceName = castTeeth?.Attributes.UserDictionary.GetString("LimitingSurface", null);

            if (limitingSurfaceName == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"No limiting surface found for {inputCastPart}.");
                return null;
            }

            var limitSurface = limitSurfaceIbb.FirstOrDefault(b => b.ToString() == limitingSurfaceName);
            var surfaceObject = objectManager.GetBuildingBlock(limitSurface);
            director.Document.Objects.Unlock(surfaceObject, true);
            return surfaceObject;
        }

        public static RhinoObject GetSurfaceFromUser(CMFImplantDirector director, List<IBB> limitSurfaceIbb)
        {
            return SelectLimitSurface(director, limitSurfaceIbb, "Select a limiting surface to edit:", out var surfaceObject)
                ? surfaceObject : null;
        }

        public static bool PromptAndCheckInformation(
            CMFImplantDirector director,
            out GuidePreferenceDataModel guidePreferenceDataModel,
            out IBB limitingSurfaceIbb,
            out IBB limitingSurfaceExtrusionIbb,
            out IBB bracketRegionIbb,
            out IBB bracketExtrusionIbb,
            out IBB teethBlockRoiIbb,
            out ExtendedImplantBuildingBlock castEIbb,
            out IBB finalSupportIbb,
            out IBB finalSupportWrappedIbb,
            out IBB reinforcementRegionIbb,
            out IBB reinforcementExtrusionIbb,
            out ExtendedImplantBuildingBlock teethBaseRegionEIbb,
            out ExtendedImplantBuildingBlock teethBaseExtrusionEIbb)
        {
            // prompt user for inputs
            var isMandible = TeethSupportedGuideUtilities.AskUserTeethType();
            
            // get values
            teethBaseRegionEIbb = null;
            teethBaseExtrusionEIbb = null;

            limitingSurfaceIbb = isMandible ?
                IBB.LimitingSurfaceMandible : IBB.LimitingSurfaceMaxilla;
            limitingSurfaceExtrusionIbb = isMandible ?
                IBB.LimitingSurfaceExtrusionMandible : IBB.LimitingSurfaceExtrusionMaxilla;
            bracketRegionIbb = isMandible ?
                IBB.BracketRegionMandible : IBB.BracketRegionMaxilla;
            bracketExtrusionIbb =
                isMandible ? IBB.BracketExtrusionMandible : IBB.BracketExtrusionMaxilla;
            reinforcementRegionIbb = isMandible ?
                IBB.ReinforcementRegionMandible : IBB.ReinforcementRegionMaxilla;
            reinforcementExtrusionIbb =
                isMandible ? IBB.ReinforcementExtrusionMandible : IBB.ReinforcementExtrusionMaxilla;

            teethBlockRoiIbb = isMandible ? IBB.TeethBlockROIMandible : IBB.TeethBlockROIMaxilla;
            finalSupportIbb = isMandible ? IBB.FinalSupportMandible : IBB.FinalSupportMaxilla;
            finalSupportWrappedIbb = isMandible ? IBB.FinalSupportWrappedMandible : IBB.FinalSupportWrappedMaxilla;

            castEIbb = GetCastEIbb(isMandible);

            var guideCaseNumberSuccess = GuidePreferencesHelper.PromptForGuideCaseNumber(
                director,
                out guidePreferenceDataModel);
            if (!guideCaseNumberSuccess)
            {
                return false;
            }

            var guideCaseComponent = new GuideCaseComponent();
            teethBaseRegionEIbb = guideCaseComponent.GetGuideBuildingBlock(IBB.TeethBaseRegion, guidePreferenceDataModel);
            teethBaseExtrusionEIbb = guideCaseComponent.GetGuideBuildingBlock(
                IBB.TeethBaseExtrusion, guidePreferenceDataModel);

            // check if IBB, cast and guide preference data model present
            var isPresent = CheckInputsPresent(
                director,
                castEIbb,
                limitingSurfaceIbb,
                teethBaseRegionEIbb);
            
            return isPresent;
        }

        private static ExtendedImplantBuildingBlock GetCastEIbb(bool isMandible)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var proPlanCastType = isMandible ?
                ProPlanImportPartType.MandibleCast : ProPlanImportPartType.MaxillaCast;
            var originalCastBlocks =
                proPlanImportComponent.Blocks
                    .Where(x => x.PartType == proPlanCastType)
                    .Where(b => ProPlanPartsUtilitiesV2.IsOriginalPart(b.PartNamePattern));
            var eIbbs =
                originalCastBlocks.Select(x => proPlanImportComponent.GetProPlanImportBuildingBlock(x.PartNamePattern));
            var castEIbb = eIbbs.First();

            return castEIbb;
        }

        private static bool CheckInputsPresent(
            CMFImplantDirector director,
            ExtendedImplantBuildingBlock castEIbb,
            IBB limitingSurfaceIbb,
            ExtendedImplantBuildingBlock teethBaseRegionEIbb)
        {
            var isIbbPresent = TeethSupportedGuideUtilities.CheckIfIbbsArePresent(
                director,
                new List<IBB>()
                {
                    limitingSurfaceIbb,
                }
            );

            var objectManager = new CMFObjectManager(director);
            var castExist = objectManager.HasBuildingBlock(castEIbb);
            var teethBaseRegionExist = objectManager.HasBuildingBlock(teethBaseRegionEIbb);
            if (!teethBaseRegionExist)
            {
                IDSPluginHelper.WriteLine(
                    LogCategory.Error,
                    $"{teethBaseRegionEIbb.Block.Layer} is not available");
            }

            return isIbbPresent && castExist && teethBaseRegionExist;
        }

        public static bool AddLimitSurfaceToDocument(CMFImplantDirector director, PatchData patchData, LimitSurfaceCreator creator, RhinoObject castTeeth)
        {
            var objectManager = new CMFObjectManager(director);
            var extendedIbb = ProPlanImportUtilities.GetProPlanImportExtendedImplantBuildingBlock(director, castTeeth);
            var proPlan = new ProPlanImportComponent();
            var partName = proPlan.GetPartName(extendedIbb.Block.Name);
            var partType = proPlan.GetBlock(partName).PartType;
            var surfaceIbb = partType == ProPlanImportPartType.MandibleCast ? IBB.LimitingSurfaceMandible : IBB.LimitingSurfaceMaxilla;

            if (objectManager.HasBuildingBlock(surfaceIbb))
            {
                var deleted = director.IdsDocument.Delete(objectManager.GetBuildingBlock(surfaceIbb).Id);
                if (!deleted)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, $"Failed to delete: {surfaceIbb}");
                    return false;
                }
            }

            var limitingSurfaceMesh = RhinoMeshConverter.ToRhinoMesh(creator.CreatedMesh);
            var meshId = IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                objectManager,
                director.IdsDocument,
                surfaceIbb,
                castTeeth.Id,
                limitingSurfaceMesh);

            if (meshId == Guid.Empty)
            {
                return false;
            }
            var limitSurfaceMesh = objectManager.GetBuildingBlock(surfaceIbb);
            if (limitSurfaceMesh != null)
            {
                patchData.Serialize(limitSurfaceMesh.Attributes.UserDictionary);

                var oriCurvePoints = string.Join("|", creator.OriginalCurvePoints.Select(p => $"{p.X},{p.Y},{p.Z}"));
                var outCurvePoints = string.Join("|", creator.OuterCurvePoints.Select(p => $"{p.X},{p.Y},{p.Z}"));
                limitSurfaceMesh.Attributes.UserDictionary.Set("OriginalCurvePoints", oriCurvePoints);
                limitSurfaceMesh.Attributes.UserDictionary.Set("OuterCurvePoints", outCurvePoints);
                limitSurfaceMesh.Attributes.UserDictionary.Set("CastPart", castTeeth.Name);
                limitSurfaceMesh.CommitChanges();
                castTeeth.Attributes.UserDictionary.Set("LimitingSurface", surfaceIbb.ToString());
                castTeeth.CommitChanges();
                return true;
            }
            return false;
        }

        public static bool IsLimitSurfaceExist(CMFObjectManager objectManager, out List<IBB> limitSurfacesIbb)
        {
            limitSurfacesIbb = new List<IBB>();
            var hasLimitSurfaceMax = objectManager.HasBuildingBlock(IBB.LimitingSurfaceMaxilla);
            var hasLimitSurfaceMan = objectManager.HasBuildingBlock(IBB.LimitingSurfaceMandible);

            if (hasLimitSurfaceMax)
            {
                limitSurfacesIbb.Add(IBB.LimitingSurfaceMaxilla);
            }

            if (hasLimitSurfaceMan)
            {
                limitSurfacesIbb.Add(IBB.LimitingSurfaceMandible);
            }

            return limitSurfacesIbb.Count > 0;
        }
    }
}
