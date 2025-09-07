using IDS.CMF.Visualization;
using IDS.PICMF.Forms;
using Rhino.Display;
using System.Drawing;

namespace IDS.PICMF.Visualization
{
    public class GuideSupportRoIConduit : SupportRoIConduit
    {
        private GuideSupportRoICreationDataModel RoIDataModel => dataModel as GuideSupportRoICreationDataModel;

        private readonly DisplayMaterial _drawnRoIMaterial;
        private readonly DisplayMaterial _integratedMetalMaterial;


        public GuideSupportRoIConduit(GuideSupportRoICreationDataModel dataModel) : base(dataModel)
        {
            _drawnRoIMaterial = CreateRoIMaterial();
            _integratedMetalMaterial = CreateMetalMaterial();
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);

            IncludeNotAccurateBoundingBox(e, RoIDataModel.RoI.DrawnRoI);
            IncludeNotAccurateBoundingBox(e, RoIDataModel.Metal.IntegratedMetal);
        }

        public static DisplayMaterial CreateRoIMaterial()
        {
            return CreateMaterial(0.1, Color.Red);
        }

        public static DisplayMaterial CreateMetalMaterial()
        {
            return CreateMaterial(0.05, Colors.PlateTemporary);
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);

            DrawMesh(e, RoIDataModel.RoI.DrawnRoI, _drawnRoIMaterial);
            DrawMesh(e, RoIDataModel.Metal.IntegratedMetal, _integratedMetalMaterial);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _drawnRoIMaterial.Dispose();
                _integratedMetalMaterial.Dispose();
            }
        }
    }
}
