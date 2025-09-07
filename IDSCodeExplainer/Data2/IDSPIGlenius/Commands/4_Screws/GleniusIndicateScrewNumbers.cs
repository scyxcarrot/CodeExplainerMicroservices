using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Drawing;
using System.Linq;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("B39090FC-5CC1-43B5-88D2-F129694E6E7B")]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.Screw)]
    public class GleniusIndicateScrewNumbers : CommandBase<GleniusImplantDirector>
    {
        public GleniusIndicateScrewNumbers()
        {
            Instance = this;
        }
        public static GleniusIndicateScrewNumbers Instance { get; private set; }

        public override string EnglishName => "GleniusIndicateScrewNumbers";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            Locking.UnlockScrews(doc);

            var isIndexGloballyVisible = GlobalScrewIndexVisualizer.IsGloballyVisible;
            GlobalScrewIndexVisualizer.SetVisible(false);

            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Click the screws one by one to assign screw numbers.");
            selectScrew.DisablePreSelect();
            selectScrew.AcceptNothing(true);
            selectScrew.EnableHighlight(false);
            selectScrew.EnableTransparentCommands(false);
            
            var screwManager = new ScrewManager(director.Document);
            var screws = screwManager.GetAllScrews().ToList();
            foreach (var screw in screws)
            {
                screw.Index = -1;
            }

            var screwIndexVisualizer = new ScrewIndexVisualizer(director, Color.DarkBlue);
            screwIndexVisualizer.DisplayConduit(true);

            var newIndex = 1;
            while (newIndex <= screws.Count)
            {
                var result = selectScrew.Get();
                switch (result)
                {
                    case GetResult.Cancel:
                        // Reset
                        foreach (var screw in screws)
                        {
                            screw.Index = -1;
                        }
                        newIndex = 1;
                        break;
                    case GetResult.Object:
                        var selectedScrew = (Screw)selectScrew.Object(0).Object();
                        if (selectedScrew.Index == -1)
                        {
                            selectedScrew.Index = newIndex;
                            newIndex++;
                        }
                        break;
                }
            }

            screwIndexVisualizer.DisplayConduit(false);

            GlobalScrewIndexVisualizer.Initialize(director);
            GlobalScrewIndexVisualizer.SetVisible(isIndexGloballyVisible);

            return Result.Success;
        }
    }
}