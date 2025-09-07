using IDS.Common;
using IDS.Core.PluginHelper;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Drawing;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("A47F2812-E29F-4B08-A6F7-F8AC36C723AC")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.Screw)]
    public class GleniusToggleCylindricalOffset : Command
    {
        public GleniusToggleCylindricalOffset()
        {
            Instance = this;
        }
        
        public static GleniusToggleCylindricalOffset Instance { get; private set; }

        public override string EnglishName => "GleniusToggleCylindricalOffset";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);
            if (director == null)
            {
                return Result.Failure;
            }

            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            var getOptions = new GetOption();

            var offsetOption = new OptionDouble(1.0, 1.0, 2.5);
            getOptions.AddOptionDouble("Offset", ref offsetOption);

            var transparencyOption = new OptionDouble(0.5, 0.0, 1.0);
            getOptions.AddOptionDouble("Transparency", ref transparencyOption);

            var colorOption = new OptionColor(Color.FromArgb(191, 64, 255));
            getOptions.AddOptionColor("Color", ref colorOption);

            getOptions.SetCommandPrompt("Change the parameter values and press enter.");
            getOptions.AcceptNothing(true);
            getOptions.EnableTransparentCommands(false);

            while (true)
            {
                var result = getOptions.Get();
                if (result == GetResult.Nothing || result == GetResult.Cancel || result == GetResult.NoResult)
                {
                    break;
                }
            }

            var offset = offsetOption.CurrentValue;
            var transparency = transparencyOption.CurrentValue;
            var color = colorOption.CurrentValue;

            var visualizer = CylindricalOffsetVisualizer.Get();
            visualizer.ToggleVisualization(director, offset, transparency, color);

            return Result.Success;
        }
    }
}
