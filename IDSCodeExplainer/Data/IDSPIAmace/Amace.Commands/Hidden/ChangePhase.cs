#if DEBUG

using IDS.Amace.Enumerators;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.GUI;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Amace.Commands
{
    [System.Runtime.InteropServices.Guid("4F490B5D-1363-4F11-B61D-0BBD7EE50A8F")]
    [IDSCommandAttributes(true, DesignPhase.Any)]
    public class ChangePhase : CommandBase<ImplantDirector>
    {
        public ChangePhase()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static ChangePhase TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "ChangePhase";

        /**
         * Aggregate all information needed by the HTML quality report template
         * and export it as HTML.
         *
         * For screenshots, set the view and let user confirm if interactive
         * option is chosen.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Ask for password
            var passwordDialog = new frmPassword();
            passwordDialog.ShowDialog();
            if (passwordDialog.DialogResult == DialogResult.Cancel)
            {
                return Result.Failure;
            }

            // Ask which phase
            var go = new GetOption();
            go.SetCommandPrompt("Change design phase");
            go.AcceptNothing(true);
            var designPhases = Enum.GetNames(typeof(DesignPhase)).ToList();
            go.AddOptionList("Target", designPhases,
                designPhases.IndexOf(director.CurrentDesignPhase.ToString()));
            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Option)
                {
                    var index = go.Option().CurrentListOptionIndex;
                    director.EnterDesignPhase((DesignPhase)Enum.Parse(typeof(DesignPhase), designPhases[index]));
                }
                if (res == GetResult.Nothing)
                {
                    break;
                }
                if (res == GetResult.Cancel)
                {
                    return Result.Failure;
                }
            }

            var rc = go.CommandResult();
            if (rc != Result.Success)
            {
                return rc;
            }

            Visibility.SetVisibilityByPhase(director);
            return Result.Success;
        }
    }
}

#endif