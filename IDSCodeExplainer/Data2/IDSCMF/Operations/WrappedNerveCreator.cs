using IDS.CMF.Constants;
using IDS.CMF.Utilities;
using Rhino.Geometry;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class WrappedNerveCreator : WrappedIbbCreator
    {
        public WrappedNerveCreator(CMFObjectManager objectManager) : base(objectManager)
        {
        }

        public Mesh CreatePlannedWrapNerves()
        {
            return CreateWrapNerves(ProPlanImport.PlannedLayer);
        }

        public Mesh CreateOriginalNerves()
        {
            return CreateWrapNerves(ProPlanImport.OriginalLayer);
        }

        private Mesh CreateWrapNerves(string parentLayer)
        {
            var subLayerNames = ProPlanImportUtilities.GetNerveRelatedComponentSubLayerNames();

            return CreateWrapBuildingBlock(subLayerNames.Select(subLayer => $"{parentLayer}::{subLayer}"));
        }
    }
}
