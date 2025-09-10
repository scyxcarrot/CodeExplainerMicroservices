using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace IDS.PICMF.Forms
{
    public class TeethDataModel : INotifyPropertyChanged
    {
        private readonly object _threadLock = new object();
        private bool _enable;
        private double _offset;
        private List<Mesh> _selectedTeethParts;
        public Mesh IntegratedTeeth { get; set; }

        public TeethDataModel(SupportRoICreationData data)
        {
            _enable = data.HasTeethIntegration;
            _offset = data.ResultingOffsetForTeeth;
            _selectedTeethParts = new List<Mesh>();
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

        public List<Mesh> SelectedTeethParts
        {
            get => _selectedTeethParts;
            set
            {
                _selectedTeethParts = value;
                OnPropertyChanged("SelectedTeethParts");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SupportRoICreationDataModel
    {
        // Noted: please not edit the data model in ImplantSupportRoICreationDialog
        // except MetalDataModel.Enable, MetalDataModel.Offset, TeethDataModel.Enable, TeethDataModel.Offset
        // Because other than that variable are not thread safe
        
        public TeethDataModel Teeth { get; }
        public bool IsDirty { get; protected set; }

        public SupportRoICreationDataModel(SupportRoICreationData data)
        {
            Teeth = new TeethDataModel(data);
            Teeth.PropertyChanged += PropertyChanged;
            IsDirty = false;
        }

        public void CleanUp()
        {
            UnsubscribeToPropertyChanged();
        }

        protected virtual void UnsubscribeToPropertyChanged()
        {
            Teeth.PropertyChanged -= PropertyChanged;
        }

        protected virtual void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is TeethDataModel)
            {
                Teeth.IntegratedTeeth = null;
                IsDirty = true;
            }

            ConduitUtilities.RefeshConduit();
        }
    }
}
