using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("30CA288E-30E4-4BDB-87AF-356CF933DA01")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.Screw)]
    public class GleniusDeleteScrews : CommandBase<GleniusImplantDirector>
    {
        public GleniusDeleteScrews()
        {
            m_thecommand = this;
            VisualizationComponent = new AddScrewVisualization();
        }

        public static GleniusDeleteScrews TheCommand => m_thecommand;

        private static GleniusDeleteScrews m_thecommand;

        public override string EnglishName => "GleniusDeleteScrews";

        private GleniusImplantDirector _dir;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            _dir = director;

            // Unlock screws
            Locking.UnlockScrews(director.Document);
            // Get screw
            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select screws to remove.");
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableTransparentCommands(false);

            // Get user input
            while (true)
            {
                var res = selectScrew.GetMultiple(0, 0);

                if (res == GetResult.Cancel || res == GetResult.Nothing)
                {
                    return Result.Failure;
                }

                if (res == GetResult.Object)
                {
                    // Ask confirmation and delete if user clicks 'Yes'
                    var result = Rhino.UI.Dialogs.ShowMessageBox(
                        "Are you sure you want to delete the selected screw(s)?",
                        "Delete Screw(s)?",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Exclamation);
                    if (result == DialogResult.Yes)
                    {
                        // Get selected objects
                        var selectedScrews = doc.Objects.GetSelectedObjects(false, false).ToList();
                        // Delete one by one (including dependencies)
                        foreach (RhinoObject rhobj in selectedScrews)
                        {
                            var screw = (Screw)rhobj;

                            //preload screw type's screw aides because they are needed during the undoing of this command and undo command could not runscript
                            ScrewBrepComponentDatabase.PreLoadScrewAides(screw.ScrewType);

                            screw.Delete();
                        }

                        // Stop user input
                        break;
                    }
                    if (result == DialogResult.Cancel)
                    {
                        return Result.Failure;
                    }
                }

                return Result.Success;
            }

            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, GleniusImplantDirector director)
        {
            GlobalScrewIndexVisualizer.Initialize(director);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, GleniusImplantDirector director)
        {
            GlobalScrewIndexVisualizer.Initialize(director);
        }
    }
}