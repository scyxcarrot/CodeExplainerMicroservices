using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Common;
using IDS.Common.Visualisation;
using IDS.Core.CommandBase;
using Rhino;
using Rhino.Commands;

namespace IDS.Amace.Commands
{
    [System.Runtime.InteropServices.Guid("b6cbeb75-806d-4c77-8061-f368e47f0d57")]
    [IDSCommandAttributes(true, DesignPhase.Plate, IBB.PlateContourTop, IBB.PlateContourBottom)]
    public class TogglePlateAnglesVisualisation : CommandBase<ImplantDirector>
    {
        private static PlateConduit Conduit => Proxies.TogglePlateAnglesVisualisation.Conduit;

        public TogglePlateAnglesVisualisation()
        {
            Instance = this;
        }

        ///<summary>The only instance of the TogglePlateEdgeVisualisation command.</summary>
        public static TogglePlateAnglesVisualisation Instance { get; private set; }

        public override string EnglishName => "TogglePlateAnglesVisualisation";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            if (Conduit == null || !Conduit.Enabled)
            {
                Proxies.TogglePlateAnglesVisualisation.Enable(director);
            }
            else if (Conduit.Enabled)
            {
                Proxies.TogglePlateAnglesVisualisation.Disable(director);
            }

            return Result.Success;
        }
    }
}
