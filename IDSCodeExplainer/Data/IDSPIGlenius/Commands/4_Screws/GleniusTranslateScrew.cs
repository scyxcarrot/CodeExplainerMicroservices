using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.PluginHelper;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Linq;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("0d4bace9-c229-48c6-8558-2c3eb07abce6"),
    CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.ScapulaDesignReamed, IBB.Head, IBB.Screw)]

    public class GleniusTranslateScrew : CommandBase<GleniusImplantDirector>
    {
        static GleniusTranslateScrew _instance;
        public GleniusTranslateScrew()
        {
            _instance = this;
            VisualizationComponent = new AddScrewVisualization()
            {
                EnableOnCommandSuccessVisualization = false
            };
        }

        ///<summary>The only instance of the GleniusTranslateScrew command.</summary>
        public static GleniusTranslateScrew Instance => _instance;

        public override string EnglishName => "GleniusTranslateScrew";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            if (!IDSPluginHelper.CheckIfCommandIsAllowed(this))
            {
                return Result.Failure;
            }

            Core.Operations.Locking.LockAll(doc);
            Locking.UnlockScrews(doc);

            var getObject = new GetObject();
            getObject.SetCommandPrompt("Select Screw to translate");
            getObject.EnablePreSelect(false, false);
            getObject.EnablePostSelect(true);
            getObject.AcceptNothing(true);
            getObject.EnableTransparentCommands(false);

            while (true)
            {
                var res = getObject.Get();

                if (res == GetResult.Object)
                {
                    var screw = doc.Objects.GetSelectedObjects(false, false).FirstOrDefault() as Screw;

                    var screwTranslator = new TranslateScrew();

                    if (!screwTranslator.DoTranslate(screw))
                    {
                        return Result.Failure;
                    }
                    GlobalScrewIndexVisualizer.Initialize(director);
                }
                else if (res == GetResult.Nothing)
                {
                    return Result.Success;
                }
                else
                {
                    return Result.Failure;
                }
            }
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
