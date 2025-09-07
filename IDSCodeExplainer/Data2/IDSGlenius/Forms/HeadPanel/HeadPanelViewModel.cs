using IDS.Common;
using IDS.Core.Drawing;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Query;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.DocObjects.Tables;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace IDS.Glenius.Forms
{
    [System.Runtime.InteropServices.Guid("0F72833A-75DB-4275-8DE9-5D5A13153663")]
    [IDSGleniusCommandAttribute(DesignPhase.Head, IBB.Scapula, IBB.Head)]
    public class HeadPanelViewModel : INotifyPropertyChanged, IDisposable
    {
        private CameraViewPresets cameraPresets;
        private HeadAlignment headAlignment;
        private HeadMeasurementsVisualizer visualizer;
        private FullSphereConduit fullSphereConduit;
        private HeadAnalysisHelper headAnalysisHelper;
        private readonly Dictionary<int, IBB> layerVisibilityList;

        public GleniusImplantDirector Director { get; set; }

        private AnatomicalMeasurements _anatomicalMeasurements;
        public AnatomicalMeasurements AnatomicalMeasurements
        {
            get
            {
                return _anatomicalMeasurements;
            }
            set
            {
                _anatomicalMeasurements = value;
                if (_anatomicalMeasurements != null)
                { 
                    var objManager = new GleniusObjectManager(Director);
                    cameraPresets = new CameraViewPresets(_anatomicalMeasurements, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport, Director.defectIsLeft);
                    Model.GlenoidInclination = _anatomicalMeasurements.GlenoidInclinationValue; //Setting this will define Head Inclination Default
                    Model.GlenoidVersion = _anatomicalMeasurements.GlenoidVersionValue;
                    headAlignment = new HeadAlignment(_anatomicalMeasurements, objManager, RhinoDoc.ActiveDoc, Director.defectIsLeft);
                    visualizer = new HeadMeasurementsVisualizer(_anatomicalMeasurements, headAlignment);
                    fullSphereConduit = new FullSphereConduit(_anatomicalMeasurements.PlGlenoid.Origin, Model.SelectedHeadType.Diameter, 1 - Model.FullSphereOpacity);
                    headAnalysisHelper = new HeadAnalysisHelper(objManager);
                    UpdateHeadAlignmentValues();
                    headAlignment.ValueChanged += UpdateHeadAlignmentValues;
                }
                else
                {
                    if (headAlignment != null)
                    {
                        headAlignment.ValueChanged -= UpdateHeadAlignmentValues;
                    }
                    cameraPresets = null;
                    headAlignment = null;
                    visualizer = null;
                    fullSphereConduit = null;
                    headAnalysisHelper = null;
                    Model.ResetValues();
                }
            }
        }

        private HeadPanelModel _model;
        public HeadPanelModel Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
                OnPropertyChanged("Model");
            }
        }

        public HeadPanelViewModel()
        {
            _model = new HeadPanelModel();
            layerVisibilityList = new Dictionary<int, IBB>();
            _model.PropertyChanged += ModelPropertyChanged;
        }

        public bool SetCameraToSuperiorView()
        {
            if(AnatomicalMeasurements != null)
            {
                cameraPresets.SetCameraToSuperiorView();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetCameraToAnteriorView()
        {
            if (AnatomicalMeasurements != null)
            {
                cameraPresets.SetCameraToAnteriorView();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetCameraToLateralView()
        {
            if (AnatomicalMeasurements != null)
            {
                cameraPresets.SetCameraToLateralView();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetCameraToPosteriorView()
        {
            if (AnatomicalMeasurements != null)
            {
                cameraPresets.SetCameraToPosteriorView();
                return true;
            }
            else
            {
                return false;
            }
        }
        
        #region Position

        public bool SetSuperior()
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.IncrementDecrementInferiorSuperiorPosition(1);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetInferior()
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.IncrementDecrementInferiorSuperiorPosition(-1);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetSuperiorInferior(double value)
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.SetInferiorSuperiorPosition(value);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetPosterior()
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.IncrementDecrementAnteriorPosteriorPosition(1);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetAnterior()
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.IncrementDecrementAnteriorPosteriorPosition(-1);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetAnteriorPosterior(double value)
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.SetAnteriorPosteriorPosition(value);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetLateral()
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.IncrementDecrementMedialLateralPosition(1);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetMedial()
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.IncrementDecrementMedialLateralPosition(-1);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetMedialLateral(double value)
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.SetMedialLateralPosition(value);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Orientation

        public bool SetAnteversion()
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.IncrementDecrementVersionAngle(1);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetRetroversion()
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.IncrementDecrementVersionAngle(-1);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetAnteRetroVersion(double value)
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.SetVersionAngle(value);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetSuperiorInclination()
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.IncrementDecrementInclinationAngle(1);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetInferiorInclination()
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.IncrementDecrementInclinationAngle(-1);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetInferiorSuperiorInclination(double value)
        {
            if (AnatomicalMeasurements != null)
            {
                headAlignment.SetInclinationAngle(value);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Measurements

        public bool ShowHideHeadComponentMeasurements(bool show)
        {
            if (AnatomicalMeasurements != null)
            {
                visualizer.ShowHideComponentMeasurements(show);
                RhinoDoc.ActiveDoc.Views.Redraw();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ShowHideGlenoidVector(bool show)
        {
            if (AnatomicalMeasurements != null)
            {
                visualizer.ShowHideGlenoidVector(show);
                RhinoDoc.ActiveDoc.Views.Redraw();
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        public void UpdateHeadType()
        {
            if(Director != null && Director.AnatomyMeasurements != null)
            {
                HeadMaker maker = new HeadMaker(Director);
                if(maker.ChangeHead(_model.SelectedHeadType.Type))
                {
                    UpdateHeadAlignmentValues();
                    UpdateAllVisualizations();
                }
            }
        }

        private void UpdateHeadAlignmentValues()
        {
            var objManager = new GleniusObjectManager(Director);
            var head = objManager.GetBuildingBlock(IBB.Head) as Head;
            var headType = Model.HeadTypes.FirstOrDefault(headTypes => headTypes.Type == head.HeadType);
            Model.SelectedHeadType = headType;

            Model.HeadVersion = headAlignment.GetVersionAngle();
            Model.HeadInclination = headAlignment.GetInclinationAngle();
            Model.AnteriorPosteriorAxis = headAlignment.GetAnteriorPosteriorPosition();
            Model.InferiorSuperiorAxis = headAlignment.GetInferiorSuperiorPosition();
            Model.MedialLateralAxis = headAlignment.GetMedialLateralPosition();

            var headCoordSystem = headAlignment.GetHeadCoordinateSystem();
            fullSphereConduit.Center = headCoordSystem.Origin;
            PerformVicinityCheck();
        }

        private void ModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedHeadType" && AnatomicalMeasurements != null)
            {
                fullSphereConduit.Diameter = Model.SelectedHeadType.Diameter;
                RhinoDoc.ActiveDoc.Views.Redraw();
            }
        }

        private bool SetBlockVisualization(IBB block, bool isVisible, double opacity)
        {
            bool set;

            if (isVisible)
            {
                set = Core.Visualization.Visibility.SetVisible(RhinoDoc.ActiveDoc, BuildingBlocks.Blocks[block].Layer);
                ImplantBuildingBlockProperties.SetTransparency(BuildingBlocks.Blocks[block], RhinoDoc.ActiveDoc, 1 - opacity);
            }
            else
            {
                set = Core.Visualization.Visibility.SetHidden(RhinoDoc.ActiveDoc, BuildingBlocks.Blocks[block].Layer);
            }

            RhinoDoc.ActiveDoc.Views.Redraw();
            return set;
        }

        public void UpdateAllVisualizations()
        {
            RhinoDoc.LayerTableEvent -= RhinoDocLayerTableEvent;
            SetupLayerVisibilityList();

            UpdateHeadComponentVisualization();
            UpdateTaperMantleSafetyVisualization();
            UpdateProductionRodVsualization();
            UpdateScapulaReamedVisualization();
            UpdateRecostructedScapulaVisualization();
            UpdateFullSphereVisualization();
            UpdateCylinderVisualization();

            RhinoDoc.LayerTableEvent += RhinoDocLayerTableEvent;
        }

        public bool UpdateHeadComponentVisualization()
        {
            return SetBlockVisualization(IBB.Head, Model.IsHeadComponentVisible, Model.HeadComponentOpacity);
        }

        public bool UpdateTaperMantleSafetyVisualization()
        {
            return SetBlockVisualization(IBB.TaperMantleSafetyZone, Model.IsTaperMantleSafetyZoneVisible, Model.TaperMantleSafetyZoneOpacity);
        }

        public bool UpdateProductionRodVsualization()
        {
            return SetBlockVisualization(IBB.ProductionRod, Model.IsProductionRodVisible, Model.ProductionRodOpacity);
        }

        public bool UpdateScapulaReamedVisualization()
        {
            return SetBlockVisualization(IBB.ScapulaReamed, Model.IsScapulaReamedVisible, Model.ScapulaReamedOpacity);
        }

        public bool UpdateRecostructedScapulaVisualization()
        {
            return SetBlockVisualization(IBB.ReconstructedScapulaBone, Model.IsReconsScapulaVisible, Model.ReconsScapulaOpacity);
        }

        public bool UpdateCylinderVisualization()
        {
            return SetBlockVisualization(IBB.CylinderHat, Model.IsCylinderVisible, Model.CylinderOpacity);
        }

        public void UpdateFullSphereVisualization()
        {
            if (AnatomicalMeasurements != null)
            {
                if (Model.IsFullSphereVisible)
                {
                    fullSphereConduit.Transparency = 1 - Model.FullSphereOpacity;
                    fullSphereConduit.Enabled = true;
                }
                else
                {
                    fullSphereConduit.Enabled = false;
                }

                RhinoDoc.ActiveDoc.Views.Redraw();
            }
        }

        private void SetupLayerVisibilityList()
        {
            layerVisibilityList.Clear();

            var blocks = new List<IBB>
            {
                IBB.Head,
                IBB.TaperMantleSafetyZone,
                IBB.ProductionRod,
                IBB.ReconstructedScapulaBone,
                IBB.CylinderHat,
                IBB.ScapulaReamed
            };

            var objectManager = new GleniusObjectManager(Director);
            foreach (var ibb in blocks)
            {
                var block = objectManager.GetBuildingBlock(ibb);
                if (block != null)
                {
                    var attr = block.Attributes;
                    layerVisibilityList.Add(attr.LayerIndex, ibb);
                }
            }
        }

        private void RhinoDocLayerTableEvent(object sender, LayerTableEventArgs e)
        {
            if (e.EventType == LayerTableEventType.Modified && layerVisibilityList.ContainsKey(e.LayerIndex))
            {
                switch (layerVisibilityList[e.LayerIndex])
                {
                    case IBB.Head:
                        Model.IsHeadComponentVisible = e.NewState.IsVisible;
                        break;
                    case IBB.TaperMantleSafetyZone:
                        Model.IsTaperMantleSafetyZoneVisible = e.NewState.IsVisible;
                        break;
                    case IBB.ProductionRod:
                        Model.IsProductionRodVisible = e.NewState.IsVisible;
                        break;
                    case IBB.ReconstructedScapulaBone:
                        Model.IsReconsScapulaVisible = e.NewState.IsVisible;
                        break;
                    case IBB.CylinderHat:
                        Model.IsCylinderVisible = e.NewState.IsVisible;
                        break;
                    case IBB.ScapulaReamed:
                        Model.IsScapulaReamedVisible = e.NewState.IsVisible;
                        break;
                    default:
                        break;
                }
            }
        }

        public void UpdateBone()
        {
            if (Director != null && headAnalysisHelper != null)
            {
                headAnalysisHelper.UpdateBoneMesh();
                PerformVicinityCheck();
            }
        }
        private void PerformVicinityCheck()
        {
            headAnalysisHelper.PerformVicinityCheck();
            Model.IsBoneHeadVicinityOK = headAnalysisHelper.IsBoneHeadVicinityOK;
            Model.IsBoneTaperVicinityOK = headAnalysisHelper.IsBoneTaperVicinityOK;
        }

        #region INotifyPropertyChanged Members 

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                fullSphereConduit.Dispose();
            }
        }
    }
}
