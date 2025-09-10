using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    public abstract class CMFImplantScrewBaseCommand : CmfCommandBase
    {
        protected Screw SelectScrew(RhinoDoc doc, string prompt)
        {
            // Unlock screws
            var success = UnlockCalibratedScrews(doc);
            if (!success)
            {
                return null;
            }

            // Select screw
            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt(prompt);
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableTransparentCommands(false);

            var res = selectScrew.Get();
            if (res == GetResult.Object)
            {
                // Get selected screw
                var screw = selectScrew.Object(0).Object() as Screw;                
                return screw;
            }

            return null;
        }

        protected static Screw SelectAllScrew(RhinoDoc doc, string prompt)
        {
            // Unlock screws
            var success = UnlockScrews(doc);
            if (!success)
            {
                return null;
            }

            // Select screw
            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt(prompt);
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableTransparentCommands(false);

            var res = selectScrew.Get();
            if (res == GetResult.Object)
            {
                // Get selected screw
                var screw = selectScrew.Object(0).Object() as Screw;
                return screw;
            }

            return null;
        }

        private static bool UnlockScrews(RhinoDoc document)
        {
            // Unlock screws
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(document.DocumentId);
            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(false);

            foreach (var screw in screws)
            {
                document.Objects.Unlock(screw, true);
            }

            return true;
        }

        protected List<Screw> SelectMultipleScrew(RhinoDoc doc, string prompt)
        {
            // Unlock screws
            var success = UnlockCalibratedScrews(doc);
            if (!success)
            {
                return null;
            }

            // Select screw
            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt(prompt);
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableHighlight(true);
            selectScrew.EnableTransparentCommands(false);

            while (true)
            {
                var res = selectScrew.GetMultiple(0, 0);

                if (res == GetResult.Cancel || res == GetResult.Nothing)
                {
                    doc.Objects.UnselectAll();
                    doc.Views.Redraw();
                    return null;
                }

                if (res != GetResult.Object)
                {
                    continue;
                }

                var screws = doc.Objects.GetSelectedObjects(false, false)
                    .Select(o => o as Screw).Where(s => s != null).ToList();

                doc.Objects.UnselectAll();
                doc.Views.Redraw();

                return screws;
            }
        }

        private bool UnlockCalibratedScrews(RhinoDoc document)
        {
            // Unlock screws
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(document.DocumentId);
            var screwManager = new ScrewManager(director);
            var calibratedScrews = screwManager.GetCalibratedImplantScrews();

            if (!calibratedScrews.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "There are no calibrated screws to select");
                return false;
            }

            if (calibratedScrews.Count != screwManager.GetAllScrews(false).Count)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Uncalibrated screws are locked for the operation");
            }

            foreach (var calibratedScrew in calibratedScrews)
            {
                document.Objects.Unlock(calibratedScrew, true);
            }

            return true;
        }

        protected void AddImplantScrewTrackingParameter(string screwNumber)
        {
            TrackingParameters.Add("Affected Screw", screwNumber);
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }

        protected virtual void OnUndoRedo(object sender, CustomUndoEventArgs e)
        {
            var screwUndoRedo = (ScrewUndoRedo)e.Tag;

            if (e.CreatedByRedo)
            {
                screwUndoRedo.Redo?.Invoke();
            }
            else //Undo
            {
                screwUndoRedo.Undo?.Invoke();
            }

            e.Document.AddCustomUndoEvent("OnUndoRedo", OnUndoRedo, screwUndoRedo);
        }

        /// <summary>
        /// Recreate and register the screw barrels after rotation or translation
        /// </summary>
        /// <param name="director">director class to recreate the barrels and register them</param>
        protected void RecreateScrewBarrels(CMFImplantDirector director, CasePreferenceDataModel casePreference)
        {
            var objManager = new CMFObjectManager(director);

            var implantCaseComponent = new ImplantCaseComponent();
            var implantCaseBarrels = implantCaseComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, casePreference);
            var implantCaseScrews = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, casePreference);

            var registeredCaseBarrels = objManager.GetAllBuildingBlocks(implantCaseBarrels);
            var implantCaseScrewObjs = objManager.GetAllBuildingBlocks(implantCaseScrews);

            // if user just got into the implant phase without guide phase, registeredBarrels = 0
            if (!registeredCaseBarrels.Any() || registeredCaseBarrels.Count() == implantCaseScrewObjs.Count())
            {
                return;
            }

            Mesh guideSupport = null;
            if (objManager.HasBuildingBlock(IBB.GuideSupport))
            {
                guideSupport = (Mesh)objManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
            }
            var screwBarrelRegistration = new CMFBarrelRegistrator(director);
            screwBarrelRegistration.RegisterOnlyNewGuideRegisteredBarrel(guideSupport,
                out bool areAllBarrelsMeetingSpecs);

            if (areAllBarrelsMeetingSpecs)
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"New barrels meet specification.");
            }
            screwBarrelRegistration.Dispose();
        }
    }

    public struct ScrewUndoRedo
    {
        public Action Undo;
        public Action Redo;
    }
}