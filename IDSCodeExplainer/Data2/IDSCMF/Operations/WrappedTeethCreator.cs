using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using Rhino.Geometry;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class WrappedTeethCreator : WrappedIbbCreator
    {
        public WrappedTeethCreator(CMFObjectManager objectManager) : base(objectManager)
        {
        }

        public Mesh CreateOriginalWrapTeeth(string teethLayer)
        {
            return CreateWrapTeeth(ProPlanImport.OriginalLayer, teethLayer);
        }

        public Mesh CreatePlannedWrapTeeth(string teethLayer)
        {
            return CreateWrapTeeth(ProPlanImport.PlannedLayer, teethLayer);
        }

        private Mesh CreateWrapTeeth(string parentLayer, string teethLayer)
        {
            var subLayerNames = ProPlanImportUtilities.GetComponentSubLayerNames(ProPlanImportPartType.Teeth);
            var selectedSubLayer = subLayerNames.FindAll(layer => layer.Contains(teethLayer));

            return CreateWrapBuildingBlock(selectedSubLayer.Select(subLayer => $"{parentLayer}::{subLayer}"));
        }
    }
}
