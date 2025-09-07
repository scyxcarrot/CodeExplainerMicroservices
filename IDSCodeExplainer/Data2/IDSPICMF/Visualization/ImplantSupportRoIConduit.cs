using IDS.CMF.Visualization;
using IDS.PICMF.Forms;
using Rhino.Display;

namespace IDS.PICMF.Visualization
{
    public class ImplantSupportRoIConduit : SupportRoIConduit
    {
        private ImplantSupportRoICreationDataModel RoIDataModel => dataModel as ImplantSupportRoICreationDataModel;

        private readonly DisplayMaterial _integratedRemovedMetalMaterial;
        private readonly DisplayMaterial _integratedRemainedMetalMaterial;

        public ImplantSupportRoIConduit(ImplantSupportRoICreationDataModel dataModel) : base(dataModel)
        {
            _integratedRemovedMetalMaterial = CreateRemovedMetalMaterial();
            _integratedRemainedMetalMaterial = CreateRemainedMetalMaterial();
        }
        
        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            
            IncludeNotAccurateBoundingBox(e, RoIDataModel.Metal.IntegratedRemovedMetal);
            IncludeNotAccurateBoundingBox(e, RoIDataModel.Metal.IntegratedRemainedMetal);
        }

        public static DisplayMaterial CreateRemovedMetalMaterial()
        {
            return CreateMaterial(0.05, Colors.ImplantSupportRoIRemovedMetal);
        }

        public static DisplayMaterial CreateRemainedMetalMaterial()
        {
            return CreateMaterial(0.05, Colors.ImplantSupportRoIRemainedMetal);
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);

            DrawMesh(e, RoIDataModel.Metal.IntegratedRemovedMetal, _integratedRemovedMetalMaterial);
            DrawMesh(e, RoIDataModel.Metal.IntegratedRemainedMetal, _integratedRemainedMetalMaterial);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _integratedRemovedMetalMaterial.Dispose();
                _integratedRemainedMetalMaterial.Dispose();
            }
        }
    }
}
