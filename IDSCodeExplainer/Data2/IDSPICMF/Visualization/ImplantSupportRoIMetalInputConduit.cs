using IDS.CMF.Visualization;
using IDS.PICMF.Forms;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace IDS.PICMF.Visualization
{
    public class ImplantSupportRoIMetalInputConduit : DisplayConduit, IDisposable
    {
        private readonly DisplayMaterial _integratedRemovedMetalMaterial;
        private readonly DisplayMaterial _integratedRemainedMetalMaterial;
        private readonly DisplayMaterial _defaultMetalDisplayMaterial;
        public Mesh MetalMesh { get; }

        public EMetalIntegrationState State { get; set; }

        public ImplantSupportRoIMetalInputConduit(Mesh metalMesh, Color defaultColor)
        {
            MetalMesh = metalMesh;
            State = EMetalIntegrationState.Unselected;

            _integratedRemovedMetalMaterial = ImplantSupportRoIConduit.CreateRemovedMetalMaterial();
            _integratedRemainedMetalMaterial = ImplantSupportRoIConduit.CreateRemainedMetalMaterial();
            _defaultMetalDisplayMaterial = SupportRoIConduit.CreateMaterial(0.05, defaultColor);
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);

            if (MetalMesh != null)
            {
                e.IncludeBoundingBox(MetalMesh.GetBoundingBox(false));
            }
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);

            if (MetalMesh != null)
            {
                var material = _defaultMetalDisplayMaterial;
                switch (State)
                {
                    case EMetalIntegrationState.Remain:
                        material = _integratedRemainedMetalMaterial;
                        break;
                    case EMetalIntegrationState.Remove:
                        material = _integratedRemovedMetalMaterial;
                        break;
                }
                e.Display.DrawMeshShaded(MetalMesh, material);
            }
        }

        public void Dispose()
        {
            _integratedRemovedMetalMaterial.Dispose();
            _integratedRemainedMetalMaterial.Dispose();
            _defaultMetalDisplayMaterial.Dispose();
        }
    }
}
