using IDS.CMF.DataModel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace IDS.PICMF.Forms
{
    public class RoIDataModel : INotifyPropertyChanged
    {
        private Mesh _drawnRoI;
        public Mesh DrawnRoI
        {
            get => _drawnRoI;
            set
            {
                _drawnRoI = value;

                OnPropertyChanged("DrawnRoI");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GuideMetalDataModel : INotifyPropertyChanged
    {
        private readonly object _threadLock = new object();
        private bool _enable;
        private double _offset;
        private List<Mesh> _selectedMetalParts;
        public Mesh IntegratedMetal { get; set; }

        public GuideMetalDataModel(GuideSupportRoICreationData data)
        {
            _enable = data.HasMetalIntegration;
            _offset = data.ResultingOffsetForMetal;
            _selectedMetalParts = new List<Mesh>();
            SelectedMetalPartIds = new List<Guid>();
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

        public double Offset
        {
            get
            {
                lock (_threadLock)
                {
                    return Math.Round(_offset, 2, MidpointRounding.AwayFromZero);
                }
            }
            set
            {
                lock (_threadLock)
                {
                    _offset = Math.Round(value, 2, MidpointRounding.AwayFromZero);
                }
                OnPropertyChanged("Offset");
            }
        }

        public List<Mesh> SelectedMetalParts    //Don't edit the list since it use by multiple thread 
        {
            get => _selectedMetalParts;
            set
            {
                _selectedMetalParts = value;
                SelectedMetalPartIds.Clear();
                OnPropertyChanged("SelectedMetalParts");
            }
        }

        //SelectedMetalPartIds is cleared everytime there is a setting of SelectedMetalParts,
        //so, set SelectedMetalPartIds AFTER setting SelectedMetalParts
        public List<Guid> SelectedMetalPartIds { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GuideSupportRoICreationDataModel : SupportRoICreationDataModel
    {
        public RoIDataModel RoI { get; }
        public GuideMetalDataModel Metal { get; }

        public Mesh RoIPreview { get; set; }

        public GuideSupportRoICreationDataModel(GuideSupportRoICreationData data) : base(data)
        {
            RoI = new RoIDataModel();
            Metal = new GuideMetalDataModel(data);

            RoI.PropertyChanged += PropertyChanged;
            Metal.PropertyChanged += PropertyChanged;
        }

        protected override void UnsubscribeToPropertyChanged()
        {
            RoI.PropertyChanged -= PropertyChanged;
            Metal.PropertyChanged -= PropertyChanged;

            base.UnsubscribeToPropertyChanged();
        }

        protected override void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is RoIDataModel)
            {
                Metal.IntegratedMetal = null;
                //Teeth integration is not affected by RoI
                IsDirty = true;
            }
            else if (sender is GuideMetalDataModel)
            {
                Metal.IntegratedMetal = null;
                IsDirty = true;
            }

            base.PropertyChanged(sender, e);

            RoIPreview = null;
        }

        public GuideSupportRoICreationData GetData()
        {
            return new GuideSupportRoICreationData
            {
                HasMetalIntegration = Metal.Enable,
                ResultingOffsetForMetal = Metal.Offset,
                HasTeethIntegration = Teeth.Enable,
                ResultingOffsetForTeeth = Teeth.Offset,
            };
        }
    }
}
