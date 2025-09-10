using IDS.Core.Drawing;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class QCDocumentScaffoldVisualizerPresets : CameraViewPresets
    {
        private readonly GleniusImplantDirector _director;
        private readonly ColoredMeshConduit _meshConduit;

        public QCDocumentScaffoldVisualizerPresets(GleniusImplantDirector director) : 
            base(director.AnatomyMeasurements, director.Document.Views.ActiveView.ActiveViewport, director.defectIsLeft)
        {
            this._director = director;
            _meshConduit = new ColoredMeshConduit();
            InitializeScaffoldScapulaContactAreaConduit(director);
        }

        private void SetCommonScaffoldViewVisualization()
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, 0.0},
                {IBB.PlateBasePlate, 0.0},
                {IBB.ScaffoldSide, 0.0},
                {IBB.ScaffoldSupport, 0.0},
            };
            _meshConduit.Enabled = false;
            Visibility.SetIBBTransparencies(_director.Document, dict);
        }

        public void SetAnteroLateralForScaffoldView()
        {
            SetCommonScaffoldViewVisualization();
            SetCameraToAnteroLateralView();
        }

        public void SetLateralForScaffoldView()
        {
            SetCommonScaffoldViewVisualization();
            SetCameraToLateralView();
        }

        public void SetPosteroLateralForScaffoldView()
        {
            SetCommonScaffoldViewVisualization();
            SetCameraToPosteroLateralView();
        }

        private void SetCommonScaffoldContactViewVisualization()
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, 0.0},
                {IBB.Screw, 0.0},
                {IBB.ScaffoldSupport, 0.0}
            };            
            _meshConduit.Enabled = true;
            Visibility.SetIBBTransparencies(_director.Document, dict);
        }

        public void SetAnteroLateralForContactView()
        {
            SetCommonScaffoldContactViewVisualization();
            SetCameraToAnteroLateralView();
        }

        public void SetLateralForContactView()
        {
            SetCommonScaffoldContactViewVisualization();
            SetCameraToLateralView();
        }

        public void SetPosteroLateralForContactView()
        {
            SetCommonScaffoldContactViewVisualization();
            SetCameraToPosteroLateralView();
        }

        public void SetForHeadReamingView()
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, 0.0},
                {IBB.RBVHead, 0.0}
            };
            _meshConduit.Enabled = false;
            Visibility.SetIBBTransparencies(_director.Document, dict);
            SetCameraToLateralView();
        }

        public void SetForImplantReamingView()
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, 0.0},
                {IBB.RbvScaffold, 0.0}
            };
            _meshConduit.Enabled = false;
            Visibility.SetIBBTransparencies(_director.Document, dict);
            SetCameraToLateralView();
        }

        public void SetForReamedScapulaView()
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, 0.0},
            };
            _meshConduit.Enabled = false;
            Visibility.SetIBBTransparencies(_director.Document, dict);
            SetCameraToLateralView();
        }

        public void SetConduitIsVisible(bool isVisible)
        {
            _meshConduit.Enabled = isVisible;
        }

        private void InitializeScaffoldScapulaContactAreaConduit(GleniusImplantDirector director)
        {
            var implantContactCreator = new QCImplantContactCreator(director);
            var contactArea = implantContactCreator.CreateScaffoldSupportContact();

            if (contactArea != null)
            {
                _meshConduit.AddMesh(contactArea, Colors.Scaffold);
                _meshConduit.Enabled = false;
            }
        }
    }
}
