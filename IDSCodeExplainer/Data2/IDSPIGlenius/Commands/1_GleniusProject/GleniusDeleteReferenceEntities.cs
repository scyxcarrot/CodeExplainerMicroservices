using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Operations;
using IDS.Glenius.Enumerators;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("7AC7A79F-4FB8-4211-B5BA-4CDACBDCBD8D")]
    [IDSGleniusCommand(~DesignPhase.Draft)]
    public class GleniusDeleteReferenceEntities : CommandBase<GleniusImplantDirector>
    {
        public GleniusDeleteReferenceEntities()
        {
            TheCommand = this;
            VisualizationComponent = new DeleteReferenceEntitiesVisualization();
        }

        public static GleniusDeleteReferenceEntities TheCommand { get; private set; }

        public override string EnglishName => "GleniusDeleteReferenceEntities";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            Operations.Locking.UnlockReferenceEntities(doc);
            
            var success = EntitiesDeleter.DeleteEntities("Select reference entities to remove.", "Are you sure you want to delete the selected reference entit(y/ies)?", "Delete Entit(y/ies)?", director);

            Locking.LockAll(doc);

            return success ? Result.Success : Result.Failure;
        }
    }
}