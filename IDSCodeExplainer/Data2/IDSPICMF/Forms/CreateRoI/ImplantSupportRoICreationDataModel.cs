using IDS.CMF.DataModel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace IDS.PICMF.Forms
{
    public enum EMetalIntegrationState
    {
        Remain,
        Remove,
        Unselected
    }

    public class MetalIntegrationInfo
    {
        public Mesh SelectedMesh { get; set; }
        public EMetalIntegrationState State { get; set; }
        public double CustomOffset { get; set; } = double.NaN;
    }

    public class ImplantMetalDataModel : INotifyPropertyChanged
    {
        private readonly object _threadLock = new object();
        private bool _enable;
        private double _defaultRemovedMetalOffset;
        private double _defaultRemainedMetalOffset;
        private List<MetalIntegrationInfo> _selectedMetalInfos;
        public Mesh IntegratedRemovedMetal { get; set; }
        public Mesh IntegratedRemainedMetal { get; set; }

        public ImplantMetalDataModel(ImplantSupportRoICreationData data)
        {
            _enable = data.HasMetalIntegration;
            _defaultRemovedMetalOffset = data.ResultingOffsetForRemovedMetal;
            _defaultRemainedMetalOffset = data.ResultingOffsetForRemainedMetal;
            _selectedMetalInfos = new List<MetalIntegrationInfo>();
        }

        public bool Enable
        {
            get
            {
                lock (_threadLock)
                {
                    return _enable;
                }
            }
            set
            {
                lock (_threadLock)
                {
                    _enable = value;
                }

                OnPropertyChanged("Enable");
            }
        }

        public double DefaultRemovedMetalOffset
        {
            get
            {
                lock (_threadLock)
                {
                    return Math.Round(_defaultRemovedMetalOffset, 2, MidpointRounding.AwayFromZero);
                }
            }
            set
            {
                lock (_threadLock)
                {
                    _defaultRemovedMetalOffset = Math.Round(value, 2, MidpointRounding.AwayFromZero);
                }
                OnPropertyChanged("DefaultRemovedMetalOffset");
            }
        }

        public double DefaultRemainedMetalOffset
        {
            get
            {
                lock (_threadLock)
                {
                    return Math.Round(_defaultRemainedMetalOffset, 2, MidpointRounding.AwayFromZero);
                }
            }
            set
            {
                lock (_threadLock)
                {
                    _defaultRemainedMetalOffset = Math.Round(value, 2, MidpointRounding.AwayFromZero);
                }
                OnPropertyChanged("DefaultRemainedMetalOffset");
            }
        }

        public List<MetalIntegrationInfo> SelectedMetalInfos    //Don't edit the list since it use by multiple thread 
        {
            get => _selectedMetalInfos;
            set
            {
                _selectedMetalInfos = value;
                OnPropertyChanged("SelectedMetalInfos");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ImplantSupportRoICreationDataModel : SupportRoICreationDataModel
    {
        public ImplantMetalDataModel Metal { get; }

        public ImplantSupportRoICreationDataModel(ImplantSupportRoICreationData data) : base(data)
        {
            Metal = new ImplantMetalDataModel(data);
            Metal.PropertyChanged += PropertyChanged;
        }

        protected override void UnsubscribeToPropertyChanged()
        {
            Metal.PropertyChanged -= PropertyChanged;
            base.UnsubscribeToPropertyChanged();
        }

        protected override void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ImplantMetalDataModel)
            {
                Metal.IntegratedRemovedMetal = null;
                Metal.IntegratedRemainedMetal = null;
                IsDirty = true;
            }

            base.PropertyChanged(sender, e);
        }

        public ImplantSupportRoICreationData GetData()
        {
            return new ImplantSupportRoICreationData
            {
                HasMetalIntegration = Metal.Enable,
                ResultingOffsetForRemovedMetal = Metal.DefaultRemovedMetalOffset,
                ResultingOffsetForRemainedMetal = Metal.DefaultRemainedMetalOffset,
                HasTeethIntegration = Teeth.Enable,
                ResultingOffsetForTeeth = Teeth.Offset,
            };
        }
    }
}
