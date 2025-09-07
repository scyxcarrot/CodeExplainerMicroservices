using IDS.Core.Drawing;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Query;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace IDS.Glenius.Forms
{
    [System.Runtime.InteropServices.Guid("8947575E-DC55-4845-91A2-B36E7E7D7632")]
    public class HeadPanelModel : INotifyPropertyChanged
    {

        public HeadPanelModel()
        {
            HeadTypes.Add(new HeadTypeHelper(HeadType.TYPE_36_MM, "36mm", HeadQueries.GetHeadDiameter(HeadType.TYPE_36_MM)));
            HeadTypes.Add(new HeadTypeHelper(HeadType.TYPE_38_MM, "38mm", HeadQueries.GetHeadDiameter(HeadType.TYPE_38_MM)));
            HeadTypes.Add(new HeadTypeHelper(HeadType.TYPE_42_MM, "42mm", HeadQueries.GetHeadDiameter(HeadType.TYPE_42_MM)));

            ResetValues();

            IsHeadComponentMeasurementsVisible = false;
            IsGlenoidVectorVisible = false;

            ResetTransparencies();

            IsBoneHeadVicinityOK = false;
            IsBoneTaperVicinityOK = false;
        }

        public void ResetValues()
        {
            SelectedHeadType = HeadTypes.First();

            InferiorSuperiorAxis = 0;
            AnteriorPosteriorAxis = 0;
            MedialLateralAxis = 0;

            HeadVersionDefault = 0;
            HeadVersion = 0;
            GlenoidVersion = 0;

            HeadInclinationDefault = 0;
            HeadInclination = 0;
            GlenoidInclination = 0;
        }

        #region Dimensions

        private ObservableCollection<HeadTypeHelper> _headTypes = new ObservableCollection<HeadTypeHelper>();
        public ObservableCollection<HeadTypeHelper> HeadTypes
        {
            get
            {
                return _headTypes;
            }
            set
            {
                _headTypes = value;
                OnPropertyChanged("HeadTypes");
            }
        }

        private HeadTypeHelper _selectedHeadType;
        public HeadTypeHelper SelectedHeadType
        {
            get
            {
                return _selectedHeadType;
            }
            set
            {
                _selectedHeadType = value;
                OnPropertyChanged("SelectedHeadType");
            }
        }

        #endregion

        #region Position

        private double _inferiorSuperiorAxis;
        public double InferiorSuperiorAxis
        {
            get
            {
                return _inferiorSuperiorAxis;
            }
            set
            {
                _inferiorSuperiorAxis = value;
                OnPropertyChanged("InferiorSuperiorAxis");
            }
        }

        private double _anteriorPosteriorAxis;
        public double AnteriorPosteriorAxis
        {
            get
            {
                return _anteriorPosteriorAxis;
            }
            set
            {
                _anteriorPosteriorAxis = value;
                OnPropertyChanged("AnteriorPosteriorAxis");
            }
        }

        private double _medialLateralAxis;
        public double MedialLateralAxis
        {
            get
            {
                return _medialLateralAxis;
            }
            set
            {
                _medialLateralAxis = value;
                OnPropertyChanged("MedialLateralAxis");
            }
        }

        #endregion

        #region Orientation

        //HEAD VERSION
        private double _headVersionDefault;
        public double HeadVersionDefault
        {
            get
            {
                return _headVersionDefault;
            }
            set
            {
                _headVersionDefault = value;
                CheckIfHeadVersionIsDefault();
                OnPropertyChanged("HeadVersionDefault");
            }
        }

        private void CheckIfHeadVersionIsDefault()
        {
            IsHeadVersionDefault = Math.Abs(_headVersion - _headVersionDefault) < double.Epsilon;
        }

        private bool _isHeadVersionDefault;
        public bool IsHeadVersionDefault
        {
            get
            {
                return _isHeadVersionDefault;
            }
            set
            {
                _isHeadVersionDefault = value;
                OnPropertyChanged("IsHeadVersionDefault");
            }
        }

        private double _headVersion;
        public double HeadVersion
        {
            get
            {
                return _headVersion;
            }
            set
            {
                _headVersion = value;
                CheckIfHeadVersionIsDefault();
                OnPropertyChanged("HeadVersion");
            }
        }

        //GLENOID VERSION
        private double _glenoidVersion;
        public double GlenoidVersion
        {
            get
            {
                return _glenoidVersion;
            }
            set
            {
                _glenoidVersion = value;
                OnPropertyChanged("GlenoidVersion");
            }
        }

        //HEAD INCLINATION
        private double _headInclinationDefault;
        public double HeadInclinationDefault
        {
            get
            {
                return _headInclinationDefault;
            }
            set
            {
                _headInclinationDefault = value;
                CheckIfHeadInclinationIsDefault();
                OnPropertyChanged("HeadInclinationDefault");
            }
        }

        private void CheckIfHeadInclinationIsDefault()
        {
            if (_headInclination >= -10.0 && _headInclination <= 0.0)
            {
                IsHeadInclinationDefault = true;
            }
            else
            {
                IsHeadInclinationDefault = false;
            }
        }

        private bool _isHeadInclinationDefault;
        public bool IsHeadInclinationDefault
        {
            get
            {
                return _isHeadInclinationDefault;
            }
            set
            {
                _isHeadInclinationDefault = value;
                OnPropertyChanged("IsHeadInclinationDefault");
            }
        }

        private double _headInclination;
        public double HeadInclination
        {
            get
            {
                return _headInclination;
            }
            set
            {
                _headInclination = value;
                CheckIfHeadInclinationIsDefault();
                OnPropertyChanged("HeadInclination");
            }
        }

        //GLENOID INCLINATION
        private double _glenoidInclination;
        public double GlenoidInclination
        {
            get
            {
                return _glenoidInclination;
            }
            set
            {
                _glenoidInclination = value;

                //Sets the Head Inclination Default
                HeadInclinationDefault = _glenoidInclination - 10.0;
                if (HeadInclinationDefault < -10)
                {
                    HeadInclinationDefault = -10;
                }
                else if (HeadInclinationDefault > 0)
                {
                    HeadInclinationDefault = 0;
                }

                OnPropertyChanged("GlenoidInclination");
            }
        }
        #endregion

        #region Transparencies
        public void ResetTransparencies()
        {
            IsHeadComponentVisible = true;
            IsScapulaReamedVisible = true;
            IsReconsScapulaVisible = false;
            IsFullSphereVisible = false;
            IsCylinderVisible = false;
            IsTaperMantleSafetyZoneVisible = false;
            IsProductionRodVisible = false;

            HeadComponentOpacity = 1.0;
            ScapulaReamedOpacity = 1.0;
            ReconsScapulaOpacity = 0.5;
            FullSphereOpacity = FullSphereConduit.DefaultTransparency;
            CylinderOpacity = 0.5;
            TaperMantleSafetyZoneOpacity = 0.5;
            ProductionRodOpacity = 0.5;
        }

        private bool _isHeadComponentMeasurementsVisible;
        public bool IsHeadComponentMeasurementsVisible
        {
            get
            {
                return _isHeadComponentMeasurementsVisible;
            }
            set
            {
                _isHeadComponentMeasurementsVisible = value;
                OnPropertyChanged("IsHeadComponentMeasurementsVisible");
            }
        }

        private bool _isGlenoidVectorVisible;
        public bool IsGlenoidVectorVisible
        {
            get
            {
                return _isGlenoidVectorVisible;
            }
            set
            {
                _isGlenoidVectorVisible = value;
                OnPropertyChanged("IsGlenoidVectorVisible");
            }
        }

        private bool _isHeadComponentVisible;
        public bool IsHeadComponentVisible
        {
            get
            {
                return _isHeadComponentVisible;
            }
            set
            {
                _isHeadComponentVisible = value;
                OnPropertyChanged("IsHeadComponentVisible");
            }
        }

        private bool _isScapulaReamedVisible;
        public bool IsScapulaReamedVisible
        {
            get
            {
                return _isScapulaReamedVisible;
            }
            set
            {
                _isScapulaReamedVisible = value;
                OnPropertyChanged("IsScapulaReamedVisible");
            }
        }

        private bool _isReconsScapulaVisible;
        public bool IsReconsScapulaVisible
        {
            get
            {
                return _isReconsScapulaVisible;
            }
            set
            {
                _isReconsScapulaVisible = value;
                OnPropertyChanged("IsReconsScapulaVisible");
            }
        }

        private bool _isFullSphereVisible;
        public bool IsFullSphereVisible
        {
            get
            {
                return _isFullSphereVisible;
            }
            set
            {
                _isFullSphereVisible = value;
                OnPropertyChanged("IsFullSphereVisible");
            }
        }

        private bool _isCylinderVisible;
        public bool IsCylinderVisible
        {
            get
            {
                return _isCylinderVisible;
            }
            set
            {
                _isCylinderVisible = value;
                OnPropertyChanged("IsCylinderVisible");
            }
        }

        private bool _isTaperMantleSafetyZoneVisible;
        public bool IsTaperMantleSafetyZoneVisible
        {
            get
            {
                return _isTaperMantleSafetyZoneVisible;
            }
            set
            {
                _isTaperMantleSafetyZoneVisible = value;
                OnPropertyChanged("IsTaperMantleSafetyZoneVisible");
            }
        }

        private bool _isProductionRodVisible;
        public bool IsProductionRodVisible
        {
            get
            {
                return _isProductionRodVisible;
            }
            set
            {
                _isProductionRodVisible = value;
                OnPropertyChanged("IsProductionRodVisible");
            }
        }

        private double _headComponentOpacity;
        public double HeadComponentOpacity
        {
            get
            {
                return _headComponentOpacity;
            }
            set
            {
                if (value > 0.0)
                {
                    _headComponentOpacity = value;
                    OnPropertyChanged("HeadComponentOpacity");
                }
            }
        }

        private double _scapulaReamedOpacity;
        public double ScapulaReamedOpacity
        {
            get
            {
                return _scapulaReamedOpacity;
            }
            set
            {
                if (value > 0.0)
                {
                    _scapulaReamedOpacity = value;
                    OnPropertyChanged("ScapulaReamedOpacity");
                }
            }
        }

        private double _reconsScapulaOpacity;
        public double ReconsScapulaOpacity
        {
            get
            {
                return _reconsScapulaOpacity;
            }
            set
            {
                if (value > 0.0)
                {
                    _reconsScapulaOpacity = value;
                    OnPropertyChanged("ReconsScapulaOpacity");
                }
            }
        }

        private double _fullSphereOpacity;
        public double FullSphereOpacity
        {
            get
            {
                return _fullSphereOpacity;
            }
            set
            {
                if (value > 0.0)
                {
                    _fullSphereOpacity = value;
                    OnPropertyChanged("FullSphereOpacity");
                }
            }
        }

        private double _cylinderOpacity;
        public double CylinderOpacity
        {
            get
            {
                return _cylinderOpacity;
            }
            set
            {
                if (value > 0.0)
                {
                    _cylinderOpacity = value;
                    OnPropertyChanged("CylinderOpacity");
                }
            }
        }

        private double _taperMantleSafetyZoneOpacity;
        public double TaperMantleSafetyZoneOpacity
        {
            get
            {
                return _taperMantleSafetyZoneOpacity;
            }
            set
            {
                if (value > 0.0)
                {
                    _taperMantleSafetyZoneOpacity = value;
                    OnPropertyChanged("TaperMantleSafetyZoneOpacity");
                }
            }
        }

        private double _productionRodOpacity;
        public double ProductionRodOpacity
        {
            get
            {
                return _productionRodOpacity;
            }
            set
            {
                if (value > 0.0)
                {
                    _productionRodOpacity = value;
                    OnPropertyChanged("ProductionRodOpacity");
                }
            }
        }
        #endregion

        #region Measurements
        private bool _isBoneHeadVicinityOK;
        public bool IsBoneHeadVicinityOK
        {
            get
            {
                return _isBoneHeadVicinityOK;
            }
            set
            {
                _isBoneHeadVicinityOK = value;
                OnPropertyChanged("IsBoneHeadVicinityOK");
            }
        }

        private bool _isBoneTaperVicinityOK;
        public bool IsBoneTaperVicinityOK
        {
            get
            {
                return _isBoneTaperVicinityOK;
            }
            set
            {
                _isBoneTaperVicinityOK = value;
                OnPropertyChanged("IsBoneTaperVicinityOK");
            }
        }
        #endregion

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
    }
}
