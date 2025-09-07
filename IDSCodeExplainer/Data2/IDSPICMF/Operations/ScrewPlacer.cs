using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Operations
{
    public class ScrewPlacer
    {
        private readonly CMFImplantDirector _director;
        private readonly List<Screw> _guideFixationScrews;
        private readonly CMFObjectManager _objManager;

        public ScrewPlacer(CMFImplantDirector director)
        {
            _director = director;

            var scrManager = new ScrewManager(director);
            _guideFixationScrews = scrManager.GetAllScrews(true);

            _objManager = new CMFObjectManager(director);
        }

        private List<Brep> GetClosestScrewEye()
        {
            return _guideFixationScrews.Select(x => x.GetScrewEye()).ToList();
        }

        public Screw DoPlaceGuideFixationScrew(Mesh reducedConstraintMesh, Mesh originalConstraintMesh, Mesh calibrationConstraintMesh, GuidePreferenceModel guidePreferenceModel, out bool closeByHasScrewButNotShared)
        {
            closeByHasScrewButNotShared = false;

            var gpts = new GetPoint();
            gpts.SetCommandPrompt("Select head point to position the screw on the mesh.");
            gpts.Constrain(reducedConstraintMesh, false);
            gpts.PermitObjectSnap(false);
            gpts.DynamicDraw += PlaceScrew;
            gpts.AcceptNothing(true); // accept ENTER to confirm
            gpts.EnableTransparentCommands(false);

            while (true)
            {
                _director.Document.Views.Redraw();
                var getRes = gpts.Get(); // function only returns after clicking
                if (getRes == GetResult.Cancel)
                {
                    return null;
                }

                if (getRes == GetResult.Point)
                {
                    var headPointOriginal = gpts.Point();

                    //Get nearest Fixation screw if the point is on existing Guide Fixation Screw or Screw Eye
                    var refScrew = ScrewUtilities.FindClosestScrew(headPointOriginal, _guideFixationScrews, 1.0);
                    if (refScrew != null && guidePreferenceModel.GuidePrefData.GuideScrewTypeValue == refScrew.ScrewType)
                    {
                        var closestScrews = ScrewUtilities.FindScrewsAroundRadiusReferencedToScrew(headPointOriginal, _guideFixationScrews, 1.0, refScrew);

                        bool isAlreadyBelongToThisGuide = false;
                        foreach (var xScrew in closestScrews)
                        {
                            var xGuidePref = _objManager.GetGuidePreference(xScrew);

                            if (xGuidePref == guidePreferenceModel)
                            {
                                isAlreadyBelongToThisGuide = true;
                                break;
                            }
                        }

                        if (isAlreadyBelongToThisGuide)
                        {
                            IDSPluginHelper.WriteLine(LogCategory.Default, "Selected screw/screw eye already belongs to this guide.");
                            continue;
                        }

                        var closestScrew = closestScrews.FirstOrDefault();
                        var guidePref = _objManager.GetGuidePreference(closestScrew);

                        //Check if screw type is different?
                        var duplicateScrew = new Screw(_director, closestScrew.HeadPoint, closestScrew.TipPoint, guidePref.GuideScrewAideData.GenerateScrewAideDictionary(), -1,
                            guidePref.GuidePrefData.GuideScrewTypeValue);

                        if (duplicateScrew == null)
                        {
                            return null;
                        }

                        AddScrewInDocument(guidePreferenceModel, duplicateScrew);
                        duplicateScrew.ShareWithScrew(closestScrew);

                        closestScrews.ForEach(x =>
                        {
                            x.ShareWithScrew(duplicateScrew);
                            var screwsItShared = x.GetScrewItSharedWith();
                            duplicateScrew.ShareWithScrews(screwsItShared);
                        }); //should also already include closestScrews;

                        return duplicateScrew;
                    }
                    else if (refScrew != null && guidePreferenceModel.GuidePrefData.GuideScrewTypeValue != refScrew.ScrewType)
                    {
                        closeByHasScrewButNotShared = true;
                    }

                    var screwDir = -VectorUtilities.FindAverageNormal(originalConstraintMesh, headPointOriginal, 
                        ScrewAngulationConstants.AverageNormalRadiusGuideFixationScrew);
                    screwDir.Unitize();
#if INTERNAL
                    if (_director.IsTestingMode)
                    {
                        IDS.Core.NonProduction.InternalUtilities.AddPoint(headPointOriginal, "headPointOriginal", Color.Crimson);
                        IDS.Core.NonProduction.InternalUtilities.AddVector(headPointOriginal, screwDir, 25, Color.GreenYellow);
                    }
#endif

                var length =
                        Queries.GetDefaultForGuideFixationScrewScrewLength(guidePreferenceModel.SelectedGuideScrewType, guidePreferenceModel.SelectedGuideScrewStyle);

                    var tipPointOriginal = headPointOriginal + (screwDir * length);

                    var screw = new Screw(_director, headPointOriginal, tipPointOriginal, guidePreferenceModel.GuideScrewAideData.GenerateScrewAideDictionary(), -1,
                        guidePreferenceModel.GuidePrefData.GuideScrewTypeValue);
                    
                    var calibrator = new GuideFixationScrewCalibrator();
                    var newScrew = calibrator.LevelScrew(screw, calibrationConstraintMesh, null);
                    if (newScrew == null)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Error, "Please check support mesh is fixed.");
                        Msai.TrackException(new IDSException($"[INTERNAL] Placing guide fixation screw failed. Please check support mesh is fixed."), "CMF");
                        return null;
                    }

                    AddScrewInDocument(guidePreferenceModel, newScrew);
                    return newScrew;
                }
            }
        }

        private void AddScrewInDocument(GuidePreferenceModel guidePreferenceModel, Screw screw)
        {
            var guideCaseComponent = new GuideCaseComponent();
            var guideFixationScrewIbb = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, guidePreferenceModel);

            var objManager = new CMFObjectManager(_director);
            objManager.AddNewBuildingBlock(guideFixationScrewIbb, screw);
            screw.UpdateAidesInDocument();
        }

        private void PlaceScrew(object sender, GetPointDrawEventArgs e)
        {
            var point = e.CurrentPoint;
            e.Display.DrawSphere(new Sphere(point, 4.0), Color.Red);
        }
    }
}
