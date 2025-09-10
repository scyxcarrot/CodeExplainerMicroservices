using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Relations;
using System.Linq;

namespace IDS.Glenius.CommandHelpers
{
    public class ReconstructionHelper
    {
        private readonly GleniusImplantDirector director;
        private readonly AnatomyMeasurementsChanger changer;

        public ReconstructionHelper(GleniusImplantDirector director)
        {
            this.director = director;
            changer = new AnatomyMeasurementsChanger(director, "ReconstructionHelper");
        }

        public void DeleteExecuteReconstructionRelatedObjects()
        {
            var objManager = new GleniusObjectManager(director);
            var ids = objManager.GetAllBuildingBlockIds(IBB.ScapulaDefectRegionRemoved).ToList();
            ids = ids.Concat(objManager.GetAllBuildingBlockIds(IBB.ReconstructedScapulaBone)).ToList();

            foreach (var id in ids)
            {
                objManager.DeleteObject(id);
            }

            changer.SubscribeUndoRedoEvent(director.Document);
            director.DefaultAnatomyMeasurements = null;
            director.AnatomyMeasurements = null;
        }
    }
}
