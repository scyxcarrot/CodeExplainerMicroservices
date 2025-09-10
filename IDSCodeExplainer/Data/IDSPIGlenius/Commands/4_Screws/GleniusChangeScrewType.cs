using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("83CCFB9C-6F16-4F6C-8D1E-BD2FCD0DDDEA")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.Screw)]
    public class GleniusChangeScrewType : CommandBase<GleniusImplantDirector>
    {
        public GleniusChangeScrewType()
        {
            m_thecommand = this;
            VisualizationComponent = new AddScrewVisualization();
        }

        public static GleniusChangeScrewType TheCommand => m_thecommand;

        private static GleniusChangeScrewType m_thecommand;

        public override string EnglishName => "GleniusChangeScrewType";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            var result = Result.Failure;
            Locking.UnlockScrews(doc);

            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select a screw to change it's type.");
            selectScrew.EnablePreSelect(true, true);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableTransparentCommands(false);

            var res = selectScrew.Get();
            if (res == GetResult.Object)
            {
                var oldScrew = selectScrew.Object(0).Object() as Screw;

                var getOption = new GetOption();
                getOption.SetCommandPrompt("Choose screw parameters.");
                getOption.AddOptionEnumList<ScrewType>("Type", oldScrew.ScrewType);
                getOption.EnableTransparentCommands(false);
                getOption.Get();

                if (getOption.CommandResult() != Result.Success)
                {
                    return getOption.CommandResult();
                }

                var selectedScrewType = getOption.GetSelectedEnumValue<ScrewType>();
                if (oldScrew.ScrewType != selectedScrewType)
                {
                    //preload old screw type's screw aides because they are needed during the undoing of this command and undo command could not runscript
                    ScrewBrepComponentDatabase.PreLoadScrewAides(oldScrew.ScrewType);

                    var currentLength = Math.Round(oldScrew.TotalLength);
                    bool exceeded;
                    var nearestLength = ScrewCalibrator.AdjustLengthToAvailableScrewLength(selectedScrewType, currentLength, out exceeded);
                    var newHeadPoint = Point3d.Subtract(oldScrew.TipPoint, oldScrew.Direction * nearestLength);
                    var newScrew = new Screw(oldScrew.Director, newHeadPoint, oldScrew.TipPoint, selectedScrewType, oldScrew.Index);
                    newScrew.Set(oldScrew.Id, false, false);

                    result = Result.Success;
                }
            }

            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            Core.Operations.Locking.LockAll(doc);
            Visibility.ScrewsDefault(doc);

            return result;
        }
    }
}