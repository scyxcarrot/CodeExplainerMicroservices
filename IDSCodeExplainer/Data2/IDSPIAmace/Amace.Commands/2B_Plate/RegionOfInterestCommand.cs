using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;

namespace IDS.Amace.Commands
{
    public abstract class RegionOfInterestCommand : CommandBase<ImplantDirector>
    {
        public override void OnCommandCannotExecute(RhinoDoc doc, ImplantDirector director)
        {
            if (director == null)
            {
                return;
            }
            var objManager = new AmaceObjectManager(director);
            if (!objManager.HasBuildingBlock(IBB.TransitionPreview))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Please click Plate Preview to proceed");
            }
        }
    }
}