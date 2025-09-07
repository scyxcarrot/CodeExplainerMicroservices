using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class GuideFlangeObjectHelper
    {
        private const string FlangeCurveKey = "flange_curve";
        private const string FlangeHeightKey = "flange_height";

        private readonly CMFImplantDirector _director;

        public GuideFlangeObjectHelper(CMFImplantDirector director)
        {
            this._director = director;
        }

        public void AddNewFlange(ICaseData caseData, Mesh flangeMesh, Curve flangeCurve, double flangeHeight)
        {
            var objManager = new CMFObjectManager(_director);
            var guideComponent = new GuideCaseComponent();
            var buildingBlock = guideComponent.GetGuideBuildingBlock(IBB.GuideFlange, caseData);
            var id = objManager.AddNewBuildingBlock(buildingBlock, flangeMesh);
            var rhinoObject = _director.Document.Objects.Find(id);
            rhinoObject.Attributes.UserDictionary.Set(FlangeCurveKey, flangeCurve);
            rhinoObject.Attributes.UserDictionary.Set(FlangeHeightKey, flangeHeight);
        }

        public Curve GetFlangeCurve(RhinoObject flangeRhinoObj)
        {
            if (flangeRhinoObj.Attributes.UserDictionary.ContainsKey(FlangeCurveKey))
            {
                return (Curve)flangeRhinoObj.Attributes.UserDictionary[FlangeCurveKey];
            }

            throw new Exception("Missing flange curve information!");
        }

        public double GetFlangeHeight(RhinoObject flangeRhinoObj)
        {
            if (flangeRhinoObj.Attributes.UserDictionary.ContainsKey(FlangeHeightKey))
            {
                return (double)flangeRhinoObj.Attributes.UserDictionary[FlangeHeightKey];
            }

            throw new Exception("Missing flange height information!");
        }

        public void ReplaceExistingFlange(ICaseData caseData, Guid existingFlangeId, Mesh flangeMesh, Curve flangeCurve, double flangeHeight)
        {
            var objManager = new CMFObjectManager(_director);
            var guideComponent = new GuideCaseComponent();
            var buildingBlock = guideComponent.GetGuideBuildingBlock(IBB.GuideFlange, caseData);
            var id = objManager.SetBuildingBlock(buildingBlock, flangeMesh, existingFlangeId);
            var rhinoObject = _director.Document.Objects.Find(id);
            rhinoObject.Attributes.UserDictionary.Set(FlangeCurveKey, flangeCurve);
            rhinoObject.Attributes.UserDictionary.Set(FlangeHeightKey, flangeHeight);
        }

        public void InvalidateOldFlanges()
        {
            var objManager = new CMFObjectManager(_director);
            
            if (!_director.NeedToRegenerateGuideFlangeGuidingOutlines || !objManager.HasBuildingBlock(IBB.GuideSupport))
            {
                _director.NeedToRegenerateGuideFlangeGuidingOutlines = false; 
                return;
            }

            ProPlanImportUtilities.RegenerateGuideGuidingOutlines(objManager);

            var hasOldFlanges = HasOldFlanges();
            _director.CasePrefManager.NotifyBuildingBlockHasChangedToAll(new[] { IBB.GuideFlangeGuidingOutline });

            if (hasOldFlanges)
            {
                var message = "Guide flange(s) were invalidated. Corresponding guide(s) were deleted. Please re-draw them.";
                IDSPluginHelper.WriteLine(LogCategory.Warning, message);
                IDSDialogHelper.ShowSuppressibleMessage(message, "Outdated Guide Flanges Found", ShowMessageIcon.Warning);
            }

            _director.Document.ClearUndoRecords(true);
            _director.Document.ClearRedoRecords();
            RhinoLayerUtilities.DeleteEmptyLayers(_director.Document);
            _director.NeedToRegenerateGuideFlangeGuidingOutlines = false;
        }

        private bool HasOldFlanges()
        {
            var objManager = new CMFObjectManager(_director);
            var flanges = objManager.GetAllBuildingBlocks(IBB.GuideFlange);
            return flanges.Any(f => !f.Attributes.UserDictionary.ContainsKey(FlangeHeightKey));
        }
    }
}
