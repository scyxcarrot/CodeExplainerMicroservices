using IDS.Amace.Enumerators;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.DataTypes;
using IDS.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using Proxies = IDS.Amace.Proxies;

namespace IDS.Commands.Screws
{
    [System.Runtime.InteropServices.Guid("54AAC489-F04D-4D65-BBB4-931A5E9ABF07")]
    [IDSCommandAttributes(true, DesignPhase.Screws | DesignPhase.ImplantQC)]
    public class ScrewInfo : CommandBase<ImplantDirector>
    {
        public ScrewInfo()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        // The one and only instance of this command
        public static ScrewInfo TheCommand { get; private set; }

        // The command name as it appears on the Rhino command line
        public override string EnglishName => "ScrewInfo";
        
        public static ScrewConduit Numbers
        {
            get { return Proxies.ScrewInfo.Numbers; }
            set { Proxies.ScrewInfo.Numbers = value; }
        }

        private static DesignPhase DesignPhase
        {
            get { return Proxies.ScrewInfo.DesignPhase; }
            set { Proxies.ScrewInfo.DesignPhase = value; }
        }

        private static ScrewConduitMode _showQualityChecks = ScrewConduitMode.NoWarnings;

        /// <summary>
        /// The commands shows the contralateral HJC/COR as a sphere and displays the lateralization using measurement arrows and
        /// a numerical label on-screen.The user can show and hide the visualizations.
        /// </summary>
        /// <param name="doc">The current document.</param>
        /// <param name="mode">The command running mode.</param>
        /// <param name="director"></param>
        /// <returns>
        /// The command result code.
        /// </returns>
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Get option if the warnings should be drawn
            var getOption = new GetOption();
            getOption.SetCommandPrompt("Show Numbers only or Numbers and Screw QC warnings?");
            getOption.AddOption("NumbersOnly");
            var modeWarnings = getOption.AddOption("Warnings");
            getOption.Get();
            if (getOption.CommandResult() != Result.Success)
            {
                return getOption.CommandResult();
            }

            var option = getOption.Option();
            if (null == option)
            {
                return Result.Failure;
            }

            var modeSelected = option.Index; // Index of the chosen method
            var screwConduitMode = modeSelected == modeWarnings ? ScrewConduitMode.WarningTextAndColor : ScrewConduitMode.NoWarnings;

            // Check if we expect modified behaviour
            var modified = (DesignPhase != director.CurrentDesignPhase || _showQualityChecks != screwConduitMode);
            DesignPhase = director.CurrentDesignPhase;
            _showQualityChecks = screwConduitMode;

            // If screw conduit does not exist yet, create it, and enable
            if (Numbers == null)
            {
                Numbers = new ScrewConduit(director, screwConduitMode);
                Enable(doc, true);
                return Result.Success;
            }

            // if screw conduit is enabled and unmodified, disable it
            if (Numbers.Enabled && !modified)
            {
                Disable(doc, true);
                return Result.Success;
            }

            // If screw conduit is modified, recreate it and enable it
            if (modified)
            {
                Numbers.UpdateConduit(screwConduitMode);
                _showQualityChecks = screwConduitMode;
                DesignPhase = director.CurrentDesignPhase;
                Enable(doc, true);
                return Result.Success;
            }

            // if screw conduit is unmodified and disabled, enable it
            if (Numbers.Enabled)
            {
                return Result.Success;
            }

            Enable(doc, true);
            return Result.Success;
        }

        public static void Disable(RhinoDoc doc, bool setVis = false)
        {
            Proxies.ScrewInfo.Disable(doc, setVis);
        }

        public static void Enable(RhinoDoc doc, bool setVis = false)
        {
            if (null == Numbers)
            {
                return;
            }

            Numbers.Enabled = true;
            if (setVis)
            {
                Visibility.ScrewNumbers(doc);
            }
                
        }

        public static void Update(RhinoDoc doc, bool setVis = false)
        {
            Proxies.ScrewInfo.Update(doc, setVis);
        }
    }
}